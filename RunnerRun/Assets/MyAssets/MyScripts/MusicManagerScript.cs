using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManagerScript : MonoBehaviour
{
    public AudioSource transitionMusic;
    public AudioSource loopMusic;

    public bool raiseTransition = false;
    public bool raiseLoop = false;

	// Use this for initialization
	void Start ()
    {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
	    if(raiseLoop)
        {   
            if(loopMusic.volume == 1) { raiseLoop = false; }
            transitionMusic.volume -= 2 * Time.deltaTime;
            loopMusic.volume += 1 * Time.deltaTime;
        }	
        else if(raiseTransition)
        {
            if (transitionMusic.volume == 1) { raiseTransition = false; }
            loopMusic.volume -= 2 * Time.deltaTime;
            transitionMusic.volume += 1 * Time.deltaTime;
        }
	}

    public void playTransition()
    {
        if(loopMusic.isPlaying)
        { raiseTransition = true; }
        transitionMusic.Play();
    }

    public void playLoop()
    {
        if(transitionMusic.isPlaying)
        { raiseLoop = true; }
        loopMusic.Play();
    }
}
