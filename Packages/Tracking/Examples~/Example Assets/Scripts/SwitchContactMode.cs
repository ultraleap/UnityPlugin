using Leap.Unity.PhysicsHands;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchContactMode : MonoBehaviour
{
    [SerializeField]
    PhysicsHandsManager physicsHandsManager = null;
    [SerializeField]
    GameObject selectionItem;


    public PhysicsHandsManager.ContactModes contactMode = PhysicsHandsManager.ContactModes.HardContact;



    // Start is called before the first frame update
    void Start()
    {
        if (physicsHandsManager == null)
        {
            physicsHandsManager = FindFirstObjectByType(typeof(PhysicsHandsManager)) as PhysicsHandsManager;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject == selectionItem)
        {
            physicsHandsManager.ContactMode = contactMode;

        }
    }
}
