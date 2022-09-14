using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    public Rigidbody rbody;

    Vector3 prevPos;

    void FixedUpdate()
    {
        if (rbody.velocity.magnitude > 0f)
        {
            if (prevPos != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(rbody.position - prevPos);
                rbody.rotation = Quaternion.Slerp(rbody.rotation, targetRotation, Time.deltaTime * 10);
            }

            prevPos = rbody.position;
        }
    }
}