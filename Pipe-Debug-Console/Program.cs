using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;

namespace Pipe_Debug_Console
{
    public static class Program
    {
        // constants
        private const int MaxArgumentsCount = 1;
        private const int MinArgumentsCount = 1;
        private const int BufferSize = 1024; // bytes

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public static void Main(string[] args)
        {
            // parse incoming arguments
            if (args.Length < MinArgumentsCount || args.Length > MaxArgumentsCount)
            {
                Console.WriteLine("Error!");
                return;
            }

            var pipeServerName = args.Single();
            var pipeServer = new NamedPipeServerStream(pipeServerName, PipeDirection.In, 1, PipeTransmissionMode.Message);

            Console.WriteLine(pipeServerName);

            // wait for a client to connect
            Console.Write("Waiting for client connection...");
            try
            {
                pipeServer.WaitForConnection();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

            // begin read incoming messages
            Console.WriteLine("Client connected.");
            var readState = new ReadMessageState(pipeServer, BufferSize);

            pipeServer.BeginRead(readState.Buffer, 0, BufferSize, EndReadCallback, readState);
            
            Console.ReadKey();
        }

        #region Pipe Server Async Callbacks

        private static void EndReadCallback(IAsyncResult result)
        {
            var state = (ReadMessageState) result.AsyncState;
            var client = state.Client;
            var readBytes = client.EndRead(result);

            if (readBytes > 0)
            {
                // get the read bytes and append them
                state.StringBuilder.Append(Encoding.UTF8.GetString(state.Buffer, 0, readBytes));

                if (client.IsMessageComplete == false) // message is not complete, continue reading
                {
                    client.BeginRead(state.Buffer, 0, BufferSize, EndReadCallback, state);
                }
                else // Message is completed
                {
                    // Finalize the received string and fire MessageReceivedEvent
                    var message = state.StringBuilder.ToString().TrimEnd('\0');

                    OnMessageReceived(message);

                    // begin a new reading operation
                    var readState = new ReadMessageState(client, BufferSize);
                    client.BeginRead(readState.Buffer, 0, BufferSize, EndReadCallback, readState);
                }
            }
            else // when no bytes were read, it can mean that the client have been disconnected
            {
                try
                {
                    if (client.IsConnected)
                    {
                        client.Disconnect();
                    }
                }
                catch (Exception exception)
                {
                    // ignore
                }
                finally
                {
                    client.Close();
                    client.Dispose();
                }
            }
        }

        #endregion

        #region Events

        private static void OnMessageReceived(string message)
        {
            var originalColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        #endregion
    }
}
