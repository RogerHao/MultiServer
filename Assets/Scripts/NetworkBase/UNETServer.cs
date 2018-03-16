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
    public InputField IpInputField;
    public InputField PortInputField;
    public InputField SendMsgInputField;
    public Text RevText;
    private string RevString;

    public Text LogText;
    private string LogString;

    public int ClientCount => _clientObjects.Length;
    public GameObject Clients;
    public ClientInstance client;
    private ClientInstance[] _clientObjects;
    private List<UnetServerBase.UnetClient> UnetClients = new List<UnetServerBase.UnetClient>();
    private bool started = false;

    void Start()
    {
        IpInputField.text = GetIp();

        UnetServerBase.ConnectionEvent += UnetServerBase_ConnectionEvent;
        UnetServerBase.DisconnectionEvent += UnetServerBase_DisconnectionEvent;
        UnetServerBase.DataEvent += UnetServerBase_DataEvent;
    }

    private void UnetServerBase_DataEvent(object sender, UnetServerBase.UnetDataMsg e)
    {
        RevString = e.Msg;
        LogString = $"Msg: {e.Msg} From: {e.ConnectionId}!";
        foreach (var client in _clientObjects)
        {
            if (client.ConnectionId == e.ConnectionId)
                client.RecString = e.Msg;
        }
    }

    private void UnetServerBase_DisconnectionEvent(object sender, UnetServerBase.UnetConnectionMsg e)
    {
        LogString = $"Client {e.ConnectionId} Disconnect!";
    }

    private void UnetServerBase_ConnectionEvent(object sender, UnetServerBase.UnetConnectionMsg e)
    {
        LogString = $"Client {e.ConnectionId} Connect!";
    }

    void Update()
    {
        LogText.text = LogString;
        RevText.text = RevString;

        for (var index = 0; index < UnetServerBase.Clients.Count; index++)
        {
            var itemClient = UnetServerBase.Clients[index];
            try
            {
                if (UnetClients.Contains(itemClient)) continue;
                UnetClients.Add(itemClient);
                ClientInstance newClientInstance = Instantiate(client, Clients.transform);
                newClientInstance.ClientInfo = itemClient.ClientInfo;
                newClientInstance.HostId = itemClient.HostId;
                newClientInstance.ConnectionId = itemClient.ConnectionId;
                _clientObjects = Clients.GetComponentsInChildren<ClientInstance>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        for (var index = 0; index < UnetClients.Count; index++)
        {
            var itemClient = UnetClients[index];
            try
            {
                if (UnetServerBase.Clients.Contains(itemClient)) return;
                for (int i = 0; i < _clientObjects.Length; i++)
                {
                    if (UnetClients[i].ClientInfo != itemClient.ClientInfo) continue;
                    if (_clientObjects[i].gameObject != null) Destroy(_clientObjects[i].gameObject);
                }

                UnetClients.Remove(itemClient);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    public void SendMsg()
    {
        if (string.IsNullOrEmpty(SendMsgInputField.text)) return;
        UnetServerBase.SendMsg(SendMsgInputField.text);
    }

    public void SendMsg(string msg)
    {
        UnetServerBase.SendMsg(msg);
    }

    public void ClearSend()
    {
        SendMsgInputField.text = "";
        //        RevText.text = "";
    }

    public void ClearRec()
    {
        //        SendMsgInputField.text = "";
        RevString = "";
    }

    public void StartUnet()
    {
        if (started)
        {
            LogString = "Unet Started";
            return;
        }

        int port;
        int.TryParse(PortInputField.text, out port);
        UnetServerBase.StartUnetServer(port);
        started = true;

        LogString = "Unet Start";
    }

    public void StopUnet()
    {
        if (!started)
        {
            LogString = "Unet Stopped";
            return;
        }

        UnetServerBase.StopUnetServer();
        started = false;

        LogString = "Unet Stop";
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
