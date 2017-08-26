using System;

namespace PacketApp {
    class Program {
        private static TransformResolverLite resolver;
        static void Main (string[] args) {
            var roomId = 0;
            if (int.TryParse (Console.ReadLine (), out roomId)) {
                var api = new BiliApi ("");
                var roomText = api.getRealRoomId ("320");
                if (string.IsNullOrEmpty (roomText)) {
                    //TODO 
                }
                var realRoomId = int.Parse (roomText);
                bool mayNotExist;
                string server;
                string portText;
                int port;
                if (!api.getDmServerAddr (roomText, out server, out portText, out mayNotExist)) {
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