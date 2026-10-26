using a;


namespace a
{
    class p
    {
        static void Main()
        {
            TcpServer t = new TcpServer("127.0.0.1", 5050);
            t.Start();
        }
    }
}
