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
    /// <summary>
    /// Main program class for the TCP server application.
    /// </summary>
    class Program
    {
        const int portNo = 1500; // Port number
        //private const string ipAddress = "10.100.102.31";
        private const string ipAddress = "127.0.0.1"; // IP address
        // DoS protection configuration
        private static readonly DosProtection dosProtection = new DosProtection(maxRequests: 100, timeWindow: TimeSpan.FromSeconds(10));

        /// <summary>
        /// Main entry point for the TCP server.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        static void Main(string[] args)
        {
            // Parse IP address
            System.Net.IPAddress localAdd = System.Net.IPAddress.Parse(ipAddress);

            // Initialize TCP listener
            TcpListener listener = new TcpListener(localAdd, portNo);

            Console.WriteLine("Simple TCP Server");
            Console.WriteLine("Listening to ip {0} port: {1}", ipAddress, portNo);
            Console.WriteLine("Server is ready.");

            // Start listening to incoming connection requests
            listener.Start();

            // Infinite loop to handle incoming connections
            while (true)
            {
                // Accept incoming TCP client connection
                TcpClient tcp = listener.AcceptTcpClient();

                // Check if the request is allowed based on DoS protection
                if (tcp != null && dosProtection.AllowRequest(((IPEndPoint)tcp.Client.RemoteEndPoint).Address.ToString()))
                {
                    // Start a new thread to handle the client connection
                    Thread t = new Thread(() => StartClient(tcp));
                    t.Start();
                }
                else
                {
                    // Handle blocked request due to DoS protection
                    Console.WriteLine("Request blocked due to DoS protection.");
                    tcp.Close();
                }
            }
        }

        /// <summary>
        /// Connects the client to the server.
        /// </summary>
        /// <param name="tcp">TCP client connection.</param>
        public static void StartClient(TcpClient tcp)
        {
            // Create a new client instance
            Client user = new Client(tcp);
            Console.WriteLine("Client connected");
        }
    }

    /// <summary>
    /// Class representing DoS protection for the TCP server.
    /// </summary>
    public class DosProtection
    {
        private Dictionary<string, DateTime> requestTimeLog;
        private Dictionary<string, int> requestCountLog;
        private int maxRequests;
        private TimeSpan timeWindow;
        private object lockObj = new object();

        /// <summary>
        /// Initializes a new instance of the DosProtection class.
        /// </summary>
        /// <param name="maxRequests">Maximum allowed requests within the time window.</param>
        /// <param name="timeWindow">Time window for request counting.</param>
        public DosProtection(int maxRequests, TimeSpan timeWindow)
        {
            this.maxRequests = maxRequests;
            this.timeWindow = timeWindow;
            this.requestTimeLog = new Dictionary<string, DateTime>();
            this.requestCountLog = new Dictionary<string, int>();
        }

        /// <summary>
        /// Checks if a request from an IP address is allowed based on DoS protection rules.
        /// </summary>
        /// <param name="ipAddress">IP address of the request.</param>
        /// <returns>True if the request is allowed; otherwise, false.</returns>
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

        /// <summary>
        /// Cleans up old request logs based on the time window.
        /// </summary>
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
