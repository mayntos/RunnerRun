using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackBuildScript : MonoBehaviour
{
    public static TrackBuildScript singleton;

    public GameObject[] track = new GameObject[7];      //array which holds the pool of Track prefabs. Straight-Lefts-Rights
    public GameObject[] loop = new GameObject[7];       //the four sides of a track. 

    public int loopSpawnPoint;

    GameObject ramp = null;                             //the ramp which is moved upon entering the track.
    private Quaternion rampRotation;

    private ArrayList currentLoop = new ArrayList();    //ArrayList which holds the sides of the current loop
    private GameObject loopTool;

    GameObject currentTrack;                            //temp variable which holds the NEXT Track to be placed.
    BoxCollider currentCollider;                        //variable which holds the Collider of the currentTrack object, this is used to determine bounds.center
    private bool canCreateTrack = false;                //boolean managed by Creation() and EnableCreation() methods. Determines whether we enter creation code or not.
    Vector3 finalPoint;                                 //the final Vector3 position at which instantiated tracks will end up.
    private bool canLerpLoop = false;

    private bool canSpawnLeft;

    private bool removeRamp = false;
    private bool returnRamp = false;

    private Vector3 rampLeavePosition = new Vector3(0, -25, 0);
    private Vector3 rampReturnPosition;

    public float lerpSpeed;                         //the speed at which instantiated track prefabs LERP into place.  
    private int trackCount;                         //number of levels created

    public Camera camReference;                     //MAY USE THIS REFERENCE TO CHANGE THE BACKGROUND COLOR EVERY TIME A NODE-LOOP IS COMPLETED.
    public GameObject node;

    private Vector3[] spawnDirections = { new Vector3(0, 25, 0), new Vector3(0, -25, 0), new Vector3(25, 0, 0)};        //the directions from which instantiated Tracks can LERP from.

    public Color[] colorSet1 = new Color[6];                                                                     //MAY USE THIS TO STORE COLORS.
    public Color[] colorSet2 = new Color[6];
    public Color[] colorSet3 = new Color[6];
    private ArrayList colorSwatch = new ArrayList();

    enum COLORSET { FIRST, SECOND, THIRD }; //indicate which color set is being pulled from.

    public MusicManagerScript musicRef;



    private void Awake()
    {
        singleton = this;                                                   //this script is utilized as a Singleton by CharControl2
        SetTrackAndCollider(GameObject.FindGameObjectWithTag("straight"));
    }

    // Use this for initialization
    void Start ()
    {
        colorSwatch.Add(colorSet1);
        colorSwatch.Add(colorSet2);
        colorSwatch.Add(colorSet3);
	} 
	
	// Update is called once per frame
	void Update ()
    {
        //every frame, check whether CharControl2 has given the OK to allow creation of the next Track prefab.
        if(canCreateTrack) { Creation(currentTrack.tag); }
        if (!currentTrack.name.Equals("TrackStraight") && !currentTrack.name.Contains("LoopSide") && !currentTrack.gameObject.tag.Equals("exitRamp"))
        {
            currentTrack.transform.position = Vector3.Lerp(currentTrack.transform.position, finalPoint, lerpSpeed * Time.deltaTime);
        }

        //if a loop has been created and all four sides have been instantiated, then we want to continously LERP them into their final positions. 
        if (canLerpLoop)
        {
            foreach(LoopNode x in currentLoop)
            {
                LERPTrack(x.getLoopPrefab(), x.getFinalPosition());
            }
        }

        //check to see if we can remove the ramp and "close" the loop.
        if(removeRamp)
        {
            if(Vector3.Distance(ramp.transform.position, rampLeavePosition) < 1 ) { removeRamp = false; rampLeavePosition = new Vector3(0, -25, 0); }
            ramp.transform.position = Vector3.Lerp(ramp.transform.position, rampLeavePosition, .2f * Time.deltaTime);
        }

        //check to see if we can move the ramp to the exit area of the loop.
        if(returnRamp)
        {
            ramp.transform.position = Vector3.Lerp(ramp.transform.position, rampReturnPosition, 2 * Time.deltaTime);
            ramp.transform.rotation = Quaternion.Slerp(ramp.transform.rotation, rampRotation, 2 * Time.deltaTime);
        }

    }

    /// <summary>
    /// Method which determines and instantiates the NEXT track to be placed, relative the PREVIOUS track that was placed.
    /// </summary>
    /// <param name="currentTag">indicator of what kind of Track the current Track is. This provides context as to which of the 7 tracks may be placed.</param>
    public void Creation(string currentTag)
    {
        trackCount++;
        canCreateTrack = false;
        UnityEngine.Random.seed = System.DateTime.Now.Millisecond;
        GameObject trackPrefab = null;      //reference to the Track Prefab which will be instantiated.
        if(trackCount == loopSpawnPoint)
        {
            returnRamp = false;
            CreateRamp(currentTag);         //create a ramp at the current position.
            trackCount = 0;                 //reset the track count.
            loopSpawnPoint++;               //increment the number of tracks it takes till a loop spawns.
        }
        else
        {
            if (currentTag.Equals("straight") || currentTag.Equals("outOfLeft") || currentTag.Equals("outOfRight"))     //if the currentTrack is a 'straight'...
            {
                if (currentTag.Equals("outOfRight") && trackCount == 2)
                {
                    if (UnityEngine.Random.value < 0.5f) { trackPrefab = track[0]; }
                    else { trackPrefab = track[2]; }
                }
                else
                { trackPrefab = track[UnityEngine.Random.Range(0, 3)]; }                                //then the next track that can be placed may go straight / left / right.
            }
            else if (currentTag.Equals("intoLeft") || currentTag.Equals("straightLeft"))    //if the currentTrack is oriented left...
            {
                trackPrefab = track[UnityEngine.Random.Range(3, 5)]; //then the next track that can be placed may continue going left, or turn right.
            }
            else if (currentTag.Equals("intoRight") || currentTag.Equals("straightRight") || currentTag.Equals("exitRamp") )  //if the currentTrack is oriented right...
            { 
                trackPrefab = track[UnityEngine.Random.Range(5, 7)];  //then the next track that can be placed may continue going right, or turn left. 
            }

            finalPoint = GenerateSpawnPoint(currentCollider, trackPrefab); //determine the final point at which the currentTrack prefab will end up. this is different from its instatiated point, below.
            SetTrackAndCollider(Instantiate(trackPrefab, finalPoint + spawnDirections[UnityEngine.Random.Range(0, spawnDirections.Length)], trackPrefab.transform.rotation)); 
            currentTrack.GetComponent<MeshRenderer>().material.color = getColor((COLORSET)UnityEngine.Random.Range(0, Enum.GetNames(typeof(COLORSET)).Length));     //fix found online: return a Random COLORSET enum value thru Random.Range(Enum.GetNames.Length returns the number of names/values in the enum.)
        }
    }

    /*
     * returns a Vector3 spawn point: which is an offset from the center of the previous Track.
     * conceptually: w/ the first Track placed, how is the second Track placed in relation to the first?
     * WHERE are we placing the generated track? We are placing it at the end of the current Track. 
     */
    private Vector3 GenerateSpawnPoint(Collider current, GameObject beingPlaced)
    {
        Vector3 spawnPoint = new Vector3();     //the Vector3 position to be returned.
        spawnPoint = current.bounds.center;     //to begin our calculation, we take the center of the previous track.
        if (current.gameObject.tag.Equals("straight") || current.gameObject.tag.Equals("outOfLeft") || (current.gameObject.tag.Equals("outOfRight")) || current.gameObject.tag.Equals("ramp")) //solved issue on right side by...removing the nested .Equals() statements.
        {
            if (beingPlaced.tag.Equals("intoLeft")) { spawnPoint += new Vector3(24.5f, 0, 25.5f); }
            else if (beingPlaced.tag.Equals("intoRight")) { spawnPoint += new Vector3(24.5f, 0, -25.5f); }
            else if(beingPlaced.tag.Equals("loopSide4")) { spawnPoint += new Vector3(-24.5f, 0, 25.5f); }
            else
            {
                if(beingPlaced.gameObject.name.Equals("LoopSide1")) { spawnPoint += new Vector3(51f, 0, 0);}
                else { spawnPoint += new Vector3(50f, 0, 0);}
                DeactivateShifter(current.gameObject);

            } //find the cornershifter of the CURRENT and disable it... two straights in a row.
        }
        else if (current.gameObject.tag.Equals("intoLeft") || currentTrack.tag.Equals("straightLeft"))
        {
            if (beingPlaced.tag.Equals("outOfLeft") || beingPlaced.tag.Equals("ramp")) { spawnPoint += new Vector3(25.5f, 0, 24.5f); }
            else if(beingPlaced.tag.Equals("loopSide3")) { spawnPoint += new Vector3(-25.5f, 0, 24.5f); }
            else { spawnPoint += new Vector3(0, 0, 50f); DeactivateShifter(current.gameObject); } //two straights in a row
        }
        else if (current.gameObject.tag.Equals("intoRight") || currentTrack.tag.Equals("straightRight") || currentTrack.tag.Equals("exitRamp"))
        {
            if (beingPlaced.tag.Equals("outOfRight") || beingPlaced.tag.Equals("ramp")) { spawnPoint += new Vector3(25.5f, 0, -24.5f); }
            else if(beingPlaced.tag.Equals("straightRight")){ spawnPoint += new Vector3(0, 0, -50f); DeactivateShifter(current.gameObject); } //two straights in a row
        }
        else if(current.gameObject.tag.Equals("loopSide3")) { spawnPoint += new Vector3(-24.5f, 0, -25.5f); }
        else if(current.gameObject.tag.Equals("loopSide4")) { spawnPoint += new Vector3(0, 0, -50f); }
        return spawnPoint;
    }

    /// <summary>
    /// Generate a Ramp and all four Tracks of a Loop, which will then be LERPed into place. DONT FORGET TO SET TRACKCOUNT = 0;
    /// </summary>
    /// <param name="c">the Track which was instantiated PRIOR to entrance into the Loop.</param>
    private void CreateRamp(string c)
    {
        //The Third Loop element controls the "Active" part of the Fourth Loop element's outer border and cornershifter.
        if(c.Equals("intoLeft") || c.Equals("straightLeft")) {ramp = loop[1];}
        else if(c.Equals("intoRight") || c.Equals("straightRight")) { ramp = loop[2]; }
        else if(c.Equals("straight") || c.Equals("outOfLeft") || c.Equals("outOfRight")) { ramp = loop[0];}

        finalPoint = GenerateSpawnPoint(currentCollider, ramp);
        ramp = Instantiate(ramp, finalPoint + spawnDirections[UnityEngine.Random.Range(0, spawnDirections.Length)], ramp.transform.rotation);
        SetTrackAndCollider(ramp);
    }

    /// <summary>
    /// instantiates 4 sides of a Loop and stores reference to those instantiations in currentLoop array for use by LERPTrack().
    /// </summary>
    public void CreateLoop()
    {
        foreach(GameObject gameObj in GameObject.FindObjectsOfType<GameObject>())
        {
            if(gameObj.name.Contains("LoopSide") || gameObject.name.Contains("Node"))
            {
                Destroy(gameObj);
            }
        }

        //get the final positions
        for(int i = 3; i < loop.Length-1; i++)
        {
            GameObject loopPrefab = loop[i];
            finalPoint = GenerateSpawnPoint(currentCollider, loopPrefab);
            currentLoop.Add(new LoopNode(currentTrack = Instantiate(loopPrefab, finalPoint, loopPrefab.transform.rotation), finalPoint)); 
            currentCollider = currentTrack.GetComponent<BoxCollider>();
        }

        rampRotation = currentTrack.transform.rotation;
        Instantiate(node, currentCollider.bounds.center + new Vector3(25, 40, 0), node.transform.rotation);         //******take a look at this later.

        musicRef.playLoop();

        //make the sides of the loop appear in their offset positions...
        foreach(LoopNode x in currentLoop)
        {
            x.getLoopPrefab().transform.position += spawnDirections[UnityEngine.Random.Range(0, spawnDirections.Length)];
            MeshRenderer rendererRef = x.getLoopPrefab().GetComponent<MeshRenderer>();
            rendererRef.material.color = getColor((COLORSET)UnityEngine.Random.Range(0, Enum.GetNames(typeof(COLORSET)).Length));
            rendererRef.enabled = true;
        }

        canLerpLoop = true;

    }

    /// <summary>
    /// shortcut for LERPing the sides of a Loop.
    /// </summary>
    /// <param name="t">the GameObject which will be LERPed.</param>
    /// <param name="finalP">the position we want it to end up in.</param>
    private void LERPTrack(GameObject t, Vector3 finalP)
    {
        t.transform.position = Vector3.Lerp(t.transform.position, finalP, lerpSpeed * Time.deltaTime);
    }


    public void CloseLoop()
    {
        removeRamp = true;
        canLerpLoop = false;
        currentLoop.Clear();

        rampLeavePosition += ramp.transform.position;
        //Candidate for being turned into method----------------------------
        GameObject Side4 = GameObject.FindGameObjectWithTag("loopSide4");
        BoxCollider[] b = Side4.GetComponentsInChildren<BoxCollider>();
        SphereCollider[] s = Side4.GetComponentsInChildren<SphereCollider>();
        foreach(BoxCollider g in b)
        {
            if(g.gameObject.name.Contains("Border3Outer")) { g.enabled = true; }
        }
        foreach(SphereCollider x in s)
        {
            if (x.gameObject.name.Contains("CornerShifter")) { x.enabled = true; }
        }
        //-----------------------------------

    }

    public void OpenLoop()
    {
        musicRef.playTransition();
        removeRamp = false;
        GameObject newRamp = Instantiate(loop[7], ramp.transform.position, ramp.transform.rotation);
        Destroy(ramp);
        ramp = newRamp;
        rampReturnPosition = GenerateSpawnPoint(currentCollider, ramp); //we set desired rotation before we get into returning the ramp.
        returnRamp = true;

        //candidate for being turned into method-----
        GameObject Side1 = GameObject.Find("LoopSide1(Clone)");
        GameObject Side4 = GameObject.Find("LoopSide4(Clone)");

        BoxCollider[] b = Side1.GetComponentsInChildren<BoxCollider>();
        foreach(BoxCollider x in b)
        {
            if (x.gameObject.name.Contains("Border1Outer")) { x.enabled = false; }
        }

        SphereCollider[] s = Side4.GetComponentsInChildren<SphereCollider>();
        foreach(SphereCollider x in s)
        {
            if (x.gameObject.name.Contains("CornerShifter")) { x.enabled = false; }
        }
        //---------------------
        SphereCollider[] sc = ramp.GetComponentsInChildren<SphereCollider>();
        foreach (SphereCollider x in sc)
        {
            if (x.gameObject.name.Contains("CornerShifter")) { x.enabled = true; }
        }

        ramp.tag = "exitRamp";
        SetTrackAndCollider(ramp);
    }


    /*
     * Deactivates the CornerShifter of a specified game object.
     * If the SphereCollider is a CornerShifter, then we want to disable it to allow acceleration.
     */
    private void DeactivateShifter(GameObject x)
    {
        SphereCollider[] s = x.GetComponentsInChildren<SphereCollider>();
        foreach(SphereCollider y in s)
        {
            if(y.gameObject.name.Contains("CornerShifter")) { y.enabled = false; y.gameObject.tag = "passedCorner"; }
        }
    }

    /// <summary>
    /// Relevant to use as a singleton, CharControl2 uses this method to allow Creation() to take place.
    /// </summary>
    public void EnableCreation()
    {
        canCreateTrack = true;
    }

    /// <summary>
    /// function for returning a randomized color from our predetermined color swatches.
    /// </summary>
    /// <param name="set">enum which helps limit the potential index (the specific array 0, 1, 2) Color array we are picking from.</param>
    /// <returns>returns a random Color from one of the swatches.</returns>
    private Color getColor(COLORSET set)
    {
        Color desiredColor;             //the color we are returning thru this function.
        int index = (int)set;
        Color[] colorTemp = (Color[])colorSwatch[index];
        desiredColor = colorTemp[UnityEngine.Random.Range(0, colorTemp.Length)];

        return desiredColor;
    }

    /// <summary>
    /// function which takes a gameobject as a parameter, sets it to currentTrack, and sets it's collider to currentCollider as well.
    /// currentTrack is generally used to move track components around
    /// currentCollider is used to calculate endpoint of tracks.
    /// </summary>
    private void SetTrackAndCollider(GameObject g)
    {
        currentTrack = g;
        currentCollider = g.GetComponent<BoxCollider>();
    }
    

    //class for my Loop sides...
    public class LoopNode
    {
        private GameObject loopPrefab = null;
        private Vector3 finalPosition = new Vector3(0, 0, 0);

        public LoopNode(GameObject g, Vector3 fp)
        {
            loopPrefab = g;
            finalPosition = fp;
        }

        public GameObject getLoopPrefab()
        {
            return loopPrefab;
        }

        public Vector3 getFinalPosition()
        {
            return finalPosition;
        }
    }

}
 