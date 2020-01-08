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
    class SocketsListener
    {
        int _port = -1;
        SocketHelper.SocketServer _server;
        List<SocketListener> _sockets = new List<SocketListener>();
        DateTime updatedTime = DateTime.Now;

        public void Start(int port)
        {
            _port = port;
            startListen();
        }

        private void startListen()
        {
            Stop();
            _server = new SocketServer();
            _server.Listen(_port);
            _server.ClientAccepted += Server_ClientAccepted;
            _server.ErrorOccured += Server_ErrorOccured;
        }

        public void Stop()
        {
            if (_server != null)
            {
                _server.ClientAccepted -= Server_ClientAccepted;
                _server.ErrorOccured -= Server_ErrorOccured;
                _server.Dispose();
            }
            _server = null;
        }

        public void Send(string message)
        {
            Send(Encoding.UTF8.GetBytes(message));
            Thread.Sleep(10);//发送间隔不低于10ms
        }

        public void Send(byte[] buffer)
        {
            DateTime start = DateTime.Now;
            List<SocketListener> invalids = new List<SocketListener>();
            foreach (var socket in _sockets)
            {
                try
                {
                    socket.Send(buffer);
                }
                catch (Exception ex)
                {
                    Common.Log.Logger.Default.Error($"发送数据错误.Local {socket.Adapter.LocalEndPoint} - Remote {socket.Adapter.RemoteEndPoint} - " + ex);
                    invalids.Add(socket);
                }
            }
            if (invalids.Count > 0)
                foreach (var inv in invalids)
                    _sockets.Remove(inv);
            updatedTime = DateTime.Now;
            if (start.AddSeconds(1) < DateTime.Now)
                Common.Log.Logger.Default.Error($"Warning: 发送占用时间过长。{DateTime.Now - start}");
        }

        private void Server_ErrorOccured(object sender, SocketHelper.Events.ErrorEventArgs args)
        {
            if (args.ErrorType == ErrorTypes.SocketAccept)
            {
                Common.Log.Logger.Default.Error("Server Accept Error, Try Start Listen Again.");
                Thread.Sleep(5000);
                if (_port > 0)
                    startListen();
            }
        }

        private void Server_ClientAccepted(object sender, ClientAcceptedEventArgs args)
        {
            string endpoint = args.Adapter.RemoteEndPoint.ToString();
            Common.Log.Logger.Default.Trace("ClientAccepted:" + endpoint);
            SocketAdapter adapter = args.Adapter;
            lock (_sockets)
            {
                _sockets.RemoveAll(_ => _.Adapter == null);
                var socket = new SocketListener(adapter, $"监听端口：{_port}.");
                _sockets.Add(socket);
            }
        }
    }
}
