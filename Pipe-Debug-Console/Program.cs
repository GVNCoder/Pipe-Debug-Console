using System;
using System.IO;
using System.Threading;

using CommandLine;

namespace Pipe_Debug_Console
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<RunOptions>(args)
                .WithParsed(options =>
                {
                    if (Directory.GetFiles("\\\\.\\pipe\\", $"{options.PipeName}*").Length > 1)
                    {
                        Console.WriteLine($"Such a named pipe ({options.PipeName}) already exists.");
                    }
                    else
                    {
                        RunDebugConsole(options);
                    }
                });
        }

        private static void RunDebugConsole(RunOptions options)
        {
            // set console
            Console.Title = $"DEBUG: {options.PipeName}";

            // create a receiving server
            var pipeServer = new PipeServerWrapper(options.PipeName, options.IncomingBufferSize);
            var manualEvent = new ManualResetEvent(false);

            // track server events
            pipeServer.Connected += (sender, args) =>
            {
                Console.WriteLine("Connected.");
            };

            pipeServer.Disconnected += (sender, args) =>
            {
                Console.WriteLine("Disconnected.");

                // pass execution
                manualEvent.Set();
            };

            pipeServer.MessageReceived += (sender, args) =>
            {
                // TODO: Process messages
            };

            Console.Write("Waiting for client connection...");

            // fire server
            pipeServer.Start();
            manualEvent.WaitOne();
        }
    }
}
