using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;
using UnityEngine.UI;


public class test : MonoBehaviour {

    private int hostId;
    private int recHostId;
    private int connectionId;
    private int channelId;
    private int myReliableChannelId;
    private string clientIp;
    private int port;
    private byte[] recBuffer = new byte[1024];
    private int bufferSize = 1024;
    private int dataSize;
    private byte error;

    public InputField PortInputField;

    public GameObject Clients;
    public ClientInstance Client;
    public List<ClientInstance> ClientsList;
    private ClientInstance[] _clientObjects;

    private bool _broadcastEnabled = true;


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

        public DataMsg(int hostId, int connectionId, int channelId,string msg)
        {
            HostId = hostId;
            ConnectionId = connectionId;
            ChannelId = channelId;
            Msg = msg;
        }
    }
    // Use this for initialization

    void Start()
    {
        NetworkTransport.Init();

        this.ConnectionEvent += Test_ConnectionEvent;
        this.DisconnectionEvent += Test_DisconnectionEvent;
        this.DataEvent += Test_DataEvent;
    }

    public void StartServer()
    {
        ConnectionConfig connectionConfig = new ConnectionConfig();
        myReliableChannelId = connectionConfig.AddChannel(QosType.Reliable);
        HostTopology hostTopology = new HostTopology(connectionConfig, 5);
        int PortNum;
        int.TryParse(PortInputField.text, out PortNum);
        hostId = NetworkTransport.AddHost(hostTopology, PortNum);
    }


    // Update is called once per frame
    void Update()
    {
        recBuffer = new byte[1024];
        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
        switch (recData)
        {
            case NetworkEventType.Nothing:
                break;
            case NetworkEventType.ConnectEvent:
                NetworkID id;
                NodeID dstNode;
                try
                {
                    NetworkTransport.GetConnectionInfo(hostId, connectionId, out clientIp, out port, out id,out dstNode,out error);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                if (ConnectionEvent != null)
                    ConnectionEvent(this, new ConnectionMsg(hostId, connectionId, channelId, clientIp.Split(':')[3], port));
                break;

            case NetworkEventType.DataEvent:
                string msg = System.Text.Encoding.Default.GetString(recBuffer);
                if (DataEvent != null) DataEvent(this, new DataMsg(hostId, connectionId,channelId,msg));
                break;

            case NetworkEventType.DisconnectEvent:
                if (DisconnectionEvent != null)
                    DisconnectionEvent(this, new ConnectionMsg(hostId, connectionId, channelId, "", 0));
                break;
        }
    }

    public void SendMessage(int client,string msg)
    {
        byte[] buffer = System.Text.Encoding.Default.GetBytes(msg);
        int size = buffer.Length;
        NetworkTransport.Send(hostId, client, myReliableChannelId, buffer, size, out error);
        Debug.Log(error);
    }

    public void MultiSendMessage(string msg,int clientId = 0,bool self = false)
    {
        byte[] buffer = System.Text.Encoding.Default.GetBytes(msg);
        int size = buffer.Length;
        foreach (ClientInstance item in _clientObjects)
        {
            if (item.IsActive) NetworkTransport.Send(hostId, item.ConnectionId, myReliableChannelId, buffer, size, out error);
        }
    }

    private void Test_DataEvent(object sender, DataMsg e)
    {
        Debug.Log(string.Format("new data: recHostId: {0}, connectionId: {1},channelId:{2},data: {3}", e.HostId, e.ConnectionId, e.ChannelId, e.Msg));
        //throw new NotImplementedException();
        foreach (ClientInstance item in _clientObjects)
        {
            if (item.ConnectionId == e.ConnectionId) item.RecText.text = e.Msg;
        }
        if (_broadcastEnabled) MultiSendMessage(e.Msg);
    }

    private void Test_DisconnectionEvent(object sender, ConnectionMsg e)
    {
        Debug.Log(string.Format("disconnection: recHostId:{0}, connectionId:{1},channelId:{2}", e.HostId, e.ConnectionId, e.ChannelId));
        //throw new NotImplementedException();
        for (int i = 0; i < ClientsList.Count; i++)
        {
            if (ClientsList[i].ConnectionId == e.ConnectionId) ClientsList.RemoveAt(i);
        }
        foreach (ClientInstance item in _clientObjects)
        {
            if (item.ConnectionId == e.ConnectionId) Destroy(item.gameObject);
        }
    }

    private void Test_ConnectionEvent(object sender, ConnectionMsg e)
    {
        Debug.Log(string.Format("new connection: recHostId: {0}, connectionId: {1},channelId:{2}, Ip: {3}, Port: {4}", e.HostId, e.ConnectionId, e.ChannelId, e.ClientIp, e.ClientPort));
        //throw new NotImplementedException();
        ClientInstance client = Instantiate(Client, Clients.transform);
        client.ClientInfo = string.Format("ID: {0}   IP:{1}  Port:{2}", e.ConnectionId, e.ClientIp, e.ClientPort);
        client.HostId = e.HostId;
        client.ConnectionId = e.ConnectionId;
        client.ChannelId = myReliableChannelId;
        ClientsList.Add(client);
        _clientObjects = Clients.GetComponentsInChildren<ClientInstance>();
    }

    public void Broadcast(string msg)
    {
        _broadcastEnabled = !_broadcastEnabled;
    }

    private void OnApplicationQuit()
    {
        NetworkTransport.Shutdown();
    }
}
