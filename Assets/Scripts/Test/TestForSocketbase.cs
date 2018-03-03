using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestForSocketbase : MonoBehaviour {

    public GameObject Clients;
    public ClientInstance client;
    private ClientInstance[] _clientObjects;
    private List<SocketServerBase.SocketClient> SocketClients = new List<SocketServerBase.SocketClient>();

    private bool started = false;

    void Start()
    {
        SocketServerBase.ConnectionEvent += UnetServerBaseOnConnectionEvent;
        SocketServerBase.DisconnectionEvent += UnetServerBase_DisconnectionEvent;
        SocketServerBase.DataEvent += SocketServerBaseOnDataEvent;
    }

    private void SocketServerBaseOnDataEvent(object sender, SocketServerBase.SocketDataMsg socketDataMsg)
    {
        Debug.Log($"ClientInfo: {socketDataMsg.ClientInfo}, Msg: {socketDataMsg.Msg}");
    }

    private void UnetServerBase_DisconnectionEvent(object sender, SocketServerBase.SocketConnectionMsg e)
    {
        Debug.Log(e.ConnectionInfo);
//        foreach (var itemClient in SocketClients)
//        {
//            if (SocketServerBase.Clients.Contains(itemClient)) return;
//            SocketClients.Remove(itemClient);
//            foreach (ClientInstance item in _clientObjects)
//            {
//                if (item.ClientInfo == itemClient.ClientInfo) Destroy(item.gameObject);
//            }
//        }
    }

    private void UnetServerBaseOnConnectionEvent(object sender, SocketServerBase.SocketConnectionMsg e)
    {
        Debug.Log(e.ConnectionInfo);
//        foreach (var itemClient in SocketServerBase.Clients)
//        {
//            if (SocketClients.Contains(itemClient)) continue;
//            SocketClients.Add(itemClient);
//            Instantiate(client, Clients.transform);
//            client.ClientInfo = e.ConnectionInfo;
//            _clientObjects = Clients.GetComponentsInChildren<ClientInstance>();
//        }
    }

    void Update()
    {
        if (!started) return;
        foreach (var itemClient in SocketServerBase.Clients)
        {
            if (SocketClients.Contains(itemClient)) continue;
            SocketClients.Add(itemClient);
            Instantiate(client, Clients.transform);
            client.ClientInfo = itemClient.ClientInfo;
            _clientObjects = Clients.GetComponentsInChildren<ClientInstance>();
        }
        foreach (var itemClient in SocketClients)
        {
            if (SocketServerBase.Clients.Contains(itemClient)) return;
            for (int i = 0; i < _clientObjects.Length; i++)
            {
                if (SocketClients[i].ClientInfo == itemClient.ClientInfo) Destroy(_clientObjects[i].gameObject);
            }
            SocketClients.Remove(itemClient);
        }

    }

    public void StartUnet()
    {
        SocketServerBase.StartSocketServer(9695);
        started = true;
    }

    public void Stop()
    {
        SocketServerBase.MultiSendMessage(SocketServerBase.Clients,"hahaha");
    }
}
