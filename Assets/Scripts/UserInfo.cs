using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UserInfo : MonoBehaviour {
    public InputField NameInput;
    public InputField AgeInput;
    public Dropdown GenderInput;


    public UserInstance UserInstance;
    public GameObject UserContent;
    public List<UserInstance> Users = new List<UserInstance>();

    
    // Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void AddUser()
    {
        if (string.IsNullOrEmpty(NameInput.text)) return;
        int age;
        int.TryParse(AgeInput.text, out age);
        if (age > 80 || age < 10) return;
        UserInstance newuser = Instantiate(UserInstance, UserContent.transform);
        newuser.Name = NameInput.text;
        newuser.Index = Users.Count + 1;
        newuser.Age = age;
        newuser.Gender = GenderInput.value == 0;
        Users.Add(newuser);
    }

    public void RemoveUser()
    {
        GameObject userObj = Users[Users.Count - 1].gameObject;
        Users.RemoveAt(Users.Count - 1);
        Destroy(userObj);
    }
}
