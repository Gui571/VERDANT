using Godot;
using Verdant.Forge;
namespace Verdant;

public enum AdventurerKind { Duck, Rabbit, Knight, Mage, Guardian, Duelist, Healer }
public enum AdventurerMotion { Idle, Walk, Run, Attack, Hurt, Special, Cast }

/// <summary>Fachada estável do elenco: mantém a API usada pela demo, pelos testes e pelos exportadores,
/// mas o desenho agora vem do motor <see cref="ForgeRenderer"/> (peças + paleta por papéis + contorno automático).
/// Gramática compartilhada: célula 32×32, origem (16,29), cabeça de 14 px, tronco de 5 px, pés de 2 px,
/// contorno de 1 px, luz no canto superior esquerdo e no máximo 16 cores.</summary>
public sealed class CompactAdventurer
{
    public const int Size = 32;
    readonly ForgeRenderer renderer;
    public Raster Buffer => renderer.Buffer;
    public AdventurerKind Kind { get; }
    public ForgeSpec Spec => renderer.Spec;
    public string Name => Spec.Name + " · " + Spec.Class.ToLowerInvariant();
    public string Description => Spec.Description;
    public bool GroundShadow { get => renderer.GroundShadow; set => renderer.GroundShadow = value; }

    /// <param name="scale">Mantido por compatibilidade; a proporção da cabeça agora é fixa (14 px) para todo o elenco.</param>
    public CompactAdventurer(float scale = 1, AdventurerKind kind = AdventurerKind.Duck) : this(ForgeRoster.For(kind), kind) { }
    /// <summary>Personagem gerado (por semente ou editado): usa a mesma fachada e os mesmos exportadores.</summary>
    public CompactAdventurer(ForgeSpec spec, AdventurerKind kind = AdventurerKind.Duck) { Kind = kind; renderer = new ForgeRenderer(spec); }

    public Raster Render(double seconds, bool walking = false, bool back = false) => Render(seconds, walking ? AdventurerMotion.Walk : AdventurerMotion.Idle, back);
    public Raster Render(double seconds, AdventurerMotion motion, bool back = false) => renderer.Render(seconds, motion, back);
}
