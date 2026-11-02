namespace Verdant.Battle;
public sealed record BattleInput(int Tick,string Kind,int Unit,float X,float Y);
public sealed partial class BattleSimulation
{
    readonly List<BattleInput> inputs=new();public IReadOnlyList<BattleInput> Inputs=>inputs.AsReadOnly();
    BattleInput[] replay=Array.Empty<BattleInput>();int replayIndex;bool applyingReplay;
    public void QueueReplay(IEnumerable<BattleInput> commands){if(Tick!=0)throw new InvalidOperationException("Replay precisa começar no tick zero");replay=commands.ToArray();}
    void RecordInput(string kind,int unit,float x,float y){if(!applyingReplay)inputs.Add(new(Tick,kind,unit,x,y));}
    void ApplyReplay(){while(replayIndex<replay.Length&&replay[replayIndex].Tick<=Tick){var c=replay[replayIndex++];applyingReplay=true;switch(c.Kind){case "manual":SetManual(c.Unit,c.X!=0);break;case "move":MoveOrder(c.Unit,new(c.X,c.Y));break;case "place":Place(c.Unit,new(c.X,c.Y));break;case "retreat":Retreat(c.Unit);break;case "special":CommandSpecial(c.Unit);break;}applyingReplay=false;inputs.Add(c);}}
}
