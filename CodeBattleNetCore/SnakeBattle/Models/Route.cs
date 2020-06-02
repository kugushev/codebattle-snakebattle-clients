using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using SnakeBattle.Api;

namespace Client.Models
{
    public class Route
    {
        public IReadOnlyList<BoardPoint> Path { get; }
        public BoardPoint GoalPoint { get; }
        public BoardElement GoalElement { get; }
        public int Length => Path.Count;
        public decimal Income { get; }

        public Route(IReadOnlyList<BoardPoint> path, BoardElement goalElement, bool isFurry)
        {
            Path = path;
            GoalElement = goalElement;
            GoalPoint = path.Last();

            decimal cost = GoalElement.GetCost(isFurry);
            Income = cost / Length;
        }
    }
}