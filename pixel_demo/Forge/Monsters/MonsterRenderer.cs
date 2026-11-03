namespace Verdant.Forge;
public sealed partial class MonsterRenderer
{
    public MonsterSpec Spec {get;}
    public Raster Buffer {get;}
    readonly Layer layer;
    public int VisualPhase {get;set;}
    public bool GroundShadow {get;set;}=true;
    public MonsterRenderer(MonsterSpec spec){spec.Validate();Spec=spec;layer=new(spec.Size);Buffer=new(spec.Size,spec.Size);}
    public static int Frames(MonsterMotion m)=>m switch{MonsterMotion.Idle=>16,MonsterMotion.Walk or MonsterMotion.Run=>8,MonsterMotion.Hurt=>4,MonsterMotion.Death=>8,MonsterMotion.Entry=>8,MonsterMotion.Special or MonsterMotion.Cast=>8,_=>6};
    public static int Fps(MonsterMotion m)=>m is MonsterMotion.Run or MonsterMotion.Death?12:8;
    int ox,oy,entryLift;
    void Rect(int x,int y,int w,int h,Tone t)=>layer.Rect(x+ox,y+oy,w,h,t);
    void Line(int x,int y,int xx,int yy,Tone t)=>layer.Line(x+ox,y+oy,xx+ox,yy+oy,t);
    void Stamp(string[] rows,int x,int y)=>layer.Stamp(rows,x+ox,y+oy);
    void Oval(int cx,int cy,int rx,int ry)
    {
        for(int y=-ry;y<=ry;y++)for(int x=-rx;x<=rx;x++)if(x*x/(double)(rx*rx)+y*y/(double)(ry*ry)<=1)
            layer.Set(cx+x+ox,cy+y+oy,y>ry/2||x>rx-2?Tone.FurD:y< -ry/3&&x<rx/2?Tone.FurL:Tone.FurM);
    }
    public Raster Render(double time,MonsterMotion motion=MonsterMotion.Idle,bool back=false)
    {
        if(motion==MonsterMotion.Entry){
            entryLift=new[]{3,3,2,1,0,0,0,0}[Math.Clamp((int)(time*8),0,7)];
            Render(0,MonsterMotion.Idle,back);entryLift=0;return Buffer;
        }
        if(motion is MonsterMotion.Special or MonsterMotion.Cast){int frame=Math.Clamp((int)(time*8),0,7);Render(Math.Min(5,Math.Max(0,frame-1))/8.0,MonsterMotion.Attack,back);ForgeEffects.Special(Buffer,MonsterGenerator.Palette(Spec),frame,motion==MonsterMotion.Cast);return Buffer;}
        layer.Clear();Buffer.Clear();int size=Spec.Size,c=size/2,g=size-3;
        int f=(int)(Math.Max(0,time)*Fps(motion));bool loop=motion<=MonsterMotion.Run;f=loop?f%Frames(motion):Math.Min(f,Frames(motion)-1);
        if(motion==MonsterMotion.Death&&f==7)return Buffer;
        int bob=motion==MonsterMotion.Idle?(f is >=5 and <=8?-1:0):motion is MonsterMotion.Walk or MonsterMotion.Run?(f%4==2?-1:0):0;
        bob-=entryLift;
        int dx=motion==MonsterMotion.Attack?new[]{0,-1,1,2,1,0}[f]:motion==MonsterMotion.Hurt?(f<2?-1:0):0;
        if(Spec.Family is MonsterFamily.Arthropod or MonsterFamily.Winged)dx=Math.Min(dx,0);
        ox=c-16+dx;oy=g-29+bob;int bulk=Spec.Mass/2,head=Spec.Head/2;
        if(Spec.Rank==Rank.Boss)BossSilhouette();
        int step=motion is MonsterMotion.Walk or MonsterMotion.Run?(f<4?1:-1):0;
        switch(Spec.Family)
        {
            case MonsterFamily.Slime:
                Oval(16,21,10+bulk,7);Oval(14,16,6+head,7);Rect(8,27,17,2,Tone.FurD);Rect(9,14,2,4,Tone.FurL);Rect(11,12,3,2,Tone.White);Line(9,23,15,25,Tone.FurL);break;
            case MonsterFamily.Beast:
                Stamp(new[]{"ff...",".ff..","..ff.","...ff"},3,18);Oval(14,22,9+bulk,5);Oval(16,18,6,7);Line(8,19,12,16,Tone.FurL);
                Rect(7+step,25,4,3,Tone.FurD);Rect(19-step,25,4,3,Tone.FurD);Oval(19,16,7+head,7);
                Stamp(new[]{"f....f","ff..ff","ffffff"},16,7);Rect(21,20,5,3,Tone.AccM);break;
            case MonsterFamily.Golem:
                Rect(9-bulk,18,14+bulk*2,8,Tone.FurM);Rect(4-bulk,15,7+bulk,7,Tone.FurM);Rect(22,15,6+bulk,7,Tone.FurM);Rect(3,21,7,6,Tone.FurD);Rect(22,21,6,6,Tone.FurD);Line(4,15,10,15,Tone.FurL);Line(22,15,27,15,Tone.FurL);Rect(4,22,4,2,Tone.MetalM);Rect(23,22,4,2,Tone.MetalM);
                Rect(10+step,26,4,2,Tone.MetalM);Rect(18-step,26,4,2,Tone.MetalM);Rect(10,10-head,12,9+head,Tone.FurM);Line(10,10-head,20,10-head,Tone.FurL);Line(17,12,15,15,Tone.FurD);
                Line(9,12,19,12,Tone.FurL);Line(19,18,17,22,Tone.FurD);Rect(14,22,4,2,Tone.ClothM);break;
            case MonsterFamily.Dragon:
                Stamp(new[]{".....f","....ff","...fff","..ffFf","fffFFf"},3,18);Oval(15,22,8+bulk,5);
                Rect(9+step,26,4,2,Tone.AccM);Rect(20-step,26,4,2,Tone.AccM);Oval(19,19,4,6);Oval(21,13,5+head,5);Line(16,21,20,17,Tone.AccM);
                Stamp(new[]{"i.....i","ii...ii","aafffaa"},15,7);Rect(22,20,5,3,Tone.FurL);Stamp(new[]{"w.w","fff"},23,22);break;
            case MonsterFamily.Arthropod:
                for(int k=0;k<3;k++){int knee=4+k%2*2;Line(11,20+k*2,knee,18+k*3,Tone.FurD);Line(knee,18+k*3,3+k*2+(k==1?step:0),22+k*3,Tone.AccM);Line(21,20+k*2,32-knee,18+k*3,Tone.FurD);Line(32-knee,18+k*3,29-k*2-(k==1?step:0),22+k*3,Tone.AccM);}
                Oval(16,23,7+bulk,5);Line(11,22,21,22,Tone.FurD);Oval(16,15,6+head,5);Stamp(new[]{"ff.ff","fffff",".fff."},3,13);Stamp(new[]{"ff.ff","fffff",".fff."},24,13);Line(6,16,9,20,Tone.FurM);Line(26,16,22,20,Tone.FurM);break;
            case MonsterFamily.Winged:
                int flap=f%4<2?0:2;
                Stamp(new[]{"f.......","ff......","fFfff...","fFFffff.","fffFFfff","..ffffff","...fffff"},3,10+flap);
                Stamp(new[]{".......f","......ff","...fffFf",".ffffFFf","fffFFfff","ffffff..","fffff..."},21,10+flap);
                Line(7,17+flap,14,22,Tone.FurD);Line(25,17+flap,19,22,Tone.FurD);Oval(16,22,5,5);Oval(16,15,6+head,6);
                Stamp(new[]{"f.....f","ff...ff","fffffff"},13,7);Rect(12,27,3,1,Tone.AccM);Rect(18,27,3,1,Tone.AccM);break;
            case MonsterFamily.Serpent:
                Oval(13,25,9+bulk,3);Oval(13,23,5,2);Rect(9,23,8,2,Tone.Ink);Line(10,22,16,22,Tone.FurL);Oval(20,20,4,6);Oval(19,14,7+head,6);
                Rect(12,25,8,1,Tone.AccM);Rect(21,18,4,2,Tone.AccM);break;
            case MonsterFamily.Aquatic:
                Stamp(new[]{"f.....","ff....","fff...","ffff..","fffff.","ffff..","fff...","ff....","f....."},3,17-Spec.Appendages/2);Oval(17,21,9+bulk,7);
                Stamp(new[]{"...i...","..ifi..",".ifffi.","fffffff"},13,11);Oval(21,19,6,6);Rect(17+step,27,5,1,Tone.AccM);Line(12,19,14,22,Tone.FurL);Line(14,20,16,23,Tone.FurD);Line(12,23,17,25,Tone.FurD);break;
            case MonsterFamily.Plant:
                Line(16,23,10+step,28,Tone.LeatherM);Line(16,23,22-step,28,Tone.LeatherM);Oval(16,23,5+bulk,4);
                Stamp(new[]{"g......g","fg....gf","ffg..gff",".ffffff.","..ffff.."},12,5);Oval(16,16,7+head,6);Stamp(new[]{"..g..",".ggf.","ggfff",".fff.","..f.."},5,10);Stamp(new[]{"..g..",".ggf.","ggfff",".fff.","..f.."},23,10);Line(8,14,12,16,Tone.FurD);Line(25,14,21,16,Tone.FurD);
                Stamp(new[]{"gg...","ffg..",".ffg.","..fff"},4,20);Stamp(new[]{"...gg","..gff",".gff.","fff.."},23,20);Line(7,23,13,24,Tone.FurM);Line(25,23,19,24,Tone.FurM);break;
        }
        // Detalhes anexados ao corpo: nenhum ponto solto no repouso.
        for(int k=0;k<=Spec.Pattern;k++)Rect(12+k*2,23+(k%2),1,1,Tone.FurL);
        if(Spec.Appendages>0&&Spec.Family is not (MonsterFamily.Slime or MonsterFamily.Aquatic)){Rect(12,11,2,4,Tone.AccM);if(Spec.Appendages>1)Rect(20,10,2,4,Tone.AccM);}
        if(Spec.Rank==Rank.Elite)Stamp(new[]{"a.a.a","aaaaa","ccccc"},14,9);
        ElementCrest(f);
        int ex=Spec.Family is MonsterFamily.Beast or MonsterFamily.Dragon or MonsterFamily.Aquatic?18:12;
        int faceY=Spec.Family==MonsterFamily.Dragon?12:16;
        if(!back){bool blink=(motion==MonsterMotion.Idle&&f==14)||motion==MonsterMotion.Death;Rect(ex,faceY,2,blink?1:3,Tone.Ink);Rect(ex+6,faceY,2,blink?1:3,Tone.Ink);if(!blink){Rect(ex,faceY,1,1,Tone.White);Rect(ex+6,faceY,1,1,Tone.White);}if(motion==MonsterMotion.Attack&&f is 2 or 3){Rect(ex+2,faceY+5,4,3,Tone.Ink);Rect(ex+2,faceY+5,1,2,Tone.White);Rect(ex+5,faceY+5,1,2,Tone.White);}}
        else {Line(14,15,16,20,Tone.FurD);Rect(11,16,2,2,Tone.FurL);}
        if(motion==MonsterMotion.Attack&&f is 2 or 3){Line(25,18,27,20,Tone.White);Line(27,20,25,22,Tone.MetalL);}
        if(!back&&Spec.Rank==Rank.Boss&&VisualPhase>0){Line(ex-1,faceY-2,ex+2,faceY-1,Tone.Ink);Line(ex+5,faceY-1,ex+8,faceY-2,Tone.Ink);}
        ElementDetails(f);
        // Base fixa no idle: o corpo respira sobre apoio estável.
        ox=c-16;oy=g-29;if(entryLift==0)Rect(12,28,8,1,Tone.FurD);
        AnimateAnatomy(motion,f,c,g);
        ForgeFinish.Outline(layer);for(int x=c-7;x<=c+7;x++)layer.Set(x,g,Tone.Ink);if(GroundShadow)ForgeFinish.Shadow(layer,c,g,7);
        var pal=MonsterGenerator.Palette(Spec);for(int y=0;y<size;y++)for(int x=0;x<size;x++){var t=layer.Get(x,y);if(t!=Tone.None)Buffer.Pixel(x,y,pal.Resolve(motion==MonsterMotion.Hurt&&f==0&&t!=Tone.Ink?Tone.White:t));}
        for(int i=0;i<layer.Clipped;i++)Buffer.Pixel(-1,-1,default);return Buffer;
    }
}
