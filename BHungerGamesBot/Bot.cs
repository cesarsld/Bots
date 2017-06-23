using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace BHungerGaemsBot
{
    public class Bot
    {
        /*
         URL to add bot: https://discordapp.com/api/oauth2/authorize?client_id=322530501774540800&scope=bot&permissions=0
        Test Info:
         URL to add bot: https://discordapp.com/api/oauth2/authorize?client_id=326010315760205834&scope=bot&permissions=0
        */
        public static string AppName = "BHungerGamesBot";
        public static string AppVersion = "1.0.0.3";
        // Real Bot Token
        // Test Bot Token
        public static string AppToken = "MzI2MDEwMzE1NzYwMjA1ODM0.DCgkSA.EkhQj0DVvMlBgBLjqWnC9hJ1vtE";
        public static char CommandPrefix = '?';

        public static DiscordSocketClient DiscordClient { get; set; }
        private readonly CommandService _commands;
        //private IServiceProvider _services;

        /// <summary>
        /// Constructor for the Bot class.
        /// </summary>
        public Bot()
        {
            if (DiscordClient != null)
            {
                throw new Exception("Bot already running");
            }
            Logger.LogInternal("Creating client.");
            DiscordClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 1000,

                // If your platform doesn't have native websockets,
                // add Discord.Net.Providers.WS4Net from NuGet,
                // add the `using` at the top, and uncomment this line:
                //WebSocketProvider = WS4NetProvider.Instance
            });

            _commands = new CommandService();
            DiscordClient.Log += Logger.Log;
            DiscordClient.MessageReceived += HandleCommandAsync;
        }

        ~Bot()
        {
            DiscordClient = null;
        }

        public async Task HandleCommandAsync(SocketMessage messageParam)
        {
            try
            {
                var msg = messageParam as SocketUserMessage;
                if (msg == null) return;

                int argPos = 0;

                if (msg.HasCharPrefix(CommandPrefix, ref argPos)) /* || msg.HasMentionPrefix(_client.CurrentUser, ref pos) */
                {
                    Logger.LogInternal($"HandleCommandAsync User: {(msg.Author == null ? "NULL" : msg.Author.ToString())}  Msg: {msg}");

                    //var context = new SocketCommandContext(DiscordClient, msg);
                    var context = new CommandContext(DiscordClient, msg);
                    var result = await _commands.ExecuteAsync(context, argPos);

                    if (!result.IsSuccess) // If execution failed, reply with the error message.
                    {
                        string message = "Command Failed: " + result;
                        await Logger.Log(new LogMessage(LogSeverity.Error, "HandleCommandAsync", message));
                        await context.Channel.SendMessageAsync(message);
                    }
                }
            }
            catch (Exception ex)
            {
                await Logger.Log(new LogMessage(LogSeverity.Error, "HandleCommandAsync", "Unexpected Exception", ex));
            }
        }

        /// <summary>
        /// Start the Discord client.
        /// </summary>
        public async Task RunAsync()
        {
            Logger.LogInternal("Registering commands.");
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());

            Logger.LogInternal("Connecting to the server.");
            await DiscordClient.LoginAsync(TokenType.Bot, AppToken);
            await DiscordClient.StartAsync();
            await Task.Delay(-1);
        }
    }
}
