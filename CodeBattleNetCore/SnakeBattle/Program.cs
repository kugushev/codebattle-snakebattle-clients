using SnakeBattle.Api;
using System;

namespace Client
{
    class Program
    {
        const string SERVER_ADDRESS = "http://codebattle-pro-2020s1.westeurope.cloudapp.azure.com/codenjoy-contest/board/player/srzojelzkw0c4gnpnw7y?code=3609046216030092067&gameName=snakebattle";

        static void Main(string[] args)
        {
            var client = new SnakeBattleClient(SERVER_ADDRESS);
            client.Run(DoRun);

            Console.ReadKey();
            client.InitiateExit();
        }

        private static SnakeAction DoRun(GameBoard gameBoard)
        {
            try
            {
                return new Brain().DoRun(gameBoard);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return RandomAi();
            }
            
        }

        private static SnakeAction RandomAi()
        {
            var random = new Random();
            var direction = (Direction) random.Next(Enum.GetValues(typeof(Direction)).Length);
            var act = random.Next() % 2 == 0;
            return new SnakeAction(act, direction);
        }
    }
}