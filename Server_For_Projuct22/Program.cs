using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server_for_projuct2
{
    class Program
    {
        const int portNo = 1500;
        //private const string ipAddress = "10.100.102.31";
        private const string ipAddress = "127.0.0.1";
        static void Main(string[] args)
        {

            System.Net.IPAddress localAdd = System.Net.IPAddress.Parse(ipAddress);

            TcpListener listener = new TcpListener(localAdd, portNo);

            Console.WriteLine("Simple TCP Server");
            Console.WriteLine("Listening to ip {0} port: {1}", ipAddress, portNo);
            Console.WriteLine("Server is ready.");

            // Start listen to incoming connection requests
            listener.Start();

            // infinit loop.
            while (true)
            {
                // AcceptTcpClient - Blocking call
                // Execute will not continue until a connection is established

                // We create an instance of ChatClient so the server will be able to 
                // server multiple client at the same time.
                TcpClient tcp = listener.AcceptTcpClient();
                if (tcp != null)
                {
                    Thread t = new Thread(() => StartClient(tcp));
                    //Thread t = new Thread(new ThreadStart(StartClient),tcp);
                    t.Start();
                }

            }
        }
        /// <summary>
        /// Connects the client to the server.
        /// </summary>
        /// <param name="tcp"></param>
        public static void StartClient(TcpClient tcp)
        {
            Client user = new Client(tcp);
            Console.WriteLine("client connected");
        }
    }
}
