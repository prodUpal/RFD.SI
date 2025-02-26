using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;

using NPFGEO;

namespace NPFGEO.LWD.Net
{
    public class Client : IDisposable
    {
        public IPAddress Address { set; get; }
        private Socket socket;
        System.Threading.Thread thread;
        private int timeOut = 7500;

        public Client()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.ReceiveTimeout = timeOut;
            socket.SendBufferSize = SwapSettings.BufferSize;
            
            thread = new System.Threading.Thread(Handler);
            thread.Priority = System.Threading.ThreadPriority.Lowest;
            thread.Start();
        }

        public bool Connect()
        {
            IPEndPoint ipPoint = new IPEndPoint(Address, SwapSettings.ReceivePort);
            if (socket == null || !socket.Connected)
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.ReceiveTimeout = timeOut;
                socket.SendBufferSize = SwapSettings.BufferSize;
            }
            socket.Connect(ipPoint);

            return true;
        }
        public void Disconnect()
        {
            try { socket?.Shutdown(SocketShutdown.Both); }
            catch { }
            try { socket?.Close(); }
            catch { }
            try { (socket as IDisposable)?.Dispose(); }
            catch { }

            socket = null;

            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (thread.IsAlive)
                thread.Abort();
        }

        public bool Connected { get { return socket != null && socket.Connected; } }

        public event EventHandler Disconnected;

        public event EventHandler Pinged;
        public event EventHandler<ReceiveDataEventArgs> ReceiveData;
        public event EventHandler<ReceiveSettingsEventArgs> ReceiveSettings;
        public event EventHandler ConnectedStatusChanged;

        void Handler()
        {
            byte[] data = new byte[SwapSettings.BufferSize];
            int bytes = 0;
            int zeroBytesCount = 0;
            bool currStatus;
            bool prevStatus = Connected;
            do
            {
                currStatus = Connected;
                if (currStatus)
                {
                    try { bytes = socket.Receive(data); }
                    catch (Exception exc) { Disconnect(); }

                    if (bytes == 0)
                    {
                        zeroBytesCount++;
                        if (zeroBytesCount > 50000)
                            Disconnect();
                        else
                            continue;
                    }

                    if (bytes == 4)
                    {
                        zeroBytesCount = 0;
                        Pinged?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        zeroBytesCount = 0;
                        byte[] buffer = new byte[bytes];
                        Buffer.BlockCopy(data, 0, buffer, 0, bytes);

                        using (MemoryStream ms = new MemoryStream(buffer))
                        {
                            
                            try
                            {
                                // Попытка прочитать как ZIP-архив
                                ZipInputStream zipStream = new ZipInputStream(ms);
            
                                ZipEntry entry;
                                while ((entry = zipStream.GetNextEntry()) != null)
                                {
                                    byte[] decompressedData = new byte[entry.Size];
                                    zipStream.Read(decompressedData, 0, decompressedData.Length);
                
                                    // Теперь можно десериализовать распакованное содержимое
                                    ms.Position = 0;
                                    DataObject dataobj = DataContractHelper<DataObject>.Deserialize(ms);
                                    Console.WriteLine("DATA ZIP TEST OUTPUT " + dataobj.ToString());
                                    ReceiveData?.Invoke(this, new ReceiveDataEventArgs() { Data = dataobj });
                                    
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Ошибка при обработке ZIP-архива: " + ex.Message);
                                // Если это не ZIP-архив, пробуем стандартную десериализацию
                                
                            }
                            
                            try
                            {
                                Settings settings = SwapHelper.DeserializeSettings(buffer);
                                Console.WriteLine("SETTINGS TEST OUTPUT " + settings); 
                                ReceiveSettings?.Invoke(this, new ReceiveSettingsEventArgs() { Settings = settings });
                            }
                            catch (Exception exc) {
                                
                                Console.WriteLine(exc.Message +  " SETTINGS TEST OUTPUT EXCEPTION");
                            }

                            try
                            {
                                ms.Position = 0;
                                DataObject dataobj = DataContractHelper<DataObject>.Deserialize(ms);
                                Console.WriteLine("DATA TEST OUTPUT" + dataobj.ToString());
                                ReceiveData?.Invoke(this, new ReceiveDataEventArgs() { Data = dataobj });
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message +  " DATA TEST OUTPUT EXCEPTION");
                            }
                        }
                    }
                }
                if (prevStatus != currStatus)
                {
                    prevStatus = currStatus;
                    ConnectedStatusChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            while (true);
        }
    }


    public class ParameterInfo
    {
        public string Name { set; get; }
        public string Units { set; get; }
        public int Float { set; get; }
    }

    [KnownType(typeof(StatusInfo))]
    public class FlagInfo
    {
        public string Name { set; get; }
    }

    public class StatusItem
    {
        public int Value { set; get; }
        //public Color Color { set; get; }
    }

    public class TimeInfo
    {
        public string Name { get; set; }
        public string Alias { get; set; }
    }

    public class StatusInfo : FlagInfo
    {
        public IEnumerable<StatusItem> Palette { set; get; }
    }

    public class Target
    {
        public byte Capacity { set; get; }
        public int GridFrequency { set; get; }
        public bool IsHalfMode { set; get; }
        public double DefaultRadius { set; get; }
        public double ReductionFactor { set; get; }
        public double SectorDirection { set; get; }
        public double SectorWidth { set; get; }
        public int FontSize { get; set; }
        public double RingWidth { get; set; }
        public bool FromCenterToBorder { get; set; }

    }

    public class Settings
    {
        public IEnumerable<FlagInfo> Flags { set; get; }
        public IEnumerable<StatusInfo> Statuses { get; set; }
        public IEnumerable<ParameterInfo> Parameters { set; get; }
        
        //public IEnumerable<Parameter> Params{ set; get; }
        
        public IEnumerable<TimeInfo> DateTimeParameters { get; set; }
        public IEnumerable<TargetPoint> TargetPoints { get; set; }
        public InfoParameters InfoParameters { get; set; }
        public Target Target { set; get; }
        public string ThemeStyle { set; get; }

    }

    public class Parameter
    {
        public string Name { set; get; }
        public double Value { set; get; }
    }

    public class Flag
    {
        public string Name { set; get; }
        public bool Value { set; get; }
    }

    public class Status
    {
        public string Name { set; get; }
        public int Value { set; get; }
    }

    public class DateTime
    {
        public string Name { get; set; }
        public TimeSpan Value { get; set; }
    }

    public class TargetPoint
    {
        public double Order { set; get; }
        public double Angle { set; get; }
        public ToolfaceType ToolfaceType { get; set; }
        public System.DateTime TimeStamp { get; set; }
        public double Value { get; set; }
        public bool IsUsed { set; get; }
    }

    public class InfoParameters
    {
        public double ToolfaceOffset { get; set; }
        public double MagneticDeclination { get; set; }
        public bool TimeStamp { get; set; }
    }

    public enum ToolfaceType { Gravity, Magnetic, Automatic }
    public class DataObject
    {
        public IEnumerable<Parameter> Parameters { set; get; }
        public IEnumerable<Flag> Flags { set; get; }
        public IEnumerable<Status> Statuses { set; get; }
        public IEnumerable<DateTime> Times { get; set; }
        public IEnumerable<TargetPoint> TargetPoints { set; get; }
        public TimeSpan TimeStamp { get; set; }

        public static DataObject Union(DataObject obj1, DataObject obj2)
        {
            DataObject union = new DataObject();
            union.Parameters = Union(obj1.Parameters, obj2.Parameters);
            union.Flags = Union(obj1.Flags, obj2.Flags);
            union.Statuses = Union(obj1.Statuses, obj2.Statuses);
            union.Times = Union(obj1.Times, obj1.Times);
            union.TargetPoints = Union(obj1.TargetPoints, obj2.TargetPoints);
            union.TimeStamp = obj2.TimeStamp;
            return union;
        }

        static IEnumerable<Parameter> Union(IEnumerable<Parameter> p1, IEnumerable<Parameter> p2)
        {
            if (p1 == null) return p2;
            if (p2 == null) return p1;

            var union = p2.Union(p1, new ParameterEqualityComparer()).ToArray();
            return union;
        }

        static IEnumerable<Flag> Union(IEnumerable<Flag> p1, IEnumerable<Flag> p2)
        {
            if (p1 == null) return p2;
            if (p2 == null) return p1;

            var union = p2.Union(p1, new FlagEqualityComparer()).ToArray();
            return union;
        }

        static IEnumerable<Status> Union(IEnumerable<Status> p1, IEnumerable<Status> p2)
        {
            if (p1 == null) return p2;
            if (p2 == null) return p1;

            var union = p2.Union(p1, new StatusEqualityComparer()).ToArray();
            return union;
        }
        static IEnumerable<DateTime> Union(IEnumerable<DateTime> p1, IEnumerable<DateTime> p2)
        {
            if (p1 == null) return p2;
            if (p2 == null) return p1;

            var union = p2.Union(p1, new DateTimeEqualityComparer()).ToArray();
            return union;
        }

        static IEnumerable<TargetPoint> Union(IEnumerable<TargetPoint> p1, IEnumerable<TargetPoint> p2)
        {
            if (p2 == null) return p1;
            return p2;
        }

        static InfoParameters Union(InfoParameters p1, InfoParameters p2)
        {
            if (p2 == null) return p1;
            return p2;
        }
    }

    public class ParameterEqualityComparer : IEqualityComparer<Parameter>
    {
        public bool Equals(Parameter x, Parameter y)
        {
            return x.Name.Equals(y.Name);
        }

        public int GetHashCode(Parameter obj)
        {
            return obj.Name.GetHashCode();
        }
    }
    public class FlagEqualityComparer : IEqualityComparer<Flag>
    {
        public bool Equals(Flag x, Flag y)
        {
            return x.Name.Equals(y.Name);
        }

        public int GetHashCode(Flag obj)
        {
            return obj.Name.GetHashCode();
        }
    }
    public class StatusEqualityComparer : IEqualityComparer<Status>
    {
        public bool Equals(Status x, Status y)
        {
            return x.Name.Equals(y.Name);
        }

        public int GetHashCode(Status obj)
        {
            return obj.Name.GetHashCode();
        }
    }

    public class DateTimeEqualityComparer : IEqualityComparer<DateTime>
    {
        public bool Equals(DateTime x, DateTime y)
        {
            return x.Name.Equals(y.Name);
        }

        public int GetHashCode(DateTime obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
