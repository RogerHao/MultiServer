using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(UnetClientBase))]
public class TestController : MonoBehaviour {
    private string Ip;
    public int Port;
    public GameObject HandContainner;
    public GameObject HandPrefab;
    public GameObject TargetHandContainner;
    public GameObject TargetHand01;
    public GameObject TargetHand02;


    // Use this for initialization
    private MyHandController myHandController;
    private UnetClientBase unetClientBase;

    private bool _connected = false;
    private int _serverCommand = 0;
    private double _serverMavCommand = 1f;
    private int _commCommand = 0;
    private bool _serverCommandNew = false;
    private bool _serverMavCommandNew = false;

    private int _comCount = 0;
    private bool _isPreparing = false;
    private bool _commpleteOneComm = false;
    private string _commpleteOneCommText = "";
    private List<long> completeTimeList = new List<long>();
    private const  string CommIntro = "Please try to move the hand to the target state";

    public Text supnationCount;
    public Text felxionCount;
    public Text closeCount;
    public GameObject Degree;

    /// <summary>
    /// UI
    /// </summary>
    public Text IntroText;

    void Start()
    {
        Ip = GetIp();
        myHandController = HandContainner.GetComponentInChildren<MyHandController>();
        UnityEngine.Debug.Log(myHandController);
        unetClientBase = gameObject.GetComponent<UnetClientBase>();

        unetClientBase.ConnectionEvent += UnetClientBase_ConnectionEvent;
        unetClientBase.DisconnectionEvent += UnetClientBase_DisconnectionEvent;
        unetClientBase.DataEvent += UnetClientBase_DataEvent;

        myHandController.Success += MyHandController_Success;
        ConnectedToServer();
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
        if (command.Length > 1)
        {
            var comm =  int.TryParse(command[1], out _commCommand);
            Debug.Log(comm);
            if (!comm)
            {
                double.TryParse(command[1], out _serverMavCommand);
                _serverMavCommand = Convert.ToInt32(Mathf.Clamp((float)_serverMavCommand, 1, 5));
//                _serverMavCommand = Convert.ToInt32(Mathf.Clamp((float)_serverMavCommand, 1, 5)*10);
            }

        }
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
        if(Input.GetKeyDown(KeyCode.A)) myHandController.IOpen(false);
        if(Input.GetKeyDown(KeyCode.Q)) myHandController.IResetHand();
        if (Input.GetKeyDown(KeyCode.D)) myHandController.IClose(false);
        if (Input.GetKeyDown(KeyCode.F)) myHandController.CloseSequence();

        if (_commpleteOneComm)
        {
            SendMessageToServer($"{completeTimeList.Last()}");
            SendMessageToServer("206");
            _commpleteOneComm = false;
        }

        if (!_connected) return;
        if (!_serverCommandNew) return;
        UnityEngine.Debug.Log($"Gesture: {_serverCommand}");
        UnityEngine.Debug.Log($"Gesture MAV: {_serverMavCommand}");
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
            case 205:
                StartCoroutine(GestureCom(_commCommand));             
                break;
            case 206:
                myHandController.SetTolerance(_commCommand);
                break;
            case 210:
                myHandController.ManualReset();
                break;
        }
    }

    void OnGUI()
    {
        supnationCount.text = myHandController.SupnationCount.ToString();
        felxionCount.text = myHandController.FelxionCount.ToString();
        closeCount.text = myHandController.CloseCount.ToString();
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

    public IEnumerator GestureCom(int i)
    {
        _isPreparing = true;
        switch (i)
        {
            case 1:
                myHandController.IFlexion();
                myHandController.IClose(false);
                myHandController.isClose = false;
                myHandController.IPronation(false);
                TargetHand01.SetActive(true);
                TargetHand02.SetActive(false);
                yield return new WaitForSeconds(2f);
                myHandController.StartOneTesting();
                IntroText.text = $"Combination {i} : Please try to move the hand to the target state";
                SendMessageToServer("205");
                break;
            case 2:
                myHandController.IExtension();
                myHandController.IClose(false);
                myHandController.isClose = false;
                myHandController.IPronation(false);
                TargetHand01.SetActive(true);
                TargetHand02.SetActive(false);
                yield return new WaitForSeconds(2f);
                myHandController.StartOneTesting();
                IntroText.text = $"Combination {i} : Please try to move the hand to the target state";
                SendMessageToServer("205");
                break;
            case 3:
                myHandController.IExtension();
                myHandController.IOpen(false);
                myHandController.isClose = true;
                myHandController.IPronation(false);
                TargetHand01.SetActive(false);
                TargetHand02.SetActive(true);
                yield return new WaitForSeconds(2f);
                myHandController.StartOneTesting();
                IntroText.text = $"Combination {i} : Please try to move the hand to the target state";
                SendMessageToServer("205");
                break;
            case 4:
                myHandController.IFlexion();
                myHandController.IOpen(false);
                myHandController.isClose = true;
                myHandController.IPronation(false);
                TargetHand01.SetActive(false);
                TargetHand02.SetActive(true);
                yield return new WaitForSeconds(2f);
                myHandController.StartOneTesting();
                IntroText.text = $"Combination {i} : Please try to move the hand to the target state";
                SendMessageToServer("205");
                break;
            case 5:
                myHandController.IFlexion();
                myHandController.IClose(false);
                myHandController.isClose = false;
                myHandController.ISupination(false);
                TargetHand01.SetActive(true);
                TargetHand02.SetActive(false);
                yield return new WaitForSeconds(2f);
                myHandController.StartOneTesting();
                IntroText.text = $"Combination {i} : Please try to move the hand to the target state";
                SendMessageToServer("205");
                break;
            case 6:
                myHandController.IExtension();
                myHandController.IClose(false);
                myHandController.isClose = false;
                myHandController.ISupination(false);
                TargetHand01.SetActive(true);
                TargetHand02.SetActive(false);
                yield return new WaitForSeconds(2f);
                myHandController.StartOneTesting();
                IntroText.text = $"Combination {i} : Please try to move the hand to the target state";
                SendMessageToServer("205");
                break;
            case 7:
                myHandController.IExtension();
                myHandController.IOpen(false);
                myHandController.isClose = true;
                myHandController.ISupination(false);
                TargetHand01.SetActive(false);
                TargetHand02.SetActive(true);
                yield return new WaitForSeconds(2f);
                myHandController.StartOneTesting();
                IntroText.text = $"Combination {i} : Please try to move the hand to the target state";
                SendMessageToServer("205");
                break;
            case 8:
                myHandController.IFlexion();
                myHandController.IOpen(false);
                myHandController.isClose = true;
                myHandController.ISupination(false);
                TargetHand01.SetActive(false);
                TargetHand02.SetActive(true);
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

    public void ResetHand()
    {
        myHandController.Success -= MyHandController_Success;
        Destroy(HandContainner.GetComponentInChildren<MyHandController>().gameObject);
        Instantiate(HandPrefab, HandContainner.transform);
        myHandController = HandContainner.GetComponentInChildren<MyHandController>();
        UnityEngine.Debug.Log(myHandController);
        myHandController.Success += MyHandController_Success;
    }

    public void ReloadTheScene()
    {
        DisConnectedToServer();
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    public void DisableText()
    {
        Degree.SetActive(!Degree.activeInHierarchy);
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