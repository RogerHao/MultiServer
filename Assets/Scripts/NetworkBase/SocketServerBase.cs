using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class SocketServerBase
{
    public static bool SocketServerStarted;
    private static CancellationTokenSource socketCancellationTokenSource = new CancellationTokenSource();

    private static TcpListener tcpListner;
    public static event EventHandler<SocketConnectionMsg> ConnectionEvent;
    public static event EventHandler<SocketConnectionMsg> DisconnectionEvent;
    public static event EventHandler<SocketDataMsg> DataEvent;
    
    public class SocketConnectionMsg : EventArgs
    {
        public TcpClient Client;
        public string ConnectionInfo;

        public SocketConnectionMsg(TcpClient client, string info)
        {
            Client = client;
            ConnectionInfo = info;
        }
    }
    public class SocketDataMsg : EventArgs
    {
        public string ClientInfo;
        public string Msg;

        public SocketDataMsg(string info, string msg)
        {
            ClientInfo = info;
            Msg = msg;
        }
    }

    public class SocketClient
    {
        public TcpClient Client;
        public string ClientInfo;
    }
    public static List<SocketClient> Clients = new List<SocketClient>();

    public static void StartSocketServer(int portNum = 9695)
    {
        if (SocketServerStarted) return;
        tcpListner = new TcpListener(IPAddress.Any,portNum);
        tcpListner.Start();
        Task.Run(() => ListenForClients(), socketCancellationTokenSource.Token);

        ConnectionEvent += OnConnectionEvent;
        DisconnectionEvent += OnDisconnectionEvent;

        SocketServerStarted = true;
    }

    public static void StopSocketServer()
    {
        if (!SocketServerStarted) return;

        socketCancellationTokenSource.Cancel();

        ConnectionEvent -= OnConnectionEvent;
        DisconnectionEvent -= OnDisconnectionEvent;

        SocketServerStarted = false;
    }

    private static void OnConnectionEvent(object sender, SocketConnectionMsg e)
    {
        SocketClient client = new SocketClient
        {
            Client = e.Client,
            ClientInfo = e.ConnectionInfo
        };
        Clients.Add(client);
    }

    private static void OnDisconnectionEvent(object sender, SocketConnectionMsg e)
    {
        foreach (SocketClient item in Clients)
        {
            if (item.ClientInfo == e.ConnectionInfo) Clients.Remove(item);
        }
    }

    public static void SendMessage(SocketClient client, string msg)
    {
        NetworkStream stream = client.Client.GetStream();
        byte[] messageBytes = Encoding.UTF8.GetBytes(msg);
        stream.Write(messageBytes, 0, messageBytes.Length);
    }

    public static void SendMessage(string client, string msg)
    {
        foreach (var itemClient in Clients)
        {
            if(!itemClient.ClientInfo.Contains(client)) continue;
            NetworkStream stream = itemClient.Client.GetStream();
            byte[] messageBytes = Encoding.UTF8.GetBytes(msg);
            stream.Write(messageBytes, 0, messageBytes.Length);
        }
    }

    public static void MultiSendMessage(List<SocketClient> clients,string msg)
    {
        foreach (var client in clients)
        {
            NetworkStream stream = client.Client.GetStream();
            byte[] messageBytes = Encoding.UTF8.GetBytes(msg);
            stream.Write(messageBytes, 0, messageBytes.Length);
        }
    }

    private static void ListenForClients()
    {
        while (!socketCancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                TcpClient client = tcpListner.AcceptTcpClient();
                ConnectionEvent?.Invoke(null,
                    new SocketConnectionMsg(client,client.Client.RemoteEndPoint.ToString()));
                Task.Run(() => ListenForData(client));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

    private static void ListenForData(object client)
    {
        TcpClient tcpClient = (TcpClient) client;
        NetworkStream clientStream = tcpClient.GetStream();
        byte[] message = new byte[4096];
        int bytesRead;
        while (true)
        {
            bytesRead = 0;
            try
            {
                bytesRead = clientStream.Read(message, 0, 4096);
            }
            catch
            {
                break;
            }
            if (bytesRead == 0)
            {
                DisconnectionEvent?.Invoke(null, new SocketConnectionMsg(tcpClient, tcpClient.Client.RemoteEndPoint.ToString()));
                break;
            }
            ASCIIEncoding encoder = new ASCIIEncoding();
            string msg = encoder.GetString(message, 0, bytesRead);
            DataEvent?.Invoke(null, new SocketDataMsg(tcpClient.Client.RemoteEndPoint.ToString(), msg));
        }
        tcpClient.Close();
    }
}
