using System;

namespace PacketApp
{
    public static class DebugEx
    {
        public static void toConsole(this string text)
        {
            Console.WriteLine(text);
        }

        public static void toDebug(this string text)
        {
            System.Diagnostics.Debug.WriteLine(text);
        }
    }
}
