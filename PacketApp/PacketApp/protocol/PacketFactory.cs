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
        private static object lockHelper = new object ();

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
                buf.writeInt (packet.packetType);
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
                    packetType = msgType,
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
            if (workFlow.ReadableBytes >= baseLength && Monitor.TryEnter (lockHelper)) {
                Monitor.Exit (lockHelper);
                readyUnpack ();
            }
        }

        private void readyUnpack () {
            Task.Run (() => {
                lock (lockHelper) {
                    var packetAvailable = true;
                    ($"Continue unpack {workFlow.ReadableBytes}").toDebug ();
                    while (packetAvailable) {
                        try {
                            ($"\n---------------------").toConsole ();
                            ($"[{DateTime.Now.ToString("HH:mm:ss fff")}] Unpacking loop").toConsole ();
                            unpack (workFlow);
                            if (workFlow.ReadableBytes < baseLength) {
                                packetAvailable = false;
                                break;
                            }
                        } catch (Exception e) {
                            System.Diagnostics.Debug.WriteLine (e.ToString ());
                            e.printStackTrace ();
                            packetAvailable = false;
                            break;
                        }
                    }
                }
            }).ContinueWith (task => {
                task.Exception?.printStackTrace ();
            });
        }

        private bool unpack (ByteBuffer flow) {
            if (flow.ReadableBytes < baseLength)
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
                Packet packet = default (Packet);
                if (unpackPayload (flow, packetLength, out packet)) {
                    flow.discardReadBytes (); //Same to buf.clear();
                    var now = DateTime.Now.ToString ("HH:mm:ss fff");
                    now.toConsole ();
                    now.toDebug ();
                    ("  " + packet.ToString ()).toDebug ();
                } else "\tUnpack fail.".toConsole (); /*else packet receive not complete*/
                ("*******************************").toConsole ();
                return true;
            } else {
                flow.discardReadBytes ();
                return true;
            }

        }

        private bool unpackPayload (ByteBuffer flow, int packetLength, out Packet packet) {
            packet = new Packet { length = packetLength };
            packet.headerLength = flow.readShort ();
            packet.devType = flow.readShort ();
            packet.packetType = flow.readInt ();
            packet.device = flow.readInt ();
            packet.payloadLength = packetLength - baseLength;
            ($"Unpacking, packetType-> {packet.packetType}").toConsole ();
            byte[] payload = null;
            switch (packet.packetType) {
                case 1: //Hot update
                case 2: //Hot update
                case 3: //Hot update
                    var hot = flow.readInt ();
                    packet.payload = hot.ToString ();
                    break;
                case 5: //danmaku data
                    payload = new byte[packet.payloadLength];
                    flow.readBytes (payload, 0, payload.Length);
                    packet.payload = Encoding.UTF8.GetString (payload);
                    if (!isValid (packet.payload)) {
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
                    ("Mark for search type of " + packet.packetType).toConsole ();
                    packet.payload = Encoding.UTF8.GetString (payload);
                    break;
            }
            return true;
        }

        private bool isValid (string json) {
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