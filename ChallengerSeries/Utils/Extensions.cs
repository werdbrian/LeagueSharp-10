using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace ChallengerSeries.Utils
{
    internal static class Extensions
    {
        private static Obj_AI_Hero Player = ObjectManager.Player;
        public static bool UnderTurret(this Vector3 pos, GameObjectTeam TurretTeam)
        {
            return ObjectManager.Get<Obj_AI_Turret>().Any(t => t.Distance(pos) < 600 && t.Team == TurretTeam);
        }

        public static int VayneWStacks(this Obj_AI_Base o)
        {
            if (!o.Buffs.Any(b => b.Name.ToLower().Contains("vaynesilver"))) return 0;
            return o.Buffs.First(b => b.Name.Contains("vaynesilver")).Count;
        }

        public static Vector3 Randomize(this Vector3 pos)
        {
            var r = new Random(Environment.TickCount);
            return new Vector2(pos.X + r.Next(-150, 150), pos.Y + r.Next(-150, 150)).To3D();
        }

        public static bool InAArange(this GameObject obj)
        {
            return ObjectManager.Player.Distance(obj.Position) < ObjectManager.Player.AttackRange - 15;
        }

        public static bool IsShroom(this Vector3 pos)
        {
            return MinionManager.GetMinions(pos, 150, MinionTypes.All,
                MinionTeam.Enemy)
                .Any(
                    m =>
                        m.BaseSkinName.Contains("mine") || m.BaseSkinName.Contains("trap") ||
                        m.BaseSkinName.Contains("shroom") || m.BaseSkinName.Contains("cait"));
        }

        public static bool IsKillable(this Obj_AI_Hero hero)
        {
            return Player.GetAutoAttackDamage(hero)*2 < hero.Health;
        }

        public static bool IsCollisionable(this Vector3 pos)
        {
            return NavMesh.GetCollisionFlags(pos).HasFlag(CollisionFlags.Wall) || 
                NavMesh.GetCollisionFlags(pos).HasFlag(CollisionFlags.Building);
        }
        public static bool IsValidState(this Obj_AI_Hero target)
        {
            return !target.HasBuffOfType(BuffType.SpellShield) && !target.HasBuffOfType(BuffType.SpellImmunity) &&
                   !target.HasBuffOfType(BuffType.Invulnerability);
        }

        public static int CountHerosInRange(this Obj_AI_Hero target, bool checkteam, float range = 1200f)
        {
            var objListTeam =
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(
                        x => x.IsValidTarget(range, false));

            return objListTeam.Count(hero => checkteam ? hero.Team != target.Team : hero.Team == target.Team);
        }
    }
}
