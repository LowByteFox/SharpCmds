using Discord.Commands;
using Discord.WebSocket;

namespace SharpCmds;

public class SharpContext : SocketCommandContext
{
    public Dictionary<string, string> args { get; set; } = new Dictionary<string, string>();

    public SharpContext(DiscordSocketClient client, SocketUserMessage msg) : base(client, msg) {}
}