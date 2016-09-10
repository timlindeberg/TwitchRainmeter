using System;
using PluginTwitchChat;
using System.Diagnostics;
using System.Drawing;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            var f = new Font("Nexa Bold", 14);
            StringMeasurer me = new StringMeasurer(f);
            Debug.WriteLine(me.MeasureString("lol "));
            Debug.WriteLine(me.MeasureString("     "));

            
            var path = @"C:\Users\Tim Lindeberg\Documents\Rainmeter\Skins\Twitch\@Resources\images";
            ImageDownloader i = new ImageDownloader(path, 1);
            MessageHandler m = new MessageHandler(new Size(500, 500), me, true, i);
            TwitchClient c = new TwitchClient("timsan90", "oauth:pm1xf56rfa61ooquew11yoli1sg0cq", m, i);
            c.JoinChannel("#clintstevens");
            while (true)
            {
                var msg = Console.ReadLine();
                c.SendMessage(msg);
                m.Update();
            }
        }


    }
}
