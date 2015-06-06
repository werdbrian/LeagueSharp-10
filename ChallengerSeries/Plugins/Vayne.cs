using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private static Vector3 condemnEndPos;
        private static Vector3 condemnEndPosSimplified;
        private static bool TumbleToKillSecondMinion;

        protected override void InitMenu()
        {
            base.InitMenu();
            ComboMenu.AddItem(new MenuItem("QCombo", "Auto Tumble").SetValue(true));
            ComboMenu.AddItem(new MenuItem("ECombo", "Auto Condemn").SetValue(true));
            ComboMenu.AddItem(new MenuItem("InsecE", "Insec Condemn").SetValue(true));
            ComboMenu.AddItem(new MenuItem("PradaE", "Authentic Prada Condemn").SetValue(true));
            ComboMenu.AddItem(new MenuItem("DrawE", "Draw Condemn Prediction").SetValue(true));
            ComboMenu.AddItem(new MenuItem("RCombo", "Auto Ult").SetValue(true));
            EscapeMenu.AddItem(new MenuItem("QEscape", "Escape with Q").SetValue(true));
            EscapeMenu.AddItem(new MenuItem("CondemnEscape", "Escape with E").SetValue(true));
            EscapeMenu.AddItem(new MenuItem("EInterrupt", "Use E to Interrupt").SetValue(true));
            LaneClearMenu.AddItem(new MenuItem("QFarm", "Use Q (SMART)").SetValue(true));
        }

        protected override void InitSpells()
        {
            Q = new Spell(SpellSlot.Q, 300f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 550f);
            R = new Spell(SpellSlot.R);
        }

        private Obj_AI_Hero GetTarget()
        {
            var attackableHeroes = HeroManager.Enemies.FindAll(h => h.Distance(Player.ServerPosition) < Player.AttackRange - 15);
            if (Items.HasItem((int) ItemId.The_Bloodthirster, Player) && Player.HealthPercent < 30)
            {
                return attackableHeroes.OrderBy(h => h.Armor).FirstOrDefault();
            }
            return attackableHeroes.FirstOrDefault(h => h.VayneWStacks() == 2) ?? attackableHeroes.OrderBy(h => h.Health).FirstOrDefault();
        }

        protected override void OnUpdate(EventArgs args)
        {
            if (E.IsReady())
            {
                Condemn();
            }
            base.OnUpdate(args);
        }

        protected override void Combo()
        {
            if (Player.CountEnemiesInRange(1000) == 0) return;
            base.Combo();
            //heal
            var minionsInRange = MinionManager.GetMinions(Player.ServerPosition, Player.AttackRange).OrderBy(m => m.Armor).ToList();
            if (Player.CountEnemiesInRange(900) == 0 && Items.HasItem((int)ItemId.The_Bloodthirster, Player) && minionsInRange.Count != 0 && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Player.HealthPercent < 60)
            {
                Orbwalker.ForceTarget(minionsInRange.FirstOrDefault());
            }

            var target = GetTarget();
            if (target != null)
            {
                Orbwalker.ForceTarget(target);
            }
        }


        private void Condemn()
        {
            if (!ComboMenu.Item("ECombo").GetValue<bool>()) return;
            if (ComboMenu.Item("PradaE").GetValue<bool>())
            {
                foreach (var hero in HeroManager.Enemies.Where(h => Player.Distance(h.ServerPosition) < E.Range))
                {
                    if (hero.HasBuffOfType(BuffType.SpellShield)) return;
                    condemnEndPosSimplified = hero.ServerPosition.To2D()
                        .Extend(Player.ServerPosition.To2D(), -420).To3D();
                    for (var i = 420; i > 0; i -= 70)
                    {
                        condemnEndPos = hero.ServerPosition.To2D().Extend(Player.ServerPosition.To2D(), -i).To3D();
                        if (condemnEndPos.IsCollisionable())
                        {
                            E.Cast(hero);
                            if (NavMesh.IsWallOfGrass(condemnEndPos, 250))
                            {
                                var blueTrinket = ItemId.Scrying_Orb_Trinket;
                                if (Items.HasItem((int) ItemId.Farsight_Orb_Trinket, Player)) blueTrinket = ItemId.Farsight_Orb_Trinket;

                                var yellowTrinket = ItemId.Warding_Totem_Trinket;
                                if (Items.HasItem((int)ItemId.Greater_Stealth_Totem_Trinket, Player)) yellowTrinket = ItemId.Greater_Stealth_Totem_Trinket;

                                if (Items.CanUseItem((int) blueTrinket))
                                    Items.UseItem((int) blueTrinket, condemnEndPos);
                                if (Items.CanUseItem((int) yellowTrinket))
                                    Items.UseItem((int) yellowTrinket, condemnEndPos);
                            }
                            return;
                        }
                    }
                }
            }
            else
            {
                foreach (var hero in
                        from hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(550f))
                        let prediction = E.GetPrediction(hero)
                        where
                            prediction.UnitPosition.To2D()
                                    .Extend(
                                        ObjectManager.Player.ServerPosition.To2D(),
                                        -420)
                                    .To3D().IsCollisionable() ||
                                prediction.UnitPosition.To2D()
                                    .Extend(
                                        ObjectManager.Player.ServerPosition.To2D(),
                                        -420)
                                    .To3D().IsCollisionable()
                        select hero)
                {
                    E.Cast(hero);
                }
            }

            if (ComboMenu.Item("InsecE").GetValue<bool>())
            {
                if (ShouldSaveCondemn()) return;
                if (Player.CountEnemiesInRange(1000) >= Player.CountAlliesInRange(1000)) return;
                foreach (var hero in HeroManager.Enemies.Where(h => Player.Distance(h.ServerPosition) < E.Range))
                {
                    //he's not a danger for me and he's not trying to run away either.
                    if (!hero.IsValidTarget(E.Range)) break;
                    if (hero.HealthPercent > 75) break;
                    if (!hero.IsFacing(Player)) break;
                    //k I suspect he's trying to escape a gank, let's ruin his fun (devil)
                    var prediction = E.GetPrediction(hero);
                    var predictedEPos = prediction.UnitPosition.Extend(Player.ServerPosition, -(E.Range));
                    if (predictedEPos.CountAlliesInRange(100) >= 2 || predictedEPos.UnderTurret(Player.Team))
                    {
                        E.Cast(hero);
                    }
                }
            }
        }

        protected override void OnDraw(EventArgs args)
        {
            base.OnDraw(args);
            if (!ComboMenu.Item("DrawE").GetValue<bool>()) return;
            if (!E.IsReady() || condemnEndPosSimplified == null || Player.CountEnemiesInRange(E.Range) == 0) return;
            if (condemnEndPos.IsCollisionable())
            {
                Geometry.Util.DrawLineInWorld(Player.ServerPosition, condemnEndPosSimplified, 2, Color.Gold);
                Drawing.DrawCircle(condemnEndPos, 70, Color.Gold);
            }
            else
            {
                Geometry.Util.DrawLineInWorld(Player.ServerPosition, condemnEndPosSimplified, 2, Color.White);
                Drawing.DrawCircle(condemnEndPos, 70, Color.White);
            }
        }

        protected override void OnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            //how to cockblock kalista 101 nigga
            if (sender.BaseSkinName.Equals("Kalista") && args.Order == GameObjectOrder.AutoAttack && args.Target.IsMe &&
                HasUltiBuff())
            {
                Q.Cast(Game.CursorPos);
            }
            if (Player.HealthPercent > 45 && Player.ManaPercent > 60 && sender.IsValid<Obj_AI_Hero>() && sender.IsEnemy && args.Target is Obj_AI_Minion &&
                sender.Distance(Player.ServerPosition) < Player.AttackRange + 250)
            {
                if (sender.InAArange())
                {
                    Orbwalker.ForceTarget(sender);
                }
                else
                {
                    var tumblePos = Player.ServerPosition.Extend(sender.ServerPosition,
                        Player.Distance(sender.ServerPosition) - Player.AttackRange + 75);

                    if (!tumblePos.IsShroom())
                    {
                        Q.Cast(tumblePos);
                        Orbwalker.ForceTarget(sender);
                    }
                }
            }
            if (sender.IsMe && args.Order == GameObjectOrder.AutoAttack &&
                HasTumbleBuff() && Player.CountEnemiesInRange(Player.AttackRange - 25) >= 1 &&
                !(args.Target is Obj_AI_Hero))
            {
                args.Process = false;
                Orbwalker.ForceTarget(GetTarget());
            }
        }

        protected override void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (!args.Unit.IsMe) return;
            if (!(args.Target is Obj_AI_Hero)) return;
            if (args.Target.IsValid<Obj_AI_Hero>())
            {
                var t = (Obj_AI_Hero)args.Target;
                if (t.IsMelee() && t.IsFacing(Player) && t != null)
                {
                    if (t.Distance(Player.ServerPosition) < Q.Range && Q.IsReady() && t.IsFacing(Player) && !Player.ServerPosition.Extend(t.ServerPosition, -(Q.Range)).IsShroom())
                    {
                        args.Process = false;
                        Q.Cast(Player.ServerPosition.Extend(t.ServerPosition, -(Q.Range)));
                    }
                }
            }
            if (Items.HasItem((int)ItemId.Thornmail, (Obj_AI_Hero)args.Target) &&
                !Items.HasItem((int)ItemId.The_Bloodthirster, Player) && Player.HealthPercent < 25 &&
                args.Target.HealthPercent > 15)
            {
                args.Process = false;
                var minionsInRange = MinionManager.GetMinions(Player.ServerPosition, Player.AttackRange).OrderBy(m => m.Armor).ToList();
                Orbwalker.ForceTarget(minionsInRange.FirstOrDefault());
            }
        }

        protected override void OnAttack(AttackableUnit sender, AttackableUnit target)
        {
            base.OnAttack(sender, target);
            if (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LaneClear) return;
            TumbleToKillSecondMinion = MinionManager.GetMinions(Player.Position, 550).Any(m => m.Health < Player.GetAutoAttackDamage(m));
        }

        protected override void AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (Player.ManaPercent > 70 && target is Obj_AI_Hero && unit.IsMe && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                var t = (Obj_AI_Hero) target;
                if (Player.CountAlliesInRange(1000) >= Player.CountEnemiesInRange(1000) && !t.InAArange())
                {
                    if (t.IsKillable())
                    {
                        Orbwalker.ForceTarget(t);
                    }

                    var tumblePos = Player.ServerPosition.Extend(t.ServerPosition, Player.Distance(t.ServerPosition) - Player.AttackRange + 55);
                    if (!tumblePos.IsShroom())
                    {
                        Q.Cast(tumblePos);
                        Orbwalker.ForceTarget(t);
                    }
                }
            }
            if (Player.CountEnemiesInRange(1000) > 0 || Player.ManaPercent < 70)
            {
                TumbleToKillSecondMinion = false;
                return;
            }
            if (LaneClearMenu.Item("QFarm").GetValue<bool>() && Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear &&
                TumbleToKillSecondMinion)
            {
                Q.Cast(Game.CursorPos);
                TumbleToKillSecondMinion = false;
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
            if (ShouldSaveCondemn()) return;
            if (!sender.IsMelee() || !args.Target.IsMe || !sender.IsValid<Obj_AI_Hero>() || !sender.IsEnemy || args.SData == null)
                return;
            //how to milk alistar/thresh/everytoplaner
            try
            {
                var spellData = SpellDb.GetByName(args.SData.Name);
                if (spellData == null) return;
                if (spellData.CcType != CcType.No)
                {
                    if (E.CanCast(sender))
                    {
                        E.Cast(sender);
                    }
                    else if (sender.ServerPosition.UnderTurret(Player.Team) && Player.UnderTurret())
                    {
                        var exhaust = Player.GetSpellSlot("summonerexhaust");
                        if (exhaust != SpellSlot.Unknown)
                        {
                            Player.Spellbook.CastSpell(exhaust, sender);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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
            if (Lists.CancerChamps.Contains(gapcloser.Sender.BaseSkinName) && E.IsReady())
            {
                E.Cast(gapcloser.Sender);
            }
            #endregion

            #region Tumble to ally to peel for you
            var closestAlly = HeroManager.Allies.FirstOrDefault(a => a.Distance(Player) < 750);
            if (closestAlly != null && Q.IsReady())
            {
                Q.Cast(Player.ServerPosition.Extend(closestAlly.ServerPosition, Q.Range));
            }
            #endregion

            #region AntiGC E for LowHealth Escape
            if (Player.HealthPercent < 30f && E.IsReady())
            {
                E.Cast(gapcloser.Sender);
            }
            #endregion
        }

        bool HasUltiBuff()
        {
            return Player.Buffs.Any(b => b.Name.Equals("VayneInquisition"));
        }

        bool HasTumbleBuff()
        {
            return Player.Buffs.Any(b => b.Name.Equals("vaynetumblebonus"));
        }

        bool ShouldSaveCondemn()
        {
            if (!HeroManager.Enemies.Any(h => h.BaseSkinName == "Katarina")) return false;
            var katarina = HeroManager.Enemies.FirstOrDefault(h => h.BaseSkinName == "Katarina");
            var kataR = katarina.GetSpell(SpellSlot.R);
            return katarina != null && katarina.IsValid<Obj_AI_Hero>() && katarina.Distance(Player.ServerPosition) < 1400 && kataR.IsReady() || (katarina.Spellbook.CanUseSpell(SpellSlot.R) != SpellState.Cooldown && katarina.Spellbook.CanUseSpell(SpellSlot.R) != SpellState.NotLearned);
        }
    }
}
