using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;
using GamePath = System.Collections.Generic.List<SharpDX.Vector2>;

namespace ChallengerSeries.Utils
{
    internal static class Extensions
    {
        private static Obj_AI_Hero Player = ObjectManager.Player;
        public static bool UnderTurret(this Vector3 pos, GameObjectTeam TurretTeam)
        {
            return ObjectManager.Get<Obj_AI_Turret>().Any(t => t.Distance(pos) < 600 && t.Team == TurretTeam);
        }

        public static int IncludePing(this float delay)
        {
            return (int)Math.Round(delay + (Game.Ping / 2000f + 0.06f));
        }

        public static Vector2 PositionAfter(this GamePath self, int t, int speed, int delay = 0)
        {
            var distance = Math.Max(0, t - delay) * speed / 1000;
            for (var i = 0; i <= self.Count - 2; i++)
            {
                var from = self[i];
                var to = self[i + 1];
                var d = (int)to.Distance(from);
                if (d > distance)
                {
                    return from + distance * (to - from).Normalized();
                }
                distance -= d;
            }
            return self[self.Count - 1];
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
            return MinionManager.GetMinions(pos, 150)
                .Any(
                    m =>
                        m.BaseSkinName.Contains("mine") || m.BaseSkinName.Contains("trap") ||
                        m.BaseSkinName.Contains("shroom") || m.BaseSkinName.Contains("cait")) && !HeroManager.Enemies.Any(h => h.IsMelee() && h.Distance(pos) < 150);
        }

        public static IEnumerable<Vector3> GetCondemnPositions(Vector3 position)
        {
            var pointList = new List<Vector3>();

            for (var j = 485; j >= 50; j -= 100)
            {
                var offset = (int)(2 * Math.PI * j / 100);

                for (var i = 0; i <= offset; i++)
                {
                    var angle = i * Math.PI * 2 / offset;
                    var point =
                        new Vector2(
                            (float)(position.X + j * Math.Cos(angle)),
                            (float)(position.Y - j * Math.Sin(angle))).To3D();

                    if (point.IsWall())
                    {
                        pointList.Add(point);
                    }
                }
            }

            return pointList;
        }

        public static IOrderedEnumerable<Obj_AI_Hero> OrderByPriority(this IEnumerable<Obj_AI_Hero> heroes)
        {
            return heroes.OrderBy(TargetSelector.GetPriority);
        }

        public static bool IsKillable(this Obj_AI_Hero hero)
        {
            return Player.GetAutoAttackDamage(hero)*2 < hero.Health;
        }

        public static bool IsCollisionable(this Vector3 pos)
        {
            return NavMesh.GetCollisionFlags(pos).HasFlag(CollisionFlags.Wall) || 
                (NavMesh.GetCollisionFlags(pos).HasFlag(CollisionFlags.Building) && Player.Distance(pos) < 550);
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
