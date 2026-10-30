using Godot;
namespace Verdant;

/// <summary>Additional class silhouettes using the approved grid, contour,
/// material roles, head/body dimensions and shared animation contract.</summary>
public sealed class ExpandedAdventurer
{
    readonly Raster canvas;
    readonly AdventurerKind kind;
    readonly int growth;
    readonly Color cloth,shade,accent;
    static readonly Color Ink=RosterVisualRules.Outline,White=RosterVisualRules.Highlight,
        Steel=RosterVisualRules.Metal,Ice=new("c1dceb"),Skin=new("edc297"),SkinShade=new("c99a76"),
        Leather=new("947052"),Gold=new("dfce80");
    int dx,dy,hand,frame; bool rear; AdventurerMotion state;
    public ExpandedAdventurer(Raster target,AdventurerKind kind,float scale)
    {
        canvas=target;this.kind=kind;growth=scale>1.08f?1:scale<.92f?-1:0;
        var colors=RosterCatalog.Get(kind).Colors;cloth=new(colors[0]);shade=new(colors[1]);accent=new(colors[2]);
    }
    void R(int x,int y,int w,int h,Color c)=>canvas.Rect(x+dx,y+dy,w,h,c);
    void P(Color c,params Vector2[] points)=>canvas.Polygon(points.Select(p=>p+new Vector2(dx,dy)).ToArray(),c);
    public Raster Render(double seconds,AdventurerMotion motion,bool back)
    {
        canvas.Clear();state=motion;rear=back;frame=(int)(Math.Max(0,seconds)*8)%32;
        int phase=(frame+(int)kind*3)%16;
        int gait=(int)(seconds*(motion==AdventurerMotion.Run?12:8))%4;
        bool moving=motion is AdventurerMotion.Walk or AdventurerMotion.Run;
        int stride=moving?(gait==1?-1:gait==3?1:0):0;
        dx=dy=0;
        R(11,27-Math.Max(0,stride),5,2,Ink);R(18,27-Math.Max(0,-stride),5,2,Ink);
        R(12,27-Math.Max(0,stride),3,1,shade);R(19,27-Math.Max(0,-stride),3,1,shade);
        dy=moving?(gait%2==1?-1:0):phase>=6&&phase<10?-1:0;
        hand=moving?-stride:phase>=8&&phase<12?-1:0;
        if(motion==AdventurerMotion.Hurt){dx=seconds<.15?-1:0;dy=seconds<.25?1:0;}
        if(motion==AdventurerMotion.Attack&&frame is 2 or 3)dx=kind==AdventurerKind.Duelist?1:0;
        switch(kind)
        {
            case AdventurerKind.Mage:Mage(phase);break;
            case AdventurerKind.Guardian:Guardian();break;
            case AdventurerKind.Duelist:Duelist(phase);break;
            case AdventurerKind.Healer:Healer();break;
            default:throw new ArgumentOutOfRangeException(nameof(kind));
        }
        return canvas;
    }
    void Tunic(bool bell=false)
    {
        int spread=bell?2:0;
        P(Ink,new(11,21),new(22,21),new(24+spread,26),new(22,27),new(10,27),new(8-spread,26));
        P(shade,new(12,22),new(21,22),new(23+spread,26),new(10-spread,26));
        R(12,23,7,2,cloth);R(12,26,9,1,shade);
    }
    void Head(Color face,Color shadow)
    {
        int l=9-growth,r=23+growth;
        P(Ink,new(l+3,10),new(r-3,10),new(r,13),new(r,18),new(r-2,21),new(l+3,22),new(l,19),new(l-1,15),new(l+1,12));
        P(shadow,new(l+3,11),new(r-3,11),new(r-1,14),new(r-1,19),new(r-3,21),new(l+3,21),new(l,16),new(l+1,13));
        P(face,new(l+3,12),new(r-3,12),new(r-2,14),new(r-2,18),new(r-4,21),new(l+3,20),new(l+1,17),new(l+1,14));
    }
    void Eyes()
    {
        if(rear)return;int h=frame==29||state==AdventurerMotion.Hurt?1:2;
        R(16,17,1,h,Ink);R(20,17,1,h,Ink);
    }
    void Mage(int phase)
    {
        int flutter=phase>=11&&phase<15?1:0;
        P(Ink,new(11,20),new(23,21),new(25,27),new(7-flutter,27));
        P(shade,new(12,21),new(22,22),new(23,26),new(9-flutter,26));Tunic(true);
        R(16,23,2,3,accent);R(16,23,1,1,White);
        // Staff's diamond remains separated from the pointed hat.
        R(26,14+hand,3,13,Ink);R(27,15+hand,1,11,Leather);
        int crystal=state==AdventurerMotion.Attack&&frame is 2 or 3?-1:hand;
        P(Ink,new(27,7+crystal),new(30,11+crystal),new(27,15+crystal),new(24,11+crystal));
        P(accent,new(27,9+crystal),new(29,11+crystal),new(27,13+crystal),new(25,11+crystal));R(27,9+crystal,1,2,White);
        if(state==AdventurerMotion.Attack&&frame is 2 or 3){R(23,8,1,1,White);R(30,8,1,1,accent);R(27,5,1,2,White);}
        Head(Skin,SkinShade);
        P(Ink,new(5,13),new(9,11),new(13,3),new(17,3),new(19,6),new(18,8),new(22,11),new(24,13),new(21,15),new(8,15));
        P(shade,new(7,13),new(11,11),new(14,4),new(16,4),new(17,6),new(16,8),new(21,12),new(22,13),new(20,14),new(9,14));
        P(cloth,new(12,11),new(14,5),new(16,5),new(15,8),new(18,11));
        R(11,12,10,1,accent);R(12,12,4,1,White);Eyes();
        // Open tome, a second class cue rather than another weapon silhouette.
        R(5,22+hand,6,5,Ink);R(6,23+hand,4,3,accent);R(6,23+hand,2,2,White);R(8,23+hand,1,3,shade);
        R(24,24+hand,2,2,Skin);
        if(rear)R(11,15,11,6,shade);
    }
    void Guardian()
    {
        // Shell creates a broad rear contour; tower shield creates a flat front.
        P(Ink,new(7,16),new(11,16),new(14,20),new(13,26),new(7,28),new(4,25),new(4,20));
        P(shade,new(7,18),new(10,18),new(12,21),new(11,25),new(7,26),new(5,24),new(5,21));
        R(6,20,3,4,cloth);Tunic();R(11,23,11,2,cloth);R(13,24,7,1,Gold);R(16,26,2,1,Gold);
        Head(accent,new Color("718453"));
        P(Ink,new(9,13),new(9,9),new(12,7),new(20,7),new(24,10),new(24,15),new(21,15),new(20,13),new(12,13),new(11,15));
        P(shade,new(10,12),new(10,10),new(13,8),new(20,8),new(23,11),new(23,14),new(22,14),new(21,12));
        R(12,9,8,2,cloth);R(12,11,10,1,Gold);Eyes();
        int slam=state==AdventurerMotion.Attack&&frame is 2 or 3?2:hand;
        R(26,21,2,6,Ink);R(26,22,1,4,Leather);
        R(23,16+slam,7,6,Ink);R(24,17+slam,5,4,shade);R(24,17+slam,5,1,Gold);R(24,18+slam,2,2,cloth);
        P(Ink,new(4,20+hand),new(11,20+hand),new(12,23+hand),new(11,28+hand),new(4,28+hand),new(3,25+hand));
        P(cloth,new(5,21+hand),new(10,21+hand),new(11,23+hand),new(10,27+hand),new(5,27+hand),new(4,24+hand));
        R(5,22+hand,5,1,Gold);R(7,23+hand,2,3,shade);R(7,24+hand,1,1,Gold);
        if(rear)R(11,14,11,6,shade);
    }
    void Duelist(int phase)
    {
        int tail=phase>=11&&phase<15?1:0;
        P(Ink,new(11,22),new(8,20-tail),new(4,20-tail),new(2,23),new(4,26),new(10,27));
        P(cloth,new(10,23),new(7,21-tail),new(4,21-tail),new(3,23),new(5,25),new(9,26));
        P(accent,new(4,21-tail),new(3,23),new(5,25),new(6,25),new(5,23));
        Tunic();R(12,23,9,2,shade);R(12,26,9,1,Leather);R(17,26,2,1,Steel);
        Head(cloth,new Color("aa633e"));
        // A low mask between two wide triangular ears, unlike Nix's long ears.
        P(Ink,new(9,13),new(8,6),new(12,7),new(15,11),new(19,11),new(23,6),new(25,7),new(24,14));
        P(cloth,new(10,11),new(9,7),new(11,8),new(14,12),new(19,12),new(23,8),new(24,8),new(23,12));
        R(10,9,1,2,accent);R(23,9,1,2,accent);
        P(accent,new(10,17),new(13,19),new(21,18),new(23,17),new(22,20),new(19,22),new(13,21));Eyes();
        R(12,22,11,1,shade);R(21,23,3,1,shade);R(23,24+tail,2,1,shade);
        int reach=state==AdventurerMotion.Attack&&frame is 2 or 3?2:0;
        P(Ink,new(5,18+hand),new(7,19+hand),new(11,25),new(9,26),new(5,21+hand));
        P(Ice,new(6,20+hand),new(7,21+hand),new(10,24),new(9,24));
        P(Ink,new(25,19+hand-reach),new(28,17+hand-reach),new(28,21+hand-reach),new(24,26),new(22,25));
        P(Ice,new(26,20+hand-reach),new(27,19+hand-reach),new(27,21+hand-reach),new(24,24));
        R(9,24,2,1,Leather);R(23,25,2,1,Leather);
        if(rear)R(11,14,11,6,cloth);
    }
    void Healer()
    {
        Tunic(true);R(11,23,11,1,accent);R(15,24,3,2,accent);R(16,24,1,1,White);
        int lift=state==AdventurerMotion.Attack&&frame is 2 or 3?-1:hand;
        // Circular crook with a real transparent opening, away from the head.
        P(Ink,new(26,8+lift),new(29,9+lift),new(31,12+lift),new(29,16+lift),new(25,16+lift),new(23,13+lift),new(24,10+lift));
        P(accent,new(26,9+lift),new(28,10+lift),new(30,12+lift),new(28,15+lift),new(25,14+lift),new(24,12+lift));
        R(26,11+lift,2,3,new Color(0,0,0,0));R(25,11+lift,1,2,new Color(0,0,0,0));
        R(26,16+lift,3,12-lift,Ink);R(27,16+lift,1,11-lift,accent);R(26,19,3,1,cloth);
        Head(accent,new Color("b5ac96"));
        P(Ink,new(8,15),new(8,12),new(11,8),new(16,7),new(21,8),new(24,12),new(24,19),new(22,21),new(21,14),new(12,13),new(10,20));
        P(accent,new(9,15),new(9,12),new(12,9),new(16,8),new(20,9),new(23,12),new(23,18),new(22,18),new(21,13),new(12,12));
        R(12,10,8,1,White);R(12,12,9,1,cloth);R(16,11,2,2,shade);
        // Two pale facial planes identify an owl without larger anime eyes.
        R(12,15,5,5,White);R(18,15,4,5,White);Eyes();R(18,19,1,1,Leather);
        if(state==AdventurerMotion.Attack&&frame is 2 or 3){R(6,21,3,1,White);R(7,20,1,3,White);}
        R(8,23+lift,3,3,Ink);R(9,23+lift,2,2,accent);
        R(23,24+hand,3,2,Ink);R(24,24+hand,2,1,accent);
        if(rear)R(11,14,11,6,accent);
    }
}
