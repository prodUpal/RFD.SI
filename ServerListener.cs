using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NPFGEO.LWD.Net
{
    public class ReceiveBroadcastEventArgs : EventArgs
    {
        public IPEndPoint Server { set; get; }
    }

    public class ServerListener
    {
        private BackgroundWorker worker;
        private UdpClient udpClient;
        private int timeOut = 5000;

        public ServerListener()
        {
            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += Worker_DoWork;
        }

        ~ServerListener()
        {
            Stop();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            byte[] recvBuffer = null;
            while (true)
            {
                if (worker.CancellationPending) break;

                var from = new IPEndPoint(0, 0);

                try { recvBuffer = udpClient.Receive(ref from); }
                catch (Exception ex)
                {
                    if (ex is System.Net.Sockets.SocketException)
                    {
                        if (worker.CancellationPending) break;

                        if (udpClient != null)
                        {
                            try { udpClient.Client.Shutdown(SocketShutdown.Both); } catch { }
                            try { udpClient.Client.Close(); } catch { }
                            try { (udpClient as IDisposable).Dispose(); } catch { }
                            udpClient = null;
                        }
                        InitializeUdpClient();
                    }
                }

                if (worker.CancellationPending) break;

                if (recvBuffer != null && Enumerable.SequenceEqual(recvBuffer, SwapHelper.GetServerMarker()))
                    ReceiveBroadcast.Invoke(this, new ReceiveBroadcastEventArgs() { Server = from });
            }
        }

        public void Start()
        {
            InitializeUdpClient();
            worker.RunWorkerAsync();
        }

        void InitializeUdpClient()
        {
            udpClient = new UdpClient();
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Parse(SwapHelper.GetLocalIPAddress()), SwapSettings.BroadcastPort));
            udpClient.Client.ReceiveTimeout = timeOut;
        }

        public void Stop()
        {
            worker.CancelAsync();

            if (udpClient != null)
            {
                try { udpClient.Client.Shutdown(SocketShutdown.Both); } catch { }
                try { udpClient.Client.Close(); } catch { }
                try { (udpClient as IDisposable).Dispose(); } catch { }
                udpClient = null;
            }
        }

        public event EventHandler<ReceiveBroadcastEventArgs> ReceiveBroadcast;
    }
}