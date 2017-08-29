namespace PacketApp{

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


}
