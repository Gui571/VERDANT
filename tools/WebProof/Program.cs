using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
namespace WebProof;
public static class Program
{
    static ProofScenario scenario=new();
    public static async Task Main(string[] args)
    {
        var builder=WebAssemblyHostBuilder.CreateDefault(args);
        var host=builder.Build();
        await host.Services.GetRequiredService<IJSRuntime>().InvokeVoidAsync("proofReady");
        await host.RunAsync();
    }
    [JSInvokable] public static object Reset(){scenario=new();return new {sprites=scenario.Sprites,state=scenario.Snapshot()};}
    [JSInvokable] public static object Advance(int ticks){scenario.Advance(ticks);return scenario.Snapshot();}
    [JSInvokable] public static bool Special(int id)=>scenario.Battle.CommandSpecial(id);
    [JSInvokable] public static string Trace()=>scenario.Trace();
}
