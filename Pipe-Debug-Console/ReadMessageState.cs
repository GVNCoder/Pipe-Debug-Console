using System.IO.Pipes;
using System.Text;

// ReSharper disable MemberCanBePrivate.Global

namespace Pipe_Debug_Console
{
    public class ReadMessageState
    {
        public readonly byte[] Buffer;
        public readonly StringBuilder StringBuilder;
        public readonly NamedPipeServerStream Client;

        public ReadMessageState(NamedPipeServerStream client, int bufferSize)
        {
            Buffer = new byte[bufferSize];
            StringBuilder = new StringBuilder();
            Client = client;
        }
    }
}