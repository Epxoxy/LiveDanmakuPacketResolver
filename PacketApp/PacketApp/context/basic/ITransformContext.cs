namespace PacketApp{
    
    public interface ITransformContext {

        void addLast (IFlowResolver resolver);
        void remove (IFlowResolver resolver);
        bool isActive ();

        void connect(string host, int port);
        void writeAndFlush (byte[] data);
        void write (byte[] data);
        void flush ();
        void close ();
    }

}
