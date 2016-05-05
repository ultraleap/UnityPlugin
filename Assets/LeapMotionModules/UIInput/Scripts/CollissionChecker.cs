using UnityEngine;
using System.Collections;

public class CollissionChecker : MonoBehaviour {
    public bool isColliding = false;
    public float collisionMass;
    int numberOfCollisions = 0;
    float lastcollision = 0f;

    void Start()
    {
        collisionMass = GetComponent<Rigidbody>().mass;
    }

    void OnCollisionStay(Collision collisionInfo)
    {
        foreach (ContactPoint contact in collisionInfo.contacts) {
            if (collisionInfo.impulse.magnitude > 0f) {
                if (collisionInfo.collider.attachedRigidbody != null) {
                    collisionMass = collisionInfo.collider.attachedRigidbody.mass;
                }
                lastcollision = Time.fixedTime;
                isColliding = true;
                Debug.DrawRay(contact.point, contact.normal * collisionInfo.impulse.magnitude/100f, Color.white);
            }
        }
    }

    void FixedUpdate()
    {
        if (Time.fixedTime - lastcollision > Time.fixedDeltaTime*3f) {
            isColliding = false;
            numberOfCollisions = 0;
        }
    }
}
