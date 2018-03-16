using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;
using HandPhysicsExtenstions;
using UnityEngine.UI;

[RequireComponent(typeof(HandPhysicsController))]
public class MyHandController : MonoBehaviour
{
    public GameObject forearm;
    public GameObject wrist;

    private FingerPart[] _fingers = new FingerPart[15];
    private List<Quaternion> defaultfingerTarget = new List<Quaternion>();
    private List<Quaternion> openfingerTarget = new List<Quaternion>()
    {
        new Quaternion(0.16f,0.5f,0.67f,1),
        new Quaternion(0,0.06f,0,1),
        new Quaternion(0,-0.06f,0,1),

        new Quaternion(-0.03f,0,-0.05f,1),
        new Quaternion(-0.03f,0,0,1),
        new Quaternion(-0.03f,0,0,1),

        new Quaternion(-0.03f,0,0,1),
        new Quaternion(-0.03f,0,0,1),
        new Quaternion(-0.03f,0,0,1),

        new Quaternion(-0.03f,-0.025f,0.01f,1),
        new Quaternion(-0.03f,0,0,1),
        new Quaternion(-0.03f,0,0,1),

        new Quaternion(-0.03f,-0.05f,0.05f,1),
        new Quaternion(-0.03f,0,0,1),
        new Quaternion(-0.03f,0,0,1),
    };

    public HandPhysicsController Controller
    {
        get
        {
            if (_controller == null)
                _controller = GetComponent<HandPhysicsController>();
            return _controller;
        }
    }
    private HandPhysicsController _controller;

    public float WristRotationSpeed
    {
        get { return Controller.Wrist.RotationSpeed; }
        set { Controller.Wrist.RotationSpeed = value; }
    }
    public float ArmRotationSpeed
    {
        get { return Controller.Forearm.RotationSpeed; }
        set { Controller.Forearm.RotationSpeed = value; }
    }

    public enum GestureState
    {
        NoMovement,
        Supination,
        Pronation,
        Flexion,
        Extension,
        Open,
        Close
    }
    public Dictionary<GestureState,string> GestureIntro = new Dictionary<GestureState, string>()
    {
        {GestureState.NoMovement,"Please Rest"},
        {GestureState.Close,"Please Close your hand"},
        {GestureState.Open,"Please Open your hand"},
        {GestureState.Supination,"Please Wrist Supination"},
        {GestureState.Pronation,"Please Wrist Pronation"},
        {GestureState.Flexion,"Please Wrist Flexion"},
        {GestureState.Extension,"Please Wrist Extension"},
    };
    public Dictionary<GestureState, string> GestureIntroTest = new Dictionary<GestureState, string>()
    {
        {GestureState.NoMovement,"Your Gesture Now:  Rest"},
        {GestureState.Close,"Your Gesture Now:  Close your hand"},
        {GestureState.Open,"Your Gesture Now:  Open your hand"},
        {GestureState.Supination,"Your Gesture Now:  Wrist Supination"},
        {GestureState.Pronation,"Your Gesture Now:  Wrist Pronation"},
        {GestureState.Flexion,"Your Gesture Now:  Wrist Flexion"},
        {GestureState.Extension,"Your Gesture Now:  Wrist Extension"},
    };

    public GestureState TrainGestureState = GestureState.NoMovement;
    public GestureState TestGestureState = GestureState.NoMovement;

    public int SupnationCount = 0;
    public int FelxionCount = 0;
    public int CloseCount = 0;
    public event EventHandler<long> Success;
    public bool IsTesting { get; private set; }
    public bool IsNullTesting { get; private set; }
    private Stopwatch sw = new Stopwatch();
    private Stopwatch sw_dewlling = new Stopwatch();
    public long OverTime = 45000;
    public long DeellingTime = 2000;
    public int TargetDistance = 75;
    public int TargetTolerance = 5;

    public Material HandNormalMaterial;
    public Material HandGreenMaterial;


    /// <summary>
    /// trick
    /// </summary>
    public bool isClose = false;
    /// 
    // Use this for initialization
    void Start ()
    {
        _fingers = gameObject.GetComponentsInChildren<FingerPart>();
        foreach (var fingerPart in _fingers)
        {
            defaultfingerTarget.Add(fingerPart.TargetRotation);
        }
    }

	// Update is called once per frame
	void Update ()
	{
        if(!IsTesting) return;
        if(sw.ElapsedMilliseconds>=OverTime) StopOneTesting(false);
	    if (Math.Abs(CloseCount-(isClose?90:15)) > TargetTolerance || Mathf.Abs(FelxionCount) > TargetTolerance || Mathf.Abs(SupnationCount) > TargetTolerance)
	    {
            if(!sw_dewlling.IsRunning) return;
	        gameObject.GetComponentInChildren<SkinnedMeshRenderer>().material= HandNormalMaterial;
            sw_dewlling.Stop();
            sw_dewlling.Reset();
	    }
	    else
	    {
	        gameObject.GetComponentInChildren<SkinnedMeshRenderer>().material= HandGreenMaterial;
            if (!sw_dewlling.IsRunning) sw_dewlling.Start();
            if(sw_dewlling.ElapsedMilliseconds>=DeellingTime) StopOneTesting(true);
	    }
	}


    /// <summary>
    /// Seven Gesture Public Method
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    /// 
    public void ISupination(bool train = true)
    {
        StartCoroutine(Supination(train));
    }
    private IEnumerator Supination(bool train = true)
    {
        if(TrainGestureState != GestureState.NoMovement) yield break;
        if(TrainGestureState == GestureState.Supination) yield break;
        for (var i = 0; i < (train?90:TargetDistance); i++)
        {
            yield return new WaitForSeconds(0.01f);
            SupinationAndPronation(true);
        }
        TrainGestureState = GestureState.Supination;
    }

    public void IPronation(bool train = true)
    {
        StartCoroutine(Pronation(train));
    }
    private IEnumerator Pronation(bool train)
    {
        if (TrainGestureState != GestureState.NoMovement) yield break;
        if (TrainGestureState == GestureState.Pronation) yield break;
        for (var i = 0; i < (train ? 150 : TargetDistance); i++)
        {
            yield return new WaitForSeconds(0.01f);
            SupinationAndPronation(false);
        }
        TrainGestureState = GestureState.Pronation;
    }

    public void IFlexion(bool train = true)
    {
        StartCoroutine(Flexion(train));
    }
    private IEnumerator Flexion(bool train)
    {
        if (TrainGestureState != GestureState.NoMovement) yield break;
        if (TrainGestureState == GestureState.Flexion) yield break;
        for (var i = 0; i < TargetDistance; i++)
        {
            yield return new WaitForSeconds(0.01f);
            FlexionAndExtension(true);
        }
        TrainGestureState = GestureState.Flexion;
    }

    public void IExtension(bool pos = true)
    {
        StartCoroutine(Extension(pos));
    }
    private IEnumerator Extension(bool pos)
    {
        if (TrainGestureState != GestureState.NoMovement) yield break;
        if (TrainGestureState == GestureState.Extension) yield break;
        for (var i = 0; i < TargetDistance; i++)
        {
            yield return new WaitForSeconds(0.01f);
            FlexionAndExtension(false);
        }
        TrainGestureState = GestureState.Extension;
    }

    public void IOpen(bool pos = true)
    {
        StartCoroutine(pos ? Open(pos) : Open());
    }
    private IEnumerator Open(bool pos)
    {
        if (pos && TrainGestureState == GestureState.Open) yield break;
        if (pos && TrainGestureState != GestureState.NoMovement) yield break;
        for (var index = 0; index < _fingers.Length; index++)
        {
            var fingerPart = _fingers[index];
            fingerPart.TargetRotation = openfingerTarget[index];
        }
        Controller.StartBendFingers();
        TrainGestureState = GestureState.Open;
    }

    public void IClose(bool pos = true)
    {
        StartCoroutine(pos ? Close(pos) : Close());
    }
    private IEnumerator Close(bool pos)
    {
        if (pos && TrainGestureState == GestureState.Close) yield break;
        if (pos && TrainGestureState != GestureState.NoMovement) yield break;
        for (var index = 0; index < _fingers.Length; index++)
        {
            var fingerPart = _fingers[index];
            fingerPart.TargetRotation = defaultfingerTarget[index];
        }
        Controller.StartBendFingers();
        TrainGestureState = GestureState.Close;
    }

    public void Rest()
    {
        switch (TrainGestureState)
        {
            case GestureState.NoMovement:
                return;
            case GestureState.Close:
                Controller.StopBendFingers();
                TrainGestureState = GestureState.NoMovement;
                break;
            case GestureState.Open:
                Controller.StopBendFingers();
                TrainGestureState = GestureState.NoMovement;
                break;
            case GestureState.Supination:
                StartCoroutine(Supination(false));
                break;
            case GestureState.Pronation:
                StartCoroutine(Pronation(false));
                break;
            case GestureState.Flexion:
                StartCoroutine(Flexion(false));
                break;
            case GestureState.Extension:
                StartCoroutine(Extension(false));
                break;
        }
    }
    public void IResetHand()
    {
        StartCoroutine(ResetHand());
    }
    public IEnumerator ResetHand()
    {
        
        for (var i = 0; i < Mathf.Abs(FelxionCount); i++)
        {
            yield return new WaitForSeconds(0.01f);
            Controller.RotateWrist(FelxionCount >= 0 ? 0.1f : -0.1f);
        }
        for (var i = 0; i < Mathf.Abs(SupnationCount); i++)
        {
            yield return new WaitForSeconds(0.01f);
            Controller.RotateForearm(SupnationCount >= 0 ? 0.1f : -0.1f);
        }
        for (var i = 0; i < Mathf.Abs(CloseCount); i++)
        {
            yield return new WaitForSeconds(0.01f);
            if(CloseCount>=0) Controller.StopBendFingersAmount();
            else Controller.StartBendFingersAmount();
        }
        Controller.StartBendFingers();
        Controller.StopBendFingers();
        TrainGestureState = GestureState.NoMovement;
        TestGestureState = GestureState.NoMovement;
        SupnationCount = 0;
        FelxionCount = 0;
        CloseCount = 0;
    }

    public void SetForearmSpeed(float speed)
    {
        Controller.Forearm.RotationSpeed = speed;
    }

    public void SetWristSpeed(float speed)
    {
        Controller.Wrist.RotationSpeed = speed;
    }

    public void SetFingerSpeed(float speed)
    {
        Controller.Fingers.BendSpeed = speed;
    }

    public void SetTolerance(int tol)
    {
        TargetTolerance = tol;
    }

    public void SupinationAndPronation(bool sup, double speed = 1f)
    {
        //        if (!IsTesting && !IsNullTesting) return;
        var speedInt = Convert.ToInt32(speed);
        speedInt = speedInt>0?speedInt:1;
        if (sup) SupnationCount += speedInt;
        else SupnationCount-= speedInt;
        Controller.RotateForearm(sup?-0.1f*speedInt:0.1f* speedInt);
    }

    public void FlexionAndExtension(bool fle, double speed = 1f)
    {
        //        if (!IsTesting && !IsNullTesting) return;
        var speedInt = Convert.ToInt32(speed);
        speedInt = speedInt > 0 ? speedInt : 1;
        if (fle) FelxionCount+= speedInt;
        else FelxionCount-= speedInt;
        Controller.RotateWrist(fle ? -0.1f * speedInt : 0.1f * speedInt);
    }

    public void CloseSequence(double speed=1f)
    {
        var speedInt = Convert.ToInt32(speed);
        speedInt = speedInt > 0 ? speedInt : 1;
        if (CloseCount + speedInt >= 100) return;
        StartCoroutine(CloseSequenceAmount(speedInt));
    }

    private IEnumerator CloseSequenceAmount(int speed)
    {
        for (var i = 0; i < speed; i++)
        {
            Controller.StartBendFingersAmount();
            yield return new WaitForSeconds(0.005f);
        }
        CloseCount += speed;
    }

    private IEnumerator Close()
    {
        for (var i = 0; i < 90; i++)
        {
            Controller.StartBendFingersAmount();
            yield return new WaitForSeconds(0.01f);
        }
        CloseCount += 90;
    }

    public void OpenSequence(double speed = 1f)
    {
        var speedInt = Convert.ToInt32(speed);
        speedInt = speedInt > 0 ? speedInt : 1;
        if (CloseCount - speedInt <= 5) return;
        StartCoroutine(OpenSequenceAmount(speedInt));
    }

    private IEnumerator OpenSequenceAmount(int speed)
    {
        for (var i = 0; i < speed; i++)
        {
            Controller.StopBendFingersAmount();
            yield return new WaitForSeconds(0.005f);
        }
        CloseCount -= speed;
    }

    private IEnumerator Open()
    {
        for (var i = 0; i < 15; i++)
        {
            Controller.StartBendFingersAmount();
            yield return new WaitForSeconds(0.01f);
        }
        CloseCount += 15;
    }

    public void StartOneTesting()
    {
        sw.Reset();
        IsTesting = true;
        sw.Start();
    }

    private void StopOneTesting(bool success)
    {
        sw.Stop();
        IsTesting = false;
        IResetHand();
        Success?.Invoke(null,sw.ElapsedMilliseconds);
    }

    public void StartNullTesting()
    {
        IsNullTesting = true;
    }
    public void StopNullTesting()
    {
        IsNullTesting = false;
        IResetHand();
    }

    public void ManualReset()
    {
        Controller.StartBendFingers();
        for (int i = 0; i < Math.Abs(forearm.transform.rotation.eulerAngles.z); i++)
        {
            SupinationAndPronation(forearm.transform.rotation.eulerAngles.z<0);
        }
        for (int i = 0; i < Math.Abs(wrist.transform.rotation.eulerAngles.x); i++)
        {
            FlexionAndExtension(wrist.transform.rotation.eulerAngles.x < 0);
        }
        Controller.StopBendFingers();
    }
}
