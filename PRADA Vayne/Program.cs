/* Seriously, please don't copy this...
 * I worked alot on it
 */

using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using PRADA_Vayne.Utils;
using SharpDX;
using Color = System.Drawing.Color;
using SpellSlot = LeagueSharp.SpellSlot;
using TargetSelector = LeagueSharp.Common.TargetSelector;
// ReSharper disable InconsistentNaming
// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable RedundantDefaultMemberInitializer

namespace PRADA_Vayne
{
    public static class Program
    {

        #region Fields & Objects

        #region Others
        private static Vector3 _condemnEndPos = Vector3.Zero;
        //private static bool _tumbleToKillSecondMinion; #TODO
        private static int _selectedSkin;
        private static bool _skinLoaded = false;
        private static int _cycleThroughSkinsTime = 0;
        private static int _lastCycledSkin;

        private static bool _canWallTumble;
        private static Vector3 _dragPreV3 = new Vector2(12050, 4828).To3D();
        private static Vector3 _dragAftV3 = new Vector2(11510, 4470).To3D();
        private static Vector3 _midPreV3 = new Vector2(6962, 8952).To3D();
        private static Vector3 _midAftV3 = new Vector2(6667, 8794).To3D();

        public static int FlashTime = 0;
        #endregion

        internal static MyOrbwalker.Orbwalker Orbwalker;
        internal static PotionManager PotionManager;
        internal static Utils.Activator Activator;
        internal static Obj_AI_Hero Player = ObjectManager.Player;

        #region Spells
        internal static Spell Q;
        internal static Spell W;
        internal static Spell E;
        internal static Spell R;
        internal static SpellSlot Flash;
        #endregion Spells

        #region Menu
        public static Menu MainMenu;
        internal static Menu ComboMenu;
        internal static Menu LaneClearMenu;
        internal static Menu EscapeMenu;
        internal static Menu ManaManagementMenu;
        internal static Menu ActivatorMenu;
        internal static Menu DrawingsMenu;
        internal static Menu SkinhackMenu;
        internal static Menu OrbwalkerMenu;
        #endregion Menu
        #endregion

        public static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += LoadPRADA;
        }

        public static void LoadPRADA(EventArgs args)
        {
            try
            {
                if (ObjectManager.Player.CharData.BaseSkinName != "Vayne")
                    return; //If the selected champion is not Vayne, we're not gonna load PRADA.

                _canWallTumble = (Utility.Map.GetMap().Type == Utility.Map.MapType.SummonersRift);

                Cache.Load(); //Start the caching process
                ConstructMenu(); //Initializing the menus
                InitOrbwalker(); //Initializing the orbwalker
                //PEvade.PEvade.Load(); //Load PRADA Evade #TODO
                FinishMenuInit(); //Add menu to main menu
                InitSpells(); //Initializing spells
                LoadSkinHax(); //Initializing the skinhax

                Game.OnUpdate += OnUpdate;
                Drawing.OnDraw += OnDraw;
                MyOrbwalker.BeforeAttack += BeforeAttack;
                MyOrbwalker.AfterAttack += AfterAttack;
                GameObject.OnCreate += GameObject_OnCreate;
                Interrupter2.OnInterruptableTarget += OnPossibleToInterrupt;
                Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
                AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;

                ShowLoadedNotifications(); //Everything went well, display successfuly loaded message.
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.Name.ToLower().Contains("leapsound.troy"))
            {
                var rengo = Heroes.EnemyHeroes.FirstOrDefault(h => h.CharData.BaseSkinName == "Rengar");
                if (rengo.IsValidTarget(545) && E.IsReady())
                {
                    E.Cast(rengo);
                }
            }
        }

        public static void BeforeAttack(MyOrbwalker.BeforeAttackEventArgs args)
        {
            if (!args.Unit.IsMe || !Q.IsReady() || !ComboMenu.Item("QCombo").GetValue<bool>()) return;
            if (HasUltiBuff() && HasTumbleBuff() && EscapeMenu.Item("QUlt").GetValue<bool>() && Heroes.EnemyHeroes.Any(h => h.IsMelee && h.Distance(Player) < h.AttackRange + 75))
            {
                args.Process = false;
            }
            if (args.Target.IsValid<Obj_AI_Hero>())
            {
                var target = (Obj_AI_Hero)args.Target;
                if (ComboMenu.Item("RCombo").GetValue<bool>() &&
                    Orbwalker.ActiveMode == MyOrbwalker.OrbwalkingMode.Combo)
                {
                    if (target.UnderTurret(true))
                    {
                        if (Heroes.Player.UnderTurret(true))
                        {
                            R.Cast();
                        }
                    }
                    else
                    {
                        R.Cast();
                    }
                }

                var t = (Obj_AI_Hero)args.Target;

                if (t.IsMelee && t.IsFacing(Player))
                {
                    if (t.Distance(Player.ServerPosition) < 325)
                    {
                        var tumblePos = t.GetTumblePos();
                        if (ComboMenu.Item("QChecks").GetValue<bool>() && tumblePos.IsShroom()) return;
                        args.Process = false;
                        Q.Cast(tumblePos);
                    }
                }
            }
        }

        public static void AfterAttack(AttackableUnit sender, AttackableUnit target)
        {
            if (!Q.IsReady() || !target.IsValid<Obj_AI_Hero>() || !ComboMenu.Item("QCombo").GetValue<bool>() || !sender.IsMe) return;
            if (!Flash.IsReady() && Environment.TickCount - FlashTime < 500) return;
            var tg = target as Obj_AI_Hero;
            if (tg == null) return;
            var mode = ComboMenu.Item("QMode").GetValue<StringList>().SelectedValue;
            var tumblePos = mode == "PRADA" ? tg.GetTumblePos() : Game.CursorPos;
            if ((ComboMenu.Item("QChecks").GetValue<bool>() && tumblePos.IsShroom())) return;
            if (Orbwalker.ActiveMode == MyOrbwalker.OrbwalkingMode.Combo)
            {
                Q.Cast(tumblePos);
            }
        }

        public static void OnUpdate(EventArgs args)
        {
            Console.WriteLine(ObjectManager.Player.ServerPosition);
            if (E.IsReady() && Orbwalker.ActiveMode == MyOrbwalker.OrbwalkingMode.Combo)
            {
                Condemn();
            }

            if (SkinhackMenu.Item("enableskinhack").GetValue<bool>())
            {
                SkinHax();
            }

            if (_canWallTumble && ComboMenu.Item("QWall").GetValue<bool>() && Q.IsReady() && Player.Distance(_dragPreV3) < 500)
            {
                DragWallTumble();
            } 
            
            if (_canWallTumble && ComboMenu.Item("QWall").GetValue<bool>() && Q.IsReady() && Player.Distance(_midPreV3) < 500)
            {
                MidWallTumble();
            }

            if (Player.Buffs.Any(b => b.Name.ToLower().Contains("rengarr")))
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

            if (Player.InFountain() && ComboMenu.Item("AutoBuy").GetValue<bool>() && Player.Level > 6 && Items.HasItem((int)ItemId.Warding_Totem_Trinket))
            {
                Player.BuyItem(ItemId.Scrying_Orb_Trinket);
            }
            if (Player.InFountain() && ComboMenu.Item("AutoBuy").GetValue<bool>() && !Items.HasItem((int)ItemId.Oracles_Lens_Trinket, Player) && Player.Level >= 9 && HeroManager.Enemies.Any(h => h.CharData.BaseSkinName == "Rengar" || h.CharData.BaseSkinName == "Talon"))
            {
                Player.BuyItem(ItemId.Oracles_Lens_Trinket);
            }
        }

        public static void OnDraw(EventArgs args)
        {
            if (DrawingsMenu.Item("streamingmode").GetValue<bool>())
            {
                return;
            }

            if (_canWallTumble && Player.Distance(_dragPreV3) < 3000)
                Drawing.DrawCircle(_dragPreV3, 75, Color.Gold); 
            if (_canWallTumble && Player.Distance(_midPreV3) < 3000)
                Drawing.DrawCircle(_midPreV3, 75, Color.Gold);

            foreach (var hero in HeroManager.Enemies.Where(h => h.IsValidTarget() && h.Distance(Player) < 1400))
            {
                var AAsNeeded = (int)(hero.Health / Player.GetAutoAttackDamage(hero));
                Drawing.DrawText(hero.HPBarPosition.X + 5, hero.HPBarPosition.Y - 30,
                    AAsNeeded <= 3 ? Color.Gold : Color.White,
                    "AAs to kill: " + AAsNeeded);
            }

            if (DrawingsMenu.Item("drawenemywaypoints").GetValue<bool>())
            {
                foreach (var e in HeroManager.Enemies.Where(en => en.IsVisible && !en.IsDead && en.Distance(Player) < 2500))
                {
                    var ip = Drawing.WorldToScreen(e.Position); //start pos

                    var wp = Utility.GetWaypoints(e);
                    var c = wp.Count - 1;
                    if (wp.Count() <= 1) break;

                    var w = Drawing.WorldToScreen(wp[c].To3D()); //endpos

                    Drawing.DrawLine(ip.X, ip.Y, w.X, w.Y, 2, Color.Red);
                }
            }
        }

        public static void Condemn()
        {
            if (Heroes.Player.CountEnemiesInRange(600) == 1)
            {
                var target = Heroes.EnemyHeroes.FirstOrDefault(e => e.IsValidTarget(545));

                if (target != null)
                {
                    if (target.IsCondemnable())
                    {
                        _condemnEndPos = Player.Position.Extend(target.Position,
                            Player.Distance(target) + 385);
                        E.Cast(target);
                    }
                }
            }
            else
            {
                foreach (var enemy in Heroes.EnemyHeroes.Where(e => e.IsValidTarget(545)))
                {
                    if (enemy == null) continue;
                    if (TargetSelector.GetPriority(enemy) >= 2)
                    {
                        if (enemy.IsCondemnable())
                        {
                            _condemnEndPos = Player.Position.Extend(enemy.Position,
                                Player.Distance(enemy) + 385);
                            E.Cast(enemy);
                            break;
                        }
                    }
                }
            }
        }

        private static void DragWallTumble()
        {
            if (Player.Distance(_dragPreV3) < 115)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, _dragPreV3.Randomize(0,1));
            }
            if (Player.Distance(_dragPreV3) < 5)
            {
                Orbwalker.SetMovement(false);
                Q.Cast(_dragAftV3);
                Utility.DelayAction.Add(100 + Game.Ping/2, () =>
                {
                    Player.IssueOrder(GameObjectOrder.MoveTo, new Vector2(6962, 8952).To3D().Randomize(0, 1));
                    Orbwalker.SetMovement(true);
                });
            }
        }

        private static void MidWallTumble()
        {
            if (Player.Distance(_midPreV3) < 115)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, _midPreV3.Randomize(0, 1));
            }
            if (Player.Distance(_midPreV3) < 5)
            {
                Orbwalker.SetMovement(false);
                Q.Cast(_midAftV3);
                Utility.DelayAction.Add(100 + Game.Ping / 2, () =>
                {
                    Player.IssueOrder(GameObjectOrder.MoveTo, _midAftV3.Randomize(0, 1));
                    Orbwalker.SetMovement(true);
                });
            }
        }

        private static void SkinHax()
        {
            if (!SkinhackMenu.Item("enableskinhack").GetValue<bool>()) return;
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

        public static void OnPossibleToInterrupt(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (args.DangerLevel == Interrupter2.DangerLevel.High && E.IsReady() && E.IsInRange(sender))
            {
                E.Cast(sender);
            }
        }

        public static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name == "summonerflash")
            {
                FlashTime = Environment.TickCount;
            }
            #region ward brush after condemn
            if (sender.IsMe && args.SData.Name.ToLower().Contains("condemn") && args.Target.IsValid<Obj_AI_Hero>())
            {
                var target = (Obj_AI_Hero)args.Target;
                if (ComboMenu.Item("EQ").GetValue<bool>() && target.IsVisible && !target.HasBuffOfType(BuffType.Stun) && Q.IsReady()) //#TODO: fix
                {
                    Q.Cast(target.GetTumblePos());
                }
                if (NavMesh.IsWallOfGrass(_condemnEndPos, 100))
                {
                    var blueTrinket = ItemId.Scrying_Orb_Trinket;
                    if (Items.HasItem((int)ItemId.Farsight_Orb_Trinket, Player)) blueTrinket = ItemId.Farsight_Orb_Trinket;

                    var yellowTrinket = ItemId.Warding_Totem_Trinket;
                    if (Items.HasItem((int)ItemId.Greater_Stealth_Totem_Trinket, Player)) yellowTrinket = ItemId.Greater_Stealth_Totem_Trinket;

                    if (Items.CanUseItem((int)blueTrinket))
                        Items.UseItem((int)blueTrinket, _condemnEndPos.Randomize(0, 100));
                    if (Items.CanUseItem((int)yellowTrinket))
                        Items.UseItem((int)yellowTrinket, _condemnEndPos.Randomize(0, 100));
                }
            }
            #endregion

            #region jQuery hates this! Click to find out why!
            if (args.SData.Name.ToLower().Contains("talonshadow") || args.SData.Name.ToLower().Contains("rengarr"))
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
            #endregion

            if (ShouldSaveCondemn()) return;
            if (sender.Distance(Player) > 1500 || !args.Target.IsMe || args.SData == null)
                return;
            //how to milk alistar/thresh/everytoplaner
            var spellData = SpellDb.GetByName(args.SData.Name);
            if (spellData != null && !Heroes.Player.UnderTurret(true) && !Lists.UselessChamps.Contains(sender.CharData.BaseSkinName))
            {
                if (spellData.CcType == CcType.Knockup || spellData.CcType == CcType.Stun ||
                    spellData.CcType == CcType.Knockback || spellData.CcType == CcType.Suppression)
                {
                    E.Cast(sender);
                }
            }
        }

        public static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Q.IsReady() && gapcloser.End.Distance(Player.ServerPosition) < 300)
            {
                Q.Cast(gapcloser.Sender.GetTumblePos());
            }
            //KATARINA IN GAME. CONCERN!
            if (ShouldSaveCondemn()) return;
            //We really don't want to get turret aggro for nothin'.
            if (Player.UnderTurret(true)) return;
            //we wanna check if the mothafucka can actually do shit to us.
            if (Player.Distance(gapcloser.End) > 350) return;
            //ok we're no pussies, we don't want to condemn the unsuspecting akali when we can jihad her.
            if (Player.Level > gapcloser.Sender.Level + 1) return;
            //k so that's not the case, we're going to check if we should condemn the gapcloser away.

            #region If it's a cancer champ, condemn without even thinking, lol
            if (Lists.CancerChamps.Contains(gapcloser.Sender.CharData.BaseSkinName) && E.IsReady())
            {
                E.Cast(gapcloser.Sender);
            }
            #endregion
        }

        static bool HasUltiBuff()
        {
            return Player.Buffs.Any(b => b.Name.ToLower().Contains("vayneinquisition"));
        }

        static bool HasTumbleBuff()
        {
            return Player.Buffs.Any(b => b.Name.ToLower().Contains("vaynetumblebonus"));
        }

        private static bool ShouldSaveCondemn()
        {
            var katarina =
                HeroManager.Enemies.FirstOrDefault(h => h.CharData.BaseSkinName == "Katarina" && h.IsValidTarget(1400));
            if (katarina != null)
            {
                var kataR = katarina.GetSpell(SpellSlot.R);
                return kataR.IsReady() ||
                       (katarina.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Ready);
            }
            var galio =
                HeroManager.Enemies.FirstOrDefault(h => h.CharData.BaseSkinName == "Galio" && h.IsValidTarget(1400));
            if (galio != null)
            {
                var galioR = galio.GetSpell(SpellSlot.R);
                return galioR.IsReady() ||
                       (galio.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Ready);
            }
            return false;
        }

        public static void ConstructMenu()
        {
            MainMenu = new Menu("PRADA Vayne", "pradamenu", true);
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
            DrawingsMenu.AddItem(new MenuItem("drawenemywaypoints", "Draw Enemy Waypoints").SetValue(true));
            SkinhackMenu = new Menu("Skin Hack", "skinhackmenu");
            OrbwalkerMenu = new Menu("Orbwalker", "orbwalkermenu");
            ComboMenu.AddItem(new MenuItem("QCombo", "Auto Tumble").SetValue(true));
            ComboMenu.AddItem(new MenuItem("QMode", "Q Mode: ").SetValue(new StringList(new[] { "PRADA", "TO MOUSE" })));
            //ComboMenu.AddItem(new MenuItem("QHarass", "AA - Q - AA").SetValue(true)); #TODO
            ComboMenu.AddItem(new MenuItem("QChecks", "Q Safety Checks").SetValue(true));
            ComboMenu.AddItem(new MenuItem("EQ", "Q After E").SetValue(false));
            ComboMenu.AddItem(new MenuItem("QWall", "Enable Wall Tumble?").SetValue(true));
            //ComboMenu.AddItem(new MenuItem("FocusTwoW", "Focus 2 W Stacks").SetValue(true)); #TODO ?
            ComboMenu.AddItem(new MenuItem("ECombo", "Auto Condemn").SetValue(true));
            ComboMenu.AddItem(new MenuItem("EMode", "E Mode").SetValue(new StringList(new[] {"PRADA", "MARKSMAN", "GOSU", "SHARPSHOOTER", "VHREWORK"})));
            ComboMenu.AddItem(new MenuItem("EPushDist", "E Push Distance").SetValue(new Slider(395, 300, 475)));
            ComboMenu.AddItem(new MenuItem("EHitchance", "E % Hitchance").SetValue(new Slider(99, 0, 100)));
            ComboMenu.AddItem(new MenuItem("RCombo", "Auto Ult").SetValue(true));
            ComboMenu.AddItem(new MenuItem("AutoBuy", "Auto-Swap Trinkets?").SetValue(true));
            EscapeMenu.AddItem(new MenuItem("QUlt", "Smart Q-Ult").SetValue(true));
            EscapeMenu.AddItem(new MenuItem("EInterrupt", "Use E to Interrupt").SetValue(true));
            //LaneClearMenu.AddItem(new MenuItem("QFarm", "Use Q").SetValue(true));#TODO
            SkinhackMenu.AddItem(
                new MenuItem("skin", "Skin: ").SetValue(
                    new StringList(new[] { "Classic", "Vindicator", "Aristocrat", "Dragonslayer", "Heartseeker", "SKT T1", "Arclight" }))).ValueChanged +=
                (sender, args) =>
                {
                    _selectedSkin = SkinhackMenu.Item("skin").GetValue<StringList>().SelectedIndex + 1;
                    Player.SetSkin(Player.CharData.BaseSkinName, _selectedSkin);
                };
            SkinhackMenu.AddItem(new MenuItem("enableskinhack", "Enable Skinhax").SetValue(true));
            SkinhackMenu.AddItem(new MenuItem("cyclethroughskins", "Cycle Through Skins").SetValue(false));
            SkinhackMenu.AddItem(new MenuItem("cyclethroughskinstime", "Cycling Time").SetValue(new Slider(30, 30, 600)));
        }

        public static void InitOrbwalker()
        {
            Orbwalker = new MyOrbwalker.Orbwalker(OrbwalkerMenu);

            //Common orbwalker messes with our orbwalker
            //so we're gonna turn it off
            Orbwalking.Attack = false;
            Orbwalking.Move = false;
        }

        public static void FinishMenuInit()
        {
            MainMenu.AddSubMenu(ComboMenu);
            //MainMenu.AddSubMenu(LaneClearMenu); #TODO
            MainMenu.AddSubMenu(EscapeMenu);
            MainMenu.AddSubMenu(ActivatorMenu);
            MainMenu.AddSubMenu(SkinhackMenu); // XD
            MainMenu.AddSubMenu(DrawingsMenu);
            MainMenu.AddSubMenu(OrbwalkerMenu);
            //MainMenu.AddSubMenu(PEvade.Config.Menu); //#TODO
            MainMenu.AddToMainMenu();
        }

        public static void InitSpells()
        {
            Q = new Spell(SpellSlot.Q, 300f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 545f);
            R = new Spell(SpellSlot.R);
            Flash = Player.GetSpellSlot("summonerflash");
        }

        public static void LoadSkinHax()
        {
            if (SkinhackMenu.Item("enableskinhack").GetValue<bool>())
            {
                _selectedSkin = SkinhackMenu.Item("skin").GetValue<StringList>().SelectedIndex + 1;
                Player.SetSkin(Player.CharData.BaseSkinName, _selectedSkin);
                _skinLoaded = true;
            }
        }

        public static void ShowLoadedNotifications()
        {
            Utility.DelayAction.Add(3000, () =>
            {
                Notifications.AddNotification("PRADA Vayne", 10000);
                Notifications.AddNotification("by GUCCI & H&M", 10000);
                Notifications.AddNotification("HF, ", 10000);
                Notifications.AddNotification("U don't need luck", 10000);
            });
        }
    }
}
