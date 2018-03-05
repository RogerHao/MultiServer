using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestForUnetInterface : MonoBehaviour {

    public GameObject Clients;
    public ClientInstance client;
    private ClientInstance[] _clientObjects;
    private List<UnetServerBase.UnetClient> UnetClients = new List<UnetServerBase.UnetClient>();

    private bool started = false;

    void Start()
    {
        UnetServerBase.ConnectionEvent += UnetServerBaseOnConnectionEvent;
        UnetServerBase.DisconnectionEvent += UnetServerBase_DisconnectionEvent;
    }

    private void UnetServerBase_DisconnectionEvent(object sender, UnetServerBase.UnetConnectionMsg e)
    {
        foreach (var itemClient in UnetClients)
        {
            if (UnetServerBase.Clients.Contains(itemClient)) return;
            UnetClients.Remove(itemClient);
            foreach (ClientInstance item in _clientObjects)
            {
                if (item.ConnectionId == itemClient.ConnectionId) Destroy(item.gameObject);
            }
        }
    }

    private void UnetServerBaseOnConnectionEvent(object sender, UnetServerBase.UnetConnectionMsg unetConnectionMsg)
    {
        foreach (var itemClient in UnetServerBase.Clients)
        {
            if (UnetClients.Contains(itemClient)) continue;
            UnetClients.Add(itemClient);
            Instantiate(client, Clients.transform);
            _clientObjects = Clients.GetComponentsInChildren<ClientInstance>();
        }
    }

    public void StartUnet()
    {
        UnetServerBase.StartUnetServer(9696,5);
        started = true;
    }

    public void Stop()
    {
        UnetServerBase.StopUnetServer();
        started = false;
    }

    void Update()
    {
        if(!started) return;
        foreach (var itemClient in UnetServerBase.Clients)
        {
            if (UnetClients.Contains(itemClient)) continue;
            UnetClients.Add(itemClient);
            Instantiate(client, Clients.transform);
            _clientObjects = Clients.GetComponentsInChildren<ClientInstance>();
        }
        foreach (var itemClient in UnetClients)
        {
            if(UnetServerBase.Clients.Contains(itemClient)) return;
            UnetClients.Remove(itemClient);
            foreach (ClientInstance item in _clientObjects)
            {
                if (item.ConnectionId == itemClient.ConnectionId) Destroy(item.gameObject);
            }
        }
    }



}
