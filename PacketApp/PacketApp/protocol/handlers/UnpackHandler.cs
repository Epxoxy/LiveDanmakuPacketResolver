namespace PacketApp {

    internal class UnpackHandler : AbstractFlowResolver {
        private PacketFactory factory = new PacketFactory ();

        public override void onReadReady (ITransformContext ctx, ByteBuffer buf) {
            factory.setWorkFlow (buf);
        }

        public override void onRead (ITransformContext ctx, ByteBuffer buf) {
            factory.fireUnpack ();
        }
    }
}