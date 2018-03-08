using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UserInstance : MonoBehaviour {

    public Text IndexText;
    public Text NameText;
    public Text AgeText;
    public Text GenderText;
    public Toggle SelectedToggle;

    public bool Selected => SelectedToggle.isOn;

    private int index;
    public int Index
    {
        set { index = value; IndexText.text = value.ToString(); }
        get { return index; }
    }
    private string name;
    public string Name
    {
        set { name = value; NameText.text = value.ToString(); }
        get { return name; }
    }
    private int age;
    public int Age
    {
        set { age = value; AgeText.text = value.ToString(); }
        get { return age; }
    }

    private bool gender;
    public bool Gender
    {
        set { gender = value; GenderText.text = value?"M":"F"; }
        get { return gender; }
    }

    public string UserInfo
    {
        get { return $"{Index}:{Name}:{Age}:{Gender}";}
    }

    public void ChangeUser()
    {
        UserInstance[] users = gameObject.transform.parent.GetComponentsInChildren<UserInstance>();
        foreach (var user in users)
        {
            if (user.Index != index) user.SelectedToggle.isOn = false;
        }
    }
}
