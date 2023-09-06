using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

class Server
{
    #region Fields
    // Server
    private TcpListener m_TCPListener;
    public bool m_IsServerOn;

    // Server address and port
    private IPAddress m_IPAddress;
    private int m_Port;

    // Client list
    public List<TcpClient> m_Clients;

    // Thread list
    public List<Thread> m_ThreadPool;
    #endregion

    // Constructor
    public Server(string p_IPAddress, int p_Port)
    {
        // Turn the server on
        m_IsServerOn = true;

        // Add the properties
        m_IPAddress = IPAddress.Parse(p_IPAddress);
        m_Port = p_Port;

        // Initialize the Listener, the Client List and the Thread List
        m_Clients = new List<TcpClient>();
        m_ThreadPool = new List<Thread>();
        m_TCPListener = new TcpListener(m_IPAddress, m_Port);
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

        // Create and Start new thread 
        Thread newThread = new Thread(new ParameterizedThreadStart(Handshake));
        newThread.Start(newClient);

        m_ThreadPool.Add(newThread);
    }

    // Method to handle client disconnection
    public void Disconnect(TcpClient p_Client)
    {
        m_Clients.Remove(p_Client);
        Console.WriteLine("Client disconnect from: " +  p_Client.Client.RemoteEndPoint);
        p_Client.Close();

        // Shutdown the Thread
        return;
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
    private void Handshake(object? p_Client)
    {
        if (p_Client != null)
        {
            TcpClient l_Client = (TcpClient)p_Client;

            NetworkStream clientStream = l_Client.GetStream();
            Socket clientSocket = clientStream.Socket;

            while (true)
            {
                if (!IsConnected(clientSocket))
                {
                    Disconnect(l_Client);
                    break; 
                }

                // Don't read data if there's no data.
                if (!clientStream.DataAvailable) continue;

                // Get the available (ready to read) data from the client.
                byte[] clientBuffer = new byte[l_Client.Available];

                // Read the client data into the buffer-
                clientStream.Read(clientBuffer, 0, clientBuffer.Length);

                string encodedData = Encoding.UTF8.GetString(clientBuffer);

                // If the encodedData is a GET request
                if (Regex.IsMatch(encodedData, "^GET"))
                {
                    const string newLine = "\r\n";

                    /*
                    1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                    2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                    3. Compute SHA-1 and Base64 hash of the new value
                    4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                    */

                    // Make the response to the client
                    byte[] response = Encoding.UTF8.GetBytes(

                                "HTTP/1.1 101 Switching Protocols" + newLine
                                + "Connection: Upgrade" + newLine
                                + "Upgrade: websocket" + newLine
                                + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                                SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(
                                new Regex("Sec-WebSocket-Key: (.*)").Match(encodedData).Groups[1].Value.Trim() 
                                + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")))
                                + newLine
                                + newLine

                                );

                    // Write response to client
                    clientStream.Write(response, 0, response.Length);
                }
                else
                {

                }
            } 
        }
    }
}