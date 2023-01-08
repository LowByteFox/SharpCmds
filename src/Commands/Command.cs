using Discord.WebSocket;

namespace SharpCmds.Commands;

public abstract class Command
{
    public readonly List<string> args = new List<string>();
    public readonly List<String> Ids = new List<string>();
    
    protected string CustomId(String id)
    {
        var clsName = GetType().Name.ToLower();
        
        if (!Ids.Contains($"{clsName}_{id}"))
        {
            Ids.Add($"{clsName}_{id}");
        }
        return $"{clsName}_{id}";
    }

    public abstract Task Run(SharpContext ctx);
    
    public virtual Task OnComponent(SocketMessageComponent component)
    {
        return Task.CompletedTask;
    }
    
    public virtual Task OnComponent(SocketModal component)
    {
        return Task.CompletedTask;
    }
}