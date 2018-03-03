using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//UNET
using UnityEngine.Networking;
using UnityEngine.Networking.Types;
//UDP Socket
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class Server : MonoBehaviour {

    //UNET UDP Server Config
    private int hostId;
    private int myReliableChannelId;
    private bool UnetEnabled;

    //Socket UDP Server Config
    Socket socket;
    EndPoint clientEnd;
    IPEndPoint ipEnd;
    string recvStr;  
    Thread connectThread;

    //UI
    public InputField UNETIP;
    public InputField SocketIP;
    public InputField UNETPort;
    public InputField SocketPort;

    public InputField UNETSendMsg;
    public InputField SocketSendMsg;
    public Text UNETRecMsg;
    public Text SocketRecMsg;

    public Text DebugInfo;

    //Clients
    public GameObject Clients;
    public ClientInstance Client;
    private ClientInstance[] _clientObjects;

    private bool _broadcastEnabled = true;

    //Delegates
    public event EventHandler<ConnectionMsg> ConnectionEvent;
    public event EventHandler<ConnectionMsg> DisconnectionEvent;
    public event EventHandler<DataMsg> DataEvent;
    public class ConnectionMsg : EventArgs
    {
        public int HostId;
        public int ConnectionId;
        public int ChannelId;
        public string ClientIp;
        public int ClientPort;

        public ConnectionMsg(int hostId, int connectionId, int channelId, string clientIp, int clientPort)
        {
            HostId = hostId;
            ConnectionId = connectionId;
            ChannelId = channelId;
            ClientIp = clientIp;
            ClientPort = clientPort;
        }
    }
    public class DataMsg : EventArgs
    {
        public int HostId;
        public int ConnectionId;
        public int ChannelId;
        public string Msg;

        public DataMsg(int hostId, int connectionId, int channelId, string msg)
        {
            HostId = hostId;
            ConnectionId = connectionId;
            ChannelId = channelId;
            Msg = msg;
        }
    }

    void Start()
    {
        NetworkTransport.Init();

        this.ConnectionEvent += OnConnectionEvent;
        this.DisconnectionEvent += OnDisconnectionEvent;
        this.DataEvent += OnDataEvent;
    }

    void Update()
    {
        if (!string.IsNullOrEmpty(recvStr))
        {
            this.MultiSendMessage(recvStr);
            SocketRecMsg.text = recvStr;
            recvStr = "";
        }

        if (!UnetEnabled) return;
        byte[] recBuffer = new byte[1024];
        int recHostId, connectionId, channelId,dataSize;
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
                NetworkTransport.GetConnectionInfo(hostId, connectionId, out clientIp, out port, out id, out dstNode, out error);

                if (ConnectionEvent != null)
                    ConnectionEvent(this, new ConnectionMsg(hostId, connectionId, channelId, clientIp.Split(':')[3], port));
                break;

            case NetworkEventType.DataEvent:
                string msg = System.Text.Encoding.Default.GetString(recBuffer);
                if (DataEvent != null) DataEvent(this, new DataMsg(hostId, connectionId, channelId, msg));
                break;

            case NetworkEventType.DisconnectEvent:
                if (DisconnectionEvent != null)
                    DisconnectionEvent(this, new ConnectionMsg(hostId, connectionId, channelId, "", 0));
                break;
        }

        
    }

    #region Socket

    public void StartSocketServer()
    {
        int portNum;
        if (!string.IsNullOrEmpty(SocketPort.text))
        {
            int.TryParse(SocketPort.text, out portNum);
        }
        else portNum = 9695;
        System.Net.IPAddress ipaddress = System.Net.IPAddress.Parse(SocketIP.text);
        ipEnd = new IPEndPoint(ipaddress, portNum);
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(ipEnd);
        DebugInfo.text=string.Format("Start Socket Server IP:{0}, Port:{1}", ipEnd.Address,ipEnd.Port);

        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        clientEnd = (EndPoint)sender;
        connectThread = new Thread(new ThreadStart(SocketReceive));
        connectThread.Start();
    }

    void SocketReceive()
    {
        while (true)
        {
            var recvData = new byte[1024];
            var recvLen = socket.ReceiveFrom(recvData, ref clientEnd);
            recvStr = Encoding.ASCII.GetString(recvData, 0, recvLen);
        }
    }

    public void SocketSend()
    {
        SocketSend(SocketSendMsg.text);
    }

    void SocketSend(string sendStr)
    {
        var sendData = new byte[1024];
        sendData = Encoding.ASCII.GetBytes(sendStr);
        socket.SendTo(sendData, sendData.Length, SocketFlags.None, clientEnd);
    }

    void SocketQuit()
    {
        if (connectThread != null)
        {
            connectThread.Interrupt();
            connectThread.Abort();
        }
        if (socket != null)
            socket.Close();
    }
    #endregion

    #region UNET
    public void StartUNETServer()
    {
        ConnectionConfig connectionConfig = new ConnectionConfig();
        myReliableChannelId = connectionConfig.AddChannel(QosType.Reliable);
        HostTopology hostTopology = new HostTopology(connectionConfig, 5);
        int portNum;
        if (!string.IsNullOrEmpty(UNETPort.text))
        {
            int.TryParse(UNETPort.text, out portNum);
        }
        else portNum = 9696;
        hostId = NetworkTransport.AddHost(hostTopology, portNum);
        UnetEnabled = true;
    }

    public void Broadcast(string msg)
    {
        _broadcastEnabled = !_broadcastEnabled;
    }

    public void UNETSend()
    {
        MultiSendMessage(UNETSendMsg.text);
    }

    public void SendMessage(int client, string msg)
    {
        byte error;
        byte[] buffer = System.Text.Encoding.Default.GetBytes(msg);
        int size = buffer.Length;
        NetworkTransport.Send(hostId, client, myReliableChannelId, buffer, size, out error);
    }

    public void MultiSendMessage(string msg, int clientId = 0, bool self = false)
    {
        byte[] buffer = System.Text.Encoding.Default.GetBytes(msg);
        int size = buffer.Length;
        if (_clientObjects == null) return;
        foreach (ClientInstance item in _clientObjects)
        {
            byte error;
            if (item.IsActive) NetworkTransport.Send(hostId, item.ConnectionId, myReliableChannelId, buffer, size, out error);
        }
    }

    private void OnDataEvent(object sender, DataMsg e)
    {
        DebugInfo.text=string.Format("new data: recHostId: {0}, connectionId: {1},channelId:{2},data: {3}", e.HostId, e.ConnectionId, e.ChannelId, e.Msg);
        foreach (ClientInstance item in _clientObjects)
        {
            if (item.ConnectionId == e.ConnectionId) item.RecText.text = e.Msg;
        }
        if (_broadcastEnabled) MultiSendMessage(e.Msg);
    }

    private void OnDisconnectionEvent(object sender, ConnectionMsg e)
    {
        DebugInfo.text = string.Format("disconnection: recHostId:{0}, connectionId:{1},channelId:{2}", e.HostId,
            e.ConnectionId, e.ChannelId);
        foreach (ClientInstance item in _clientObjects)
        {
            if (item.ConnectionId == e.ConnectionId) Destroy(item.gameObject);
        }
    }

    private void OnConnectionEvent(object sender, ConnectionMsg e)
    {
        DebugInfo.text=string.Format("new connection: recHostId: {0}, connectionId: {1},channelId:{2}, Ip: {3}, Port: {4}", e.HostId, e.ConnectionId, e.ChannelId, e.ClientIp, e.ClientPort);
        ClientInstance client = Instantiate(Client, Clients.transform);
        client.ClientInfo = string.Format("ID: {0}   IP:{1}  Port:{2}", e.ConnectionId, e.ClientIp, e.ClientPort);
        client.HostId = e.HostId;
        client.ConnectionId = e.ConnectionId;
        client.ChannelId = myReliableChannelId;
        _clientObjects = Clients.GetComponentsInChildren<ClientInstance>();
    }
#endregion

    private void OnApplicationQuit()
    {
        NetworkTransport.Shutdown();
        SocketQuit();
    }

}
