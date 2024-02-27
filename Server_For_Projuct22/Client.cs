using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using Server_for_projuct2;
using System.Net.Mail;
using System.Net;
using System.CodeDom.Compiler;
using System.Security.Cryptography;
using System.IO;
using System.Runtime.CompilerServices;

namespace Server_for_projuct2
{
    /// <summary>
    /// The ChatClient class represents info about each client connecting to the server.
    /// </summary>
    class Client
    {
        private RSAServiceProvider Rsa;
        private string ClientPublicKey;
        private string PrivateKey;
        private string SymmetricKey;
        public static Random _random = new Random();
        private Node Lobbys = new Node(new string[7]);
        private bool isHost = false;
        // Store list of all clients connecting to the server
        // the list is static so all memebers of the chat will be able to obtain list
        // of current connected client
        public static Hashtable AllClients = new Hashtable();
        public static List<Client> clientsList = new List<Client>();
        private int emailcode;
        private string client_email;
        // information about the client
        private TcpClient _client;
        private string _clientIP;
        // used for sending and reciving data
        private byte[] data;
        /// <summary>
        /// When the client gets connected to the server the server will create an instance of the ChatClient and pass the TcpClient
        /// </summary>
        /// <param name="client"></param>
        public Client(TcpClient client)
        {
            Rsa = new RSAServiceProvider();
            PrivateKey = Rsa.GetPrivateKey();
            _client = client;
            // get the ip address of the client to register him with our client list
            _clientIP = client.Client.RemoteEndPoint.ToString();
            // Add the new client to our clients collection
            AllClients.Add(_clientIP, this);
            clientsList.Add(this);
            // Read data from the client async
            data = new byte[_client.ReceiveBufferSize];
            // BeginRead will begin async read from the NetworkStream
            // This allows the server to remain responsive and continue accepting new connections from other clients
            // When reading complete control will be transfered to the ReviveMessage() function.
            _client.GetStream().BeginRead(data, 0, System.Convert.ToInt32(_client.ReceiveBufferSize), ReceiveMessage, null);
        }
        /// <summary>
        /// Receives a messege from the client and acts in accordance to the messege that was received.
        /// </summary>
        /// <param name="ar"></param>
         public static string RandomKey(int Length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string (Enumerable.Repeat(chars, Length).Select(s => s[_random.Next(s.Length)]).ToArray());
        }
    public void ReceiveMessage(IAsyncResult ar)
        {
            int bytesRead;
            try
            {
                lock (_client.GetStream())
                {
                    // call EndRead to handle the end of an async read.
                    bytesRead = _client.GetStream().EndRead(ar);
                }
                // if bytesread<1 -> the client disconnected
                if (bytesRead < 1)
                {
                    // remove the client from out list of clients
                    AllClients.Remove(_clientIP);
                    return;
                }
                else // client still connected
                {
                    string messageReceived = System.Text.Encoding.ASCII.GetString(data, 0, bytesRead);
                    Console.WriteLine(messageReceived);
                    if (messageReceived.StartsWith("PogurC")) 
                    {
                        messageReceived= messageReceived.Substring(6);
                        ClientPublicKey =  messageReceived;
                        SendMessage("PogurS" + Rsa.GetPublicKey());
                        //SendMessage(EncryptionServerPublicKeyReciever + "$" + Rsa.GetPublicKey());
                        SymmetricKey = RandomKey(32);
                        string EncryptedSymmerticKey = Rsa.Encrypt(SymmetricKey, ClientPublicKey);
                        SendMessage("YavulS" + EncryptedSymmerticKey);
                        //SendMessage(EncryptionSymmetricKeyReciever + "$" + EncryptedSymmerticKey);

                    }
                    else
                    {
                        byte[] Key = Encoding.UTF8.GetBytes(SymmetricKey);
                        byte[] IV = new byte[16];
                        messageReceived = AESServiceProvider.Decrypt(messageReceived, Key, IV);
                        if (messageReceived.StartsWith("regist:"))
                        {
                            string[] parts = messageReceived.Split(':');
                            if (isExistUsername(parts[1]))
                            {
                                SendMessage("regist:username_taken:");
                                Console.WriteLine("sent:username_taken");
                            }
                            else if (isExistPassword(parts[2]))
                            {
                                SendMessage("regist:password_taken:");
                                Console.WriteLine("sent:password_taken");
                            }
                            else
                            {
                                string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDBFilename=D:\projects\Server_For_Projuct22\Server_For_Projuct22\DATABASE1.MDF;Integrated Security=True";
                                SqlConnection connection = new SqlConnection(connectionString);
                                SqlCommand cmd = new SqlCommand();
                                cmd.Connection = connection;
                                cmd.CommandText = "INSERT INTO UsersDetails VALUES('" + parts[1] + "', '" + CreateMD5Hash(parts[2]) + "', '" + parts[3] + "')";
                                connection.Open();
                                int x = cmd.ExecuteNonQuery();
                                connection.Close();
                                if (x > 0)
                                {
                                    Emailer e = new Emailer(_clientIP, 1500);
                                    this.emailcode = e.getPasscode();
                                    e.SendEmail(parts[3]);
                                    SendMessage("regist:ok:");
                                    Console.WriteLine("sent: regist ok");
                                }
                                else
                                {
                                    SendMessage("regist:Not_ok:");
                                    Console.WriteLine("sent: regist not ok");
                                }
                            }
                        }
                        else
                        if (messageReceived.StartsWith("login:"))
                        {
                            string[] parts = messageReceived.Split(':');
                            string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDBFilename=D:\projects\Server_For_Projuct22\Server_For_Projuct22\DATABASE1.MDF;Integrated Security=True";
                            SqlConnection connection = new SqlConnection(connectionString);
                            SqlCommand cmd = new SqlCommand();
                            cmd.Connection = connection;
                            cmd.CommandText = "SELECT imagetext FROM CaptchaPic WHERE id='" + Int32.Parse(parts[3]) + "'";
                            connection.Open();
                            string answer = (string)cmd.ExecuteScalar();
                            connection.Close();
                            if (parts[4].Equals(answer))
                            {
                                //if login ok;
                                if (isExistUsername(parts[1]) && isExistPassword(parts[2]))
                                {
                                    SendMessage("login:ok:");
                                    Console.WriteLine("sent: login ok");
                                }
                                else
                                {
                                    SendMessage("login:Not_ok:");
                                    Console.WriteLine("sent: login not ok");
                                }
                            }
                            else
                            {
                                SendMessage("login:captchaIncorrect:");
                                Console.WriteLine("sent: login captcha incorrect");
                            }
                        }
                        else
                        if (messageReceived.StartsWith("vercode:"))
                        {
                            string[] parts = messageReceived.Split(':');
                            if (Convert.ToString(this.emailcode).Equals(parts[1]))
                            {
                                SendMessage("vercode:ok");
                                Console.WriteLine("sent: vercode ok");
                            }
                            else
                            {
                                SendMessage("vercode:notok");
                                Console.WriteLine("sent: vercode not ok");
                            }
                        }
                        else if (messageReceived.StartsWith("getcaptcha"))
                        {
                            Random rnd = new Random();
                            int id = rnd.Next(8) + 1;
                            SendMessage("captcha:" + id + ":");
                            Console.WriteLine("Sent: captcha:" + id + ":");
                        }
                        else if (messageReceived.StartsWith("reset"))
                        {
                            string[] parts = messageReceived.Split(':');
                            if (isExistEmail(parts[1]))
                            {
                                client_email= parts[1];
                                Emailer e = new Emailer(_clientIP, 1500);
                                this.emailcode = e.getPasscode();
                                e.SendEmail(parts[1]);
                                SendMessage("reset:email:verify");
                            }
                            else if (parts[1].Equals("vercode"))
                            {
                                if (Convert.ToString(this.emailcode).Equals(parts[2]))
                                {
                                    SendMessage("reset:vercode:ok");
                                    Console.WriteLine("sent: reset vercode ok");
                                }
                                else
                                {
                                    SendMessage("reset:vercode:notok");
                                    Console.WriteLine("sent: reset vercode not ok");
                                }
                            }
                            else if (parts[1].Equals("password"))
                            {
                                string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDBFilename=D:\projects\Server_For_Projuct22\Server_For_Projuct22\DATABASE1.MDF;Integrated Security=True";
                                SqlConnection connection = new SqlConnection(connectionString);
                                SqlCommand cmd = new SqlCommand();
                                cmd.Connection = connection;
                                cmd.CommandText = "UPDATE UsersDetails SET password='" + CreateMD5Hash(parts[2]) + "' WHERE email='" + client_email + "'";
                                connection.Open();
                                int x = cmd.ExecuteNonQuery();
                                connection.Close();
                                if (x > 0)
                                {
                                    SendMessage("reset:successful");
                                    Console.WriteLine("sent: reset successful");
                                }
                                else 
                                {
                                    SendMessage("reset:eror");
                                    Console.WriteLine("sent: reset eror");
                                }
                            }
                            else SendMessage("reset:email:notexist");
                        }
                        else if (messageReceived.StartsWith("game"))
                        {
                            string[] parts = messageReceived.Split(':');
                            if (parts[1].Equals("host"))
                            {
                                string[] lobby = new string[7];
                                lobby[0] = _clientIP;
                                Lobbys.setValue(lobby);
                                isHost= true;
                                SendMessage("game:hosted");
                            }
                            else if (parts[1].Equals("start"))
                            {
                                if (Lobbys.getValue()[0].Equals(_clientIP))
                                {
                                    if (Lobbys.getValue()[1] != null)
                                    {
                                        SendMessage("game:start:ok");
                                    }
                                    else SendMessage("game:start:not_enough_players");
                                }
                            }
                            else if (parts[1].Equals("join"))
                            {
                                Node temp = Lobbys;
                                int x=0;
                                while (temp != null)
                                {
                                    if (temp.getValue()[0]==null) 
                                    {
                                        SendMessage("join:invalid");
                                        Console.WriteLine("sent:join:invalid");
                                    }
                                    for(int i=1;i<7;i++)
                                    {
                                        if (temp.getValue()[i] == null)
                                        {
                                            temp.getValue()[i] = _clientIP;
                                            SendMessage("join:valid");
                                            Console.WriteLine("sent:join:valid");
                                            x = i;
                                            break;
                                        }
                                        if (i == 6 && temp.getValue()[i]!=null)
                                            temp=temp.getNext();
                                    }
                                }
                                if (x != 0)
                                {
                                    SendMessage("join:" + x, temp);
                                    Console.WriteLine("sent:join:"+x);
                                }
                            }
                        }
                        else if (messageReceived.StartsWith("isHost"))
                        {
                            if (isHost)
                            {
                                SendMessage("Host:true");
                                Console.WriteLine("sent:Host:true");
                            }
                            else
                            {
                                SendMessage("Host:false");
                                Console.WriteLine("sent:Host:false");
                            }
                        }
                    }
                }
                lock (_client.GetStream())
                {
                    // continue reading form the client
                    _client.GetStream().BeginRead(data, 0, System.Convert.ToInt32(_client.ReceiveBufferSize), ReceiveMessage, null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                AllClients.Remove(_clientIP);
            }
        }//end ReceiveMessage
        /// <summary>
        /// allow the server to send messages to the client.
        /// </summary>
        /// <param name="message"></param>
        public static string CreateMD5Hash(string Input)//crypts passwords
        {
            // Step 1, calculate MD5 hash from input
            System.Security.Cryptography.MD5 Md5 = System.Security.Cryptography.MD5.Create();
        byte[] InputBytes = System.Text.Encoding.ASCII.GetBytes(Input);
        byte[] HashBytes = Md5.ComputeHash(InputBytes);
        // Step 2, convert byte array to hex string
        StringBuilder StringBuilder = new StringBuilder();
            for (int i = 0; i<HashBytes.Length; i++)
            {
                StringBuilder.Append(HashBytes[i].ToString("X2"));
            }
            return StringBuilder.ToString();
        }
        public void SendMessage(string message, string clientIP)
        {

        }
        public void SendMessage(string message, Node Lobby)
        {
            foreach (Client client in clientsList)
            {
                for (int i = 0; i < 7; i++)
                {
                    if (client._clientIP.Equals(Lobby.getValue()[i]))
                    {
                        try
                        {
                            System.Net.Sockets.NetworkStream ns;
                            lock (client._client.GetStream())
                            {
                                if (!(message.StartsWith("Pogur")) && !(message.StartsWith("Yavul")))
                                {
                                    byte[] Key = Encoding.UTF8.GetBytes(client.SymmetricKey);
                                    byte[] IV = new byte[16];
                                    message = AESServiceProvider.Encrypt(message, Key, IV);
                                }
                                // we use lock to present multiple threads from using the networkstream object
                                // this is likely to occur when the server is connected to multiple clients all of 
                                // them trying to access to the networkstram at the same time.
                                ns = client._client.GetStream();
                                // Send data to the client
                                byte[] bytesToSend = System.Text.Encoding.ASCII.GetBytes(message);
                                ns.Write(bytesToSend, 0, bytesToSend.Length);
                                ns.Flush();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
            }
        }
        public void SendMessage(string message)
        {
            try
            {
                System.Net.Sockets.NetworkStream ns;
                lock (_client.GetStream())
                {
                    if (!(message.StartsWith("Pogur")) && !(message.StartsWith("Yavul")))
                    {
                        byte[] Key = Encoding.UTF8.GetBytes(SymmetricKey);
                        byte[] IV = new byte[16];
                        message = AESServiceProvider.Encrypt(message, Key, IV);
                    }
                    // we use lock to present multiple threads from using the networkstream object
                    // this is likely to occur when the server is connected to multiple clients all of 
                    // them trying to access to the networkstram at the same time.
                    ns = _client.GetStream();
                    // Send data to the client
                    byte[] bytesToSend = System.Text.Encoding.ASCII.GetBytes(message);
                    ns.Write(bytesToSend, 0, bytesToSend.Length);
                    ns.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }//end SendMessage
        /// <summary>
        /// Receives a string that represents a username and checks if the username exists in the sql database - returns true or false.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        private bool isExistUsername(String username)
        {
            string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDBFilename=D:\projects\Server_For_Projuct22\Server_For_Projuct22\DATABASE1.MDF;Integrated Security=True";

            SqlConnection connection = new SqlConnection(connectionString);

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = connection;
            cmd.CommandText = "SELECT COUNT(*) FROM UsersDetails WHERE username='" + username + "'";

            connection.Open();
            int c = (int)cmd.ExecuteScalar();
            connection.Close();
            return c > 0;
        }
        /// <summary>
        /// Receives a string that represents a password and checks if the password exists in the sql database - returns true or false.
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        private bool isExistPassword(String password)
        {
            string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDBFilename=D:\projects\Server_For_Projuct22\Server_For_Projuct22\DATABASE1.MDF;Integrated Security=True";

            SqlConnection connection = new SqlConnection(connectionString);

            SqlCommand cmd = new SqlCommand();

            cmd.Connection = connection;
            cmd.CommandText = "SELECT COUNT(*) FROM UsersDetails WHERE password='" + CreateMD5Hash(password) + "'";

            connection.Open();
            int c = (int)cmd.ExecuteScalar();
            connection.Close();
            return c > 0;
        }
        /// <summary>
        /// Receives a string and sends it to all the clients that are connected to the server.
        /// </summary>
        /// <param name="message"></param>
        private bool isExistEmail(String email)
        {
            string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDBFilename=D:\projects\Server_For_Projuct22\Server_For_Projuct22\DATABASE1.MDF;Integrated Security=True";

        SqlConnection connection = new SqlConnection(connectionString);

        SqlCommand cmd = new SqlCommand();

        cmd.Connection = connection;
            cmd.CommandText = "SELECT COUNT(*) FROM UsersDetails WHERE email='" + email + "'";

            connection.Open();
            int c = (int)cmd.ExecuteScalar();
        connection.Close();
            return c > 0;
        }
}
    public class Emailer
    {
        private string smtpServer;
        private int smtpPort;
        private int code;
        public Emailer(string server, int port)
        {
            smtpServer = server;
            smtpPort = port;
            Random rand = new Random();
            code = rand.Next(11111, 99999);
        }
        /// <summary>
        /// Returns the passcode
        /// </summary>
        /// <returns></returns>
        public int getPasscode()
        {
            return this.code;
        }

        /// <summary>
        /// Sends and email with the passcode to the required email
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public int SendEmail(string email)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com", 25);
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.EnableSsl = true;
            //olxtxpzmaacefmrh
            //uafhlbdjikglfyfr
            smtpClient.Credentials = new NetworkCredential("cybergeemail@gmail.com", "olxtxpzmaacefmrh");
            var message = new System.Net.Mail.MailMessage("cybergeemail@gmail.com", email, "Account verification password", "Your password is: " + this.code);
            smtpClient.Send(message);
            return this.code;
        }
    }
    internal class RSAServiceProvider
    {
        private string PrivateKey;
        private string PublicKey;
        private UnicodeEncoding Encoder;
        private RSACryptoServiceProvider RSA;

        public RSAServiceProvider()
        {
            Encoder = new UnicodeEncoding();
            RSA = new RSACryptoServiceProvider();

            PrivateKey = RSA.ToXmlString(true);
            PublicKey = RSA.ToXmlString(false);
        }
        /// <summary>
        /// return PrivateKey
        /// </summary>
        /// <returns>PrivateKey</returns>
        public string GetPrivateKey()
        {
            return this.PrivateKey;
        }
        /// <summary>
        /// return PublicKey
        /// </summary>
        /// <returns>PublicKey</returns>
        public string GetPublicKey()
        {
            return this.PublicKey;
        }
        /// <summary>
        /// decript data by privateKey
        /// </summary>
        /// <param name="Data">data to decript</param>
        /// /// <param name="PrivateKey">privateKey</param>
        /// <returns>decripted data</returns>
        public string Decrypt(string Data, string PrivateKey)
        {

            var DataArray = Data.Split(new char[] { ',' });
            byte[] DataByte = new byte[DataArray.Length];
            for (int i = 0; i < DataArray.Length; i++)
            {
                DataByte[i] = Convert.ToByte(DataArray[i]);
            }

            RSA.FromXmlString(PrivateKey);
            var DecryptedByte = RSA.Decrypt(DataByte, false);
            return Encoder.GetString(DecryptedByte);
        }
        /// <summary>
        /// Encrypt the data by public key
        /// </summary>
        /// <param name="Data">data to encrypt</param>
        /// <param name="PublicKey"></param>
        /// <returns>encripted data</returns>
        public string Encrypt(string Data, string PublicKey)
        {
            var Rsa = new RSACryptoServiceProvider();
            Rsa.FromXmlString(PublicKey);
            var DataToEncrypt = Encoder.GetBytes(Data);
            var EncryptedByteArray = Rsa.Encrypt(DataToEncrypt, false);
            var Length = EncryptedByteArray.Length;
            var Item = 0;
            var StringBuilder = new StringBuilder();
            foreach (var EncryptedByte in EncryptedByteArray)
            {
                Item++;
                StringBuilder.Append(EncryptedByte);

                if (Item < Length)
                    StringBuilder.Append(",");
            }

            return StringBuilder.ToString();
        }
    }
    internal class AESServiceProvider
    {
        public static string Encrypt(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            string encrypted;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }

            // Return the encrypted string from the memory stream.
            return encrypted;
        }
        public static byte[] EncryptToBytes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            // Return the encrypted string from the memory stream.
            return encrypted;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cipherText"></param>
        /// <param name="Key"></param>
        /// <param name="IV"></param>
        /// <returns>Decrypt String</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string DecryptFromByte(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream((Stream)msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader((Stream)csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }

        public static string Decrypt(string cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;
            byte[] buffer = Convert.FromBase64String(cipherText);
            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(buffer))
                {
                    using (CryptoStream csDecrypt = new CryptoStream((Stream)msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader((Stream)csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }
    }
    public class Node
    {
        private bool full = false;
        private int players = 0;
        private string[]arr;
        private Node next;
        public Node(string[] arr)
        {
            players = arrCount(arr) ;
            this.arr = arr;
            this.next = null;
            if(players==7)
                full= true;
        }
        public Node(string[] arr, Node next)
        {
            this.arr = arr;
            this.next = next;
        }
        public string[] getValue() { return this.arr; }
        public void setValue(string[] arr) 
        { 
            this.arr = arr; 
            players = arrCount(this.arr);
            if (players == 7)
                full = true;
        }
        public Node getNext() { return this.next; }
        public void setNext(Node next) { this.next = next; }
        public bool hasNext() { return this.next != null; }
        public String toString() { return this.arr + " " + this.next; }
        public static int arrCount(string[] a)
        {
            int sum = 0;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != null)
                    sum++;
            return sum;
        }
    }
}
