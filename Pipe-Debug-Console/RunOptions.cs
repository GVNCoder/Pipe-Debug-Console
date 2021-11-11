using CommandLine;

namespace Pipe_Debug_Console
{
    public class RunOptions
    {
        [Option('p'
            , "pipe"
            , Required = true
            , HelpText = "Pipe name to create a server.")]
        public string PipeName { get; set; }

        [Option("bufferSize"
            , Required = false
            , Default = 1024
            , Min = 256
            , Max = 16384 // 16 KBytes
            , HelpText = "The size of the incoming buffer.")]
        public int IncomingBufferSize { get; set; }
    }
}