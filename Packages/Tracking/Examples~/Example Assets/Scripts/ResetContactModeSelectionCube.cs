using Leap.Unity.PhysicsHands;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetContactModeSelectionCube : MonoBehaviour
{
    [SerializeField]
    GameObject NoContactTrigger;
    [SerializeField]
    GameObject SoftContactTrigger;
    [SerializeField]
    GameObject HardContactTrigger;
    [SerializeField]
    PhysicsHandsManager physicsHandsManager;

    Vector3 offset = new Vector3(0, 0.12f, 0);


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(this.transform.position.y < 0.1)
        {
            this.transform.rotation = Quaternion.identity;
            this.GetComponent<Rigidbody>().velocity = Vector3.zero;
            switch (physicsHandsManager.ContactMode)
            {
                case PhysicsHandsManager.ContactModes.NoContact:
                    {
                        this.transform.position = NoContactTrigger.transform.position;
                        break;
                    }
                    case PhysicsHandsManager.ContactModes.SoftContact:
                    {
                        this.transform.position = SoftContactTrigger.transform.position;
                        break;
                    }
                    case PhysicsHandsManager.ContactModes.HardContact:
                    {
                        this.transform.position = HardContactTrigger.transform.position;
                        break;
                    }
            }

            this.transform.Translate(offset);


        }
    }
}
