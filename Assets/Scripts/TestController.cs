using System.Collections;
using System.Collections.Generic;
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
    private bool _serverCommandNew = false;

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
    void Update()
    {
        if (!_connected) return;
        if (!_serverCommandNew) return;
        _serverCommandNew = false;
        switch (_serverCommand)
        {
            case 0:
                IntroText.text = myHandController.GestureIntroTest[MyHandController.GestureState.NoMovement];
                break;
            case 1:
            case 11:
                IntroText.text = myHandController.GestureIntroTest[MyHandController.GestureState.Close];
                myHandController.CloseSequence();
                break;
            case 2:
            case 22:
                IntroText.text = myHandController.GestureIntroTest[MyHandController.GestureState.Open];
                myHandController.OpenSequence();
                break;
            case 3:
            case 33:
                IntroText.text = myHandController.GestureIntroTest[MyHandController.GestureState.Supination];
                myHandController.SupinationAndPronation(true);
                break;
            case 4:
            case 44:
                IntroText.text = myHandController.GestureIntroTest[MyHandController.GestureState.Pronation];
                myHandController.SupinationAndPronation(false);
                break;
            case 5:
            case 55:
                IntroText.text = myHandController.GestureIntroTest[MyHandController.GestureState.Flexion];
                myHandController.FlexionAndExtension(true);
                break;
            case 6:
            case 66:
                IntroText.text = myHandController.GestureIntroTest[MyHandController.GestureState.Extension];
                myHandController.FlexionAndExtension(false);
                break;
            case 10:
                myHandController.IResetHand();
                break;
        }
    }

    public IEnumerator ConnectedToServer()
    {
        yield return new WaitForSeconds(2);
        unetClientBase.ConnectToServer(Ip, Port);
        var count = 0;
        while (!_connected && count < 10)
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

    #region Combination
    public void GestureCom1()
    {
        myHandController.IFlexion();
        myHandController.IClose();
        myHandController.IPronation();
    }
    public void GestureCom2()
    {
        myHandController.IExtension();
        myHandController.IClose();
        myHandController.IPronation();
    }
    public void GestureCom3()
    {
        myHandController.IExtension();
        myHandController.IOpen();
        myHandController.IPronation();
    }
    public void GestureCom4()
    {
        myHandController.IFlexion();
        myHandController.IOpen();
        myHandController.IPronation();
    }
    public void GestureCom5()
    {
        myHandController.IFlexion();
        myHandController.IClose();
        myHandController.ISupination();
    }
    public void GestureCom6()
    {
        myHandController.IExtension();
        myHandController.IClose();
        myHandController.ISupination();
    }
    public void GestureCom7()
    {
        myHandController.IExtension();
        myHandController.IOpen();
        myHandController.ISupination();
    }
    public void GestureCom8()
    {
        myHandController.IFlexion();
        myHandController.IOpen();
        myHandController.ISupination();
    }
    #endregion

}
