using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NPFGEO.LWD.Net;

namespace NPFGEO.LWD.Net
{
    public class ReceiveDataEventArgs : EventArgs
    {
        public DataObject Data { set; get; }
    }
    public class ReceiveSettingsEventArgs : EventArgs
    {
        public Settings Settings { set; get; }
    }

    public interface IConnectResolver
    {
        bool ResolveConnect(Socket socket);
    }

    public class AllConnectResolver : IConnectResolver
    {
        public bool ResolveConnect(Socket socket)
        {
            return true;
        }
    }

    public class Server
    {
        //object lockObj = new object();
        //IConnectResolver connectResolver = new AllConnectResolver();

        public Server()
        {
        }

        //Socket listenSocket;
        //System.Threading.Thread thread;
        public List<ServerForIP> Items = new List<ServerForIP>();
        private List<NetworkInterface> _networkInterfaces;
        private int _count = 0;

        public void Start()
        {
            //IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(SwapHelper.GetLocalIPAddress()), SwapSettings.ReceivePort);
            //listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //listenSocket.Bind(ipPoint);
            //listenSocket.Listen(10);

            //thread = new System.Threading.Thread(DoWork);
            //thread.Priority = System.Threading.ThreadPriority.Lowest;
            //thread.Start();

            _networkInterfaces = UpdateNetworkInterfaces();
            _count = 0;

            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                try
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var item = new ServerForIP(ip.ToString(), this);
                        item.Start();
                        Items.Add(item);
                    }
                }
                catch (Exception exc) { }
            }
        }

        public void Stop()
        {
            foreach (var item in Items.ToArray())
            {
                item.Stop();
                Items.Remove(item);
            }
            //lock (lockObj)
            //{
            //    while (handlers.Count > 0)
            //    {
            //        handlers[0].Disconnect();
            //    }
            //}

            //listenSocket.Close();
            //(listenSocket as IDisposable).Dispose();
            //thread.Abort();
        }

        //void DoWork()
        //{
        //    while (true)
        //    {
        //        Socket socket = listenSocket.Accept();
        //        var permission = connectResolver.ResolveConnect(socket);

        //        if (permission)
        //        {
        //            var handler = new ServerHandler(socket);
        //            AddHandler(handler);
        //            Connected?.Invoke(this, EventArgs.Empty);
        //        }
        //        else
        //            DisconnectSocket(socket);
        //    }
        //}

        public event EventHandler<EventArgs> Connected;
        public event EventHandler<EventArgs> Disconnected;

        internal void CallConnected()
        {
            Connected?.Invoke(this, EventArgs.Empty);
        }

        //void DisconnectSocket(Socket socket)
        //{
        //    try { socket.Shutdown(SocketShutdown.Both); }
        //    catch { }
        //    try { socket.Close(); }
        //    catch { }
        //    try { (socket as IDisposable).Dispose(); }
        //    catch { }
        //}


        List<ServerHandler> handlers = new List<ServerHandler>();

        //void AddHandler(ServerHandler handler)
        //{
        //    lock (lockObj)
        //    {
        //        handlers.Add(handler);
        //    }
        //    handler.Disconnected += Handler_Disconnected;
        //}

        //void RemoveHandler(ServerHandler handler)
        //{
        //    lock (lockObj)
        //    {
        //        handlers.Remove(handler);
        //    }
        //    handler.Disconnected -= Handler_Disconnected;
        //}

        //private void Handler_Disconnected(object sender, EventArgs e)
        //{
        //    ServerHandler handler = sender as ServerHandler;
        //    RemoveHandler(handler);
        //}

        public void SendBroadcast()
        {
            if (_count > 60)
            {
                _networkInterfaces = UpdateNetworkInterfaces();
                _count = 0;
            }

            foreach (var ni in _networkInterfaces)
            {
                foreach (UnicastIPAddressInformation uip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (uip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        IPEndPoint local = new IPEndPoint(uip.Address.Address, 0);
                        UdpClient udpc = new UdpClient(local);
                        udpc.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                        udpc.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontRoute, 1);
                        byte[] data = SwapHelper.GetServerMarker();
                        IPEndPoint ip = new IPEndPoint(IPAddress.Parse("255.255.255.255"), SwapSettings.BroadcastPort);
                        udpc.Send(data, data.Length, ip);
                    }
                }
            }

            _count++;
        }

        private List<NetworkInterface> UpdateNetworkInterfaces()
        {
            List<NetworkInterface> @return = new List<NetworkInterface>();
            var allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var ni in allNetworkInterfaces)
            {
                if (ni.OperationalStatus == OperationalStatus.Up && ni.SupportsMulticast && ni.GetIPProperties().GetIPv4Properties() != null)
                {
                    int id = ni.GetIPProperties().GetIPv4Properties().Index;
                    if (NetworkInterface.LoopbackInterfaceIndex != id)
                    {
                        @return.Add(ni);
                    }
                }
            }
            return @return;
        }

        public bool IsLastDataSent { private set; get; } = true;

        public void SendData(DataObject data)
        {
            try
            {
                IsLastDataSent = false;
                foreach (var item in Items)
                    item.SendData(data);
            }
            catch (Exception ex) { throw; }
            finally { IsLastDataSent = true; }

            //lock (lockObj)
            //{
            //    try
            //    {
            //        foreach (var handler in handlers)
            //            handler.SendData(data);
            //    }
            //    catch { }
            //}
        }

        public void SendSettings(Settings settings)
        {
            try
            {
                foreach (var item in Items)
                    item.SendSettings(settings);
            }
            catch (Exception ex) { throw; }
            //lock (lockObj)
            //{
            //    try
            //    {
            //        foreach (var handler in handlers)
            //        {
            //            handler.SendSettings(settings);
            //        }
            //    }
            //    catch { }
            //}
        }

        public bool? CheckIpAdress()
        {
            var result = true;

            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                try
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        bool checkResult = false;
                        foreach (var item in Items)
                        {
                            var r = item.CheckIpAdress(ip);
                            if (r.HasValue && r.Value)
                            {
                                checkResult = true;
                                break;
                            }
                        }
                        if (!checkResult)
                            result = false;
                    }
                }
                catch (Exception exc) { }
            }



            //foreach (var item in items)
            //{
            //    var r = item.CheckIpAdress();
            //    if (r.HasValue && !r.Value)
            //        result = false;
            //}
            return result;

            //if (listenSocket == null || listenSocket.LocalEndPoint == null)
            //    return null;

            //IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(SwapHelper.GetLocalIPAddress()), SwapSettings.ReceivePort);
            //var currentIpPoint = listenSocket.LocalEndPoint as IPEndPoint;
            //if (currentIpPoint.Address.ToString() != ipPoint.Address.ToString())
            //    return false;
            //else
            //    return true;
        }
    }
}
