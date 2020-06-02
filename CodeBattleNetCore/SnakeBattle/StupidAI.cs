// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Roy_T.AStar.Grids;
// using Roy_T.AStar.Paths;
// using Roy_T.AStar.Primitives;
// using SnakeBattle.Api;
// using static SnakeBattle.Api.BoardElement;
//
// namespace Client
// {
//     public class StupidAi
//     {
//         public SnakeAction DoRun(GameBoard gameBoard)
//         {
//             var dead = gameBoard.FindFirstElement(HeadDead, HeadSleep);
//             if (dead != null)
//                 return new SnakeAction(false, Direction.Stop);
//             
//             var grid = CreateGrid(gameBoard);
//
//             var pathFinder = new PathFinder();
//
//             var me = gameBoard.GetMyHead();
//             if (me == null)
//                 return new SnakeAction(false, Direction.Stop);
//
//             List<Path> results = new List<Path>();
//             foreach (var apple in gameBoard.GetApples())
//             {
//                 var path = pathFinder.FindPath(me.Value.ToPosition(), apple.ToPosition(), grid);
//                 results.Add(path);
//                 
//                 // todo: check deadend
//             }
//             
//             var target = results.OrderBy(p => p.Distance).First();
//             var edge = target.Edges.First();
//             int x = (int) Math.Round(edge.End.Position.X);
//             int y = (int) Math.Round(edge.End.Position.X);
//
//             if (x < me.Value.X)
//                 return new SnakeAction(false, Direction.Left);
//             if (x > me.Value.X)
//                 return new SnakeAction(false, Direction.Right);
//             if (y < me.Value.Y)
//                 return new SnakeAction(false, Direction.Down);
//             return new SnakeAction(false, Direction.Up);
//         }
//
//         private static Grid CreateGrid(GameBoard gameBoard)
//         {
//             var gridSize = new GridSize(gameBoard.Size, gameBoard.Size);
//             var cellSize = new Size(Distance.FromMeters(1), Distance.FromMeters(1));
//             var traversalVelocity = Velocity.FromMetersPerSecond(1);
//
//             var grid = Grid.CreateGridWithLateralConnections(gridSize, cellSize, traversalVelocity);
//
//             foreach (var wall in gameBoard.GetWalls())
//                 grid.RemoveEdge(wall.ToPosition(), wall.ToPosition());
//
//             foreach (var stone in gameBoard.GetStones())
//                 grid.RemoveEdge(stone.ToPosition(), stone.ToPosition());
//             
//             return grid;
//         }
//
//         
//     }
// }