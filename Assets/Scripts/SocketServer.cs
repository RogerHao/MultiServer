using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UI;

public class SocketServer : MonoBehaviour {
    Socket socket;
    EndPoint clientEnd;
    IPEndPoint ipEnd;
    string recvStr;
    private CancellationTokenSource socketCancellationTokenSource = new CancellationTokenSource();

    public InputField SocketIP;
    public InputField SocketPort;
    public InputField SocketSendMsg;
    public Text SocketRecMsg;
    public Text DebugInfo;

    public GameObject Clients;
    public ClientInstance Client;
    private ClientInstance[] _clientObjects;

    // Use this for initialization
    void Start ()
    {
        socketCancellationTokenSource.Cancel();
    }
	// Update is called once per frame
	void Update () {
		
	}

    public void StartSocketServer()
    {
        if(!socketCancellationTokenSource.IsCancellationRequested) return;
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
        DebugInfo.text = string.Format("Start Socket Server IP:{0}, Port:{1}", ipEnd.Address, ipEnd.Port);

        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        clientEnd = (EndPoint)sender;

        socketCancellationTokenSource = new CancellationTokenSource();
        Task.Run(() => SocketReceive(), socketCancellationTokenSource.Token);
    }

    private void SocketReceive()
    {
        while (!socketCancellationTokenSource.IsCancellationRequested)
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

    public void SocketSend(string sendStr)
    {
        var sendData = new byte[1024];
        sendData = Encoding.ASCII.GetBytes(sendStr);
        socket.SendTo(sendData, sendData.Length, SocketFlags.None, clientEnd);
    }

    public void StopSocketServer()
    {
        if (socketCancellationTokenSource.IsCancellationRequested) return;
        socketCancellationTokenSource.Cancel();
        socket.Close();
    }

    private void OnApplicationQuit()
    {
        StopSocketServer();
    }
}
