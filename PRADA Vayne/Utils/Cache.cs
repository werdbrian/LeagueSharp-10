using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

// ReSharper disable InconsistentNaming

namespace PRADA_Vayne.Utils
{
    public static class Turrets
    {
        private static List<Obj_AI_Turret> _turrets;

        public static List<Obj_AI_Turret> AllyTurrets
        {
            get { return _turrets.FindAll(t => t.IsAlly); }
        }
        public static List<Obj_AI_Turret> EnemyTurrets
        {
            get { return _turrets.FindAll(t => t.IsEnemy); }
        }

        public static void Load()
        {
            Utility.DelayAction.Add(2000, () => _turrets = ObjectManager.Get<Obj_AI_Turret>().ToList());
            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += OnDelete;
        }

        private static void OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.Type != GameObjectType.obj_AI_Turret) return;
            if (!_turrets.Contains(sender))
            {
                _turrets.Add(sender as Obj_AI_Turret);
            }
        }

        private static void OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.Type != GameObjectType.obj_AI_Turret) return;
            _turrets.RemoveAll(t => t.NetworkId == sender.NetworkId);
        }
    }

    public static class HeadQuarters
    {
        private static List<Obj_HQ> _headQuarters;

        public static Obj_HQ AllyHQ
        {
            get { return _headQuarters.FirstOrDefault(t => t.IsAlly); }
        }
        public static Obj_HQ EnemyHQ
        {
            get { return _headQuarters.FirstOrDefault(t => t.IsEnemy); }
        }

        public static void Load()
        {
            Utility.DelayAction.Add(3000, () => _headQuarters = ObjectManager.Get<Obj_HQ>().ToList());
        }
    }

    public static class Heroes
    {
        private static List<Obj_AI_Hero> _heroes;

        public static Obj_AI_Hero Player = ObjectManager.Player;

        public static List<Obj_AI_Hero> AllyHeroes
        {
            get { return _heroes.FindAll(h => h.IsAlly); }
        }

        public static List<Obj_AI_Hero> EnemyHeroes
        {
            get { return _heroes.FindAll(h => h.IsEnemy); }
        }

        public static void Load()
        {
            Player = ObjectManager.Player;
            Utility.DelayAction.Add(1000, () => _heroes = ObjectManager.Get<Obj_AI_Hero>().ToList());
        }
    }

    public static class Cache
    {
        public static void Load()
        {
            Turrets.Load();
            HeadQuarters.Load();
            Heroes.Load();
        }
    }
}
