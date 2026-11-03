namespace Verdant.Forge;
public sealed partial class MonsterRenderer
{
    void ElementCrest(int frame)
    {
        int beat=(frame/2)%4;
        switch(Spec.Element){
            case Element.Fire:
                Stamp(beat is 1 or 2?new[]{"...a.","..aa.",".aaaf","aaaff","fffff"}:new[]{".a...",".aa..",".aaaf","aaaff","fffff"},13,7);
                Rect(15,10,1,2,beat==1?Tone.White:Tone.AccM);break;
            case Element.Ice:
                Stamp(new[]{"..i..",".imi.","imimi","mmmmm"},14,7);Rect(16,8,1,2,beat==1?Tone.White:Tone.MetalL);break;
            case Element.Plant:
                Stamp(beat<2?new[]{"gg...gg","fgg.ggf",".fffff.","..fff.."}:new[]{".g...g.","ggg.ggg","fffffff","..fff.."},12,7);break;
            case Element.Poison:
                Stamp(beat<2?new[]{".pp..","pFFp.","pffpp",".ffff"}:new[]{"...pp","..pFp",".ppff","fffff"},13,7);break;
            case Element.Shadow:
                Stamp(beat<2?new[]{"C....","cC.C.","ccCCc","ccccc"}:new[]{"....C",".C.Cc","cCCcc","ccccc"},14,7);break;
        }
    }

    void BossSilhouette()
    {
        // A célula maior oferece espaço para anatomia nativa, sem ampliar o pixel.
        switch(Spec.Family){
            case MonsterFamily.Golem:
                Rect(-1,16,11,10,Tone.FurD);Rect(23,16,10,10,Tone.FurD);Rect(-1,15,10,4,Tone.MetalM);Rect(23,15,10,4,Tone.MetalM);Line(0,15,8,15,Tone.MetalL);break;
            case MonsterFamily.Winged:
                Stamp(new[]{"f.........","ff........","fFfff.....","fFFffff...","fffFFffff.","..ffffffff","....ffffff"},-4,7);
                Stamp(new[]{".........f","........ff",".....fffFf","...ffffFFf",".ffffFFfff","ffffffff..","ffffff...."},27,7);Line(1,13,12,22,Tone.FurD);Line(31,13,20,22,Tone.FurD);break;
            case MonsterFamily.Dragon:
                Line(-2,17,-2,22,Tone.FurL);Line(-2,22,8,26,Tone.FurM);Line(-1,23,8,27,Tone.FurD);Stamp(new[]{"i.......i","ii.....ii","aimmmmaii",".fffffff."},17,3);break;
            case MonsterFamily.Arthropod:
                for(int k=0;k<3;k++){Line(10,19+k*3,-2,15+k*4,Tone.FurD);Line(-2,15+k*4,1,20+k*4,Tone.AccM);Line(22,19+k*3,34,15+k*4,Tone.FurD);Line(34,15+k*4,31,20+k*4,Tone.AccM);}break;
            case MonsterFamily.Plant:
                Stamp(new[]{"..ggg..",".gfffgg","ggfffgf",".fffff.","..fff.."},-1,9);Stamp(new[]{"..ggg..","ggfffg.","fgfffgg",".fffff.","..fff.."},27,9);Line(3,13,13,23,Tone.LeatherM);Line(30,13,20,23,Tone.LeatherM);break;
            case MonsterFamily.Serpent:Oval(12,24,14,4);Line(0,24,16,24,Tone.AccM);break;
            case MonsterFamily.Aquatic:
                Stamp(new[]{"f......f","ff....ff","fff..fff","ffffffff",".ffffff.","..ffff.."},-2,16);Oval(14,22,13,6);break;
            case MonsterFamily.Slime:Oval(15,22,15,6);Oval(8,20,8,7);break;
            case MonsterFamily.Beast:Oval(11,19,10,9);Stamp(new[]{"i...","ii..","aii.",".fff"},24,18);break;
        }
    }

    // Detalhes aderem à superfície: não são partículas independentes nem novos efeitos de combate.
    void ElementDetails(int frame)
    {
        int pulse=(frame/2)%4;
        for(int y=12;y<25;y++)for(int x=6;x<27;x++){
            int px=x+ox,py=y+oy;var t=layer.Get(px,py);
            if(t is not (Tone.FurM or Tone.FurD))continue;
            if(VisualPhase>0&&Spec.Rank==Rank.Boss&&(x+y)%7==0)layer.Set(px,py,Tone.AccM);
            bool vein=(x+y+Spec.Pattern)%9==0&&x%3!=0;
            if(Spec.Element==Element.Fire&&vein)layer.Set(px,py,pulse==1?Tone.AccM:Tone.AccD);
            if(Spec.Element==Element.Ice&&x%6==0&&y%5<2)layer.Set(px,py,y%5==0?Tone.MetalL:Tone.MetalM);
            if(Spec.Element==Element.Plant&&y%6==0&&x%5<2)layer.Set(px,py,Tone.FurL);
            if(Spec.Element==Element.Poison&&x%7<2&&y%6<2)layer.Set(px,py,x%7==0&&y%6==0?Tone.FurL:Tone.AccD);
            if(Spec.Element==Element.Shadow&&vein)layer.Set(px,py,Tone.ClothD);
        }
        if(Spec.Rank!=Rank.Minion){Line(8,21,11,23,Tone.MetalM);Line(23,21,26,23,Tone.MetalM);Rect(8,20,3,1,Tone.MetalL);}
    }

    void AnimateAnatomy(MonsterMotion motion,int frame,int center,int ground)
    {
        // Blocos rígidos preservam os planos da pedra: comprimir colunas fazia a morte parecer líquida.
        if(motion==MonsterMotion.Death&&Spec.Family==MonsterFamily.Golem&&frame>0){
            layer.Clear();
            foreach(var (x,y,w,h,delay) in new[]{(4,15,7,7,0),(22,15,6,7,1),(10,10,12,9,2),(10,21,11,5,0)}){
                int fall=Math.Max(0,frame-delay),height=Math.Max(2,h-frame/2);
                int bx=center-16+x+(x<16?-1:1)*frame/3,by=Math.Min(ground-height-1,ground-29+y+fall*fall);
                layer.Rect(bx,by,w,height,Tone.FurM);layer.Rect(bx,by,w-1,1,Tone.FurL);layer.Rect(bx,by+height-1,w,1,Tone.FurD);
                if(height>3)layer.Line(bx+w/2,by+1,bx+w/2-1,by+height-2,Tone.FurD);
            }
            return;
        }
        var old=(Tone[])layer.Px.Clone();layer.Clear();
        bool moving=motion is MonsterMotion.Walk or MonsterMotion.Run;
        int gait=new[]{0,1,2,1,0,-1,-2,-1}[frame%8];
        int attack=motion==MonsterMotion.Attack?new[]{-1,-2,2,3,1,0}[frame]:0;
        for(int y=1;y<layer.Dimension-1;y++)for(int x=1;x<layer.Dimension-1;x++){
            var tone=old[y*layer.Dimension+x];if(tone==Tone.None)continue;
            int lx=x-(center-16),ly=y-(ground-29),nx=x,ny=y;
            if(motion==MonsterMotion.Death){
                int fall=frame;
                switch(Spec.Family){
                    case MonsterFamily.Slime:ny=ground-(ground-y)*(7-fall)/7;nx=x+(x<center?-1:1)*fall/4;break;
                    case MonsterFamily.Dragon:ny=lx<10?Math.Min(ground-1,y+Math.Max(0,fall-3)*2):ground-(ground-y)*(7-fall)/7;break;
                    case MonsterFamily.Beast:ny=ground-(ground-y)*(7-fall)/7;nx=x+(ly<20?fall/2:0);break;
                    case MonsterFamily.Arthropod:ny=ly<22?Math.Min(ground-2,y+fall):ground-6+fall/2;nx=center+(x-center)*(10-fall)/10;break;
                    case MonsterFamily.Winged:ny=Math.Min(ground-1,y+fall*2);nx=center+(x-center)*(9-fall)/9;break;
                    case MonsterFamily.Serpent:ny=ground-(ground-y)*(7-fall)/7;nx=x+(ly<20?fall/2:-fall/3);break;
                    case MonsterFamily.Aquatic:ny=ground-(ground-y)*(7-fall)/7;nx=center+(x-center)*(12-fall)/12;break;
                    case MonsterFamily.Plant:ny=Math.Min(ground-1,y+fall+(Math.Abs(x-center)*fall/8));if(fall>3)tone=Tone.LeatherD;break;
                }
            }else{
                switch(Spec.Family){
                    case MonsterFamily.Slime:if(ly<27){ny+=moving?(ly<18?gait/2:0):attack<0?1:attack>0?-1:0;}break;
                    case MonsterFamily.Golem:if(lx<10||lx>22){ny+=moving?gait/2:attack;nx+=attack>0&&lx>22?1:0;}break;
                    case MonsterFamily.Winged:if(lx<11||lx>22)ny+=(moving?gait:((frame+2)%8<4?0:1));break;
                    case MonsterFamily.Serpent:if(ly<23)nx+=moving?gait/2:attack/2;break;
                    case MonsterFamily.Aquatic:if(lx<10)ny+=moving?gait/2:((frame+2)%8<4?0:1);break;
                    case MonsterFamily.Plant:if(lx<11||lx>22)ny+=moving?gait/2:attack/2;break;
                    case MonsterFamily.Arthropod:if(lx<8||lx>24)ny+=moving?(lx<8?gait/2:-gait/2):attack/2;break;
                    default:if(ly>=25&&moving)nx+=(lx<16?gait/2:-gait/2);if(lx<10&&ly<25)ny+=(frame+2)%8<4?0:1;break;
                }
            }
            // Limites de pose, como o Reach das armas: a anatomia não invade a margem do contorno.
            nx=Math.Clamp(nx,2,layer.Dimension-3);ny=Math.Clamp(ny,2,ground-1);
            layer.Set(nx,ny,tone);
        }
        if(motion==MonsterMotion.Idle&&entryLift==0)for(int x=center-4;x<center+4;x++)layer.Set(x,ground-1,Tone.FurD);
    }
}
