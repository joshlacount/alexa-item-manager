using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;

namespace Item_Manager
{
    static class Network
    {
        private const int PORT_NO = 55555;
        private const string SERVER_IP = "";

        private static TcpListener listener = new TcpListener(IPAddress.Parse(SERVER_IP), PORT_NO);
        private static bool isListening;

        public static void StartListen()
        {
            Console.WriteLine("Listening...");
            listener.Start();
            isListening = true;

            while (isListening)
            {
                TcpClient client = listener.AcceptTcpClient();

                NetworkStream nwStream = client.GetStream();
                byte[] buffer = new byte[client.ReceiveBufferSize];

                int bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);

                string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                var args = dataReceived.Split('-');
                Console.WriteLine("Received: " + dataReceived);

                DestinyAPI.TransferItem(args[0], args[1], "true");
                DestinyAPI.TransferItem(args[0], args[2], "false");

                Reply("Item transfered", ref nwStream);
            }
            listener.Stop();
        }

        public static void StopListen()
        {
            isListening = false;
        }

        private static void Reply(string msg, ref NetworkStream nws)
        {
            Console.WriteLine("Sending back: " + msg);
            byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(msg);
            nws.Write(bytesToSend, 0, bytesToSend.Length);
        }
    }
}
