using ChallengerSeries.Plugins;
using LeagueSharp;
using LeagueSharp.Common;

namespace ChallengerSeries
{
    public class Program
    {
        internal static ChallengerPlugin RunningPlugin;

        public static void Main(string[] args)
        {
            RunningPlugin = new Vayne();
        }
    }
}
