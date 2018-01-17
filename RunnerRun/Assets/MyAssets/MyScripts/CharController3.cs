using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharController3 : MonoBehaviour
{

    //variables related to speed
    public float moveSpeed;
    public float stepCount = 0;        //track number of steps, so that we can multiply by the number of times the user has tapped in between FixedUpdate frames.
    public int currentGear = 1;        //track current gear, so that we can use this as an additional multiplier.
    public float moveCooldown;         //cooldown before all multipliers are tallied and applied to Runner's speed.
    private float previousSpeed;       //the speed of Runner before it enters a corner.
    enum gear { Jog, Stride, Sprint }; //the 3 potential running states for the Runner.


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
    private bool canLoop = false;       //this variable is switched in the 3rd corner; indicates that the user is able to start looping. this prevents the beginning corner shifter from changing the rotation of the Runner.
    private Collider lastColliderHit;   //*****May be able to remove this variable.


    //ShifterSlider variables
    public float sliderSlideRate;       //the speed at which the Slider value increases.
    public float shiftZoneScale;        //width of the shift zone's RectTransform
    public float successZoneMaxPosition;//from 0-160, the max value at which the successZone may begin.
    public float successZoneMinSize;    //the base width value for the success zone
    public float successZoneMaxSize;    //the max width value for the success zone. random value between min and max.


    //references
    Rigidbody rb;
    public Slider shifterSlider;
    public Slider sprintTimeSlider;
    public RectTransform successZone;
    public Text currentGearDisplay;



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
        //Shoots a Raycast upwards. If returns true, store the velocity of the runner before it enters a corner.
        if (Physics.Raycast(transform.position, Vector3.up, 2)) { previousSpeed = rb.velocity.magnitude; }

        canMoveLeftFoot = Input.GetKeyDown(KeyCode.J);
        canMoveRightFoot = Input.GetKeyDown(KeyCode.K);
        if (canMoveLeftFoot && !turn) { turn = true; stepCount++; } //the player MUST press P after O, in order to move.
        else if (canMoveRightFoot && turn) { turn = false; stepCount++; } //the player MUST press O after P, in order to move
        //in update, we keep track of the number of steps & we apply that number of steps to Move() in FixedUpdate.


        if ((Input.GetKeyUp(KeyCode.F) || Input.GetKeyUp(KeyCode.D)))
        {
            //if we release shifter button within the success zone, THEN we can increment or decrement gears.
            if (shifterSlider.value >= successZone.anchoredPosition.x && shifterSlider.value <= (successZone.anchoredPosition.x + successZone.rect.width))
            {
                if (Input.GetKeyUp(KeyCode.F) && currentGear < 2) { currentGear++; }
                else if (Input.GetKeyUp(KeyCode.F) && currentGear == 2 && sprintCoolDownTime <= 0.0f) { currentGear++; } //not able to enter Sprint unless cooldownTime <= 0.
                else if (Input.GetKeyUp(KeyCode.D)) { currentGear--; }
                switch (currentGear) //responsible for updating text display of current gear. *May have to change this to Jog | Stride | Sprint *Done 5/28/17
                {
                    case 1:
                        isSprinting = false;
                        currentGearDisplay.text = "[Jog]\t" + "Stride\t" + "Sprint";
                        break;
                    case 2:
                        isSprinting = false;
                        currentGearDisplay.text = "Jog\t" + "[Stride]\t" + "Sprint";
                        break;
                    case 3:
                        isSprinting = true; //runner has entered Sprint.
                        currentGearDisplay.text = "Jog\t" + "Stride\t" + "[Sprint]";
                        break;
                    default:
                        break;
                }
            }
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
            if (sprintTime >= sprintLimitTime && currentGear == 3)
            {
                decrementGear();
            }
        }
        else { sprintCoolDownTime -= Time.deltaTime; }
    }

    /*
     * OnTriggerEnter function is responsible for 
     */
    void OnTriggerEnter(Collider other)
    {
        {
            //Debug.Log("Enter velocity: " + rb.velocity);
            if (other == lastColliderHit) { return; }
            else
            {
                //upon entering a corner trigger, we want to rotate 90 degrees
                lastColliderHit = other; //ensure that we dont instantly bump into the same collider twice. 
                if (other.tag == "corner1") { transform.LookAt(GameObject.FindWithTag("corner2").transform); }
                else if (other.tag == "corner2") { transform.LookAt(GameObject.FindWithTag("corner3").transform); }
                else if (other.tag == "corner3") { transform.LookAt(GameObject.FindWithTag("corner4").transform); canLoop = true; } //once we reach the final corner, give the player the option to Lap.
                else if (other.tag == "corner4" && canLoop) { transform.LookAt(GameObject.FindWithTag("corner1").transform); }

                rb.AddRelativeForce(Vector3.forward * previousSpeed, ForceMode.VelocityChange); //after we change rotation, then we reapply the entry speed. (since the velocity is 0 prior to this point.)
            }
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
     * function for decreasing the current gear, out of Sprint mode / turns off Sprinting boolean, and resets sprint time/slider value, and sets Cooldown Time.
     */
    public void decrementGear()
    {
        currentGear--;
        isSprinting = false;
        currentGearDisplay.text = currentGearDisplay.text = "Jog\t" + "[Stride]\t" + "Sprint";
        sprintTime = 0.0f;
        sprintTimeSlider.value = 0.0f;
        sprintCoolDownTime = 10f;
    }
}
