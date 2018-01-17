using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour {

    Transform nodeTransform;
    public Transform endMarker;
    Vector3 endMarkerReal;
    float dist;


	// Use this for initialization
	void Start () {
        nodeTransform = gameObject.GetComponent<Transform>();
        endMarkerReal = new Vector3(nodeTransform.position.x, endMarker.position.y, nodeTransform.position.z);
	}
	
	// Update is called once per frame
	void Update () {
        //nodeTransform.position += (Vector3.down) * Time.deltaTime * 0.25f;
        //dist = Vector3.Distance(nodeTransform.position, endMarkerReal);


       Debug.Log("node Time: " + Vector3.Distance(nodeTransform.position, endMarkerReal) / gameObject.GetComponent<Rigidbody>().velocity.magnitude);
	}
}
