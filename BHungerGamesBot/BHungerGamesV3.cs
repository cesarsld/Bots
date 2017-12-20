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

        private static readonly ScenarioRPG[] Scenarios;

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

            Scenarios = new[]
            {
                new ScenarioRPG("<{player_name}> stumbled across an abandoned sack. Perhaps a captured Bully dropped it? Opening it, they find a * {rarity_type} * {plass_type} loot.", RarityRPG.Common),
                new ScenarioRPG("<{player_name}> was wistfully skulking around the pier in town, hoping that fishing was released! All of a sudden, they lost their balance, and fell in! Oh my goodness! Hidden beneath the surface they found a * {rarity_type} * <{class_type}> loot!", RarityRPG.Common),
                new ScenarioRPG("Astaroth is very lonely these days, no one bothers to come see him anymore. He tries to get <{player_name}>s attention by offering them a * {rarity_type} * <{class_type}> loot.", RarityRPG.Common),
                new ScenarioRPG("Just when <{player_name}> was about to use some scissors on their credit card, they find a * {rarity_type} * <{class_type}> loot! Baited again!", RarityRPG.Common),
                new ScenarioRPG("While trying to think of a funny HG scenario, <{player_name}> stumbles across a * {rarity_type} * <{class_type}> loot! How ironic. . .", RarityRPG.Common),
                new ScenarioRPG("<{player_name}> defeats Kaleido in a best-of-three to the death arm wrestling competition. For their bravery and strength, they're awarded a * {rarity_type} * <{class_type}> loot.", RarityRPG.Common),
                new ScenarioRPG("{player__name} successfully uses all the Discord channels correctly. As a reward, Tarri slips a * {rarity_type} * <{class_type}> loot into their pocket. Good job!", RarityRPG.Common),
                new ScenarioRPG("<{player_name}> boldly but stupidly tries to win a drinking competition with Taters. They wake up two days later with a 'sorry about the mess' note taped to their forehead, and a brand new * {rarity_type} * <{class_type}> loot on their pillow.", RarityRPG.Common),
                new ScenarioRPG("<{player_name}> finds an extremely rare vending machine in an R4. They deposit 1 rombit. Whirr bzzt zzzzzt nnnngggg bzzzz click thunk! A * {rarity_type} * <{class_type}> loot falls out!", RarityRPG.Common),
                new ScenarioRPG("Congratulations <{player_name}>! You have been visited by the Mythical Magical Mystical Miraculous Gobby of Giving! With a wave of his hand, he conjures up a * {rarity_type} * <{class_type}> loot just for you! Enjoy!", RarityRPG.Common),
                new ScenarioRPG("<{player_name}> has been hiding in z2d4 dressed in their full blubber suit for four days now, and no one has noticed. Finally the moment comes, Gemm turns his back! You steal a * {rarity_type} * <{class_type}> loot!  Muahaha!", RarityRPG.Common),
                new ScenarioRPG("<{player_name}> - 'Wahhhhhh. I did 500 raids with a super scroll and all I found was this * {rarity_type} * <{class_type}> loot. Rigged! Refund! Reeeeeeeee!!'", RarityRPG.Common),
                new ScenarioRPG("Roses are red, Robomax-6000 is blue.\nShadown88 has a * {rarity_type} * <{class_type}> loot,\nAnd now <{player_name}> has one too.", RarityRPG.Common),
                new ScenarioRPG("As <{player_name}> proceeds to peel off Prof. Oak's bark, they find a * {rarity_type} * <{class_type}> loot hidden inside it's shell.", RarityRPG.Common),
                new ScenarioRPG("<{player_name}> signs a death wish under Grimz. Wait! It appears Grimz presented the wrong contract, <{player_name}>  receives a * {rarity_type} * <{class_type}> loot from signing Grimz's death wish.", RarityRPG.Common),
                new ScenarioRPG("<{player_name}> lands a critical empower dual strike  on Capt. Woodbeard and is rewarded with a * {rarity_type} * <{class_type}> loot.", RarityRPG.Common),
                new ScenarioRPG("<{player_name}> raids the Hyper Dimension for the Walkom schematic. <{player_name}> receives 10 friend requests, turns out it was just a : {rarity_legendary} : <{class_type}> loot.", RarityRPG.Common),
                new ScenarioRPG("<{player_name}> blacked out on a Fammy Slop bender and woke up in Quinn’s Stables clutching a * {rarity_type} * <{class_type}> loot as a makeshift pillow.", RarityRPG.Common),
                new ScenarioRPG("<{player_name}> decided to go for a 0/0/1 speedster build. The regulars at #bh_theorycrafting took pity on such a foolish endeavor and gifted the player a * {rarity_type} * <{class_type}> loot out of charity.", RarityRPG.Common),
                new ScenarioRPG("Uh oh, <{player_name}>. Someone spiked the hot cocoa last night. Those aren’t your pants you’re wearing. You check the pockets for identification and find a * {rarity_type} * <{class_type}> loot, instead.", RarityRPG.Common),
                new ScenarioRPG("<{player_name}> sees Sir Quackers waddling around the pier and throws him some breadcrumbs. Overjoyed, Sir Quackers leads them to his secret loot stash and offers the player a * {rarity_type} * <{class_type}> loot.", RarityRPG.Common),
                new ScenarioRPG("<{player_name}> sees a “Take an item, leave an item” bin by the guild hall entrance. <{player_name}> takes a * {rarity_type} * <{class_type}> loot and leaves a Bronze Coin in its place. Way to go, you jerk.", RarityRPG.Common)
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
                        sb.Append(player.adventure.PerformAdventure(player, day, _adventureAffinity, _players.Count, Scenarios));
                    }
                    else
                    {
                        player.Train();
                    }
                }

                showMessageDelegate("" + sb, null);
                sb.Clear();


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
                foreach (ScenarioRPG scenario in Scenarios)
                {
                    scenario.ReduceTimer();
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

        private void Duel()
        {
            //PlayerRPG player1;
            //PlayerRPG player2;
            int duelAmount = _players.Count;
            while (duelAmount != 0)
            {
                int index = _random.Next(_players.Count);
                int index2;
                int playerCount = _players.Count;
                //player1 = _players[index];
                if (_players[index].HasDueled) continue;
                index2 = index;
                if (index < 3)
                {
                    while (index2 != index && _players[index2].HasDueled) { index2 = _random.Next(5); }
                }
                else if (index > _players.Count - 3)
                {
                    while (index2 != index && _players[index2].HasDueled) { index2 = _players.Count - _random.Next(5); }
                }
                else
                {
                    while (index2 != index && _players[index2].HasDueled) { index2 = _random.Next(index - 2, index + 3); }
                }

                duelAmount--;
            }

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

