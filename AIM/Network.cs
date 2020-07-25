using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace AIM
{
    static class Network
    {
        private const int PORT_NO = 55555;
        private const string SERVER_IP = "174.97.176.64";

        private static TcpClient client;
        private static NetworkStream nwStream;

        public static void Send(string textToSend)
        {
            client = new TcpClient(SERVER_IP, PORT_NO);
            nwStream = client.GetStream();
            byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(textToSend);
            nwStream.Write(bytesToSend, 0, bytesToSend.Length);
        }

        public static string Receive()
        {
            byte[] bytesToRead = new byte[client.ReceiveBufferSize];
            int bytesRead = nwStream.Read(bytesToRead, 0, client.ReceiveBufferSize);
            return Encoding.ASCII.GetString(bytesToRead, 0, bytesRead);
        }

        public static void CloseClient()
        {
            client.Close();
        }
    }
}
