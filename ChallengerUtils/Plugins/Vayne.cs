using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using ChallengerSeries.Utils;
using SharpDX;
using Color = System.Drawing.Color;
using SpellSlot = LeagueSharp.SpellSlot;
using Orbwalking = ChallengerSeries.Utils.Orbwalking;
using Geometry = ChallengerSeries.Utils.Geometry;

namespace ChallengerSeries.Plugins
{
    internal class Vayne : ChallengerPlugin
    {
        public Vayne()
        {
            InitChallengerSeries();
        }

        private static Vector3 _condemnEndPos;
        private static Vector3 _condemnEndPosSimplified;
        private static bool _tumbleToKillSecondMinion;
        private static bool _skinLoaded = false;
        private static int _cycleThroughSkinsTime = 0;
        private static int _lastCycledSkin;

        protected override void InitMenu()
        {
            base.InitMenu();
        }


        protected override void OnGameLoad(EventArgs args)
        {
            base.OnGameLoad(args);
        }

        protected override void OnUpdate(EventArgs args)
        {

            base.OnUpdate(args);
        }

        protected override void OnDraw(EventArgs args)
        {
            base.OnDraw(args);
        }
    }
}
