using a;
using System.Text;


namespace a
{
    class p
    {
        static void Main()
        {
            try
            {
                string ip = "127.0.0.1";
                int port = 5050;
                MyTcpServer t = new MyTcpServer(ip, port);

                t.ClientConnected += Do.ReciveConn;
                t.ClientDataReceived += Do.ReceiveData;
                t.ClientDisConnected += Do.CloseConn;
                Console.WriteLine("开始监听:" + ip + ":" + port.ToString());
                t.Start();


                
                Console.ReadLine();
            }
            catch
            {
                throw;
            }
            
        }
    }

    class Do()
    {
        static public void ReciveConn(Object? sender , ClientConnectedEventArgs e)
        {
            Console.WriteLine("接收到连接:" + e.ClientSocket.IP.ToString() + " " + e.ClientSocket.Port.ToString());
        }

        static public void CloseConn(Object? sender,EventArgs e)
        {
            if (sender == null) return;

            ClientSocket clientSocket = (ClientSocket)sender;

            Console.WriteLine("断开连接:" + clientSocket.IP.ToString() + " " + clientSocket.Port.ToString());
        }

        static public void ReceiveData(Object? sender, DataReceivedEventArgs e)
        {
            if (sender == null) return;

            ClientSocket clientSocket = (ClientSocket)sender;

            Console.WriteLine(clientSocket.IP + ":" + clientSocket.Port.ToString() + " 接收到"+e.Data.Length +"字节数据：" + Encoding.UTF8.GetString(e.Data));
        }
    }
}
