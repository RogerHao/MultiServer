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
	        case 0:
	            myHandController.Rest();
	            IntroText.text = myHandController.GestureIntro[MyHandController.GestureState.NoMovement];
                break;
	        case 1:
	        case 11:
	            IntroText.text = myHandController.GestureIntro[MyHandController.GestureState.Close];
	            myHandController.IClose();
	            break;
	        case 2:
	        case 22:
	            IntroText.text = myHandController.GestureIntro[MyHandController.GestureState.Open];
	            myHandController.IOpen();
	            break;
	        case 3:
	        case 33:
	            IntroText.text = myHandController.GestureIntro[MyHandController.GestureState.Supination];
	            myHandController.ISupination();
	            break;
	        case 4:
	        case 44:
	            IntroText.text = myHandController.GestureIntro[MyHandController.GestureState.Pronation];
	            myHandController.IPronation();
	            break;
	        case 5:
	        case 55:
	            IntroText.text = myHandController.GestureIntro[MyHandController.GestureState.Flexion];
	            myHandController.IFlexion();
	            break;
	        case 6:
	        case 66:
	            IntroText.text = myHandController.GestureIntro[MyHandController.GestureState.Extension];
	            myHandController.IExtension();
	            break;
	    }
    }

    public IEnumerator ConnectedToServer()
    {
        yield return new WaitForSeconds(2);
        unetClientBase.ConnectToServer(Ip, Port);
        var count = 0;
        while (!_connected && count <10)
        {
            yield return new WaitForSeconds(5);
            unetClientBase.ConnectToServer(Ip, Port);
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
