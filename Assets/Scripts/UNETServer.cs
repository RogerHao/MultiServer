using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;
using UnityEngine.UI;

public class UNETServer : MonoBehaviour {
    //UNET UDP Server Config
    private int hostId;
    private int myReliableChannelId;
    private bool UnetEnabled;

    public InputField UNETClientNum;
    public InputField UNETPort;
    public InputField UNETSendMsg;
    public Text UNETRecMsg;
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
    
    // Use this for initialization
    void Start ()
    {
        UNETClientNum.text = GetIp();

        this.ConnectionEvent += OnConnectionEvent;
        this.DisconnectionEvent += OnDisconnectionEvent;
        this.DataEvent += OnDataEvent;
    }
	// Update is called once per frame
	void Update () {
	    if (!UnetEnabled) return;
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

    public void StartUnetServer()
    {
        NetworkTransport.Init();
        ConnectionConfig connectionConfig = new ConnectionConfig();
        myReliableChannelId = connectionConfig.AddChannel(QosType.Reliable);
//        int clientNum;
//        int.TryParse(UNETClientNum.text, out clientNum);
        HostTopology hostTopology = new HostTopology(connectionConfig, 5);
        int portNum;
        if (!string.IsNullOrEmpty(UNETPort.text))
        {
            int.TryParse(UNETPort.text, out portNum);
        }
        else portNum = 9696;
        hostId = NetworkTransport.AddHost(hostTopology, portNum);
        DebugInfo.text = $"Init UNET Server: Port:{portNum}, CientsNum:{5}";
        UnetEnabled = true;
    }

    public void StopUnetServer()
    {
        NetworkTransport.Shutdown();
    }

    public void Broadcast(string msg)
    {
        _broadcastEnabled = !_broadcastEnabled;
    }

    public void UnetSend()
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
        DebugInfo.text = string.Format("new data: recHostId: {0}, connectionId: {1},channelId:{2},data: {3}", e.HostId, e.ConnectionId, e.ChannelId, e.Msg);
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
        DebugInfo.text =
            $"new connection: recHostId: {e.HostId}, connectionId: {e.ConnectionId},channelId:{e.ChannelId}, Ip: {e.ClientIp}, Port: {e.ClientPort}";
        ClientInstance client = Instantiate(Client, Clients.transform);
        client.ClientInfo = $"ID: {e.ConnectionId}   IP:{e.ClientIp}  Port:{e.ClientPort}";
        client.HostId = e.HostId;
        client.ConnectionId = e.ConnectionId;
        client.ChannelId = myReliableChannelId;
        _clientObjects = Clients.GetComponentsInChildren<ClientInstance>();
    }

    private string GetIp()
    {
        string name = Dns.GetHostName();
        IPAddress[] ipadrlist = Dns.GetHostAddresses(name);
        foreach (IPAddress ipa in ipadrlist)
        {
            if (ipa.AddressFamily == AddressFamily.InterNetwork)
            {
                return ipa.ToString();
            }
        }
        return "";
    }
}
