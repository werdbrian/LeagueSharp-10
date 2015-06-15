using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using ClipperLib;
using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;
using SS = LeagueSharp.SpellSlot;

namespace ChallengerSeries.Utils
{
    internal class Positioning
    {
        public static SS[] SpellSlots = {SS.Q, SS.W, SS.E, SS.R};
        public static List<Geometry.Polygon> DangerZone()
        {
            var polygons = new List<Geometry.Polygon>();
            foreach (
                var enemy in
                    ObjectManager.Player.GetEnemiesInRange(1400)
                        .OrderBy(e => e.Distance(ObjectManager.Player.Position)).Where(e => e.IsVisible && e.IsValidTarget()))
            {
                if (enemy.IsDead) continue;
                var highestSpellRange = 0;
                foreach (var slot in SpellSlots)
                {
                    var spell = enemy.Spellbook.GetSpell(slot);
                    if (enemy.Spellbook.CanUseSpell(slot) != SpellState.NotLearned &&
                        (enemy.Spellbook.CanUseSpell(slot) != SpellState.Cooldown || spell.IsReady()))
                    {
                        var sd = SpellDb.GetByName(spell.Name);
                        if (sd != null)
                        {
                            if (sd.Range > highestSpellRange && sd.Range < 2000) highestSpellRange = (int)sd.Range;
                        }
                    }
                }
                polygons.Add(new Geometry.Circle(enemy.Position.To2D(), highestSpellRange).ToPolygon());
            }
            return polygons;
        }

        public static int EnemyCC()
        {
            var i = 0;
            foreach (
                var enemy in
                    HeroManager.Enemies.Where(h => !h.IsDead && h.Distance(ObjectManager.Player) < 1400 && h.IsMeele))
            {
                foreach (var slot in SpellSlots)
                {
                    var spell = enemy.Spellbook.GetSpell(slot);
                    if (enemy.Spellbook.CanUseSpell(slot) != SpellState.NotLearned &&
                        (enemy.Spellbook.CanUseSpell(slot) != SpellState.Cooldown || spell.IsReady()))
                    {
                        var sd = SpellDb.GetByName(spell.Name);
                        if (sd == null) continue;
                        var cct = sd.CcType;
                        if (ObjectManager.Player.BaseSkinName == "Katarina")
                        {
                            if (cct != CcType.No && cct != CcType.Slow && cct != CcType.Snare)
                            {
                                i++;
                            }
                        }
                        else
                        {
                            if (cct != CcType.No && cct != CcType.Slow && sd.Type == SpellType.Targeted)
                            {
                                i++;
                            }
                        }
                    }
                }
            }
            return i;
        }
    }
}
