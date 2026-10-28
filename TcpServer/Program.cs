using a;


namespace a
{
    class p
    {
        static void Main()
        {
            string ip = "192.168.0.134";
            int port = 5050;
            TcpServer t = new TcpServer(ip,port);
            Console.WriteLine("正在监听:" + ip + ":" + port.ToString());
            t.Start();

            Console.ReadLine();
        }
    }
}
