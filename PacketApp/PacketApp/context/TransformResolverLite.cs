using System.Net.Sockets;
using System.Threading.Tasks;

namespace PacketApp {

    internal class TransformResolverLite : ITransformContext {
        private bool isAlive = false;
        private TcpClient client;
        private NetworkStream stream;
        private IWrappedResolver head = new WrappedHeadResolver ();
        private IWrappedResolver tail;

        public async void connect (string host, int port) {
            client = new TcpClient ();
            client.Connect (host, port);
            stream = client.GetStream ();
            isAlive = true;
            head?.Resolver.onConnected (this, null);
            while (isAlive) {
                if (!stream.DataAvailable) {
                    await Task.Delay (100);
                    continue;
                }
                int readSize = 0;
                var cache = new byte[1024];
                var buffer = ByteBuffer.allocate (1024);
                head?.Resolver.onReadReady (this, buffer);
                try {
                    while ((readSize = stream.Read (cache, 0, cache.Length)) > 0) {
                        buffer.writeBytes (cache, 0, readSize);
                        head?.Resolver.onRead (this, buffer);
                    }
                } catch (System.Exception e) {
                    e.printStackTrace ();
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

        public void remove (IFlowResolver resolver) {
            var current = head;
            var previous = head;
            while (current != null) {
                if (current.Resolver == resolver) {
                    previous.Next = current.Next;
                    if (current == tail) {
                        tail = previous;
                    }
                }
                //Prepare for next check
                previous = current;
                current = current.Next;
            }
        }

    }

    internal class WrappedHeadResolver : IWrappedResolver, IFlowResolver {
        public IFlowResolver Resolver => this;
        public WrappedResolver Next { get; set; }

        public void onConnected (ITransformContext ctx, object data) {
            Task.Run (() => {
                var current = Next;
                while (current != null) {
                    current.Resolver.onConnected (ctx, data);
                    current = current.Next;
                }
            }).ContinueWith (task => {
                task.Exception?.printStackTrace ();
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public void onRead (ITransformContext ctx, ByteBuffer buf) {
            Task.Run (() => {
                var current = Next;
                while (current != null) {
                    current.Resolver.onRead (ctx, buf);
                    current = current.Next;
                }
            }).ContinueWith (task => {
                task.Exception?.printStackTrace ();
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public void onReadReady (ITransformContext ctx, ByteBuffer buf) {
            Task.Run (() => {
                var current = Next;
                while (current != null) {
                    current.Resolver.onReadReady (ctx, buf);
                    current = current.Next;
                }
            }).ContinueWith (task => {
                task.Exception?.printStackTrace ();
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

    }

}