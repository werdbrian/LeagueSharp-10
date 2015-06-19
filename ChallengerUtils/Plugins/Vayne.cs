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
            SkinhackMenu.AddItem(
                new MenuItem("skin", "Skin: ").SetValue(
                    new Slider(1, 1, 9))).ValueChanged +=
                (sender, args) =>
                {
                    Player.SetSkin(Player.BaseSkinName, SkinhackMenu.Item("skin").GetValue<Slider>().Value);
                };
            SkinhackMenu.AddItem(new MenuItem("enableskinhack", "Enable Skinhax").SetValue(true));
            SkinhackMenu.AddItem(new MenuItem("cyclethroughskins", "Cycle Through Skins").SetValue(false));
            SkinhackMenu.AddItem(new MenuItem("cyclethroughskinstime", "Cycling Time").SetValue(new Slider(30, 30, 600)));
        }

        protected override void InitSpells()
        {
            Q = new Spell(SpellSlot.Q, 300f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 550f);
            R = new Spell(SpellSlot.R);
        }

        protected override void OnGameLoad(EventArgs args)
        {
            base.OnGameLoad(args);
            if (SkinhackMenu.Item("enableskinhack").GetValue<bool>())
            {
                Player.SetSkin(Player.BaseSkinName, SkinhackMenu.Item("skin").GetValue<Slider>().Value);
                _skinLoaded = true;
            }
        }

        protected override void OnUpdate(EventArgs args)
        {

            if (Player.Buffs.Any(b => b.Name.ToLower().Contains("rengarr")))
            {
                if (Items.HasItem((int)ItemId.Oracles_Lens_Trinket))
                {
                    Items.UseItem((int)ItemId.Oracles_Lens_Trinket, Player.Position);
                }
                else if (Items.HasItem((int)ItemId.Vision_Ward, Player))
                {
                    Items.UseItem((int) ItemId.Vision_Ward, Player.Position.Randomize(0, 125));
                }
            }
            
            if (Player.InFountain() && ComboMenu.Item("AutoBuy").GetValue<bool>() && Player.Level > 6 && Items.HasItem((int)ItemId.Warding_Totem_Trinket))
            {
                Player.BuyItem(ItemId.Scrying_Orb_Trinket);
            }
            if (Player.InFountain() && ComboMenu.Item("AutoBuy").GetValue<bool>() && !Items.HasItem((int)ItemId.Oracles_Lens_Trinket, Player) && Player.Level >= 9 && HeroManager.Enemies.Any(h => h.BaseSkinName == "Rengar"))
            {
                Player.BuyItem(ItemId.Oracles_Lens_Trinket);
            }
            base.OnUpdate(args);
            if (SkinhackMenu.Item("enableskinhack").GetValue<bool>())
            {
                SkinHax();
            }
        }

        private static void SkinHax()
        {
            if (Player.IsDead && _skinLoaded)
            {
                _skinLoaded = false;
            }

            if (Player.InFountain() && !Player.IsDead && !_skinLoaded &&
                SkinhackMenu.Item("enableskinhack").GetValue<bool>())
            {
                Player.SetSkin(Player.BaseSkinName, SkinhackMenu.Item("skin").GetValue<Slider>().Value);
                _skinLoaded = true;
            }

            if (SkinhackMenu.Item("cyclethroughskins").GetValue<bool>() &&
                Environment.TickCount - _cycleThroughSkinsTime >
                SkinhackMenu.Item("cyclethroughskinstime").GetValue<Slider>().Value * 1000)
            {
                if (_lastCycledSkin <= 6)
                {
                    _lastCycledSkin++;
                }
                else
                {
                    _lastCycledSkin = 1;
                }

                Player.SetSkin(Player.BaseSkinName, _lastCycledSkin);
                _cycleThroughSkinsTime = Environment.TickCount;
            }
        }

        protected override void OnDraw(EventArgs args)
        {
            base.OnDraw(args);

            if (DrawingsMenu.Item("streamingmode").GetValue<bool>())
                return;


            foreach (var hero in HeroManager.Enemies.Where(h => h.IsValidTarget() && h.Distance(Player) < 1400))
            {
                var WProcDMG = (((hero.Health / Player.GetAutoAttackDamage(hero)) / 3) - 1) * W.GetDamage(hero);
                var AAsNeeded = 0;
                if (W.Instance.State != SpellState.NotLearned)
                {
                    AAsNeeded = (int) ((hero.Health - WProcDMG)/Player.GetAutoAttackDamage(hero));
                }
                else
                {
                    AAsNeeded = (int)(hero.Health / Player.GetAutoAttackDamage(hero));
                }
                if (AAsNeeded <= 3)
                {
                    Drawing.DrawText(hero.HPBarPosition.X+5, hero.HPBarPosition.Y - 30, Color.Gold,
                        "AAs to kill: " + AAsNeeded);
                }
                else
                {
                    Drawing.DrawText(hero.HPBarPosition.X+5, hero.HPBarPosition.Y - 30, Color.White,
                        "AAs to kill: " + AAsNeeded);
                }
            }

            if (!ComboMenu.Item("DrawE").GetValue<bool>()) return;
            if (!E.IsReady() || !_condemnEndPosSimplified.IsValid() || Player.CountEnemiesInRange(E.Range) == 0) return;
            if (_condemnEndPos.IsCollisionable())
            {
                Geometry.Util.DrawLineInWorld(Player.ServerPosition, _condemnEndPosSimplified, 2, Color.Gold);
                Drawing.DrawCircle(_condemnEndPos, 70, Color.Gold);
            }
            else
            {
                Geometry.Util.DrawLineInWorld(Player.ServerPosition, _condemnEndPosSimplified, 2, Color.White);
                Drawing.DrawCircle(_condemnEndPos, 70, Color.White);
            }
        }
    }
}
