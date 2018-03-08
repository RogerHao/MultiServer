using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ExperiementInfo : MonoBehaviour {
    public UserInfo UserInfo;

    public Text ProtocalNow;
    public Text DeviceNow;
    public Text ProcessNow;

    public Text UserNow;
    public Text TrainNumNow;
    public Text TrainNumTotal;


	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void SetUser()
    {
        foreach (var user in UserInfo.Users)
        {
            if (user.Selected) UserNow.text = user.Name;
        }
    }
}
