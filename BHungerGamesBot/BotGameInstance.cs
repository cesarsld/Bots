using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace BHungerGaemsBot
{
    public class BotGameInstance
    {
        public const string Smiley = "😃"; // :smiley:
        public const string Smile = "😄"; // :smile:
        public const string ReactionToUse = Smiley;
        public const string ReactionToUseText = ":smiley:(Smiley)";

        private readonly object _syncObj = new object();
        private Dictionary<string,string> _playersNickNameLookup;
        private bool _cancelGame;

        private ulong _messageId;
        private IMessageChannel _channel;
        private int _testUsers;

        public void LogAndReply(string message)
        {
            Logger.LogInternal(message);
            _channel.SendMessageAsync(message);
        }

        public void LogAndReplyError(string message, string method)
        {
            Logger.Log(new LogMessage(LogSeverity.Error, method, message));
            _channel.SendMessageAsync(message);
        }

        private Task HandleReactionAdded(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction)
        {
            try
            {
                if (reaction != null && reaction.User.IsSpecified) // for now except all reactions && reaction.Emote.Name == ReactionToUse)
                {
                    SocketGuildUser user = reaction.User.Value as SocketGuildUser;
                    string playerUserName = reaction.User.Value.Username;
                    string playerName = user?.Nickname ?? playerUserName;
                    lock (_syncObj)
                    {
                        if (msg.Value?.Id == _messageId)
                            _playersNickNameLookup[playerName] = playerUserName;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(new LogMessage(LogSeverity.Error, "HandleReactionAdded", "Unexpected Exception", ex));
            }
            return Task.CompletedTask;
        }
/*
        private Task HandleReactionRemoved(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction)
        {
            try
            {
                if (reaction != null && reaction.User.IsSpecified)
                {
                    SocketGuildUser user = reaction.User.Value as SocketGuildUser;
                    string playerName = user?.Nickname ?? reaction.User.Value.Username;
                    lock (_syncObj)
                    {
                        if (msg.Value?.Id == _messageId)
                        {
                            _playersNickNameLookup.Remove(playerName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(new LogMessage(LogSeverity.Error, "HandleReactionRemoved", "Unexpected Exception", ex));
            }
            return Task.CompletedTask;
        }
*/
        public void AbortGame()
        {
            lock (_syncObj)
            {
                _cancelGame = true;
            }
        }

        private bool GetCancelGame()
        {
            lock (_syncObj)
            {
                return _cancelGame;
            }
        }

        public bool StartGame(int numWinners, int maxUsers, int maxMinutesToWait, int secondsDelayBetweenDays, IMessageChannel channel, string userName, int testUsers)
        {
            _testUsers = testUsers;

            Task.Run(() => RunGame(numWinners, maxUsers, maxMinutesToWait, secondsDelayBetweenDays, channel, userName));
            return true;
        }

        private void CheckReactionUsers(IUserMessage gameMessage, Dictionary<string, string> newPlayersNickNameLookup)
        {
            int eventReactionsCount = newPlayersNickNameLookup.Count;
            int badGetReactions = 0;
            int addedGetReactions = 0;
            int existingGetReactions = 0;
            int getReactionsCount = 0;
            var result = gameMessage.GetReactionUsersAsync(ReactionToUse, DiscordConfig.MaxUsersPerBatch).GetAwaiter().GetResult();
            if (result != null)
            {
                Dictionary<string, string> playersUserNameLookup = new Dictionary<string, string>();
                foreach (KeyValuePair<string, string> keyValuePair in newPlayersNickNameLookup)
                {
                    playersUserNameLookup[keyValuePair.Value] = keyValuePair.Key;
                }

                getReactionsCount = result.Count;
                foreach (IUser user in result)
                {
                    if (playersUserNameLookup.ContainsKey(user.Username) == false)
                    {
                        SocketGuildUser userLookup = _channel.GetUserAsync(user.Id).GetAwaiter().GetResult() as SocketGuildUser;
                        if (userLookup != null)
                        {
                            newPlayersNickNameLookup[userLookup.Nickname ?? userLookup.Username] = userLookup.Username;
                            addedGetReactions++;
                        }
                        else
                        {
                            badGetReactions++;
                        }
                    }
                    else
                    {
                        existingGetReactions++;
                    }
                }
            }
            Logger.Log($"RunGame - GetReactionsReturned: {getReactionsCount} EventReactions: {eventReactionsCount} BadUsers: {badGetReactions} AddedUsers: {addedGetReactions} ExistingUsers: {existingGetReactions} TotalPlayers: {newPlayersNickNameLookup.Count}");
        }

        private void RunGame(int numWinners, int maxUsers, int maxMinutesToWait, int secondsDelayBetweenDays, IMessageChannel channel, string userName)
        {
            bool removeHandler = false;
            try
            {
                _channel = channel;

                string gameMessageText = $"Preparing to start a Game for ```Markdown\r\n<{userName}> in {maxMinutesToWait} minutes or when we get {maxUsers} reactions!```\r\n"
                    + $"React to this message with the {ReactionToUseText} emoji to enter!  Multiple Reactions(emojis) will NOT enter you more than once.\r\nPlayer entered: ";
                Task<IUserMessage> messageTask = channel.SendMessageAsync(gameMessageText + "0");
                messageTask.Wait();
                if (messageTask.IsFaulted)
                {
                    LogAndReplyError("Error getting players.", "RunGame");
                    return;
                }
                var gameMessage = messageTask.Result;
                if (gameMessage == null)
                {
                    LogAndReplyError("Error accessing Game Message.", "RunGame");
                    return;
                }
                lock (_syncObj)
                {
                    _playersNickNameLookup = new Dictionary<string, string>();
                    _messageId = gameMessage.Id;
                }

                Bot.DiscordClient.ReactionAdded += HandleReactionAdded;
                //Bot.DiscordClient.ReactionRemoved += HandleReactionRemoved;
                removeHandler = true;

                Dictionary<string, string> newPlayersNickNameLookup;
                DateTime now = DateTime.Now;
                int secondCounter = 0;
                int lastPlayerCount = 0;
                while (true)
                {
                    int currentPlayerCount;
                    lock (_syncObj)
                    {
                        if (_cancelGame)
                            return;
                        currentPlayerCount = _playersNickNameLookup.Count;
                        if (currentPlayerCount >= maxUsers)
                        {
                            _messageId = 0;
                            newPlayersNickNameLookup = new Dictionary<string, string>(_playersNickNameLookup);
                            break;
                        }
                    }
                    if (secondCounter > 10)
                    {
                        secondCounter = 0;
                        if (currentPlayerCount != lastPlayerCount)
                        {
                            lastPlayerCount = currentPlayerCount;
                            gameMessage.ModifyAsync(x => x.Content = gameMessageText + currentPlayerCount); // + "```\r\n");
                        }
                    }

                    Thread.Sleep(1000);
                    secondCounter++;
                    if ((DateTime.Now - now).Minutes >= maxMinutesToWait)
                    {
                        lock (_syncObj)
                        {
                            if (_cancelGame)
                                return;
                            _messageId = 0;
                            newPlayersNickNameLookup = new Dictionary<string, string>(_playersNickNameLookup);
                        }
                        break;
                    }
                }

                Bot.DiscordClient.ReactionAdded -= HandleReactionAdded;
                //Bot.DiscordClient.ReactionRemoved -= HandleReactionRemoved;
                removeHandler = false;

                CheckReactionUsers(gameMessage, newPlayersNickNameLookup);
                // for now we don't use this anymore so don't update it.
                //lock (_syncObj)
                //{
                //    _playersNickNameLookup = new Dictionary<string, string>(newPlayersNickNameLookup);
                //}

                List<string> players;
                if (_testUsers > 0)
                {
                    players = new List<string>(_testUsers);
                    for (int i = 0; i < _testUsers; i++)
                        players.Add("P" + i);
                }
                else
                {
                    players = new List<string>(newPlayersNickNameLookup.Keys);
                    if (players.Count < 1)
                    {
                        LogAndReplyError("Error, no players reacted.", "RunGame");
                        return;
                    }
                }

                if (lastPlayerCount != players.Count)
                {
                    gameMessage.ModifyAsync(x => x.Content = gameMessageText + players.Count); //  + "```\r\n"
                }

                new BHungerGames().Run(numWinners, secondsDelayBetweenDays, players, LogToChannel, GetCancelGame);
            }
            catch (Exception ex)
            {
                Logger.Log(new LogMessage(LogSeverity.Error, "RunGame", "Unexpected Exception", ex));
                try
                {
                    channel.SendMessageAsync("Error getting players.");
                }
                catch (Exception ex2)
                {
                    Logger.Log(new LogMessage(LogSeverity.Error, "RunGame", "Unexpected Exception Sending Error to Discord", ex2));
                }
            }
            finally
            {
                try
                {
                    if (removeHandler)
                    {
                        Bot.DiscordClient.ReactionAdded -= HandleReactionAdded;
                        //Bot.DiscordClient.ReactionRemoved -= HandleReactionRemoved;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(new LogMessage(LogSeverity.Error, "RunGame", "Unexpected Exception In Finally", ex));
                }
                try
                {
                    BaseCommands.RemoveChannelCommandInstance(channel.Id);
                }
                catch (Exception ex)
                {
                    Logger.Log(new LogMessage(LogSeverity.Error, "RunGame", "Unexpected Exception In Finally2", ex));
                }
                try
                {
                    string cancelMessage = null;
                    lock (_syncObj)
                    {
                        if (_cancelGame)
                        {
                            cancelMessage = "GAME CANCELLED!!!";
                        }
                    }
                    if (cancelMessage != null)
                    {
                        LogAndReply(cancelMessage);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(new LogMessage(LogSeverity.Error, "RunGame", "Unexpected Exception In Finally3", ex));
                }
            }
        }

        private void LogToChannel(string msg)
        {
            _channel.SendMessageAsync("```Markdown\r\n" + msg + "```\r\n").GetAwaiter().GetResult();
        }
    }
}
