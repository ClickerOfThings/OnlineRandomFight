using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Server
{
    class Client
    {
        public TcpClient client;
        public NetworkStream stream;
        public string name;
        static int id = 0;
        public int id_user;
        public Fighter fighter;
        public Client(TcpClient client)
        {
            this.client = client;
            id_user = id;
            id++;
            stream = client.GetStream();
            string name;
            using (BinaryReader br = new BinaryReader(stream, Encoding.UTF8, true))
            {
                name = br.ReadString();
            }
            if (name == "" || name.Length < 1)
            {
                this.name = "Воин" + id_user;
            }
            else
            {
                this.name = name;
            }
            Server.SendToAll(name + " подключился!");
            fighter = new Fighter(name, this);
            //ConnectionCheck = new Thread(this.CheckMessages);
            //ConnectionCheck.Start();
        }
        public string GetMessage()
        {
            try
            {
                NetworkStream ns = this.stream;
                using (BinaryReader br = new BinaryReader(ns, Encoding.UTF8, true))
                {

                    //while (true)
                    //{
                    string mes = br.ReadString();
                    return mes;
                    // нет надобности в сообщениях, пока что
                    // TODO: добавить чат
                    //string message = this.name + ": " + mes;
                    //Console.WriteLine(message);
                    //Server.SendToAll(message);
                    //ns.Flush();
                    //}
                }
            }
            catch (IOException)
            {
                CloseConnection();
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.TargetSite);
                Console.WriteLine(e.HelpLink);
                Console.WriteLine("GetMessage");
                return null;
            }
        }
        public void CheckMessage()
        {
            try
            {
                NetworkStream ns = this.stream;
                using (BinaryWriter br = new BinaryWriter(ns, Encoding.UTF8, true))
                {
                    br.Write(new byte());
                }
            }
            catch (IOException)
            {
                CloseConnection();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.TargetSite);
                Console.WriteLine(e.HelpLink);
                Console.WriteLine("GetMessage");
            }
        }
        public void SendToClient(string message)
        {
            try
            {
                using (BinaryWriter bw = new BinaryWriter(this.stream, Encoding.UTF8, true))
                {
                    bw.Write(message);
                }
            }
            catch (IOException)
            {
                CloseConnection();
            }
        }
        public void CloseConnection()
        {
            this.stream.Close();
            Server.players.Remove(this);
            string message = this.name + " отключился";
            Console.WriteLine(message);
            Server.SendToAll(message);
        }
    }
}
