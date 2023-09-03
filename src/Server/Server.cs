using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;

class Server
{
    // Server
    private TcpListener m_TCPListener;

    // Server address and port
    private IPAddress m_IPAddress;
    private int m_Port;

    // Client list
    public List<TcpClient> m_Clients;

    // Constructor
    public Server(string p_IPAddress, int p_Port)
    {
        m_IPAddress = IPAddress.Parse(p_IPAddress);
        m_Port = p_Port;

        // Initialize the Listener and the Client List
        m_TCPListener = new TcpListener(m_IPAddress, m_Port);
        m_Clients = new List<TcpClient>();
    }

    // Start listening for connections
    public void Listen()
    {
        m_TCPListener.Start();

        Console.WriteLine("Server Listening at: " + m_IPAddress.ToString() + " " + m_Port);
    }

    // Accept clients
    public void Accept()
    {
        TcpClient newClient = m_TCPListener.AcceptTcpClient();

        m_Clients.Add(newClient);

        Console.WriteLine("A new client has joined from " + newClient.Client.RemoteEndPoint);

        Handshake(newClient);
    }

    // Method to handle client disconnection
    public void Disconnect(TcpClient p_Client)
    {
        m_Clients.Remove(p_Client);
        Console.WriteLine("Client disconnect from: " +  p_Client.Client.RemoteEndPoint);
        p_Client.Close();
    }

    // Checks if socket is connected
    public bool IsConnected(Socket p_Socket)
    {
        try
        {
            return !(p_Socket.Poll(1000, SelectMode.SelectRead) && p_Socket.Available == 0);
        }
        catch (SocketException)
        {
            return false;
        }
    }

    // When a client connects to a server, it sends a GET request to upgrade the connection to a WebSocket from a simple HTTP request. 
    // This is known as handshaking.
    private void Handshake(TcpClient p_Client)
    {
        NetworkStream clientStream = p_Client.GetStream();
        Socket clientSocket = clientStream.Socket;

        while (true)
        {
            if (!IsConnected(clientSocket))
            {
               Disconnect(p_Client);
               break; 
            }

            // Don't read data if there's no data.
            if (!clientStream.DataAvailable) continue;

            // Get the available (ready to read) data from the client.
            byte[] clientBuffer = new byte[p_Client.Available];

            Console.WriteLine(clientStream.Read(clientBuffer, 0, clientBuffer.Length));
        }
    }
 
}