using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Client : MonoBehaviour
{
    private int hostId;
    private int recHostId;
    private int connectionId;
    private int myConnectionId;
    private int channelId;
    private int myReliableChannelId;
    private byte[] recBuffer = new byte[1024];
    private int bufferSize = 1024;
    private int dataSize;
    private byte error;

    public InputField InputField;
    public Text recText;
        
    // Use this for initialization
    void Start () {
        NetworkTransport.Init();
    }

    public void StartClient()
    {
        ConnectionConfig connectionConfig = new ConnectionConfig();
        myReliableChannelId = connectionConfig.AddChannel(QosType.Reliable);
        HostTopology hostTopology = new HostTopology(connectionConfig, 2);
        hostId = NetworkTransport.AddHost(hostTopology);
        myConnectionId = NetworkTransport.Connect(hostId, "192.168.1.100", 9696, 0, out error);
        Debug.Log(myConnectionId);
    }
	// Update is called once per frame
	void Update ()
	{
	    recBuffer = new byte[1024];
        NetworkEventType recData = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error);
	    switch (recData)
	    {
	        case NetworkEventType.Nothing:
	            break;
	        case NetworkEventType.ConnectEvent:
	            Debug.Log(string.Format("new connection: recHostId:{0}, connectionOId:{1},channelId:{2},error:{3}", recHostId, connectionId, channelId, error));
	            break;
	        case NetworkEventType.DataEvent:
	            Debug.Log(string.Format("new data: recHostId:{0}, connectionOId:{1},channelId:{2},data:{3},error:{4}", recHostId, connectionId, channelId, System.Text.Encoding.UTF8.GetString(recBuffer), error));
	            recText.text = System.Text.Encoding.UTF8.GetString(recBuffer);
                break;
	        case NetworkEventType.DisconnectEvent:
	            Debug.Log(string.Format("disconnection: recHostId:{0}, connectionOId:{1},channelId:{2},error:{3}", recHostId, connectionId, channelId, error));
	            break;
	    }
    }

    public void SendMessage()
    {
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(InputField.text);
        int size = buffer.Length;
        NetworkTransport.Send(hostId, myConnectionId, myReliableChannelId, buffer, size, out error);
        Debug.Log(error);
    }

    public void DisconnectClient()
    {
        NetworkTransport.Disconnect(hostId, myConnectionId, out error);
        Debug.Log(error);
    }

    private void OnApplicationQuit()
    {
        NetworkTransport.Shutdown();
    }
}
