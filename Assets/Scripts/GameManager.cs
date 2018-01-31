using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
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

    public bool gameStarted = false;
    public GameObject sceneGround;
    public GameObject sceneObjs;

    public CameraDetection cameraDetection;
    public GameObject screen;
    public GameObject targetCharacter;

    void Awake()
    {
        cameraDetection.enabled = false;
        screen.SetActive(false);
        targetCharacter.SetActive(false);
        sceneGround.SetActive(false);
    }

    void Update()
    {
        if (Controller != null && Controller.GetPress(Valve.VR.EVRButtonId.k_EButton_ApplicationMenu) == true && !gameStarted)
        {
            cameraDetection.enabled = true;
            screen.SetActive(true);
            targetCharacter.SetActive(true);
            sceneGround.SetActive(false);
        }
        else
        {
            cameraDetection.enabled = false;
            screen.SetActive(false);
            targetCharacter.SetActive(false);
            if (cameraDetection.detectionFinished == true && !gameStarted)
                StartGame();
        }
    }

    private void StartGame()
    {
        GameObject objs = Instantiate(sceneObjs, targetCharacter.transform.position - (Vector3.up * 0.93f), sceneObjs.transform.rotation);
        sceneGround.SetActive(true);
        sceneGround.GetComponent<HoldMyBeer>().baseBeerTransform = objs.transform.GetChild(0);
        sceneGround.GetComponent<HoldMyBeer>().baseBallTransform = objs.transform.GetChild(1);

        var rigidbodies = GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
            rb.isKinematic = false;
        gameStarted = true;
    }
}
