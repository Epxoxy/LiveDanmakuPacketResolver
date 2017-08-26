using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace PacketApp {

    internal class Packet {
        public const int heartbeatId = 2;
        public const int handshakeId = 7;
        public int length;
        public short headerLength;
        public short devType;
        public int msgType;
        public int device;
        public string payload;

        public int payloadLength;
        public object entity;
        public Packet () { }
        public Packet (int length) { this.length = length; }

        public override string ToString () {
            return $"[{DateTime.Now.ToString("HH:mm:ss fff")}]\n\tlength:{length},header:{headerLength},devType{devType},device:{device},msgType:{msgType},payloadLength:{payloadLength}\n\tdata:{entity ?? payload}";
        }
    }

}