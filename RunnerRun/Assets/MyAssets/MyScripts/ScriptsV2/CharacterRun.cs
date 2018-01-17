using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterRun : MonoBehaviour {

    public Animator myAnim;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown((KeyCode.W)))
        {
            myAnim.SetBool("canWalk", true);
        }
        else if(Input.GetKeyUp(KeyCode.W))
        {
            myAnim.SetBool("canWalk", false);
        }
	}
}
