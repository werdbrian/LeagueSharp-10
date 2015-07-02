using System;
using System.Linq;
using System.Windows.Media.Media3D;
using LeagueSharp;
using LeagueSharp.Common;
using ChallengerSeries.Utils;
using SharpDX;
using Color = System.Drawing.Color;
using SpellSlot = LeagueSharp.SpellSlot;
using Orbwalking = ChallengerSeries.Utils.Orbwalking;
using Geometry = ChallengerSeries.Utils.Geometry;
using TargetSelector = LeagueSharp.Common.TargetSelector;
// ReSharper disable InconsistentNaming

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
        private static int _selectedSkin;
        private static bool _skinLoaded = false;
        private static int _cycleThroughSkinsTime = 0;
        private static int _lastCycledSkin;
        private static Vector3 _preV3 = new Vector2(12050, 4828).To3D();
        private static Vector3 _aftV3 = new Vector2(11510, 4470).To3D();

        protected override void InitMenu()
        {
            base.InitMenu();
            ComboMenu.AddItem(new MenuItem("QCombo", "Auto Tumble").SetValue(true));
            ComboMenu.AddItem(new MenuItem("QHarass", "AA - Q - AA").SetValue(true));
            ComboMenu.AddItem(new MenuItem("QChecks", "Q Safety Checks").SetValue(true));
            ComboMenu.AddItem(new MenuItem("QWall", "Enable Wall Tumble?").SetValue(true));
            ComboMenu.AddItem(new MenuItem("QUltSpam", "Spam Q when R active").SetValue(false));
            ComboMenu.AddItem(new MenuItem("FocusTwoW", "Focus 2 W Stacks").SetValue(true));
            ComboMenu.AddItem(new MenuItem("ECombo", "Auto Condemn").SetValue(true));
            ComboMenu.AddItem(new MenuItem("PradaE", "Authentic Prada Condemn").SetValue(true));
            ComboMenu.AddItem(new MenuItem("EHitchance", "E % Hitchance").SetValue(new Slider(100, 50, 100)));
            ComboMenu.AddItem(new MenuItem("DrawE", "Draw Condemn Prediction").SetValue(true));
            ComboMenu.AddItem(new MenuItem("RCombo", "Auto Ult (soon)").SetValue(false));
            ComboMenu.AddItem(new MenuItem("AutoBuy", "Auto-Swap Trinkets?").SetValue(true));
            EscapeMenu.AddItem(new MenuItem("QEscape", "Escape with Q").SetValue(true));
            EscapeMenu.AddItem(new MenuItem("CondemnEscape", "Escape with E").SetValue(true));
            EscapeMenu.AddItem(new MenuItem("EInterrupt", "Use E to Interrupt").SetValue(true));
            LaneClearMenu.AddItem(new MenuItem("QFarm", "Use Q (SMART)").SetValue(true));
            SkinhackMenu.AddItem(
                new MenuItem("skin", "Skin: ").SetValue(
                    new StringList(new string[]
                    {"Classic", "Vindicator", "Aristocrat", "Dragonslayer", "Heartseeker", "SKT T1", "Arclight"}))).ValueChanged +=
                (sender, args) =>
                {
                    _selectedSkin = SkinhackMenu.Item("skin").GetValue<StringList>().SelectedIndex + 1;
                    Player.SetSkin(Player.CharData.BaseSkinName, _selectedSkin);
                };
            SkinhackMenu.AddItem(new MenuItem("enableskinhack", "Enable Skinhax").SetValue(true));
            SkinhackMenu.AddItem(new MenuItem("cyclethroughskins", "Cycle Through Skins").SetValue(false));
            SkinhackMenu.AddItem(new MenuItem("cyclethroughskinstime", "Cycling Time").SetValue(new Slider(30, 30, 600)));
        }

        protected override void InitSpells()
        {
            Q = new Spell(SpellSlot.Q, 300f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 545f);
            R = new Spell(SpellSlot.R);
        }

        protected override void OnGameLoad(EventArgs args)
        {
            base.OnGameLoad(args);
            if (SkinhackMenu.Item("enableskinhack").GetValue<bool>())
            {
                _selectedSkin = SkinhackMenu.Item("skin").GetValue<StringList>().SelectedIndex + 1;
                Player.SetSkin(Player.CharData.BaseSkinName, _selectedSkin);
                _skinLoaded = true;
            }
        }

        protected override void OnUpdate(EventArgs args)
        {
            if (E.IsReady())
            {
                Condemn();
            }

            if (SkinhackMenu.Item("enableskinhack").GetValue<bool>())
            {
                SkinHax();
            }

            if (ComboMenu.Item("QWall").GetValue<bool>() && Q.IsReady() && Player.Distance(_preV3) < 500 && LeagueSharp.Common.Utility.Map.GetMap().Type == LeagueSharp.Common.Utility.Map.MapType.SummonersRift)
            {
                WallTumble();
            }

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
            if (Player.InFountain() && ComboMenu.Item("AutoBuy").GetValue<bool>() && !Items.HasItem((int)ItemId.Oracles_Lens_Trinket, Player) && Player.Level >= 9 && HeroManager.Enemies.Any(h => h.CharData.BaseSkinName == "Rengar" || h.CharData.BaseSkinName == "Talon"))
            {
                Player.BuyItem(ItemId.Oracles_Lens_Trinket);
            }
            base.OnUpdate(args);
        }

        private static void WallTumble()
        {
            if (Player.Distance(_preV3) < 100)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, _preV3);
            }
            if (Player.Distance(_preV3) < 5)
            {
                Orbwalker.SetMovement(false);
                Q.Cast(_aftV3);
                Utility.DelayAction.Add(100, () =>
                {
                    Player.IssueOrder(GameObjectOrder.MoveTo, _aftV3);
                    Orbwalker.SetMovement(true);
                });
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
                Player.SetSkin(Player.CharData.BaseSkinName, _selectedSkin);
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

                Player.SetSkin(Player.CharData.BaseSkinName, _lastCycledSkin);
                _cycleThroughSkinsTime = Environment.TickCount;
            }
        }

        protected override void Combo()
        {
            var minionsInRange = MinionManager.GetMinions(Player.ServerPosition, Orbwalking.GetRealAutoAttackRange(null)).OrderBy(m => m.Armor).ToList();
            if (Player.CountEnemiesInRange(600) == 0 && Items.HasItem((int)ItemId.The_Bloodthirster, Player) && minionsInRange.Count != 0 && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Player.HealthPercent < 60)
            {
                Orbwalker.ForceTarget(minionsInRange.FirstOrDefault());
            }

            if (Player.CountEnemiesInRange(1000) == 0) return;
            base.Combo();
        }


        private void Condemn()
        {
            if (!ComboMenu.Item("ECombo").GetValue<bool>()) return;
            if (ShouldSaveCondemn() || !E.IsReady() ||
                (Player.UnderTurret(true) && Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)) return;
            var condemnTargets =
                HeroManager.Enemies.Where(
                    h => Player.Distance(h.ServerPosition) < E.Range && !h.HasBuffOfType(BuffType.SpellShield));

            if ((ComboMenu.Item("PradaE").GetValue<bool>()))
            {
                foreach (var hero in condemnTargets)
                {
                    var pushDist = Player.ServerPosition.Distance(hero.ServerPosition) + 395;
                    if (hero.IsDashing())
                    {
                        if (Player.ServerPosition.Extend(hero.GetDashInfo().EndPos.To3D(), -400).IsCollisionable())
                        {
                            E.Cast(hero);
                        }
                        break;
                    }

                    _condemnEndPosSimplified = Player.ServerPosition.To2D()
                        .Extend(hero.ServerPosition.To2D(), pushDist).To3D();

                    _condemnEndPos = Player.ServerPosition.To2D()
                        .Extend(hero.ServerPosition.To2D(), pushDist).To3D();

                    if (_condemnEndPos.IsCollisionable())
                    {
                        if (!hero.CanMove || hero.GetWaypoints().Count <= 1 || !hero.IsMoving)
                        {
                            E.Cast(hero);
                            return;
                        }

                        var wayPoints = hero.GetWaypoints();
                        var wCount = ((ComboMenu.Item("EHitchance").GetValue<Slider>().Value) / 100) * wayPoints.Count;

                        if (wayPoints.Count(w => Player.ServerPosition.Extend(w.To3D(), pushDist).IsCollisionable()) >=
                            wCount)
                        {
                            E.Cast(hero);
                            return;
                        }

                        /*if (Geometry.PositionAfter(wayPoints, 463, (int) hero.MoveSpeed)
                                .To3D()
                                .IsCollisionable())
                            {
                                E.Cast(hero);
                            }*/
                    }
                }
            }
        }

        protected override void OnDraw(EventArgs args)
        {
            base.OnDraw(args);

            if (DrawingsMenu.Item("streamingmode").GetValue<bool>())
                return;
            if (Player.Distance(_preV3) < 1000 && LeagueSharp.Common.Utility.Map.GetMap().Type == LeagueSharp.Common.Utility.Map.MapType.SummonersRift)
            Drawing.DrawCircle(_preV3, 75, Color.Gold);

            foreach (var hero in HeroManager.Enemies.Where(h => h.IsValidTarget() && h.Distance(Player) < 1400))
            {
                var AAsNeeded = (int)(hero.Health/Player.GetAutoAttackDamage(hero));
                Drawing.DrawText(hero.HPBarPosition.X + 5, hero.HPBarPosition.Y - 30,
                    AAsNeeded <= 3 ? Color.Gold : Color.White,
                    "AAs to kill: " + AAsNeeded);
            }

            if (!ComboMenu.Item("DrawE").GetValue<bool>() || (Player.UnderTurret(true) && Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)) return;
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

        protected override void OnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            if (HasUltiBuff() && Q.IsReady() && sender.CharData.BaseSkinName.Equals("Kalista") && args.Order == GameObjectOrder.AutoAttack && args.Target.IsMe)
            {
                if (ComboMenu.Item("QChecks").GetValue<bool>() && Game.CursorPos.IsShroom()) return;
                Q.Cast(Game.CursorPos);
            }
            if (Player.HealthPercent > 45 && Player.ManaPercent > 60 && Player.CountEnemiesInRange(1000) <= 2 && sender.IsValid<Obj_AI_Hero>() && sender.IsEnemy && args.Target is Obj_AI_Minion &&
                sender.Distance(Player.ServerPosition) < Orbwalking.GetRealAutoAttackRange(null) + 250)
            {
                if (sender.InAArange())
                {
                    Orbwalker.ForceTarget(sender);
                }
                else
                {
                    var tumblePos = Player.ServerPosition.Extend(sender.ServerPosition,
                        Player.Distance(sender.ServerPosition) - Orbwalking.GetRealAutoAttackRange(null));

                    if (!tumblePos.IsShroom() && tumblePos.CountEnemiesInRange(300) == 0 && Q.IsReady())
                    {
                        Q.Cast(tumblePos);
                        Orbwalker.ForceTarget(sender);
                    }
                }
            }
        }

        protected override void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (!args.Unit.IsMe) return;

            var realTarget = Utils.TargetSelector.GetTarget(Orbwalking.GetRealAutoAttackRange(null), TargetSelector.DamageType.Physical);
            if (args.Target.Type == GameObjectType.obj_AI_Minion && !Orbwalker.ShouldWait() && realTarget.IsValidTarget())
            {
                Orbwalker.ForceTarget(realTarget);
                return;
            }
            if (args.Target.IsValid<Obj_AI_Hero>())
            {
                var t = (Obj_AI_Hero)args.Target;
                if (Q.IsReady() && t.IsValidTarget() && t.IsMelee() && t.IsFacing(Player) && ComboMenu.Item("QCombo").GetValue<bool>())
                {
                    if (t.Distance(Player.ServerPosition) < Q.Range && t.IsFacing(Player) && !Player.ServerPosition.Extend(t.ServerPosition, -(Q.Range)).IsShroom())
                    {
                        args.Process = false;
                        Q.Cast(Player.ServerPosition.Extend(t.ServerPosition, -(Q.Range)));
                    }
                }

                var minion = MinionManager.GetMinions(Player.ServerPosition, Orbwalking.GetRealAutoAttackRange(null)).OrderBy(m => m.Armor).FirstOrDefault();
                if (minion == null) return;

                if (Items.HasItem((int)ItemId.Thornmail, t) &&
                    !Items.HasItem((int)ItemId.The_Bloodthirster, Player) && Player.HealthPercent < 25 &&
                    t.HealthPercent > 15 && t.VayneWStacks() != 2 && t.IsFacing(Player))
                {
                    Orbwalker.ForceTarget(minion);
                }
            }
        }

        protected override void OnAttack(AttackableUnit sender, AttackableUnit target)
        {
            base.OnAttack(sender, target);

            if (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LaneClear) return;
            _tumbleToKillSecondMinion = MinionManager.GetMinions(Player.Position, Orbwalking.GetRealAutoAttackRange(null)).Any(m => m.Health < Player.GetAutoAttackDamage(m) + 15);
        }

        protected override void AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe) return;
            var myRange = Orbwalking.GetRealAutoAttackRange(null);
            var AArange = myRange + 15;
            var tg = (Obj_AI_Hero)target;
            var realTarget = Utils.TargetSelector.GetTarget(AArange, TargetSelector.DamageType.Physical);
            if (target.Type == GameObjectType.obj_AI_Hero && tg != realTarget && realTarget.IsValidTarget())
            {
                Orbwalker.ForceTarget(realTarget);
            }

            if (tg.IsValidTarget() && E.IsReady() && tg.VayneWStacks() == 2 && tg.Health < Player.GetSpellDamage(tg, SpellSlot.W))
            {
                E.Cast(tg);
            }

            if (!Q.IsReady())
            {
                if (_tumbleToKillSecondMinion)
                {
                    _tumbleToKillSecondMinion = false;
                }
                return;
            }

            if (target.Type == GameObjectType.obj_AI_Minion && !Orbwalker.ShouldWait() && realTarget.IsValidTarget())
            {
                Orbwalker.ForceTarget(realTarget);
                return;
            }

            if (ComboMenu.Item("QChecks").GetValue<bool>() && Game.CursorPos.IsShroom()) return;

            if (Player.CountEnemiesInRange(1400) == 0 && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear && !Orbwalker.ShouldWait() && Player.ManaPercent > 70 && LaneClearMenu.Item("QFarm").GetValue<bool>() && MinionManager.GetMinions(Game.CursorPos, myRange).Any())
            {
                if (Game.CursorPos.UnderTurret(Player.Team == GameObjectTeam.Order
                                  ? GameObjectTeam.Chaos
                                  : GameObjectTeam.Order) && !Player.UnderTurret(true)) return;
                Q.Cast(Game.CursorPos);
            }
            if (Player.ManaPercent > 25 && realTarget.IsValidTarget() && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                if (Player.CountAlliesInRange(1000) >= Player.CountEnemiesInRange(1000) && realTarget.Distance(Game.CursorPos) < Orbwalking.GetRealAutoAttackRange(null))
                {
                    Q.Cast(Game.CursorPos);
                }
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear ||
                Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                if (ComboMenu.Item("QHarass").GetValue<bool>() && Game.CursorPos.Distance(target.Position) < myRange && Player.CountEnemiesInRange(1000) <= 2 && Player.Level < 11)
                {
                    var pos = Player.Position.Extend(Game.CursorPos,
                        Player.Distance(target.Position) - myRange + 15);
                    if (
                        pos.UnderTurret(Player.Team == GameObjectTeam.Order
                            ? GameObjectTeam.Chaos
                            : GameObjectTeam.Order) && !Player.UnderTurret(true)) return;
                    Q.Cast(pos);
                }
            }

            if (!ComboMenu.Item("QCombo").GetValue<bool>()) return;
            if (Player.ManaPercent > 25)
            {
                if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                {
                    Q.Cast(Game.CursorPos);
                }
            }
        }

        protected override void OnPossibleToInterrupt(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (args.DangerLevel == Interrupter2.DangerLevel.High && E.IsReady() && E.IsInRange(sender))
            {
                E.Cast(sender);
            }
        }

        protected override void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            #region ward brush after condemn
            if (sender.IsMe && args.SData.Name.ToLower().Contains("condemn"))
            {
                if (NavMesh.IsWallOfGrass(_condemnEndPos, 150))
                {
                    var blueTrinket = ItemId.Scrying_Orb_Trinket;
                    if (Items.HasItem((int)ItemId.Farsight_Orb_Trinket, Player)) blueTrinket = ItemId.Farsight_Orb_Trinket;

                    var yellowTrinket = ItemId.Warding_Totem_Trinket;
                    if (Items.HasItem((int)ItemId.Greater_Stealth_Totem_Trinket, Player)) yellowTrinket = ItemId.Greater_Stealth_Totem_Trinket;

                    if (Items.CanUseItem((int)blueTrinket))
                        Items.UseItem((int)blueTrinket, _condemnEndPos.Randomize(0, 150));
                    if (Items.CanUseItem((int)yellowTrinket))
                        Items.UseItem((int)yellowTrinket, _condemnEndPos.Randomize(0, 150));
                }
            }
            #endregion

            if (ShouldSaveCondemn()) return;
            if (sender.Distance(Player) > 700 || !sender.IsMelee() || !args.Target.IsMe || sender.Distance(Player) > 700 ||
                !sender.IsValid<Obj_AI_Hero>() || !sender.IsEnemy || args.SData == null)
                return;
            //how to milk alistar/thresh/everytoplaner
            var spellData = SpellDb.GetByName(args.SData.Name);
            if (spellData != null)
            {
                if (spellData.CcType == CcType.Knockup || spellData.CcType == CcType.Stun)
                {
                    if (E.CanCast(sender))
                    {
                        E.Cast(sender);
                    }
                }
            }

            if (args.SData.Name.ToLower().Contains("talonshadow"))
            {
                if (Items.HasItem((int)ItemId.Oracles_Lens_Trinket))
                {
                    Items.UseItem((int)ItemId.Oracles_Lens_Trinket, Player.Position);
                }
                else if (Items.HasItem((int)ItemId.Vision_Ward, Player))
                {
                    Items.UseItem((int)ItemId.Vision_Ward, Player.Position.Randomize(0, 125));
                }
            }
        }

        protected override void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            //KATARINA IN GAME. CONCERN!
            if (ShouldSaveCondemn()) return;
            //we wanna check if the mothafucka can actually do shit to us.
            if (Player.Distance(gapcloser.End) > gapcloser.Sender.AttackRange) return;
            //ok we're no pussies, we don't want to condemn the unsuspecting akali when we can jihad her.
            if (Player.Level > gapcloser.Sender.Level + 1) return;
            //k so that's not the case, we're going to check if we should condemn the gapcloser away.

            #region If it's a cancer champ, condemn without even thinking, lol
            if (Lists.CancerChamps.Contains(gapcloser.Sender.CharData.BaseSkinName) && E.IsReady())
            {
                E.Cast(gapcloser.Sender);
            }
            #endregion

            #region Tumble to ally to peel for you
            var closestAlly = HeroManager.Allies.FirstOrDefault(a => a.Distance(Player) < 750);
            if (closestAlly != null && Q.IsReady())
            {
                var tumblePos = Player.ServerPosition.Extend(closestAlly.ServerPosition, Q.Range);
                if (!tumblePos.IsShroom() && tumblePos.CountEnemiesInRange(300) == 0 && Q.IsReady())
                {
                    Q.Cast(tumblePos);
                }
            }
            #endregion
        }

        bool HasUltiBuff()
        {
            return Player.Buffs.Any(b => b.Name.ToLower().Contains("vayneinquisition"));
        }

        bool HasTumbleBuff()
        {
            return Player.Buffs.Any(b => b.Name.ToLower().Contains("vaynetumblebonus"));
        }

        bool ShouldSaveCondemn()
        {
            if (HeroManager.Enemies.Any(h => h.CharData.BaseSkinName == "Katarina" && h.Distance(Player) < 1400 && !h.IsDead && h.IsValidTarget()))
            {
                var katarina = HeroManager.Enemies.FirstOrDefault(h => h.CharData.BaseSkinName == "Katarina");
                var kataR = katarina.GetSpell(SpellSlot.R);
                if (katarina != null)
                {
                    return katarina.IsValid<Obj_AI_Hero>() && kataR.IsReady() ||
                           (katarina.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Ready);
                }
            }
            if (HeroManager.Enemies.Any(h => h.CharData.BaseSkinName == "Galio" && h.Distance(Player) < 1400 && !h.IsDead && h.IsValidTarget()))
            {
                var galio = HeroManager.Enemies.FirstOrDefault(h => h.CharData.BaseSkinName == "Galio");
                if (galio != null)
                {
                    var galioR = galio.GetSpell(SpellSlot.R);
                    return galio.IsValidTarget() && galioR.IsReady() ||
                           (galio.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Ready);
                }
            }
            return false;
        }
    }
}
