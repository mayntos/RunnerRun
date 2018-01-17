using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeFall : MonoBehaviour {

    public float fallRate;
    private RaycastHit lastGeneration;
    private string lastGenHit;
    private Rigidbody nodeRB;

    private void Awake()
    {
        nodeRB = gameObject.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update ()
    {
        transform.Rotate(new Vector3(0, 4, 0) * Time.deltaTime);
	}

    private void FixedUpdate()
    {

        nodeRB.AddForce(0, -30, 0);

        Ray ray = new Ray(gameObject.GetComponent<BoxCollider>().bounds.max, Vector3.left);
        if (Physics.Raycast(ray, out lastGeneration) && lastGenHit != lastGeneration.collider.tag)
        {
            lastGenHit = lastGeneration.collider.tag;
            TrackBuildScript.singleton.OpenLoop();
        }
    }
}
