using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UI;

public class SocketServer : MonoBehaviour
{
    public InputField IpInputField;
    public InputField PortInputField;
    public InputField SendMsgInputField;
    public Text RevText;
    private string RevString;

    public Text LogText;
    private string LogString;

    public GameObject Clients;
    public ClientInstance client;
    private ClientInstance[] _clientObjects;
    private List<SocketServerBase.SocketClient> SocketClients = new List<SocketServerBase.SocketClient>();

    private bool started = false;

    void Start()
    {
        IpInputField.text = GetIp();
        
        SocketServerBase.ConnectionEvent += SocketServerBaseOnConnectionEvent;
        SocketServerBase.DisconnectionEvent += SocketServerBase_DisconnectionEvent;
        SocketServerBase.DataEvent += SocketServerBaseOnDataEvent;
    }

    void Update()
    {
        if (!started) return;

        LogText.text = LogString;
        RevText.text = RevString;
        
        foreach (var itemClient in SocketServerBase.Clients)
        {
            if (SocketClients.Contains(itemClient)) continue;
            SocketClients.Add(itemClient);
            ClientInstance newClientInstance = Instantiate(client, Clients.transform);
            newClientInstance.ClientInfo = itemClient.ClientInfo;
            newClientInstance.HostId = 100;
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

    private void SocketServerBaseOnDataEvent(object sender, SocketServerBase.SocketDataMsg socketDataMsg)
    {
        RevString = socketDataMsg.Msg;
        LogString = $"Msg: {socketDataMsg.Msg} From: {socketDataMsg.ClientInfo}!";
        foreach (var client in _clientObjects)
        {
            if(client.ClientInfo == socketDataMsg.ClientInfo)
            client.RecString = socketDataMsg.Msg;
        }
    }

    private void SocketServerBase_DisconnectionEvent(object sender, SocketServerBase.SocketConnectionMsg e)
    {
        LogString = $"Client {e.ConnectionInfo} Connect!";
    }

    private void SocketServerBaseOnConnectionEvent(object sender, SocketServerBase.SocketConnectionMsg e)
    {
        LogString = $"Client {e.ConnectionInfo} Disconnect!";
    }

    public void SendMsg()
    {
        if(string.IsNullOrEmpty(SendMsgInputField.text)) return;
        SocketServerBase.MultiSendMessage(SocketServerBase.Clients,SendMsgInputField.text);
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

    public void StartSocket()
    {
        if (started)
        {
            LogString = "Socket Started";
            return;
        }
        
        int port;
        int.TryParse(PortInputField.text, out port);
        SocketServerBase.StartSocketServer(port);
        started = true;

        LogString = "Socket Start";
    }

    public void StopSocket()
    {
        if (!started)
        {
            LogString = "Socket Stopped";
            return;
        }

        SocketServerBase.StopSocketServer();
        started = false;

        LogString = "Socket Stop";
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
