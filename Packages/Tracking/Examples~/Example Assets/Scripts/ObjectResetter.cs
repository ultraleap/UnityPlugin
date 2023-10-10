using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectResetter : MonoBehaviour
{
    public float distanceToReset = 2;

    Vector3 originalPos;
    Quaternion originalRot;

    Rigidbody rb;

    void Start()
    {
        originalPos = transform.position;
        originalRot = transform.rotation;
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (transform.position.y < originalPos.y - distanceToReset)
        {
            if (rb != null)
            {
                rb.position = originalPos;
                rb.rotation = originalRot;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            else
            {
                transform.position = originalPos;
                transform.rotation = originalRot;
            }
        }
    }
}
