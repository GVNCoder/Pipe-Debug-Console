using System;
using System.IO.Pipes;
using System.Text;

namespace ClientTest
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var pipeServerName = "testPipe";
            var pipeClient =
                new NamedPipeClientStream(".", pipeServerName, PipeDirection.Out);

            pipeClient.Connect();

            var buffer = Encoding.UTF8.GetBytes("Bou bou bou");
            pipeClient.BeginWrite(buffer, 0, buffer.Length, asyncResult =>
            {
                pipeClient.EndWrite(asyncResult);
                pipeClient.Flush();

            }, null);

            pipeClient.WaitForPipeDrain();

            Console.ReadKey();
        }
    }
}
