using System.Text.Json;
namespace Verdant.Terrain;
public static class TerrainSave
{
    sealed record Document(int Version,TerrainRecipe Recipe);
    public static string Serialize(TerrainRecipe recipe){recipe.Validate();return JsonSerializer.Serialize(new Document(1,recipe),new JsonSerializerOptions{WriteIndented=true});}
    public static TerrainRecipe Deserialize(string json){var doc=JsonSerializer.Deserialize<Document>(json)??throw new ArgumentException("Receita vazia");if(doc.Version!=1||doc.Recipe==null)throw new ArgumentException("Versão de terreno não suportada");doc.Recipe.Validate();return doc.Recipe;}
}
