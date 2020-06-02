using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using System.Xml.Linq;
using SnakeBattle.Api;

namespace Client
{
    public class AStar
    {
        public SnakeAction DoRun(GameBoard gameBoard)
        {
            var start = gameBoard.GetMyHead();
            if (start == null)
                return new SnakeAction(false, Direction.Stop);

            var routes = new ConcurrentBag<IReadOnlyList<BoardPoint>>();
            var targets = NiceTargets(gameBoard);

            Parallel.ForEach(targets, point =>
            {
                try
                {
                    var path = FindPath(start.Value, point, gameBoard);
                    if (path.Count > 0)
                        routes.Add(path);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });

            var bestRoutes = routes.OrderBy(r => r.Count).ToList();
            // todo: assign properties to each GOAL (route): enemy snake is near, amount of deadends, enemy with enrage is near, etc.
            CheckDeadend(bestRoutes, gameBoard);

            var best = bestRoutes.FirstOrDefault();
            if (best != null)
            {
                var next = best.Skip(1).First();
                Report(start.Value, next, best, gameBoard);

                if (next.X < start.Value.X)
                    return new SnakeAction(false, Direction.Left);
                if (next.X > start.Value.X)
                    return new SnakeAction(false, Direction.Right);
                if (next.Y < start.Value.Y)
                    return new SnakeAction(false, Direction.Up);
                if (next.Y > start.Value.Y)
                    return new SnakeAction(false, Direction.Down);
            }

            return new SnakeAction(false, Direction.Stop);
        }

        private static IEnumerable<BoardPoint> NiceTargets(GameBoard gameBoard)
        {
            return gameBoard.GetApples()
                .Concat(gameBoard.GetGold())
                .Concat(gameBoard.GetFuryPills());
        }

        private void CheckDeadend(List<IReadOnlyList<BoardPoint>> bestRoutes, GameBoard gameBoard)
        {
            var toExclude = new ConcurrentBag<IReadOnlyList<BoardPoint>>();
            Parallel.ForEach(bestRoutes, route =>
            {
                try
                {
                    var goal = route.Last();
                    var otherTargets = NiceTargets(gameBoard).Except(new[] {goal});
                    foreach (var target in otherTargets)
                    {
                        var path = FindPath(goal, target, gameBoard);
                        if (path.Count > 0)
                            return;
                    }

                    toExclude.Add(route);
                    Console.WriteLine($"DEADEND {goal} {gameBoard.GetElementAt(goal)}");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });

            foreach (var deadend in toExclude)
                bestRoutes.Remove(deadend);
        }

        private void Report(BoardPoint start, BoardPoint next, IReadOnlyList<BoardPoint> best, GameBoard gameBoard,
            string name = "GOTO")
        {
            var last = best.Last();
            Console.WriteLine($"{name} {gameBoard.GetElementAt(last)} at {last}\t{start} -> {next}");
        }


        private IReadOnlyList<BoardPoint> FindPath(BoardPoint start, BoardPoint goal, GameBoard board)
        {
            var frontier = new Queue<BoardPoint>();
            frontier.Enqueue(start);

            var cameFrom = new Dictionary<BoardPoint, BoardPoint> {[start] = new BoardPoint()};

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                foreach (var next in GetNeighbors(current, board))
                {
                    if (!cameFrom.ContainsKey(next))
                    {
                        frontier.Enqueue(next);
                        cameFrom[next] = current;
                    }
                }
            }

            if (!cameFrom.ContainsKey(goal))
                return Array.Empty<BoardPoint>();

            {
                var current = goal;
                var path = new List<BoardPoint>(cameFrom.Count - 1)
                {
                    current
                };
                while (current != start)
                {
                    current = cameFrom[current];
                    path.Add(current);
                }

                path.Reverse();
                return path;
            }
        }

        private IEnumerable<BoardPoint> GetNeighbors(BoardPoint point, GameBoard board)
        {
            {
                var neighbor = point.ShiftLeft();
                if (IsAcceptablePath(board, neighbor))
                    yield return neighbor;
            }
            {
                var neighbor = point.ShiftRight();
                if (IsAcceptablePath(board, neighbor))
                    yield return neighbor;
            }
            {
                var neighbor = point.ShiftTop();
                if (IsAcceptablePath(board, neighbor))
                    yield return neighbor;
            }
            {
                var neighbor = point.ShiftBottom();
                if (IsAcceptablePath(board, neighbor))
                    yield return neighbor;
            }
        }

        private static bool IsAcceptablePath(GameBoard board, BoardPoint leftPoint)
        {
            var left = board.GetElementAt(leftPoint);
            return left == BoardElement.None || left == BoardElement.FuryPill ||
                   left == BoardElement.Apple || left == BoardElement.Gold;
        }
    }
}