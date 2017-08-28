using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace PacketApp {
    internal class PacketFactory {
        private const int baseLength = 16;
        private const int maxLength = 10 * 1024 * 1024;
        private ByteBuffer workFlow;
        private int max;
        private object lockHelper = new object();

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
            if(workFlow.ReadableBytes >= baseLength && Monitor.TryEnter(lockHelper)){
                Monitor.Exit(lockHelper);
                readyUnpack();
            }
        }

        private void readyUnpack() {
            Task.Run(() => {
                lock (lockHelper) {
                    var packetAvailable = true;
                    ($"Continue unpack {workFlow.ReadableBytes}").toDebug();
                    while (packetAvailable) {
                        try {
                            ($"\n---------------------").toConsole();
                            ($"[{DateTime.Now.ToString("HH:mm:ss fff")}] Unpacking loop").toConsole();
                            unpackHead(workFlow);
                            if (workFlow.ReadableBytes < baseLength) {
                                packetAvailable = false;
                                break;
                            }
                        } catch (Exception e) {
                            e.printStackTrace();
                            packetAvailable = false;
                            break;
                        }
                    }
                }
            }).ContinueWith(task => {
                task.Exception?.printStackTrace();
            });
        }

        private bool unpackHead (ByteBuffer flow) {
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
            if (packetLength < maxLength) {
                Packet packet = null;
                if (unpackPayload (flow, packetLength, out packet)) {
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

        private bool unpackPayload (ByteBuffer flow, int packetLength, out Packet packet) {
            packet = new Packet (packetLength);
            packet.headerLength = flow.readShort ();
            packet.devType = flow.readShort ();
            packet.msgType = flow.readInt ();
            packet.device = flow.readInt ();
            packet.payloadLength = packetLength - baseLength;
            ($"Unpacking, packetType-> {packet.msgType}").toConsole ();
            byte[] payload = null;
            switch (packet.msgType) {
                case 1: //Hot update
                case 2: //Hot update
                case 3: //Hot update
                    var hot = flow.readInt ();
                    packet.entity = hot;
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