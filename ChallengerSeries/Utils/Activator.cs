using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using ChallengerSeries.Utils;
using LeagueSharp.Common.Data;

namespace ChallengerSeries.Utils
{
    internal static class Activator
    {
        public static bool Loaded { get { return loaded; } }
        private static Obj_AI_Hero Player = ObjectManager.Player;
        private static bool loaded = false;

        public static void Load()
        {
            Game.OnUpdate += OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Notifications.AddNotification("CK Activator Loaded", 3000);
            loaded = true;
        }

        public static void Unload()
        {
            Game.OnUpdate -= OnUpdate;
            Obj_AI_Base.OnProcessSpellCast -= OnProcessSpellCast;
            Notifications.AddNotification("CK Activator Unloaded", 3000);
            loaded = false;
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

            if (!ChallengerPlugin.MiscMenu.Item("exploits").GetValue<bool>()) return;

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
            var TeamfightRange = 800;
            var TeamfightingEnemies = HeroManager.Enemies.FindAll(e => e.Distance(Player) < TeamfightRange);
            if (Items.HasItem((int) ItemId.Bilgewater_Cutlass, Player))
            {
                var target =
                    TeamfightingEnemies.FirstOrDefault(
                        e => e.Distance(Player) < 550 && !e.IsFacing(Player) && e.HealthPercent < 35);
                if (target != null)
                {
                    UseItem(ItemId.Bilgewater_Cutlass, target);
                }
            }
            if (Player.HealthPercent < 80 && Items.HasItem((int)ItemId.Blade_of_the_Ruined_King) && Items.CanUseItem((int)ItemId.Blade_of_the_Ruined_King))
            {
                var KSableEnemy =
                    HeroManager.Enemies.FirstOrDefault(e => e.Health < (e.MaxHealth * 0.1) - (e.MaxHealth * 0.1) * ((e.Armor / 10) * 8) / 100);
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
                if (Items.HasItem((int) ItemId.Quicksilver_Sash, Player) || Items.HasItem((int) ItemId.Mercurial_Scimitar))
                if (Player.HealthPercent < 70)
                {
                    UseBOTRK();
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
            if (Player.HasBuffOfType(BuffType.Suppression))
            {
                UseItem(qss);
            }
            if (Player.HasBuffOfType(BuffType.Knockup) &&
                HeroManager.Enemies.Any(e => e.BaseSkinName.ToLower().Contains("yasuo")))
            {
                UseItem(qss);
            }
            if (Player.HealthPercent < 80 || Player.CountEnemiesInRange(650) < Player.CountAlliesInRange(650))
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
                        e => e.BaseSkinName.ToLower().Contains("cassiopeia") && e.Distance(Player) < 700))
                {
                    UseItem(qss);
                }
            }
        }

        private static void UseBOTRK(Obj_AI_Hero enemy = null)
        {
            var targetsInRange = Player.GetEnemiesInRange(500).FindAll(e => !e.IsDead && e.IsValidTarget());
            if (targetsInRange.Count == 0 || !Items.CanUseItem((int)ItemId.Blade_of_the_Ruined_King)) return;
            if (enemy != null)
            {
                UseItem(ItemId.Blade_of_the_Ruined_King, enemy);
            }
            else if (targetsInRange.Count == 1 && targetsInRange.FirstOrDefault() != null)
            {
                UseItem(ItemId.Blade_of_the_Ruined_King, targetsInRange.FirstOrDefault());
            }
            else
            {
                var target =
                    targetsInRange.OrderByDescending(h => h.Health)
                        .Take((int) Math.Abs(targetsInRange.Count*0.75))
                        .OrderBy(h => h.Armor)
                        .FirstOrDefault();
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
}
