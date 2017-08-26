using System;
using System.Threading.Tasks;

namespace PacketApp {

    internal class DealProtocolHandler : IFlowResolver {
        private PacketFactory factory = new PacketFactory ();
        private int retryTimes = 3;

        public void onConnected (ITransformContext ctx, object data) {
            //Handshake
            int channelId = (int) data;
            var tmpUid = (long) (1e14 + 2e14 * new Random ().NextDouble ());
            var payload = "{ \"roomid\":" + channelId + ", \"uid\":" + tmpUid + "}";
            var bytes = factory.packSimple (Packet.handshakeId, payload);
            try {
                ctx.writeAndFlush (bytes);
            } catch (Exception e) {
                e.printStackTrace ();
                ctx.close ();
                return;
            }
            heartbeat (ctx);
        }

        public void onReadReady(ITransformContext ctx, ByteBuffer buf){
            factory.setWorkFlow(buf);
        }

        public void onRead (ITransformContext ctx, ByteBuffer buf) {
            factory.fireUnpack ();
        }

        private void heartbeat (ITransformContext ctx) {
            //Heartbeat
            Task.Run (async () => {
                var errorTimes = 0;
                var ping = factory.packSimple (Packet.heartbeatId, payload: "");
                while (ctx.isActive ()) {
                    try {
                        ctx.writeAndFlush (ping);
                        "Heartbeat...".toConsole ();
                        await Task.Delay (30000);
                    } catch (Exception e) {
                        e.printStackTrace ();
                        if (errorTimes > retryTimes) break;
                        ++errorTimes;
                    }
                }
                ctx.close ();
            });
        }

    }
}