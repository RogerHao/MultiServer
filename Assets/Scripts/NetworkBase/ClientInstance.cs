using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;

public class ClientInstance : MonoBehaviour {

    //Item
    public Button StateButton;
    public Text ClientInfoText;
    public InputField SendInputField;
    public Text RecText;
    public string RecString;
    public Button SendButton;
    public Button ClearButton;

    //Public Properties
    public bool IsActive = true;

    //Network Info and Settings
    public int HostId;
    public int ConnectionId;
    public int ChannelId;

    public string ClientInfo
    {
        get { return clientInfo; }
        set
        {
            clientInfo = value;
            ClientInfoText.text = clientInfo;
        }
    }
    private string clientInfo;
    private byte _error;
    private NetworkError _networkError;

    void Update()
    {
        RecText.text = RecString;
    }

    public void ChangeState()
    {
        IsActive = !IsActive;
        ColorBlock colorBlock = new ColorBlock {normalColor = IsActive ? new Color(102/255f,219/255f,146/255f,1) : Color.red, highlightedColor = IsActive ? new Color(102 / 255f, 219 / 255f, 146 / 255f, 1) : Color.red, pressedColor = IsActive ? new Color(102 / 255f, 219 / 255f, 146 / 255f, 1) : Color.red, colorMultiplier=1,fadeDuration=0.1f};
        StateButton.colors = colorBlock;
    }

    public void ClearData()
    {
        SendInputField.text = "";
        RecString = "";
    }

    public void SendData()
    {
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(SendInputField.text);
        int size = buffer.Length;
        if (size == 0) return;

        if (HostId == 100)
        {
            SocketServerBase.SendMessage(clientInfo.Split(':')[1], SendInputField.text);
        }
        else
        {
            NetworkTransport.Send(HostId, ConnectionId, ChannelId, buffer, size, out _error);
            _networkError = (NetworkError)_error;
        }
    }
}
