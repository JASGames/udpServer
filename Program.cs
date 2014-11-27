using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Polenter.Serialization;
using UdpKit;

namespace udpServer
{
    class Program
    {
        private static UdpSocket _socket;
        private static readonly List<UdpConnection> Clients = new List<UdpConnection>();
        
        static void Main(string[] args)
        {
            _socket = UdpSocket.Create<UdpPlatformManaged, ServerSerializer>();
            _socket.Start(new UdpEndPoint(UdpIPv4Address.Any, 14000));
            
            Console.WriteLine("Status: "+_socket.State);

            UdpEvent ev;

            while (true)
            {
                while (_socket.Poll(out ev))
                {
                    switch (ev.EventType)
                    {
                        case UdpEventType.Connected:
                            Console.WriteLine("Client connect from {0}", ev.Connection.RemoteEndPoint);
                            Clients.Add(ev.Connection);
                            ev.Connection.Send("YourPlayer:"+Clients.Count+";");
                            break;
                        case UdpEventType.Disconnected:
                            Clients.Remove(ev.Connection);
                            Console.WriteLine("Client disconnected from {0}", ev.Connection.RemoteEndPoint);
                            break;
                            //When we receive, just forward to all clients
                        case UdpEventType.ObjectReceived:
                            SendToOtherPlayers(ev.Object as string, ev.Connection);
                            break;
                        case UdpEventType.ObjectLost:
                            //ev.Connection.Send(ev.Object);
                            break;
                    }
                }
                
                Thread.Sleep(16);
            }
        }

        private static void SendToOtherPlayers(string message, UdpConnection connection)
        {
            foreach (var client in Clients)
            {
                if (client != connection)
                {
                    client.Send(message);
                }
            }
        }
    }

    internal class ServerSerializer : UdpSerializer
    {
        public override bool Pack(UdpStream stream, ref object o)
        {
            // cast to string and get bytes
            Packet p = (Packet)o;
            var s = new MemoryStream();

            var serializer = new SharpSerializer(true);
            serializer.Serialize(p, s);

            byte[] bytes = s.ToArray();

            // write length and bytes into buffer
            stream.WriteInt(bytes.Length);
            stream.WriteByteArray(bytes);

            return true;
        }

        public override bool Unpack(UdpStream stream, ref object o)
        {
            // read length and create array, then read bytes into array
            byte[] bytes = new byte[stream.ReadInt()];
            stream.ReadByteArray(bytes);

            var s = new MemoryStream(bytes);

            // convert bytes to string
            var serializer = new SharpSerializer(true);
            o = serializer.Deserialize(s);

            return true;
        }
    }
}
