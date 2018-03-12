using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class UNETClient : MonoBehaviour {

    public InputField IpInput;
    public InputField PortInput;
    private int port;
    public InputField MsgInput;
    public Text MsgRevText;

    public Text Log;

    
    public Image[] GestImages;

    private string LogString { get; set; }
    private UnetClientBase unetClientBase;


    // Use this for initialization
    void Start () {
        unetClientBase = gameObject.GetComponent<UnetClientBase>();
        Debug.Log(GetIp());
        IpInput.text = GetIp();
        int.TryParse(PortInput.text, out port);
        if (port < 1000) port = 9696;

        unetClientBase.ConnectionEvent += UnetClientBase_ConnectionEvent;
        unetClientBase.DisconnectionEvent += UnetClientBase_DisconnectionEvent;
        unetClientBase.DataEvent += UnetClientBase_DataEvent;
    }

    private void UnetClientBase_DataEvent(object sender, UnetClientBase.UnetDataMsg e)
    {
        MsgRevText.text = e.Msg;
        LogString = $"Msg: {e.Msg} From: {e.ConnectionId}!";
        foreach (var img in GestImages)
        {
            if (MsgRevText.text.Contains(img.gameObject.name)) img.gameObject.SetActive(true);
            else img.gameObject.SetActive(false);
        }
    }

    private void UnetClientBase_DisconnectionEvent(object sender, UnetClientBase.UnetConnectionMsg e)
    {
        LogString = $"Server {e.ConnectionId} Disconnect!";
    }

    private void UnetClientBase_ConnectionEvent(object sender, UnetClientBase.UnetConnectionMsg e)
    {
        LogString = $"Server {e.ConnectionId} Connect!";
    }

    // Update is called once per frame
    void Update()
    {
        Log.text = LogString;
    }
    public void ConnectedToServer()
    {
        unetClientBase.ConnectToServer(IpInput.text,port);
    }

    public void DisConnectedToServer()
    {
        unetClientBase.DisconnectToServer();
    }

    public void SendMessageToServer()
    {
        unetClientBase.SendMessageToServer(MsgInput.text);
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
