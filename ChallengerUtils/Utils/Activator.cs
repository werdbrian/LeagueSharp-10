using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using ChallengerSeries.Utils;
using LeagueSharp.Common.Data;
using SS = LeagueSharp.SpellSlot;

namespace ChallengerSeries.Utils
{
    internal class Activator
    {
        public Activator(Menu menu)
        {
            Load(menu);
        }
        public static bool Loaded { get { return loaded; } }
        private static Obj_AI_Hero Player = ObjectManager.Player;
        private static bool loaded = false;

        public static void Load(Menu menu)
        {
            CustomEvents.Game.OnGameLoad += GameBuff.OnGameLoad;
            new Cleansers().Initialize(ChallengerPlugin.ActivatorMenu);
            new PotionManager(ChallengerPlugin.ActivatorMenu);
            menu.AddItem(new MenuItem("activator", "Use CK Activator?").SetValue(true));
            menu.AddItem(new MenuItem("exploits", "Enable Exploits?").SetValue(true));
         //   Game.OnUpdate += OnUpdate;
          //  Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Notifications.AddNotification("CK Activator Loaded", 3000);
            loaded = true;
        }

        public static void OnUpdate(EventArgs args)
        {
            if (ChallengerPlugin.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Player.CountEnemiesInRange(1000) >= 1)
            {
                Combo();
            }
        }

        public static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.Distance(Player) > 1400) return;
            var qss = ItemId.Quicksilver_Sash;
            if (Items.HasItem((int) ItemId.Mercurial_Scimitar))
            {
                qss = ItemId.Mercurial_Scimitar;
            }

            if (sender.IsValid<Obj_AI_Hero>() && sender.IsEnemy && args.Target == Player)
            {
                var sData = SpellDb.GetByName(args.SData.Name);
                if (sData != null && sData.ChampionName.ToLower() == "syndra" && sData.Spellslot == SpellSlot.R)
                {
                    Utility.DelayAction.Add(sData.Delay*1000 - Game.Ping, () => UseItem(qss));
                }
                if (args.SData.Name == "summonerdot" && sender.GetSpellDamage(Player, "summonerdot") < Player.Health + sender.GetAutoAttackDamage(Player))
                {
                    UseItem(qss);
                }
            }

            if (!ChallengerPlugin.ActivatorMenu.Item("exploits").GetValue<bool>()) return;

            if (sender.Name.ToLower() == "cassiopeia" && args.SData.Name.ToLower().Contains("petrifying") && Player.Distance(sender) < 750 && Player.IsFacing(sender))
            {
                ChallengerPlugin.Orbwalker.SetMovement(false);
                ChallengerPlugin.Orbwalker.SetAttack(false);
                Player.IssueOrder(GameObjectOrder.MoveTo,
                    sender.Position.To2D().Extend(Player.ServerPosition.To2D(), Player.Distance(sender)+100).To3D());
                Utility.DelayAction.Add(300, () =>
                {
                    ChallengerPlugin.Orbwalker.SetMovement(true);
                    ChallengerPlugin.Orbwalker.SetAttack(true);
                });
            }

            if (sender.Name.ToLower() == "shaco" && args.SData.Name.ToLower().Contains("twoshiv") && args.Target.IsMe && !Player.IsFacing(sender))
            {
                ChallengerPlugin.Orbwalker.SetMovement(false);
                ChallengerPlugin.Orbwalker.SetAttack(false);
                Player.IssueOrder(GameObjectOrder.MoveTo,
                    sender.Position.To2D().Extend(Player.ServerPosition.To2D(), Player.Distance(sender) - 100).To3D());
                Utility.DelayAction.Add(250, () =>
                {
                    ChallengerPlugin.Orbwalker.SetMovement(true);
                    ChallengerPlugin.Orbwalker.SetAttack(true);
                });
            }

            if (sender.Name.ToLower() == "tryndamere" && args.SData.Name.ToLower().Contains("mockingshout") && args.Target.IsMe && !Player.IsFacing(sender))
            {
                ChallengerPlugin.Orbwalker.SetMovement(false);
                ChallengerPlugin.Orbwalker.SetAttack(false);
                Player.IssueOrder(GameObjectOrder.MoveTo,
                    sender.Position.To2D().Extend(Player.ServerPosition.To2D(), Player.Distance(sender) - 100).To3D());
                Utility.DelayAction.Add(250, () =>
                {
                    ChallengerPlugin.Orbwalker.SetMovement(true);
                    ChallengerPlugin.Orbwalker.SetAttack(true);
                });
            }
        }

        public static void Combo()
        {
            UseQSS();
            UseBOTRK();
            var TeamfightRange = 800;
            var TeamfightingEnemies = HeroManager.Enemies.FindAll(e => e.Distance(Player) < TeamfightRange);
            if (Player.HealthPercent < 80 && Items.HasItem((int)ItemId.Blade_of_the_Ruined_King) && Items.CanUseItem((int)ItemId.Blade_of_the_Ruined_King))
            {
                var KSableEnemy =
                    HeroManager.Enemies.FirstOrDefault(e => e.Health < (e.MaxHealth * 0.1) - (e.MaxHealth * 0.1) * ((e.Armor / 10) * 7) / 100);
                if (KSableEnemy.IsValidTarget())
                {
                    UseBOTRK(KSableEnemy);
                }
                if (Player.CountEnemiesInRange(TeamfightRange) >= Player.CountAlliesInRange(TeamfightRange)) return;
                var escaping = HeroManager.Enemies.FirstOrDefault(e => e.HealthPercent < 40);
                if (!escaping.IsFacing(Player) && escaping.Distance(Game.CursorPos) < 250)
                {
                    UseBOTRK(escaping.IsValidTarget(500) ? escaping : null);
                }
            }
            if (Items.HasItem((int) ItemId.Youmuus_Ghostblade) &&
                Player.CountEnemiesInRange(TeamfightRange) <= Player.CountAlliesInRange(TeamfightRange) &&
                HeroManager.Allies.Any(a => a.Distance(TeamfightingEnemies.FirstOrDefault()) < 600))
            {
                UseItem(ItemId.Youmuus_Ghostblade);
            }
        }

        private static void UseQSS()
        {
            var qss = ItemId.Quicksilver_Sash;
            if (Items.HasItem((int) ItemId.Mercurial_Scimitar))
            {
                qss = ItemId.Mercurial_Scimitar;
            }
            if (!Items.CanUseItem((int) qss)) return;

            if (Player.HasBuffOfType(BuffType.Suppression))
            {
                UseItem(qss);
            }
            if (Player.HasBuffOfType(BuffType.Knockup) &&
                HeroManager.Enemies.Any(e => e.BaseSkinName.ToLower().Contains("yasuo")))
            {
                UseItem(qss);
            }
            if (Player.CountEnemiesInRange(650) >= Player.CountAlliesInRange(650))
            {
                if (Player.HasBuffOfType(BuffType.Stun))
                {
                    UseItem(qss);
                }
                if (Player.HasBuffOfType(BuffType.Silence) && Player.Health < 30 &&
                    Player.Spellbook.CanUseSpell(Player.GetSpellSlot("summonerflash")) == SpellState.Ready)
                {
                    UseItem(qss);
                }
                if (Player.HasBuffOfType(BuffType.Charm))
                {
                    UseItem(qss);
                }
                if (Player.HasBuffOfType(BuffType.Poison) &&
                    HeroManager.Enemies.Any(
                        e => e.BaseSkinName.ToLower().Contains("cassiopeia") && (e.Level > Player.Level || Player.HealthPercent < 30) && e.Distance(Player) < 700))
                {
                    UseItem(qss);
                }
            }
        }

        private static void UseBOTRK(Obj_AI_Hero enemy = null)
        {
            var botrk = ItemId.Bilgewater_Cutlass;
            if (Items.HasItem((int) ItemId.Blade_of_the_Ruined_King, Player))
            {
                botrk = ItemId.Blade_of_the_Ruined_King;
            }
            var targetsInRange = Player.GetEnemiesInRange(450).FindAll(e => !e.IsDead && e.IsValidTarget());

            if (targetsInRange.Count == 0 || !Items.CanUseItem((int)botrk)) return;

            var target = targetsInRange.OrderBy(h => h.Armor)
                            .FirstOrDefault();
            if (botrk == ItemId.Bilgewater_Cutlass || Player.Health + Player.GetItemDamage(target, Damage.DamageItems.Botrk) < ObjectManager.Player.MaxHealth)
                UseItem(botrk, target);
            if (enemy != null)
            {
                UseItem(ItemId.Blade_of_the_Ruined_King, enemy);
            }
            else
            {
                if (target != null)
                {
                    UseItem(ItemId.Blade_of_the_Ruined_King, target);
                }
            }
        }

        public static void UseItem(ItemId id, Obj_AI_Base target = null)
        {
            if (!Items.CanUseItem((int) id)) return;
            foreach (var slot in Player.InventoryItems.Where(slot => slot.Id == id))
            {
                if (target != null)
                {
                    Player.Spellbook.CastSpell(slot.SpellSlot, target);
                }
                else
                {
                    Player.Spellbook.CastSpell(slot.SpellSlot);
                }
            }
        }
    }

    #region Marskman Potion Manager <3

    internal class PotionManager
    {
        private readonly Menu ExtrasMenu;
        private List<Potion> potions;

        public PotionManager(Menu extrasMenu)
        {
            ExtrasMenu = extrasMenu;
            potions = new List<Potion>
            {
                new Potion
                {
                    Name = "ItemCrystalFlask",
                    MinCharges = 1,
                    ItemId = (ItemId) 2041,
                    Priority = 1,
                    TypeList = new List<PotionType> {PotionType.Health, PotionType.Mana}
                },
                new Potion
                {
                    Name = "RegenerationPotion",
                    MinCharges = 0,
                    ItemId = (ItemId) 2003,
                    Priority = 2,
                    TypeList = new List<PotionType> {PotionType.Health}
                },
                new Potion
                {
                    Name = "ItemMiniRegenPotion",
                    MinCharges = 0,
                    ItemId = (ItemId) 2010,
                    Priority = 4,
                    TypeList = new List<PotionType> {PotionType.Health, PotionType.Mana}
                },
                new Potion
                {
                    Name = "FlaskOfCrystalWater",
                    MinCharges = 0,
                    ItemId = (ItemId) 2004,
                    Priority = 3,
                    TypeList = new List<PotionType> {PotionType.Mana}
                }
            };
            Load();
        }

        private void Load()
        {
            potions = potions.OrderBy(x => x.Priority).ToList();
            ExtrasMenu.AddSubMenu(new Menu("Potion Manager", "PotionManager"));

            ExtrasMenu.SubMenu("PotionManager").AddSubMenu(new Menu("Health", "Health"));
            ExtrasMenu.SubMenu("PotionManager")
                .SubMenu("Health")
                .AddItem(new MenuItem("HealthPotion", "Use Health Potion").SetValue(true));
            ExtrasMenu.SubMenu("PotionManager")
                .SubMenu("Health")
                .AddItem(new MenuItem("HealthPercent", "HP Trigger Percent").SetValue(new Slider(60)));

            ExtrasMenu.SubMenu("PotionManager").AddSubMenu(new Menu("Mana", "Mana"));
            ExtrasMenu.SubMenu("PotionManager")
                .SubMenu("Mana")
                .AddItem(new MenuItem("ManaPotion", "Use Mana Potion").SetValue(true));
            ExtrasMenu.SubMenu("PotionManager")
                .SubMenu("Mana")
                .AddItem(new MenuItem("ManaPercent", "MP Trigger Percent").SetValue(new Slider(45)));

            Game.OnUpdate += OnGameUpdate;
        }

        private void OnGameUpdate(EventArgs args)
        {
            if (ObjectManager.Player.HasBuff("Recall") ||
                ObjectManager.Player.InFountain() && ObjectManager.Player.InShop())
                return;

            try
            {
                if (ExtrasMenu.Item("HealthPotion").GetValue<bool>())
                {
                    if (ObjectManager.Player.HealthPercent <= ExtrasMenu.Item("HealthPercent").GetValue<Slider>().Value)
                    {
                        var healthSlot = GetPotionSlot(PotionType.Health);
                        if (!IsBuffActive(PotionType.Health))
                            ObjectManager.Player.Spellbook.CastSpell(healthSlot.SpellSlot);
                    }
                }
                if (ExtrasMenu.Item("ManaPotion").GetValue<bool>())
                {
                    if (ObjectManager.Player.ManaPercent <= ExtrasMenu.Item("ManaPercent").GetValue<Slider>().Value)
                    {
                        var manaSlot = GetPotionSlot(PotionType.Mana);
                        if (!IsBuffActive(PotionType.Mana))
                            ObjectManager.Player.Spellbook.CastSpell(manaSlot.SpellSlot);
                    }
                }
            }

            catch (Exception)
            {
            }
        }

        private InventorySlot GetPotionSlot(PotionType type)
        {
            return (from potion in potions
                    where potion.TypeList.Contains(type)
                    from item in ObjectManager.Player.InventoryItems
                    where item.Id == potion.ItemId && item.Charges >= potion.MinCharges
                    select item).FirstOrDefault();
        }

        private bool IsBuffActive(PotionType type)
        {
            return (from potion in potions
                    where potion.TypeList.Contains(type)
                    from buff in ObjectManager.Player.Buffs
                    where buff.Name == potion.Name && buff.IsActive
                    select potion).Any();
        }

        private enum PotionType
        {
            Health,
            Mana
        };

        private class Potion
        {
            public string Name { get; set; }
            public int MinCharges { get; set; }
            public ItemId ItemId { get; set; }
            public int Priority { get; set; }
            public List<PotionType> TypeList { get; set; }
        }
    }
#endregion Marksman Potion Manager <3

#region Kurisu's Oracle's Cleanser.. :cat_lazy: thx Kuriseru <3
    internal class Cleansers
    {
        private static Menu _menuConfig, _mainMenu, _ccTypeConfig;
        private static readonly Obj_AI_Hero Me = ObjectManager.Player;

        public void Initialize(Menu root)
        {

            _mainMenu = new Menu("Cleansers", "cmenu");
            _menuConfig = new Menu("Cleansers Settings", "cconfig");
            _mainMenu.AddSubMenu(new Menu("Cleanse CcType", "ccTypeConfig"));
            _ccTypeConfig = _mainMenu.SubMenu("ccTypeConfig");

            _ccTypeConfig.AddItem(new MenuItem("suppresion", "Suppresions").SetValue(true));
            _ccTypeConfig.AddItem(new MenuItem("stun", "Stuns").SetValue(true));
            _ccTypeConfig.AddItem(new MenuItem("knockup", "Knockups").SetValue(true));
            _ccTypeConfig.AddItem(new MenuItem("knockback", "Knockbacks").SetValue(false));
            _ccTypeConfig.AddItem(new MenuItem("charm", "Charms").SetValue(true));
            _ccTypeConfig.AddItem(new MenuItem("polymorph", "Polymorphs").SetValue(false));
            _ccTypeConfig.AddItem(new MenuItem("snare", "Snares").SetValue(false));
            _ccTypeConfig.AddItem(new MenuItem("taunt", "Taunts").SetValue(false));
            _ccTypeConfig.AddItem(new MenuItem("slow", "Slows").SetValue(false));
            _ccTypeConfig.AddItem(new MenuItem("fear", "Fears").SetValue(false));
            _ccTypeConfig.AddItem(new MenuItem("blind", "Blinds").SetValue(false));
            _ccTypeConfig.AddItem(new MenuItem("poison", "Poisons").SetValue(false));

            foreach (var a in ObjectManager.Get<Obj_AI_Hero>().Where(a => a.Team == Me.Team))
                _menuConfig.AddItem(new MenuItem("cccon" + a.SkinName, "Use for " + a.SkinName)).SetValue(true);
            _mainMenu.AddSubMenu(_menuConfig);

            CreateMenuItem("Dervish Blade", "Dervish", 2);
            CreateMenuItem("Quicksilver Sash", "Quicksilver", 2);
            CreateMenuItem("Mercurial Scimitar", "Mercurial", 2);
            CreateMenuItem("Mikael's Crucible", "Mikaels", 2);

            // delay the cleanse value * 100
            _mainMenu.AddItem(new MenuItem("cleansedelay", "Cleanse delay ")).SetValue(new Slider(0, 0, 25));

            _mainMenu.AddItem(
                new MenuItem("cmode", "Mode: "))
                .SetValue(new StringList(new[] { "Always", "Combo" }, 1));


            root.AddSubMenu(_mainMenu);

            Game.OnUpdate += Game_OnGameUpdate;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (ChallengerPlugin.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo ||
                _mainMenu.Item("cmode").GetValue<StringList>().SelectedIndex != 1)
            {
                UseItem("Mikaels", 3222, 600f);
                UseItem("Quicksilver", 3140);
                UseItem("Mercurial", 3139);
                UseItem("Dervish", 3137);
            }
        }

        private static void UseItem(string name, int itemId, float range = float.MaxValue)
        {
            if (!Items.CanUseItem(itemId) || !Items.HasItem(itemId))
                return;

            if (!_mainMenu.Item("use" + name).GetValue<bool>())
                return;

            var target = Me;

            if (range < 5000)
            {
                foreach (var unit in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(x => x.IsAlly && x.IsValidTarget(900, false))
                        .OrderByDescending(
                            xe => xe.Health/xe.MaxHealth*100))
                {
                    target = unit ?? Me;
                }
            }
            if (_mainMenu.Item("cccon" + target.SkinName).GetValue<bool>())
            {
                if (target.Distance(Me.ServerPosition, true) <= range * range && target.IsValidState())
                {
                    var delay = _mainMenu.Item("cleansedelay").GetValue<Slider>().Value * 10;

                    foreach (var buff in GameBuff.CleanseBuffs)
                    {
                        var buffinst = target.Buffs;
                        if (buffinst.Any(aura => aura.Name.ToLower() == buff.BuffName ||
                                                 aura.Name.ToLower().Contains(buff.SpellName)))
                        {
                            if (!_ccTypeConfig.Item("cure" + buff.BuffName).GetValue<bool>())
                            {
                                return;
                            }

                            Utility.DelayAction.Add(delay + buff.Delay, delegate
                            {
                                Items.UseItem(itemId, target);
                            });
                        }
                    }

                    foreach (var b in target.Buffs)
                    {
                        if (_ccTypeConfig.Item("slow").GetValue<bool>() && b.Type == BuffType.Slow)
                        {
                            Utility.DelayAction.Add(delay, () => Items.UseItem(itemId, target));
                        }

                        if (_ccTypeConfig.Item("stun").GetValue<bool>() && b.Type == BuffType.Stun)
                        {

                            Utility.DelayAction.Add(delay, () => Items.UseItem(itemId, target));
                        }

                        if (_ccTypeConfig.Item("charm").GetValue<bool>() && b.Type == BuffType.Charm)
                        {
                            Utility.DelayAction.Add(delay, () => Items.UseItem(itemId, target));
                        }

                        if (_ccTypeConfig.Item("taunt").GetValue<bool>() && b.Type == BuffType.Taunt)
                        {
                            Utility.DelayAction.Add(delay, () => Items.UseItem(itemId, target));
                        }

                        if (_ccTypeConfig.Item("fear").GetValue<bool>() && b.Type == BuffType.Fear)
                        {
                            Utility.DelayAction.Add(delay, () => Items.UseItem(itemId, target));
                        }

                        if (_ccTypeConfig.Item("snare").GetValue<bool>() && b.Type == BuffType.Snare)
                        {
                            Utility.DelayAction.Add(delay, () => Items.UseItem(itemId, target));
                        }

                        if (_ccTypeConfig.Item("silence").GetValue<bool>() && b.Type == BuffType.Silence)
                        {
                            Utility.DelayAction.Add(delay, () => Items.UseItem(itemId, target));
                        }

                        if (_ccTypeConfig.Item("suppression").GetValue<bool>() && b.Type == BuffType.Suppression)
                        {
                            Utility.DelayAction.Add(delay, () => Items.UseItem(itemId, target));
                        }

                        if (_ccTypeConfig.Item("polymorph").GetValue<bool>() && b.Type == BuffType.Polymorph)
                        {
                            Utility.DelayAction.Add(delay, () => Items.UseItem(itemId, target));
                        }

                        if (_ccTypeConfig.Item("blind").GetValue<bool>() && b.Type == BuffType.Blind)
                        {
                            Utility.DelayAction.Add(delay, () => Items.UseItem(itemId, target));
                        }

                        if (_ccTypeConfig.Item("poison").GetValue<bool>() && b.Type == BuffType.Poison)
                        {
                            Utility.DelayAction.Add(delay, () => Items.UseItem(itemId, target));
                        }
                    }
                }
            }
        }

        private static void CreateMenuItem(string displayname, string name, int ccvalue)
        {
            var menuName = new Menu(name, name);
            menuName.AddItem(new MenuItem("use" + name, "Use " + displayname)).SetValue(true);
            menuName.AddItem(new MenuItem(name + "Count", "Min spells to use"));
            menuName.AddItem(new MenuItem(name + "Duration", "Buff duration to use"));
            _mainMenu.AddSubMenu(menuName);
        }
    }

    internal class GameBuff
    {
        public string ChampionName { get; set; }
        public string BuffName { get; set; }
        public SpellSlot Slot { get; set; }
        public string SpellName { get; set; }
        public int Delay { get; set; }

        public static readonly List<GameBuff> EvadeBuffs = new List<GameBuff>();
        public static readonly List<GameBuff> CleanseBuffs = new List<GameBuff>();


        public static void OnGameLoad(EventArgs args)
        {
            CleanseBuffs.Add(new GameBuff
            {
                ChampionName = "Braum",
                BuffName = "braummark",
                SpellName = "braumq",
                Slot = SpellSlot.Q,
                Delay = 0
            });

            CleanseBuffs.Add(new GameBuff
            {
                ChampionName = "Zed",
                BuffName = "zedulttargetmark",
                SpellName = "zedult",
                Slot = SpellSlot.R,
                Delay = 1800
            });

            CleanseBuffs.Add(new GameBuff
            {
                ChampionName = "Fizz",
                BuffName = "fizzmarinerdoombomb",
                SpellName = "fizzmarinerdoom",
                Slot = SpellSlot.R,
                Delay = 0
            });

            CleanseBuffs.Add(new GameBuff
            {
                ChampionName = "Leblanc",
                BuffName = "leblancsoulshackle",
                SpellName = "leblancsoulshackle",
                Slot = SpellSlot.E,
                Delay = 500
            });

            CleanseBuffs.Add(new GameBuff
            {
                ChampionName = "LeeSin",
                BuffName = "blindmonkqonechaos",
                SpellName = "blindmonkqone",
                Slot = SpellSlot.Q,
                Delay = 0
            });

            CleanseBuffs.Add(new GameBuff
            {
                ChampionName = "Leblanc",
                BuffName = "leblancsoulshacklem",
                SpellName = "leblancsoulshacklem",
                Slot = SpellSlot.R,
                Delay = 500
            });

            CleanseBuffs.Add(new GameBuff
            {
                ChampionName = "Nasus",
                BuffName = "NasusW",
                SpellName = "NasusW",
                Slot = SpellSlot.W,
                Delay = 0
            });

            CleanseBuffs.Add(new GameBuff
            {
                ChampionName = "Mordekaiser",
                BuffName = "mordekaiserchildrenofthegrave",
                SpellName = "mordekaiserchildrenofthegrave",
                Slot = SpellSlot.R,
                Delay = 0
            });

            CleanseBuffs.Add(new GameBuff
            {
                ChampionName = "Poppy",
                BuffName = "poppydiplomaticimmunity",
                SpellName = "poppydiplomaticimmunity",
                Slot = SpellSlot.R,
                Delay = 0
            });

            CleanseBuffs.Add(new GameBuff
            {
                ChampionName = "Skarner",
                BuffName = "skarnerimpale",
                SpellName = "skarnerimpale",
                Slot = SpellSlot.R,
                Delay = 0
            });

            CleanseBuffs.Add(new GameBuff
            {
                ChampionName = "Urgot",
                BuffName = "urgotswap2",
                SpellName = "urgotswap2",
                Slot = SpellSlot.R,
                Delay = 0
            });

            CleanseBuffs.Add(new GameBuff
            {
                ChampionName = "Vladimir",
                BuffName = "vladimirhemoplague",
                SpellName = "vladimirhemoplague",
                Slot = SpellSlot.R,
                Delay = 2000
            });

            CleanseBuffs.Add(new GameBuff
            {
                ChampionName = "Morgana",
                BuffName = "soulshackles",
                SpellName = "soulshackles",
                Slot = SpellSlot.R,
                Delay = 1000
            });

            EvadeBuffs.Add(new GameBuff
            {
                ChampionName = "Karthus",
                BuffName = "fallenonetarget",
                SpellName = "fallenone",
                Slot = SpellSlot.R,
                Delay = 2500
            });

            EvadeBuffs.Add(new GameBuff
            {
                ChampionName = "Morgana",
                BuffName = "soulshackles",
                SpellName = "soulshackles",
                Slot = SpellSlot.R,
                Delay = 2500
            });

            EvadeBuffs.Add(new GameBuff
            {
                ChampionName = "Vladimir",
                BuffName = "vladimirhemoplague",
                SpellName = "vladimirhemoplague",
                Slot = SpellSlot.R,
                Delay = 4500
            });

            EvadeBuffs.Add(new GameBuff
            {
                ChampionName = "Zed",
                BuffName = "zedulttargetmark",
                SpellName = "zedult",
                Slot = SpellSlot.R,
                Delay = 2800
            });

            EvadeBuffs.Add(new GameBuff
            {
                ChampionName = "Caitlyn",
                BuffName = "caitlynaceinthehole",
                SpellName = "caitlynaceinthehole",
                Slot = SpellSlot.R,
                Delay = 1000
            });
        }
    }

    public class SpellList<T> : List<T>
    {
        public event EventHandler OnAdd;
        public event EventHandler OnRemove;

        public new void Add(T item)
        {
            if (OnAdd != null)
            {
                OnAdd(this, null); // TODO: return item
            }

            base.Add(item);
        }

        public new void Remove(T item)
        {
            if (OnRemove != null)
            {
                OnRemove(this, null); // TODO: return item
            }

            base.Remove(item);
        }

        public new void RemoveAll(Predicate<T> match)
        {
            if (OnRemove != null)
            {
                OnRemove(this, null); // TODO: return items
            }

            base.RemoveAll(match);
        }
    }
#endregion
}
