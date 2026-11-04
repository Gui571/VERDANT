using Fluitown.Domain;
namespace Verdant.Terrain;

public enum TerrainBiome { Woodland, Marsh, Ember, Frost }
public enum GroundKind : byte { Grass, Stone, Water, Path, Bridge }
public sealed record TerrainRecipe(ulong Seed=42,TerrainBiome Biome=TerrainBiome.Woodland,int Relief=3,int Water=2,int Forest=3,bool Arena=false)
{
    public void Validate(){if(!Enum.IsDefined(Biome)||Relief is <0 or >5||Water is <0 or >5||Forest is <0 or >5)throw new ArgumentException("Receita de terreno inválida");}
}
public readonly record struct TerrainCell(byte Level,GroundKind Kind,byte Tree);
public sealed class TerrainMap
{
    public const int Width=48,Depth=32;
    public const float StepHeight=1f;
    readonly TerrainCell[] cells;
    public TerrainRecipe Recipe {get;}
    public TerrainCell this[int x,int z]=>cells[z*Width+x];
    public TerrainMap(TerrainRecipe recipe,TerrainCell[] data){if(data.Length!=Width*Depth)throw new ArgumentException("Tamanho inválido");Recipe=recipe;cells=(TerrainCell[])data.Clone();}
    public static TerrainMap Generate(TerrainRecipe recipe)
    {
        recipe.Validate();var cells=new TerrainCell[Width*Depth];
        // Dobras da semente de 64 bits; o ruído original trabalha com inteiros de 32 bits.
        double seed=(uint)(recipe.Seed^(recipe.Seed>>32));
        for(int z=0;z<Depth;z++)for(int x=0;x<Width;x++){
            int bx=x/2*2,bz=z/2*2;
            double noise=Elevation.fractalValueNoise(seed,bx,bz,19,3,2,.4);
            double peakX=9+Elevation.latticeHash(seed+120,0,0)*28;
            double ridge=Math.Max(0,.8-((bx-peakX)*(bx-peakX)/340+(bz-5)*(bz-5)/190));
            int level=1+(int)Math.Floor(Math.Clamp((noise-.28)*1.6+ridge,0,.999)*recipe.Relief);
            double river=Width*.64+(Elevation.valueNoise(seed+19,0,bz,14)-.5)*18;
            bool water=recipe.Water>0&&(Math.Abs(bx-river)<recipe.Water*1.1||noise<.26+recipe.Water*.018);
            GroundKind kind=water?GroundKind.Water:level>=3?GroundKind.Stone:GroundKind.Grass;
            if(water)level=0;
            // Trilha contínua e pontes legíveis; na arena a clareira inteira é plana para preservar o combate.
            bool path=Math.Abs(z-(Depth/2+(int)((Elevation.valueNoise(seed+71,x,0,11)-.5)*4)))<=2;
            if(path){kind=water?GroundKind.Bridge:GroundKind.Path;level=1;}
            bool arena=recipe.Arena&&x>=7&&x<=40&&z>=7&&z<=24;
            if(arena){kind=GroundKind.Grass;level=1;}
            byte tree=0;
            if(!arena&&!path&&!water&&x>1&&z>1&&x<Width-2&&z<Depth-2&&Elevation.latticeHash(seed+911,x,z)<recipe.Forest*.035)
                tree=(byte)(1+(int)(Elevation.latticeHash(seed+42,x,z)*3));
            cells[z*Width+x]=new((byte)level,kind,tree);
        }
        return new(recipe,cells);
    }
    public byte[] FingerprintBytes()=>cells.SelectMany(c=>new[]{c.Level,(byte)c.Kind,c.Tree}).ToArray();
}
