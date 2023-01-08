using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using dotenv.net;
using Newtonsoft.Json;
using SharpCmds.Commands;

namespace SharpCmds;

public enum CommandType
{
    Guild,
    Global
}

public abstract class SharpBot
{
    // * Bot Related Stuff
    private readonly IDictionary<string, string?> _env;
    
    protected DiscordSocketClient Client;
    private readonly Dictionary<string, Command> _botCtxCommands = new Dictionary<string, Command>();
    private readonly Dictionary<string, Command> _botCtxComponents = new Dictionary<string, Command>();
    private readonly Dictionary<string, SlashCommand> _botCommands = new Dictionary<string, SlashCommand>();
    private readonly Dictionary<string, SlashCommand> _botComponents = new Dictionary<string, SlashCommand>();
    
    private readonly Dictionary<ulong, long> _timeouts = new Dictionary<ulong, long>();

    // * Bot Settings
    protected string Prefix { get; init; } = "SharpBot!";
    protected long Timeout { get; init; }
    protected CommandType Type { get; init; }
    protected ulong DevGuildId { get; init; }
    protected string WaitMsg { get; init; } = "Please wait {0} seconds!";
    protected string NotEnoughArgs { get; init; } = "Not enough args!";
    
    protected SharpBot()
    {
        _env = DotEnv.Fluent().WithExceptions().WithEnvFiles().WithTrimValues().Read();
        var config = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.All
        };

        Client = new DiscordSocketClient(config);
    }
    protected async Task MainAsync()
    {
        // * Setting Events
        Client.Log += Log;
        Client.Ready += ClientReady;
        Client.SlashCommandExecuted += SlashHandler;
        Client.ButtonExecuted += ComponentHandler;
        Client.SelectMenuExecuted += ComponentHandler;
        Client.ModalSubmitted += ComponentHandler;
        Client.MessageReceived += CommandHandler;

        await Client.LoginAsync(TokenType.Bot, _env["TOKEN"]);
        await Client.StartAsync();

        await Task.Delay(-1);
    }
    
    private Task Log(LogMessage msg)
    {
        switch (msg.Severity)
        {
            case LogSeverity.Info:
                LogColor(msg.Message, "Info", ConsoleColor.Cyan);
                break;
            case LogSeverity.Warning:
                LogColor(msg.Message, "Warning", ConsoleColor.Yellow);
                break;
            case LogSeverity.Verbose:
                LogColor(msg.Message, "Verbose", ConsoleColor.Green);
                break;
            case LogSeverity.Critical:
                LogColor(msg.Message, "Info", ConsoleColor.Magenta);
                break;
            case LogSeverity.Error:
                LogColor(msg.Message, "Info", ConsoleColor.Red);
                break;
            case LogSeverity.Debug:
                LogColor(msg.Message, "Info", ConsoleColor.Blue);
                break;
        }
        return Task.CompletedTask;
    }
    
    private async Task SlashHandler(SocketSlashCommand command)
    {
        if (CheckTimeout(command.User.Id))
        {
            await command.RespondAsync(String.Format(WaitMsg, 
                _timeouts[command.User.Id] - DateTimeOffset.Now.ToUnixTimeSeconds()), ephemeral: true);
            return;
        }
        var cls = _botCommands[command.Data.Name];
        await cls.Run(command);
    }

    private bool CheckTimeout(ulong id)
    {
        long now = DateTimeOffset.Now.ToUnixTimeSeconds();
        if (!_timeouts.ContainsKey(id))
        {
            _timeouts.Add(id, now + Timeout);
        }
        else
        {
            long user = _timeouts[id];
            if (user > now)
            {
                return true;
            }
                
            _timeouts[id] = now + Timeout;
        }

        return false;
    }

    private async Task CommandHandler(SocketMessage message)
    {
        var msg = message as SocketUserMessage;
        if (msg == null) return;

        if (msg.Author.IsBot) return;
        if (!msg.CleanContent.StartsWith(Prefix)) return;

        SharpContext context = new SharpContext(Client, msg);

        var clearMsg = msg.Content.Replace(Prefix, "").Split(" ");
        var name = clearMsg[0];
        if (!_botCtxCommands.ContainsKey(name))
        {
            LogColor($"Command {name} doesn't exist!", "Warning", ConsoleColor.Yellow);
            return;
        }
        
        if (CheckTimeout(msg.Author.Id))
        {
            await msg.ReplyAsync(String.Format(WaitMsg, 
                _timeouts[msg.Author.Id] - DateTimeOffset.Now.ToUnixTimeSeconds()));
            return;
        }
        
        var cmd = _botCtxCommands[name];

        if (cmd.args.Count() + 1 != clearMsg.Length)
        {
            await msg.ReplyAsync(NotEnoughArgs);
            return;
        }
        
        for (var i = 0; i < cmd.args.Count; i++)
        {
            context.args.Add(cmd.args[i], clearMsg[i + 1]);
        }

        await cmd.Run(context);
    }
    
    private async Task ComponentHandler(SocketMessageComponent component)
    {
        if (_botCtxComponents.ContainsKey(component.Data.CustomId))
        {
            var cls = _botCtxComponents[component.Data.CustomId];
            await cls.OnComponent(component);
        }
        else
        {
            var cls = _botComponents[component.Data.CustomId];
            await cls.OnComponent(component);
        }
    }
    
    private async Task ComponentHandler(SocketModal component)
    {
        if (_botCtxComponents.ContainsKey(component.Data.CustomId))
        {
            var cls = _botCtxComponents[component.Data.CustomId];
            await cls.OnComponent(component);
        }
        else
        {
            var cls = _botComponents[component.Data.CustomId];
            await cls.OnComponent(component);
        }
    }

    private void RegisterComponents(SlashCommand var)
    {
        foreach (var id in var.Ids)
        {
            _botComponents.Add(id, var);
        }
    }
    
    private void RegisterComponents(Command var)
    {
        foreach (var id in var.Ids)
        {
            _botCtxComponents.Add(id, var);
        }
    }
    
    protected void RegisterSlashCommand(SlashCommand var) {
        _botCommands.Add(var.GetType().Name.ToLower(), var);
        RegisterComponents(var);
    }

    protected void RegisterCommand(Command cmd)
    {
        _botCtxCommands.Add(cmd.GetType().Name.ToLower(), cmd);
        RegisterComponents(cmd);
    }

    protected virtual Task Ready()
    {
        return Task.CompletedTask;
    }
    private async Task ClientReady()
    {
        await Ready();
        try
        {
            foreach (var cmd in _botCommands)
            {
                if (Type == CommandType.Guild)
                    await Client.Rest.CreateGuildCommand(cmd.Value.Build().Build(), DevGuildId);
                else
                    await Client.Rest.CreateGlobalCommand(cmd.Value.Build().Build());
            }
        }
        catch(HttpException exception)
        {
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
            Console.WriteLine(json);
        }
    }
    
    private void LogColor(string msg, string type, ConsoleColor color)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"{DateTimeOffset.Now.Hour}:{DateTimeOffset.Now.Minute}:{DateTimeOffset.Now.Second} [");
        Console.ForegroundColor = color;
        Console.Write(type);
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"] {msg}");
    }
}