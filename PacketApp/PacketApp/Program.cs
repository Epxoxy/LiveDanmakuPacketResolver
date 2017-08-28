using System;

namespace PacketApp {
    class Program {
        private static TransformResolverLite resolver;
        static void Main (string[] args) {
            var roomId = -1;
            Console.WriteLine ("Input room id:");
            var roomIdText = Console.ReadLine ();
            var api = new BiliApi ("");
            string realRoomText = null;
            while (true) {
                if (roomId == -1 && !int.TryParse (roomIdText, out roomId)) {
                    Console.WriteLine ("Error id, try again.");
                    roomIdText = Console.ReadLine ();
                } else {
                    realRoomText = api.getRealRoomId (roomIdText);
                    if (string.IsNullOrEmpty (realRoomText)) {
                        roomId = -1;
                        Console.WriteLine ("Error id, try again.");
                        roomIdText = Console.ReadLine ();
                    } else break;
                }
            }
            Console.Clear();
            Console.WriteLine($"Connectting to room {roomIdText}({realRoomText})");
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
            var press = Console.ReadKey ();
            while (press.Key != ConsoleKey.Escape) {
                press = Console.ReadKey ();
            }
        }

    }
}