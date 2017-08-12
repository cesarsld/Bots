using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace BHungerGaemsBot
{
    public class BaseCommands : ModuleBase
    {
        private static readonly object SyncObj;
        private static readonly Dictionary<ulong, RunningCommandInfo> ChannelCommandInstances;

        static BaseCommands()
        {
            ChannelCommandInstances = new Dictionary<ulong, RunningCommandInfo>();
            SyncObj = new object();
        }

        public static RunningCommandInfo GetRunningCommandInfo(ulong channelId)
        {
            RunningCommandInfo instance;
            lock (SyncObj)
            {
                ChannelCommandInstances.TryGetValue(channelId, out instance);
            }
            return instance;
        }

        public static bool CreateChannelCommandInstance(string commandName, ulong userId, ulong channelId, ulong guildId, BotGameInstance gameInstance, out RunningCommandInfo instance)
        {
            lock (SyncObj)
            {
                ChannelCommandInstances.TryGetValue(channelId, out instance);
                if (instance == null)
                {
                    instance = new RunningCommandInfo(commandName, userId, channelId, guildId, gameInstance);
                    ChannelCommandInstances[channelId] = instance;
                    return true;
                }
            }
            return false;
        }

        public static void RemoveChannelCommandInstance(ulong channelId)
        {
            lock (SyncObj)
            {
                ChannelCommandInstances.Remove(channelId);
            }
        }

        public static bool ChannelHasRunningCommand(ulong channelId)
        {
            lock (SyncObj)
            {
                return ChannelCommandInstances.ContainsKey(channelId);
            }
        }

        public async Task LogAndReplyAsync(string message)
        {
            Logger.LogInternal(message);
            await ReplyAsync(message);
        }

        /* Roles that can run commands on Bit Heros Guild Only
            Server Admin    1 members
            Admin           3 members
            Mod             4 members
            Admin & Mods    8 members
            300+ OH EM GEE  3 members
            Level 250 - 299 1 members
            Level 200 - 249 29 members          */
        private bool CheckAccess(bool allowPrivateMessage = false)
        {
            try
            {
                if (Context.Guild == null)
                {
                    return allowPrivateMessage;
                }
                if (Context.Guild.Name.Equals("Bit Heroes"))
                {
                    HashSet<ulong> rolesWithAccess = new HashSet<ulong>();
                    var roles = Context.Guild.Roles;
                    foreach (IRole role in roles)
                    {
                        if (role.Name.Contains("300+") || role.Name.Contains("Admin") || role.Name.StartsWith("Mod ") || Context.User.Username.Contains("Owl of"))
                        {
                            rolesWithAccess.Add(role.Id);
                        }
                    }

                    var authorRoles = ((IGuildUser)Context.Message.Author).RoleIds;
                    foreach (ulong roleId in authorRoles)
                    {
                        if (rolesWithAccess.Contains(roleId))
                            return true;
                    }

                    Logger.LogInternal($"User '{Context.Message.Author.Username}' does not have Access to run Command");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(new LogMessage(LogSeverity.Error, "CheckAccess", "Unexpected Exception", ex));
            }
            return false;
        }

        private bool CheckCancelAccess()
        {
            try
            {
                if (Context.Guild.Name.Equals("Bit Heroes"))
                {
                    HashSet<ulong> rolesWithAccess = new HashSet<ulong>();
                    var roles = Context.Guild.Roles;
                    foreach (IRole role in roles)
                    {
                        if (role.Name.Contains("300+") || role.Name.Contains("Admin") || role.Name.StartsWith("Mod "))
                        {
                            rolesWithAccess.Add(role.Id);
                        }
                    }

                    var authorRoles = ((IGuildUser)Context.Message.Author).RoleIds;
                    foreach (ulong roleId in authorRoles)
                    {
                        if (rolesWithAccess.Contains(roleId))
                            return true;
                    }

                    Logger.LogInternal($"User '{Context.Message.Author.Username}' does not have Access to run Cancel Command");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(new LogMessage(LogSeverity.Error, "CheckCancelAccess", "Unexpected Exception", ex));
            }
            return false;
        }

        public async Task ShowGameHelp()
        {
            await ReplyAsync("```Markdown\r\n<!StartGame> - Starts a new game if one is not already running.\n"
                                + "You must provide the first parameter to run, all other parameters are optional, like so:\n"
                                + "StartGame 100 5 10 2\n"
                                + "Parameters are in order:\n"
                                + "<Max User that can play>\n"
                                + "<Max minutes to wait for players (Default: 5)>\n"
                                + "<Seconds to delay between displaying next day (Default: 10)>\n"
                                + "<Number of Winners (Default: 1)>```\r\n");
        }

        [Command("num1"), Summary("input")]
        public async Task PvP()
        {
            if (CheckAccess())
            {
                await ReplyAsync("Yo Num2 members, don't forget that there is a channel called #salt_mines");
            }
        }

        [Command("ShadownBot"), Summary("Funny message")]
        public async Task shadownBot()
        {
            Random rnd = new Random(Guid.NewGuid().GetHashCode());
            if (CheckAccess())
            {
                switch (rnd.Next(4))
                {
                    case 0:
                        await ReplyAsync("``` Beep boop... Shad made me! Wait... no! I made him >:D```");
                        break;
                    case 1:
                        await ReplyAsync("``` Stop Pestering me!!!```");
                        break;
                    case 2:
                        await ReplyAsync("``` Shad the weird freak keep on molesting me D:```");
                        break;
                    case 3:
                        await ReplyAsync("``` Do you have oil? *Hic* I really like it :3```");
                        break;
                    default:
                        break;
                }
            }
        }

        [Command("StartV2", RunMode = RunMode.Async), Summary("Start the Hunger Games V2")]
        public async Task StartV2( [Summary("Max User that can play")]string strMaxUsers,
            [Summary("Max minutes to wait for players")]string strMaxMinutesToWait = null,
            [Summary("Number of Winners")]string strNumWinners = null
            )
        {
            await StartIGameInterval(strMaxUsers, strMaxMinutesToWait, strNumWinners);
        }

        [Command("Help"), Summary("Shows Bot Help")]
        public async Task Help()
        {
            try
            {
                //Logger.LogInternal($"Command 'Help' executed by '{Context.Message.Author.Username}'");
                if (CheckAccess(true))
                {
                    await ReplyAsync("```Markdown\r\n<!CancelGame> - Cancels the current running game if there is one.\n"
                                     + "Only the user that started the game or an Admin/Mod can run this command.```\r\n");
                    await ReplyAsync("```Markdown\r\n<!CleanUp> - Deletes all the messages by this Bot in the last 100 messages.```\r\n");
                    await ShowGameHelp();
                }
            }
            catch (Exception ex)
            {
                await Logger.Log(new LogMessage(LogSeverity.Error, "CancelGame", "Unexpected Exception", ex));
            }
        }

        [Command("CancelGame"), Summary("Cancels the current running game.")]
        public async Task CancelGame()
        {
            try
            {
                //Logger.LogInternal($"Command 'CancelGame' executed by '{Context.Message.Author.Username}'");
                if (CheckAccess())
                {
                    string returnMessage = "No Game is currently running!";
                    bool hasCancelAccess = CheckCancelAccess();
                    lock (SyncObj)
                    {
                        RunningCommandInfo commandInfo = GetRunningCommandInfo(Context.Channel.Id);
                        if (commandInfo?.GameInstance != null)
                        {
                            if (Context.Message.Author.Id == commandInfo.UserId || hasCancelAccess)
                            {
                                RemoveChannelCommandInstance(Context.Channel.Id);
                                commandInfo.GameInstance.AbortGame();
                                returnMessage = "Cancelling current running game!";
                            }
                        }
                    }
                    await LogAndReplyAsync(returnMessage);
                }
            }
            catch (Exception ex)
            {
                await Logger.Log(new LogMessage(LogSeverity.Error, "CancelGame", "Unexpected Exception", ex));
            }
        }

        /*
                [RequireBotPermission(GuildPermission.ManageMessages)]
                [RequireUserPermission(GuildPermission.ManageMessages)]
        */
        [Command("cleanup", RunMode = RunMode.Async), Summary("Provides Help with commands.")]
        public async Task CleanUp()
        {
            bool cleanupCommandInstance = false;
            try
            {
                //Logger.LogInternal($"Command 'CleanUp' executed by '{Context.Message.Author.Username}'");
                if (CheckAccess())
                {
                    RunningCommandInfo commandInfo;
                    if (CreateChannelCommandInstance("CleanUp", Context.User.Id, Context.Channel.Id, Context.Guild.Id, null, out commandInfo))
                    {
                        cleanupCommandInstance = true;
                        var botUser = Context.Client.CurrentUser;
                        var result = await Context.Channel.GetMessagesAsync().Flatten();
                        List<IMessage> messagesToDelete = new List<IMessage>();
                        if (result != null)
                        {
                            foreach (var message in result)
                            {
                                if (message.Author.Id == botUser.Id)
                                {
                                    messagesToDelete.Add(message);
                                }
                            }
                            if (messagesToDelete.Count > 0)
                            {
                                await Context.Channel.DeleteMessagesAsync(messagesToDelete);
                                if (messagesToDelete.Count == 1)
                                {
                                    Logger.Log("Deleted 1 message.");
                                    //await LogAndReplyAsync("Deleted 1 message.");
                                }
                                else 
                                {
                                    Logger.Log($"Deleted {messagesToDelete.Count} messages.");
                                    //await LogAndReplyAsync($"Deleted {messagesToDelete.Count} messages.");
                                }
                                return;
                            }
                        }
                        Logger.Log("No messages to delete.");
                        //await LogAndReplyAsync("No messages to delete.");
                    }
                    else
                    {
                        try
                        {
                            await LogAndReplyAsync($"The '{commandInfo.CommandName}' command is currently running!.  Can't run this command until that finishes");
                        }
                        catch (Exception ex)
                        {
                            await Logger.Log(new LogMessage(LogSeverity.Error, "CleanUp", "Unexpected Exception", ex));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Logger.Log(new LogMessage(LogSeverity.Error, "CleanUp", "Unexpected Exception", ex));
            }
            finally
            {
                try
                {
                    if (cleanupCommandInstance)
                        RemoveChannelCommandInstance(Context.Channel.Id);
                }
                catch (Exception ex)
                {
                    await Logger.Log(new LogMessage(LogSeverity.Error, "CleanUp", "Unexpected Exception", ex));
                }
            }
        }

        [Command("startGame"), Summary("Starts the BHungerGames.")]
        public async Task StartGame()
        {
            //Logger.LogInternal($"Command 'startGame <Help>' executed by '{Context.Message.Author.Username}'");

            if (CheckAccess(true))
            {
                await ShowGameHelp();
            }
        }

        [Command("startGame", RunMode = RunMode.Async), Summary("Starts the BHungerGames.")]
        public async Task StartGame([Summary("Max User that can play")]string strMaxUsers,
            [Summary("Max minutes to wait for players")]string strMaxMinutesToWait = null,
            [Summary("Seconds to delay between displaying next day")]string strSecondsDelayBetweenDays = null,
            [Summary("Number of Winners")]string strNumWinners = null,
            [Remainder, Summary("Message to display")]string displayMessage = null)
        {
            await StartGameInternal(strMaxUsers, strMaxMinutesToWait, strSecondsDelayBetweenDays, strNumWinners, displayMessage, 0);
        }

        [Command("startGameT", RunMode = RunMode.Async), Summary("Starts the BHungerGames.")]
        public async Task StartGameT([Summary("Max User that can play")]string strMaxUsers,
            [Summary("Max minutes to wait for players")]string strMaxMinutesToWait = null,
            [Summary("Seconds to delay between displaying next day")]string strSecondsDelayBetweenDays = null,
            [Summary("Number of Winners")]string strNumWinners = null,
            [Remainder, Summary("Message to display")]string displayMessage = null)
        {
            await StartGameInternal(strMaxUsers, strMaxMinutesToWait, strSecondsDelayBetweenDays, strNumWinners, displayMessage, 300);
        }

        private async Task StartGameInternal(string strMaxUsers, string strMaxMinutesToWait, string strSecondsDelayBetweenDays, string strNumWinners, string displayMessage, int testUsers)
        {
            bool cleanupCommandInstance = false;
            try
            {
                Logger.LogInternal("Command " + (testUsers > 0 ? "T" : "") + $"'startGame' executed by '{Context.Message.Author.Username}'");

                if (CheckAccess())
                {
                    BotGameInstance gameInstance = new BotGameInstance();
                    RunningCommandInfo commandInfo;
                    if (CreateChannelCommandInstance("StartGame", Context.User.Id, Context.Channel.Id, Context.Guild.Id, gameInstance, out commandInfo))
                    {
                        cleanupCommandInstance = true;
                        int maxUsers;
                        int maxMinutesToWait;
                        int secondsDelayBetweenDays;
                        int numWinners;

                        if (Int32.TryParse(strMaxUsers, out maxUsers) == false) maxUsers = 100;
                        if (Int32.TryParse(strMaxMinutesToWait, out maxMinutesToWait) == false) maxMinutesToWait = 5;
                        if (Int32.TryParse(strSecondsDelayBetweenDays, out secondsDelayBetweenDays) == false) secondsDelayBetweenDays = 10;
                        if (Int32.TryParse(strNumWinners, out numWinners) == false) numWinners = 1;
                        if (numWinners <= 0) numWinners = 1;
                        if (maxMinutesToWait <= 0) maxMinutesToWait = 1;
                        if (secondsDelayBetweenDays <= 0) secondsDelayBetweenDays = 5;
                        if (maxUsers <= 0) maxUsers = 1;


                        SocketGuildUser user = Context.Message.Author as SocketGuildUser;
                        string userThatStartedGame = user?.Nickname ?? Context.Message.Author.Username;
                        gameInstance.StartGame(numWinners, maxUsers, maxMinutesToWait, secondsDelayBetweenDays, Context.Channel, userThatStartedGame, testUsers);
                        cleanupCommandInstance = false;
                       //await Context.Channel.SendMessageAsync($"MaxUsers: {maxUsers}  MaxMinutesToWait: {maxMinutesToWait} SecondsDelayBetweenDays: {secondsDelayBetweenDays} NumWinners: {numWinners}");
                    }
                    else
                    {
                        try
                        {
                            await LogAndReplyAsync($"The '{commandInfo.CommandName}' command is currently running!.  Can't run this command until that finishes");
                        }
                        catch (Exception ex)
                        {
                            await Logger.Log(new LogMessage(LogSeverity.Error, "StartGameInternal", "Unexpected Exception", ex));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Logger.Log(new LogMessage(LogSeverity.Error, "StartGame", "Unexpected Exception", ex));
            }
            finally
            {
                try
                {
                    if (cleanupCommandInstance)
                        RemoveChannelCommandInstance(Context.Channel.Id);
                }
                catch (Exception ex)
                {
                    await Logger.Log(new LogMessage(LogSeverity.Error, "StartGame", "Unexpected Exception in Finally", ex));
                }
            }
        }

        private async Task StartIGameInterval(string strMaxUsers, string strMaxMinutesToWait, string strNumWinners)
        {
            bool cleanupCommandInstance = false;
            try
            {
                if (CheckAccess())
                {
                    BotGameInstance gameInstance = new BotGameInstance();
                    RunningCommandInfo commandInfo;
                    if (CreateChannelCommandInstance("StartV2", Context.User.Id, Context.Channel.Id, Context.Guild.Id, gameInstance, out commandInfo))
                    {
                        cleanupCommandInstance = true;
                        int maxUsers;
                        int maxMinutesToWait;
                        //int secondsDelayBetweenDays;
                        int numWinners;

                        if (Int32.TryParse(strMaxUsers, out maxUsers) == false) maxUsers = 100;
                        if (Int32.TryParse(strMaxMinutesToWait, out maxMinutesToWait) == false) maxMinutesToWait = 5;

                        if (Int32.TryParse(strNumWinners, out numWinners) == false) numWinners = 1;
                        if (numWinners <= 0) numWinners = 1;
                        if (maxMinutesToWait <= 0) maxMinutesToWait = 1;

                        if (maxUsers <= 0) maxUsers = 1;


                        SocketGuildUser user = Context.Message.Author as SocketGuildUser;
                        string userThatStartedGame = user?.Nickname ?? Context.Message.Author.Username;
                        gameInstance.StartIGame(numWinners, maxUsers, maxMinutesToWait, Context.Channel, userThatStartedGame);
                        cleanupCommandInstance = false;
                    }
                    else
                    {
                        try
                        {
                            await LogAndReplyAsync($"The '{commandInfo.CommandName}' command is currently running!.  Can't run this command until that finishes");
                        }
                        catch (Exception ex)
                        {
                            await Logger.Log(new LogMessage(LogSeverity.Error, "StartIGameInternal", "Unexpected Exception", ex));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await Logger.Log(new LogMessage(LogSeverity.Error, "StartV2", "Unexpected Exception", ex));
            }
            finally
            {
                try
                {
                    if (cleanupCommandInstance)
                        RemoveChannelCommandInstance(Context.Channel.Id);
                }
                catch (Exception ex)
                {
                    await Logger.Log(new LogMessage(LogSeverity.Error, "StartV2", "Unexpected Exception in Finally", ex));
                }
            }
        }
    }
}
