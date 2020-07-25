using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Item_Manager
{
    class Program
    {
        static void Main(string[] args)
        {
            Oauth.Start();
            Task.Run(() => { Network.StartListen(); });
            //DestinyAPI.GetItemHash("Whisper of the Worm");
            //DestinyAPI.TransferItem("Whisper of the Worm", "Hunter", "true");
            //DestinyAPI.TransferItem("Whisper of the Worm", "Warlock", "false");
            Console.ReadLine();
        }
    }
}
