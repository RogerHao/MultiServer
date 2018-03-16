using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


[RequireComponent(typeof(UnetClientBase))]
public class TrainController : MonoBehaviour
{
    private string Ip;
    public int Port;

    // Use this for initialization
    public GameObject HandContainner;
    public GameObject HandPrefab;
    private MyHandController myHandController;
    private UnetClientBase unetClientBase;

    private bool _connected = false;
    private int _serverCommand = 0;
    private bool _serverCommandNew = false;

    /// <summary>
    /// UI
    /// </summary>
    public Text IntroText;

	void Start ()
	{
	    Ip = GetIp();
	    myHandController = HandContainner.GetComponentInChildren<MyHandController>();
	    unetClientBase = gameObject.GetComponent<UnetClientBase>();

	    unetClientBase.ConnectionEvent += UnetClientBase_ConnectionEvent;
	    unetClientBase.DisconnectionEvent += UnetClientBase_DisconnectionEvent;
	    unetClientBase.DataEvent += UnetClientBase_DataEvent;
	}

    private void UnetClientBase_DataEvent(object sender, UnetClientBase.UnetDataMsg e)
    {
        var parse = int.TryParse(e.Msg, out _serverCommand);
        _serverCommandNew = parse;
    }

    private void UnetClientBase_DisconnectionEvent(object sender, UnetClientBase.UnetConnectionMsg e)
    {
        
    }

    private void UnetClientBase_ConnectionEvent(object sender, UnetClientBase.UnetConnectionMsg e)
    {
        _connected = true;
    }

    // Update is called once per frame
	void Update () {
	    if (!_connected) return;
        if(!_serverCommandNew) return;
	    _serverCommandNew = false;
	    switch (_serverCommand)
	    {
	        case 100:
	            myHandController.IResetHand();
	            IntroText.text = myHandController.GestureIntro[MyHandController.GestureState.NoMovement];
                break;
	        case 101:
	            IntroText.text = myHandController.GestureIntro[MyHandController.GestureState.Close];
	            myHandController.IClose();
	            break;
	        case 102:
	            IntroText.text = myHandController.GestureIntro[MyHandController.GestureState.Open];
	            myHandController.IOpen();
	            break;
	        case 103:
	            IntroText.text = myHandController.GestureIntro[MyHandController.GestureState.Supination];
	            myHandController.ISupination();
	            break;
	        case 104:
	            IntroText.text = myHandController.GestureIntro[MyHandController.GestureState.Pronation];
	            myHandController.IPronation();
	            break;
	        case 105:
	            IntroText.text = myHandController.GestureIntro[MyHandController.GestureState.Flexion];
	            myHandController.IFlexion();
	            break;
	        case 106:
	            IntroText.text = myHandController.GestureIntro[MyHandController.GestureState.Extension];
	            myHandController.IExtension();
	            break;
	        case 1010:
	            IntroText.text = "The Trainning Process is Over, Waitting for next process";
	            myHandController.IResetHand();
	            break;
	        case 1011:
	            StartTestScene();
	            break;
            default:
                break;
        }
    }

    public void ConnectedToServer()
    {
        unetClientBase.ConnectToServer(Ip, Port);
    }

    public void DisConnectedToServer()
    {
        unetClientBase.DisconnectToServer();
    }

    public void SendMessageToServer(string msg)
    {
        unetClientBase.SendMessageToServer(msg);
    }

    public void StartTestScene()
    {
        DisConnectedToServer();
        SceneManager.LoadScene("UNETClienTest");
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

