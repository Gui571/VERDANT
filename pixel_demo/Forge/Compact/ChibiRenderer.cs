using Verdant.Forge.StyleV2;
namespace Verdant.Forge.Compact;

public enum Facing { South, East, North, West }

// Anatomia própria em pixels inteiros. A receita e os números de combate continuam os mesmos.
public sealed class ChibiRenderer
{
    public ForgeSpec Spec {get;}
    public Raster Buffer {get;}=new(32,32);
    public Layer Parts {get;private set;}=new();
    public MaterialStyle? Style {get;set;}
    public bool GroundShadow {get;set;}=true;
    public byte[] MaterialIds {get;}=new byte[1024];
    public static int DefaultTeam(ForgeSpec spec)=>4;
    public static MaterialStyle Look(ForgeSpec spec,int? team=null){
        var look=MaterialStyle.For(spec,team??DefaultTeam(spec));
        if((team??4)==4)look=look with{TeamRamp=MaterialRamps.Cornflower};
        return spec.Species==Species.Duck?look with{Fur=MaterialRamps.Rabbit}:look;
    }
    public ChibiRenderer(ForgeSpec spec){Spec=spec;}
    int bx,by,hy,flutter,hand,attack=-1;bool back,blink;
    static readonly int[] Stride={0,1,1,0,0,-1,-1,0};
    public Raster Render(double seconds,AdventurerMotion motion=AdventurerMotion.Idle,Facing facing=Facing.East)
    {
        Parts=new();Buffer.Clear();Array.Clear(MaterialIds);
        int frame=(int)(Math.Max(0,seconds)*(motion==AdventurerMotion.Run?12:8));
        bx=by=hand=flutter=0;attack=-1;blink=false;back=facing==Facing.North;
        int step=0;
        if(motion==AdventurerMotion.Idle){frame%=32;by=frame is >=10 and <=15?-1:0;flutter=frame is >=12 and <=17?1:0;blink=frame==26;}
        if(motion is AdventurerMotion.Walk or AdventurerMotion.Run){frame%=8;step=Stride[frame];by=frame is 2 or 3 or 6?-1:0;flutter=frame is 3 or 4 or 5?1:0;}
        if(motion==AdventurerMotion.Attack){attack=Math.Min(5,frame);bx=new[]{-1,-1,2,1,0,0}[attack];by=new[]{1,0,1,0,-1,0}[attack];hand=new[]{0,-2,-1,0,1,0}[attack];flutter=attack is 3 or 4?1:0;}
        if(motion is AdventurerMotion.Special or AdventurerMotion.Cast){frame=Math.Min(7,frame);attack=Math.Clamp(frame-1,0,5);bx=frame is 3 or 4?1:0;hand=frame is >=1 and <=5?-2:0;flutter=frame is 3 or 4?1:0;}
        if(motion==AdventurerMotion.Hurt){frame=Math.Min(3,frame);bx=frame<2?-1:0;by=frame==0?1:0;blink=true;}
        hy=14+by;
        DrawBack();
        DrawBody(step);
        DrawOffhand();DrawHead(facing);DrawHat();
        // Lenço curto e uma ponta móvel deixam o peito legível.
        if(Spec.Scarf){Parts.Rect(13+bx,22+by,7,1,Tone.White);Parts.Rect(17+bx,23+by,3,1,Tone.MetalL);Parts.Rect(8+bx-flutter,23+by,4,2,Tone.ClothL);}
        DrawWeapon();
        Parts.Rect((attack is 2 or 3?18:22)+bx,(attack is 2 or 3?23:24)+by+hand,2,2,Tone.FurM);
        if(back&&Spec.Back==BackGear.Cape){Parts.Rect(12+bx,23+by,8,4,Tone.ClothM);Parts.Rect(12+bx,23+by,2,3,Tone.ClothL);Parts.Rect(18+bx,25+by,2,2,Tone.ClothD);}
        Outline();
        if(GroundShadow)ForgeFinish.Shadow(Parts,16,29,5);
        if(facing==Facing.West){var reflected=new Layer();for(int y=0;y<32;y++)for(int x=0;x<32;x++)if(Parts.Get(x,y)!=Tone.None)reflected.Set(31-x,y,Parts.Get(x,y));Parts=reflected;}
        Paint(motion==AdventurerMotion.Hurt&&frame==0);
        if(motion is AdventurerMotion.Special or AdventurerMotion.Cast)ForgeEffects.Special(Buffer,Spec.Palette,frame,motion==AdventurerMotion.Cast);
        for(int i=0;i<Parts.Clipped;i++)Buffer.Pixel(-1,-1,default);
        return Buffer;
    }
    public Raster RenderDeath(double seconds,Facing facing=Facing.East)
    {
        int f=Math.Clamp((int)(Math.Max(0,seconds)*8),0,5);
        if(f==0)return Render(0,AdventurerMotion.Hurt,facing);
        if(f==5){Buffer.Clear();return Buffer;}
        Render(0,AdventurerMotion.Idle,facing);var old=Parts;Parts=new();Buffer.Clear();Array.Clear(MaterialIds);
        for(int y=2;y<29;y++)for(int x=3;x<23;x++){
            var tone=old.Get(x,y);if(tone is Tone.None or Tone.Ink)continue;
            if(f==1)Parts.Set(x,Math.Min(28,y+(29-y)/3),tone);
            else Parts.Set(y-1,Math.Clamp(27-(x-7),9,28),tone);
        }
        if(f>1){Parts.Line(19,27,27,27,Tone.MetalM);Parts.Line(17,27,19,27,Tone.LeatherM);Parts.Set(26,26,Tone.MetalL);}
        Outline();if(GroundShadow)ForgeFinish.Shadow(Parts,16,29,10);Paint(false);return Buffer;
    }
    void DrawBody(int step)
    {
        // A massa da roupa é larga; a perna distante fica sob a bainha, sem listras na cintura.
        Parts.Rect(11+step,26-(step<0?1:0),4,2,Tone.ClothD);
        Parts.Rect(17-step,27-(step>0?1:0),5,2,Tone.ClothD);
        Parts.Rect(18-step,27-(step>0?1:0),3,1,Tone.MetalM);
        int x=bx,y=by;
        Parts.Poly(Tone.ClothM,(11+x,21+y),(19+x,21+y),(22+x,25+y),(20+x,27),(13+x,27),(9+x,25+y));
        Parts.Poly(Tone.ClothD,(19+x,22+y),(22+x,25+y),(20+x,27),(16+x,27),(18+x,26+y));
        Parts.Rect(10+x,23+y,3,3,Tone.ClothL);
        // Gola curta: os claros unem rosto e ombro sem fatiar o tronco.
        if(Spec.Weapon==Weapon.Bow){Parts.Line(12+x,22+y,18+x,26+y,Tone.LeatherM);Parts.Set(16+x,25+y,Tone.AccM);}
        else if(Spec.Weapon==Weapon.Staff){Parts.Rect(12+x,22+y,2,4,Tone.MetalL);Parts.Set(15+x,23+y,Tone.AccM);}
        else if(Spec.Weapon==Weapon.Crook){Parts.Line(12+x,22+y,16+x,25+y,Tone.MetalL);Parts.Line(16+x,25+y,20+x,22+y,Tone.MetalL);}
        else {Parts.Rect(13+x,22+y,5,2,Tone.MetalL);Parts.Rect(15+x,24+y,3,1,Tone.White);}
        if(Spec.Outfit==Outfit.Plate){Parts.Rect(14+x,24+y,5,3,Tone.MetalM);Parts.Rect(14+x,24+y,3,2,Tone.MetalL);}
        if(Spec.Outfit==Outfit.Robe){Parts.Poly(Tone.ClothM,(12+x,25+y),(20+x,25+y),(21+x,28),(11+x,28));Parts.Line(12+x,26,12+x,27,Tone.ClothL);Parts.Line(19+x,26,20+x,27,Tone.ClothD);}
        if(Spec.Offhand==Offhand.None){
            int lift=Spec.Weapon==Weapon.Crook&&attack is >=1 and <=3?3:0;
            Parts.Rect(9+x,23+y-lift,3,2,Tone.MetalL);Parts.Rect(10+x,25+y-lift,2,2,Tone.FurM);
        }
    }
    void DrawHead(Facing facing)
    {
        DrawEars();
        // Planos em 3/4: bochecha avançada, crânio arredondado e sombra lateral.
        (int,int)[] spans=Spec.Species switch {
            Species.Fox=>new[]{(3,9),(1,11),(0,12),(0,12),(1,12),(2,13),(3,13),(4,12),(5,10)},
            Species.Owl=>new[]{(3,9),(1,11),(0,12),(0,12),(0,12),(1,11),(2,10),(3,9),(5,8)},
            Species.Turtle=>new[]{(3,9),(1,11),(0,12),(0,13),(0,13),(1,14),(2,14),(3,12),(5,10)},
            _=>new[]{(3,9),(1,11),(0,12),(0,13),(0,13),(1,13),(1,12),(2,11),(4,9)}
        };
        for(int y=0;y<spans.Length;y++)for(int x=spans[y].Item1;x<=spans[y].Item2;x++){
            Tone t=x<=3||y>=7?Tone.FurD:y<=1&&x<9?Tone.FurL:Tone.FurM;
            Parts.Set(9+bx+x,hy+y,t);
        }
        if(back){Parts.Rect(11+bx,hy+2,3,3,Tone.FurL);return;}
        if(Spec.Species is Species.Human or Species.Elf){Parts.Rect(9+bx,hy+1,3,5,Tone.LeatherD);Parts.Rect(12+bx,hy+1,3,2,Tone.LeatherM);Parts.Rect(11+bx,hy+5,2,2,Tone.FurL);}
        int eye=facing==Facing.South?13:16;
        if(Spec.Species is not (Species.Frog or Species.Owl)){Parts.Rect(eye+bx,hy+4,1,blink||facing is Facing.East or Facing.West?1:2,Tone.Ink);Parts.Rect(eye+4+bx,hy+4,1,blink?1:2,Tone.Ink);}
        switch(Spec.Species){
            case Species.Duck:Parts.Rect(19+bx,hy+6,5,2,Tone.AccM);Parts.Rect(20+bx,hy+7,4,1,Tone.AccD);break;
            case Species.Fox:Parts.Rect(18+bx,hy+6,5,2,Tone.White);Parts.Rect(15+bx,hy+7,4,1,Tone.White);Parts.Set(23+bx,hy+6,Tone.Ink);break;
            case Species.Owl:
                Parts.Rect(12+bx,hy+2,3,4,Tone.White);Parts.Rect(18+bx,hy+2,3,4,Tone.White);Parts.Rect(14+bx,hy+4,1,blink?1:2,Tone.Ink);Parts.Rect(19+bx,hy+4,1,blink?1:2,Tone.Ink);Parts.Set(17+bx,hy+6,Tone.AccM);break;
            case Species.Turtle:Parts.Rect(20+bx,hy+6,4,1,Tone.FurL);break;
            case Species.Frog:Parts.Rect(10+bx,hy-1,3,3,Tone.FurL);Parts.Rect(19+bx,hy-1,3,3,Tone.FurL);Parts.Rect(12+bx,hy,1,blink?1:2,Tone.Ink);Parts.Rect(20+bx,hy,1,blink?1:2,Tone.Ink);break;
            case Species.Rabbit:case Species.Mouse:case Species.Cat:Parts.Set(21+bx,hy+6,Tone.Pink);Parts.Rect(18+bx,hy+7,3,1,Tone.FurL);break;
            case Species.Bear:Parts.Rect(18+bx,hy+5,4,3,Tone.FurL);Parts.Rect(21+bx,hy+5,2,1,Tone.Ink);break;
        }
    }
    void DrawEars()
    {
        int y=hy;
        switch(Spec.Species){
            case Species.Rabbit:
                Parts.Rect(10+bx,y-7,3,8,Tone.FurM);Parts.Rect(11+bx,y-6,1,5,Tone.Pink);Parts.Set(10+bx,y-7,Tone.FurL);
                Parts.Rect(18+bx,y-8+flutter,3,9-flutter,Tone.FurM);Parts.Rect(19+bx,y-7+flutter,1,6-flutter,Tone.Pink);Parts.Set(18+bx,y-8+flutter,Tone.FurL);break;
            case Species.Fox:case Species.Cat:
                int ear=Spec.Species==Species.Cat?3:5;
                Parts.Poly(Tone.FurM,(9+bx,y+2),(10+bx,y-ear),(15+bx,y+1));Parts.Poly(Tone.FurM,(17+bx,y+1),(21+bx,y-ear+flutter),(22+bx,y+3));
                Parts.Line(11+bx,y-2,12+bx,y,Tone.Pink);Parts.Line(20+bx,y-2+flutter,19+bx,y,Tone.Pink);break;
            case Species.Mouse:case Species.Bear:
                int size=Spec.Species==Species.Bear?4:5;Parts.Rect(8+bx,y-2,size,size,Tone.FurM);Parts.Rect(19+bx,y-2,size,size,Tone.FurM);Parts.Set(9+bx,y-3,Tone.FurL);Parts.Set(20+bx,y-3,Tone.FurL);
                if(Spec.Species==Species.Mouse){Parts.Rect(9+bx,y-1,3,2,Tone.Pink);Parts.Rect(20+bx,y-1,3,2,Tone.Pink);}break;
            case Species.Elf:
                Parts.Poly(Tone.FurM,(6+bx,y+1),(11+bx,y+4),(9+bx,y+6));Parts.Poly(Tone.FurM,(25+bx,y+1),(20+bx,y+4),(22+bx,y+6));break;
            case Species.Owl:Parts.Line(10+bx,y-2,12+bx,y+1,Tone.FurM);Parts.Line(21+bx,y-2,19+bx,y+1,Tone.FurM);break;
        }
    }
    void DrawHat()
    {
        int x=bx,y=hy;
        switch(Spec.Headwear){
            case Headwear.Headband:
                Parts.Rect(9+x,y,13,2,Tone.ClothM);Parts.Rect(11+x,y,9,1,Tone.ClothL);Parts.Rect(7+x,y+1,3,2,Tone.ClothD);Parts.Rect(6+x-flutter,y+3,3,1,Tone.ClothM);break;
            case Headwear.IceHelm:
                Parts.Poly(Tone.MetalM,(9+x,y),(10+x,y-4),(13+x,y-6),(19+x,y-5),(22+x,y-2),(22+x,y+1));
                Parts.Poly(Tone.MetalL,(10+x,y-2),(13+x,y-5),(17+x,y-4),(19+x,y-2));
                Parts.Rect(10+x,y-2,11,2,Tone.ClothM);Parts.Rect(11+x,y-2,7,1,Tone.ClothL);
                Parts.Line(10+x,y-4,8+x,y-7,Tone.MetalL);Parts.Line(20+x,y-4,22+x,y-7,Tone.MetalM);
                Parts.Rect(8+x,y,16,2,Tone.MetalM);Parts.Rect(9+x,y,13,1,Tone.MetalL);
                Parts.Rect(9+x,y+2,3,5,Tone.MetalM);Parts.Rect(9+x,y+2,2,3,Tone.MetalL);break;
            case Headwear.WizardHat:
                Parts.Poly(Tone.ClothM,(10+x,y),(13+x,y-8),(19+x,y-9),(17+x,y-7),(19+x,y-5),(22+x,y));
                Parts.Poly(Tone.ClothL,(11+x,y-2),(14+x,y-7),(17+x,y-7),(16+x,y-5));
                Parts.Rect(11+x,y-1,10,1,Tone.MetalL);
                Parts.Rect(7+x,y+1,17,1,Tone.ClothM);Parts.Rect(8+x,y+2,15,1,Tone.ClothD);break;
            case Headwear.Hood:case Headwear.Cowl:
                Parts.Poly(Tone.ClothM,(8+x,y+6),(8+x,y),(11+x,y-4),(17+x,y-5),(22+x,y-1),(23+x,y+6),(21+x,y+8),(20+x,y+1),(12+x,y+1),(11+x,y+8));
                Parts.Line(9+x,y-1,12+x,y-3,Tone.ClothL);Parts.Line(12+x,y-3,17+x,y-3,Tone.ClothL);
                Parts.Rect(8+x,y+1,2,5,Tone.ClothD);Parts.Rect(22+x,y+2,1,5,Tone.ClothD);
                if(Spec.Headwear==Headwear.Cowl){Parts.Line(11+x,y+1,13+x,y,Tone.White);Parts.Line(13+x,y,20+x,y,Tone.MetalL);Parts.Line(21+x,y+1,21+x,y+6,Tone.MetalL);}
                if(back){Parts.Rect(10+x,y,12,7,Tone.ClothM);Parts.Rect(10+x,y,2,4,Tone.ClothL);Parts.Line(18+x,y+1,20+x,y+7,Tone.ClothD);}break;
            case Headwear.WideHat:
                Parts.Poly(Tone.ClothM,(10+x,y),(12+x,y-4),(18+x,y-4),(21+x,y));Parts.Rect(12+x,y-4,6,1,Tone.ClothL);
                Parts.Rect(7+x,y,18,1,Tone.MetalL);Parts.Rect(8+x,y+1,16,1,Tone.ClothD);break;
            case Headwear.Beret:
                Parts.Rect(10+x,y-2,11,2,Tone.ClothM);Parts.Rect(13+x,y-3,6,1,Tone.ClothL);Parts.Rect(10+x,y,12,1,Tone.ClothD);break;
            case Headwear.Crown:
                Parts.Rect(12+x,y-1,8,2,Tone.AccM);for(int i=12;i<21;i+=3)Parts.Rect(i+x,y-3,1,2,Tone.AccM);break;
        }
    }
    void DrawBack()
    {
        if(Spec.Back==BackGear.Tail){var tail=new Layer();
            if(Spec.Species==Species.Fox){tail.Poly(Tone.FurM,(12+bx,24),(7-flutter,20),(5-flutter,21),(5,25),(11+bx,27));tail.Line(5-flutter,21,7-flutter,23,Tone.White);}
            else {tail.Line(11+bx,26,6,25,Tone.FurM);tail.Line(6,25,5,22-flutter,Tone.FurM);}
            Parts.Composite(tail,true);
        }
        if(Spec.Back==BackGear.Cape){Parts.Poly(Tone.ClothD,(12+bx,21+by),(8+bx,22+by),(6+bx-flutter,26),(12+bx,27));Parts.Line(10+bx,22+by,8+bx-flutter,25,Tone.ClothM);}
        if(Spec.Back==BackGear.Quiver){Parts.Rect(8+bx,19+by,3,7,Tone.LeatherM);Parts.Line(9+bx,20+by,7+bx,13,Tone.LeatherM);Parts.Set(7+bx,12,Tone.MetalL);Parts.Line(10+bx,20+by,10+bx,12,Tone.LeatherM);Parts.Set(10+bx,11,Tone.MetalL);}
        if(Spec.Back==BackGear.Shell||Spec.Species==Species.Turtle){Parts.Rect(7+bx,18+by,5,9,Tone.FurD);Parts.Rect(7+bx,19+by,2,6,Tone.FurM);Parts.Line(9+bx,19+by,9+bx,25,Tone.AccD);}
    }
    void DrawOffhand()
    {
        var shield=new Layer();int x=bx,y=by;
        if(Spec.Offhand==Offhand.RoundShield){shield.Poly(Tone.MetalM,(6+x,21+y),(10+x,20+y),(13+x,22+y),(12+x,26+y),(9+x,28+y),(6+x,25+y));shield.Line(6+x,22+y,9+x,21+y,Tone.MetalL);shield.Rect(7+x,22+y,4,3,Tone.ClothM);shield.Rect(7+x,22+y,1,3,Tone.ClothL);shield.Rect(9+x,23+y,2,2,Tone.MetalL);}
        if(Spec.Offhand==Offhand.TowerShield){shield.Poly(Tone.MetalM,(5+x,20+y),(11+x,19+y),(12+x,26+y),(9+x,28+y),(6+x,26+y));shield.Poly(Tone.ClothM,(6+x,21+y),(10+x,20+y),(11+x,26+y),(9+x,27+y),(7+x,26+y));shield.Line(6+x,21+y,9+x,20+y,Tone.MetalL);shield.Line(8+x,22+y,9+x,25+y,Tone.MetalM);}
        if(Spec.Offhand==Offhand.Tome){int lift=attack is >=1 and <=3?2:0;shield.Rect(6+x,23+y-lift,6,4,Tone.ClothD);shield.Rect(7+x,23+y-lift,4,2,Tone.White);shield.Line(9+x,23+y-lift,9+x,25+y-lift,Tone.AccM);}
        Parts.Composite(shield,true);
    }
    void DrawWeapon()
    {
        var w=new Layer();int x=bx,y=by+(Spec.Weapon==Weapon.Bow?0:hand);
        bool striking=attack is 2 or 3;
        int mode=striking?2:attack==1?1:0;
        int hx=(mode==2?15:23)+x,hy=(mode==2?23:25)+y;
        (int x,int y) Point(int u,int v)=>mode switch{2=>(hx+(Spec.Weapon==Weapon.Spear?u*2/3:u),hy+v),1=>(hx-u/3+v,hy-u),_=>(hx+v+(Spec.Weapon is Weapon.Sword or Weapon.Daggers or Weapon.Axe?u/4:0),hy-u)};
        void Line(int u0,int v0,int u1,int v1,Tone t){var a=Point(u0,v0);var b=Point(u1,v1);w.Line(a.x,a.y,b.x,b.y,t);}
        void Rect(int u0,int v0,int u1,int v1,Tone t){var a=Point(u0,v0);var b=Point(u1,v0);var c=Point(u1,v1);var d=Point(u0,v1);w.Poly(t,a,b,c,d);}
        switch(Spec.Weapon){
            case Weapon.Sword:case Weapon.Daggers:
                int blade=Spec.Weapon==Weapon.Daggers?8:11;
                Line(-1,0,2,0,Tone.LeatherM);Line(2,-2,2,2,Tone.AccM);Rect(3,-1,blade,2,Tone.MetalM);Line(4,-1,blade,-1,Tone.MetalL);Line(blade,0,blade+2,0,Tone.MetalL);Line(blade+1,0,blade+2,0,Tone.White);
                if(Spec.Weapon==Weapon.Daggers){w.Line(7+x,24+y,4+x,19+y,Tone.MetalL);w.Line(7+x,25+y,5+x,22+y,Tone.LeatherM);}break;
            case Weapon.Axe:
                Line(-1,0,13,0,Tone.LeatherM);
                Rect(8,0,12,2,Tone.MetalM);Rect(7,2,13,3,Tone.MetalM);Rect(8,3,12,4,Tone.MetalL);
                Line(9,4,11,4,Tone.White);Line(12,-2,12,0,Tone.MetalL);break;
            case Weapon.Hammer:
                Line(-1,0,12,0,Tone.LeatherM);Rect(10,-3,14,4,Tone.MetalM);Line(14,-3,14,3,Tone.MetalL);Line(11,-3,14,-3,Tone.MetalL);break;
            case Weapon.Staff:case Weapon.Crook:
                // Cajados ficam verticais na conjuração para preservar a forma e a margem.
                w.Line(24+x,27+by,24+x,8+y,Tone.LeatherM);w.Line(25+x,26+by,25+x,10+y,Tone.LeatherD);
                if(Spec.Weapon==Weapon.Staff){w.Poly(Tone.AccM,(24+x,5+y),(27+x,8+y),(24+x,11+y),(22+x,8+y));w.Line(24+x,6+y,23+x,8+y,Tone.White);}
                else {w.Line(22+x,7+y,26+x,7+y,Tone.AccM);w.Line(26+x,7+y,27+x,9+y,Tone.AccM);w.Line(27+x,9+y,25+x,11+y,Tone.AccM);w.Line(22+x,7+y,21+x,9+y,Tone.White);w.Line(21+x,9+y,23+x,11+y,Tone.White);}break;
            case Weapon.Bow:
                w.Line(24+x,15+y,27+x,18+y,Tone.LeatherM);w.Line(27+x,18+y,27+x,23+y,Tone.LeatherM);w.Line(27+x,23+y,24+x,26+y,Tone.LeatherM);
                int pull=attack==1?2:0;w.Line(24+x,15+y,24+x-pull,21+y,Tone.White);w.Line(24+x-pull,21+y,24+x,26+y,Tone.White);
                if(!striking){w.Line(21+x-pull,21+y,29+x-pull,21+y,Tone.MetalM);w.Set(29+x-pull,21+y,Tone.MetalL);}break;
            case Weapon.Spear:
                Line(-1,0,15,0,Tone.LeatherM);Line(15,0,19,0,Tone.MetalM);Line(17,-1,17,1,Tone.MetalL);break;
        }
        // A mão pode avançar, mas a arma completa é deslocada antes de tocar a margem; nunca corta a ponta.
        var filled=Enumerable.Range(0,1024).Where(i=>w.Px[i]!=Tone.None).ToArray();
        for(int i=0;i<w.Clipped;i++)Parts.Set(-1,-1,Tone.Ink);
        if(filled.Length>0){int dx=Math.Min(0,29-filled.Max(i=>i%32)),dy=Math.Max(0,2-filled.Min(i=>i/32));Parts.Composite(w,true,dx,dy);}
    }
    void Outline()
    {
        // Peças já separadas não recebem um segundo pixel de contorno no passe final.
        var filled=new List<(int,int)>();for(int y=0;y<32;y++)for(int x=0;x<32;x++)if(Parts.Get(x,y) is not (Tone.None or Tone.Ink))filled.Add((x,y));Parts.Halo(filled);
    }
    void Paint(bool flash)
    {
        var style=Style??Look(Spec);bool pale=Spec.Species is Species.Duck or Species.Rabbit or Species.Owl;
        for(int y=0;y<32;y++)for(int x=0;x<32;x++){
            var tone=Parts.Get(x,y);var material=MaterialLighting.Identify(tone);MaterialIds[y*32+x]=(byte)material;if(tone==Tone.None)continue;
            int level=tone switch{Tone.FurD or Tone.ClothD or Tone.AccD or Tone.LeatherD or Tone.MetalD=>1,Tone.FurL or Tone.ClothL or Tone.MetalL=>3,Tone.White=>4,_=>2};
            if(pale&&material==Material.Fur)level=tone==Tone.FurD?1:tone==Tone.FurL?4:3;
            if(material==Material.Metal)level=tone==Tone.MetalD?1:tone==Tone.MetalM?3:4;
            uint color=style.Pass==ProofPass.Silhouette?0x8CC6B6:material==Material.Ink?MaterialStyle.Ink:flash?MaterialRamps.Bone.Shine:style.Ramp(material)[style.Pass==ProofPass.Materials?2:level];
            int i=(y*32+x)*4;Buffer.Pixels[i]=(byte)(color>>16);Buffer.Pixels[i+1]=(byte)(color>>8);Buffer.Pixels[i+2]=(byte)color;Buffer.Pixels[i+3]=255;
        }
    }
}
