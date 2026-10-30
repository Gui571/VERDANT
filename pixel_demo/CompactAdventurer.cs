using Godot;
namespace Verdant;

public enum AdventurerKind { Duck, Rabbit, Knight, Mage, Guardian, Duelist, Healer }
public enum AdventurerMotion { Idle, Walk, Run, Attack, Hurt }

/// <summary>Shared 32px art grammar: 14px head, 5px torso, 2px boots,
/// one-pixel outer contour, upper-left highlights and a fixed (16,29) origin.</summary>
public sealed class CompactAdventurer
{
    public const int Size=32;
    public readonly Raster Buffer=new(Size,Size);
    public AdventurerKind Kind {get;}
    readonly int growth;
    readonly ExpandedAdventurer? expanded;
    readonly Color cloth,clothShadow;
    static readonly Color Ink=new("151829"), Blue=new("5098ef"), BlueShade=new("3455a4"),
        Ice=new("c1dceb"), White=new("f0f0e5"), Steel=new("829fb9"), Gold=new("dfce80"),
        GoldShade=new("b5a15d"), Orange=new("e5953d"), Rust=new("98572e"),
        Leather=new("947052"), Pink=new("e991aa"), Gray=new("b0b5ba"),
        Skin=new("edc297"), SkinShade=new("c99a76");
    int dx,dy,hand,phase;
    float weaponAngle;
    AdventurerMotion state;
    public string Name=>(int)Kind>=3?RosterCatalog.Get(Kind).Name+" · "+RosterCatalog.Get(Kind).Class:Kind switch {AdventurerKind.Duck=>"Bram · guerreiro azul",AdventurerKind.Rabbit=>"Nix · guerreira coelho",_=>"Rowan · aventureiro do bosque"};
    public string Description=>(int)Kind>=3?RosterCatalog.Get(Kind).Description:Kind switch {AdventurerKind.Duck=>"Capacete de gelo, machado e escudo. Guarda firme e silhueta compacta.",AdventurerKind.Rabbit=>"Orelhas longas, lenço cobre e espada curta. Leve e alerta.",_=>"Capuz verde, arco e aljava. Couro e tecido em blocos simples."};
    public CompactAdventurer(float scale=1,AdventurerKind kind=AdventurerKind.Duck)
    {
        if((int)kind>=3)expanded=new ExpandedAdventurer(Buffer,kind,scale);
        Kind=kind;growth=scale>1.08f?1:scale<.92f?-1:0;
        cloth=kind==AdventurerKind.Knight?new Color("849967"):Blue;
        clothShadow=kind==AdventurerKind.Knight?new Color("4e6754"):BlueShade;
    }
    void R(int x,int y,int w,int h,Color c)=>Buffer.Rect(x+dx,y+dy,w,h,c);
    void P(Color c,params Vector2[] p)=>Buffer.Polygon(p.Select(v=>v+new Vector2(dx,dy)).ToArray(),c);
    void W(Color c,params Vector2[] p)
    {
        var pivot=Kind==AdventurerKind.Duck?new Vector2(9,23):new Vector2(22,23);
        var transformed=p.Select(v=>{var d=v-pivot;float co=MathF.Cos(weaponAngle),si=MathF.Sin(weaponAngle);return new Vector2(MathF.Round(pivot.X+d.X*co-d.Y*si)+dx+(Kind==AdventurerKind.Rabbit&&weaponAngle>0?-3:0),MathF.Round(pivot.Y+d.X*si+d.Y*co)+dy+hand);}).ToArray();
        Buffer.Polygon(transformed,c);
    }
    public Raster Render(double seconds,bool walking=false,bool back=false)=>Render(seconds,walking?AdventurerMotion.Walk:AdventurerMotion.Idle,back);
    public Raster Render(double seconds,AdventurerMotion motion,bool back=false)
    {
        if(expanded!=null)return expanded.Render(seconds,motion,back);
        Buffer.Clear();state=motion;phase=(int)Math.Floor(Math.Max(0,seconds)*8)%32;
        int personal=(phase+(int)Kind*3)%32;
        bool moving=motion is AdventurerMotion.Walk or AdventurerMotion.Run;
        int gait=(int)(seconds*(motion==AdventurerMotion.Run?12:8))%4;
        int stride=moving?(gait==1?-1:gait==3?1:0):0;
        int breath=personal%16>=6&&personal%16<10?-1:0;
        int bodyBob=moving?(gait%2==1?-1:0):breath;
        dx=dy=0;weaponAngle=0;
        // Ground contacts stay at the same origin; only swing feet lift.
        R(11,27-Math.Max(0,stride),5,2,Ink);R(18,27-Math.Max(0,-stride),5,2,Ink);
        R(12,27-Math.Max(0,stride),3,1,Steel);R(19,27-Math.Max(0,-stride),3,1,clothShadow);
        dy=bodyBob;hand=moving?-stride:personal%16>=8&&personal%16<12?-1:0;
        if(motion==AdventurerMotion.Attack)
        {
            int attackFrame=(int)(Math.Clamp(seconds,0,.749)*8);
            weaponAngle=(Kind==AdventurerKind.Duck?new[]{0f,-.10f,.45f,1.12f,.65f,.1f}:new[]{0f,-.1f,.6f,1.25f,.55f,0f})[Math.Min(5,attackFrame)];
            dx=attackFrame is 2 or 3?1:0;
        }
        if(motion==AdventurerMotion.Hurt){dx=seconds<.15?-1:0;dy=seconds<.25?1:0;}
        Cape(personal);BackGear();
        if(Kind==AdventurerKind.Duck&&state!=AdventurerMotion.Attack)Axe();
        // A wide five-pixel tunic replaces a tall segmented torso.
        P(Ink,new(10,22),new(23,22),new(24,25),new(22,27),new(11,27),new(9,25));
        R(11,23,12,3,clothShadow);R(12,23,6,2,cloth);
        if(Kind==AdventurerKind.Knight)
        {R(12,23,2,1,Leather);R(14,24,2,1,Leather);R(16,25,2,1,Leather);}
        else
        {R(14,23,7,1,White);R(13,24,5,1,Ice);R(18,24,3,1,Steel);}
        R(12,26,10,1,Leather);R(17,26,2,1,Kind==AdventurerKind.Duck?Gold:Steel);
        if(Kind==AdventurerKind.Rabbit){R(11,22,11,1,Rust);R(12,22,7,1,Orange);}
        // Hands are two pixels, wrapped around the handles.
        R(8,23+hand,4,3,Ink);R(9,23+hand,2,2,Ice);
        if(Kind==AdventurerKind.Rabbit)Sword();
        if(Kind==AdventurerKind.Knight)Bow();
        Head(back,personal);
        if(Kind==AdventurerKind.Duck&&state==AdventurerMotion.Attack)Axe();
        if(Kind==AdventurerKind.Duck)Shield();
        else {R(22,24+hand,2,2,Ink);R(22,24+hand,2,1,Kind==AdventurerKind.Rabbit?White:Skin);}
        return Buffer;
    }
    void Cape(int personal)
    {
        int hem=personal%16>=10&&personal%16<14?1:0;
        P(Ink,new(11,21),new(14,24),new(12,27),new(5-hem,27),new(5-hem,25),new(9,24));
        R(6-hem,25,6,1,clothShadow);R(8-hem,25,4,1,cloth);
        if(Kind==AdventurerKind.Rabbit){R(7-hem,22,4,1,Rust);R(8-hem,21,3,1,Orange);}
    }
    void BackGear()
    {
        if(Kind==AdventurerKind.Knight)
        {R(7,19,4,7,Ink);R(8,20,2,5,Leather);R(7,16,1,5,Leather);R(9,15,1,6,Leather);R(6,15,2,2,Ice);R(8,14,2,2,White);}
        if(Kind==AdventurerKind.Rabbit){R(8,23,3,3,Ink);R(9,23,2,2,Leather);}
    }
    void Axe()
    {
        W(Ink,new(5,13),new(7,13),new(11,25),new(8,25));
        W(Leather,new(6,14),new(7,17),new(10,24),new(9,24));
        W(Ink,new(4,9),new(6,9),new(6,11),new(9,12),new(9,15),new(6,17),new(2,15),new(2,12));
        W(Ice,new(4,11),new(5,10),new(5,12),new(8,13),new(8,15),new(6,16),new(3,14),new(3,12));
        W(White,new(4,11),new(5,11),new(4,14),new(3,13));
    }
    void Sword()
    {
        W(Ink,new(27,16),new(29,15),new(29,18),new(24,25),new(22,23));
        W(Ice,new(28,17),new(29,16),new(29,18),new(24,23),new(24,21));
        W(White,new(28,17),new(29,16),new(29,17),new(25,21));
        W(Leather,new(22,23),new(23,22),new(25,24),new(24,25));
    }
    void Bow()
    {
        int draw=state==AdventurerMotion.Attack&&phase is 1 or 2?1:0;
        P(Ink,new(25,18),new(28,20),new(29,24),new(26,28),new(24,27),new(26,23),new(24,20));
        P(Leather,new(26,20),new(27,21),new(28,24),new(26,26),new(25,26),new(26,24));
        P(Ice,new(25,20),new(24-draw,23),new(25,26));
        if(state==AdventurerMotion.Attack&&phase==3){R(26,22,5,1,White);}
        else R(21,23,6,1,Leather);
    }
    void Shield()
    {
        P(Ink,new(24,21+hand),new(29,21+hand),new(30,23+hand),new(29,27+hand),new(26,28+hand),new(23,26+hand));
        P(Ice,new(25,22+hand),new(28,22+hand),new(29,23+hand),new(28,26+hand),new(26,27+hand),new(24,25+hand));
        R(25,22+hand,3,1,White);R(26,23+hand,2,2,Blue);R(26,23+hand,1,1,Ice);
    }
    void Head(bool back,int personal)
    {
        int l=9-growth,r=23+growth;
        // Shared rounded head rhythm: 3/2/1 steps, no circular raster outline.
        P(Ink,new(l+3,10),new(r-3,10),new(r,13),new(r,18),new(r-2,21),new(l+3,22),new(l,19),new(l-1,15),new(l+1,12));
        Color face=Kind==AdventurerKind.Duck?Gold:Kind==AdventurerKind.Rabbit?White:Skin;
        Color shade=Kind==AdventurerKind.Duck?GoldShade:Kind==AdventurerKind.Rabbit?Gray:SkinShade;
        P(shade,new(l+3,11),new(r-3,11),new(r-1,13),new(r-1,18),new(r-3,21),new(l+3,21),new(l+1,18),new(l,15),new(l+2,12));
        P(face,new(l+3,12),new(r-3,12),new(r-2,14),new(r-2,18),new(r-4,21),new(l+3,20),new(l+1,17),new(l+1,14));
        if(Kind==AdventurerKind.Duck)Helmet(l,r);
        else if(Kind==AdventurerKind.Rabbit)Ears(l,r,personal);
        else Hood(l,r);
        if(back){R(l+3,14,r-l-5,6,Kind==AdventurerKind.Knight?clothShadow:shade);return;}
        int glance=Kind==AdventurerKind.Knight&&personal>=16&&personal<21?-1:0;
        int eyeHeight=personal==29||state==AdventurerMotion.Hurt?1:2;
        R(16+glance,17,1,eyeHeight,Ink);R(20+glance,17,1,eyeHeight,Ink);
        if(Kind==AdventurerKind.Duck){R(18,19,8,1,Orange);R(19,20,7,1,Rust);}
        if(Kind==AdventurerKind.Rabbit){R(21,19,1,1,Pink);R(19,21,3,1,Ice);}
    }
    void Helmet(int l,int r)
    {
        P(Ink,new(l-1,10),new(l-1,7),new(l+2,9),new(l+4,9),new(l+6,6),new(l+9,6),new(l+11,4),new(l+12,4),new(l+11,7),new(r,10),new(r+2,7),new(r+3,7),new(r+2,12),new(r,14),new(l+1,14));
        P(Blue,new(l,9),new(l+2,11),new(l+4,10),new(l+7,7),new(l+9,7),new(l+11,5),new(l+11,6),new(l+9,9),new(r,11),new(r+2,8),new(r+1,12),new(r-1,13),new(l+1,13));
        R(l+4,9,1,3,Ice);R(l+2,12,2,1,BlueShade);
        P(Steel,new(l+7,11),new(r-1,11),new(r,13),new(l+6,13));
        R(l+8,11,4,1,Ice);R(l+7,12,5,1,Ice);R(l+10,12,1,1,Pink);
        R(l+2,14,r-l-4,1,GoldShade);
    }
    void Ears(int l,int r,int personal)
    {
        int twitch=personal>=18&&personal<22?1:0;
        P(Ink,new(l+2,11),new(l+1-twitch,3),new(l+3-twitch,3),new(l+5,10),new(r-4,10),new(r-2,2),new(r,2),new(r-1,12));
        P(White,new(l+2-twitch,4),new(l+3-twitch,5),new(l+4,10),new(l+3,10));
        R(l+3,7,1,3,Pink);P(White,new(r-1,3),new(r-1,9),new(r-3,10),new(r-2,6));R(r-2,7,1,3,Pink);
        R(l+1,11,r-l-1,1,Blue);R(l,12,3,1,BlueShade);
    }
    void Hood(int l,int r)
    {
        P(Ink,new(l-1,16),new(l-1,12),new(l+2,8),new(l+6,6),new(l+10,7),new(r+1,10),new(r+1,20),new(r-2,22),new(r-3,18),new(r-3,13),new(l+2,13),new(l,17));
        P(clothShadow,new(l,15),new(l,12),new(l+3,9),new(l+6,7),new(l+10,8),new(r,11),new(r,20),new(r-2,21),new(r-2,12),new(l+2,12));
        P(cloth,new(l+1,12),new(l+3,9),new(l+6,8),new(l+9,8),new(r-2,11),new(r-2,12));
        R(r-1,13,1,6,cloth);R(l+2,13,5,1,Leather);R(l+2,14,2,1,Leather);
        R(l-1,9,1,3,Ink);R(l-2,7,1,3,White);R(l-1,8,1,2,Ice);
    }
}
