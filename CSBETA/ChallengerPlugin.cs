using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using ChallengerSeries.Utils;
using SharpDX;
using Color = System.Drawing.Color;
using Orbwalking = ChallengerSeries.Utils.Orbwalking;

namespace ChallengerSeries
{
    internal abstract class ChallengerPlugin
    {
        private static int _lastUpdate;
        /// <summary>
        /// add me in your plugin's constructor :3
        /// </summary>
        protected void InitChallengerSeries()
        {
            InitMenu();
            InitOrbwalker();
            FinishMenuInit();
            InitSpells();
            CustomEvents.Game.OnGameLoad += OnGameLoad;
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += OnPossibleToInterrupt;
            Orbwalking.BeforeAttack += BeforeAttack;
            Orbwalking.OnAttack += OnAttack;
            Orbwalking.AfterAttack += AfterAttack;
            Orbwalking.OnNonKillableMinion += OnNonKillableMinion;
            Obj_AI_Base.OnIssueOrder += OnIssueOrder;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
        }

        /// <summary>
        /// insert all menu related stuff here XD
        /// </summary>
        protected virtual void InitMenu()
        {
            MainMenu = new Menu("Challenger " + ObjectManager.Player.BaseSkinName, "challengermenu", true);
            ComboMenu = new Menu("Combo Settings", "combomenu");
            LaneClearMenu = new Menu("Laneclear Settings", "laneclearmenu");
            EscapeMenu = new Menu("Escape Settings", "escapemenu");

            ActivatorMenu = new Menu("CK Activator", "activatormenu");
            Activator = new Utils.Activator(ActivatorMenu);

            ManaManagementMenu = new Menu("Mana Management", "manamanagementmenu");
            ManaManagementMenu.AddItem(
                new MenuItem("activateonmanapercent", "Activate on % Mana: ").SetValue(new Slider(30, 15, 60)));
            DrawingsMenu = new Menu("Drawing Settings", "drawingsmenu");
            DrawingsMenu.AddItem(new MenuItem("streamingmode", "Disable All Drawings").SetValue(false));
            DrawingsMenu.AddItem(new MenuItem("drawenemyrangecircle", "Draw Enemy Spells Range").SetValue(false));
            DrawingsMenu.AddItem(new MenuItem("enemycccounter", "Enemy CC Counter: ").SetShared().SetValue(Positioning.EnemyCC()));
            DrawingsMenu.AddItem(new MenuItem("enemycounter",
                "Enemies in 2000 range: ").SetShared().SetValue(0));
            DrawingsMenu.Item("enemycccounter").Permashow(true, "Enemy CC Counter");
            DrawingsMenu.Item("enemycounter").Permashow(true, "Enemies in 2000 range");
            SkinhackMenu = new Menu("Skin Hack", "skinhackmenu");
            OrbwalkerMenu = new Menu("Orbwalker", "orbwalkermenu");
        }

        /// <summary>
        /// you don't really have to touch me :P
        /// </summary>
        protected static void InitOrbwalker()
        {
            Orbwalker = new Orbwalking.Orbwalker(OrbwalkerMenu);

            //Common orbwalker messes with our orbwalker
            //so we're gonna turn it off :^ )
            LeagueSharp.Common.Orbwalking.Attack = false;
            LeagueSharp.Common.Orbwalking.Move = false;
        }

        /// <summary>
        /// you don't really have to touch me :P
        /// </summary>
        protected static void FinishMenuInit()
        {
            MainMenu.AddSubMenu(ComboMenu);
            MainMenu.AddSubMenu(LaneClearMenu);
            MainMenu.AddSubMenu(ManaManagementMenu);
            MainMenu.AddSubMenu(EscapeMenu);
            MainMenu.AddSubMenu(ActivatorMenu);
            MainMenu.AddSubMenu(SkinhackMenu); // XD
            MainMenu.AddSubMenu(DrawingsMenu);
            MainMenu.AddSubMenu(OrbwalkerMenu);
            MainMenu.AddToMainMenu();
        }

        protected virtual void InitSpells() { }

        protected virtual void OnGameLoad(EventArgs args)
        {
            const int timeonscreen = 3000;
            Notifications.AddNotification("Fashion Series by GUCCI & H&M loaded", timeonscreen);
            Utility.DelayAction.Add(timeonscreen-550, () => { Notifications.AddNotification("HF, you don't need luck ;)", timeonscreen); });
        }

        protected virtual void OnUpdate(EventArgs args)
        {
            Escape();

            if (Player.ManaPercent < ManaManagementMenu.Item("activateonmanapercent").GetValue<Slider>().Value)
            {
                LowMana();
            }

            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                {
                    Combo();
                    break;
                }
                case Orbwalking.OrbwalkingMode.LaneClear:
                {
                    LaneClear();
                    break;
                }
                case Orbwalking.OrbwalkingMode.Mixed:
                {
                    Mixed();
                    break;
                }
                case Orbwalking.OrbwalkingMode.LastHit:
                {
                    Mixed();
                    break;
                }
            }
        }

        protected virtual void OnDraw(EventArgs args)
        {
            if (DrawingsMenu.Item("streamingmode").GetValue<bool>())
            {
                DrawingsMenu.Item("enemycccounter").Permashow(false);
                DrawingsMenu.Item("enemycounter").Permashow(false);
                return;
            }

            DrawingsMenu.Item("enemycccounter").Permashow();
            DrawingsMenu.Item("enemycounter").Permashow();
            if (Environment.TickCount - _lastUpdate > 367)
            {
                DrawingsMenu.Item("enemycccounter").SetValue(Positioning.EnemyCC());
                DrawingsMenu.Item("enemycounter").SetValue(Player.CountEnemiesInRange(2000));
                _lastUpdate = Environment.TickCount;
            }
            if (DrawingsMenu.Item("drawenemyrangecircle").GetValue<bool>())
            {
                foreach (var polygon in Positioning.DangerZone())
                {
                    polygon.Draw(Color.Red, 2);
                }
            }
        }

        protected virtual void OnEnemyGapcloser(ActiveGapcloser gapcloser) { }
        protected virtual void OnPossibleToInterrupt(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args) { }
        protected virtual void BeforeAttack(Orbwalking.BeforeAttackEventArgs args) { }
        protected virtual void OnAttack(AttackableUnit sender, AttackableUnit target) { }
        protected virtual void AfterAttack(AttackableUnit unit, AttackableUnit target) { }
        protected virtual void OnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args) { }
        protected virtual void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args) { }
        protected virtual void OnNonKillableMinion(AttackableUnit minion) { }

        protected virtual void Combo() { }
        protected virtual void LaneClear() { }
        protected virtual void Mixed() { }
        protected virtual void Escape() { }
        protected virtual void LowMana() { }

        internal static Orbwalking.Orbwalker Orbwalker;
        internal static PotionManager PotionManager;
        internal static Utils.Activator Activator;
        internal static Obj_AI_Hero Player = ObjectManager.Player;

        #region Spells
        internal static Spell Q;
        internal static Spell W;
        internal static Spell E;
        internal static Spell R;
        #endregion Spells

        #region Menu
        internal static Menu MainMenu;
        internal static Menu ComboMenu;
        internal static Menu LaneClearMenu;
        internal static Menu EscapeMenu;
        internal static Menu ManaManagementMenu;
        internal static Menu ActivatorMenu;
        internal static Menu DrawingsMenu;
        internal static Menu SkinhackMenu;
        internal static Menu OrbwalkerMenu;
        #endregion Menu
    }
}
