using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using HandPhysicsExtenstions;

[RequireComponent(typeof(HandPhysicsController))]
public class HandPhysicsUnetInput : MonoBehaviour
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
        Rest,
        State1,
        State2
    }

    public GestureState ForearmState = GestureState.Rest;
    public GestureState WristState = GestureState.Rest;
    public GestureState HandState = GestureState.Rest;
    
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
	    if (Input.GetKeyDown(KeyCode.Alpha1))
	        StartCoroutine(Open(true));
	    else if (Input.GetKeyDown(KeyCode.Alpha2))
	        StartCoroutine(Close(true));
	    else if (Input.GetKeyDown(KeyCode.Alpha0))
	        HandRest();
	    else if (Input.GetKeyDown(KeyCode.Alpha4))
	        Controller.StartBendFingers();
    }

    //  wrist flexion, wrist extension, wrist supination, wrist pronation, hand open, hand closed, and no movement
    public IEnumerator Pronation(bool pos)
    {
        if (ForearmState != GestureState.Rest) yield break;
        for (int i = 0; i < 15; i++)
        {
            Controller.RotateForearm(pos?1f:-1f);
            yield return new WaitForSeconds(0.05f);
        }
        ForearmState = pos ? GestureState.State1:GestureState.Rest;
    }

    public IEnumerator Supination(bool pos)
    {
        if (ForearmState != GestureState.Rest) yield break;
        for (int i = 0; i < 20; i++)
        {
            Controller.RotateForearm(pos ? -0.45f:0.45f);
            yield return new WaitForSeconds(0.05f);
        }
        ForearmState = pos ? GestureState.State2 : GestureState.Rest;
    }

    public IEnumerator FormArmRest()
    {
        if (ForearmState == GestureState.Rest) yield break;
        switch (ForearmState)
        {
            case GestureState.State1:
                ForearmState = GestureState.Rest;
                StartCoroutine(Pronation(false));
                break;
            case GestureState.State2:
                ForearmState = GestureState.Rest;
                StartCoroutine(Supination(false));
                break;
        }
    }

    public IEnumerator Flexion(bool pos)
    {
        if (WristState != GestureState.Rest) yield break;
        for (int i = 0; i < 10; i++)
        {
            Controller.RotateWrist(pos ? -1f : 1f);
            yield return new WaitForSeconds(0.1f);
        }
        WristState = pos ? GestureState.State1 : GestureState.Rest;
    }

    public IEnumerator Extension(bool pos)
    {
        if (WristState != GestureState.Rest) yield break;
        for (int i = 0; i < 10; i++)
        {
            Controller.RotateWrist(pos ? 1f : -1f);
            yield return new WaitForSeconds(0.1f);
        }
        WristState = pos ? GestureState.State2 : GestureState.Rest;
    }

    public IEnumerator WristRest()
    {
        if (WristState == GestureState.Rest) yield break;
        switch (WristState)
        {
            case GestureState.State1:
                WristState = GestureState.Rest;
                StartCoroutine(Flexion(false));
                break;
            case GestureState.State2:
                WristState = GestureState.Rest;
                StartCoroutine(Extension(false));
                break;
        }
    }

    public IEnumerator Open(bool pos)
    {
        if (HandState != GestureState.Rest) yield break;
        for (var index = 0; index < _fingers.Length; index++)
        {
            var fingerPart = _fingers[index];
            fingerPart.TargetRotation = openfingerTarget[index];
        }
        Controller.StartBendFingers();
        HandState = pos ? GestureState.State1 : GestureState.Rest;
    }

    public IEnumerator Close(bool pos)
    {
        if (HandState != GestureState.Rest) yield break;
        for (var index = 0; index < _fingers.Length; index++)
        {
            var fingerPart = _fingers[index];
            fingerPart.TargetRotation = defaultfingerTarget[index];
        }
        Controller.StartBendFingers();
        HandState = pos ? GestureState.State2 : GestureState.Rest;
    }

    public void HandRest()
    {
        if (HandState == GestureState.Rest) return;
        Controller.StopBendFingers();
        HandState = GestureState.Rest;
    }


}
