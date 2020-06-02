using System;
using System.Collections.Generic;
using System.Linq;
using SnakeBattle.Api;

namespace Client.Models
{
    public class WeightedRoute
    {
        public WeightedRoute(Route route, RouteProperties properties)
        {
            Route = route;
            Properties = properties;
        }

        public Route Route { get; }
        public RouteProperties Properties { get; }
        
        public decimal LastPotentialIncome { get; private set; }

        public decimal PotentialIncome(int length)
        {
            IEnumerable<Route> subRoutes;
            if (Route.GoalElement == BoardElement.FuryPill)
                subRoutes = Properties.RoutesInArea.Where(r => r.GoalElement == BoardElement.Apple || 
                                                            r.GoalElement == BoardElement.Gold ||
                                                            r.GoalElement == BoardElement.Stone);
            else
                subRoutes = Properties.RoutesInArea.Where(r => r.GoalElement == BoardElement.Apple ||
                                                               r.GoalElement == BoardElement.Gold);

            length -= Route.Length;
            int fullLength = Route.Length;
            decimal totalCost = 0.0m;
            foreach (var subRoute in subRoutes.OrderBy(r => r.Income))
            {
                if (length < 0)
                    break;

                totalCost += subRoute.GoalElement.GetCost(Route.GoalElement == BoardElement.FuryPill);
                length -= subRoute.Length;
                fullLength += subRoute.Length;
                // todo: use true pathfinding to calculate total income
            }

            decimal totalIncome = totalCost / fullLength;
            return LastPotentialIncome = totalIncome;
        }

        public float BiteEnemyProbability()
        {
            if (Route.GoalElement.IsEnemy())
                return 1.0f / Route.Length;

            return 0;
        }
        
        

        // public int Calc()
        // {
        //     int length = Route.Path.Count;
        //     var weight = Weight.Ok;
        //
        //     if (Properties.IsDeadEnd)
        //         return (int) Weight.Awful;
        //
        //     if (length <= 3) weight = SetIfBetter(weight, Weight.Good);
        //
        //     if (Properties.TargetIsFury)
        //     {
        //         if (Properties.Hunting)
        //         {
        //             weight = SetIfBetter(weight, length <= 2 ? Weight.Better : Weight.Bad);
        //         }
        //         else
        //         {
        //             if (length <= 2) weight = SetIfBetter(weight, Weight.TheBest);
        //             else if (length <= 5) weight = SetIfBetter(weight, Weight.Good);
        //             else weight = SetIfBetter(weight, Weight.Bad);
        //         }
        //     }
        //
        //     if (Properties.BonusesInTarget > 8) weight = SetIfBetter(weight, Weight.Better);
        //     else if (Properties.BonusesInTarget > 4) weight = SetIfBetter(weight, Weight.Good);
        //     else if (Properties.BonusesInTarget > 2) weight = SetIfBetter(weight, Weight.Ok);
        //
        //     if (Properties.StoneEater)
        //     {
        //         if (length <= 5) weight = SetIfBetter(weight, Weight.TheBest);
        //         else if (length <= 10) weight = SetIfBetter(weight, Weight.Good);
        //         else weight = SetIfBetter(weight, Weight.Bad);
        //     }
        //
        //     if (Properties.Hunting)
        //     {
        //         if (Properties.EvilIsNear > 0)
        //         {
        //             weight = SetIfBetter(weight, Weight.Good);
        //         }
        //         else
        //         {
        //             if (length <= 2) weight = SetIfBetter(weight, Weight.TheBest);
        //             else if (length <= 5) weight = SetIfBetter(weight, Weight.Good);
        //             else weight = SetIfBetter(weight, Weight.Bad);
        //         }
        //     }
        //     else
        //     {
        //         // evade
        //         if (Properties.EnemyIsNear >= 3 && length > 10)
        //             weight = SetIfBetter(weight, Weight.Better);
        //
        //         if (Properties.EvilIsNear > 0 && length > 10)
        //             weight = SetIfBetter(weight, Weight.Better);
        //     }
        //
        //     return (int) weight;
        // }
        //
        // private Weight SetIfBetter(Weight current, Weight next)
        // {
        //     var currentInt = (int) current;
        //     var nextInt = (int) next;
        //     return currentInt > nextInt ? current : next;
        // }

        // public int CalcWeight()
        // {
        //     int length = Route.Path.Count; // less is better
        //
        //     int weight = length;
        //
        //     if (Properties.IsDeadEnd)
        //         weight *= 1000;
        //
        //     if (Properties.TargetIsFury)
        //     {
        //         var pickRange = Properties.Hunting ? 2 : 5;
        //         if (length < pickRange)
        //         {
        //             var newWeight = weight - length;
        //             weight = Math.Max(newWeight, 2);
        //         }
        //     }
        //
        //     if (Properties.BonusesInTarget > 3)
        //     {
        //         var newWeight = weight / Properties.BonusesInTarget;
        //         weight = Math.Max(4, newWeight);
        //     }
        //
        //     if (!Properties.Hunting)
        //     {
        //         if (Properties.EnemyIsNear > 2)
        //             weight *= Properties.EnemyIsNear;
        //
        //         if (Properties.EvilIsNear > 0)
        //             weight *= Properties.EvilIsNear + 2;
        //     }
        //
        //     if (Properties.StoneEater)
        //     {
        //         if (length < 10)
        //             weight /= 3;
        //     }
        //
        //     return weight;
        // }
    }
}