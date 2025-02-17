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
using NPFGEO;

namespace NPFGEO.LWD.Net
{
    public class ServerHandler : IDisposable
    {
        public ServerHandler(Socket socket)
        {
            Socket = socket;
            Socket.ReceiveBufferSize = SwapSettings.BufferSize;
        }

        ~ServerHandler()
        {
            Dispose();
        }

        public Socket Socket { private set; get; }

        public void SendData(DataObject data)
        {
            byte[] temp = null;
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractHelper<DataObject>.Serialize(data, ms);
                temp = ms.ToArray();
            }

            try { Socket.Send(temp); }
            catch (Exception ex) { Disconnect(); throw; }
        }
        public bool SendSettings(Settings settings)
        {
            if (Socket == null) return false;

            try { byte[] temp = SwapHelper.SerializeSettings(settings); Socket.Send(temp); }
            catch (Exception ex) { Disconnect(); throw; }
            return true;
        }

        public void Disconnect()
        {
            try { Socket.Shutdown(SocketShutdown.Both); }
            catch { }
            try { Socket.Close(); }
            catch { }
            try { (Socket as IDisposable).Dispose(); }
            catch { }

            Socket = null;

            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (Socket != null && Socket.Connected)
                Disconnect();
        }

        public event EventHandler Disconnected;
    }
}
