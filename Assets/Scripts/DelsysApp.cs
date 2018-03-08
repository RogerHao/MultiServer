using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.ProgressBars.Scripts;
using DelsysPlugin;
using UnityEngine;
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

    public Image[] GestImages;

    

    private int _SampleAmount = 5000;
    private int _lastResult = 0;
    private int _realResult  = 0;
    private int _rate = 0;

    private int _userIndex;
    private int _trianIndex;
    private int _gestureNum;

    private bool isPredicting = false;

    // Use this for initialization
    void Start ()
    {
        SocketServerBase.DataEvent += SocketServerBaseOnDataEvent;
        UnetServerBase.DataEvent += UnetServerBase_DataEvent;
    }

    private void UnetServerBase_DataEvent(object sender, UnetServerBase.UnetDataMsg e)
    {
        var res = e.Msg.Contains("delsys") ? delsysSolver.SendCommand(e.Msg) : e.Msg;
        Broadcast(res);
    }

    private void SocketServerBaseOnDataEvent(object sender, SocketServerBase.SocketDataMsg e)
    {
        var res = e.Msg.Contains("delsys") ? delsysSolver.SendCommand(e.Msg) : e.Msg;
        Broadcast(res);
    }

    void OnGUI()
    {
        if (!isPredicting)
        {
            progressBar.Value = (float)delsysSolver.DataNowCount / _SampleAmount;

            GestureNow.text = (delsysSolver.DataTotal / 3000).ToString();
            DataAmountNow.text = delsysSolver.DataNowCount.ToString();
            DataTotalNow.text = delsysSolver.DataTotal.ToString();
            RealResult.text = _realResult.ToString();
            if (delsysSolver.DataTotal / 3000 == _lastResult) return;
            GestImages[_lastResult].enabled = false;
            _lastResult += 1;
            GestImages[_lastResult].enabled = true;
        }
        else
        {
            BroadcastResult();
            PreResult.text = delsysSolver.GestureResult.ToString();
            Rate.text = $"{_rate:F} %";
            if (delsysSolver.GestureResult == _lastResult) return;
            GestImages[_lastResult].enabled = false;
            _lastResult = delsysSolver.GestureResult;
            GestImages[_lastResult].enabled = true;
        }
    }

    public async void GetOneGesture()
    {
        int.TryParse(SampleCountField.text, out _SampleAmount);
        await delsysSolver.GetOneGestureAsync(_SampleAmount==0?5000:_SampleAmount, 1000, 3000);
    }

    public void GenerateModel()
    {
        delsysSolver.GenerateModel();
    }

    public void ConnectToDelsys()
    {
        var res = delsysSolver.Connect();
        Debug.Log(res);
    }

    public void DisconnectToDelsys()
    {
        delsysSolver.Disconnect();
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

    public void BroadcastResult()
    {
        UnetServer.SendMsg(delsysSolver.GestureResult.ToString());
        SocketServer.SendMsg(delsysSolver.GestureResult.ToString());
    }

    public void Broadcast(string msg)
    {
        UnetServer.SendMsg(msg);
        SocketServer.SendMsg(msg);
    }

    public void SaveData()
    {
        delsysSolver.SaveDataToCsv("",$"_{DateTime.Now.ToString("yyyyMMddhhmmss")}_{UserNow.text}", null);
    }

    public void StartTrial()
    {
        int g, c,s;
        int.TryParse(GestureNumField.text, out g);
        int.TryParse(ChannelNumField.text, out c);
        int.TryParse(SampleCountField.text, out s);
        delsysSolver.InitClassifier(g,c,s-2000,s,1000);

    }
}
