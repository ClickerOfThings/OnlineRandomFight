using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace Server
{
    class Server
    {
        static int port;
        public static TcpListener listener;
        public static List<Client> players = new List<Client>();
        static bool opened = true;
        public static int MaxHealthSet = 150;

        static void Main(string[] args)
        {
            while (true) //запрашиваем порт у юзера
            {
                try
                {
                    Console.Write("Введите порт (от 49152 до 65536): ");
                    port = int.Parse(Console.ReadLine());
                    if (port > 65536 || port < 49152) throw new FormatException();
                    break;
                }
                catch (FormatException)
                {
                    Console.WriteLine("Введите верные данные!");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Ошибка:\n" + e.Message);
                    Console.WriteLine("Попробуйте снова!");
                }
            }
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            TcpListener();
        }

        static void TcpListener()
        {
            Console.WriteLine("Настроить игру? Y/N");
            while (true)
            {
                ConsoleKey key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Y)
                {
                    bool leave = false;
                    do
                    {
                        leave = Settings();
                    } while (!leave);
                    break;
                }
                else if (key == ConsoleKey.N)
                {
                    Console.WriteLine("OK");
                    break;
                }
            }
            Console.WriteLine("TCP сервер запущен.");
            Console.WriteLine("Ожидаем игроков...");
            Task connects = Task.Factory.StartNew(() => { Listen(); }, TaskCreationOptions.LongRunning);
            while (true)
            {
                if (Console.ReadLine() == "close")
                {
                    opened = false;
                    Thread.Sleep(300);
                    listener.Stop();
                    break;
                }
            }
        }

        static bool Settings()
        {
            void SetMax()
            {
                Console.WriteLine("Введите новое максимальное здоровье (пустое для стандарта - {0})", MaxHealthSet);
                //TODO: лимит здоровья?
                // TODO: переписать скрипт для свойства, как ниже
                while (true)
                {
                    int.TryParse(Console.ReadLine(), out int max);
                    if (max != 0)
                    {
                        if (max > int.MaxValue) max = int.MaxValue;
                        MaxHealthSet = max;
                    }
                    Console.WriteLine("Установлено.");
                    break;
                }
            }
            void setDelay()
            {
                Console.WriteLine("Введите новую задержку (пустое - {0} мс, минимальное значение)", FightProcess.MIN_DELAY);
                int.TryParse(Console.ReadLine(), out int max);
                FightProcess.Delay = max;
                Console.WriteLine("Установлено.");
            }
            Console.WriteLine("Текущее максимальное здоровье: " + MaxHealthSet);
            Console.WriteLine("Текущая задержка: " + FightProcess.Delay + " мс");
            Console.WriteLine("h - новое максимальное здоровье");
            Console.WriteLine("d - задержки между действиями");
            Console.WriteLine("ESC - выйти");
            ConsoleKey key;
            do
            {
                key = Console.ReadKey(true).Key;
            } while (key != ConsoleKey.H && key != ConsoleKey.Escape && key != ConsoleKey.D);
            switch (key)
            {
                case ConsoleKey.H:
                    SetMax();
                    break;
                case ConsoleKey.D:
                    setDelay();
                    break;
                case ConsoleKey.Escape:
                    return true;
            }
            return false;
        }

        static Thread closeConnections;
        static void Listen()
        {
            try
            {
                while (true)
                {
                    Console.WriteLine("Ожидание подключения...");
                    TcpClient newplayer = listener.AcceptTcpClient();
                    if (new BinaryReader(newplayer.GetStream()).ReadString() != "notbrowser") // так как к серверу умудряется еще браузер подключаться...
                    {
                        newplayer.Close();
                        continue;
                    }
                    /*if (!opened)
                    {
                        using (BinaryWriter bw = new BinaryWriter(newplayer.GetStream(), Encoding.UTF8, false))
                        {
                            bw.Write("Игра не открыта, подождите некоторое время...");
                        }
                        continue;
                    }*/
                    else
                    {
                        CheckConnections();
                        switch (players.Count)
                        {
                            case 0:
                                CreateClient(newplayer);
                                break;
                            case 1:
                                CreateClient(newplayer);
                                opened = false;
                                closeConnections = new Thread(CloseWhilePlaying);
                                closeConnections.Start();
                                bool revanche;
                                do
                                {
                                    revanche = FightProcess.Start();
                                    ReInitialize();
                                } while (revanche);
                                SendToAll("Второй игрок отключился. Ожидание нового оппонента...");
                                Thread.Sleep(2000);
                                opened = true;
                                new TcpClient().Connect(IPAddress.Loopback, port);
                                ReInitialize();
                                break;
                            default:
                                using (BinaryWriter bw = new BinaryWriter(newplayer.GetStream(), Encoding.UTF8, false))
                                {
                                    bw.Write("Игра уже идет, подождите конца раунда.");
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.TargetSite);
                Console.WriteLine(e.HelpLink);
                Console.WriteLine("Listen");
                throw;
            }
        }

        static void CloseWhilePlaying()
        {
            do
            {
                TcpClient kick = listener.AcceptTcpClient();
                if (new BinaryReader(kick.GetStream()).ReadString() != "notbrowser")
                {
                    kick.Close();
                    continue;
                }
                using (BinaryWriter bw = new BinaryWriter(kick.GetStream(), Encoding.UTF8, false))
                {
                    bw.Write("Игра не открыта, подождите некоторое время...");
                }
            } while (!opened);
        }

        static void CreateClient(TcpClient newplayer)
        {
            Client newclient = new Client(newplayer);
            Console.WriteLine("Подключился " + newclient.name);
            players.Add(newclient);
            using (BinaryWriter bw = new BinaryWriter(newclient.stream, Encoding.UTF8, true))
            {
                bw.Write(string.Format("Текущее максимальное здоровье: {0}", MaxHealthSet));
                bw.Write(string.Format("Текущая задержка между действиями: {0} мс", FightProcess.Delay));
                //newclient.stream.Flush();
            }
        }

        // deprecated Fighter sender = null
        public static void SendToAll(string message)
        {
            foreach (Client player in players.ToArray())
            {
                player.CheckMessage();
                /*if (player == sender)
                {
                    continue;
                }*/
            }
            foreach (Client player in players)
            {
                using (BinaryWriter bw = new BinaryWriter(player.stream, Encoding.UTF8, true))
                {
                    bw.Write(message);
                }
            }

        }
        static void ReInitialize()
        {
            SendToAll("erase|debug_erase");
            FightProcess.ReInitalizeAll();
        }

        public static void CheckConnections()
        {
            foreach (Client player in players.ToArray())
            {
                player.CheckMessage();
            }

        }

    }
}
