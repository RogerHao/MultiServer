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
    public Text LogText;
    private string Log;

    public Image[] GestImages;

    private int _SampleAmount = 8000;
    private int _lastResult = 0;
    private int _realResult  = 0;
    private int _rate = 0;

    private int _userIndex;
    private int _trianIndex;
    private int _gestureNum;
    private int _commNum = 0;

    private bool isPredicting = false;
    private bool _newResultBool;
    private int _newResutl;

    // Use this for initialization
    void Start ()
    {
        SocketServerBase.DataEvent += SocketServerBaseOnDataEvent;
        UnetServerBase.DataEvent += UnetServerBase_DataEvent;
        delsysSolver.ResultGenerated += DelsysSolverOnResultGenerated;
    }

    private void DelsysSolverOnResultGenerated(object sender, int result)
    {
        _newResultBool = true;
        _newResutl = result;
    }

    private void UnetServerBase_DataEvent(object sender, UnetServerBase.UnetDataMsg e)
    {
        var res = e.Msg.Contains("delsys") ? delsysSolver.SendCommand(e.Msg) : e.Msg;
        Broadcast(res);
        int command;
        int.TryParse(e.Msg, out command);
        switch (command)
        {
                
        }
    }

    private void SocketServerBaseOnDataEvent(object sender, SocketServerBase.SocketDataMsg e)
    {
        var res = e.Msg.Contains("delsys") ? delsysSolver.SendCommand(e.Msg) : e.Msg;
        Broadcast(res);
    }

    void OnGUI()
    {
        LogText.text = Log;
        if (!isPredicting)
        {
            progressBar.Value = (float)delsysSolver.DataNowCount / _SampleAmount;

            GestureNow.text = (delsysSolver.DataTotal / 6000).ToString();
            DataAmountNow.text = delsysSolver.DataNowCount.ToString();
            DataTotalNow.text = delsysSolver.DataTotal.ToString();
            RealResult.text = _realResult.ToString();
            if (delsysSolver.DataTotal / 6000 == _lastResult || delsysSolver.DataTotal / 6000 >= GestImages.Length) return;
            _lastResult += 1;
            GestImages[_lastResult - 1].enabled = false;
            GestImages[_lastResult>7?7:_lastResult].enabled = true;
        }
        else
        {
            if (_newResultBool)
            {
                UnetServer.SendMsg(_newResutl.ToString());
                _newResultBool = false;
            }
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

    public void Broadcast(string msg)
    {
        UnetServer.SendMsg(msg);
        SocketServer.SendMsg(msg);
    }

    public void SaveData()
    {
        delsysSolver.SaveDataToCsv("",$"EmgData_{DateTime.Now.ToString("yyyyMMddhhmmss")}_{UserNow.text}", true);
    }

    public void StartTrial()
    {
        if (!delsysSolver.Connected)
        {
            Log = "No Delsys, Manual Mode";
            return;
        }

        int g, c,s;
        int.TryParse(GestureNumField.text, out g);
        int.TryParse(ChannelNumField.text, out c);
        int.TryParse(SampleCountField.text, out s);
        delsysSolver.InitClassifier(g,c,s-2000,s,1000,600,200);

        if (UnetServer.ClientCount == 0)
        {
            Log = "No Client, Manual Mode";
            return;
        }
        StartCoroutine(TrainningProcess()); 
    }

    public IEnumerator TrainningProcess()
    {
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
            yield return new WaitForSeconds(4f);
        }
        UnetServer.SendMsg("107");
        delsysSolver.GenerateModel();
    }

    public void StartTesting2DProcess()
    {
        _commNum++;
        if (_commNum > 8) return;
        if(!delsysSolver.IsPredicting) StartPre();
    }

    public void StopTesting2DProcess()
    {
        if (delsysSolver.IsPredicting) StopPre();
        delsysSolver.SaveDataToCsv("", $"ResultData_{_commNum}_{DateTime.Now.ToString("yyyyMMddhhmmss")}_{UserNow.text}", false);
        delsysSolver.SaveDataToCsv("", $"EmgData_{_commNum}_{DateTime.Now.ToString("yyyyMMddhhmmss")}_{UserNow.text}", true);
    }
}
