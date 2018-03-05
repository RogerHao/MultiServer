using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;

public class UnetServerBase : MonoBehaviour
{
    public static int hostId;
    public static int myReliableChannelId;
    public static bool UnetServerStarted;
    
    public static event EventHandler<UnetConnectionMsg> ConnectionEvent;
    public static event EventHandler<UnetConnectionMsg> DisconnectionEvent;
    public static event EventHandler<UnetDataMsg> DataEvent;

    public class UnetClient
    {
        public int HostId;
        public int ConnectionId;
        public int ChannelId;
        public string ClientInfo;
    }
    public static List<UnetClient> Clients = new List<UnetClient>();

    public class UnetConnectionMsg : EventArgs
    {
        public int HostId;
        public int ConnectionId;
        public int ChannelId;
        public string ClientIp;
        public int ClientPort;

        public UnetConnectionMsg(int hostId, int connectionId, int channelId, string clientIp, int clientPort)
        {
            HostId = hostId;
            ConnectionId = connectionId;
            ChannelId = channelId;
            ClientIp = clientIp;
            ClientPort = clientPort;
        }
    }
    public class UnetDataMsg : EventArgs
    {
        public int HostId;
        public int ConnectionId;
        public int ChannelId;
        public string Msg;

        public UnetDataMsg(int hostId, int connectionId, int channelId, string msg)
        {
            HostId = hostId;
            ConnectionId = connectionId;
            ChannelId = channelId;
            Msg = msg;
        }
    }

    public static void StartUnetServer(int portNum=9696,int clientNum=5)
    {
        if(UnetServerStarted) return;
        NetworkTransport.Init();
        ConnectionConfig connectionConfig = new ConnectionConfig();
        myReliableChannelId = connectionConfig.AddChannel(QosType.Reliable);
        HostTopology hostTopology = new HostTopology(connectionConfig, clientNum);
        hostId = NetworkTransport.AddHost(hostTopology, portNum);

        ConnectionEvent += OnConnectionEvent;
        DisconnectionEvent += OnDisconnectionEvent;

        UnetServerStarted = true;
    }

    public static void StopUnetServer()
    {
        if (!UnetServerStarted) return;

        ConnectionEvent -= OnConnectionEvent;
        DisconnectionEvent -= OnDisconnectionEvent;

        UnetServerStarted = false;
        NetworkTransport.Shutdown();
    }

    public static void SendMsg(string msg)
    {
        byte error;
        byte[] buffer = System.Text.Encoding.Default.GetBytes(msg);
        int size = buffer.Length;
        foreach (var client in Clients)
        {
            NetworkTransport.Send(hostId, client.ConnectionId, myReliableChannelId, buffer, size, out error);
        }
    }

    public static void MultiSendMessage(string msg, int clientId = 0, bool self = false)
    {
        byte[] buffer = System.Text.Encoding.Default.GetBytes(msg);
        int size = buffer.Length;
        if (Clients == null) return;
        foreach (UnetClient item in Clients)
        {
            byte error;
            NetworkTransport.Send(hostId, item.ConnectionId, myReliableChannelId, buffer, size, out error);
        }
    }

    private static void OnConnectionEvent(object sender, UnetConnectionMsg e)
    {
        UnetClient client = new UnetClient
        {
            HostId = e.HostId,
            ConnectionId = e.ConnectionId,
            ChannelId = myReliableChannelId,
            ClientInfo = $"ID: {e.ConnectionId}   IP:{e.ClientIp}  Port:{e.ClientPort}"
        };
        Clients.Add(client);
    }

    private static void OnDisconnectionEvent(object sender, UnetConnectionMsg e)
    {
        foreach (UnetClient item in Clients)
        {
            if (item.ConnectionId == e.ConnectionId) Clients.Remove(item);
        }
    }

    void Update()
    {
        if (UnetServerStarted)
        {
            byte[] recBuffer = new byte[1024];
            int recHostId, connectionId, channelId, dataSize;
            byte error;
            NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, 1024, out dataSize, out error);
            switch (recData)
            {
                case NetworkEventType.Nothing:
                    break;
                case NetworkEventType.ConnectEvent:
                    NetworkID id;
                    NodeID dstNode;
                    string clientIp;
                    int port;
                    NetworkTransport.GetConnectionInfo(hostId, connectionId, out clientIp, out port, out id,
                        out dstNode, out error);
                    ConnectionEvent?.Invoke(null,
                        new UnetConnectionMsg(hostId, connectionId, channelId, clientIp.Split(':')[3], port));
                    break;

                case NetworkEventType.DataEvent:
                    string msg = System.Text.Encoding.Default.GetString(recBuffer);
                    DataEvent?.Invoke(null, new UnetDataMsg(hostId, connectionId, channelId, msg));
                    break;

                case NetworkEventType.DisconnectEvent:
                    DisconnectionEvent?.Invoke(null, new UnetConnectionMsg(hostId, connectionId, channelId, "", 0));
                    break;
            }
        }
    }
}

