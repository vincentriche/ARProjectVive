using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoldMyBeer : MonoBehaviour
{
    public SteamVR_TrackedObject trackedObj;
    private SteamVR_Controller.Device Controller
    {
        get
        {
            if (trackedObj.index == SteamVR_TrackedObject.EIndex.None)
                return null;
            return SteamVR_Controller.Input((int)trackedObj.index);
        }
    }

    public Transform baseBeerTransform;
    public Transform baseBallTransform;
    public GameObject ball;

    private void Update()
    {
        if (ball == null)
            ball = GameObject.Find("BasketBall");
        if (ball == null)
            return;

        if (Controller.GetPress(Valve.VR.EVRButtonId.k_EButton_ApplicationMenu) == true)
        {
            ball.transform.GetComponent<Rigidbody>().velocity = Vector3.zero;
            ball.transform.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            ball.transform.position = baseBallTransform.position;
            ball.transform.rotation = Quaternion.identity;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {  
        if (collision.transform.CompareTag("Beer") == true)
        {
            collision.transform.GetComponent<Rigidbody>().velocity = Vector3.zero;
            collision.transform.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            collision.transform.position = baseBeerTransform.position;
            collision.transform.rotation = Quaternion.identity;

            GameObject cap = GameObject.Find("Caps");
            cap.transform.GetComponent<Rigidbody>().velocity = Vector3.zero;
            cap.transform.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            cap.transform.position = baseBeerTransform.position + baseBeerTransform.up * 0.1f;
            cap.transform.rotation = Quaternion.identity;
        }
    }
}
