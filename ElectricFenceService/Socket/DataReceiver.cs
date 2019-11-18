using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ElectricFenceService.Socket
{
    public abstract class DataReceiver : IDisposable
    {
        public Action<ShipInfo> DynamicEvent { get; set; }
        string _host;
        int _port;
        TcpClient _client;
        public Action<byte[]> ReceivedEvent { get; set; }
        ManualResetEvent _disposeEvent = new ManualResetEvent(false);
        public DataReceiver(string host, int port)
        {
            _host = host;
            _port = port;
            _client = new TcpClient();
            _client.ReceiveTimeout = 15000;
            _client.SendTimeout = 15000;
            _disposeEvent.Reset();
            new Thread(run) { IsBackground = true }.Start();
        }

        private void run()
        {
            do
            {
                try
                {
                    close();
                    Thread.Sleep(1000);
                    _client.Connect(_host, _port);
                }
                catch (Exception ex)
                {
                    Common.Log.Logger.Default.Trace("Client Connected Error." + ex);
                    continue;
                }
                try
                {
                    byte[] buffer = new byte[2 * 1024];
                    while (true)
                    {
                        int length = _client.Client.Receive(buffer);
                        if (length > 0)
                        {
                            byte[] buf = new byte[length];
                            Array.Copy(buffer, buf, length);
                            received(buf);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.Log.Logger.Default.Trace("Receive Data Error." + ex);
                }
            }
            while (!_disposeEvent.WaitOne(15000));
        }

        protected abstract void received(byte[] buffer);

        protected void onDynamic(ShipInfo target)
        {
            DynamicEvent?.Invoke(target);
        }

        void close()
        {
            try
            {
                _client.Close();
            }
            catch { }
        }

        public void Dispose()
        {
            _disposeEvent.Set();
            close();
        }
    }
}
