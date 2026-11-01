namespace Verdant.Forge;
// Mesmo acabamento do elenco, agora acessível a todas as receitas e células.
public static class ForgeFinish
{
    public static void Outline(Layer layer)
    {
        var filled=new List<(int,int)>();for(int y=0;y<layer.Dimension;y++)for(int x=0;x<layer.Dimension;x++)if(layer.Get(x,y)!=Tone.None)filled.Add((x,y));layer.Halo(filled);
    }
    public static void Shadow(Layer layer,int cx,int ground,int radius)
    {
        for(int x=cx-radius;x<=cx+radius;x++)if(layer.Get(x,ground)==Tone.None)layer.Set(x,ground,Tone.Ink);
        for(int x=cx-radius+2;x<=cx+radius-2;x++)if(layer.Get(x,ground+1)==Tone.None)layer.Set(x,ground+1,Tone.Ink);
    }
}
