# SharpCmds
<img alt="Icon" src="https://raw.githubusercontent.com/Fire-The-Fox/SharpCmds/develop/icon.png" width="256">

### SharpCmds is a Discord Bot framework based on top of Discord.Net

## Sample bot
`Program.cs`
```cs
using Discord;
using DiscordBot.commands;
using SharpCmds;

namespace DiscordBot
{
    public class Program : SharpBot
    {

        private Program()
        {
            Timeout = 5;
            DevGuildId = 934731487683706881;
            WaitMsg = "HEY! Wait {0} seconds";
            Type = CommandType.Guild;
        }
        
        public static Task Main(string[] args) => new Program().MainAsync();

        protected override Task Ready()
        {
            RegisterSlashCommand(new Hello());
            
            Client.SetGameAsync("you from afar", type: ActivityType.Watching);

            return Task.CompletedTask;
        }
    }
}
```

`.env`
```dotenv
TOKEN=bot_token
```

`commands/Hello.cs`
```cs
using Discord;
using Discord.WebSocket;
using SharpCmds.Commands;

namespace DiscordBot.commands;

public class Hello : SlashCommand
{
    public Hello()
    {
        Description = "I'll say hello";
        Cmd.AddOption("name", ApplicationCommandOptionType.String, "Write a name", true);
    }
    
    public override Task Run(SocketSlashCommand command)
    {
        string? nameArg = command.Data.Options.First().Value.ToString();
        
        command.RespondAsync($"Hello {nameArg}!");
        
        return Task.CompletedTask;
    }
}
```