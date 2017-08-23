using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Linq;
using Discord;

namespace BHungerGaemsBot
{
    public class BHungerGamesV2
    {
        private const int DelayValue = 7;

        private static readonly TimeSpan DelayBetweenCycles;
        private static readonly TimeSpan DelayAfterOptions;
        private static readonly ReadOnlyCollection<DangerLevel> LootDangerLevels;
        private static readonly int[] ShowPlayersWhenCountEqual;
        private static readonly ReadOnlyCollection<IEmote> EmojiListOptions;
        private static readonly ReadOnlyCollection<IEmote> EmojiListEnhancedOptions;
        private static readonly Scenario[] Scenarios;

        private readonly Random _random;
        private readonly HashSet<InteractivePlayer> _enchancedPlayers;
        private readonly HashSet<InteractivePlayer> _duelImmune;
        private volatile bool _ignoreReactions;
        private List<Trap> _traps;
        private List<InteractivePlayer> _contestants;
        private bool _enhancedOptions;

        private DangerLevel _currentDangerLevel;

        public class Trap
        {
            public int Damage;
            public ulong TrapUserID;

            public Trap(InteractivePlayer contestant, Random random)
            {
                Damage = 5 * random.Next(2, 9);
                TrapUserID = contestant.UserId;
            }
        }

        public class DangerLevel
        {
            public readonly string Name;
            public readonly int FailChance;
            public readonly int CommonChance;
            public readonly int RareChance;
            public readonly int EpicChance;
            public readonly int LegendaryChance;
            public readonly int SetChance;

            public DangerLevel(string name, int failChance, int commonChance, int rareChance, int epicChance, int legendaryChance, int setChance)
            {
                Name = name;
                FailChance = failChance;
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

        private class Scenario
        {
            private readonly string _description;
            public readonly int TypeValue;
            public readonly ScenarioType Type;
            public int Delay { get; set; }

            public Scenario(string description, ScenarioType type, int typeValue) //creates instance of a scenario, defining its description and players needed
            {
                _description = description;
                Type = type;
                TypeValue = typeValue;
                if (type != ScenarioType.Lethal)
                {
                    _description = _description.Replace("{_typeValue}", TypeValue.ToString());
                }
                Delay = 0;
            }

            public string GetText(string player) //replace {@Px} by player name
            {
                string value = _description?.Replace("{@P1}", player);
                return value;
            }

            public void ReduceDelay()// reduce delay after scenario has been used
            {
                if (Delay > 0)
                    Delay -= 1;
            }
        }

        static BHungerGamesV2()
        {
            DelayBetweenCycles = new TimeSpan(0, 0, 0, 25);
            DelayAfterOptions = new TimeSpan(0, 0, 0, 15);
            LootDangerLevels = new ReadOnlyCollection<DangerLevel>(new List<DangerLevel>()
            {
                new DangerLevel("Safe", 0, 60, 87, 97, 99, 100),
                new DangerLevel("Unsafe", 10, 57, 82, 94, 98, 100),
                new DangerLevel("Dangerous", 25, 40, 70, 85, 95, 100),
                new DangerLevel("Deadly", 50,  0, 40, 70, 90, 100)
            });

            ShowPlayersWhenCountEqual = new[] {20, 10, 5, 2, 0 };
            EmojiListOptions = new ReadOnlyCollection<IEmote>(new List<IEmote> { new Emoji("💰"), new Emoji("❗"), new Emoji("⚔") });
            EmojiListEnhancedOptions = new ReadOnlyCollection<IEmote>(new List<IEmote> { new Emoji("💣"), new Emoji("🔫"), new Emoji("🔧") });

            Scenarios = new[]
            {
                //new Scenario ("{@P1} has been dealt {_typeValue} HP", ScenarioType.Damaging, 20),
                //new Scenario ("{@P1} has been killed", ScenarioType.Lethal, 100),
                //new Scenario ("{@P1} has been healed for {_typeValue} HP", ScenarioType.Healing, 20),
                //new Scenario ("{@P1} has increased loot find pf {_typeValue} for the next turn", ScenarioType.LootFind, 10),

                //Miscellaneous stuff
                new Scenario ("{@P1} swam through a pond filled with Blubbler's acidic waste to pursue their journey. (-{_typeValue}HP)", ScenarioType.Damaging, 20),
                new Scenario ("{@P1} stubbed their toe on a Hypershard. (-100000000HP)", ScenarioType.Lethal, 100000000),
                new Scenario ("{@P1} forgot that this was the INTERACTIVE Hunger Games and stood idle for 5 minutes, which was just enough time for a Grampz to wander by and smack them with his cane. (-{_typeValue}HP)", ScenarioType.Damaging, 5),
                new Scenario ("{@P1} saw a Booty fly and tried to catch it. It noticed and, unhappy about that, decided to boop {@P1} on the head.  (-{_typeValue}HP)", ScenarioType.Damaging, 10),
                new Scenario ("{@P1} encounters Ragnar in his quest for Loot. Ragnar will only let them pass if {@P1} beats them at a game of chess. Sadly, {@P1} forgot Ragnar was an avid chess player... (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("'Let’s play hangman” declared Zorul. Sadly Zorul never really understood that game. {@P1} got hanged and died.", ScenarioType.Lethal, 100),
                new Scenario ("{@P1} got caught staring at Kov'alg's cleavage... (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                //new Scenario ("{@P1} entered R3 and overheard Woodbeard talking to Beido, his long distance brother with a meth addiction, who he hasn't seen for months. Being an intimate moment, Woodbeard kicked {@P1} out of the dungeon. (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("{@P1} encountered the legendary Wemmbo in the woods while searching for cover! 'Heal me please!' - 'Get lost kiddo, I'm just a mantis *bzzt bzzt*' (-{_typeValue}HP)", ScenarioType.Damaging, 10),
                new Scenario ("While adventuring out into the wilderness {@P1} found a horde of Zirg. Attempting to back away {@P1} steps on a twig and causes the Zirg to zerg them. (-{_typeValue}HP) ", ScenarioType.Damaging, 30),
                new Scenario ("{@P1} finds several piles of bones from previous adventurers. While searching the bones, some of them start to shake violently. {@P1} proceeds to get Jacked up. (-{_typeValue}HP)", ScenarioType.Damaging, 40),
                new Scenario ("{@P1} found a treasure chest. Sadly the treasure chest was actually Mimzy. (-{_typeValue}HP)", ScenarioType.Damaging, 20),
                new Scenario ("{@P1} went in search of his old friend Bob whom they had heard lived in a small cottage deep inside the forest. Wait... wrong Bob. (-{_typeValue}HP)", ScenarioType.Damaging, 25),
                new Scenario ("While trekking up a mountain in an attempt to get a better view of the arena {@P1} slipped on a loose rock and tumbled back down to the base. Time to start over... (-{_typeValue}HP)", ScenarioType.Damaging, 5),
                new Scenario ("While searching for shelter, {@P1} came across a roaming Trixie. {@P1} ran as fast as possible, but tripped on a log...oh shiieeeet. (-{_typeValue}HP)", ScenarioType.Damaging, 25),
                new Scenario ("{@P1} found a slightly damaged parachute. 'This will surely work'...it didn't. (-{_typeValue}HP)", ScenarioType.Damaging, 40),
                new Scenario ("{@P1} encountered a Bobodom and tried to slay him for loot. While fighting {@P1} could hear Bobodom hum 'Can't Touch This' by MC Hammer.  (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("{@P1} sees a dark figure in the horizon. It is the powerful SSS1. It is said that people who encounter him witness a bright light before their death. {@P1} isn't an exception (-10000HP)", ScenarioType.Lethal, 10000), //to be edited
                //new Scenario ("{@P1} bumped into Tarri in his journey to slay Grimz. Tarri didn't like that and cock slapped him with her well endowed penis (-{_typeValue}HP)", ScenarioType.Lethal, 696969),
                new Scenario ("{@P1} ran into a battle with 4 Bargz on his way to slay Woodbeard. They all bombarded them with dozens of cannon shots. (-{_typeValue}HP))", ScenarioType.Damaging, 35),
                new Scenario ("{@P1} is wandering through the woods. They encounter Gobby, Olxa, Mimzy AND Bully swinging their sack at a poor defenceless Batty. {@P1} tried to interfere, but ended up getting sack-whacked. (-{_typeValue}HP)", ScenarioType.Damaging, 45),
                new Scenario ("{@P1} was standing on the pier, waiting for a fishing minigame to be implemented. The wood broke under their feet, and they fell into the water. (-{_typeValue}HP)", ScenarioType.Damaging, 20),
                new Scenario ("{@P1} mistook Capt. Woodbeard for Jack Sparrow and asked him for an autograph. Woodbeard signed wiith his cutlass and slapped {@P1} with the book (-{_typeValue}HP)", ScenarioType.Damaging, 5),
                new Scenario ("{@P1}, while hiding in a tree woke up a colony of Batty that startled them. {@P1} fell off the tree (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("{@P1} hurt their back carrying all the unnecessary common and rare materials in their bag. (-{_typeValue}HP)", ScenarioType.Damaging, 5),
                new Scenario ("{@P1} attacked Mimzy while he was sleeping! Inside his chest they found a minor healing potion! (+{_typeValue}HP)", ScenarioType.Healing, 25),
                new Scenario ("{@P1} challenged Krackers to a tickle fight! They didn't realised Krackers had eight legs... (-{_typeValue}HP)", ScenarioType.Damaging, 10),
                new Scenario ("{@P1} tried to beat Conan in an arm wrestle. 'Tried' (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("{@P1} is exhausted... They were on the verge of dying, but wait! A wild HP shrine appears! (+{_typeValue})", ScenarioType.Healing, 100),
                new Scenario ("{@P1} used Epic Speed Kick to reach loot faster! Sadly they didn't tie their laces properly, tripped and fell on their face. (-{_typeValue}hP)", ScenarioType.Damaging, 25),
                new Scenario ("{@P1} saw a Shrump and tried to bribe him. tsk tsk tsk... Shrump can't be bribed! {@P1} got bribed instead and forced to serve Shrump. (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("Feeling thirsty, {@P1} ventured in Quirrel's fortress for water. They found Juice instead who promptly attempted to impale them. (-{_typeValue}HP)", ScenarioType.Damaging, 30),
                new Scenario ("In his quest, {@P1} found a sad Trixie sitting on a rock. {@P1} tried to give it a hug but Trixie couldn't hug back due to its small arms. Filled with rage, Trixie chomped {@P1}'s arm off (-{_typeValue}HP)", ScenarioType.Damaging, 65),
                new Scenario ("{@P1} found Zayu cheating on his body pillow with an actual woman! Zayu made sure {@P1} couldn't see anything anymore. (-{_typeValue}HP)", ScenarioType.Damaging, 30),
                new Scenario ("'Nice legs you got there, Woodbea-errr... legendaries, nice legendaries' said {@P1}. Woodbeard proceeded to plunder {@P1}'s booty (-{_typeValue}HP)", ScenarioType.Damaging, 25),
                new Scenario ("{@P1} sneaked into Warty's dungeon looking for the Wemmbo schematic. Sadly {@P1} encountered a flock of Zammy heading towards them (-{_typeValue}HP)", ScenarioType.Damaging, 10),
                new Scenario ("While avoiding the other survivors, {@P1} unknowingly entered Remruade's hunting grounds. Remruade shot an arrow towards {@P1}. *Thunk*. {@P1} takes an arrow to the knee!  (-{_typeValue}HP)\" _typeValue = 15} (-{_typeValue}hP)", ScenarioType.Damaging, 10),
                new Scenario ("{@P1} found Blubber's mating grounds. Many Blubbies (baby Blubbers) started swarming towards {@P1} and nearly suffocated them to death (-{_typeValue}HP)", ScenarioType.Damaging, 65),
                new Scenario ("{@P1} imagined a fusion in between Gemm and Conan. Distracted by their deep thinking, a wild Tubbo walked up and kicked them in the groin. (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("A rock fell onto {@P1}'s head. Wait~ what? But it already happened before! HG is rigged!! (-100HP)", ScenarioType.Lethal, 100),
                new Scenario ("{@P1} is on his way to defeat the mighty King Dina. HP shrine available, potions unused, what could go wrong? Dina was slain but at the cost of {@P1}’s left arm (-{_typeValue}HP)", ScenarioType.Damaging, 80),
                new Scenario ("{@P1} found the Legendary B.I.T. Chain! It was guarded by the mighty Kaleido. On his attempt it to steal it, {@P1} bumped into a Violace that tried slaying them (-{_typeValue}HP)", ScenarioType.Damaging, 30),
               
               
               
               
               
                //pet related
                new Scenario ("{@P1} encounters a flock of legendary Nemo feasting on a Rexxie carcass. Those things look deadly. *crack* {@P1} stepped on a twig. All the Nemo started swarming towards the sound. {@P1} managed to escape, but with minor bruises. (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("{@P1} found a Legendary Nerder. It is said no one likes them, that they're too selfish. But this Nerder looked different. Argh, {@P1}, how can they be fooled like this. Nerder proceeded to rob {@P1}  (-{_typeValue}HP)", ScenarioType.Damaging, 20),
                new Scenario ("{@P1} is blessed with Gemmi's great healing! (+{_typeValue}HP)", ScenarioType.Healing, 15),
                new Scenario ("{@P1} was heading back towards B.I.T. Town when they bumped into a lone Sudz. They spent the evening together. Drunk, {@P1} tripped walking down stairs and bumped his head (-{_typeValue}HP)", ScenarioType.Damaging, 20),
                new Scenario ("Even in the darkest of times, light shines if you look closely enough. {@P1} sees a dim orange light in the horizon. It is the Legendary Crem! {@P1} is granted an immense revitalising heal. (+{_typeValue}HP)", ScenarioType.Healing, 40),
                new Scenario ("{@P1} encounters the Legendary Nelson! A majestic creature. His bite is even moreso majestic and painful(-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("{@P1} was about to eat a BITburger when they realised it was in fact Boiguh. Unhappy, Boiguh headbutted them. No one eats Boiguh. (-{_typeValue}HP)", ScenarioType.Damaging, 25),
                new Scenario ("{@P1}'s Melvin Champ slipped off their stand and double kicked {@P1} in the face (-{_typeValue}HP)", ScenarioType.Damaging, 25),
                //new Scenario ("{@P1} (-{_typeValue})", ScenarioType.Damaging, 10),
                //new Scenario ("{@P1} (-{_typeValue})", ScenarioType.Damaging, 10),
                //new Scenario ("{@P1} (-{_typeValue})", ScenarioType.Damaging, 10),
                
           

                //material related
                new Scenario ("{@P1} picked a Doubloon up off the floor! However, Bully saw this and knocked out {@P1} to steal it. (-{_typeValue}HP)", ScenarioType.Damaging, 30),
                new Scenario ("After many miles travelled, {@P1} found his first Hypershard. Tears start dripping on the rare crystal as {@P1} is filled with joy. But wait! They forgot Hypershards dissolved in water. Filled with anger, {@P1} slammed his head against a tree. (-{_typeValue}HP)", ScenarioType.Damaging, 25),
                new Scenario ("{@P1} didn't realise they used all their rare materials on rare enchantment rerollsl. {@P1} facepalmed themself so hard, they lost {_typeValue}HP", ScenarioType.Damaging, 10),
                new Scenario ("{@P1} was looking for leg sneks in Z5D3. Sadly, {@P1} only found common materials and a Brute charging at them. (-{_typeValue}HP)", ScenarioType.Damaging, 20),
                new Scenario ("{@P1}, after a long search for treasure, found a Harmony Orb! A valuable resource. It would have been theirs, if there wasn't a Bluz guarding it. (-{_typeValue}HP)", ScenarioType.Damaging, 20),
                new Scenario ("{@P1}, while pausing briefly to catch their breath, saw movement in the bushes. An enchanted snek! {@P1} sprung headlong into the brambles, seizing the alarmed serpent, before quickly discovering that it was just a regular (but incredibly venomous and now angry) one.  (-{_typeValue}HP)", ScenarioType.Damaging, 10),
                new Scenario ("{@P1} was running through a forest glade when they tripped over a rock, sending their knee 10% more painfully into the ground thanks to their Bushido set. (-{_typeValue}HP)", ScenarioType.Damaging, 22),
                //new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),
                //new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),
                
                //Notorious players related
                new Scenario ("{@P1}, on his journey to become the best BIT Hero, thought about all the past legends. Blasian, Zim, Leg0Lars.. how they wished they were like them. They also ended up wishing they had paid more attention, as they ended up walking right into a raging Tubbo. (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("{@P1} laughed at Shadown88's build. Shadown88 laughed too when he dual crit empower striked {@P1} for 30386 damage. (-30386HP)", ScenarioType.Lethal, 10),
                //new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),
                //new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),

                //fam related
                new Scenario ("{@P1} encountered Prof. Oak. 'Don't come back here until you've completed your Juppiodex!' *kicks {@P1} out of his lab*. (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("{@P1} went to walk around the beach to fish when an Ives attacked them unexpectedly (-{_typeValue}HP)", ScenarioType.Damaging, 15),
                new Scenario ("{@P1} sees a Doubloon in the corner of the cabin! {@P1} walks right in front of Woodbeard who was waiting for them. 'You arrrr mine!'. (-{_typeValue}HP)", ScenarioType.Damaging, 40),
                new Scenario ("{@P1} walked through the Hyper Dimension and got ran over by a herd of Oevor. (-{_typeValue}HP)", ScenarioType.Damaging, 35),
                new Scenario ("{@P1} lost their mind eating some psychedelic Shrump offsprings. They attempted to ride a wild Trixxie while shouting 'Toga! Toga!'.  (-{_typeValue}HP)", ScenarioType.Damaging, 35),
                new Scenario ("{@P1} was apprehensively moving through shadowy woods when they were suddenly startled by the sound of heavy footsteps behind them. Out of desperation, and perhaps a dash of curiosity, {@P1} tried mounting their Driffin like a horse to gallop to a hasty getaway. The Driffin made its displeasure known. Painfully. (-{_typeValue}HP)", ScenarioType.Damaging, 45),
                //new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),
               // new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),

                //cosmetic related
                new Scenario ("{@P1} was hiding in the bushes when they overheard other players making fun of their choice in cosmetics. Sometimes, the trauma of the arena leaves no physical mark, but hurts just the same. (-{_typeValue}HP)", ScenarioType.Damaging, 10),
                new Scenario ("{@P1} has finally looted the notoriously rare Blubber Suit. As soon as they put it on, they get attacked by other players thinking he is a Blubber! (-{_typeValue}HP)", ScenarioType.Damaging, 20),

                //HG related
                new Scenario ("{@P1} stepped on a mine. (-{_typeValue}HP)", ScenarioType.Damaging, 25),
                new Scenario ("{@P1} died\n.\n.\n.\njk. They did lose -{_typeValue}HP though.", ScenarioType.Damaging, 10),
                new Scenario ("{@P1} died\n.\n.\n.\njk. They did lose -{_typeValue}HP though.", ScenarioType.Damaging, 25),
                new Scenario ("{@P1} died\n.\n.\n.\njk. They did lose -{_typeValue}HP though.", ScenarioType.Damaging, 15),
                new Scenario ("{@P1} died\n.\n.\n.\njk. They did lose -{_typeValue}HP though.", ScenarioType.Damaging, 20),


               // new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),
              //  new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),
             //   new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),
              //  new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),
               // new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),
               // new Scenario ("{@P1} (-{_typeValue}HP)", ScenarioType.Damaging, 10),


            };

        }

        public BHungerGamesV2()
        {
            _random = new Random();
            _enchancedPlayers = new HashSet<InteractivePlayer>();
            _duelImmune = new HashSet<InteractivePlayer>();
            _ignoreReactions = true;
        }

        private Scenario GetScenario() //get a scenario
        {
            while (true)
            {
                int randIndex = _random.Next(Scenarios.Length);
                if (Scenarios[randIndex].Delay <= 0)
                {
                    Scenarios[randIndex].Delay = DelayValue;
                    return Scenarios[randIndex];
                }
            }
        }

        public void Run(int numWinners, List<Player> contestantsTransfer, BotGameInstance.ShowMessageDelegate showMessageDelegate, Func<bool> cannelGame, int maxPlayers = 0)
        {
            int day = 0;
            int night = 0;
            int showPlayersWhenCountEqualIndex = 0;
            int duelCooldown = 4;

            StringBuilder sb = new StringBuilder(2000);
            StringBuilder sbLoot = new StringBuilder(2000);
            _traps = new List<Trap>();
            List<Trap> trapsToBeRemoved = new List<Trap>();
            List<InteractivePlayer> playersToBeRemoved = new List<InteractivePlayer>();

            if (maxPlayers > 0 && contestantsTransfer.Count > maxPlayers)
            {
                int numToRemove = contestantsTransfer.Count - maxPlayers;
                for (int i = 0; i < numToRemove; i++)
                {
                    int randIndex = _random.Next(contestantsTransfer.Count);
                    sb.Append($"<{contestantsTransfer[randIndex].ContestantName}>\t");
                    contestantsTransfer.RemoveAt(randIndex);
                }
                showMessageDelegate("Players killed in the stampede trying to get to the arena:\r\n" + sb);
                sb.Clear();
            }

            _contestants = new List<InteractivePlayer>();
            foreach (Player player in contestantsTransfer)
            {
                InteractivePlayer interactivePlayer = player as InteractivePlayer;
                if (interactivePlayer != null)
                {
                    _contestants.Add(interactivePlayer);
                }
            }

            while (_contestants.Count > numWinners)
            {
                // day cycle
                day++;
                List<int> scenarioImmune = new List<int>();
                int startingContestantCount = _contestants.Count;
                var scenarioToBeExecuted = startingContestantCount / 4;
                if (day > 11)
                {
                    scenarioToBeExecuted = startingContestantCount / 2;
                }
                if (scenarioToBeExecuted < 1)
                {
                    scenarioToBeExecuted = 1;
                }
                int playerToEnhance = _contestants.Count / 4;
                if (playerToEnhance <= 0)
                {
                    playerToEnhance = 1;
                }

                _currentDangerLevel = LootDangerLevels[_random.Next(LootDangerLevels.Count)];
                sb.Append($"\nDanger level to look for loot = * {_currentDangerLevel.Name} *");
                // Select Enhanced players

                _ignoreReactions = false;
                showMessageDelegate($"\n Day**{day}**\nYou have {DelayAfterOptions.Seconds} seconds to input your decision\n"
                    + "You may select <:moneybag:> to Loot, <:exclamation:> to Stay On Alert or <:crossed_swords:> to be immuned to Duels! If you do NOT select a reaction, you will Do Nothing." + sb, null, EmojiListOptions);
                sb.Clear();
                Thread.Sleep(DelayAfterOptions);
                _ignoreReactions = true;
                if (cannelGame())
                    return;

                //sbLoot.Append("People that successfully dropped loot: \n\n");
                foreach (InteractivePlayer contestant in _contestants)
                {
                    switch (contestant.InteractiveDecision)
                    {
                        case InteractiveDecision.Loot:
                            Loot(contestant, sbLoot, playersToBeRemoved);
                            break;
                        case InteractiveDecision.StayOnAlert:
                            StayOnAlert(contestant, sb);
                            break;
                        case InteractiveDecision.ImmuneToDuel:
                            DuelImmuneOption(sb, contestant);
                            break;
                    }
                }
                showMessageDelegate("" + sbLoot + sb);
                sbLoot.Clear();
                sb.Clear();

                _contestants = _contestants.Except(playersToBeRemoved).ToList();

                //enhanced
                _enhancedOptions = true;
                sb.Append("Enhanced Decisions have been attributed to:\n");
                _enchancedPlayers.Add(_contestants[_random.Next(_contestants.Count)]);
                while (_enchancedPlayers.Count < playerToEnhance)
                {
                    InteractivePlayer player = _contestants[_random.Next(_contestants.Count)];
                    _enchancedPlayers.Add(player);
                }
                foreach (InteractivePlayer player in _enchancedPlayers)
                {
                    sb.Append($"<{player.NickName}>\n");
                }

                _ignoreReactions = false;
                showMessageDelegate(sb + $"You have {DelayAfterOptions.Seconds} seconds to input your decision\n"
                    + "You may select <:bomb:> to Make A Trap, <:gun:> To Steal or <:wrench:> To Sabotage! If you do NOT select a reaction, you will Do Nothing.\n", null, EmojiListEnhancedOptions);
                //make bot react to prevent players from searching emojis
                sb.Clear();
                Thread.Sleep(DelayAfterOptions);
                _ignoreReactions = true;
                if (cannelGame())
                    return;

                foreach (InteractivePlayer contestant in _contestants)
                {
                    switch (contestant.EnhancedDecision)
                    {
                        case EnhancedDecision.Sabotage:
                            Sabotage(contestant, sb);
                            break;
                        case EnhancedDecision.Steal:
                            Steal(contestant, sb);
                            break;
                        case EnhancedDecision.MakeATrap:
                            MakeTrap(contestant, sb);
                            break;
                    }
                }
                _enhancedOptions = false;
                showMessageDelegate(sb.ToString());
                sb.Clear();

                //night cycle
                night++;
                int index;

                while (scenarioToBeExecuted != 0 && _contestants.Count > 2) //scenarios
                {
                    index = _random.Next(_contestants.Count);
                    if (scenarioImmune.Contains(index))
                    {
                        continue;
                    }
                    switch (_contestants[index].Debuff)
                    {
                        case Debuff.IncreasedScenarioLikelihood when _contestants[index].DebuffTimer > 0:
                            _contestants[index].ScenarioLikelihood += 10;
                            _contestants[index].DebuffTimer--;
                            break;
                        case Debuff.SeverlyIncreasedScenarioLikelihood when _contestants[index].DebuffTimer > 0:
                            _contestants[index].ScenarioLikelihood += 10;
                            _contestants[index].DebuffTimer--;
                            break;
                    }
                    if (RngRoll(_contestants[index].ScenarioLikelihood))
                    {
                        scenarioImmune.Add(index);
                        scenarioToBeExecuted--;
                        var selectedPlayer = "<" + _contestants[index].NickName + ">";

                        //scenario method
                        Scenario currentScenario = GetScenario();
                        sb.Append(currentScenario.GetText(selectedPlayer)).Append(" ");
                        switch (currentScenario.Type)
                        {
                            case ScenarioType.Damaging:
                                _contestants[index].Hp -= currentScenario.TypeValue;
                                sb.Append($" * Current HP = {_contestants[index].Hp} *\n\n");
                                if (_contestants[index].Hp <= 0)
                                {
                                    _contestants.RemoveAt(index);
                                }
                                break;
                            case ScenarioType.Lethal:
                                _contestants.RemoveAt(index);
                                sb.Append("\n\n");
                                break;
                            case ScenarioType.Healing:
                                _contestants[index].Hp += currentScenario.TypeValue;
                                if (_contestants[index].Hp > 100)
                                {
                                    _contestants[index].Hp = 100;
                                }
                                sb.Append($" * Current HP = {_contestants[index].Hp} *\n\n");
                                break;
                        }
                    }
                }
                foreach (Trap trap in _traps)
                {
                    if (RngRoll(15))
                    {
                        index = _random.Next(_contestants.Count);
                        while (trap.TrapUserID == _contestants[index].UserId)
                        {
                            index = _random.Next(_contestants.Count);
                        }
                        _contestants[index].Hp -= trap.Damage;
                        sb.Append($"<{_contestants[index].NickName}> fell into a trap damaging them for {trap.Damage}HP. * Current HP = {_contestants[index].Hp} *\n\n");


                        if (_contestants[index].Hp <= 0)
                        {
                            sb.Append($"<{_contestants[index].NickName}> died from the trap.\n\n");
                            _contestants.RemoveAt(index);
                        }
                        trapsToBeRemoved.Add(trap);
                    }
                }
                _traps = _traps.Except(trapsToBeRemoved).ToList();
                if (duelCooldown != 0)
                {
                    duelCooldown--;
                }
                else if (_contestants.Count - _duelImmune.Count >= 2)
                {
                    Duel(sb);
                }
                else
                {
                    sb.Append("#No Duel occured due to lack of available players.\n\n");
                }
                foreach (InteractivePlayer contestant in _contestants)
                {
                    contestant.Reset(); //resets value of scenariolikelihood and interactive options
                }
                foreach (Scenario scenario in Scenarios)
                {
                    scenario.ReduceDelay();
                }
                showMessageDelegate($"\nNight**{night}** <{startingContestantCount}> players remaining\n\n" + sb);
                scenarioImmune.Clear();
                _enchancedPlayers.Clear();
                trapsToBeRemoved.Clear();
                playersToBeRemoved.Clear();
                _duelImmune.Clear();
                sb.Clear();

                if (_contestants.Count <= ShowPlayersWhenCountEqual[showPlayersWhenCountEqualIndex])
                {
                    showPlayersWhenCountEqualIndex++;
                    foreach (InteractivePlayer contestant in _contestants)
                    {
                        sb.Append($"<{contestant.ContestantName}> * HP = {contestant.Hp} *\t");
                    }
                    showMessageDelegate("Players Remaining:\r\n" + sb);
                    sb.Clear();
                }

                if (night == 11)
                {
                    showMessageDelegate("Next day until the end of the game, scenario number will be increased\n=====================================================================");
                }

                Thread.Sleep(DelayBetweenCycles);

                if (cannelGame())
                    return;

            }

            sb.Append("\n\n**Game Over**\r\n\r\n");
            StringBuilder sbP = new StringBuilder(1000);
            foreach (InteractivePlayer contestant in _contestants)
            {
                sbP.Append($"(ID:{contestant.UserId})<{contestant.FullUserName}> is victorious!\r\n");
                sb.Append($"<{contestant.FullUserName}> is victorious!\r\n");
            }
            showMessageDelegate(sb.ToString(), sbP.ToString());
        }

        private int GetDuelChance(Rarity itemRarity)
        {
            switch (itemRarity)
            {
                case Rarity.Common:
                    return 5;
                case Rarity.Rare:
                    return 10;
                case Rarity.Epic:
                    return 15;
                case Rarity.Legendary:
                    return 25;
                case Rarity.Set:
                    return 35;
            }
            return 0;
        }

        private int GetContestantDuelChance(InteractivePlayer player)
        {
            int duelChance = 0;
            if (player.WeaponLife > 0)
            {
                player.WeaponLife--;
                duelChance += GetDuelChance(player.WeaponRarity);
                if (player.WeaponLife == 0)
                {
                    player.WeaponRarity = Rarity.None;
                }
            }
            if (player.ArmourLife > 0)
            {
                player.ArmourLife--;
                duelChance += GetDuelChance(player.ArmourRarity);
                if (player.ArmourLife == 0)
                {
                    player.ArmourRarity = Rarity.None;
                }
            }
            if (player.HelmetLife > 0)
            {
                player.HelmetLife--;
                duelChance += GetDuelChance(player.HelmetRarity);
                if (player.HelmetLife == 0)
                {
                    player.HelmetRarity = Rarity.None;
                }
            }
            if (player.OffhandLife > 0)
            {
                player.OffhandLife--;
                duelChance += GetDuelChance(player.OffhandRarity);
                if (player.OffhandLife == 0)
                {
                    player.OffhandRarity = Rarity.None;
                }
            }

            if (player.Debuff == Debuff.DecreasedDuelChance && player.DebuffTimer > 0)
            {
                duelChance -= 5;
                player.DebuffTimer--;
            }
            else if (player.Debuff == Debuff.SeverlyDecreasedDuelChance && player.DebuffTimer > 0)
            {
                duelChance -= 10;
                player.DebuffTimer--;
            }

            return duelChance;
        }

        //duel method
        private void Duel(StringBuilder sb)
        {
            int duelChance = 50;
            int duelist1 = _random.Next(_contestants.Count);
            while (_duelImmune.Contains(_contestants[duelist1]))
            {
                duelist1 = _random.Next(_contestants.Count);
            }
            int duelist2 = _random.Next(_contestants.Count);
            while (duelist1 == duelist2 || _duelImmune.Contains(_contestants[duelist2]))
            {
                duelist2 = _random.Next(_contestants.Count);
            }
            sb.Append($"A Duel started in between <{_contestants[duelist1].NickName}> and <{_contestants[duelist2].NickName}>\n\n");

            duelChance += GetContestantDuelChance(_contestants[duelist1]);
            duelChance += GetContestantDuelChance(_contestants[duelist2]);

            if (RngRoll(duelChance))
            {
                sb.Append($"<{_contestants[duelist1].NickName}> won the duel and slew <{_contestants[duelist2].NickName}>\n\n");
                _contestants.RemoveAt(duelist2);
            }
            else
            {
                sb.Append($"<{_contestants[duelist2].NickName}> won the duel and slew <{_contestants[duelist1].NickName}>\n\n");
                _contestants.RemoveAt(duelist1);
            }
        }

        void DuelImmuneOption(StringBuilder sb, InteractivePlayer contestant)
        {
            if (contestant.DuelCooldown == 0)
            {
                contestant.DuelCooldown = 5;
                _duelImmune.Add(contestant);
                sb.Append($"<{contestant.NickName}> will not participate in a Duel today.\n\n");
            }
            else
            {
                sb.Append($"<{contestant.NickName}> has already used that option less than 5 days ago. They will instead Do Nothing today.\n\n");
            }
        }
        //interactive options
        private void Loot(InteractivePlayer contestant, StringBuilder sbLoot, List<InteractivePlayer> playersToBeRemoved)
        {
            contestant.ScenarioLikelihood += 10;
            int lootChance = 100 - _currentDangerLevel.FailChance;
            if (contestant.Debuff == Debuff.DecreasedItemFind && contestant.DebuffTimer > 0)
            {
                lootChance -= 5;
                contestant.DebuffTimer--;
            }
            else if (contestant.Debuff == Debuff.SeverlyDecreasedItemFind && contestant.DebuffTimer > 0)
            {
                lootChance -= 10;
                contestant.DebuffTimer--;
            }
            if (RngRoll(lootChance)) // chance to loot item
            {
                int lootType = _random.Next(4); // armour or weapon
                int lootRarity = _random.Next(100);
                //lootRarity = 55;
                Rarity itemRarity = _currentDangerLevel.GetRarity(lootRarity);
                bool tookItem = false;
                string itemTypeName = null;
                switch (lootType)
                {
                    case 0:
                        itemTypeName = "weapon";
                        if ((int)contestant.WeaponRarity < (int)itemRarity)
                        {
                            contestant.WeaponLife = 5;
                            contestant.WeaponRarity = itemRarity;
                            tookItem = true;
                        }
                        break;
                    case 1:
                        itemTypeName = "armour";
                        if ((int)contestant.ArmourRarity < (int)itemRarity)
                        {
                            contestant.ArmourLife = 5;
                            contestant.ArmourRarity = itemRarity;
                            tookItem = true;
                        }
                        break;
                    case 2:
                        itemTypeName = "helmet";
                        if ((int)contestant.HelmetRarity < (int)itemRarity)
                        {
                            contestant.HelmetLife = 5;
                            contestant.HelmetRarity = itemRarity;
                            tookItem = true;
                        }
                        break;
                    default:
                        itemTypeName = "offhand";
                        if ((int)contestant.OffhandRarity < (int)itemRarity)
                        {
                            contestant.OffhandLife = 5;
                            contestant.OffhandRarity = itemRarity;
                            tookItem = true;
                        }
                        break;
                }
                if (itemRarity != Rarity.Common)
                {
                    if (tookItem)
                    {
                        sbLoot.Append($"<{contestant.NickName}> has obtained a * {itemRarity} * {itemTypeName}!\n");
                    }
                    else
                    {
                        ReduceTextCongestion($"<{contestant.NickName}> has obtained an inferior {itemTypeName} and decided to throw it away\n", sbLoot);
                    }
                }
            }
            else
            {
                int failureDamage = 0;
                string failureDescription = null;
                switch (_currentDangerLevel.FailChance)
                {
                    case 10:
                        failureDamage = (_random.Next(2) + 2) * 5; // 10 - 15
                        failureDescription = "got ambushed while looking for loot and got injured for";
                        break;
                    case 25:
                        failureDamage = (_random.Next(2) + 6) * 5; // 30 - 35
                        failureDescription = "encountered a mini boss while looking for loot and got injured for";
                        break;
                    case 50:
                        failureDamage = (_random.Next(4) + 10) * 5; // 50 - 65
                        failureDescription = "recieved a nearly life taking blow by a powerful beast while looking for loot and got injured for";
                        break;
                }
                if (string.IsNullOrEmpty(failureDescription) == false)
                {
                    contestant.Hp -= failureDamage;
                    sbLoot.Append($"<{contestant.NickName}> {failureDescription} {failureDamage}HP. * Current HP = {contestant.Hp} *\n ");
                    if (contestant.Hp <= 0)
                    {
                        playersToBeRemoved.Add(contestant);
                    }
                }
            }
        }

        private void StayOnAlert(InteractivePlayer contestant, StringBuilder sb)
        {
            if (contestant.AlertCooldown == 0)
            {
                contestant.AlertCooldown = 4;
                contestant.ScenarioLikelihood -= 10;
                ReduceTextCongestion($"<{contestant.NickName}> successfully stayed On Alert. -10% Scenario likelihood. \n", sb);
            }
            else
            {
                int failureChance = _random.Next(10);
                if (failureChance < 6)
                {
                    contestant.AlertCooldown = 4;
                    contestant.ScenarioLikelihood += 40;
                    sb.Append($"<{contestant.NickName}> tried to Stay On Alert but fell in a deep sleep. +40% Scenario likelihood. \n");
                }
                else
                {
                    contestant.AlertCooldown = 4;
                    contestant.ScenarioLikelihood -= 10;
                    ReduceTextCongestion($"<{contestant.NickName}> successfully stayed On Alert. -10% Scenario likelihood. \n", sb);

                }
            }
        }

        private Debuff ConvertToDebuff(int id, bool severityFactor)
        {
            if (severityFactor == false)
            {
                switch (id)
                {
                    case 0:
                        return Debuff.DecreasedItemFind;
                    //case 1:
                    //    return Debuff.IncreasedDamageTaken;
                    case 1:
                        return Debuff.DecreasedDuelChance;
                    case 2:
                        return Debuff.IncreasedScenarioLikelihood;
                }
                return Debuff.DecreasedItemFind; // what should be default if not found?
            }
            switch (id)
            {
                case 0:
                    return Debuff.SeverlyDecreasedItemFind;
                //case 1:
                //   return Debuff.SeverlyIncreasedDamageTaken;
                case 1:
                    return Debuff.SeverlyDecreasedDuelChance;
                case 2:
                    return Debuff.SeverlyIncreasedScenarioLikelihood;
            }
            return Debuff.SeverlyDecreasedItemFind; // what should be default if not found?
        }

        //enhanced options
        private void Sabotage(InteractivePlayer contestant, StringBuilder sb)
        {
            if (RngRoll(75))
            {
                int index = _random.Next(_contestants.Count);
                while (_contestants[index].UserId == contestant.UserId)
                {
                    index = _random.Next(_contestants.Count);
                }

                bool severityFactor = RngRoll(20);
                int debuffSelection = _random.Next(3);

                _contestants[index].Debuff = ConvertToDebuff(debuffSelection, severityFactor);
                _contestants[index].DebuffTimer = severityFactor ? 3 : 5;
                sb.Append($"<{contestant.NickName}> has sabotaged <{_contestants[index].NickName}> by giving themm a {_contestants[index].Debuff} debuff for {_contestants[index].DebuffTimer} turns!\n");
            }
            else
            {
                ReduceTextCongestion($"<{contestant.NickName}> failed to sabotage someone... U trash or wut?\n", sb);
            }
        }

        private void MakeTrap(InteractivePlayer contestant, StringBuilder sb)
        {
            Logger.Log("accessed trap method");
            if (_random.Next(10) < 12)
            {
                Logger.Log("trap made");
                _traps.Add(new Trap(contestant, _random));
                sb.Append($"<{contestant.NickName}> made a Trap!\n");

            }
            else
            {
                sb.Append($"<{contestant.NickName}> has failed to make a trap.\n");
            }
        }

        private void Steal(InteractivePlayer contestant, StringBuilder sb)
        {
            if (RngRoll(35))
            {
                int index = _random.Next(_contestants.Count);
                while (_contestants[index].UserId == contestant.UserId)
                {
                    index = _random.Next(_contestants.Count);
                }
                int stealType = _random.Next(4);
                switch (stealType)
                {
                    case 0:
                        if (_contestants[index].WeaponLife > 0 && (int)contestant.WeaponRarity < (int)_contestants[index].WeaponRarity)
                        {
                            contestant.WeaponRarity = _contestants[index].WeaponRarity;
                            contestant.WeaponLife = 5;
                            _contestants[index].WeaponLife = 0;

                            sb.Append($"<{contestant.NickName}> stole <{_contestants[index].NickName}>'s * {_contestants[index].ArmourRarity} * Weapon!\n");
                        }
                        else
                        {
                            ReduceTextCongestion($"<{contestant.NickName}> tried to steal from <{_contestants[index].NickName}> but realised that his Weapon was worse than theirs. \n", sb);
                        }
                        break;
                    case 1:
                        if (_contestants[index].ArmourLife > 0 && (int)contestant.ArmourRarity < (int)_contestants[index].ArmourRarity)
                        {
                            contestant.ArmourRarity = _contestants[index].ArmourRarity;
                            contestant.ArmourLife = 5;
                            _contestants[index].ArmourLife = 0;
                            sb.Append($"<{contestant.NickName}> stole <{_contestants[index].NickName}>'s * {_contestants[index].ArmourRarity} * Armour!\n");
                        }
                        else
                        {
                            ReduceTextCongestion($"<{contestant.NickName}> tried to steal from <{_contestants[index].NickName}> but realised that his armour was worse than theirs. \n", sb);
                        }
                        break;
                    case 2:
                        if (_contestants[index].OffhandLife > 0 && (int)contestant.OffhandRarity < (int)_contestants[index].OffhandRarity)
                        {
                            contestant.OffhandRarity = _contestants[index].OffhandRarity;
                            contestant.OffhandLife = 5;
                            _contestants[index].OffhandLife = 0;
                            sb.Append($"<{contestant.NickName}> stole <{_contestants[index].NickName}>'s * {_contestants[index].OffhandRarity} * Offhand!\n");
                        }
                        else
                        {
                            ReduceTextCongestion($"<{contestant.NickName}> tried to steal from <{_contestants[index].NickName}> but realised that his Offhand was worse than theirs. \n", sb);
                        }
                        break;
                    case 3:
                        if (_contestants[index].HelmetLife > 0 && (int)contestant.HelmetRarity < (int)_contestants[index].HelmetRarity)
                        {
                            contestant.HelmetRarity = _contestants[index].HelmetRarity;
                            contestant.HelmetLife = 5;
                            _contestants[index].HelmetLife = 0;
                            sb.Append($"<{contestant.NickName}> stole <{_contestants[index].NickName}>'s * {_contestants[index].HelmetRarity} * Helmet!\n");
                        }
                        else
                        {
                            ReduceTextCongestion($"<{contestant.NickName}> tried to steal from <{_contestants[index].NickName}> but realised that his Helmet was worse than theirs. \n", sb);
                        }
                        break;
                }
            }
            else
            {
                ReduceTextCongestion($"<{contestant.NickName}> failed to steal something... git gud ¯\\_(ツ)_/¯\n", sb);
            }
        }

        private bool RngRoll(int a)
        {
            int chance = a * 10;
            int roll = _random.Next(0, 1000);
            return roll <= chance;
        }

        private void ReduceTextCongestion(string text, StringBuilder sb)
        {
            if (_contestants.Count < 26)
            {
                sb.Append(text);
            }
        }

        public void HandlePlayerInput(ulong userId, string reactionName)
        {
            if (_ignoreReactions) return;

            if (_enhancedOptions)
            {
                foreach (InteractivePlayer player in _enchancedPlayers)
                {
                    if (player.UserId == userId)
                    {
                        switch (reactionName)
                        {
                            case "💣":
                                player.EnhancedDecision = EnhancedDecision.MakeATrap;
                                break;
                            case "🔫":
                                player.EnhancedDecision = EnhancedDecision.Steal;
                                break;
                            case "🔧":
                                player.EnhancedDecision = EnhancedDecision.Sabotage;
                                break;
                        }
                    }
                }
            }
            else
            {
                var authenticPlayer = _contestants.FirstOrDefault(contestant => contestant.UserId == userId);
                if (authenticPlayer != null)
                {
                    switch (reactionName)
                    {
                        case "💰":
                            authenticPlayer.InteractiveDecision = InteractiveDecision.Loot;
                            break;
                        case "❗":
                            authenticPlayer.InteractiveDecision = InteractiveDecision.StayOnAlert;
                            break;
                        case "⚔":
                            authenticPlayer.InteractiveDecision = InteractiveDecision.ImmuneToDuel;
                            break;
                    }
                }
            }
        }
    }
}
