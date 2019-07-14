using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ChatSender
{
    [ApiVersion(2, 1)]
    public class ChatSender : TerrariaPlugin
    {
        public override string Name => "ChatSender";
        public override Version Version => new Version(1,0,0);
        public override string Author => "Desin";
        public override string Description => "Send Messages.";

        public bool firstconnect = false;
        public int Port = 7879;//端口
        public string Host = "47.107.149.184";//IP
        public IPAddress ip;
        public IPEndPoint ipe;
        public Socket sSocket;
        public bool Connected = false;

        public ChatSender(Main game) : base(game)
        {
            
        }

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.ServerChat.Register(this, OnChat);
            ip = IPAddress.Parse(Host);
            ipe = new IPEndPoint(ip,Port);
            Thread thread = new Thread(new ThreadStart(Connect));
            thread.Start();
        }

        private void OnInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("reconnect", Reconnect, "reconnect")
            {
                HelpText = "重新连接"
            });
        }
        private void OnChat(ServerChatEventArgs args)
        {
            if (sSocket.Connected)
            {
                if (args.Text != null)
                {
                    TSPlayer player = new TSPlayer(args.Who);
                    string message = player.Name + ":" + args.Text;
                    byte[] sendBytes = Encoding.UTF8.GetBytes(message);
                    if (Connected)
                    {
                        sSocket.Send(sendBytes);
                    }
                }
            }
        }
        private void Reconnect(CommandArgs args)
        {
            sSocket.Disconnect(false);
            Connected = false;
            Console.WriteLine("正在重新连接");
            while (!Connected)
            {
                try
                {
                    sSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    sSocket.Connect(ipe);
                }
                catch
                {
                    sSocket.Close();
                    continue;
                }
                Connected = true;
                Console.WriteLine("连接已经建立");
                break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Connected)
                {
                    sSocket.Close();
                }
                
                ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
            }
            base.Dispose(disposing);
        }
        private void Connect()
        {
            while (true)
            {
                if (!firstconnect)
                {
                    Console.WriteLine("正在尝试连接");
                    try
                    {
                        sSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        sSocket.Connect(ipe);
                    }
                    catch
                    {
                        sSocket.Close();
                        continue;
                    }
                    firstconnect = true;
                    Connected = true;
                    Console.WriteLine("连接已经建立");
                }
                else
                {
                    while (Connected)
                    {
                        try
                        {
                            if (sSocket.Poll(-1, SelectMode.SelectRead))
                            {
                                byte[] temp = new byte[2048];
                                int nRead = sSocket.Receive(temp);
                            }
                        }
                        catch { Connected = false; }
                    }
                    while (!Connected)
                    {
                        Console.WriteLine("连接已断开，正在尝试重新连接");
                        try
                        {
                            sSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            sSocket.Connect(ipe);
                        }
                        catch
                        {
                            sSocket.Close();
                            continue;
                        }
                        Connected = true;
                        Console.WriteLine("连接已经建立");
                    }
                }
            }
        }
    }
}
