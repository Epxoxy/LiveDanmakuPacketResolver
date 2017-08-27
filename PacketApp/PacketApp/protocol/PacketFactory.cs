using System;
using System.Text;
using System.Threading.Tasks;


namespace PacketApp {
    internal class PacketFactory {
        private const int baseLength = 16;
        private const int maxLength = 10 * 1024 * 1024;
        private bool unpacking = false;
        private ByteBuffer workFlow;
        private int max;

        public PacketFactory () { }
        public PacketFactory (ByteBuffer workFlow) {
            this.workFlow = workFlow;
        }

        public byte[] pack (Packet packet) {
            var payload = Encoding.UTF8.GetBytes (packet.payload);
            packet.length = payload.Length + baseLength;
            packet.headerLength = baseLength;
            try {
                var buf = ByteBuffer.allocate (packet.length);
                buf.writeInt (packet.length);
                buf.writeShort (packet.headerLength);
                buf.writeShort (packet.devType);
                buf.writeInt (packet.msgType);
                buf.writeInt (packet.device);
                if (payload.Length > 0)
                    buf.writeBytes (payload);
                return buf.toArray ();
            } catch (Exception e) {
                e.printStackTrace ();
                return null;
            }
        }

        public byte[] packSimple (int msgType, string payload) {
            return pack (new Packet () {
                devType = 1,
                    msgType = msgType,
                    device = 1,
                    payload = payload
            });
        }

        public void setWorkFlow (ByteBuffer workFlow) {
            this.workFlow = workFlow;
        }
        
        public void fireUnpack () { 
            ($"\n---------------------").toConsole ();
            ($"Fire unpack {workFlow.ReadableBytes}").toConsole ();
            if (workFlow.ReadableBytes > max) {
                max = workFlow.ReadableBytes;
                ($"WorkFlow Max {max}").toDebug ();
            }
            if (workFlow.ReadableBytes > 0 && !unpacking) {
                ($"Continue unpack {workFlow.ReadableBytes}").toDebug();
                Task.Run (() => {
                    unpacking = true;
                    while (unpacking) {
                        ($"\n---------------------").toConsole ();
                        ($"[{DateTime.Now.ToString("HH:mm:ss fff")}] Unpacking loop").toConsole();
                        unpack0 (workFlow);
                        if (workFlow.ReadableBytes < baseLength)
                            unpacking = false;
                    }
                }).ContinueWith(task => {
                    task.Exception?.printStackTrace();
                });
            }
        }

        private bool unpack0 (ByteBuffer flow) {
            if(flow.ReadableBytes < baseLength)
                return false;
            flow.markReaderIndex ();
            int packetLength = flow.readInt ();
            int payloadLength = packetLength - sizeof (int);
            if (packetLength < baseLength || 
                flow.ReadableBytes < payloadLength) {
                flow.resetReaderIndex ();
                return false;
            }
            ("*******************************").toConsole ();
            ($"{packetLength}/{flow.ReadableBytes + sizeof(int)} Unpacking .......").toConsole ();
            ("*******************************").toConsole ();
            if (packetLength >= baseLength && packetLength < maxLength) {
                Packet packet = null;
                if (unpack1 (flow, packetLength, out packet)) {
                    flow.discardReadBytes (); //Same to buf.clear();
                } /*else packet receive not complete*/
                (packet == null ? "\tUnpack fail." : DateTime.Now.ToString ("HH:mm:ss fff")).toConsole ();
                packet.ToString ().toDebug ();
                ("*******************************").toConsole ();
                return true;
            } else {
                flow.discardReadBytes ();
                return true;
            }

        }

        private bool unpack1 (ByteBuffer flow, int packetLength, out Packet packet) {
            packet = new Packet (packetLength);
            packet.headerLength = flow.readShort ();
            packet.devType = flow.readShort ();
            packet.msgType = flow.readInt ();
            packet.device = flow.readInt ();
            packet.payloadLength = packetLength - baseLength;
            ($"Unpacking, packetType-> {packet.msgType}").toConsole ();
            byte[] payload = null;
            switch (packet.msgType) {
                case 1: //online count param
                case 2: //online count param
                case 3: //online count param
                    var userCount = flow.readInt ();
                    packet.entity = userCount;
                    break;
                case 5: //danmaku data
                    payload = new byte[packet.payloadLength];
                    flow.readBytes (payload, 0, payload.Length);
                    packet.payload = Encoding.UTF8.GetString (payload);
                    if (!toEntity (packet.payload, out packet.entity)) {
                        return false;
                    }
                    break;
                case 4: //unknow
                case 6: //newScrollMessage
                case 7:
                case 8: //hand shake ok.
                case 16:
                default:
                    payload = new byte[packet.payloadLength];
                    flow.readBytes (payload, 0, payload.Length);
                    ("Mark for search type of " + packet.msgType).toConsole ();
                    packet.payload = Encoding.UTF8.GetString (payload);
                    break;
            }
            return true;
        }

        private bool toEntity (string json, out object obj) {
            obj = json;
            try {
                //
                return true;
            } catch (Exception e) {
                e.printStackTrace ();
                return false;
            }
        }

    }
}