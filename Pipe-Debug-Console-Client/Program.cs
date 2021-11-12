using System;
using System.Threading.Tasks;

namespace ClientTest
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var client = new PipeClientWrapper("test");

            client.Start();

            while (true)
            {
                var userInput = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(userInput))
                {
                    break;
                }

                client.SendMessageAndForget(userInput);
            }
        }
    }
}
