using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Linq;
using Discord;

namespace BHungerGaemsBot
{
    class BHungerGamesV3
    {
        private volatile bool _ignoreReactions;

        public const int NumItemTypes = 4;

        private const int DelayValue = 1;

        private static readonly TimeSpan DelayBetweenCycles;
        private static readonly TimeSpan DelayAfterOptions;
        private static readonly int[] ShowPlayersWhenCountEqual;
        private static readonly ReadOnlyCollection<LootLevel> LootLevels;
        private static readonly ReadOnlyCollection<IEmote> EmojiClassListOptions;
        private static readonly ReadOnlyCollection<IEmote> EmojiAdventureListOption;
        private static readonly ReadOnlyCollection<IEmote> EmojiListCrowdDecision;
        //private static readonly Scenario[] Scenarios;
        private static readonly List<ulong> BannedPlayers;

        private readonly Random _random;
        private readonly HashSet<PlayerRPG> _enchancedPlayers;
        private readonly HashSet<PlayerRPG> _duelImmune;
        private List<PlayerRPG> _players;
        private bool _enhancedOptions;
        private bool _crowdOptions;
        private bool _classInit;
        private HeroClass _adventureAffinity;
        public static Dictionary<HeroClass, List<float>> HeroScalingDictionary;
        public static Dictionary<HeroClass, List<int>> HeroBaseStatsDictionary;
        public static Dictionary<HeroClass, List<int>> HeroLevelUpDictionary;
        private int _reactionA;
        private int _reactionB;

        public class LootLevel
        {
            public readonly LootQuality Quality;
            //public readonly int FailChance;
            public readonly int CommonChance;
            public readonly int RareChance;
            public readonly int EpicChance;
            public readonly int LegendaryChance;
            public readonly int SetChance;

            public LootLevel(LootQuality quality, int commonChance, int rareChance, int epicChance, int legendaryChance, int setChance)
            {
                Quality = quality;
                //FailChance = failChance;
                CommonChance = commonChance;
                RareChance = rareChance;
                EpicChance = epicChance;
                LegendaryChance = legendaryChance;
                SetChance = setChance;
            }

            public Rarity GetRarity(int value)
            {
                if (value < CommonChance) return Rarity.Common;
                if (value < RareChance) return Rarity.Rare;
                if (value < EpicChance) return Rarity.Epic;
                if (value < LegendaryChance) return Rarity.Legendary;
                if (value < SetChance) return Rarity.Set;
                return Rarity.None;
            }
        }

        static BHungerGamesV3()
        {
            DelayBetweenCycles = new TimeSpan(0, 0, 0, 10);
            DelayAfterOptions = new TimeSpan(0, 0, 0, 10);

            LootLevels = new ReadOnlyCollection<LootLevel>(new List<LootLevel>()
            {
                new LootLevel(LootQuality.None, 0, 0, 0, 0, 0),
                new LootLevel(LootQuality.Micro, 80, 92, 97, 100, 105),
                new LootLevel(LootQuality.Low, 57, 82, 94, 98, 100),
                new LootLevel(LootQuality.Medium, 40, 70, 85, 95, 100),
                new LootLevel(LootQuality.High, 0, 40, 70, 90, 100),
                new LootLevel(LootQuality.Extreme, 0, 40, 70, 90, 100)
            });

            ShowPlayersWhenCountEqual = new[] { 20, 10, 5, 2, 0 };
            EmojiClassListOptions = new ReadOnlyCollection<IEmote>(new List<IEmote> { new Emoji("🇦"), new Emoji("🇧"), new Emoji("🇨"), new Emoji("🇩"), new Emoji("🇪"), new Emoji("🇫"), new Emoji("🇬") });
            EmojiAdventureListOption = new ReadOnlyCollection<IEmote>(new List<IEmote> { new Emoji("💡"), new Emoji("⚔"), new Emoji("💰"), new Emoji("💪") });
            EmojiListCrowdDecision = new ReadOnlyCollection<IEmote>(new List<IEmote> { new Emoji("🅰"), new Emoji("🅱") });

            BannedPlayers = new List<ulong>
            {
            };

            HeroScalingDictionary = new Dictionary<HeroClass, List<float>>()
            {                                           //  STA    STR    AGI    INT   DEX    WIS  LCK
                {HeroClass.Mage,         new List<float>{ 0.75f,  0.5f,  1.2f,  2.5f,   1f,  1.5f,  1f } },
                {HeroClass.Knight,       new List<float>{    2f,  2.5f,    1f,  0.3f, 0.6f, 0.25f,  1f } },
                {HeroClass.Archer,       new List<float>{    1f,    1f,  2.5f,  0.5f,   2f,  0.5f,  1f } },
                {HeroClass.Monk,         new List<float>{ 0.25f, 0.25f, 0.25f,  2.5f, 1.5f,  2.5f,  1f } },
                {HeroClass.Necromancer,  new List<float>{ 0.75f, 0.75f, 0.5f,     3f,   1f,    2f,  1f } },
                {HeroClass.Assassin,     new List<float>{   1f,   1.5f, 2.5f,  0.25f,   2f, 0.25f,  1f } },
                {HeroClass.Elementalist, new List<float>{   1f,     1f,   1f,   2.5f, 2.5f,    1f,  1f } },
            };

            HeroBaseStatsDictionary = new Dictionary<HeroClass, List<int>>()
            {                                        //  STA    STR    AGI    INT   DEX    WIS  LCK
                {HeroClass.Mage,         new List<int>{   15,    15,    10,    60,   25,    45,  15 } }, //170
                {HeroClass.Knight,       new List<int>{   50,    60,    15,    10,   20,    15,  15 } }, //170
                {HeroClass.Archer,       new List<int>{   20,    25,    50,    15,   50,    10,  15 } }, //170
                {HeroClass.Monk,         new List<int>{   10,    10,    10,    50,   30,    60,  15 } }, //170
                {HeroClass.Necromancer,  new List<int>{   20,    20,    10,    60,   20,    40,  15 } }, //170
                {HeroClass.Assassin,     new List<int>{   20,    35,    50,     5,   55,     5,  15 } }, //170
                {HeroClass.Elementalist, new List<int>{   20,    15,    25,    45,   50,    15,  15 } }, //170
            };

            HeroLevelUpDictionary = new Dictionary<HeroClass, List<int>>()
            {                                        //  STA    STR    AGI    INT   DEX    WIS  LCK
                {HeroClass.Mage,         new List<int>{    3,     0,     3,    15,    4,     8,   3 } },
                {HeroClass.Knight,       new List<int>{   10,    11,     5,     2,    3,     2,   3 } },
                {HeroClass.Archer,       new List<int>{    3,     4,    11,     2,   11,     2,   3 } },
                {HeroClass.Monk,         new List<int>{    0,     2,     3,    10,    4,    13,   3 } },
                {HeroClass.Necromancer,  new List<int>{    3,     2,     2,    12,    4,    10,   3 } },
                {HeroClass.Assassin,     new List<int>{    2,     9,     8,     3,   9,      2,   3 } },
                {HeroClass.Elementalist, new List<int>{    3,     4,     6,     8,    9,     3,   3 } },
            };
        }

        public BHungerGamesV3()
        {
            _random = new Random();
            _duelImmune = new HashSet<PlayerRPG>();
            _ignoreReactions = true;
        }

        public void Run(int numWinners, List<Player> contestantsTransfer, BotGameInstance.ShowMessageDelegate showMessageDelegate, Func<bool> cannelGame, int maxPlayers = 0)
        {
            int day = 0;
            int playerNumberinLeaderboard = 20;
            int showPlayersWhenCountEqualIndex = 0;
            int duelCooldown = 4;
            bool crowdExtraDuel = false;
            
            bool bonusItemFind = false;
            
            
            

            StringBuilder sb = new StringBuilder(2000);
            StringBuilder sbLoot = new StringBuilder(2000);
            StringBuilder sbFamLoot = new StringBuilder(2000);
            //_traps = new List<Trap>();
            //List<Trap> trapsToBeRemoved = new List<Trap>();
            List<PlayerRPG> playersToBeRemoved = new List<PlayerRPG>();
            List<Player> bannedPlayersToRemove = new List<Player>();

            Logger.LogInternal("V3 Game started, total ppl: " + contestantsTransfer.Count);

            foreach (Player player in contestantsTransfer)
            {
                if (BannedPlayers.Contains(player.UserId))
                {
                    bannedPlayersToRemove.Add(player);
                }
            }
            contestantsTransfer = contestantsTransfer.Except(bannedPlayersToRemove).ToList();
            if (bannedPlayersToRemove.Count > 0)
            {
                showMessageDelegate($"Number of banned players attempting to join game:{bannedPlayersToRemove.Count}\r\n");
            }
            if (maxPlayers > 0 && contestantsTransfer.Count > maxPlayers)
            {
                int numToRemove = contestantsTransfer.Count - maxPlayers;
                for (int i = 0; i < numToRemove; i++)
                {
                    int randIndex = _random.Next(contestantsTransfer.Count);
                    //sb.Append($"<{contestantsTransfer[randIndex].ContestantName}>\t");
                    contestantsTransfer.RemoveAt(randIndex);
                }
                //showMessageDelegate("Players killed in the stampede trying to get to the arena:\r\n" + sb);
                //sb.Clear();
            }

            _players = new List<PlayerRPG>();
            foreach (Player player in contestantsTransfer)
            {
                PlayerRPG interactivePlayer = player as PlayerRPG;
                if (interactivePlayer != null)
                {
                    _players.Add(interactivePlayer);
                    sb.Append($"<{player.ContestantName}>\t");
                }
            }
            for (int g = 1; g <= 10; g++)
            {
                _players.Add(new PlayerRPG(g));
            }
            showMessageDelegate("Players that successfully entered the arena:\r\n" + sb);
            sb.Clear();
            _ignoreReactions = false;
            _classInit = true;
            showMessageDelegate($"\nPlease select the hero class that you would like to become. Available classes are :"
                               + " Mage ( * A * ), Knight ( * B * ), Archer ( * C * ), Monk ( * D * ), Necromancer ( * E * ), Assassin ( * F * ), Elementalist ( * G * )", null, EmojiClassListOptions);

            Thread.Sleep(DelayAfterOptions);
            _ignoreReactions = true;
            _classInit = false;
            SortClasses();

            if (playerNumberinLeaderboard > _players.Count)
            {
                playerNumberinLeaderboard = _players.Count;
            }

            while (_players.Count > /*numWinners*/ 0)
            {
                Console.WriteLine("reached whileloop");
                day++;
                int startingContestantCount = _players.Count;

                // FindAdventure();
                _adventureAffinity = (HeroClass)_random.Next(7);
                //add flavour text

                _ignoreReactions = false;
                showMessageDelegate($"\n Day **{day}**\nYou have {DelayAfterOptions.Seconds} seconds to input your decision\n"
                    + "You may select how you will want to pursue your * adventure * . \nYour options are: "
                    + "<:bulb:> to Complete your adventure, <:crossed_swords:> To gain more EXP or <:money_bag:> To gain better" 
                    + " loot or <:muscle:> to skip the adventure and train.", null, EmojiAdventureListOption);
                sb.Clear();
                Thread.Sleep(DelayAfterOptions);
                _ignoreReactions = true;

                foreach (PlayerRPG player in _players)
                {
                    player.InteractiveRPGDecision = (InteractiveRPGDecision)(_random.Next(3) + 2);
                    Console.WriteLine(player.InteractiveRPGDecision);
                }

                    foreach (PlayerRPG player in _players)
                {
                    if (player.InteractiveRPGDecision != InteractiveRPGDecision.Train)
                    {
                        player.adventure.PerformAdventure(player, day, _adventureAffinity);
                    }
                    else
                    {
                        //train()
                    }
                }

                Console.WriteLine("reached adventure completion");

                List<PlayerRPG> descendingList = _players.OrderByDescending(player => player.Points).ToList();
                if (day > 2 && day % 4 == 0)
                {
                    sb.Append("LEADERBOARD\n\n");
                    for (int i = 0; i < playerNumberinLeaderboard; i++)
                    {
                        sb.Append($"{i + 1}. {descendingList[i].NickName} || Score = {descendingList[i].Points} || Lvl = {descendingList[i].Level} || Combat power = {descendingList[i].EffectiveCombatStats}\n");
                    }
                    showMessageDelegate("" + sb, null);
                    sb.Clear();
                }
                if (_players.Count <= ShowPlayersWhenCountEqual[showPlayersWhenCountEqualIndex])
                {
                    showPlayersWhenCountEqualIndex++;
                    foreach (PlayerRPG contestant in _players)
                    {
                     //   sb.Append($"<{contestant.ContestantName}> * HP = {contestant.Stamina} *\t");
                    }
                    //showMessageDelegate("Players Remaining:\r\n" + sb);
                    sb.Clear();
                }


                Thread.Sleep(DelayBetweenCycles);

                if (cannelGame())
                    return;
                if (duelCooldown != 0)
                {
                    duelCooldown--;
                }
                if (day == 35) break;
            }

            sb.Append("\n\n**Game Over**\r\n\r\n");
            StringBuilder sbP = new StringBuilder(1000);
            foreach (PlayerRPG contestant in _players)
            {
                sbP.Append($"(ID:{contestant.UserId})<{contestant.FullUserName}> is victorious!\r\n");
                sb.Append($"<{contestant.FullUserName}> is victorious!\r\n");
            }
            showMessageDelegate(sb.ToString(), sbP.ToString());



        }

        private void SortClasses()
        {
            foreach (PlayerRPG contestant in _players)
            {
                MakeClass(contestant);
            }
        }
        
        private void MakeClass(PlayerRPG player)
        {
            for (int i = 0; i < player.HeroStats.Length; i++)
            {
                player.HeroStats[i] = HeroBaseStatsDictionary[player.HeroClass][i];
                player.HeroStatMult[i] = HeroScalingDictionary[player.HeroClass][i];
            }
        }
        
        public void HandlePlayerInput(ulong userId, string reactionName)
        {
            if (_ignoreReactions) return;

            if (_classInit)
            {
                var authenticPlayer = _players.FirstOrDefault(contestant => contestant.UserId == userId);
                if (authenticPlayer != null)
                {
                    switch (reactionName)
                    { //🇦 🇧 🇨 🇩 🇪 🇫 🇬
                        case "🇦":
                            authenticPlayer.HeroClass = HeroClass.Mage;
                            Console.WriteLine("mage");
                            break;
                        case "🇧":
                            authenticPlayer.HeroClass = HeroClass.Knight;
                            break;
                        case "🇨":
                            authenticPlayer.HeroClass = HeroClass.Archer;
                            break;
                        case "🇩":
                            authenticPlayer.HeroClass = HeroClass.Monk;
                            break;
                        case "🇪":
                            authenticPlayer.HeroClass = HeroClass.Necromancer;
                            break;
                        case "🇫":
                            authenticPlayer.HeroClass = HeroClass.Assassin;
                            break;
                        case "🇬":
                            authenticPlayer.HeroClass = HeroClass.Elementalist;
                            break;
                    }
                }
            }
            else if (_crowdOptions)
            {
                switch (reactionName)
                {
                    case "🅰":
                        _reactionA++;
                        break;
                    case "🅱":
                        _reactionB++;
                        break;
                }
            }
            else
            {
                var authenticPlayer = _players.FirstOrDefault(contestant => contestant.UserId == userId);
                if (authenticPlayer != null)
                {

                    switch (reactionName)
                    {
                        case "💡":
                            authenticPlayer.InteractiveRPGDecision = InteractiveRPGDecision.LookForCompletion;
                            break;
                        case "⚔":
                            authenticPlayer.InteractiveRPGDecision = InteractiveRPGDecision.LookForExp;
                            break;
                        case "💰":
                            authenticPlayer.InteractiveRPGDecision = InteractiveRPGDecision.LookForLoot;
                            break;
                        case "💪":
                            authenticPlayer.InteractiveRPGDecision = InteractiveRPGDecision.Train;
                            break;
                    }
                }
            }
        }
    }
}

