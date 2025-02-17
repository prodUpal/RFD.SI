using NPFGEO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NPFGEO.LWD.Net
{
    public static class SwapSettings
    {
        public const int BroadcastPort = 8001;
        public const int ReceivePort = 8001;
        public const int BufferSize = 8192 * 8;
    }

    public static class SwapHelper
    {
        public static byte[] GetServerMarker()
        {
            return new byte[] { 0xFF, 0xFF, 0xF0, 0xF0 };
        }

        public static byte[] GetPingMarker()
        {
            return new byte[] { 0xAA, 0xAA, 0x55, 0x55 };
        }

        public static byte[] GetPingAnswerMarker()
        {
            return new byte[] { 0xA5, 0xA5, 0x5A, 0x5A };
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }


            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public static byte[] SerializeSettings(Settings settings)
        {
            string str = null;
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractHelper<Settings>.Serialize(settings, ms);
                ms.Flush();
                str = Encoding.ASCII.GetString(ms.ToArray());
                ms.Close();
            }


            byte[] result;

            using (MemoryStream resultStream = new MemoryStream())
            {
                using (DeflateStream compressionStream = new DeflateStream(resultStream,
                         CompressionLevel.Optimal))
                {
                    byte[] inBuffer = Encoding.Unicode.GetBytes(str);
                    compressionStream.Write(inBuffer, 0, inBuffer.Length);
                    resultStream.Seek(0, SeekOrigin.Begin);
                }
                result = resultStream.ToArray();
            }
            return result;
        }

        public static Settings DeserializeSettings(byte[] data)
        {
            string str = null;

            using (MemoryStream resultStream = new MemoryStream(data))
            {
                using (DeflateStream compressionStream = new DeflateStream(resultStream, CompressionMode.Decompress))
                {
                    byte[] outBuffer = new byte[128 * 1024]; 
                    int length = compressionStream.Read(outBuffer, 0, outBuffer.Length);
                    str = Encoding.Unicode.GetString(outBuffer, 0, length);
                }
            }
            Settings settings = null;
            using (MemoryStream ms = new MemoryStream(Encoding.ASCII.GetBytes(str)))
            {
                settings = DataContractHelper<Settings>.Deserialize(ms);
                ms.Close();
            }
            return settings;
        }
    }
}
