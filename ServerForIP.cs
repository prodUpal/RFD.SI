using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NPFGEO.LWD.Net
{
    public class ServerForIP
    {
        string _ipAddress;
        object lockObj = new object();
        IConnectResolver connectResolver = new AllConnectResolver();
        Socket listenSocket;
        System.Threading.Thread thread;
        Server _parent;

        public ServerForIP(string ipAddress, Server parent)
        {
            _ipAddress = ipAddress;
            _parent = parent;
        }

        public void Start()
        {
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(_ipAddress), SwapSettings.ReceivePort);
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(ipPoint);
            listenSocket.Listen(10);

            thread = new System.Threading.Thread(DoWork);
            thread.Priority = System.Threading.ThreadPriority.Lowest;
            thread.Start();
        }

        public void Stop()
        {
            listenSocket.Close();
            (listenSocket as IDisposable).Dispose();
            thread.Abort();

            lock (lockObj)
            {
                while (Handlers.Count > 0)
                {
                    Handlers[0].Disconnect();
                }
            }
        }

        void DoWork()
        {
            while (true)
            {
                try
                {
                    Socket socket = listenSocket.Accept();
                    var permission = connectResolver.ResolveConnect(socket);

                    if (permission)
                    {
                        var handler = new ServerHandler(socket);
                        AddHandler(handler);
                        _parent.CallConnected();
                        //Connected?.Invoke(this, EventArgs.Empty);
                    }
                    else
                        DisconnectSocket(socket);
                }
                catch (Exception exc)
                {

                }
            }
        }

        //public event EventHandler<EventArgs> Connected;
        //public event EventHandler<EventArgs> Disconnected;

        void DisconnectSocket(Socket socket)
        {
            try { socket.Shutdown(SocketShutdown.Both); }
            catch { }
            try { socket.Close(); }
            catch { }
            try { (socket as IDisposable).Dispose(); }
            catch { }
        }


        public List<ServerHandler> Handlers = new List<ServerHandler>();

        void AddHandler(ServerHandler handler)
        {
            lock (lockObj)
            {
                Handlers.Add(handler);
            }
            handler.Disconnected += Handler_Disconnected;
        }

        void RemoveHandler(ServerHandler handler)
        {
            lock (lockObj)
            {
                Handlers.Remove(handler);
            }
            handler.Disconnected -= Handler_Disconnected;
        }

        private void Handler_Disconnected(object sender, EventArgs e)
        {
            ServerHandler handler = sender as ServerHandler;
            RemoveHandler(handler);
        }

        public void SendData(DataObject data)
        {
            lock (lockObj)
            {
                try
                {
                    for (int i = 0; i < Handlers.Count; i++)
                        Handlers[i].SendData(data);
                }
                catch (Exception ex) { throw; }
            }
        }

        public void SendSettings(Settings settings)
        {
            lock (lockObj)
            {
                try
                {
                    for (int i = 0; i < Handlers.Count; i++)
                    {
                        Handlers[i].SendSettings(settings);
                    }
                }
                catch (Exception ex) { throw; }
            }
        }

        public bool? CheckIpAdress(IPAddress iPAddress)
        {
            if (listenSocket == null || listenSocket.LocalEndPoint == null)
                return null;
            var currentIpPoint = listenSocket.LocalEndPoint as IPEndPoint;
            if (currentIpPoint.Address.ToString() != iPAddress.ToString())
                return false;
            else
                return true;
        }
    }
}
