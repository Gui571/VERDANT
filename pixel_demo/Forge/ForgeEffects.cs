namespace Verdant.Forge;
public static class ForgeEffects
{
    // Somente tons já existentes na paleta; a margem de segurança é de dois pixels.
    public static void Special(Raster buffer,ForgePalette palette,int frame,bool cast)
    {
        if(frame<2||frame>6)return;int cx=buffer.Width/2,cy=buffer.Height-18,r=3+(frame-2)%3;
        for(int y=-r;y<=r;y++)for(int x=-r;x<=r;x++)if(Math.Abs(x)+Math.Abs(y)==r){int px=cx+x,py=cy+y;buffer.Pixel(px,py,palette.MetalL);}
        if(!cast){for(int i=0;i<5;i++)buffer.Pixel(cx+5+i,cy+i/2,palette.White);}
    }
    public static ForgeValidation.Result ValidateIcons(IEnumerable<string> ids){int checks=0;var failures=new List<string>();foreach(var id in ids){var a=Icon(id);var b=Icon(id);checks+=3;if(a.Clipped!=0)failures.Add("icon bounds "+id);var colors=new HashSet<uint>();bool alpha=true;for(int i=0;i<a.Pixels.Length;i+=4){alpha&=a.Pixels[i+3] is 0 or 255;if(a.Pixels[i+3]>0)colors.Add(BitConverter.ToUInt32(a.Pixels,i));}if(!alpha||colors.Count>3||colors.Count==0)failures.Add("icon palette "+id);if(!a.Pixels.SequenceEqual(b.Pixels))failures.Add("icon determinism "+id);}return new(checks,failures);}
    public static Raster Icon(string id)
    {
        var r=new Raster(16,16);var l=new Layer(16);
        // Símbolos legíveis por habilidade, com a mesma rampa de três cores.
        switch(id){
            case "ice-wall": l.Rect(4,3,8,7,Tone.ClothM);l.Line(4,10,8,13,Tone.ClothM);l.Line(11,10,8,13,Tone.ClothM);l.Line(8,4,8,10,Tone.MetalL);break;
            case "lightning-cut": l.Line(10,2,5,8,Tone.MetalL);l.Line(5,8,10,8,Tone.MetalL);l.Line(10,8,5,13,Tone.MetalL);break;
            case "arrow-rain": for(int x=4;x<=12;x+=4){l.Line(x,3,x-2,11,Tone.MetalL);l.Line(x-2,11,x-3,8,Tone.ClothM);}break;
            case "nova": for(int k=0;k<4;k++){l.Line(3+k,7,11-k,7,Tone.MetalL);l.Line(7,3+k,7,11-k,Tone.MetalL);}break;
            case "quake": l.Rect(4,3,8,4,Tone.ClothM);l.Line(8,7,8,12,Tone.MetalL);l.Line(3,12,5,10,Tone.MetalL);l.Line(11,10,13,12,Tone.MetalL);break;
            case "dagger-dance": l.Line(3,3,12,12,Tone.MetalL);l.Line(12,3,3,12,Tone.MetalL);l.Line(2,9,6,13,Tone.ClothM);l.Line(9,13,13,9,Tone.ClothM);break;
            case "blessing": l.Rect(6,3,4,10,Tone.MetalL);l.Rect(3,6,10,4,Tone.MetalL);break;
            case "acid": l.Line(8,3,4,9,Tone.ClothM);l.Line(8,3,12,9,Tone.ClothM);l.Rect(4,9,9,3,Tone.ClothM);l.Line(6,8,6,10,Tone.MetalL);break;
            case "roar": l.Line(3,6,7,3,Tone.ClothM);l.Line(3,9,7,12,Tone.ClothM);l.Line(7,3,7,12,Tone.MetalL);l.Line(10,5,12,7,Tone.MetalL);l.Line(12,7,10,10,Tone.MetalL);break;
            case "breath": l.Line(3,8,12,3,Tone.ClothM);l.Line(3,8,12,12,Tone.ClothM);l.Line(5,8,12,8,Tone.MetalL);break;
            case "web": l.Line(3,3,12,12,Tone.MetalL);l.Line(12,3,3,12,Tone.MetalL);l.Line(3,7,12,7,Tone.ClothM);l.Line(7,3,7,12,Tone.ClothM);l.Line(3,7,7,3,Tone.MetalL);l.Line(7,3,12,7,Tone.MetalL);break;
            case "dive": l.Line(3,3,8,7,Tone.ClothM);l.Line(12,3,8,7,Tone.ClothM);l.Line(8,7,8,13,Tone.MetalL);l.Line(5,10,8,13,Tone.MetalL);l.Line(11,10,8,13,Tone.MetalL);break;
            case "constrict": l.Rect(4,4,8,3,Tone.ClothM);l.Rect(4,9,8,3,Tone.ClothM);l.Line(11,6,11,10,Tone.MetalL);l.Line(4,3,4,5,Tone.MetalL);break;
            case "water-jet": for(int y=4;y<=12;y+=4){l.Line(3,y,7,y-2,Tone.MetalL);l.Line(7,y-2,12,y,Tone.ClothM);}break;
            case "roots": l.Line(8,3,8,12,Tone.MetalL);l.Line(8,7,3,4,Tone.ClothM);l.Line(8,7,12,4,Tone.ClothM);l.Line(8,9,3,13,Tone.ClothM);l.Line(8,9,12,13,Tone.ClothM);break;
            default: l.Rect(6,3,4,4,Tone.MetalL);l.Rect(5,8,6,5,Tone.ClothM);l.Rect(2,7,2,4,Tone.MetalL);l.Rect(12,7,2,4,Tone.MetalL);break;
        }
        ForgeFinish.Outline(l);
        var pal=ForgeRoster.All[0].Palette;for(int y=0;y<16;y++)for(int x=0;x<16;x++)if(l.Get(x,y)!=Tone.None)r.Pixel(x,y,pal.Resolve(l.Get(x,y)));return r;
    }
}
