using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Client
{
    class Client
    {
        static TcpClient client;
        static IPAddress adress;
        static NetworkStream ns;
        static int port;
        static void Main(string[] args)
        {
            while (true) //запрашиваем порт у юзера
            {
                try
                {
                    Console.Write("Введите адрес для подключения: ");
                    adress = IPAddress.Parse(Console.ReadLine());
                    Console.Write("Введите порт: ");
                    port = int.Parse(Console.ReadLine());
                    break;
                }
                catch (FormatException)
                {
                    Console.WriteLine("Введите верные данные!");
                }
            }
            client = new TcpClient();
            client.Connect(adress, port);
            Console.WriteLine("Соединение установлено.");
            ns = client.GetStream();
            BinaryWriter bw = new BinaryWriter(ns);
            bw.Write("notbrowser");
            // пояснение за костыль выше - к серверу умудряется подключиться браузер. это - своеобразная проверка на подлинность
            bw.Flush();
            Console.Write("Введите имя воина (меньше 12 символов): ");
            string name;
            while (true)
            {
                name = Console.ReadLine();
                if (name.Length > 12)
                {
                    Console.WriteLine("Имя должно быть меньше 12 символов!");
                }
                else if (name.Contains('|')) // oh, unexploitable!
                {
                    Console.WriteLine("Недопустимое имя!");
                }
                else break;
            }
            try
            {
                bw.Write(name);
                bw.Flush();
            }
            catch (IOException)
            {
                Console.WriteLine("Сервер разорвал подключение. Возможно, игра уже идёт. Если нет - багрепорт.");
                Console.WriteLine("Нажмите Enter, чтобы выйти...");
                Console.ReadLine();
                Environment.Exit(0);
            }
            Console.WriteLine("Ожидаем второго игрока.");
            Task.Run(() => { Reading(ns); });
            CheckWriting();
            bw.Close();
            Console.ReadKey();
            client.Close();

        }

        // TODO: ввести чат
        static void CheckWriting()
        {
            while (true)
            {
                /*string message = Console.ReadLine();
                if (message == "exit")
                {
                    break;
                }
                //bw.Write(message);
                //bw.Flush();
                //Console.WriteLine("Отправлено...");*/
            }
        }

        static void Reading(NetworkStream ns)
        {
            BinaryReader br = new BinaryReader(ns);
            try
            {
                while (true)
                {

                    string mes = br.ReadString();
                    string[] command_check = mes.Split('|');
                    if (command_check.Length == 1)
                    {
                        Console.WriteLine(mes);
                    }
                    else
                    {
                        if (command_check[0] == "erase")
                        {
                            Console.Clear();
                        }
                        else if (command_check[0] == "notification")
                        {
                            Task.Run(() => { Console.Beep(1000, 500); Console.Beep(1400, 500); });
                        }
                        else if (command_check[0] == "replay")
                        {
                            ConsoleKey key;
                            do
                            {
                                key = Console.ReadKey(true).Key;
                            } while (key != ConsoleKey.Y && key != ConsoleKey.N);
                            using (BinaryWriter bw = new BinaryWriter(ns, Encoding.UTF8, true))
                            {
                                switch (key)
                                {
                                    case ConsoleKey.Y:
                                        bw.Write("replay_y|debug_replay_y");
                                        break;
                                    case ConsoleKey.N:
                                        bw.Write("replay_n|debug_replay_n");
                                        break;
                                }
                            }
                        }
                    }
                    ns.Flush();
                }
            }
            catch (IOException)
            {
                Console.WriteLine("Подключение прервано");
                ns.Close();
                client.Close();
                Console.WriteLine("Нажмите Enter, чтобы выйти...");
                Console.ReadLine();
                Environment.Exit(0);
            }
        }
    }
}
