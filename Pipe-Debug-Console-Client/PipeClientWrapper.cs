using System;
using System.IO.Pipes;
using System.Text;

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

        public void SendMessageAndForget(string message)
        {
            if (m_pipeClient.IsConnected == false)
            {
                return;
            }

            var messageBuffer = Encoding.UTF8.GetBytes(message);
            try
            {
                m_pipeClient.BeginWrite(messageBuffer, 0, messageBuffer.Length, EndWriteCallback, null);
            }
            catch (Exception ex)
            {
                // ignore
            }
        }

        #endregion

        #region Async Callbacks

        private void EndWriteCallback(IAsyncResult asyncResult)
        {
            try
            {
                m_pipeClient.EndWrite(asyncResult);
                m_pipeClient.Flush();
            }
            catch (Exception ex)
            {
                // ignore
            }
        }

        #endregion
    }
}