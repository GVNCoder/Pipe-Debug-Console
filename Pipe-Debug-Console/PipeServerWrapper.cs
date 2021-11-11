using System;
using System.IO.Pipes;
using System.Text;

using Pipe_Debug_Console.EventArgs;

// ReSharper disable InvertIf
// ReSharper disable MemberCanBePrivate.Global

namespace Pipe_Debug_Console
{
    public class PipeServerWrapper
    {
        private readonly NamedPipeServerStream m_pipeServer;
        private readonly object m_lock = new object();
        private readonly int m_bufferSize;

        private bool m_isStopping;

        #region Ctor

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public PipeServerWrapper(string pipeName, int bufferSize)
        {
            m_pipeServer =
                new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            m_bufferSize = bufferSize;
        }

        #endregion

        #region Puplic interface

        public void Start()
        {
            // set some internal state
            m_isStopping = false;

            try
            {
                m_pipeServer.BeginWaitForConnection(WaitForConnectionCallback, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public void Stop()
        {
            // set some internal state
            m_isStopping = true;

            try
            {
                if (m_pipeServer.IsConnected)
                {
                    m_pipeServer.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
            finally
            {
                m_pipeServer.Close();
                m_pipeServer.Dispose();
            }
        }

        public event EventHandler<ClientConnectedEventArgs> Connected;
        public event EventHandler<ClientDisconnectedEventArgs> Disconnected;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        #endregion

        #region Async Callbacks

        private void WaitForConnectionCallback(IAsyncResult asyncResult)
        {
            if (m_isStopping == false)
            {
                lock (m_lock)
                {
                    if (m_isStopping == false)
                    {
                        // complete the connection async operation
                        m_pipeServer.EndWaitForConnection(asyncResult);

                        OnConnected();
                        BeginReadOperation(new ReadMessageState(m_bufferSize));
                    }
                }
            }
        }

        private void EndReadCallback(IAsyncResult asyncResult)
        {
            var readBytes = m_pipeServer.EndRead(asyncResult);
            if (readBytes > 0)
            {
                var state = (ReadMessageState) asyncResult.AsyncState;
                var messageBuilder = state.StringBuilder;

                // decode received message
                var encodedMessage = Encoding.UTF8.GetString(state.Buffer, 0, readBytes);

                // append message part
                messageBuilder.Append(encodedMessage);

                // message is incomplete, continue read
                if (m_pipeServer.IsMessageComplete)
                {
                    // finalize message
                    var message = messageBuilder
                        .ToString()
                        .TrimEnd('\0');

                    OnMessageReceived(message);

                    // Begin a new reading operation
                    BeginReadOperation(new ReadMessageState(m_bufferSize));
                }
                else // message is incomplete, continue read
                {
                    BeginReadOperation(state);
                }
            }
            else // When no bytes were read, it can mean that the client have been disconnected
            {
                if (m_isStopping == false)
                {
                    lock (m_lock)
                    {
                        if (m_isStopping == false)
                        {
                            OnDisconnected();
                            Stop();
                        }
                    }
                }
            }
        }

        #endregion

        #region Private helpers

        private void OnConnected()
        {
            Connected?.Invoke(this, new ClientConnectedEventArgs());
        }

        private void OnDisconnected()
        {
            Disconnected?.Invoke(this, new ClientDisconnectedEventArgs());
        }

        private void OnMessageReceived(string message)
        {
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
        }

        private void BeginReadOperation(ReadMessageState state)
        {
            try
            {
                m_pipeServer.BeginRead(state.Buffer, 0, m_bufferSize, EndReadCallback, state);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        #endregion
    }
}