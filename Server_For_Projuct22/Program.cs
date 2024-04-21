using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        private static readonly DosProtection dosProtection = new DosProtection(maxRequests: 100, timeWindow: TimeSpan.FromSeconds(10));
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
                if (tcp != null && dosProtection.AllowRequest(((IPEndPoint)tcp.Client.RemoteEndPoint).Address.ToString()))
                {
                    Thread t = new Thread(() => StartClient(tcp));
                    t.Start();
                }
                else
                {
                    // Handle blocked request
                    Console.WriteLine("Request blocked due to DoS protection.");
                    tcp.Close();
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
    public class DosProtection
    {
        private Dictionary<string, DateTime> requestTimeLog;
        private Dictionary<string, int> requestCountLog;
        private int maxRequests;
        private TimeSpan timeWindow;
        private object lockObj = new object();
        public DosProtection(int maxRequests, TimeSpan timeWindow)
        {
            this.maxRequests = maxRequests;
            this.timeWindow = timeWindow;
            this.requestTimeLog = new Dictionary<string, DateTime>();
            this.requestCountLog = new Dictionary<string, int>();
        }
        public bool AllowRequest(string ipAddress)
        {
            lock (lockObj)
            {
                if (!requestTimeLog.ContainsKey(ipAddress))
                {
                    requestTimeLog[ipAddress] = DateTime.Now;
                    requestCountLog[ipAddress] = 1; // First request from this IP
                    return true;
                }
                if (requestTimeLog[ipAddress] < DateTime.Now - timeWindow)
                {
                    CleanupOldRequests();
                    requestTimeLog[ipAddress] = DateTime.Now; // Reset request time for this IP
                    requestCountLog[ipAddress] = 1; // Reset request count for this IP
                    return true;
                }
                if (requestCountLog.ContainsKey(ipAddress) && requestCountLog[ipAddress] < maxRequests)
                {
                    requestCountLog[ipAddress]++;
                    return true;
                }
                return false; // IP reached max requests within time window
            }
        }
        private void CleanupOldRequests()
        {
            var oldRequests = requestTimeLog.Where(kv => kv.Value < DateTime.Now - timeWindow).ToList();
            foreach (var oldRequest in oldRequests)
            {
                requestTimeLog.Remove(oldRequest.Key);
                requestCountLog.Remove(oldRequest.Key); // Remove request count for old entries
            }
        }
    }
}
