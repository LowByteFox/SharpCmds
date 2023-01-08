
# SharpCmds

SharpCmds is a Discord Bot framework based on top of Discord.Net


![Logo](https://raw.githubusercontent.com/Fire-The-Fox/SharpCmds/develop/SharpCmds_small.png)
## Authors

- [@FireTheFox](https://github.com/Fire-The-Fox)
## Features

- Simple Command Handlers
- Configurable
- Simple By Design
- Beginner Friendly


## Roadmap

* Slash and Text based commands will share the same context
* As described above, allow command usage in both ways -> Slash and Text based
* Automatic registration of commands
* Lavalink support
* Templates
* CLI
## Acknowledgements

- SharpCmds is still pretty unfinished, you'll probably find bugs
- Project was inspired by [gcommands](https://garlic-team.js.org/docs/)


## Run Locally

Create empty .NET core project

Install SharpCmds from nuget
```bash
dotnet add package SharpCmds
```

Create `.env` file with similar content
```env
TOKEN=YOUR_BOT_TOKEN
```


## Example

Create similar file structure
```
...
.env
Program.cs
commands/Ping.cs
commands/Hello.cs
...
```
Now, lets create files

`.env`
```env
TOKEN=YOUR_BOT_TOKEN
```

`Program.cs`
```cs
using Discord;
using <your_solution_name>.commands;
using SharpCmds;

namespace <your_solution_name>
{
    public class Program : SharpBot
    {
        private Program()
        {
            Timeout = 5;
            DevGuildId = DEV_SERVER_ID;
            WaitMsg = "HEY! Wait {0} seconds";
            Type = CommandType.Guild;
            Prefix = "!";
        }

        public static Task Main(string[] args) => new Program().MainAsync();

        protected override Task Ready()
        {
            RegisterSlashCommand(new Ping(Client.Latency));
            RegisterCommand(new Hello());
            return Task.CompletedTask;
        }
    }
}
```
Now lets create our commands

`commands/Ping.cs`
```cs
using Discord;
using Discord.WebSocket;
using SharpCmds.Commands;

namespace <your_solution_name>.commands;

public class Ping : SlashCommand
{
    private readonly int _latency;
    public Ping(int latency)
    {
        Description = "Simple pong";
        _latency = latency;
    }

    public override Task Run(SocketSlashCommand command)
    {
        command.RespondAsync($"Pong {_latency}ms!");

        return Task.CompletedTask;
    }
}
```

`commands/Hello.cs`
```cs
using Discord;
using SharpCmds;
using SharpCmds.Commands;

namespace <your_solution_name>.commands;

public class Hello : Command
{
    public Hello()
    {
        args.Add("name");
    }

    public override Task Run(SharpContext ctx)
    {
        ctx.Message.ReplyAsync($"Hello {ctx.args["name"]}!");
        
        return Task.CompletedTask;
    }
}
```

All you need to do is just execute your bot, remember that you need to be near where `.env` is located