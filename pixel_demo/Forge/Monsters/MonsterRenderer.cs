namespace Verdant.Forge;
public sealed class MonsterRenderer
{
    public MonsterSpec Spec {get;}
    public Raster Buffer {get;}
    readonly Layer layer;
    public bool GroundShadow {get;set;}=true;
    public MonsterRenderer(MonsterSpec spec){spec.Validate();Spec=spec;layer=new(spec.Size);Buffer=new(spec.Size,spec.Size);}
    public static int Frames(MonsterMotion m)=>m switch{MonsterMotion.Idle=>16,MonsterMotion.Walk or MonsterMotion.Run=>8,MonsterMotion.Hurt=>4,MonsterMotion.Special or MonsterMotion.Cast=>8,_=>6};
    public static int Fps(MonsterMotion m)=>m==MonsterMotion.Run?12:8;
    int ox,oy;
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
        if(motion is MonsterMotion.Special or MonsterMotion.Cast){int frame=Math.Clamp((int)(time*8),0,7);Render(Math.Min(5,Math.Max(0,frame-1))/8.0,MonsterMotion.Attack,back);ForgeEffects.Special(Buffer,MonsterGenerator.Palette(Spec),frame,motion==MonsterMotion.Cast);return Buffer;}
        layer.Clear();Buffer.Clear();int size=Spec.Size,c=size/2,g=size-3;
        int f=(int)(Math.Max(0,time)*Fps(motion));bool loop=motion<=MonsterMotion.Run;f=loop?f%Frames(motion):Math.Min(f,Frames(motion)-1);
        if(motion==MonsterMotion.Death&&f==5)return Buffer;
        int bob=motion==MonsterMotion.Idle?(f is >=5 and <=8?-1:0):motion is MonsterMotion.Walk or MonsterMotion.Run?(f%4==2?-1:0):0;
        int dx=motion==MonsterMotion.Attack?new[]{0,-1,1,2,1,0}[f]:motion==MonsterMotion.Hurt?(f<2?-1:0):0;
        if(Spec.Family is MonsterFamily.Arthropod or MonsterFamily.Winged)dx=Math.Min(dx,0);
        ox=c-16+dx;oy=g-29+bob;int bulk=Spec.Mass/2,head=Spec.Head/2;
        if(Spec.Rank==Rank.Boss){ // Anatomia de chefe: massa adicional real, sem ampliar pixels.
            Oval(16,21,14,7);Rect(3,26,6,3,Tone.FurD);Rect(23,26,6,3,Tone.FurD);
            Stamp(new[]{"..i..",".imi.","mmmmm",".mmm."},3,9);Stamp(new[]{"..i..",".imi.","mmmmm",".mmm."},24,9);
        }
        int step=motion is MonsterMotion.Walk or MonsterMotion.Run?(f<4?1:-1):0;
        switch(Spec.Family)
        {
            case MonsterFamily.Slime:
                Oval(16,22,10+bulk,6);Oval(15,18,7,7);Rect(8,27,17,2,Tone.FurD);Rect(11,14,3,2,Tone.White);break;
            case MonsterFamily.Beast:
                Stamp(new[]{"ff...",".ff..","..ff.","...ff"},3,18);Oval(14,22,9+bulk,5);
                Rect(7+step,25,4,3,Tone.FurD);Rect(19-step,25,4,3,Tone.FurD);Oval(19,16,7+head,7);
                Stamp(new[]{"f....f","ff..ff","ffffff"},16,7);Rect(21,20,5,3,Tone.AccM);break;
            case MonsterFamily.Golem:
                Rect(9-bulk,18,14+bulk*2,8,Tone.FurM);Rect(5,19,4,8,Tone.FurD);Rect(23,19,4,8,Tone.FurD);
                Rect(10+step,26,4,2,Tone.MetalM);Rect(18-step,26,4,2,Tone.MetalM);Oval(16,14,8+head,6);
                Line(9,12,19,12,Tone.FurL);Line(19,18,17,22,Tone.FurD);Rect(14,22,4,2,Tone.ClothM);break;
            case MonsterFamily.Dragon:
                Stamp(new[]{".....f","....ff","...fff","..ffFf","fffFFf"},3,18);Oval(15,22,8+bulk,5);
                Rect(9+step,26,4,2,Tone.AccM);Rect(20-step,26,4,2,Tone.AccM);Oval(19,16,7+head,6);
                Stamp(new[]{"i.....i","ii...ii","aafffaa"},15,7);Rect(22,20,5,3,Tone.FurL);Stamp(new[]{"w.w","fff"},23,22);break;
            case MonsterFamily.Arthropod:
                for(int k=0;k<3;k++){Line(10,20+k*2,4,19+k*3,Tone.FurD);Line(4,19+k*3,4+(k==1?step:0),22+k*3,Tone.AccM);Line(22,20+k*2,28,19+k*3,Tone.FurD);Line(28,19+k*3,28-(k==1?step:0),22+k*3,Tone.AccM);}
                Oval(16,21,8+bulk,6);Oval(16,15,6+head,5);Stamp(new[]{"ff.ff","fffff",".fff."},3,13);Stamp(new[]{"ff.ff","fffff",".fff."},24,13);Line(6,16,9,20,Tone.FurM);Line(26,16,22,20,Tone.FurM);break;
            case MonsterFamily.Winged:
                int flap=f%4<2?0:2;
                Stamp(new[]{"f.......","ff......","fFfff...","fFFffff.","fffFFfff","..ffffff","...fffff"},3,10+flap);
                Stamp(new[]{".......f","......ff","...fffFf",".ffffFFf","fffFFfff","ffffff..","fffff..."},21,10+flap);
                Line(7,17+flap,14,22,Tone.FurD);Line(25,17+flap,19,22,Tone.FurD);Oval(16,22,5,5);Oval(16,15,6+head,6);
                Stamp(new[]{"f.....f","ff...ff","fffffff"},13,7);Rect(12,27,3,1,Tone.AccM);Rect(18,27,3,1,Tone.AccM);break;
            case MonsterFamily.Serpent:
                Oval(13,25,9+bulk,3);Oval(13,23,5,2);Rect(10,22,6,2,Tone.FurD);Oval(20,20,4,6);Oval(19,14,7+head,6);
                Rect(12,25,8,1,Tone.AccM);Rect(21,18,4,2,Tone.AccM);break;
            case MonsterFamily.Aquatic:
                Stamp(new[]{"f.....","ff....","fff...","ffff..","fffff.","ffff..","fff...","ff....","f....."},3,17);Oval(17,21,9+bulk,7);
                Stamp(new[]{"...i...","..ifi..",".ifffi.","fffffff"},13,11);Oval(21,19,6,6);Rect(17+step,27,5,1,Tone.AccM);Line(12,23,17,25,Tone.FurD);break;
            case MonsterFamily.Plant:
                Line(16,23,10+step,28,Tone.LeatherM);Line(16,23,22-step,28,Tone.LeatherM);Oval(16,23,5+bulk,4);
                Stamp(new[]{"g......g","fg....gf","ffg..gff",".ffffff.","..ffff.."},12,5);Oval(16,16,8+head,7);
                Stamp(new[]{"gg...","ffg..",".ffg.","..fff"},4,20);Stamp(new[]{"...gg","..gff",".gff.","fff.."},23,20);Line(7,23,13,24,Tone.FurM);Line(25,23,19,24,Tone.FurM);break;
        }
        // Detalhes anexados ao corpo: nenhum ponto solto no repouso.
        for(int k=0;k<=Spec.Pattern;k++)Rect(12+k*2,23+(k%2),1,1,Tone.FurL);
        if(Spec.Appendages>0){Rect(12,11,2,4,Tone.AccM);if(Spec.Appendages>1)Rect(20,10,2,4,Tone.AccM);}
        if(Spec.Rank==Rank.Elite)Stamp(new[]{"a.a.a","aaaaa","ccccc"},14,9);
        switch(Spec.Element){
            case Element.Fire:Stamp(new[]{"..a.",".aa.","aaaf","ffff"},14,8);break;
            case Element.Ice:Stamp(new[]{".i.","imi","mmm"},15,8);break;
            case Element.Plant:Stamp(new[]{"ggff",".fff","..ff"},14,8);break;
            case Element.Shadow:Stamp(new[]{"C..C","cCCc","cccc"},14,8);break;
            case Element.Poison:Stamp(new[]{".pp.","pFFp","ffff"},14,8);break;
        }
        int ex=Spec.Family is MonsterFamily.Beast or MonsterFamily.Dragon or MonsterFamily.Aquatic?18:12;
        if(!back){bool blink=motion==MonsterMotion.Idle&&f==14;Rect(ex,16,2,blink?1:3,Tone.Ink);Rect(ex+6,16,2,blink?1:3,Tone.Ink);if(!blink){Rect(ex,16,1,1,Tone.White);Rect(ex+6,16,1,1,Tone.White);}if(motion==MonsterMotion.Attack&&f is 2 or 3){Rect(ex+2,21,4,3,Tone.Ink);Rect(ex+2,21,1,2,Tone.White);Rect(ex+5,21,1,2,Tone.White);}}
        else {Line(14,15,16,20,Tone.FurD);Rect(11,16,2,2,Tone.FurL);}
        if(motion==MonsterMotion.Attack&&f is 2 or 3){Line(25,18,27,20,Tone.White);Line(27,20,25,22,Tone.MetalL);}
        // Base fixa no idle: o corpo respira sobre apoio estável.
        ox=c-16;oy=g-29;Rect(12,28,8,1,Tone.FurD);
        if(motion==MonsterMotion.Death){var old=(Tone[])layer.Px.Clone();layer.Clear();int h=size-4;for(int y=1;y<size-2;y++)for(int x=1;x<size-1;x++){var t=old[y*size+x];if(t!=Tone.None)layer.Set(x,g-(g-y)*(5-f)/5,t);}}
        ForgeFinish.Outline(layer);for(int x=c-7;x<=c+7;x++)layer.Set(x,g,Tone.Ink);if(GroundShadow)ForgeFinish.Shadow(layer,c,g,7);
        var pal=MonsterGenerator.Palette(Spec);for(int y=0;y<size;y++)for(int x=0;x<size;x++){var t=layer.Get(x,y);if(t!=Tone.None)Buffer.Pixel(x,y,pal.Resolve(motion==MonsterMotion.Hurt&&f==0&&t!=Tone.Ink?Tone.White:t));}
        for(int i=0;i<layer.Clipped;i++)Buffer.Pixel(-1,-1,default);return Buffer;
    }
}
