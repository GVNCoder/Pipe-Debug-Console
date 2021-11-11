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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
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

        public Task<TaskResult> SendMessageAsync(string message)
        {
            var taskCompletionSource = new TaskCompletionSource<TaskResult>();

            if (m_pipeClient.IsConnected)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                m_pipeClient.BeginWrite(buffer, 0, buffer.Length, asyncResult =>
                {
                    try
                    {
                        taskCompletionSource.SetResult(EndWriteCallback(asyncResult));
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.SetException(ex);
                    }

                }, null);
            }
            else
            {
                Console.WriteLine("Cannot send message, pipe is not connected.");
                throw new IOException("Pipe is not connected.");
            }

            return taskCompletionSource.Task;
        }

        #endregion

        #region Async Callbacks

        private TaskResult EndWriteCallback(IAsyncResult asyncResult)
        {
            m_pipeClient.EndWrite(asyncResult);
            m_pipeClient.Flush();

            return new TaskResult { IsSuccess = true };
        }

        #endregion
    }
}