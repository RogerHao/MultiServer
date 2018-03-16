using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using System.Xml.Schema;
using Assets.ProgressBars.Scripts;
using DelsysPlugin;
using MathNet.Numerics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DelsysApp : MonoBehaviour
{
    private DelsysEmgSolver delsysSolver = new DelsysEmgSolver();
    public UNETServer UnetServer;
    public SocketServer SocketServer;
    
    public GuiProgressBarUI progressBar;
    public InputField SampleCountField;
    public InputField TestModelField;
    public InputField GestureNumField;
    public InputField ChannelNumField;

    public Text GestureNow;
    public Text DataAmountNow;
    public Text DataTotalNow;

    public Text PreResult;
    public Text RealResult;
    public Text Rate;

    public Text UserNow;
    public Text LogText;
    private string Log;

    public Image[] GestImages;

    private int _SampleAmount = 8000;
    private int _lastResult = 0;
    private int _realResult  = 0;
    private double _rate = 0f;

    private int _userIndex;
    private int _trianIndex;
    private int _gestureNum;
    private int _commNum = 0;

    private bool isPredicting = false;
    private bool isTraining = false;
    private bool _newResultBool;
    private int _newResutl;
    private double _newMavResutl;

    private int _imageNum = 0;

    // Use this for initialization
    void Start ()
    {
        SocketServerBase.DataEvent += SocketServerBaseOnDataEvent;
        UnetServerBase.DataEvent += UnetServerBase_DataEvent;
        delsysSolver.ResultGenerated += DelsysSolverOnResultGenerated;
        delsysSolver.IntResultGenerated += IntDelsysSolverOnResultGenerated;

        UnetServer.StartUnet();
    }

    private void IntDelsysSolverOnResultGenerated(object sender, int e)
    {
        _newResultBool = true;
        _newResutl = e;
    }

    private void DelsysSolverOnResultGenerated(object sender, double[] doubles)
    {
        _newResultBool = true;
        _newResutl = Convert.ToInt32(doubles[0]);
        _newMavResutl = doubles[1];
    }

    private void UnetServerBase_DataEvent(object sender, UnetServerBase.UnetDataMsg e)
    {
        int command;
        int.TryParse(e.Msg, out command);
        switch (command)
        {
            case 205:
                StartTesting2DProcess();
                break;
            case 206:
                StopTesting2DProcess();
                break;
            default:
                Log = command.ToString();
                break;
        }
    }

    private void SocketServerBaseOnDataEvent(object sender, SocketServerBase.SocketDataMsg e)
    {
        var res = e.Msg.Contains("delsys") ? delsysSolver.SendCommand(e.Msg) : e.Msg;
//        Broadcast(res);
    }

    void OnGUI()
    {
        LogText.text = Log;
        if (isTraining)
        {
            progressBar.Value = (float)delsysSolver.DataNowCount / _SampleAmount;
            GestureNow.text = (delsysSolver.DataTotal / 6000).ToString();
            DataAmountNow.text = delsysSolver.DataNowCount.ToString();
            DataTotalNow.text = delsysSolver.DataTotal.ToString();
        }

        if (!isPredicting) return;
        if (_newResultBool)
        {
            PreResult.text = _newResutl.ToString();
            GestImages[_imageNum].enabled = false;
            _imageNum = _newResutl;
            GestImages[_imageNum].enabled = true;
            UnetServer.SendMsg($"{_newResutl},{_newMavResutl}");
            _newResultBool = false;
        }
        Rate.text = $"{_rate:F} %";
    }

    public async void GetOneGesture()
    {
        int.TryParse(SampleCountField.text, out _SampleAmount);
        await delsysSolver.GetOneGestureAsync(8000,1000,6000);
    }

    public void GenerateModel()
    {
        delsysSolver.GenerateModel();
    }

    public void ConnectToDelsys()
    {
        var res = delsysSolver.Connect();
        Debug.Log(res);

        int g, c, s;
        int.TryParse(GestureNumField.text, out g);
        int.TryParse(ChannelNumField.text, out c);
        int.TryParse(SampleCountField.text, out s);
        delsysSolver.InitClassifier(g, c, s - 2000, s, 1000, 300, 100,true);
    }

    private IEnumerator DisconnectToDelsys()
    {
        delsysSolver.Disconnect();
        UnetServer.StopUnet();
        yield return new WaitForSeconds(3f);
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    public void StartPre()
    {
        int result =  delsysSolver.StartPredictAsync();
        if (result == 0) isPredicting = true;
    }

    public void StopPre()
    {
        int result = delsysSolver.StopPredictAsync();
        if (result == 0) isPredicting = false;
    }

    public void TestModel()
    {
        int a;
        int.TryParse(TestModelField.text, out a);
        PreResult.text = delsysSolver.TestModel(a).ToString();
    }

    public void SetZeroFactor()
    {
        double a;
        double.TryParse(TestModelField.text, out a);
        delsysSolver.ThresholdMavFactor = a;
    }

    public void Broadcast(string msg)
    {
        UnetServer.SendMsg(msg);
        SocketServer.SendMsg(msg);
    }

    public void SaveData()
    {
        delsysSolver.SaveDataToCsv("",$"EmgData_{DateTime.Now.ToString("yyyyMMddhhmmss")}_{UserNow.text}", DelsysEmgSolver.FileType.Emgdata);
    }

    public void StartTrial()
    {
        if (!delsysSolver.Connected)
        {
            Log = "No Delsys, Manual Mode";
            return;
        }

        if (UnetServer.ClientCount == 0)
        {
            Log = "No Client, Manual Mode";
            return;
        }

        int g, c, s;
        int.TryParse(GestureNumField.text, out g);
        int.TryParse(ChannelNumField.text, out c);
        int.TryParse(SampleCountField.text, out s);
        delsysSolver.InitClassifier(g, c, s - 2000, s, 1000, 300, 100);

        StartCoroutine(TrainningProcess()); 
    }
    private IEnumerator TrainningProcess()
    {
        isTraining = true;
        UnetServer.SendMsg("10");
        yield return new WaitForSeconds(3f);
        Log = "Start Trainning";
        for (int i = 0; i < 7; i++)
        {
            UnetServer.SendMsg($"10{i}");
            yield return new WaitForSeconds(1f);
            delsysSolver.GetOneGestureAsync(8000,1000,6000);
            yield return new WaitForSeconds(5.5f);
            UnetServer.SendMsg($"100");
            GestImages[_imageNum].enabled = false;
            _imageNum = i;
            GestImages[_imageNum].enabled = true;
            yield return new WaitForSeconds(4f);
        }
        UnetServer.SendMsg("1010");
        delsysSolver.GenerateModel();
        Debug.Log(delsysSolver.ClassMavStr);
        isTraining = false;
    }

    public void StartTestingClassifier()
    {
        if (!delsysSolver.Connected)
        {
            Log = "No Delsys";
            return;
        }

        if (UnetServer.ClientCount == 0)
        {
            Log = "No Client";
            return;
        }

        if (!delsysSolver.ModelGenerated)
        {
            Log = "No Model";
            return;
        }
        StartCoroutine(ClassfierTestingProcess());
    }
    private IEnumerator ClassfierTestingProcess()
    {
        delsysSolver.SaveDataToCsv("", $"ResultData_before_TestClassifier_{DateTime.Now.ToString("yyyyMMddhhmmss")}_{UserNow.text}", DelsysEmgSolver.FileType.ResultData);
        delsysSolver.SaveDataToCsv("", $"EmgData_before_TestClassifier_{DateTime.Now.ToString("yyyyMMddhhmmss")}_{UserNow.text}", DelsysEmgSolver.FileType.Emgdata);
        UnetServer.SendMsg("10");
        yield return new WaitForSeconds(3f);
        Log = "Start Testing";
        for (int i = 0; i < 7; i++)
        {
            UnetServer.SendMsg($"10{i}");
            yield return new WaitForSeconds(1f);
            if (!delsysSolver.IsPredicting) StartPre();
            yield return new WaitForSeconds(5.5f);
            if (delsysSolver.IsPredicting) StopPre();
            UnetServer.SendMsg($"100");
            _rate = delsysSolver.CalculateAccuracy();
            delsysSolver.SaveDataToCsv("", $"{i}_ResultData_TestClassifier_{DateTime.Now.ToString("yyyyMMddhhmmss")}_{UserNow.text}", DelsysEmgSolver.FileType.ResultData);
            delsysSolver.SaveDataToCsv("", $"{i}_EmgData_TestClassifier_{DateTime.Now.ToString("yyyyMMddhhmmss")}_{UserNow.text}", DelsysEmgSolver.FileType.Emgdata);
            yield return new WaitForSeconds(4f);
        }
        delsysSolver.SaveDataToCsv("", $"ClassifierAccuracy_{DateTime.Now.ToString("yyyyMMddhhmmss")}_{UserNow.text}", DelsysEmgSolver.FileType.ClassifierData);
        UnetServer.SendMsg("1010");

    }

    public void StartTestingNextComm()
    {
        UnetServer.SendMsg("204");
    }
    private void StartTesting2DProcess()
    {
        _commNum++;
        if (_commNum > 8) return;
        if(!delsysSolver.IsPredicting) StartPre();
    }
    private void StopTesting2DProcess()
    {
        if (delsysSolver.IsPredicting) StopPre();
        delsysSolver.SaveDataToCsv("", $"ResultData_{_commNum}_{DateTime.Now.ToString("yyyyMMddhhmmss")}_{UserNow.text}", DelsysEmgSolver.FileType.ResultData);
        delsysSolver.SaveDataToCsv("", $"EmgData_{_commNum}_{DateTime.Now.ToString("yyyyMMddhhmmss")}_{UserNow.text}", DelsysEmgSolver.FileType.Emgdata);
    }

    public void Restart()
    {
        StartCoroutine(DisconnectToDelsys());
    }
}
