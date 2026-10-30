using Godot;
namespace Verdant;

public static class RosterVisualRules
{
    public const int Cell=32, FootY=29, HeadWidth=14, TorsoHeight=5, MaxColors=16;
    public static readonly Vector2I Origin=new(16,FootY);
    public static readonly Color Outline=new("151829"), Highlight=new("f0f0e5"), Metal=new("829fb9");
}
public sealed record RosterEntry(AdventurerKind Kind,string Id,string Name,string Class,string Description,string Silhouette,string[] Colors);
public static class RosterCatalog
{
    public static readonly IReadOnlyList<RosterEntry> All=Array.AsReadOnly(new[]{
        new RosterEntry(AdventurerKind.Duck,"bram","Bram","Guerreiro","Capacete de gelo, machado e escudo. Guarda firme e silhueta compacta.","Coroa ramificada + escudo",new[]{"5098ef","c1dceb","dfce80"}),
        new RosterEntry(AdventurerKind.Rabbit,"nix","Nix","Batedora","Orelhas longas, lenço cobre e espada curta. Leve e alerta.","Orelhas verticais + espada",new[]{"f0f0e5","5098ef","e5953d"}),
        new RosterEntry(AdventurerKind.Knight,"rowan","Rowan","Arqueiro","Capuz verde, arco e aljava. Couro e tecido em blocos simples.","Capuz inclinado + arco",new[]{"849967","4e6754","947052"}),
        new RosterEntry(AdventurerKind.Mage,"iria","Iria","Maga astral","Chapéu violeta, grimório e cristal suspenso. Curiosa e metódica.","Triângulo alto + cristal",new[]{"9678cf","514777","edacd3"}),
        new RosterEntry(AdventurerKind.Guardian,"boro","Boro","Guardião","Carapaça de bronze, escudo-torre e martelo. Calmo e inabalável.","Massa larga + retângulos",new[]{"c29454","765740","a6b477"}),
        new RosterEntry(AdventurerKind.Duelist,"suri","Suri","Duelista","Raposa de lenço vinho, cauda clara e duas adagas. Ágil e provocadora.","Orelhas triangulares + cauda",new[]{"df9855","8c4665","f0e6cd"}),
        new RosterEntry(AdventurerKind.Healer,"mira","Mira","Curandeira","Coruja de manto jade e báculo circular. Atenta e acolhedora.","Capuz redondo + aro",new[]{"57afa4","356e78","e5dcc2"})
    });
    public static RosterEntry Get(AdventurerKind kind)=>All.First(e=>e.Kind==kind);
}
