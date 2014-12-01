using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

                            var packet = new Packet(Command.Id, Clients.Count, 0);
                            ev.Connection.Send(packet);
                            break;
                        case UdpEventType.Disconnected:
                            Clients.Remove(ev.Connection);
                            Console.WriteLine("Client disconnected from {0}", ev.Connection.RemoteEndPoint);
                            break;
                            //When we receive, just forward to all clients
                        case UdpEventType.ObjectReceived:
                            SendToOtherPlayers(ev.Object as Packet, ev.Connection);
                            break;
                        case UdpEventType.ObjectLost:
                            //ev.Connection.Send(ev.Object);
                            break;
                    }
                }
                
                Thread.Sleep(16);
            }
        }

        private static void SendToOtherPlayers(Packet packet, UdpConnection connection)
        {
            foreach (var client in Clients)
            {
                if (client != connection)
                {
                    client.Send(packet);
                }
            }
        }
    }

    internal class ServerSerializer : UdpSerializer
    {
        public override bool Pack(UdpStream stream, ref object o)
        {
            Packet p = (Packet)o;

            //command
            stream.WriteByte((byte)p.command);
            //data
            if (p.command == Command.Engine)
            {
                var vec = (NetworkVector)p.data;
                stream.WriteFloat(vec.x);
                stream.WriteFloat(vec.y);
                stream.WriteFloat(vec.z);
            }
            else if (p.command == Command.Rigidbody)
            {
                var rigidbody = (NetworkRigidbody)p.data;

                stream.WriteFloat(rigidbody.pos.x);
                stream.WriteFloat(rigidbody.pos.y);
                stream.WriteFloat(rigidbody.pos.z);

                stream.WriteFloat(rigidbody.rot.x);
                stream.WriteFloat(rigidbody.rot.y);
                stream.WriteFloat(rigidbody.rot.z);

                stream.WriteFloat(rigidbody.vel.x);
                stream.WriteFloat(rigidbody.vel.y);
                stream.WriteFloat(rigidbody.vel.z);

                stream.WriteFloat(rigidbody.ang.x);
                stream.WriteFloat(rigidbody.ang.y);
                stream.WriteFloat(rigidbody.ang.z);
            }
            else if (p.command == Command.Id)
            {
                stream.WriteInt((int)p.data);
            }
            //timestamp
            stream.WriteFloat(p.timestamp);

            return true;
        }

        public override bool Unpack(UdpStream stream, ref object o)
        {

            //command
            var packet = new Packet(Command.Id, null, 0);
            packet.command = (Command)stream.ReadByte();
            //data
            if (packet.command == Command.Engine)
            {
                var vec = new NetworkVector(stream.ReadFloat(), stream.ReadFloat(), stream.ReadFloat());
                packet.data = vec;
            }
            else if (packet.command == Command.Rigidbody)
            {
                var pos = new NetworkVector(stream.ReadFloat(), stream.ReadFloat(), stream.ReadFloat());
                var rot = new NetworkVector(stream.ReadFloat(), stream.ReadFloat(), stream.ReadFloat());
                var vel = new NetworkVector(stream.ReadFloat(), stream.ReadFloat(), stream.ReadFloat());
                var ang = new NetworkVector(stream.ReadFloat(), stream.ReadFloat(), stream.ReadFloat());

                packet.data = new NetworkRigidbody(pos, rot, vel, ang);
            }
            else if (packet.command == Command.Id)
            {
                packet.data = stream.ReadInt();
            }
            //timestamp
            packet.timestamp = stream.ReadFloat();

            o = packet;

            return true;
        }
    }
}
