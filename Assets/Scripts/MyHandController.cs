using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using HandPhysicsExtenstions;

[RequireComponent(typeof(HandPhysicsController))]
public class MyHandController : MonoBehaviour
{
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

    private int _supnationCount = 0;
    private int _felxionCount = 0;

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
//	void Update ()
//	{
//        if(Input.GetKeyDown(KeyCode.Alpha1)) OpenSequence();
//        if(Input.GetKeyDown(KeyCode.Alpha2)) CloseSequence();
//        if(Input.GetKeyDown(KeyCode.Alpha3)) IClose();
//
//        if (!ServerCommandNew) return;
//	    ServerCommandNew = false;
//	    switch (ServerCommand)
//	    {
//	        case 0:
//	            Rest();
//	            break;
//	        case 1:
//	        case 11:
//	            IClose();
//	            break;
//	        case 2:
//	        case 22:
//	            IOpen();
//	            break;
//	        case 3:
//	        case 33:
//	            ISupination();
//	            break;
//	        case 4:
//	        case 44:
//	            IPronation();
//	            break;
//	        case 5:
//	        case 55:
//	            IFlexion();
//	            break;
//	        case 6:
//	        case 66:
//	            IExtension();
//	            break;
//            case 7:
//                CloseSequence();
//                break;
//	        case 8:
//	            OpenSequence();
//	            break;
//
//        }
//    }

    /// <summary>
    /// Seven Gesture Public Method
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    /// 
    public void ISupination(bool pos = true)
    {
        StartCoroutine(Supination(pos));
    }
    private IEnumerator Supination(bool pos=true)
    {
        if(pos && TrainGestureState != GestureState.NoMovement) yield break;
        if(pos && TrainGestureState == GestureState.Supination) yield break;
        for (int i = 0; i < 20; i++)
        {
            Controller.RotateForearm(pos ? -0.45f:0.45f);
            yield return new WaitForSeconds(0.05f);
        }
        _supnationCount = _supnationCount + Convert.ToInt32(20 * 0.45f / 0.083f);
        TrainGestureState = pos ? GestureState.Supination : GestureState.NoMovement;
    }

    public void IPronation(bool pos = true)
    {
        StartCoroutine(Pronation(pos));
    }
    private IEnumerator Pronation(bool pos = true)
    {
        if (pos && TrainGestureState != GestureState.NoMovement) yield break;
        if (pos && TrainGestureState == GestureState.Pronation) yield break;
        for (int i = 0; i < 15; i++)
        {
            Controller.RotateForearm(pos?1f:-1f);
            yield return new WaitForSeconds(0.05f);
        }
        _supnationCount = _supnationCount - Convert.ToInt32(15 * 1f / 0.083f);
        TrainGestureState = pos ? GestureState.Pronation : GestureState.NoMovement;
    }

    public void IFlexion(bool pos = true)
    {
        StartCoroutine(Flexion(pos));
    }
    private IEnumerator Flexion(bool pos=true)
    {
        if (pos && TrainGestureState != GestureState.NoMovement) yield break;
        if (pos && TrainGestureState == GestureState.Flexion) yield break;
        for (int i = 0; i < 10; i++)
        {
            Controller.RotateWrist(pos ? -1f : 1f);
            yield return new WaitForSeconds(0.1f);
        }
        _felxionCount = _felxionCount + Convert.ToInt32(10 * 1f / 0.1111f);
        TrainGestureState = pos ? GestureState.Flexion : GestureState.NoMovement;
    }

    public void IExtension(bool pos = true)
    {
        StartCoroutine(Extension(pos));
    }
    private IEnumerator Extension(bool pos = true)
    {
        if (pos && TrainGestureState == GestureState.Extension) yield break;
        if (pos && TrainGestureState != GestureState.NoMovement) yield break;
        for (int i = 0; i < 10; i++)
        {
            Controller.RotateWrist(pos ? 1f : -1f);
            yield return new WaitForSeconds(0.1f);
        }
        _felxionCount = _felxionCount - Convert.ToInt32(10 * 1f / 0.1111f);
        TrainGestureState = pos ? GestureState.Extension : GestureState.NoMovement;
    }

    public void IOpen(bool pos = true)
    {
        StartCoroutine(Open(pos));
    }
    private IEnumerator Open(bool pos = true)
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
        StartCoroutine(Close(pos));
    }
    private IEnumerator Close(bool pos = true)
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
        Controller.StopBendFingers();
        Controller.StartBendFingers();
        Controller.StopBendFingers();

        for (int i = 0; i < Mathf.Abs(_felxionCount); i++)
        {
            yield return new WaitForSeconds(0.1f);
            Controller.RotateWrist(_felxionCount >= 0 ? 0.1111f : -0.1111f);
        }
        for (int i = 0; i < Mathf.Abs(_supnationCount); i++)
        {
            yield return new WaitForSeconds(0.1f);
            Controller.RotateForearm(_supnationCount >= 0 ? 0.083f : -0.083f);
        }
        _supnationCount = 0;
        _felxionCount = 0;
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

    public void SupinationAndPronation(bool sup)
    {
        if (sup) _supnationCount++;
        else _supnationCount--;
        Controller.RotateForearm(sup?-0.083f:0.083f);
    }

    public void FlexionAndExtension(bool fle)
    {
        if (fle) _felxionCount++;
        else _felxionCount--;
        Controller.RotateWrist(fle ? -0.1111f : 0.1111f);
    }

    public void CloseSequence()
    {
        Controller.StartBendFingersAmount();
    }

    public void OpenSequence()
    {
        Controller.StopBendFingersAmount();
    }

}
