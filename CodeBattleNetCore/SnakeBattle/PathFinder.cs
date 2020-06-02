using System;
using System.Collections.Generic;
using System.Diagnostics;
using Client.Models;
using SnakeBattle.Api;

namespace Client
{
    public delegate bool IsGoal(BoardElement element, BoardPoint point, Func<int> requestRouteLength);

    public class PathFinder
    {
        private readonly GameBoard _board;

        public PathFinder(GameBoard board, bool isFury)
        {
            _board = board;
            IsFury = isFury;
        }
        
        public bool IsFury { get; }

        public IReadOnlyList<Route> FindRoutes(BoardPoint start, IsGoal isGoal, bool stopOnFirstGoal = false,
            int? maxLength = null)
        {
            var result = new List<Route>();

            var frontier = new Queue<BoardPoint>();
            frontier.Enqueue(start);

            var cameFrom = new Dictionary<BoardPoint, BoardPoint> {[start] = new BoardPoint()};

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                var currentElement = _board.GetElementAt(current);

                var lazyRouteLength = new Lazy<int>(() => GetRouteLength(cameFrom, start, current));

                if (isGoal(currentElement, current, () => lazyRouteLength.Value))
                {
                    result.Add(CreateRoute(cameFrom, start, current, currentElement));
                    if (stopOnFirstGoal)
                        break;
                }

                if (maxLength.HasValue && lazyRouteLength.Value > maxLength)
                    break;

                foreach (var next in GetNeighbors(current))
                {
                    if (!cameFrom.ContainsKey(next))
                    {
                        frontier.Enqueue(next);
                        cameFrom[next] = current;
                    }
                }
            }

            return result;
        }

        private IEnumerable<BoardPoint> GetNeighbors(BoardPoint point)
        {
            {
                var neighbor = point.ShiftLeft();
                if (IsAcceptablePath(neighbor))
                    yield return neighbor;
            }
            {
                var neighbor = point.ShiftRight();
                if (IsAcceptablePath(neighbor))
                    yield return neighbor;
            }
            {
                var neighbor = point.ShiftTop();
                if (IsAcceptablePath(neighbor))
                    yield return neighbor;
            }
            {
                var neighbor = point.ShiftBottom();
                if (IsAcceptablePath(neighbor))
                    yield return neighbor;
            }
        }

        private bool IsAcceptablePath(BoardPoint leftPoint)
        {
            var left = _board.GetElementAt(leftPoint);
            switch (left, _isFury: IsFury)
            {
                case (BoardElement.None, _):
                case (BoardElement.FuryPill, _):
                case (BoardElement.Apple, _):
                case (BoardElement.Gold, _):
                // cell is left in the next round
                case (BoardElement.EnemyTailEndDown, _):
                case (BoardElement.EnemyTailEndLeft, _):
                case (BoardElement.EnemyTailEndUp, _):
                case (BoardElement.EnemyTailEndRight, _):
                case (BoardElement.EnemyTailInactive, _):
                case (BoardElement.Stone, true):
                // hunting
                case (BoardElement.EnemyBodyHorizontal, true):
                case (BoardElement.EnemyBodyVertical, true):
                case (BoardElement.EnemyBodyLeftDown, true):
                case (BoardElement.EnemyBodyLeftUp, true):
                case (BoardElement.EnemyBodyRightDown, true):
                case (BoardElement.EnemyBodyRightUp, true):
                case (BoardElement.EnemyHeadDown, true):
                case (BoardElement.EnemyHeadLeft, true):
                case (BoardElement.EnemyHeadRight, true):
                case (BoardElement.EnemyHeadUp, true):
                    return true;
                default:
                    return false;        
            }
        }

        private Route CreateRoute(IReadOnlyDictionary<BoardPoint, BoardPoint> map, BoardPoint start, BoardPoint goal,
            BoardElement goalElement)
        {
            var current = goal;
            var path = new List<BoardPoint>(map.Count - 1)
            {
                current
            };
            while (current != start)
            {
                current = map[current];
                path.Add(current);
            }

            path.Reverse();
            return new Route(path, goalElement, IsFury);
        }

        private int GetRouteLength(IReadOnlyDictionary<BoardPoint, BoardPoint> map, BoardPoint start, BoardPoint goal)
        {
            var current = goal;
            int length = 0;
            while (current != start)
            {
                current = map[current];
                length++;
            }

            return length;
        }
    }
}