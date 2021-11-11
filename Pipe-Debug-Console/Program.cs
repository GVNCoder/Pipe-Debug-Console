using System;
using System.IO;
using System.Linq;
using System.Threading;

using CommandLine;

namespace Pipe_Debug_Console
{
    public static class Program
    {
        private const char MessageContextLabel = '^';
        private const int MessageMinLength = 3; // message context label char + context char + message content chars 1+
        private const int MessageContextLabelIndex = 0;
        private const int MessageContextIndex = 1;

        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<RunOptions>(args)
                .WithParsed(options =>
                {
                    if (Directory.GetFiles("\\\\.\\pipe\\", $"{options.PipeName}*").Length != 0)
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
                // message template
                // ^i[Message content]

                var rawMessage = args.Message;
                if (rawMessage.Length < MessageMinLength)
                {
                    return;
                }

                var contextLabel = rawMessage[MessageContextLabelIndex];
                var context = rawMessage[MessageContextIndex];
                var availableContexts = new[] { 'i', 'd', 'w', 'e' };

                if (contextLabel != MessageContextLabel || availableContexts.Contains(context) == false)
                {
                    Console.WriteLine(rawMessage);
                }
                else
                {
                    var message = rawMessage.Substring(MessageMinLength - 1);
                    var originalForeground = Console.ForegroundColor;

                    switch (context)
                    {
                        case 'i': // info
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.WriteLine($"[INF] {message}");
                            Console.ForegroundColor = originalForeground;
                            break;
                        case 'd': // debug
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine($"[DBG] {message}");
                            Console.ForegroundColor = originalForeground;
                            break;
                        case 'w': // warning
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"[WRN] {message}");
                            Console.ForegroundColor = originalForeground;
                            break;
                        case 'e': // error
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[ERR] {message}");
                            Console.ForegroundColor = originalForeground;
                            break;
                        default:
                            Console.WriteLine($"[UNK] {message}");
                            break;
                    }
                }
            };

            Console.Write("Waiting for client connection...");

            // fire server
            pipeServer.Start();
            manualEvent.WaitOne();
        }
    }
}
