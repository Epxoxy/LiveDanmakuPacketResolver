namespace PacketApp {

    public interface IFlowResolver {
        void onConnected (ITransformContext ctx, object data);
        void onReadReady (ITransformContext ctx, ByteBuffer buf);
        void onRead (ITransformContext ctx, ByteBuffer buf);
    }
    
}