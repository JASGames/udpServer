using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace udpServer
{
    public enum Command : byte
    {
        Id,
        Guid,
        Position,
        Velocity
    }

    public class NetworkVector
    {
        public float x;
        public float y;
        public float z;

        public NetworkVector(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    public class Packet
    {
        public Command command;
        public object data;
        public float timestamp;

        public Packet(Command c, object o, float t)
        {
            this.command = c;
            this.data = o;
            this.timestamp = t;
        }
    }
}
