namespace PacketApp{
    public abstract class AbstractFlowResolver : IFlowResolver {
        public virtual void onConnected (ITransformContext ctx, object data) { }
        public virtual void onReadReady (ITransformContext ctx, ByteBuffer buf) { }
        public virtual void onRead (ITransformContext ctx, ByteBuffer buf) { }
    }
}
