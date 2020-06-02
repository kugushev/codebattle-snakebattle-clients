using Roy_T.AStar.Primitives;
using SnakeBattle.Api;

namespace Client
{
    public static class Helper
    {
        public static GridPosition ToPosition(this BoardPoint point) => new GridPosition(point.X, point.Y);
    }
}