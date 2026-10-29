using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

//每一个方法正常工作，异常工作，别人终止，所操作对象的状态。考虑这三种情况即可
//调用者负责对出错的方法提供后续的处理，工作的方法只负责： 工作/抛出问题

namespace a
{

    /// <summary>
    /// 被动监听
    /// </summary>
    internal class MyTcpServer
    {
        public string IP;
        public int Port;
        public Socket? ListenSocket;
        public Thread? ThreadListen;
        public Object _lock = new object();
        public List<ClientSocket> ClientSockets = new List<ClientSocket>();
        public volatile bool IsContinue;

        public event EventHandler<ClientConnectedEventArgs>? ClientConnected;//事件 有客户端成功连接
        public event EventHandler? ClientDisConnected; //事件 有客户端断开连接,这个事件会直接给到客户端，这里只做一个转发的功能
        public event EventHandler<DataReceivedEventArgs>? ClientDataReceived;//事件 客户端收到消息，这个事件会直接给到客户端，这里只做一个转发的功能

        public MyTcpServer(string ip, int port)
        {
            try
            {
                this.IP = ip;
                this.Port = port;
                this.IsContinue = true;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// 启动循环监听线程
        /// </summary>
        public void Start()
        {
            if (ThreadListen !=null && ThreadListen.IsAlive) { throw new Exception("监听对象已经启动，不要再启动了"); }

            try
            {
                Socket socket1 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(IP), Port);
                socket1.Bind(ipEndPoint);
                socket1.Listen();
                ListenSocket = socket1;
                IsContinue = true;

                Thread th = new Thread(() =>
                {
                    while (IsContinue)
                    {
                        try
                        {
                            Socket socket2 = ListenSocket.Accept();
                            ClientSocket clientSocket = new ClientSocket(socket2);

                            //当客户端收到数据，填写回调函数
                            clientSocket.DataReceived += this.ClientDataReceived;

                            //当客户端关闭连接，填写回调函数
                            clientSocket.ConnectClosed += this.ClientDisConnected;

                            lock(_lock)
                            {
                                ClientSockets.Add(clientSocket);
                            }
                            //有客户端连接，触发事件
                            OnClientConnected(clientSocket);
                            clientSocket.Start();
                        }
                        catch(Exception ex) 
                        {
                            Console.WriteLine(ex.StackTrace);
                            Console.WriteLine(ex.Message);
                        }
                    }
                });
                th.IsBackground = true;
                ThreadListen = th;
                th.Start();
            }
            catch
            {
                throw;
                
            }
        }


        /// <summary>
        /// 断开连接，停止监听，释放所有socke
        /// </summary>
        public void Stop()
        {
            this.IsContinue = false;
            ListenSocket?.Close();
            ThreadListen?.Join();
            lock(_lock)
            {
                foreach (ClientSocket clientSocket in ClientSockets)
                {
                    try
                    {
                        clientSocket.Stop();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        private void OnClientConnected(ClientSocket clientSocket)
        {
            EventHandler<ClientConnectedEventArgs>? handler = ClientConnected;
            if (handler == null) return;

            ClientConnectedEventArgs args = new ClientConnectedEventArgs(clientSocket);
            foreach (EventHandler<ClientConnectedEventArgs> d in handler.GetInvocationList())
            {
                try
                {
                    d(this, args);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }

    public class ClientConnectedEventArgs : EventArgs
    {
        public ClientSocket ClientSocket;
        public ClientConnectedEventArgs(ClientSocket clientSocket)
        {
            this.ClientSocket = clientSocket;
        }
    }
    public class DataReceivedEventArgs : EventArgs
    {
        public byte[] Data { get; }
        public DataReceivedEventArgs(byte[] data)
        {
            this.Data = data;
        }
    }
    public class ClientSocket
    {
        public Socket socket;
        public string IP;
        public int Port;
        public Object _lock = new Object();
        private byte[] OriginalData = new byte[1024 * 1024];
        public int OriginalDataLength;
        public event EventHandler? ConnectClosed;//事件 客户端断开连接
        public event EventHandler<DataReceivedEventArgs>? DataReceived;//事件 客户端收到消息
        

        public ClientSocket(Socket socket)
        {
            try
            {
                if (socket == null) throw new Exception("不要拿null来糊弄我!");

                this.socket = socket;
                IPEndPoint? remoteEp = socket.RemoteEndPoint as IPEndPoint;
                if (remoteEp != null)
                {
                    IP = remoteEp.Address.ToString();
                    Port = remoteEp.Port;

                    Console.WriteLine("连接到"+IP +":"+Port);
                }
                else
                {
                    IP = String.Empty;
                    Port = 0;
                }
            }
            catch
            {
                throw;
            }
        }


        //循环接收消息
        public void Start()
        {
            Thread th = new Thread(() =>
            {
                try
                {
                    byte[] buffer = new byte[1024 * 1024];//程序缓冲区1MB大小
                    int length = -1;

                    //循环接收信息
                    while (true)
                    {
                        try
                        {
                            length = socket.Receive(buffer);
                            if (length == 0)
                            {
                                break;
                            }
                        }
                        catch (SocketException ex)
                        {
                            if (ex.ErrorCode == 10053 || ex.ErrorCode == 10054)
                            {
                                throw;
                            }
                            else
                            {
                                throw;
                            }
                        }
                        catch
                        {
                            throw;
                        }

                        if (length > 0)
                        {
                            lock (_lock)
                            {
                                Buffer.BlockCopy(buffer, 0, OriginalData, 0, length);
                                OriginalDataLength = length;
                            }
                            OnDataReceived();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    OnConnectClosed();
                    this.Stop();
                }

            });
            th.IsBackground = true;
            th.Start();
        }


        public byte[] GetOriginalData()
        {
            lock (_lock)
            {
                byte[] res = new byte[OriginalDataLength];
                Buffer.BlockCopy(OriginalData, 0, res, 0, OriginalDataLength);
                return res;
            }
        }


        //主动断开连接
        public void Stop()
        {
            try
            {
                if (this.socket != null)
                {
                    socket.Close();
                }
            }
            catch (ObjectDisposedException) { }
            catch { throw; }
        }


        //发送数据
        public int SendMsg(byte[] buffer)
        {
            try
            {
                int count = socket.Send(buffer);
                Console.WriteLine("发送" + count + "字节的数据");
                return count;
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode == 10053 || ex.ErrorCode == 10054)
                {
                    //10053:自己断开连接  10054:对方断开连接
                    throw;
                }
                else
                {
                    throw;
                }
            }
            catch
            {
                throw;
            }
        }


        //当前socket断开，触发信号
        public void OnConnectClosed()
        {
            EventHandler? handler = ConnectClosed;

            if (handler == null) return;

            foreach (EventHandler d in handler.GetInvocationList())
            {
                try
                {
                    d(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(ex.Message);
                }
            }
        }

        //接收到消息，触发信号
        private void OnDataReceived()
        {
            EventHandler<DataReceivedEventArgs>? handler = DataReceived;
            if (handler == null) return;

            byte[] data = this.GetOriginalData();
            DataReceivedEventArgs args = new DataReceivedEventArgs(data);
            foreach (EventHandler<DataReceivedEventArgs> d in handler.GetInvocationList())
            {
                try
                {
                    d(this, args);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
