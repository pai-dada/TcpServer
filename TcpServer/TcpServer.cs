using System.Net;
using System.Net.Sockets;

namespace a
{

    /// <summary>
    /// 被动监听。并且只和一个客户端通讯
    /// </summary>
    internal class TcpServer
    {
        public string IP;
        public int Port;
        public Socket ListenSocket;
        public Socket ClientSocket;
        public bool IsConnect;
        public Object _lock = new Object();
        public byte[] OriginalData;

        public TcpServer(string ip, int port)
        {
            this.IP = ip;
            this.Port = port;
            this.IsConnect = false;
        }

        /// <summary>
        /// 启动循环监听线程。如果有客户端连接，就循环接收来自客户端的数据
        /// </summary>
        public void Start()
        {
            Thread th = new Thread(() =>
            {
                ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(IP), Port);
                ListenSocket.Bind(ipEndPoint);
                ListenSocket.Listen();
                while (true)
                {
                    ClientSocket = ListenSocket.Accept();

                    IsConnect = true;
                    byte[] buffer = new byte[1024 * 1024];
                    int Count=0;
                    bool readData = false;
                    while (true)
                    {
                        try
                        {
                            if(ClientSocket.Poll(0,SelectMode.SelectRead)==true)
                            {
                                Count = ClientSocket.Receive(buffer);
                                if (Count == 0)
                                {
                                    break;
                                }
                                readData = true;
                            }
                           
                        }
                        catch (SocketException ex)
                        {
                            if (ex.ErrorCode == 10054)
                            {
                                break;
                            }
                            else
                            {
                                break;
                            }
                        }

                        if(readData)
                        {
                            Console.WriteLine($"接收{Count}字节");
                            Thread.Sleep(10000);
                            lock (_lock)
                            {
                                OriginalData = new byte[Count];
                                Array.Copy(buffer, OriginalData, Count);
                                readData = false;
                            }
                        }
                        else
                        {
                            
                        }

                    }
                    IsConnect = false;
                }
            });
            th.IsBackground = false;
            th.Start();
        }


        /// <summary>
        /// 断开连接，停止监听，释放资源
        /// </summary>
        public void Stop()
        {
            if (ClientSocket != null)
            {
                ClientSocket.Shutdown(SocketShutdown.Both);
                ClientSocket.Close();
            }

            if (ListenSocket != null)
            {
                ListenSocket.Close();
            }
        }

        public byte[] GetOriginalData()
        {
            lock(_lock)
            {
                return OriginalData;
            }
        }
    }
}
