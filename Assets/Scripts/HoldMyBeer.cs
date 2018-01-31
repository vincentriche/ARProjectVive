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
    public Transform baseBoxTransform;

    private GameObject[] sceneObjects;
    private GameObject beer;
    private GameObject caps;


    private void Update()
    {
        if (beer == null)
            beer = GameObject.Find("Beer");
        if (beer == null)
            return;

        if (Controller.GetPress(Valve.VR.EVRButtonId.k_EButton_ApplicationMenu) == true)
        {
            sceneObjects = GameObject.FindGameObjectsWithTag("Target");
            if (sceneObjects != null)
                ResetObjects();
        }
    }

    private void ResetObjects()
    {
        Vector3 yOffset = Vector3.zero;
        yOffset.y = baseBoxTransform.position.y;
        for (int i = 0; i < sceneObjects.Length; i++)
        {
            sceneObjects[i].transform.GetComponent<Rigidbody>().velocity = Vector3.zero;
            sceneObjects[i].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            sceneObjects[i].transform.position = baseBoxTransform.position + yOffset;
            sceneObjects[i].transform.rotation = Quaternion.identity;
            yOffset.y += 0.2f;
        }

        GameObject ball = GameObject.Find("BasketBall");
        ball.transform.position = baseBallTransform.position;
        ball.transform.rotation = Quaternion.identity;
        ball.GetComponent<Rigidbody>().velocity = Vector3.zero;
        ball.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        ResetBeerAndCaps();
    }

    private void ResetBeerAndCaps()
    {
        beer.transform.GetComponent<Rigidbody>().velocity = Vector3.zero;
        beer.transform.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        beer.transform.position = baseBeerTransform.position;
        beer.transform.rotation = Quaternion.identity;

        GameObject cap = GameObject.Find("Caps");
        cap.transform.GetComponent<Rigidbody>().velocity = Vector3.zero;
        cap.transform.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        cap.transform.position = baseBeerTransform.position + baseBeerTransform.up * 0.1f;
        cap.transform.rotation = Quaternion.identity;
    }

    private void OnCollisionEnter(Collision collision)
    {  
        if (collision.transform.CompareTag("Beer") == true)
        {
            ResetBeerAndCaps();
            BasketScore.Instance.IncreaseScore();
        }
    }
}
