using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace ClientTest
{
    public class PipeClientWrapper
    {
        private readonly NamedPipeClientStream m_pipeClient;

        #region Ctor

        public PipeClientWrapper(string pipeName)
        {
            m_pipeClient =
                new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
        }

        #endregion

        #region Public interface

        public void Start()
        {
            m_pipeClient.Connect();
        }

        public void Stop()
        {
            try
            {
                m_pipeClient.WaitForPipeDrain();
            }
            finally
            {
                m_pipeClient.Close();
                m_pipeClient.Dispose();
            }
        }

        public void SendMessageAsync(string message)
        {
            if (m_pipeClient.IsConnected)
            {
                var messageBuffer = Encoding.UTF8.GetBytes(message);

                m_pipeClient.BeginWrite(messageBuffer, 0, messageBuffer.Length, EndWriteCallback, null);
            }
            else
            {
                Console.WriteLine("Cannot send message, pipe is not connected.");
                throw new IOException("Pipe is not connected.");
            }
        }

        #endregion

        #region Async Callbacks

        private void EndWriteCallback(IAsyncResult asyncResult)
        {
            m_pipeClient.EndWrite(asyncResult);
            m_pipeClient.Flush();
        }

        #endregion
    }
}