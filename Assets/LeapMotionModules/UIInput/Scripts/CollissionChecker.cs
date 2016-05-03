using UnityEngine;
using System.Collections;

public class CollissionChecker : MonoBehaviour {
    public bool isColliding = false;
    int numberOfCollisions = 0;
    float lastcollision = 0f;

    void OnCollisionEnter(Collision collision)
    {
        numberOfCollisions++;

        if (numberOfCollisions > 0) {
            isColliding = true;
        }
        lastcollision = Time.fixedTime;
    }

    void OnCollisionStay(Collision collisionInfo)
    {
        foreach (ContactPoint contact in collisionInfo.contacts) {
            if (collisionInfo.impulse.magnitude <= 0f) {
               // Debug.DrawRay(contact.point, contact.normal * 0.05f, Color.white);
            } else {
                Debug.DrawRay(contact.point, contact.normal * 0.05f, Color.red);
            }
        }
        lastcollision = Time.fixedTime;
    }

    void OnCollisionExit(Collision collision)
    {
        numberOfCollisions--;

        if (numberOfCollisions == 0) {
            isColliding = false;
        }
    }

    void FixedUpdate()
    {
        if (Time.fixedTime - lastcollision > Time.fixedDeltaTime*2f) {
            isColliding = false;
            numberOfCollisions = 0;
        }
    }
}
