﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace BHungerGaemsBot
{
    class BotV3GameInstance : BotGameInstance
    {
        private BHungerGamesV3 _gameInstance;

        protected override Player CreatePlayer(IUser user)
        {
            return new PlayerRPG(user);
        }

        public Task FetchPlayerInput(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction)
        {
            try
            {
                if (reaction != null && reaction.User.IsSpecified) // for now except all reactions && reaction.Emote.Name == ReactionToUse)
                {
                    lock (SyncObj)
                    {
                        if (msg.Value?.Id == MessageId)
                        {
                            _gameInstance.HandlePlayerInput(reaction.UserId, reaction.Emote.Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(new LogMessage(LogSeverity.Error, "FethPlayerInput", "Unexpected Exception", ex));
            }
            return Task.CompletedTask;
        }

        protected override string GetRunGameMessage(string bhgRoleMention, string userName, int maxUsers, int maxMinutesToWait, bool startWhenMaxUsers)
        {
            return $"{bhgRoleMention} Preparing to start a Bit Heroes BattleGround Game for ```Markdown\r\n<{userName}> in {maxMinutesToWait} minutes"
                   + (startWhenMaxUsers ? $" or when we get {maxUsers} reactions!" : $"!  At the start of the game the # of players will be reduced to {maxUsers} if needed.") + "```\r\n"
                   + "React to this message with any emoji to enter!  Multiple Reactions(emojis) will NOT enter you more than once.\r\nPlayer entered: ";
        }

        protected override void RunGameInternal(int numWinners, int secondsDelayBetweenDays, List<Player> players, int maxPlayers = 0)
        {
            Bot.DiscordClient.ReactionAdded += FetchPlayerInput;
            try
            {
                _gameInstance = new BHungerGamesV3();
                _gameInstance.Run(numWinners, players, LogToChannel, GetCancelGame, maxPlayers);

            }
            finally
            {
                Bot.DiscordClient.ReactionAdded -= FetchPlayerInput;
            }
        }


    }
}
