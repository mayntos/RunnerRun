using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharControl2 : MonoBehaviour {

    //variables related to speed
    public float moveSpeed;
    public float stepCount = 0;     //track number of steps, so that we can multiply by the number of times the user has tapped in between FixedUpdate frames.
    public int currentGear = 1;     //track current gear, so that we can use this as an additional multiplier.
    public float moveCooldown;      //cooldown before all multipliers are tallied and applied to Runner's speed.
    private float previousSpeed;    //the speed of Runner before it enters a corner.


    //sprint variables
    public float sprintTime = 0.0f;     //amount of time the Runner spends in Sprint phase.
    public float sprintLimitTime;       //the maximum amount of the Runner may spend in the Sprint phase.
    public float sprintCoolDownTime;    //the amount of time which must pass before the Runner can reenter the Sprint phase.
    private bool isSprinting = false;   //check to enter the block of code in Update function which manages Sprint action.

    //variables related to button presses
    private bool canMoveLeftFoot = false; 
    private bool canMoveRightFoot = false;
    private bool turn = false;          //may have to refactor this variable lol //bool which maintains whether we may alternate key presses.

    //misc variables
    private bool canLoop = false;              //this variable is switched in the 3rd corner; indicates that the user is able to start looping. this prevents the beginning corner shifter from changing the rotation of the Runner.
    private Collider lastColliderHit = null;   //variable utilized to ensure that we are not bumping into the same collider twice in a corner.
    private RaycastHit lastGeneration;         //variable which stores the RaycastHit in Update(). This allows me to get more information (tag, name, etc.) about the gameobject which the hit collider is attached to.
    private Collider lastGenHit;               //the Collider of the previous gameobject we hit.


    //ShifterSlider variables
    public float sliderSlideRate;       //the speed at which the Slider value increases.
    public float shiftZoneScale;        //width of the shift zone's RectTransform
    public float successZoneMaxPosition;//from 0-160, the max value at which the successZone may begin.
    public float successZoneMinSize;    //the base width value for the success zone
    public float successZoneMaxSize;    //the max width value for the success zone. random value between min and max.

    
    //references
    Rigidbody rb;
    public Slider shifterSlider;               //Overarching shifter slider.
    public Slider sprintTimeSlider;            //small grey space which indicates successful shift to sprint.
    public RectTransform successZone;          //the transform of the successzone space, which ends up being randomized upon every activation.
    public Text currentGearDisplay;            //the display of the current gear.



    public Animator myAnim;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>(); //grab this object's Rigidbody
    }

    // Use this for initialization
    void Start()
    {
        currentGearDisplay.text = "[Jog]\t" + "Stride\t" + "Sprint";
        sprintTimeSlider.value = 0f;
        
    }

    void Update()
    {
        //generate a new Ray for use in update.
        Ray ray = new Ray(transform.position, Vector3.up);
        if (Physics.Raycast(ray, out lastGeneration) && lastGenHit != lastGeneration.collider)
        {
            lastGenHit = lastGeneration.collider;
            switch (lastGeneration.collider.tag)
            {
                case "generate":
                    TrackBuildScript.singleton.EnableCreation();
                    break;

                case "ramp":
                    TrackBuildScript.singleton.CreateLoop();
                    break;

                case "closeLoop":
                    lastGeneration.collider.tag = "Untagged";
                    TrackBuildScript.singleton.CloseLoop();
                    break;

                default:
                    break;
            };
        }

        //Debug.DrawRay(transform.position, forward, Color.cyan);
        //Shoots a Raycast upwards. If returns true, store the velocity of the runner before it enters a corner.
        if (Physics.Raycast(transform.position, Vector3.up, 2)) { previousSpeed = rb.velocity.magnitude;}


        canMoveLeftFoot = Input.GetKeyDown(KeyCode.J);
        canMoveRightFoot = Input.GetKeyDown(KeyCode.K);
        if (canMoveLeftFoot && !turn) { turn = true; stepCount++;}           //the player MUST press K after J, in order to move.
        else if (canMoveRightFoot && turn) { turn = false; stepCount++;}     //the player MUST press J after K, in order to move.
        //in update, we keep track of the number of steps & we apply that number of steps to Move() in FixedUpdate.


        if ((Input.GetKeyUp(KeyCode.F) || Input.GetKeyUp(KeyCode.D)))
        {
            //if we release shifter button WITHIN the success zone, THEN we can increment or decrement gears.
            if ((shifterSlider.value >= successZone.anchoredPosition.x) && (shifterSlider.value <= (successZone.anchoredPosition.x + successZone.rect.width)))
            {
                //We may only move up a gear if we are in first gear OR we are in second gear + we are not in sprint cooldown. 
                if ((Input.GetKeyUp(KeyCode.F)) && ((currentGear == 1) || (currentGear == 2 && sprintCoolDownTime <= 0.0f))) { currentGear++; }

                //We may decrement gears at any point, in fact one may downshift from Sprint in order to conserve sprint time.
                else if (Input.GetKeyUp(KeyCode.D)) { currentGear--; }

                DisplayGear();
            }
            //responsible for updating text display of current gear.

            shifterSlider.value = 0.0f; //reset the slider value
        }

        //I think the creation and use of Shifter() function here was a smart decision.
        if (Input.GetKey(KeyCode.F) && currentGear < 3) { Shifter(); }      //if the current gear level is below 1 or 2, then we can move up a gear.
        else if (Input.GetKey(KeyCode.D) && currentGear > 1) { Shifter(); } //if the current gear level is greater than 1 (....2 or 3) then we can shift down.
        else { shifterSlider.gameObject.SetActive(false); }


        //if Runner is Sprinting, then we count sprint time until it hits the SprintLimitTime.
        if (isSprinting)
        {
            IncrementTime();
            if(sprintTime >= sprintLimitTime && currentGear == 3)
            {
                decrementGear();
                myAnim.SetBool("canJog", true);
                myAnim.SetBool("canRun", false);
            }
        }
        else { sprintCoolDownTime -= Time.deltaTime;}
    }

    /*
     * OnTriggerEnter function is responsible for storing speed prior to entering a corner, for re-application after exiting the corner.
     */
    void OnTriggerEnter(Collider other)
    {
        {
            if (other == lastColliderHit) { return; }
            if(!other.gameObject.tag.Equals("nextCorner"))
            {
                //score and score multiplier stuff goes here
                if (other.gameObject.tag.Equals("corner1")) { transform.LookAt(GameObject.FindWithTag("corner2").transform); }
                else if (other.gameObject.tag.Equals("corner2")) { transform.LookAt(GameObject.FindWithTag("corner3").transform);  }
                else if (other.gameObject.tag.Equals("corner3")) { transform.LookAt(GameObject.FindWithTag("corner4").transform); }
                else if (other.gameObject.tag.Equals("corner4")) { transform.LookAt(GameObject.FindWithTag("corner1").transform); }
                //point to each corner of the Loop.
            }
            else
            {
                other.gameObject.tag = "passedCorner"; //THANK YOU KEVIN!
                transform.LookAt(GameObject.FindWithTag("nextCorner").transform); 
            }

            //regardless of the type of corner we enter, we need to apply speed in order for the the cube to carry its momentum.
            rb.AddRelativeForce(Vector3.forward * previousSpeed, ForceMode.VelocityChange); //after we change rotation, then we reapply the entry speed. (since the velocity is 0 prior to this point.)
            lastColliderHit = other;
        }
    }



    private void FixedUpdate()
    {

        //this if-else block is responible for ticking Move Cooldown and calling the Move() function.
        if (moveCooldown < 0 && stepCount > 0)
        {
            Move();
            moveCooldown = .2f; //comment these out in order to test velocity
            stepCount -= (stepCount / 2);  //comment these out in order to test velocity.
        }
        else { moveCooldown -= Time.deltaTime; }
    }


    /**
     * adds force to the Runner's rigidbody. simple multiplier of designated move speed * the current gear * the number of button presses in between fixed-update calls.
     */
    private void Move()
    {
        rb.AddRelativeForce(Vector3.forward * moveSpeed * currentGear * stepCount); 
    }


    /**
     * displays the Shifter slider and increments it's value at a designated rate in order to slide it.
     */
    private void Shifter()
    {
         //----beginning of Gear Shift code
        if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.D))
        {
            successZone.sizeDelta = new Vector2(Random.Range(successZoneMinSize, successZoneMaxSize), 10);
            successZone.anchoredPosition = new Vector2(Random.Range(0, successZoneMaxPosition), 0);
        } //set the Success Zone's size. 
        shifterSlider.gameObject.SetActive(true); //display the shifterSlider
        if (shifterSlider.value < 160)
        {
            shifterSlider.value += (sliderSlideRate * Time.deltaTime);
        } //increment the slider's value
        else { shifterSlider.value = 0.0f; } //once the slider's value hits the cap, reset it.
    }    


    /*
     * function for updating/incrementing my "time" float variables.
     */
    private void IncrementTime()
    {
        sprintTime += 1 * Time.deltaTime;
        sprintTimeSlider.value += 1 * Time.deltaTime;
    }

    /*
     * function for decreasing the current gear, out of Sprint mode 
     * turns off Sprinting boolean, and resets sprint time/slider value
     * sets Cooldown Time.
     */
    public void decrementGear()
    {
        currentGear--;
        isSprinting = false;
        currentGearDisplay.text = "Jog\t" + "[Stride]\t" + "Sprint";
        sprintTime = 0.0f;
        sprintTimeSlider.value = 0.0f;
        sprintCoolDownTime = 10f;
    }


    /// <summary>
    /// function responsible for determining which string of text we should display in scene.
    /// once gear display is changed, we must also note whether or not we are sprinting.
    /// </summary>
    private void DisplayGear()
    {
        switch (currentGear)
        {
            case 1:
                currentGearDisplay.text = "[Jog]\t" + "Stride\t" + "Sprint";
                break;

            case 2:
                isSprinting = false;
                currentGearDisplay.text = "Jog\t" + "[Stride]\t" + "Sprint";
                myAnim.SetBool("canJog", true);
                break;

            case 3:
                isSprinting = true; //runner has entered Sprint.
                currentGearDisplay.text = "Jog\t" + "Stride\t" + "[Sprint]";
                myAnim.SetBool("canJog", false);
                myAnim.SetBool("canRun", true);
                break;

            default:
                break;
        }
    }
}
