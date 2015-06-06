using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            DrawingsMenu.AddItem(new MenuItem("censorownname", "Censor Own Name").SetValue(true));
            DrawingsMenu.AddItem(new MenuItem("drawenemycccounter", "Draw Enemy CC Counter").SetValue(true));
            DrawingsMenu.AddItem(new MenuItem("drawenemyrangecircle", "Draw Enemy Spells Range").SetValue(true));
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
            MainMenu.AddSubMenu(DrawingsMenu);
            MainMenu.AddSubMenu(OrbwalkerMenu);
            MainMenu.AddToMainMenu();
        }

        protected virtual void InitSpells() { }

        protected virtual void OnGameLoad(EventArgs args)
        {
            const int TIMEONSCREEN = 3000;
            Notifications.AddNotification("Fashion Series by GUCCI & H&M loaded", TIMEONSCREEN);
            Utility.DelayAction.Add(TIMEONSCREEN-550, () => { Notifications.AddNotification("HF, you don't need luck ;)", TIMEONSCREEN); });
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
            if (DrawingsMenu.Item("streamingmode").GetValue<bool>()) return;
            if (DrawingsMenu.Item("drawenemycccounter").GetValue<bool>())
            {
                var enemyCC = Positioning.EnemyCC();
                if (enemyCC >= 3)
                {
                    Drawing.DrawText(Player.HPBarPosition.X + 5, Player.HPBarPosition.Y - 30, Color.Red,
                        "Enemy HARD-CC: " + enemyCC);
                }
                else if (enemyCC > 0 && enemyCC < 3)
                {
                    Drawing.DrawText(Player.HPBarPosition.X + 5, Player.HPBarPosition.Y - 30, Color.Gold,
                        "Enemy HARD-CC: " + enemyCC);
                }
                else
                {
                    Drawing.DrawText(Player.HPBarPosition.X + 5, Player.HPBarPosition.Y - 30, Color.White,
                        "Enemy HARD-CC: " + enemyCC);
                }
            }
            if (DrawingsMenu.Item("censorownname").GetValue<bool>())
            {
                Drawing.DrawLine(Player.HPBarPosition.X + 10, Player.HPBarPosition.Y - 15, Player.HPBarPosition.X + 140, Player.HPBarPosition.Y - 15, 20, Color.Black);
                switch (Player.BaseSkinName)
                {
                    case "Vayne":
                        Drawing.DrawText(Player.HPBarPosition.X + 27, Player.HPBarPosition.Y - 15, Color.Gold,
                            "PRADA Vayne");
                        break;
                    case "Katarina":
                        Drawing.DrawText(Player.HPBarPosition.X + 27, Player.HPBarPosition.Y - 15, Color.Gold,
                            "CARTIERina");
                        break;
                }
            }
            if (DrawingsMenu.Item("drawenemyrangecircle").GetValue<bool>())
            {
                foreach (var polygon in Positioning.DangerZone())
                {
                    polygon.Draw(Color.Red, 3);
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
        internal static Menu OrbwalkerMenu;
        #endregion Menu
    }
}
