using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(HandPhysicsController))]
[RequireComponent(typeof(MyHandController))]
[RequireComponent(typeof(UnetClientBase))]
public class TrainController : MonoBehaviour
{
    public string Ip;
    public int Port;

    // Use this for initialization
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
	    myHandController = gameObject.GetComponent<MyHandController>();
	    unetClientBase = gameObject.GetComponent<UnetClientBase>();

	    unetClientBase.ConnectionEvent += UnetClientBase_ConnectionEvent;
	    unetClientBase.DisconnectionEvent += UnetClientBase_DisconnectionEvent;
	    unetClientBase.DataEvent += UnetClientBase_DataEvent;

	    StartCoroutine(ConnectedToServer());
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
            case 10:
                IntroText.text = "The Training Process is Starting";
                break;
	        case 100:
	            myHandController.Rest();
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
	            myHandController.Rest();
	            break;
            default:
                IntroText.text = $"Msg is: {_serverCommand}";
                break;
        }
    }

    public IEnumerator ConnectedToServer()
    {
        yield return new WaitForSeconds(1);
        unetClientBase.ConnectToServer(Ip, Port);
        var count = 0;
        while (count <10)
        {
            yield return new WaitForSeconds(10);
            if (!_connected)
            {
                unetClientBase.ConnectToServer(Ip, Port);
                yield break;
            }
            count++;
        }
    }

    public void DisConnectedToServer()
    {
        unetClientBase.DisconnectToServer();
    }

    public void SendMessageToServer()
    {
        unetClientBase.SendMessageToServer("");
    }
}
