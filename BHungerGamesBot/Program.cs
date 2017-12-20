using System;
using System.Threading;
using Discord;

namespace BHungerGaemsBot
{
    class Program
    {
        static void Main()
        {
            while (true)
            { 
                try
                {
                    new Bot().RunAsync().GetAwaiter().GetResult();
                    //BHungerGames.Test();
                }
                catch (Exception ex)
                {
                    Logger.Log(new LogMessage(LogSeverity.Error, "Main", "Unexpected Exception", ex));
                }
                Thread.Sleep(1000);
            }
            //BHungerGamesV3 bh = new BHungerGamesV3();
            //bh.Run(1, new System.Collections.Generic.List<Player>(),);
        }
    }
}
