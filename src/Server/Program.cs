using System;

static class ServerBootstrap
{
    public static void Main(string[] args)
    {
        Server server = new Server("127.0.0.1", 6666);

        server.Listen();

        while (server.m_IsServerOn)
        {
            server.Accept();
        }
    }
}