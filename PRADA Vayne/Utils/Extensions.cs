using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;
using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;
using GamePath = System.Collections.Generic.List<SharpDX.Vector2>;

namespace PRADA_Vayne.Utils
{
    internal static class Extensions
    {
        private static Obj_AI_Hero Player = ObjectManager.Player;
        private static int drawn = 0;

        public static bool IsCondemnable(this Obj_AI_Hero hero)
        {
            var pP = Heroes.Player.ServerPosition;
            var p = hero.ServerPosition;
            /*var pD = pP.Distance(p) + 385;

            if (!pP.Extend(p, pD).IsCollisionable() && !pP.Extend(p, pD/2).IsCollisionable()) return false;*/

            var pD = 385;
            if (!p.Extend(pP, -pD).IsCollisionable() && !p.Extend(pP, -pD / 2).IsCollisionable() && !p.Extend(pP, -pD / 3).IsCollisionable()) return false;

            if (!hero.CanMove || hero.IsWindingUp)/* || (!hero.IsMoving && hero.HealthPercent > Heroes.Player.HealthPercent))*/ return true;

            var eT = 0.063 + Game.Ping/2000 + 0.06;
            eT += (double)Program.ComboMenu.Item("EHitchance").GetValue<Slider>().Value*4/1000;
            var d = hero.MoveSpeed * eT;
            
            var pList = new List<Vector3>();
            pList.Add(hero.ServerPosition);
            

            for (var i = 0; i <= 360; i += 60)
            {
                var v3 = new Vector2((int) (p.X + d*Math.Cos(i)), (int) (p.Y - d*Math.Sin(i))).To3D();
                pList.Add(v3.Extend(pP, -pD));
            }

            return pList.All(el => el.IsCollisionable());
        }

        public static Vector3 GetTumblePos(this Obj_AI_Hero target)
        {
            //if the target is not a melee and he's alone he's not really a danger to us, proceed to 1v1 him :^ )
            if (!target.IsMelee && Heroes.Player.CountEnemiesInRange(800) == 1) return Game.CursorPos;

            var aRC = new Geometry.Circle(Heroes.Player.ServerPosition.To2D(), 300).ToPolygon().ToClipperPath();
            var tP = target.ServerPosition;
            var pList = new List<Vector3>();
            var additionalDistance = (0.106 + Game.Ping/2000) * target.MoveSpeed;
            foreach (var p in aRC)
            {
                var v3 = new Vector2(p.X, p.Y).To3D();

                if (target.IsFacing(Heroes.Player))
                {
                    if (!v3.UnderTurret(true) && v3.Distance(tP) > 325 && v3.Distance(tP) < 550 &&
                        (v3.CountEnemiesInRange(425) <= v3.CountAlliesInRange(325))) pList.Add(v3);
                }
                else
                {
                    if (!v3.UnderTurret(true) && v3.Distance(tP) > 325 &&
                        v3.Distance(tP) < (550 - additionalDistance) &&
                        (v3.CountEnemiesInRange(425) <= v3.CountAlliesInRange(325))) pList.Add(v3);
                }
            }
            return pList.Count > 1 ? pList.OrderByDescending(el => el.Distance(tP)).FirstOrDefault() : Vector3.Zero;
        }

        public static int VayneWStacks(this Obj_AI_Base o)
        {
            if (o == null) return 0;
            if (o.Buffs.FirstOrDefault(b => b.Name.Contains("vaynesilver")) == null || !o.Buffs.Any(b => b.Name.Contains("vaynesilver"))) return 0;
            return o.Buffs.FirstOrDefault(b => b.Name.Contains("vaynesilver")).Count;
        }

        public static Vector3 Randomize(this Vector3 pos)
        {
            var r = new Random(Environment.TickCount);
            return new Vector2(pos.X + r.Next(-150, 150), pos.Y + r.Next(-150, 150)).To3D();
        }

        public static bool IsShroom(this Vector3 pos)
        {
            return pos == Vector3.Zero || HeroManager.Enemies.Any(e => !e.IsDead && e.IsVisible && e.Distance(pos) < 300) && MinionManager.GetMinions(pos, 150)
                .Any(m => m.CharData.Name.Contains("mine") || m.CharData.Name.Contains("trap") ||
                        m.CharData.Name.Contains("shroom") || m.CharData.Name.Contains("cait")) && !HeroManager.Enemies.Any(h => h.IsMelee() && h.Distance(pos) < 200);
        }

        public static bool IsKillable(this Obj_AI_Hero hero)
        {
            return Player.GetAutoAttackDamage(hero) * 2 < hero.Health;
        }

        public static bool IsCollisionable(this Vector3 pos)
        {
            return NavMesh.GetCollisionFlags(pos).HasFlag(CollisionFlags.Wall) ||
                (Program.Orbwalker.ActiveMode == MyOrbwalker.OrbwalkingMode.Combo && NavMesh.GetCollisionFlags(pos).HasFlag(CollisionFlags.Building));
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
