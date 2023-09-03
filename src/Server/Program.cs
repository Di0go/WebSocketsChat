using System;

class ServerBootstrap
{
    public static void Main(string[] args)
    {
        Server server = new Server("127.0.0.1", 6666);

        server.Listen();
        server.Accept();
    }
}