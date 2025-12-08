using Server.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class Program
    {
        static IPAddress ServerIPAddress;
        static int ServerPort;
        static int MaxClient;
        static int Duration;
        static List<Client> AllClients = new List<Client>();
        static Context dbContext;

        static void Main(string[] args)
        {
            dbContext = new Context();
            OnSettings();
            Thread tListner = new Thread(ConnectServer);
            tListner.Start();
            Thread tDisconnect = new Thread(DisconnectClient);
            tDisconnect.Start();
            while (true) SetCommand();
        }


        static void OnSettings()
        {
            string Path = Directory.GetCurrentDirectory() + "/.config";
            if (File.Exists(Path))
            {
                StreamReader sr = new StreamReader(Path);
                ServerIPAddress = IPAddress.Parse(sr.ReadLine());
                ServerPort = int.Parse(sr.ReadLine());
                MaxClient = int.Parse(sr.ReadLine());
                Duration = int.Parse(sr.ReadLine());
                sr.Close();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"IP-address сервера: {ServerIPAddress.ToString()};\nПорт сервера: {ServerPort};\nМаксимум клиентов: {MaxClient};\nПродолжительность: {Duration};");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"Укажите свой IP-адрес: ");
                ServerIPAddress = IPAddress.Parse(Console.ReadLine());
                Console.Write($"Укажите свой порт: ");
                ServerPort = int.Parse(Console.ReadLine());
                Console.Write($"Укажите максимальное количество клиентов: ");
                MaxClient = int.Parse(Console.ReadLine());
                Console.Write($"Укажите срок действия лицензии: ");
                Duration = int.Parse(Console.ReadLine());
                StreamWriter sw = new StreamWriter(Path);
                sw.WriteLine(ServerIPAddress.ToString());
                sw.WriteLine(ServerPort);
                sw.WriteLine(MaxClient);
                sw.WriteLine(Duration);
                sw.Close();
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Чтобы изменить, введите команду: /config");
        }

        static void SetCommand()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            string Command = Console.ReadLine();
            switch (Command)
            {
                case "/config": File.Delete(Directory.GetCurrentDirectory() + "/.config"); OnSettings(); break;
                case "/status": GetStatus(); break;
                case "/help": Help(); break;
                case "/ban": AddToBlacklist(); break;
                case "/removeban": RemoveFromBlacklist(); break;
                default: if (Command.Contains("/disconnect")) DisconnectServer(Command); break;
            }
        }

        static void GetStatus()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Count clients: {AllClients.Count}");
            foreach (var client in AllClients)
            {
                int Duration = (int)DateTime.Now.Subtract(client.DateConnect).TotalSeconds;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Клиент: {client.Token}, время подключения: {client.DateConnect.ToString("HH:mm:ss dd.MM")}, продолжительность: {Duration}");
            }
        }

        static void Help()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Команда в сторону клиента: ");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/config");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - начальные настройки");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/disconnect");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - отключение пользователей от сервера");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/status");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - показать список пользователей");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/ban");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - добавить клиента в черный список");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/removeban");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - удалить клиента из черного списка");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("/banlist");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - список черного листа");
        }

        static void AddToBlacklist()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Введите имя пользователя для добавления в черный список: ");
            string username = Console.ReadLine();
            var client = AllClients.FirstOrDefault(c => c.Username == username);
            if (client != null)
            {
                AllClients.Remove(client);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Клиент {client.Token} отключен из-за добавления в черный список.");
            }
        }

        static void RemoveFromBlacklist()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Введите имя пользователя для удаления из черного списка: ");
            string username = Console.ReadLine();
        }

        static void DisconnectServer(string Command)
        {
            try
            {
                string Token = Command.Replace("/disconnect ", "");
                var DisconnectClient = AllClients.Find(x => x.Token == Token);
                AllClients.Remove(DisconnectClient);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"Клиент: {Token} отключен от сервера");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Ошибка: " + ex.Message);
            }
        }

        static void ConnectServer()
        {
            IPEndPoint EndPoint = new IPEndPoint(ServerIPAddress, ServerPort);
            Socket SocketListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SocketListener.Bind(EndPoint);
            SocketListener.Listen(MaxClient);
            while (true)
            {
                try
                {
                    Socket Handler = SocketListener.Accept();
                    byte[] bytes = new byte[10485760];
                    int byteRec = Handler.Receive(bytes);
                    string Message = Encoding.UTF8.GetString(bytes, 0, byteRec);
                    string Response = SetCommandClient(Message);
                    Handler.Send(Encoding.UTF8.GetBytes(Response));
                    Handler.Shutdown(SocketShutdown.Both);
                    Handler.Close();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Ошибка: " + ex.Message);
                }
            }
        }

        static void DisconnectClient()
        {
            while (true)
            {
                for (int i = 0; i < AllClients.Count; i++)
                {
                    int ClientDuration = (int)DateTime.Now.Subtract(AllClients[i].DateConnect).TotalSeconds;
                    if (ClientDuration > Duration)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Клиент: {AllClients[i].Token} отключен от сервера из-за окончания времени");
                        AllClients.RemoveAt(i);
                    }
                }
                Thread.Sleep(1000);
            }
        }

        static string SetCommandClient(string Command)
        {
            if (Command == "/token")
            {
                if (AllClients.Count < MaxClient)
                {
                    var newClient = new Client();
                    AllClients.Add(newClient);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Новый клиент подключен: {newClient.Token}");
                    return newClient.Token;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("На сервере нет места для еще одного подключения");
                    return "/limit";
                }
            }
            else
            {
                var Client = AllClients.Find(x => x.Token == Command);
                return Client != null ? "/connect" : "/disconnect";
            }
        }
    }
}
