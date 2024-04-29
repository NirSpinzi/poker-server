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
using System.Threading;
using System.Security.Policy;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Reflection.Emit;
using System.Reflection;
using System.Text.RegularExpressions;
using Server_For_Projuct22;
using System.Security.Permissions;

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
        public static Node Lobbys = new Node(new Client[7]);
        private Node ThisLobby;
        private int TableSitNum;
        private string[] Cards = new string[2];
        private int money = 1000000;
        private int PlacedBet = 0;
        private bool IsMyTurn = false;
        private bool isHost = false;
        private bool IsFolded = false;
        private int Strikes = 0;
        private int Warnings = 0;
        private int InGameStrikes = 0;
        private Timer Timeout = new Timer(1000);
        private Timer InGameTimer = new Timer(1000);
        private int counter;
        private bool IsTimedOut = false;
        private static List<string> LoggedUsers = new List<string>();
        private bool MarkedToLeave = false;
        private bool isInGame = false;
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
        private string _clientNick;
        private static string SQLFilePath = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDBFilename=D:\projects\poker-server\Server_For_Projuct22\DATABASE1.MDF;Integrated Security=True";
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
            Timeout.Elapsed += Timeout_Tick;
            InGameTimer.Elapsed += TurnTimer;
        }
        /// <summary>
        /// Receives a messege from the client and acts in accordance to the messege that was received.
        /// </summary>
        /// <param name="ar"></param>
        public string getClientNick() { return _clientNick; }
        public bool isMarkedToLeave()
        {
            return MarkedToLeave;
        }
        public int getMoney() { return money; }
        public bool isUserLoggedIn(string nickName)
        {
            foreach(string username in LoggedUsers)
                if(username == nickName) return true;
            return false;
        }
        public void addMoney(int Num)
        {
            money += Num;
        }
        public void reduceMoney(int Num)
        {
            if (money < Num)
                money = 0;
            else
                money -= Num;
        }
        public void setPlacedBet(int Num)
        {
            PlacedBet = Num;
        }
        public void TurnTimer(object sender, EventArgs e)
        {
            /*counter--;
            SendMessage("in_game_timer:" + counter);
            Console.WriteLine("sent:in_game_timer:" + counter);
            if (counter == 0)
            {
                InGameStrikes++;
                if (InGameStrikes == 1)
                {
                    SendMessage("kick_next_time");
                    IsFolded = true;
                    IsMyTurn = false;
                    Thread.Sleep(50);
                    SendMessage("fold:" + TableSitNum + ":" + _clientNick, ThisLobby);
                    Console.WriteLine("sent:fold:" + TableSitNum + ":" + _clientNick);
                }
                else if (InGameStrikes == 2)
                {
                    //kick him
                    InGameStrikes = 0;
                }
                InGameTimer.Stop();
            }*/
        }
        public void TurnToNextPlayer(int TableSitNum)
        {
            if (TableSitNum != 6)
            {
                int i = 1;
                while ((TableSitNum + i) != 7 && ThisLobby.getArrayOfClients()[TableSitNum + i] != null)
                {
                    if (ThisLobby.getArrayOfClients()[TableSitNum + i].IsFolded)
                        i++;
                    else
                    {
                        ThisLobby.getArrayOfClients()[TableSitNum + i].IsMyTurn = true;
                        ThisLobby.getArrayOfClients()[TableSitNum + i].SendMessage("turn:");
                        counter = 30;
                        ThisLobby.getArrayOfClients()[TableSitNum + i].InGameTimer.Start();
                        break;
                    }
                }
                if ((TableSitNum + i) == 7)
                    roundEnd();
                else if (ThisLobby.getArrayOfClients()[TableSitNum + i] == null || ThisLobby.getArrayOfClients()[TableSitNum] == null)
                {
                    TurnToNextPlayer(TableSitNum + 1);
                }
            }
            else roundEnd();
        }
        public bool CheckIfAllAreFolded()
        {
            int VacantSits = 0;
            int FoldedPlayers = 0;
            foreach(Client client in ThisLobby.getArrayOfClients())
            {
                if (client == null)
                    VacantSits++;
                else if (client.IsFolded)
                    FoldedPlayers++;
            }
            if ((7 - VacantSits - FoldedPlayers) == 1)
                return true;
            return false;
        }
        public void NewGame()
        {
            ThisLobby.resetTableTotalMoney();
            ThisLobby.resetRounds();
            ThisLobby.resetTableCards();
            ThisLobby.resetClientBets();
            ThisLobby.setTableBetAmount(5000);
            ThisLobby.GameOngoing();
        }
        public void roundEnd()
        {
            if (ThisLobby.getRoundNum() == 4)
            {
                for (int i = 0; i < 7; i++)
                {
                    if (ThisLobby.getArrayOfClients()[i] != null && !ThisLobby.getArrayOfClients()[i].IsFolded)
                        ThisLobby.setPlayerHandStrength(PokerHandEvaluator.EvaluateHand(ThisLobby.getArrayOfClients()[i].getCards()
                            , ThisLobby.getArrayOfTableCards()), i);
                }
                int[] SitNumOfWinnerArray = PokerHandEvaluator.FindStrongestHand(ThisLobby.getPlayerHandStrengths());
                switch (SitNumOfWinnerArray.Length) 
                {
                    case 1:
                        {
                            ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[0]].addMoney(ThisLobby.getTableTotalMoney());
                            ThisLobby.GameNotOngoing();
                            ThisLobby.kickAllMarkedToLeave();
                            Thread.Sleep(50);
                            RevealAllCards();
                            Thread.Sleep(50);
                            SendMessage("winner:" + SitNumOfWinnerArray[0] + ":" + ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[0]]._clientNick + ":" +
                            ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[0]].money, ThisLobby);
                            Console.WriteLine("sent:winner:" + SitNumOfWinnerArray[0] + ":" + ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[0]]._clientNick + ":" +
                            ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[0]].money);
                            break;
                        }
                    case 2:
                        {
                            foreach(Client client in ThisLobby.getArrayOfClients())
                                if(client != null)
                                    if(client.TableSitNum == SitNumOfWinnerArray[0] || client.TableSitNum == SitNumOfWinnerArray[1])
                                        client.addMoney(ThisLobby.getTableTotalMoney() / 2);
                            ThisLobby.GameNotOngoing();
                            ThisLobby.kickAllMarkedToLeave();
                            Thread.Sleep(50);
                            RevealAllCards();
                            Thread.Sleep(50);
                            SendMessage("winners:2:" + SitNumOfWinnerArray[0] + ":" + ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[0]]._clientNick + ":" +
                            ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[0]].money + ":" + SitNumOfWinnerArray[1] + ":" + ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[1]]._clientNick + ":" +
                            ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[1]].money, ThisLobby);
                            Console.WriteLine("sent:winners:2:" + SitNumOfWinnerArray[0] + ":" + ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[0]]._clientNick + ":" +
                            ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[0]].money + ":" + SitNumOfWinnerArray[1] + ":" + ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[1]]._clientNick + ":" +
                            ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[1]].money);
                            break;
                        }
                    case 3:
                        {
                            foreach (Client client in ThisLobby.getArrayOfClients())
                                if (client != null)
                                    if (client.TableSitNum == SitNumOfWinnerArray[0] || client.TableSitNum == SitNumOfWinnerArray[1] || client.TableSitNum == SitNumOfWinnerArray[2])
                                        client.addMoney(ThisLobby.getTableTotalMoney() / 3);
                            ThisLobby.GameNotOngoing();
                            ThisLobby.kickAllMarkedToLeave();
                            Thread.Sleep(50);
                            RevealAllCards();
                            Thread.Sleep(50);
                            SendMessage("winners:3:" + SitNumOfWinnerArray[0] + ":" + ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[0]]._clientNick + ":" +
                            ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[0]].money + ":" + SitNumOfWinnerArray[1] + ":" + ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[1]]._clientNick + ":" +
                            ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[1]].money + ":" + SitNumOfWinnerArray[2] + ":" + ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[2]]._clientNick + ":" +
                            ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[2]].money, ThisLobby);
                            Console.WriteLine("sent:winners:3:" + SitNumOfWinnerArray[0] + ":" + ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[0]]._clientNick + ":" +
                            ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[0]].money + ":" + SitNumOfWinnerArray[1] + ":" + ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[1]]._clientNick + ":" +
                            ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[1]].money + ":" + SitNumOfWinnerArray[2] + ":" + ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[2]]._clientNick + ":" +
                            ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[2]].money);
                            break;
                        }
                    case 4:
                        {
                            foreach (Client client in ThisLobby.getArrayOfClients())
                                if (client != null)
                                    if (client.TableSitNum == SitNumOfWinnerArray[0] || client.TableSitNum == SitNumOfWinnerArray[1] || client.TableSitNum == SitNumOfWinnerArray[2] || client.TableSitNum == SitNumOfWinnerArray[3])
                                        client.addMoney(ThisLobby.getTableTotalMoney() / 4);
                            ThisLobby.GameNotOngoing();
                            ThisLobby.kickAllMarkedToLeave();
                            Thread.Sleep(50);
                            RevealAllCards();
                            Thread.Sleep(50);
                            SendMessage("winners:3:" + SitNumOfWinnerArray[0] + ":" + ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[0]]._clientNick + ":" +
                            ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[0]].money + ":" + SitNumOfWinnerArray[1] + ":" + ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[1]]._clientNick + ":" +
                            ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[1]].money + ":" + SitNumOfWinnerArray[2] + ":" + ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[2]]._clientNick + ":" +
                            ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[2]].money + ":" + SitNumOfWinnerArray[3] + ":" + ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[3]]._clientNick + ":" +
                            ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[3]].money, ThisLobby);
                            Console.WriteLine("sent:winners:3:" + SitNumOfWinnerArray[0] + ":" + ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[0]]._clientNick + ":" +
                            ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[0]].money + ":" + SitNumOfWinnerArray[1] + ":" + ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[1]]._clientNick + ":" +
                            ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[1]].money + ":" + SitNumOfWinnerArray[2] + ":" + ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[2]]._clientNick + ":" +
                            ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[2]].money + ":" + SitNumOfWinnerArray[3] + ":" + ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[3]]._clientNick + ":" +
                            ThisLobby.getArrayOfClients()[SitNumOfWinnerArray[3]].money);
                            break;
                        }
                }
            }
            else
            {
                switch (ThisLobby.getRoundNum()) 
                {
                    case 1:
                        {
                            SendMessage("round:1:" + ThisLobby.getArrayOfTableCards()[0] + ":" + ThisLobby.getArrayOfTableCards()[1] +
                            ":" + ThisLobby.getArrayOfTableCards()[2], ThisLobby);
                            ThisLobby.nextRound();
                            break;
                        }
                    case 2:
                        {
                            SendMessage("round:2:" + ThisLobby.getArrayOfTableCards()[3], ThisLobby);
                            ThisLobby.nextRound();
                            break;
                        }
                    case 3:
                        {
                            SendMessage("round:3:" + ThisLobby.getArrayOfTableCards()[4], ThisLobby);
                            ThisLobby.nextRound();
                            break;
                        }
                }
                if (!ThisLobby.getArrayOfClients()[0].IsFolded)
                {
                    ThisLobby.getArrayOfClients()[0].IsMyTurn = true;
                    ThisLobby.getArrayOfClients()[0].SendMessage("turn:");
                }
                else TurnToNextPlayer(0);
            }
        }
        public void RevealAllCards()
        {
            foreach(Client client in ThisLobby.getArrayOfClients())
            {
                if (client != null)
                {
                    SendMessage("reveal:" + client.TableSitNum + ":" + client.Cards[0] + ":" + client.Cards[1] + ":" + client._clientNick, ThisLobby);
                    Console.WriteLine("sent:reveal:" + client.TableSitNum + ":" + client.Cards[0] + ":" + client.Cards[1] + ":" + client._clientNick);
                    Thread.Sleep(50);
                }
            }
        }
        public void TimeoutPlayer()
        {
            switch (Warnings)
            {
                case 1:
                    {
                        counter = 31;
                        IsTimedOut= true;
                        Timeout.Start();
                        break;
                    }
                case 2:
                    {
                        counter = 120;
                        IsTimedOut = true;
                        Timeout.Start();
                        break;
                    }
                case 3:
                    {
                        counter = 1800;
                        IsTimedOut = true;
                        Timeout.Start();
                        break;
                    }
                case 4:
                    {
                        counter = 7200;
                        IsTimedOut = true;
                        Timeout.Start();
                        break;
                    }
                default:
                    {
                        counter = 86400;
                        IsTimedOut = true;
                        Timeout.Start();
                        break;
                    }
            }
        }
        private void Timeout_Tick(object sender, EventArgs e)
        {
            counter--;
            SendMessage("timeout:" + counter);
            if (counter == 0)
            {
                IsTimedOut= false;
                Timeout.Stop();
            }
        }
        /// <summary>
        /// Generates a random key of the specified length.
        /// </summary>
        /// <param name="Length">The length of the random key to generate.</param>
        /// <returns>A string representing the random key.</returns>
        public static string RandomKey(int Length)
        {
            // Characters to use for generating the random key
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            // Generate the random key using LINQ and Enumerable.Repeat
            return new string(Enumerable.Repeat(chars, Length)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        public void setCards(string card,int index)
        {
            Cards[index] = card;
        }
        public string[] getCards()
        {
            return Cards;
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
                            if (IsUsernameValid(parts[1]) && IsPasswordValid(parts[2]))
                            {
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
                                else if (!IsValidEmail(parts[3]))
                                {
                                    SendMessage("regist:email_not_valid:");
                                    Console.WriteLine("sent:regist:email_not_valid");
                                }
                                else
                                {
                                    string connectionString = SQLFilePath;
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
                            {
                                SendMessage("regist:username_or_password_are_not_valid");
                            }
                        }
                        else
                        if (messageReceived.StartsWith("login:") && !IsTimedOut)
                        {
                            string[] parts = messageReceived.Split(':');
                            string connectionString = SQLFilePath;
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
                                    if (!isUserLoggedIn(parts[1]))
                                    {
                                        SendMessage("login:ok:");
                                        Console.WriteLine("sent: login ok");
                                        _clientNick = parts[1];
                                        LoggedUsers.Add(_clientNick);
                                    }
                                    else SendMessage("login:user_already_logged_in");
                                }
                                else
                                {
                                    Strikes++;
                                    if (Strikes % 3 == 0)
                                    {
                                        Warnings++;
                                        TimeoutPlayer();
                                    }
                                    SendMessage("login:Not_ok:");
                                    Console.WriteLine("sent: login not ok");
                                }
                            }
                            else
                            {
                                Strikes++;
                                if (Strikes % 3 == 0)
                                {
                                    Warnings++;
                                    TimeoutPlayer();
                                }
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
                                string connectionString = SQLFilePath;
                                if (IsPasswordValid(parts[2]))
                                {
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
                                else
                                {
                                    SendMessage("reset:password_not_valid");
                                    Console.WriteLine("sent:reset:password_not_valid");
                                }
                            }
                            else SendMessage("reset:email:notexist");
                        }
                        else if (messageReceived.StartsWith("game"))
                        {
                            string[] parts = messageReceived.Split(':');
                            if (parts[1].Equals("host") && !isHost)
                            {
                                Node temp = Lobbys;
                                Client[] lobby = new Client[7];
                                lobby[0] = this;
                                while (temp != null)
                                {
                                    if (temp.getArrayOfClients()[0] == null)
                                    {
                                        isHost = true;
                                        isInGame = true;
                                        temp.setValue(lobby);
                                        ThisLobby = temp;
                                        TableSitNum = 0;
                                        SendMessage("game:hosted:" + _clientNick + ":" + money);
                                        break;
                                    }
                                    else if (temp.getNext() == null)
                                    {
                                        isHost = true;
                                        isInGame = true;
                                        temp.setNext(new Node(lobby));
                                        ThisLobby = temp;
                                        TableSitNum = 0;
                                        SendMessage("game:hosted:" + _clientNick + ":" + money);
                                        break;
                                    }
                                    else temp = temp.getNext();
                                }
                            }
                            else if (parts[1].Equals("start"))
                            {
                                if (isHost)
                                {
                                    Node temp = Lobbys;
                                    while (temp != null)
                                    {
                                        if (temp.getArrayOfClients()[0] == this && temp.getArrayOfTableCards()[0] == null)
                                        {
                                            if (temp.getArrayOfClients()[1] != null)
                                            {
                                                GenerateCards(temp);
                                                foreach (Client client in temp.getArrayOfClients())
                                                {
                                                    if (client != null)
                                                    {
                                                        client.SendMessage("game:start:ok:" + client.getCards()[0] + ":" + client.getCards()[1]);
                                                        Console.WriteLine("sent:game:start:ok:" + client.getCards()[0] + ":" + client.getCards()[1]);
                                                    }
                                                }
                                                ThisLobby.GameOngoing();
                                                temp.getArrayOfClients()[0].IsMyTurn = true;
                                                temp.getArrayOfClients()[0].SendMessage("turn:");
                                                counter = 30;
                                                temp.getArrayOfClients()[0].InGameTimer.Start();
                                            }
                                            else
                                            {
                                                SendMessage("game:start:not_enough_players");
                                                Console.WriteLine("sent:game:start:not_enough_players");
                                            }
                                        }
                                        temp = temp.getNext();
                                    }
                                }
                            }
                            else if (parts[1].Equals("join"))
                            {
                                Node temp = Lobbys;
                                while (temp != null)
                                {
                                    if (temp.getArrayOfClients()[0] == null || temp.isGameOngoin()) 
                                    {
                                        SendMessage("join:invalid");
                                        Console.WriteLine("sent:join:invalid");
                                        break;
                                    }
                                    for(int i=1;i<7;i++)
                                    {
                                        if (temp.getArrayOfClients()[i] == null)
                                        {
                                            temp.setValue(this,i);
                                            isInGame = true;
                                            ThisLobby = temp;
                                            TableSitNum = i;
                                            SendMessage("join:valid:" + money);
                                            Console.WriteLine("sent:join:valid:" + money);
                                            Thread.Sleep(600);
                                            SendMessage("join:" + (i + 1) + ":" + _clientNick + ":" + money, temp);
                                            Console.WriteLine("sent:join:" + (i + 1) + _clientNick + ":" + money);
                                            temp = null;
                                            break;
                                        }
                                        if (i == 6 && temp.getArrayOfClients()[i] != null)
                                        {
                                            temp = temp.getNext();
                                            if (temp == null)
                                            {
                                                SendMessage("join:invalid");
                                                Console.WriteLine("sent:join:invalid");
                                            }
                                        }
                                    }
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
                        else if (messageReceived.StartsWith("table"))
                        {
                            string[] parts = messageReceived.Split(':');
                            if (parts[1].Equals("names"))
                            {
                                Node temp = Lobbys;
                                while (temp != null)
                                {
                                    for (int i = 1; i < 7; i++)
                                    {
                                        if (temp.getArrayOfClients()[i] != null && temp.getArrayOfClients()[i] == this)
                                        {
                                            SendMessage("table_names:" + parts[2] + ":" + temp.getArrayOfClients()[0].getClientNick() + ":" + temp.getArrayOfClients()[0].getMoney());
                                            Console.WriteLine("sent:table_names:" + parts[2] + ":" + temp.getArrayOfClients()[0].getClientNick() + ":" + temp.getArrayOfClients()[0].getMoney());
                                            for (int j = 1; j < 7; j++)
                                            {
                                                if (temp.getArrayOfClients()[j] != null && temp.getArrayOfClients()[j] != this)
                                                {
                                                    SendMessage("table_names:" + (j + 1) + ":" + temp.getArrayOfClients()[j].getClientNick() + ":" + temp.getArrayOfClients()[j].getMoney());
                                                    Console.WriteLine("sent:table_names:" + (j + 1) + ":" + temp.getArrayOfClients()[j].getClientNick() + ":" + temp.getArrayOfClients()[j].getMoney());
                                                }
                                            }
                                            temp = null;
                                            break;
                                        }
                                        if (i == 6)
                                            temp = temp.getNext();
                                    }
                                }
                            }
                            else if (parts[1].Equals("bets"))
                            {
                                if (ThisLobby.getArrayOfClients()[0].PlacedBet < ThisLobby.getTableBetAmount())
                                {
                                    SendMessage("call:" + 0 + ":" + 0 + ":" + TableSitNum + ":" + parts[2] + ":" + ThisLobby.getArrayOfClients()[0].PlacedBet);
                                    Console.WriteLine("sent:call:" + 0 + ":" + 0 + ":" + TableSitNum + ":" + parts[2] + ":" + ThisLobby.getArrayOfClients()[0].PlacedBet);
                                }
                                else
                                {
                                    SendMessage("call:" + (ThisLobby.getTableBetAmount() - ThisLobby.getArrayOfClients()[0].PlacedBet) + ":"
                                    + parts[3] + ":" + TableSitNum + ":" + parts[2] + ":" + ThisLobby.getArrayOfClients()[0].PlacedBet);
                                    Console.WriteLine("sent:call:" + (ThisLobby.getTableBetAmount() - ThisLobby.getArrayOfClients()[0].PlacedBet) + ":"
                                    + parts[3] + ":" + TableSitNum + ":" + parts[2] + ":" + ThisLobby.getArrayOfClients()[0].PlacedBet);
                                }
                            }
                            else if (parts[1].Equals("folds"))
                            {
                                SendMessage("fold:" + TableSitNum + ":" + parts[2]);
                                Console.WriteLine("sent:fold:" + TableSitNum + ":" + parts[2]);
                            }
                            else if (parts[1].Equals("raise"))
                            {
                                SendMessage("raise:" + parts[2] + ":" + TableSitNum + ":" + parts[3] + ":" + ThisLobby.getArrayOfClients()[0].money);
                                Console.WriteLine("sent:raise:" + parts[2] + ":" + TableSitNum + ":" + parts[3] + ":" + ThisLobby.getArrayOfClients()[0].money);
                            }
                            else if (parts[1].Equals("update_money"))
                            {
                                SendMessage("winner:" + TableSitNum + ":" + ThisLobby.getArrayOfClients()[0]._clientNick + ":" + ThisLobby.getArrayOfClients()[0].money);
                            }
                            else if (parts[1].Equals("reveal"))
                            {
                                SendMessage("reveal:" + TableSitNum + ":" + ThisLobby.getArrayOfClients()[0].Cards[0] + ":"
                                    + ThisLobby.getArrayOfClients()[0].Cards[1] + ":" + ThisLobby.getArrayOfClients()[0]._clientNick);
                            }
                            else if (parts[1].Equals("leave"))
                            {
                                SendMessage("leave:" + TableSitNum);
                            }
                        }
                        else if (messageReceived.StartsWith("call"))
                        {
                            if (IsMyTurn && !IsFolded)
                            {
                                IsMyTurn = false;
                                if ((ThisLobby.getTableBetAmount() - PlacedBet) > money)
                                {
                                    ThisLobby.addTableTotalMoney(money);
                                    SendMessage("call:" + money + ":" + money + ":" + TableSitNum + ":" + _clientNick + ":" + PlacedBet, ThisLobby);
                                    Console.WriteLine("sent:call:" + money + ":" + money + ":" + TableSitNum + ":" + _clientNick + ":" + PlacedBet);
                                    PlacedBet += money;
                                    reduceMoney(money);
                                }
                                else
                                {
                                    ThisLobby.addTableTotalMoney(ThisLobby.getTableBetAmount() - PlacedBet);
                                    SendMessage("call:" + (ThisLobby.getTableBetAmount() - PlacedBet) + ":" + money + ":" + TableSitNum + ":" + _clientNick
                                        + ":" + PlacedBet, ThisLobby);
                                    Console.WriteLine("sent:call:" + (ThisLobby.getTableBetAmount() - PlacedBet) + ":" + money + ":" + TableSitNum + ":"
                                        + _clientNick + ":" + PlacedBet);
                                    reduceMoney(ThisLobby.getTableBetAmount() - PlacedBet);
                                    PlacedBet = ThisLobby.getTableBetAmount();
                                }
                                TurnToNextPlayer(TableSitNum);
                            }
                        }
                        else if (messageReceived.StartsWith("fold"))
                        {
                            if(IsMyTurn && !IsFolded)
                            {
                                IsMyTurn= false;
                                IsFolded = true;
                                SendMessage("fold:" + TableSitNum + ":" + _clientNick, ThisLobby);
                                Console.WriteLine("sent:fold:" + TableSitNum + ":" + _clientNick);
                                if (CheckIfAllAreFolded())
                                {
                                    ThisLobby.setRoundNum(4);
                                    roundEnd();
                                }
                                else
                                    TurnToNextPlayer(TableSitNum);
                            }
                        }
                        else if (messageReceived.StartsWith("raise"))
                        {
                            if (IsMyTurn && !IsFolded)
                            {
                                IsMyTurn = false;
                                string[] parts = messageReceived.Split(':');
                                if (parts[1].Equals(""))
                                    SendMessage("raise:nothing_inserted");
                                try
                                {
                                    int raiseNum = int.Parse(parts[1]);
                                    if (raiseNum > money)
                                        SendMessage("raise:not_enough_money");
                                    else
                                    {
                                        if (raiseNum > 5000 || (raiseNum < 5000 && raiseNum == money))
                                        {
                                            reduceMoney(raiseNum);
                                            SendMessage("raise:" + (PlacedBet + raiseNum) + ":" + TableSitNum + ":" + _clientNick + ":" + money, ThisLobby);
                                            Console.WriteLine("sent:raise:" + (PlacedBet + raiseNum) + ":" + TableSitNum + ":" + _clientNick + ":" + money);
                                            ThisLobby.setTableBetAmount(PlacedBet + raiseNum);
                                            ThisLobby.addTableTotalMoney(raiseNum);
                                            PlacedBet += raiseNum;
                                            Thread.Sleep(800);
                                            if (TableSitNum != 0)
                                                TurnToNextPlayer(-1);
                                            else TurnToNextPlayer(0);
                                        }
                                        else SendMessage("raise:min_raise_is_5000");
                                    }
                                }
                                catch (Exception)
                                {
                                    SendMessage("raise:not_a_number");
                                }
                            }
                        }
                        else if (messageReceived.StartsWith("leave"))
                        {
                            if(!ThisLobby.isGameOngoin())
                                ClientLeave();
                        }
                        else if (messageReceived.StartsWith("new_game"))
                        {
                            if (isHost)
                            {
                                NewGame();
                                if (ThisLobby.getArrayOfClients()[1] != null)
                                {
                                    GenerateCards(ThisLobby);
                                    foreach (Client client in ThisLobby.getArrayOfClients())
                                    {
                                        if (client != null)
                                        {
                                             client.SendMessage("game:play_again:" + client.getCards()[0] + ":" + client.getCards()[1]);
                                             Console.WriteLine("sent:game:play_again:" + client.getCards()[0] + ":" + client.getCards()[1]);
                                        }
                                    }
                                    ThisLobby.GameOngoing();
                                    ThisLobby.getArrayOfClients()[0].IsMyTurn = true;
                                    ThisLobby.getArrayOfClients()[0].SendMessage("turn:");
                                    counter = 30;
                                    ThisLobby.getArrayOfClients()[0].InGameTimer.Start();
                                }
                                else
                                {
                                    SendMessage("game:start:not_enough_players");
                                    Console.WriteLine("sent:game:start:not_enough_players");
                                }
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
                if (isInGame)
                {
                    IsFolded= true;
                    MarkedToLeave = true;
                    ThisLobby.getArrayOfClients()[TableSitNum] = null;
                    ClientLeave();
                    if (CheckIfAllAreFolded())
                    {
                        ThisLobby.setRoundNum(4);
                        roundEnd();
                    }
                }
                LoggedUsers.Remove(_clientNick);
                AllClients.Remove(_clientIP);
            }
        }//end ReceiveMessage
        /// <summary>
        /// allow the server to send messages to the client.
        /// </summary>
        /// <param name="message"></param>
        public void ClientLeave()
        {
            if (!ThisLobby.isGameOngoin())
            {
                int tableSitNum = TableSitNum;
                isHost = false;
                isInGame = false;
                ThisLobby.getArrayOfClients()[tableSitNum] = null;
                SendMessage("leave:" + tableSitNum, ThisLobby);
                Console.WriteLine("sent:leave:" + TableSitNum);
                int i;
                for (i = 0; i < 7 - tableSitNum; i++)
                {
                    if (tableSitNum + i + 1 != 7)
                    {
                        if (ThisLobby.getArrayOfClients()[tableSitNum + i + 1] != null)
                        {
                            Thread.Sleep(100);
                            ThisLobby.getArrayOfClients()[tableSitNum + i] = ThisLobby.getArrayOfClients()[tableSitNum + i + 1];
                            ThisLobby.getArrayOfClients()[tableSitNum + i].TableSitNum = tableSitNum + i;
                            if (tableSitNum + i == 0)
                            {
                                ThisLobby.getArrayOfClients()[tableSitNum + i].isHost = true;
                                ThisLobby.getArrayOfClients()[tableSitNum + i].SendMessage("Host:true");
                                Console.WriteLine("sent:Host:true");
                            }
                            ThisLobby.getArrayOfClients()[tableSitNum + i + 1] = null;
                            SendMessage("switch:" + (tableSitNum + i), ThisLobby);
                            Console.WriteLine("sent:switch:" + (tableSitNum + i));
                            Thread.Sleep(200);
                        }
                    }
                    else break;
                }
            }
            else MarkedToLeave = true;
        }
        /// <summary>
        /// Creates an MD5 hash from the input string, typically used for encrypting passwords.
        /// </summary>
        /// <param name="Input">The input string to hash.</param>
        /// <returns>A string representing the MD5 hash of the input.</returns>
        public static string CreateMD5Hash(string Input)
        {
            // Step 1, calculate MD5 hash from input
            System.Security.Cryptography.MD5 Md5 = System.Security.Cryptography.MD5.Create();
            byte[] InputBytes = System.Text.Encoding.ASCII.GetBytes(Input);
            byte[] HashBytes = Md5.ComputeHash(InputBytes);

            // Step 2, convert byte array to hex string
            StringBuilder StringBuilder = new StringBuilder();
            for (int i = 0; i < HashBytes.Length; i++)
            {
                StringBuilder.Append(HashBytes[i].ToString("X2"));
            }
            return StringBuilder.ToString();
        }

        /// <summary>
        /// Receives a string and sends it to all the clients that are connected to the lobby.
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(string message, Node Lobby)
        {
            foreach (Client client in Lobby.getArrayOfClients())
            {
                if (client != null)
                {
                    client.SendMessage(message);
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
        private void GenerateCards(Node lobby)
        {
            string[] BannedCards = new string[19];
            string x;
            foreach(Client client in lobby.getArrayOfClients())
            {
                if (client != null)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        x = getRandomCard(lobby.getArrayOfCards(), BannedCards);
                        client.setCards(x, i);
                        for (int j = 0; j < BannedCards.Length; j++)
                        {
                            if (BannedCards[j] == null)
                            {
                                BannedCards[j] = x;
                                break;
                            }
                        }
                    }
                }
            }
            for(int i = 0; i < 5; i++)
            {
                x = getRandomCard(lobby.getArrayOfCards(), BannedCards);
                lobby.setTableCard(x);
                for (int j = 0; j < BannedCards.Length; j++)
                {
                    if (BannedCards[j] == null)
                    {
                        BannedCards[j] = x;
                        break;
                    }
                }
            }
        }
        private string getRandomCard(string[] CardArray, string[] BannedCards)
        {
            Random rnd = new Random();
            int x = rnd.Next(CardArray.Length);
                for (int i = 0; i < BannedCards.Length; i++)
                {
                    if (BannedCards[i] != null && CardArray[x].Equals(BannedCards[i]))
                    {
                        x = rnd.Next(CardArray.Length);
                        i = -1;
                    }
                }
            return CardArray[x];
        }
        /// <param name="username"></param>
        /// <returns></returns>
        private bool isExistUsername(String username)
        {
            string connectionString = SQLFilePath;
                SqlConnection connection = new SqlConnection(connectionString);

                SqlCommand cmd = new SqlCommand();

                cmd.Connection = connection;
                cmd.CommandText = "SELECT COUNT(*) FROM UsersDetails WHERE username='" + username + "'";

                connection.Open();
                int c = (int)cmd.ExecuteScalar();
                connection.Close();
                return c > 0;
        }
        static bool IsUsernameValid(string username)
        {
            // Check if the username is 3 letters or longer
            if (username.Length>2)
                // Check if the username does not contain parentheses or equal signs
                if (!Regex.IsMatch(username, "[()=]"))
                    return true;
            return false;
        }
        /// <summary>
        /// Receives a string that represents a password and checks if the password exists in the sql database - returns true or false.
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        private bool isExistPassword(String password)
        {
            string connectionString = SQLFilePath;
                SqlConnection connection = new SqlConnection(connectionString);

                SqlCommand cmd = new SqlCommand();

                cmd.Connection = connection;
                cmd.CommandText = "SELECT COUNT(*) FROM UsersDetails WHERE password='" + CreateMD5Hash(password) + "'";

                connection.Open();
                int c = (int)cmd.ExecuteScalar();
                connection.Close();
                return c > 0;
        }
        static bool IsPasswordValid(string password)
        {
            // Regular expression to check for at least one uppercase letter, one lowercase letter, and one digit
            string pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$";

            // Match the password against the pattern
            Match match = Regex.Match(password, pattern);
            if (match.Success)
                // Check if the username does not contain parentheses or equal signs
                if (!Regex.IsMatch(password, "[()=]"))
                    return true;
            return false;
        }
        private bool isExistEmail(String email)
        {
            string connectionString = SQLFilePath;
                SqlConnection connection = new SqlConnection(connectionString);

                SqlCommand cmd = new SqlCommand();

                cmd.Connection = connection;
                cmd.CommandText = "SELECT COUNT(*) FROM UsersDetails WHERE email='" + email + "'";

                connection.Open();
                int c = (int)cmd.ExecuteScalar();
                connection.Close();
                return c > 0;
        }
        private bool IsValidEmail(string email)
        {
            // Define a regular expression for email validation
            string pattern = @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$";

            Regex regex = new Regex(pattern);

            // Check if the email matches the regular expression
            return regex.IsMatch(email);
        }
    }
    /// <summary>
    /// Represents an email sender that uses SMTP to send emails.
    /// </summary>
    public class Emailer
    {
        // The SMTP server address.
        private string smtpServerIP;
        // The SMTP port number.
        private int smtpPort;
        // The passcode generated by the Emailer.
        private int code;
        /// <summary>
        /// Initializes a new instance of the Emailer class with the specified SMTP server and port.
        /// </summary>
        /// <param name="serverIP">The SMTP server address.</param>
        /// <param name="port">The SMTP port number.</param>
        public Emailer(string serverIP, int port)
        {
            smtpServerIP = serverIP;
            smtpPort = port;
            Random rand = new Random();
            code = rand.Next(11111, 99999);
        }
        /// <summary>
        /// Gets the passcode generated by the Emailer.
        /// </summary>
        /// <returns>The passcode.</returns>
        public int getPasscode()
        {
            return this.code;
        }
        /// <summary>
        /// Sends an email with the passcode to the specified email address.
        /// </summary>
        /// <param name="email">The recipient's email address.</param>
        /// <returns>The passcode sent in the email.</returns>
        public int SendEmail(string email)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com", 25);
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.EnableSsl = true;
            smtpClient.Credentials = new NetworkCredential("cybergeemail@gmail.com", "olxtxpzmaacefmrh");
            var message = new System.Net.Mail.MailMessage("cybergeemail@gmail.com", email, "Account verification password", "Your password is: " + this.code);
            smtpClient.Send(message);
            return this.code;
        }
    }
    /// <summary>
    /// Provides RSA encryption and decryption services.
    /// </summary>
    internal class RSAServiceProvider
    {
        // The private key for RSA encryption.
        private string PrivateKey;

        // The public key for RSA encryption.
        private string PublicKey;

        // The Unicode encoder for data conversion.
        private UnicodeEncoding Encoder;

        // The RSACryptoServiceProvider for RSA operations.
        private RSACryptoServiceProvider RSA;

        /// <summary>
        /// Initializes a new instance of the RSAServiceProvider class.
        /// </summary>
        public RSAServiceProvider()
        {
            Encoder = new UnicodeEncoding();
            RSA = new RSACryptoServiceProvider();

            PrivateKey = RSA.ToXmlString(true);
            PublicKey = RSA.ToXmlString(false);
        }

        /// <summary>
        /// Gets the private key for RSA encryption.
        /// </summary>
        /// <returns>The private key.</returns>
        public string GetPrivateKey()
        {
            return this.PrivateKey;
        }

        /// <summary>
        /// Gets the public key for RSA encryption.
        /// </summary>
        /// <returns>The public key.</returns>
        public string GetPublicKey()
        {
            return this.PublicKey;
        }
        /// <summary>
        /// Encrypts data using the public key.
        /// </summary>
        /// <param name="Data">The data to encrypt.</param>
        /// <param name="PublicKey">The public key.</param>
        /// <returns>The encrypted data.</returns>
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
    /// <summary>
    /// Provides AES encryption and decryption services.
    /// </summary>
    internal class AESServiceProvider
    {
        /// <summary>
        /// Encrypts a plain text using AES with the specified key and initialization vector (IV).
        /// </summary>
        /// <param name="plainText">The plain text to encrypt.</param>
        /// <param name="Key">The AES encryption key.</param>
        /// <param name="IV">The AES initialization vector (IV).</param>
        /// <returns>The encrypted text.</returns>
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
            // Create an Aes object with the specified key and IV.
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
        /// <summary>
        /// Decrypts a cipher text using AES with the specified key and initialization vector (IV).
        /// </summary>
        /// <param name="cipherText">The encrypted string to decrypt.</param>
        /// <param name="Key">The AES decryption key.</param>
        /// <param name="IV">The AES initialization vector (IV).</param>
        /// <returns>The decrypted string.</returns>
        public static string Decrypt(string cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            string plaintext = null;
            byte[] buffer = Convert.FromBase64String(cipherText);

            // Create an Aes object with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(buffer))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            // Read the decrypted bytes from the decrypting stream and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            return plaintext;
        }
    }
    class Node
    {
        // Array of clients.
        private Client[] ClientArray;
        // Array of cards.
        private string[] CardArray = {"diamond:2","diamond:3","diamond:4","diamond:5", "diamond:6", "diamond:7", "diamond:8"
        ,"diamond:9","diamond:10","diamond:11","diamond:12","diamond:13","diamond:14","club:2","club:3","club:4","club:5","club:6","club:7","club:8"
        ,"club:9","club:10","club:11","club:12","club:13","club:14","spade:2","spade:3","spade:4","spade:5","spade:6","spade:7","spade:8","spade:9"
        ,"spade:10","spade:11","spade:12","spade:13","spade:14","heart:2","heart:3","heart:4","heart:5","heart:6","heart:7","heart:8","heart:9","heart:10"
        ,"heart:11","heart:12","heart:13","heart:14"};
        // Array of cards on the table in the game.
        private string[] TableCards = new string[5];
        // Array of hand strengths for each player.
        private HandStrength[] PlayerHandStrengths = new HandStrength[7];
        // Amount of bet on the table.
        private int TableBetAmount = 5000;
        // Total money on the table.
        private static int TableTotalMoney = 0;
        // Round number in the game.
        private int RoundNum = 1;
        // Flag indicating if the game is ongoing.
        private bool isGameOngoing = false;
        // Reference to the next node in the linked list.
        private Node next;
        public Node(Client[] arr)
        {
            this.ClientArray = arr;
            this.next = null;
        }
        public Client[] getArrayOfClients() { return this.ClientArray; }
        public string[] getArrayOfCards() { return this.CardArray; }
        public string[] getArrayOfTableCards() { return this.TableCards; }
        public void setValue(Client[] arr) 
        { 
            this.ClientArray = arr; 
        }
        public void GameOngoing() { isGameOngoing = true; }
        public void GameNotOngoing() { isGameOngoing = false; }
        public bool isGameOngoin() { return isGameOngoing; }
        public int getRoundNum() { return this.RoundNum; }
        public void setRoundNum(int Num) { RoundNum = Num; }
        public void nextRound() { this.RoundNum += 1; }
        public void resetRounds() { this.RoundNum = 1; }
        public void addTableTotalMoney(int Num)
        {
            TableTotalMoney += Num;
        }
        public int getTableTotalMoney() { return TableTotalMoney; }
        public void resetTableTotalMoney() { TableTotalMoney = 0; }
        public void setTableBetAmount(int Num)
        {
            TableBetAmount = Num;
        }
        public void setValue(Client client,int index)
        {
            this.ClientArray[index] = client;
        }
        public void setTableCard(string card)
        {
            for(int i=0;i<TableCards.Length;i++)
            {
                if (TableCards[i] == null)
                {
                    TableCards[i] = card;
                    break;
                }
            }
        }
        public void resetTableCards()
        {
            TableCards = new string[5];
        }
        public int getTableBetAmount() { return this.TableBetAmount; }
        public Node getNext() { return this.next; }
        public void setNext(Node next) { this.next = next; }
        public void setPlayerHandStrength(HandStrength HS,int index)
        {
            PlayerHandStrengths[index] = HS;
        }
        public HandStrength[] getPlayerHandStrengths() { return PlayerHandStrengths; }
        public void resetClientBets()
        {
            foreach(Client client in ClientArray)
            {
                if (client != null)
                {
                    client.setPlacedBet(0);
                }
            }
        }
        public void kickAllMarkedToLeave()
        {
            foreach (Client client in ClientArray)
                if (client != null && client.isMarkedToLeave())
                    client.ClientLeave();
        }
    }
}
