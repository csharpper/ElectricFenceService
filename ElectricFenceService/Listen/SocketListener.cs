using SocketHelper;
using SocketHelper.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ElectricFenceService.Listen
{
    public class SocketListener : IDisposable
    {
        ManualResetEvent _disposeEvent = new ManualResetEvent(false);
        public SocketAdapter Adapter { get; private set; }
        public string ConnectInfom { get; set; } = "";
        public SocketListener(SocketAdapter adapter, string connectInfom)
        {
            _disposeEvent.Reset();
            Init(adapter, connectInfom);
        }

        public void Init(SocketAdapter adapter, string connectInfom)
        {
            Adapter = adapter;
            ConnectInfom = connectInfom;
            Common.Log.Logger.Default.Trace($"{adapter.RemoteEndPoint} 上线.{ConnectInfom}");
            Adapter.ErrorOccured += onErrorOccured;
            Adapter.Closed += onClosed;
        }

        private void onClosed(object sender, EventArgs e)
        {
            SocketAdapter adapter = sender as SocketAdapter;
            //Logger.Default.Debug("Adapter has closed!_" + adapter.RemoteEndPoint);
            Common.Log.Logger.Default.Trace($"{adapter.RemoteEndPoint} 离线.{ConnectInfom}");
            Dispose();
        }

        private void onErrorOccured(object sender, SocketHelper.Events.ErrorEventArgs args)
        {
            SocketAdapter adapter = sender as SocketAdapter;
            Common.Log.Logger.Default.Trace($"Adapter ErrorOccured:{ConnectInfom} {args.ErrorMessage} __{adapter.RemoteEndPoint}");
            adapter.Close();
        }

        public void Send(byte[] bytes)
        {
            if (Adapter != null && Adapter.IsConnected)
                Adapter.SendOnly(bytes);
        }

        public void Dispose()
        {
            _disposeEvent.Set();
            if (Adapter != null)
            {
                Adapter.ErrorOccured -= onErrorOccured;
                Adapter.Closed -= onClosed;
            }
            Adapter = null;
        }

    }
}
