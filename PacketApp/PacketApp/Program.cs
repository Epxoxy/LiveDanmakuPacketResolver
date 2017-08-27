using System;

namespace PacketApp {
    class Program {
        private static TransformResolverLite resolver;
        static void Main (string[] args) {
            var roomId = 0;
            var roomIdText = Console.ReadLine();
            if (int.TryParse (roomIdText, out roomId)) {
                var api = new BiliApi ("");
                var realRoomText = api.getRealRoomId (roomIdText);
                if (string.IsNullOrEmpty (realRoomText)) {
                    //TODO 
                }
                var realRoomId = int.Parse (realRoomText);
                bool mayNotExist;
                string server;
                string portText;
                int port;
                if (!api.getDmServerAddr (realRoomText, out server, out portText, out mayNotExist)) {
                    if (mayNotExist)
                        return;
                    //May exist, generate default address
                    var hosts = BiliApi.Const.DefaultHosts;
                    server = hosts[new Random ().Next (hosts.Length)];
                    port = BiliApi.Const.DefaultChatPort;
                } else if (!int.TryParse (portText, out port)) {
                    port = BiliApi.Const.DefaultChatPort;
                }
                resolver = new TransformResolverLite ();
                resolver.addLast (new DealProtocolHandler ());
                resolver.connect (server, port, realRoomId);
            }
            Console.ReadKey ();
        }

    }
}