using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PacketApp {
    
    public interface ITransformContext {
        void addLast (IFlowResolver resolver);
        bool isActive ();
        void writeAndFlush (byte[] data);
        void write (byte[] data);
        void flush ();
        void close ();
    }

    internal class TransformResolverLite : ITransformContext {
        private bool isAlive = false;
        private TcpClient client;
        private NetworkStream stream;
        private IWrappedResolver head = new WrappedHeadResolver ();
        private IWrappedResolver tail;

        [SuppressMessage ("Microsoft.Performance", "CS4014")]
        public async void connect (string host, int port, int channelId) {
            client = new TcpClient ();
            client.Connect (host, port);
            stream = client.GetStream ();
            isAlive = true;
            head?.Resolver.onConnected (this, channelId);
            while (isAlive) {
                if (!stream.DataAvailable) {
                    await Task.Delay (100);
                    continue;
                }
                int readSize = 0;
                var cache = new byte[1024];
                var buffer = ByteBuffer.allocate (1024);
                head?.Resolver.onReadReady(this, buffer);
                while ((readSize = stream.Read (cache, 0, cache.Length)) > 0) {
                    buffer.writeBytes (cache, 0, readSize);
                    head?.Resolver.onRead(this, buffer);
                }
            }
        }

        public bool isActive () => isAlive;

        public void write (byte[] bytes) {
            stream.Write (bytes, 0, bytes.Length);
        }

        public void flush () {
            stream.Flush ();
        }

        public void writeAndFlush (byte[] data) {
            write (data);
            flush ();
        }

        public void close () {
            isAlive = false;
            client.Close ();
            stream = null;
        }

        public void addLast (IFlowResolver resolver) {
            var ctx = new WrappedResolver (resolver);
            if (tail == null)
                tail = head;
            tail.Next = ctx;
            tail = ctx;
        }

    }

}