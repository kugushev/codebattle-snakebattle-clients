using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Client.Models;
using SnakeBattle.Api;
using static SnakeBattle.Api.BoardElement;

namespace Client
{
    public class Brain
    {
        private const int FearArea = 5;
        private const int FuryArea = 10;

        public SnakeAction DoRun(GameBoard gameBoard)
        {
            var stopwatch = Stopwatch.StartNew();
            var action = Think(gameBoard, stopwatch);
            Console.WriteLine(stopwatch.Elapsed);
            return action;
        }

        private SnakeAction Think(GameBoard gameBoard, Stopwatch stopwatch)
        {
            var start = gameBoard.GetMyHead();
            if (start == null)
                return new SnakeAction(false, Direction.Stop);
            var startElement = gameBoard.GetElementAt(start.Value);
            if (startElement == HeadSleep || startElement == HeadDead)
                return new SnakeAction(false, Direction.Stop);


            var isFury = gameBoard.AmIEvil();

            var pathFinder = new PathFinder(gameBoard, isFury);

            // find all apples/coins
            var routes = pathFinder.FindRoutes(start.Value,
                (elem, point, requestRoute) => ConsiderGoal(elem, point, requestRoute, isFury));

            // find weighted routes
            var max = routes.OrderByDescending(r => r.Length).First();
            Console.WriteLine($"Max: {max.Length}");
            var weighted = FindWeightedRoutes(routes, pathFinder, gameBoard, max);

            // avoid deadend
            var possible = weighted.Where(w => !w.Properties.IsDeadEnd).ToList();
            if (possible.Count == 0)
            {
                Console.WriteLine("DEADEND!!!!");
                // choose the longest: in the future it will be changed
                return RouteToAction(weighted.OrderByDescending(r => r.Route.Length).First(), start.Value);
            }


            // make a decision
            var best = Decide(possible, max, isFury, start.Value, pathFinder);

            return RouteToAction(best, start.Value);
        }

        private WeightedRoute Decide(List<WeightedRoute> possible, Route median, bool isFury, BoardPoint start,
            PathFinder pathFinder)
        {
            if (isFury)
            {
                // check hunting
                var bite = possible.OrderByDescending(r => r.BiteEnemyProbability()).First();
                if (bite.BiteEnemyProbability() >= 0.25f)
                {
                    Console.WriteLine("BITE");
                    return bite;
                }
            }
            else
            {
                // check avoidance
                var evil = FindEvilNear(start, pathFinder, FearArea);
                if (evil.Count > 0)
                {
                    Console.WriteLine("RUN AWAY");
                    return possible.OrderBy(r => r.Properties.EvilIsNear.Count).First();
                }
            }

            // check further hunting
            // var toHunt = possible
            //     .Where(r => r.Route.GoalElement == FuryPill && r.Route.Length < 5)
            //     .OrderByDescending(r => r.Properties.PreyInArea.Count)
            //     .ThenBy(r => r.Route.Length)
            //     .FirstOrDefault();
            //
            // if (toHunt != null)
            // {
            //     Console.WriteLine("TO HUNT");
            //     return toHunt;
            // }


            // use income rule
            var byIncomeFull = possible.OrderByDescending(r => r.Route.Income + r.PotentialIncome(median.Length))
                .ToList();

            return byIncomeFull.First();
        }


        private bool ConsiderGoal(BoardElement elem, BoardPoint point, Func<int> requestRouteLength,
            bool isFury)
        {
            if (elem == Apple || elem == Gold)
                return true;

            if (elem == FuryPill)
            {
                // route is short
                return true; //requestRouteLength() < 7;

                // var toEnemy = pathFinder.FindRoutes(point, (element, boardPoint, _) => element.IsEnemy());
                // return toEnemy.Any(r => r.Length < 7);
            }

            if (isFury)
            {
                var length = requestRouteLength();
                switch (elem)
                {
                    // we can eat stones
                    case Stone:
                        return length < 12;
                    // we can bite other snakes bodies, don't try to eat tail: you're never reach it
                    case EnemyBodyHorizontal:
                    case EnemyBodyVertical:
                    case EnemyBodyLeftDown:
                    case EnemyBodyLeftUp:
                    case EnemyBodyRightDown:
                    case EnemyBodyRightUp:
                    // no problem in eating head: it is going to replace with body in the next round
                    case EnemyHeadDown:
                    case EnemyHeadLeft:
                    case EnemyHeadRight:
                    case EnemyHeadUp:
                        return length < 6;
                }
            }

            return false;
        }

        private SnakeAction RouteToAction(WeightedRoute best, BoardPoint start)
        {
            if (best != null)
            {
                var next = best.Route.Path.Skip(1).First();
                Report(start, next, best);

                if (next.X < start.X)
                    return new SnakeAction(false, Direction.Left);
                if (next.X > start.X)
                    return new SnakeAction(false, Direction.Right);
                if (next.Y < start.Y)
                    return new SnakeAction(false, Direction.Up);
                if (next.Y > start.Y)
                    return new SnakeAction(false, Direction.Down);
            }

            return new SnakeAction(false, Direction.Stop);
        }

        private void Report(BoardPoint start, BoardPoint next, WeightedRoute route, string name = "GOTO") =>
            Console.WriteLine(
                $"{name} {route.Route.GoalElement} at {route.Route.GoalPoint}\t{start} -> {next}. L:{route.Route.Length} I:{route.Route.Income:00.0000} P:{route.LastPotentialIncome:00.0000}");

        #region Weighted Route

        private IReadOnlyCollection<WeightedRoute> FindWeightedRoutes(IReadOnlyList<Route> routes,
            PathFinder pathFinder, GameBoard gameBoard, Route max)
        {
            var weighted = new ConcurrentBag<WeightedRoute>();

            Parallel.ForEach(routes, route =>
            {
                var furtherRoute = FindAnyFurtherRoute(route, pathFinder, max.Length / 2);
                int lookupAreaSize = max.Length - route.Length;
                var routesInArea = FindRoutesInArea(route, pathFinder, lookupAreaSize);
                var routeProperties = new RouteProperties
                {
                    IsDeadEnd = !furtherRoute.Any(),
                    TargetIsFury = route.GoalElement == FuryPill,
                    RoutesInArea = routesInArea,
                    EvilIsNear = FindEvilNear(route.GoalPoint, pathFinder, FearArea),
                    PreyInArea = FindPreyInArea(route, pathFinder, FuryArea),

                    BonusesInTarget = FindBonusesConcentration(route.GoalPoint, gameBoard, 6),
                    EnemiesInTarget = FindEnemyConcentration(route.GoalPoint, gameBoard, 6),
                    EnemyIsNear = FindEnemyConcentration(route.Path.Skip(1).First(), gameBoard, 6),
                    //EvilIsNear = FindEvilSnakesConcentration(route.Path.Skip(1).First(), gameBoard, 6),
                    Hunting = route.GoalElement.IsEnemy(),
                    StoneEater = route.GoalElement == Stone
                };
                weighted.Add(new WeightedRoute(route, routeProperties));
            });

            return weighted;
        }

        private IReadOnlyList<Route> FindAnyFurtherRoute(Route parent, PathFinder pathFinder, int minLength) =>
            pathFinder.FindRoutes(parent.GoalPoint,
                (elem, point, requestRouteLength) =>
                {
                    if (parent.GoalPoint == point)
                        return false;
                    if (requestRouteLength() < minLength)
                        return false;
                    
                    var becameFury = parent.GoalElement == FuryPill;
                    return ConsiderGoal(elem, point, requestRouteLength, becameFury);
                }, true);

        private IReadOnlyList<Route> FindRoutesInArea(Route parent, PathFinder pathFinder, int size)
        {
            if (size <= 0)
                return Array.Empty<Route>();
            return pathFinder.FindRoutes(parent.GoalPoint,
                (elem, point, requestRoute) =>
                {
                    if (parent.GoalPoint == point)
                        return false;

                    var becameFury = parent.GoalElement == FuryPill;
                    return ConsiderGoal(elem, point, requestRoute, becameFury);
                }, maxLength: size);
        }

        private IReadOnlyList<Route> FindEvilNear(BoardPoint start, PathFinder pathFinder, int size)
        {
            if (size <= 0)
                return Array.Empty<Route>();
            return pathFinder.FindRoutes(start, (elem, point, requestRoute) => elem == EnemyHeadEvil, maxLength: size);
        }

        private IReadOnlyList<Route> FindPreyInArea(Route parent, PathFinder pathFinder, int size)
        {
            if (size <= 0 || !pathFinder.IsFury)
                return Array.Empty<Route>();
            return pathFinder.FindRoutes(parent.GoalPoint,
                (elem, point, requestRoute) => elem.IsEnemy(), maxLength: size);
        }

        private int FindBonusesConcentration(BoardPoint target, GameBoard gameBoard, int squareSize)
            => FindConcentration(target, gameBoard.GetApples().Concat(gameBoard.GetGold()), squareSize);

        private int FindEvilSnakesConcentration(BoardPoint target, GameBoard gameBoard, int squareSize)
            => FindConcentration(target, gameBoard.FindAllElements(EnemyHeadEvil), squareSize);

        private int FindEnemyConcentration(BoardPoint target, GameBoard gameBoard, int squareSize)
            => FindConcentration(target,
                gameBoard.FindAllElements(EnemyHeadDown, EnemyHeadUp, EnemyHeadLeft, EnemyHeadRight, EnemyHeadEvil),
                squareSize);

        private int FindConcentration(BoardPoint around, IEnumerable<BoardPoint> points, int squareSize)
        {
            int minX = around.X - squareSize;
            int maxX = around.X + squareSize;
            int minY = around.Y - squareSize;
            int maxY = around.Y + squareSize;

            return points.Count(p => p.X > minX && p.X < maxX && p.Y > minY && p.Y < maxY);
        }

        #endregion
    }
}