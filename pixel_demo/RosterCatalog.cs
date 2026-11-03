using Verdant.Forge;
namespace Verdant;

public static class RosterVisualRules
{
    public const int Cell=32, FootY=29, HeadWidth=14, TorsoHeight=5, MaxColors=16;
    public static readonly PixelPoint Origin=new(16,FootY);
    public static readonly Rgba Outline=new("151829"), Highlight=new("f0f0e5"), Metal=new("829fb9");
}
public sealed record RosterEntry(AdventurerKind Kind,string Id,string Name,string Class,string Description,string Silhouette,string[] Colors);
/// <summary>Catálogo do elenco fixo. Nome, classe, descrição e cores vêm das receitas do <see cref="ForgeRoster"/> (fonte única).</summary>
public static class RosterCatalog
{
    static readonly Dictionary<string,string> Silhouettes=new(){
        ["bram"]="Crista de espinhos + escudo redondo",["nix"]="Orelhas verticais + espada",["rowan"]="Capuz inclinado + arco",
        ["iria"]="Triângulo alto + cristal",["boro"]="Aba larga + carapaça + martelo",["suri"]="Orelhas triangulares + cauda",["mira"]="Capuz redondo + aro"};
    static string Hex(Rgba c)=>((int)MathF.Round(c.R*255)).ToString("x2")+((int)MathF.Round(c.G*255)).ToString("x2")+((int)MathF.Round(c.B*255)).ToString("x2");
    public static readonly IReadOnlyList<RosterEntry> All=Array.AsReadOnly(ForgeRoster.All.Select((s,i)=>new RosterEntry((AdventurerKind)i,s.Id,s.Name,s.Class,s.Description,Silhouettes[s.Id],
        new[]{Hex(s.Palette.ClothM),Hex(s.Palette.FurM),Hex(s.Palette.AccM)})).ToArray());
    public static RosterEntry Get(AdventurerKind kind)=>All.First(e=>e.Kind==kind);
}
