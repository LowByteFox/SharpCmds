using Discord;
using Discord.WebSocket;

namespace SharpCmds.Commands;

public abstract class SlashCommand
{
    protected string Description { get; init; } = "";

    protected readonly SlashCommandBuilder Cmd = new SlashCommandBuilder();
    public readonly List<String> Ids = new List<string>();

    public SlashCommandBuilder Build()
    {
        Cmd.WithName(GetType().Name.ToLower());
        Cmd.WithDescription(Description);
        
        return Cmd;
    }

    protected string CustomId(String id)
    {
        Ids.Add($"{GetType().Name.ToLower()}_{id}");
        return $"{GetType().Name.ToLower()}_{id}";
    }

    public virtual Task Run(SocketSlashCommand command)
    {
        return Task.CompletedTask;
    }
    
    public virtual Task OnComponent(SocketMessageComponent component)
    {
        return Task.CompletedTask;
    }
}