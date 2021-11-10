namespace Pipe_Debug_Console.EventArgs
{
    public class MessageReceivedEventArgs : System.EventArgs
    {
        public string Message { get; }

        public MessageReceivedEventArgs(string message)
        {
            Message = message;
        }
    }
}