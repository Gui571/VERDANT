namespace Verdant.Forge.StyleV2;

public readonly record struct Ramp5(uint Deep, uint Shade, uint Base, uint Light, uint Shine)
{
    public uint this[int tone] => tone switch { 0=>Deep, 1=>Shade, 2=>Base, 3=>Light, 4=>Shine, _=>throw new ArgumentOutOfRangeException(nameof(tone)) };
}
public enum Material { Empty, Ink, Fur, Team, Accent, Leather, Metal, Ivory, Blush }
public enum ProofPass { Lit, Silhouette, Materials }

// Cor e apresentação são opções da renderização; nenhuma entra no estado do combate.
public sealed record MaterialStyle(Ramp5 Fur, int Team=4, ProofPass Pass=ProofPass.Lit, Ramp5? Accent=null, Ramp5? TeamRamp=null)
{
    public const uint Ink=0x151829;
    public const int MaxColors=32;
    public Ramp5 Ramp(Material material)=>material switch {
        Material.Fur=>Fur, Material.Team=>TeamRamp??MaterialRamps.Teams[Team], Material.Accent=>Accent??MaterialRamps.Gold,
        Material.Leather=>MaterialRamps.Leather, Material.Metal=>MaterialRamps.Metal,
        Material.Ivory=>MaterialRamps.Bone, Material.Blush=>MaterialRamps.Rose,
        _=>throw new ArgumentOutOfRangeException(nameof(material)) };
    public static MaterialStyle For(ForgeSpec spec,int team=4)=>For(spec.Species,team) with{Accent=spec.Id switch{"iria"=>MaterialRamps.Jewel,"suri" or "mira"=>MaterialRamps.Ivory,_=>MaterialRamps.Amber}};
    public static MaterialStyle For(Species species,int team=4)=>new(species switch {
        Species.Duck=>MaterialRamps.Duck, Species.Rabbit=>MaterialRamps.Rabbit, Species.Human=>MaterialRamps.Human,
        Species.Turtle=>MaterialRamps.Turtle, Species.Fox=>MaterialRamps.Fox, Species.Owl=>MaterialRamps.Owl,
        Species.Mouse=>MaterialRamps.Mouse, Species.Cat=>MaterialRamps.Cat, Species.Frog=>MaterialRamps.Frog,
        Species.Elf=>MaterialRamps.Elf, Species.Bear=>MaterialRamps.Bear,
        Species.Wolf=>MaterialRamps.Wolf, Species.Dog=>MaterialRamps.Dog, Species.Panda=>MaterialRamps.Panda, Species.Boar=>MaterialRamps.Boar, Species.Pig=>MaterialRamps.Pig,
        Species.Goat=>MaterialRamps.Goat, Species.Deer=>MaterialRamps.Deer, Species.Raccoon=>MaterialRamps.Raccoon, Species.Monkey=>MaterialRamps.Monkey, Species.Orc=>MaterialRamps.Orc,
        Species.Goblin=>MaterialRamps.Goblin, Species.Lizard=>MaterialRamps.Lizard, Species.Skeleton=>MaterialRamps.Bone, Species.Dwarf=>MaterialRamps.Dwarf, _=>throw new ArgumentOutOfRangeException(nameof(species)) },team);
    public static MaterialStyle For(Element element,int team=4)=>new(element switch {
        Element.Fire=>MaterialRamps.Fire,Element.Ice=>MaterialRamps.Ice,Element.Plant=>MaterialRamps.Plant,
        Element.Shadow=>MaterialRamps.Shadow,Element.Poison=>MaterialRamps.Poison,_=>throw new ArgumentOutOfRangeException(nameof(element)) },team);
}
public static class MaterialLighting
{
    public static Material Identify(Tone t)=>t switch {
        Tone.None=>Material.Empty,Tone.Ink=>Material.Ink,
        Tone.FurD or Tone.FurM or Tone.FurL=>Material.Fur,
        Tone.ClothD or Tone.ClothM or Tone.ClothL=>Material.Team,
        Tone.AccD or Tone.AccM=>Material.Accent,Tone.LeatherD or Tone.LeatherM=>Material.Leather,
        Tone.MetalD or Tone.MetalM or Tone.MetalL=>Material.Metal,Tone.White=>Material.Ivory,Tone.Pink=>Material.Blush,
        _=>throw new ArgumentOutOfRangeException(nameof(t)) };
    public static int Light(Layer layer,int x,int y)
    {
        var t=layer.Get(x,y);var material=Identify(t);
        int step=t switch {Tone.FurD or Tone.ClothD or Tone.LeatherD or Tone.MetalD or Tone.AccD=>1,Tone.FurL or Tone.ClothL or Tone.MetalL=>3,Tone.White=>4,_=>2};
        // A borda exposta superior/esquerda recebe luz; a borda inferior/direita, sombra.
        // Só usa inteiros e rótulos de material: mudar uma paleta nunca muda máscaras ou tons.
        bool Same(int dx,int dy)=>Identify(layer.Get(x+dx,y+dy))==material;
        if(!Same(0,-1)&&!Same(-1,0))step=Math.Min(4,step+1);
        else if(!Same(0,1)&&!Same(1,0))step=Math.Max(0,step-1);
        return step;
    }
    public static void Paint(Layer layer, Raster output, MaterialStyle style, bool flash=false, byte[]? materialIds=null)
    {
        if(style.Team is <0 or >5)throw new ArgumentOutOfRangeException(nameof(style.Team));
        for(int y=0;y<layer.Dimension;y++)for(int x=0;x<layer.Dimension;x++){
            var material=Identify(layer.Get(x,y));if(materialIds!=null)materialIds[y*layer.Dimension+x]=(byte)material;if(material==Material.Empty)continue;
            uint rgb=style.Pass==ProofPass.Silhouette?0x8CC6B6:material==Material.Ink?MaterialStyle.Ink:
                flash?MaterialRamps.Bone.Shine:style.Ramp(material)[style.Pass==ProofPass.Materials?2:Light(layer,x,y)];
            int i=(y*output.Width+x)*4;output.Pixels[i]=(byte)(rgb>>16);output.Pixels[i+1]=(byte)(rgb>>8);output.Pixels[i+2]=(byte)rgb;output.Pixels[i+3]=255;
        }
    }
}
