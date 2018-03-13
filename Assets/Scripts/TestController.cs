using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(HandPhysicsController))]
[RequireComponent(typeof(MyHandController))]
[RequireComponent(typeof(UnetClientBase))]
public class TestController : MonoBehaviour {
    public string Ip;
    public int Port;

    // Use this for initialization
    private MyHandController myHandController;
    private UnetClientBase unetClientBase;

    private bool _connected = false;
    private int _serverCommand = 0;
    private double _serverMavCommand = 0f;
    private bool _serverCommandNew = false;
    private bool _serverMavCommandNew = false;

    private int _comCount = 0;
    private bool _isPreparing = false;
    private bool _commpleteOneComm = false;
    private string _commpleteOneCommText = "";
    private List<long> completeTimeList = new List<long>();
    private const  string CommIntro = "Please try to move the hand to the opposite state";

    /// <summary>
    /// UI
    /// </summary>
    public Text IntroText;

    void Start()
    {
        myHandController = gameObject.GetComponent<MyHandController>();
        unetClientBase = gameObject.GetComponent<UnetClientBase>();

        unetClientBase.ConnectionEvent += UnetClientBase_ConnectionEvent;
        unetClientBase.DisconnectionEvent += UnetClientBase_DisconnectionEvent;
        unetClientBase.DataEvent += UnetClientBase_DataEvent;
        myHandController.Success += MyHandController_Success;

        StartCoroutine(ConnectedToServer());
    }

    private void MyHandController_Success(object sender, long l)
    {
        _commpleteOneComm = true;
        completeTimeList.Add(l);
        _commpleteOneCommText = l >= 45000 ? "Time Out" : "Success";
        IntroText.text = _commpleteOneCommText;
    }

    private void UnetClientBase_DataEvent(object sender, UnetClientBase.UnetDataMsg e)
    {
        var command = e.Msg.Split(',');
        int.TryParse(command[0], out _serverCommand);
        if(command.Length>1) double.TryParse(command[1], out _serverMavCommand);
        _serverCommandNew = true;
    }

    private void UnetClientBase_DisconnectionEvent(object sender, UnetClientBase.UnetConnectionMsg e)
    {

    }

    private void UnetClientBase_ConnectionEvent(object sender, UnetClientBase.UnetConnectionMsg e)
    {
        _connected = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (_commpleteOneComm)
        {
            SendMessageToServer($"{completeTimeList.Last()}");
            SendMessageToServer("206");
            _commpleteOneComm = false;
        }

        if (!_connected) return;
        if (!_serverCommandNew) return;
        UnityEngine.Debug.Log(_serverCommand);
        _serverCommandNew = false;

        switch (_serverCommand)
        {
            case 0:
                if (!myHandController.IsTesting && !myHandController.IsNullTesting) return;
                IntroText.text = myHandController.GestureIntroTest[MyHandController.GestureState.NoMovement];
                break;
            case 1:
            case 11:
                if (!myHandController.IsTesting && !myHandController.IsNullTesting) return;
                IntroText.text = myHandController.GestureIntroTest[MyHandController.GestureState.Close];
                myHandController.CloseSequence(_serverMavCommand);
                break;
            case 2:
            case 22:
                if (!myHandController.IsTesting && !myHandController.IsNullTesting) return;
                IntroText.text = myHandController.GestureIntroTest[MyHandController.GestureState.Open];
                myHandController.OpenSequence(_serverMavCommand);
                break;
            case 3:
            case 33:
                if (!myHandController.IsTesting && !myHandController.IsNullTesting) return;
                IntroText.text = myHandController.GestureIntroTest[MyHandController.GestureState.Supination];
                myHandController.SupinationAndPronation(true, _serverMavCommand);
                break;
            case 4:
            case 44:
                if (!myHandController.IsTesting && !myHandController.IsNullTesting) return;
                IntroText.text = myHandController.GestureIntroTest[MyHandController.GestureState.Pronation];
                myHandController.SupinationAndPronation(false, _serverMavCommand);
                break;
            case 5:
            case 55:
                if (!myHandController.IsTesting && !myHandController.IsNullTesting) return;
                IntroText.text = myHandController.GestureIntroTest[MyHandController.GestureState.Flexion];
                myHandController.FlexionAndExtension(true, _serverMavCommand);
                break;
            case 6:
            case 66:
                if (!myHandController.IsTesting && !myHandController.IsNullTesting) return;
                IntroText.text = myHandController.GestureIntroTest[MyHandController.GestureState.Extension];
                myHandController.FlexionAndExtension(false, _serverMavCommand);
                break;
            case 10:
                myHandController.IResetHand();
                break;
            case 202:
                myHandController.StartNullTesting();
                break;
            case 203:
                myHandController.StopNullTesting();
                break;
            case 204:
                _comCount++;
                if (!_isPreparing) StartCoroutine(GestureCom(_comCount));
                break;
            case 210:
                myHandController.ManualReset();
                break;
        }
    }

    public IEnumerator ConnectedToServer()
    {
        yield return new WaitForSeconds(2);
        unetClientBase.ConnectToServer(Ip, Port);
//        var count = 0;
//        while (!_connected && count < 10)
//        {
//            yield return new WaitForSeconds(5);
//            unetClientBase.ConnectToServer(Ip, Port);
//            count++;
//        }
    }

    public void DisConnectedToServer()
    {
        unetClientBase.DisconnectToServer();
    }

    public void SendMessageToServer(string msg)
    {
        unetClientBase.SendMessageToServer(msg);
    }

    public IEnumerator GestureCom(int i)
    {
        _isPreparing = true;
        switch (i)
        {
            case 1:
                myHandController.IFlexion();
                myHandController.IClose(false);
                myHandController.IPronation(false);
                yield return new WaitForSeconds(2f);
                myHandController.StartOneTesting();
                IntroText.text = $"Combination {i} : Please try to move the hand to the opposite state";
                SendMessageToServer("205");
                break;
            case 2:
                myHandController.IExtension();
                myHandController.IClose(false);
                myHandController.IPronation(false);
                yield return new WaitForSeconds(2f);
                myHandController.StartOneTesting();
                IntroText.text = $"Combination {i} : Please try to move the hand to the opposite state";
                SendMessageToServer("205");
                break;
            case 3:
                myHandController.IExtension();
                myHandController.IOpen(false);
                myHandController.IPronation(false);
                yield return new WaitForSeconds(2f);
                myHandController.StartOneTesting();
                IntroText.text = $"Combination {i} : Please try to move the hand to the opposite state";
                SendMessageToServer("205");
                break;
            case 4:
                myHandController.IFlexion();
                myHandController.IOpen(false);
                myHandController.IPronation(false);
                yield return new WaitForSeconds(2f);
                myHandController.StartOneTesting();
                IntroText.text = $"Combination {i} : Please try to move the hand to the opposite state";
                SendMessageToServer("205");
                break;
            case 5:
                myHandController.IFlexion();
                myHandController.IClose(false);
                myHandController.ISupination(false);
                yield return new WaitForSeconds(2f);
                myHandController.StartOneTesting();
                IntroText.text = $"Combination {i} : Please try to move the hand to the opposite state";
                SendMessageToServer("205");
                break;
            case 6:
                myHandController.IExtension();
                myHandController.IClose(false);
                myHandController.ISupination(false);
                yield return new WaitForSeconds(2f);
                myHandController.StartOneTesting();
                IntroText.text = $"Combination {i} : Please try to move the hand to the opposite state";
                SendMessageToServer("205");
                break;
            case 7:
                myHandController.IExtension();
                myHandController.IOpen(false);
                myHandController.ISupination(false);
                yield return new WaitForSeconds(2f);
                myHandController.StartOneTesting();
                IntroText.text = $"Combination {i} : Please try to move the hand to the opposite state";
                SendMessageToServer("205");
                break;
            case 8:
                myHandController.IFlexion(false);
                myHandController.IOpen(false);
                myHandController.ISupination();
                yield return new WaitForSeconds(2f);
                myHandController.StartOneTesting();
                IntroText.text = "Please try to move the hand to the original state";
                SendMessageToServer("205");
                break;
            case 9:
                IntroText.text = "The Testing Process is Over, Waitting for next process";
                break;
            default:
                IntroText.text = "Wrong Combiantion Index";
                break;
            
        }
        _isPreparing = false;

    }
}