using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamFollowScript : MonoBehaviour {

    //Store a reference to the runner
    public GameObject runner;

    //store a speed at which we want to follow
    public float followSpeed;

    //Fixed update is called automatically by MonoBehaviour
    void FixedUpdate()
    {
        //store the distance on every fixed update frame between the camera parent/transform and the ship's transform position.
        float superDistance = Vector3.Distance(transform.position, runner.transform.position);

        //Assign a new vector 3 to the transform's position that is calculated by the LERP function.
        //The LERP function takes 3 arguments a: where we are lerping from, b: where we are lerping to, c: where on the scale between the 2 we want to be
        transform.position = Vector3.Lerp(transform.position, runner.transform.position,followSpeed * superDistance * Time.deltaTime);
    }



    
}
