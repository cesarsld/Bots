﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading;

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

        private bool CheckAccessForBitHeroesGuildOnly(bool checkCancelAccess = false)
        {
            HashSet<ulong> rolesWithAccess = new HashSet<ulong>();
            var roles = Context.Guild.Roles;
            if (Context.Guild.Name.Equals("Bit Heroes"))
            {
                foreach (IRole role in roles)
                {
                    if ((checkCancelAccess == false && role.Name.Contains("Level 2"))
                        || role.Name.Contains("300+") || role.Name.Contains("400") || role.Name.Contains("Admin") || role.Name.StartsWith("Mod "))
                    {
                        rolesWithAccess.Add(role.Id);
                    }
                }
            }
            else
            { // default
                return false;
            }

            var authorRoles = ((IGuildUser)Context.Message.Author).RoleIds;
            foreach (ulong roleId in authorRoles)
            {
                if (rolesWithAccess.Contains(roleId))
                    return true;
            }

            StringBuilder sb = new StringBuilder();
            if (checkCancelAccess)
                sb.AppendLine($"Guild: '{Context.Guild.Name}' User: '{Context.Message.Author.Username}' does not have Access to run Cancel Command");
            else
                sb.AppendLine($"Guild: '{Context.Guild.Name}' User: '{Context.Message.Author.Username}' does not have Access to run Command");

            sb.AppendLine("Guild Roles:");
            foreach (IRole role in roles)
            {
                sb.AppendLine($"{role.Name} -- {role.Id}");
            }

            sb.Append("User Roles: ");
            foreach (ulong id in authorRoles)
            {
                sb.AppendLine(id + ",");
            }

            Logger.LogInternal(sb.ToString());
            return false;
        }

        private bool CheckAccessByGuild(bool checkCancelAccess = false)
        {
            HashSet<ulong> rolesWithAccess = new HashSet<ulong>();
            var roles = Context.Guild.Roles;
            if (Context.Guild.Name.Equals("Bit Heroes"))
            {
                foreach (IRole role in roles)
                {
                    if ((checkCancelAccess == false && role.Name.Contains("Level 6"))
                        || role.Name.Contains("300+") || role.Name.Contains("400") || role.Name.Contains("Admin") || role.Name.StartsWith("Mod ") || Context.User.Username.Contains("Owl of") || role.Name.Contains("[K]"))
                    {
                        rolesWithAccess.Add(role.Id);
                    }
                }
            }
            else if (Context.Guild.Name.Equals("Bit Heroes - FR") || Context.Guild.Name.Equals("Crash Test Server"))
            {
                // 150-199  ,  200-249 ,  250+ ,  Admin , Modérateur
                foreach (IRole role in roles)
                {
                    if ((checkCancelAccess == false && (role.Name.Contains("150") || role.Name.Contains("200")))
                        || role.Name.Contains("Admin") || role.Name.StartsWith("Mod"))
                    {
                        rolesWithAccess.Add(role.Id);
                    }
                }
            }
            else
            { // default
                if (checkCancelAccess == false)
                    return true;

                foreach (IRole role in roles)
                {
                    if (role.Name.Contains("Admin") || role.Name.StartsWith("Mod"))
                    {
                        rolesWithAccess.Add(role.Id);
                    }
                }
            }

            var authorRoles = ((IGuildUser)Context.Message.Author).RoleIds;
            foreach (ulong roleId in authorRoles)
            {
                if (rolesWithAccess.Contains(roleId))
                    return true;
            }

            StringBuilder sb = new StringBuilder();
            if (checkCancelAccess)
                sb.AppendLine($"Guild: '{Context.Guild.Name}' User: '{Context.Message.Author.Username}' does not have Access to run Cancel Command");
            else
                sb.AppendLine($"Guild: '{Context.Guild.Name}' User: '{Context.Message.Author.Username}' does not have Access to run Command");

            sb.AppendLine("Guild Roles:");
            foreach (IRole role in roles)
            {
                sb.AppendLine($"{role.Name} -- {role.Id}");
            }

            sb.Append("User Roles: ");
            foreach (ulong id in authorRoles)
            {
                sb.AppendLine(id + ",");
            }

            Logger.LogInternal(sb.ToString());
            return false;
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
                return CheckAccessByGuild();
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
                return CheckAccessByGuild(true);
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

        public async Task ShowGameHelpV2()
        {
            await ReplyAsync("```Markdown\r\n<!StartV2> - Starts a new game if one is not already running.\n"
                                + "You must provide the first parameter to run, all other parameters are optional, like so:\n"
                                + "StartV2 100 5 2\n"
                                + "Parameters are in order:\n"
                                + "<Max User that can play>\n"
                                + "<Max minutes to wait for players (Default: 5)>\n"
                                + "<Number of Winners (Default: 1)>\n"
                                + "For more information on how to play, type the command: <!V2Rules> ```\r\n");
        }

        [Command("V2Rules"), Summary("Show HG2 rules")]
        public async Task Helpv2()
        {
            try
            {
                if (CheckAccess(true))
                {
                    await ReplyAsync("__**Interactive HGv2**__\n"
                        + "__**Objective**__: Be the last survivor.\n" 
                        + "__**Initial Options**__\n"
                        + "__Loot__ ( :moneybag: ) = You have a chance to obtain gear (weapon/offhand/armor/helmet). Gear increases your chance to win duels. Weapon is destroyed after 5 duels.\n"
                        + "  •    Safe = No Damage = Low Quality Loot\n"
                        + "  •    Unsafe = Small chance for minor Damage = Medium Quality Loot\n"
                        + "  •    Dangerous= Medium chance for heavy Damage = High Quality Loot\n"
                        + "  •    Deadly = High chance for extreme Damage = Very High Quality Loot\n"
                        + "__Capture Familiars__ ( " + Emote.Parse("<:blubber:244666398738087936>") + " you need an emote with the name : blubber :)= You have the chance to obtain a familiar that will increase your duel chance + small chance that it deals damage on another survivor. As for loot, you will have danger levels. Higher danger level = higher risk to fail = higher quality of familiar.\n"
                        + "  •    Safe = No Damage = Low Quality Familiar\n"
                        + "  •    Unsafe = Small chance for minor Damage = Medium Quality Familiar\n"
                        + "  •    Dangerous_= Medium chance for heavy Damage = High Quality Familiar\n"
                        + "  •    Deadly = High chance for extreme Damage = Very High Quality Familiar\n"
                        + "__Stay Alert__ (:exclamation:) = +10% chance for avoiding scenario's. Less chance of success if used in succession.\n"
                        + "__Duel Immunity__ (:crossed_swords:) = Prevents you from being targeted for a duel. Cannot be used in succession; has a 5 day cooldown.\n"
                        + "__Do Nothing__ = Occurs when you do not input a command.\n"
                        );
                    await ReplyAsync("__**Enhanced Decisions**__\n"
                        + "Players will be randomly selected to perform an additional action after each round.\n"
                        + "  •    Sabotage  (:wrench:)  = Chance to apply a debuff to another player\n"
                        + "  •    Steal (:gun:)  = Chance to steal an opponents item. Will only steal items greater than what you currently have\n"
                        + "  •    Make a Trap (:bomb:) = Chance to create a trap that will target enemy players and apply immediate damage\n"
                        + "__**Game Effects:**__\n"
                        + "__Crowd Decisions__ = every 3 days a public crowd poll will be executed to modify the game\n"
                        + "__Terra_forming__ = every 4 days the arena changes to obtain different advantages and disadvantages.\n"
                        + "__Scenarios__ = Every day will have a chance for a scenario. Each scenario will either heal you (small chance), instantly kill you (small chance) or damage you (common)\n"
                        + "__Duel__ = Start after 4 days. Every day a random duel between people will start. This is not influenced by Stay Alert\n"
                        + "__Debuffs__ = Negative effects incurred by sabotage\n"
                        + "  •    Reduced Item Find\n"
                        + "  •    Increased Scenario Likelihood\n"
                        + "  •    Increased Duel Chance\n"
                        );
                }
            }
            catch (Exception ex)
            {
                await Logger.Log(new LogMessage(LogSeverity.Error, "CancelGame", "Unexpected Exception", ex));
            }
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
                    await ShowGameHelpV2();
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
        

        [Command("startv2"), Summary("input")]
        public async Task PvP()
        {
            if (CheckAccess())
            {
                Thread.Sleep(5000);
                await ReplyAsync("```md\n * FATAL ERROR CODE 104 *:\nBHungerGamesV2.cs could not be found.```");
                Thread.Sleep(5000);
                await ReplyAsync("```Have SSS1 and Shadown88 bamboozled all of you? Could they have pretended to create a great game but only brought memes?```");
                Thread.Sleep(8000);
                await ReplyAsync("```jk.... it's the wrong command bros... hehe\n\nBot humour, get it? alright, alright, enjoy the event. *beep boop*```");
               
            }
        }

        [Command("startGame", RunMode = RunMode.Async), Summary("Starts the BHungerGames.")]
        public async Task StartGame([Summary("Max User that can play")]string strMaxUsers,
            [Summary("Max minutes to wait for players")]string strMaxMinutesToWait = null,
            [Summary("Seconds to delay between displaying next day")]string strSecondsDelayBetweenDays = null,
            [Summary("Number of Winners")]string strNumWinners = null)
        {
            BotGameInstance gameInstance = new BotGameInstance();
            await StartGameInternal(gameInstance, strMaxUsers, strMaxMinutesToWait, strSecondsDelayBetweenDays, strNumWinners, 0);
        }

        [Command("startGameT", RunMode = RunMode.Async), Summary("Starts the BHungerGames.")]
        public async Task StartGameT([Summary("Max User that can play")]string strMaxUsers,
            [Summary("Max minutes to wait for players")]string strMaxMinutesToWait = null,
            [Summary("Seconds to delay between displaying next day")]string strSecondsDelayBetweenDays = null,
            [Summary("Number of Winners")]string strNumWinners = null)
        {
            BotGameInstance gameInstance = new BotGameInstance();
            await StartGameInternal(gameInstance, strMaxUsers, strMaxMinutesToWait, strSecondsDelayBetweenDays, strNumWinners, 300);
        }

        [Command("StartV2", RunMode = RunMode.Async), Summary("Start the Hunger Games V2")]
        public async Task StartV2([Summary("Max User that can play")]string strMaxUsers,
            [Summary("Max minutes to wait for players")]string strMaxMinutesToWait = null,
            [Summary("Number of Winners")]string strNumWinners = null
        )
        {
           // if (CheckAccessForBitHeroesGuildOnly())
           // {
                BotV2GameInstance gameInstance = new BotV2GameInstance();
                await StartGameInternal(gameInstance, strMaxUsers, strMaxMinutesToWait, "1", strNumWinners, 0);
            //}
        }

        private async Task StartGameInternal(BotGameInstance gameInstance, string strMaxUsers, string strMaxMinutesToWait, string strSecondsDelayBetweenDays, string strNumWinners, int testUsers)
        {
            bool cleanupCommandInstance = false;
            try
            {
                Logger.LogInternal($"G:{Context.Guild.Name}  Command " + (testUsers > 0 ? "T" : "") + $"'startGame' executed by '{Context.Message.Author.Username}'");

                if (CheckAccess())
                {
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
                        gameInstance.StartGame(numWinners, maxUsers, maxMinutesToWait, secondsDelayBetweenDays, Context, userThatStartedGame, testUsers);
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
    }
}
