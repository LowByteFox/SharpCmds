using Discord;
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
    private readonly Dictionary<string, SlashCommand> _botCommands = new Dictionary<string, SlashCommand>();
    private readonly Dictionary<string, SlashCommand> _botComponents = new Dictionary<string, SlashCommand>();
    
    private readonly Dictionary<ulong, long> _timeouts = new Dictionary<ulong, long>();

    // * Bot Settings
    protected long Timeout { get; init; }
    protected CommandType Type { get; init; }
    protected ulong DevGuildId { get; init; }
    protected string WaitMsg { get; init; } = "Please wait {0} seconds!";
    
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
        Client.ButtonExecuted += ButtonHandler;
        Client.SelectMenuExecuted += MenuHandler;
        // _client.ModalSubmitted += ModalHandler;

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
        long now = DateTimeOffset.Now.ToUnixTimeSeconds();
        if (!_timeouts.ContainsKey(command.User.Id))
        {
            _timeouts.Add(command.User.Id, now + Timeout);
        }
        else
        {
            long user = _timeouts[command.User.Id];
            if (user > now)
            {
                await command.RespondAsync(String.Format(WaitMsg, user - now), ephemeral: true);
                return;
            }
                
            _timeouts[command.User.Id] = now + Timeout;
        }
        var cls = _botCommands[command.Data.Name];
        await cls.Run(command);
    }
    
    private async Task ButtonHandler(SocketMessageComponent component)
    {
        var cls = _botComponents[component.Data.CustomId];
        await cls.OnComponent(component);
    }
    
    private async Task MenuHandler(SocketMessageComponent component)
    {
        var cls = _botComponents[component.Data.CustomId];
        await cls.OnComponent(component);
    }

    private void RegisterComponents(SlashCommand var)
    {
        foreach (var id in var.Ids)
        {
            _botComponents.Add(id, var);
        }
    }
    
    protected void RegisterSlashCommand(SlashCommand var) {
        _botCommands.Add(var.GetType().Name.ToLower(), var);
        RegisterComponents(var);
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