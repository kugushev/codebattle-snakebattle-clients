using System.Collections.Generic;

namespace Client.Models
{
    public class RouteProperties
    {
        public bool IsDeadEnd { get; set; }
        public bool TargetIsFury { get; set; }
        public int BonusesInTarget { get; set; }
        public int EnemyIsNear { get; set; }
        public int EnemiesInTarget { get; set; }
        public bool Hunting { get; set; }
        public bool StoneEater { get; set; }
        public IReadOnlyList<Route> RoutesInArea { get; set; }
        public IReadOnlyList<Route> EvilIsNear { get; set; }
        public IReadOnlyList<Route> PreyInArea { get; set; }
    }
}