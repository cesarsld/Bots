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
        }
    }
}
