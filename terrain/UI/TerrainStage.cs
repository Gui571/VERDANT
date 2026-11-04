using Godot;
namespace Verdant.Terrain;

// Malha fixa por receita; só a água varia no shader, nunca na simulação.
public partial class TerrainStage : Node3D
{
    public TerrainMap Map {get;private set;}=null!;
    public Camera3D Camera {get;private set;}=null!;
    Node3D? geometry;
    readonly List<Resource> owned=new();
    float yaw=18,zoom=1;
    public int TriangleCount {get;private set;}
    public void Build(TerrainRecipe recipe,bool arena=false)
    {
        if(geometry!=null){RemoveChild(geometry);geometry.Free();}foreach(var resource in owned)resource.Dispose();owned.Clear();
        Map=TerrainMap.Generate(recipe with{Arena=arena});geometry=new Node3D();AddChild(geometry);
        if(Camera==null){Camera=new Camera3D{Projection=Camera3D.ProjectionType.Orthogonal,Current=true,Near=.1f,Far=200};AddChild(Camera);}
        yaw=arena?0:18;zoom=1;Aim(arena);
        var colors=Palette.For(recipe.Biome);var land=new MeshBatch();var water=new MeshBatch();
        for(int z=0;z<TerrainMap.Depth;z++)for(int x=0;x<TerrainMap.Width;x++){
            var cell=Map[x,z];float wx=x-TerrainMap.Width/2f,wz=z-TerrainMap.Depth/2f;
            float h=cell.Kind==GroundKind.Water?.13f:cell.Level*TerrainMap.StepHeight;
            var color=cell.Kind switch{GroundKind.Stone=>colors.Rock,GroundKind.Path=>colors.Path,GroundKind.Bridge=>colors.Wood,_=>colors.Grass};
            float variation=(float)Fluitown.Domain.Elevation.latticeHash((uint)recipe.Seed+14,x/2,z/2);
            color=Shade(color,.94f+variation*.09f);
            var batch=cell.Kind==GroundKind.Water?water:land;
            if(cell.Kind==GroundKind.Water)color=colors.Water;
            batch.Quad(new(wx,h,wz),new(wx+1,h,wz),new(wx+1,h,wz+1),new(wx,h,wz+1),color);
            if(cell.Kind==GroundKind.Water)continue;
            foreach(var (dx,dz) in new[]{(0,-1),(1,0),(0,1),(-1,0)}){
                int nx=x+dx,nz=z+dz;float low=-1;
                if(nx>=0&&nz>=0&&nx<TerrainMap.Width&&nz<TerrainMap.Depth){var n=Map[nx,nz];low=n.Kind==GroundKind.Water?.13f:n.Level*TerrainMap.StepHeight;}
                if(low>=h)continue;
                Vector3 a,b;
                if(dx==1){a=new(wx+1,h,wz);b=new(wx+1,h,wz+1);}else if(dx==-1){a=new(wx,h,wz+1);b=new(wx,h,wz);}else if(dz==1){a=new(wx+1,h,wz+1);b=new(wx,h,wz+1);}else{a=new(wx,h,wz);b=new(wx+1,h,wz);}
                var cliff=Shade(colors.Rock,dx==1?.64f:dz==1?.79f:.92f);
                land.Quad(a,b,new(b.X,low,b.Z),new(a.X,low,a.Z),cliff);
                // Contorno no topo e estratos na parede substituem o pós-processamento Forward+ da origem.
                Vector3 inward=new(-dx*.09f,0,-dz*.09f);land.Quad(a+Vector3.Up*.012f,b+Vector3.Up*.012f,b+inward+Vector3.Up*.012f,a+inward+Vector3.Up*.012f,colors.Ink);
                for(float y=low+.4f;y<h-.15f;y+=.7f)land.Quad(new(a.X+dx*.006f,y,a.Z+dz*.006f),new(b.X+dx*.006f,y,b.Z+dz*.006f),new(b.X+dx*.006f,y+.05f,b.Z+dz*.006f),new(a.X+dx*.006f,y+.05f,a.Z+dz*.006f),Shade(cliff,.75f));
                if(low==.13f){Vector3 shore=new(dx*.14f,0,dz*.14f);Vector3 aa=new(a.X,.145f,a.Z),bb=new(b.X,.145f,b.Z);water.Quad(aa,bb,bb+shore,aa+shore,colors.Foam);}
            }
            if(cell.Kind==GroundKind.Bridge){
                for(int plank=0;plank<4;plank++)land.Box(new(wx+plank*.25f,h+.02f,wz),new(.025f,.015f,1),Shade(colors.Wood,.6f));
                if(x%2==0){land.Box(new(wx,h,wz),new(.1f,.4f,.1f),colors.Wood);land.Box(new(wx,h,wz+.9f),new(.1f,.4f,.1f),colors.Wood);}
            }
            if(cell.Tree>0){
                // Sombra de contato e copa facetada; agrupadas na mesma malha do terreno.
                land.Disc(new(wx+.65f,h+.02f,wz+.6f),.75f,Shade(colors.Grass,.66f));
                land.Box(new(wx+.4f,h,wz+.4f),new(.18f,1.15f,.18f),colors.Wood);
                float tall=cell.Tree*.18f;var leaf=cell.Tree==3?colors.LeafAccent:colors.Leaf;
                land.Crown(new(wx+.5f,h+1.05f,wz+.5f),.9f,1.1f+tall,leaf);
                land.Crown(new(wx+.35f,h+1.7f,wz+.4f),.62f,.75f,Shade(leaf,1.12f));
            }else if(cell.Kind==GroundKind.Grass&&variation>.76f){
                land.Quad(new(wx+.2f,h+.016f,wz+.2f),new(wx+.45f,h+.016f,wz+.2f),new(wx+.4f,h+.016f,wz+.25f),new(wx+.2f,h+.016f,wz+.25f),Shade(colors.Grass,1.16f));
            }
        }
        TriangleCount=land.Count/3+water.Count/3;
        var material=new StandardMaterial3D{VertexColorUseAsAlbedo=true,ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,CullMode=BaseMaterial3D.CullModeEnum.Disabled};owned.Add(material);
        Add(land,material);
        if(water.Count>0){var shader=new Shader{Code=WaterShader};owned.Add(shader);var mat=new ShaderMaterial{Shader=shader};mat.SetShaderParameter("water_color",colors.Water);mat.SetShaderParameter("foam_color",colors.Foam);owned.Add(mat);Add(water,mat);}
        void Add(MeshBatch batch,Material mat){var mesh=batch.Mesh();owned.Add(mesh);geometry.AddChild(new MeshInstance3D{Mesh=mesh,MaterialOverride=mat});}
    }
    public void ShowObstacles(IEnumerable<Verdant.Battle.Obstacle> obstacles){
        var batch=new MeshBatch();foreach(var o in obstacles)batch.Box(new(o.X/10-16,TerrainMap.StepHeight,o.Y/10-7.5f),new(o.Width/10,.7f,o.Height/10),new Color("82918e"));
        if(batch.Count==0)return;var mesh=batch.Mesh();var material=new StandardMaterial3D{VertexColorUseAsAlbedo=true,ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,CullMode=BaseMaterial3D.CullModeEnum.Disabled};owned.Add(mesh);owned.Add(material);geometry!.AddChild(new MeshInstance3D{Mesh=mesh,MaterialOverride=material});
    }
    public void Turn(float degrees){yaw+=degrees;Aim(Map.Recipe.Arena);}
    public void Zoom(float delta){zoom=Math.Clamp(zoom+delta,.65f,1.5f);Aim(Map.Recipe.Arena);}
    void Aim(bool arena){float rad=Mathf.DegToRad(yaw);Camera.Position=new(Mathf.Sin(rad)*30,38,Mathf.Cos(rad)*30);Camera.LookAt(new(0,TerrainMap.StepHeight,0));Camera.Size=(arena?28:38)*zoom;}
    public Vector2 Project(System.Numerics.Vector2 p)=>Camera.UnprojectPosition(new(p.X/10-16,TerrainMap.StepHeight,p.Y/10-7.5f));
    public System.Numerics.Vector2 Unproject(Vector2 pixel){var origin=Camera.ProjectRayOrigin(pixel);var direction=Camera.ProjectRayNormal(pixel);var p=origin+direction*((TerrainMap.StepHeight-origin.Y)/direction.Y);return new(Math.Clamp((p.X+16)*10,0,320),Math.Clamp((p.Z+7.5f)*10,0,150));}
    public override void _ExitTree(){foreach(var resource in owned)resource.Dispose();owned.Clear();}
    static Color Shade(Color c,float k)=>new(Math.Min(c.R*k,1),Math.Min(c.G*k,1),Math.Min(c.B*k,1),1);
    const string WaterShader="""
shader_type spatial;
render_mode unshaded, cull_disabled;
uniform vec4 water_color : source_color;
uniform vec4 foam_color : source_color;
varying vec3 position;
void vertex(){ position=VERTEX; }
void fragment(){
    float ripple=step(.94,sin(position.x*3.5+position.z*4.0+TIME*.9))*step(.1,sin(position.z*2.0-TIME*.3));
    ALBEDO=mix(COLOR.rgb,foam_color.rgb,ripple*.28);
}
""";
    sealed record Palette(Color Grass,Color Rock,Color Path,Color Wood,Color Leaf,Color LeafAccent,Color Water,Color Foam)
    {
        public Color Ink=>new("27363e");
        public static Palette For(TerrainBiome b)=>b switch{
            TerrainBiome.Marsh=>new(new("708765"),new("657e77"),new("a6a17a"),new("66504f"),new("496b69"),new("9e8aac"),new("457d89"),new("a1cbb2")),
            TerrainBiome.Ember=>new(new("9b7654"),new("554e5f"),new("d4a476"),new("63414a"),new("a64b4b"),new("dc9463"),new("c95538"),new("f8cf7e")),
            TerrainBiome.Frost=>new(new("c9dedc"),new("75899d"),new("aebcc6"),new("657080"),new("648e9c"),new("abced3"),new("548da7"),new("e0f4ed")),
            _=>new(new("91ac69"),new("9b9e8b"),new("c6b387"),new("775847"),new("4d8268"),new("b2bd74"),new("409caa"),new("a4ded2"))};
    }
    sealed class MeshBatch
    {
        readonly List<Vector3> vertices=new();readonly List<Color> colors=new();public int Count=>vertices.Count;
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color){Tri(a,b,c,color);Tri(a,c,d,color);}
        void Tri(Vector3 a,Vector3 b,Vector3 c,Color color){vertices.Add(a);vertices.Add(b);vertices.Add(c);colors.Add(color);colors.Add(color);colors.Add(color);}
        public void Box(Vector3 p,Vector3 size,Color color){Vector3 a=p+Vector3.Up*size.Y,b=a+Vector3.Right*size.X,c=b+Vector3.Back*size.Z,d=a+Vector3.Back*size.Z;Quad(a,b,c,d,color);Quad(p,p+Vector3.Right*size.X,b,a,Shade(color,.83f));Quad(b,b-Vector3.Up*size.Y,c-Vector3.Up*size.Y,c,Shade(color,.65f));Quad(c,c-Vector3.Up*size.Y,d-Vector3.Up*size.Y,d,Shade(color,.78f));Quad(d,d-Vector3.Up*size.Y,p,a,Shade(color,.92f));}
        public void Disc(Vector3 p,float radius,Color color){for(int i=0;i<8;i++){float a=i*Mathf.Tau/8,b=(i+1)*Mathf.Tau/8;Tri(p,p+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius*.65f),p+new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius*.65f),color);}}
        public void Crown(Vector3 p,float radius,float height,Color color){for(int i=0;i<7;i++){float a=i*Mathf.Tau/7,b=(i+1)*Mathf.Tau/7;Vector3 ra=new(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius),rb=new(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius);Vector3 top=p+Vector3.Up*height;Quad(p+ra,p+rb,p+rb*.7f+Vector3.Up*height*.65f,p+ra*.7f+Vector3.Up*height*.65f,Shade(color,.68f+i*.055f));Tri(p+ra*.7f+Vector3.Up*height*.65f,p+rb*.7f+Vector3.Up*height*.65f,top,Shade(color,.88f+i*.025f));}}
        public ArrayMesh Mesh(){var arrays=new Godot.Collections.Array();arrays.Resize((int)Godot.Mesh.ArrayType.Max);arrays[(int)Godot.Mesh.ArrayType.Vertex]=vertices.ToArray();arrays[(int)Godot.Mesh.ArrayType.Color]=colors.ToArray();var mesh=new ArrayMesh();mesh.AddSurfaceFromArrays(Godot.Mesh.PrimitiveType.Triangles,arrays);return mesh;}
    }
}
