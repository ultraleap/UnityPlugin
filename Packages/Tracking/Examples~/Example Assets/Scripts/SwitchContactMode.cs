using Leap.Unity.PhysicalHands;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchContactMode : MonoBehaviour
{
    [SerializeField]
    PhysicalHandsManager physicsHandsManager = null;
    [SerializeField]
    GameObject selectionItem;


    public PhysicalHandsManager.ContactMode contactMode = PhysicalHandsManager.ContactMode.HardContact;



    // Start is called before the first frame update
    void Start()
    {
        if (physicsHandsManager == null)
        {
            physicsHandsManager = FindFirstObjectByType(typeof(PhysicalHandsManager)) as PhysicalHandsManager;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject == selectionItem)
        {
            physicsHandsManager.SetContactMode(contactMode);

        }
    }
}
