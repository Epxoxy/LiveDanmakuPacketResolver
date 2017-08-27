using System;

namespace PacketApp {

    public interface IFlowResolver {
        void onConnected (ITransformContext ctx, object data);
        void onReadReady(ITransformContext ctx, ByteBuffer buf);
        void onRead (ITransformContext ctx, ByteBuffer buf);
    }

    public interface IWrappedResolver {
        IFlowResolver Resolver { get; }
        WrappedResolver Next { get; set; }
    }
    
    public class WrappedResolver : IWrappedResolver {
        public IFlowResolver Resolver { get; }
        public WrappedResolver Next { get; set; }
        public WrappedResolver (IFlowResolver resolver) {
            this.Resolver = resolver;
        }
        public WrappedResolver (IFlowResolver resolver, WrappedResolver next) : this (resolver) {
            this.Next = next;
        }
        public WrappedResolver (IFlowResolver resolver, IFlowResolver next) : this (resolver, new WrappedResolver (next)) { }
    }

    internal class WrappedHeadResolver : IWrappedResolver, IFlowResolver {
        public IFlowResolver Resolver => this;
        public WrappedResolver Next { get; set; }

        public void onConnected (ITransformContext ctx, object data) {
            var current = Next;
            while (current != null) {
                current.Resolver.onConnected (ctx, data);
                current = current.Next;
            }
        }

        public void onRead (ITransformContext ctx, ByteBuffer buf) {
            var current = Next;
            while (current != null) {
                current.Resolver.onRead (ctx, buf);
                current = current.Next;
            }
        }

        public void onReadReady(ITransformContext ctx, ByteBuffer buf){
            var current = Next;
            while (current != null) {
                current.Resolver.onReadReady(ctx, buf);
                current = current.Next;
            }
        }
    }

}