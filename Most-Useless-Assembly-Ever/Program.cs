using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;
using ObjectManager = LeagueSharp.ObjectManager;
namespace Most_Useless_Assembly_Ever
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Slack;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        public static void Slack()
        {
                //IM NOT DOING ANYTHIN AT ALL LOL
        }
        public static void Drawing_OnDraw(EventArgs args)
        {

            Render.Circle.DrawCircle(Player.Position, 600, Q.IsReady() ? Color.Green : Color.Red);
            var pos = ObjectManager.Player.Position.To2D() + 600 * ObjectManager.Player.Direction.To2D().Perpendicular();
            Drawing.DrawCircle(pos.To3D(), 50, Q.IsReady() ? Color.Green : Color.Red);
            var playerPosition = ObjectManager.Player.Position.To2D();
            var direction = ObjectManager.Player.Direction.To2D().Perpendicular();
            var currentScreenPlayer = Drawing.WorldToScreen(ObjectManager.Player.Position);
            const int distance = 600;
            var currentAngel = 30 * (float) Math.PI / 180;
            var currentCheckPoint1 = playerPosition + distance * direction.Rotated(currentAngel);
            currentAngel = 335 * (float) Math.PI / 180;
            var currentCheckPoint2 = playerPosition + distance * direction.Rotated(currentAngel);
            var currentScreenCheckPoint1 = Drawing.WorldToScreen(currentCheckPoint1.To3D());
            var currentScreenCheckPoint2 = Drawing.WorldToScreen(currentCheckPoint2.To3D());
            Drawing.DrawLine(currentScreenPlayer.X, currentScreenPlayer.Y, currentScreenCheckPoint1.X, currentScreenCheckPoint1.Y, 2,Q.IsReady() ? Color.Green : Color.Red);
            Drawing.DrawLine(currentScreenPlayer.X, currentScreenPlayer.Y, currentScreenCheckPoint2.X, currentScreenCheckPoint2.Y,2, Q.IsReady() ? Color.Green : Color.Red);
                    
    }
}
