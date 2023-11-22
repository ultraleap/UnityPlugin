using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ObjectResetter : MonoBehaviour
{
    public float distanceToReset = 2;

    Vector3 originalPos;
    Quaternion originalRot;

    Rigidbody[] rbs;
    Vector3[] rbPositions;
    Quaternion[] rbRotations;

    [Space]
    public UnityEvent OnReset;

    void Start()
    {
        originalPos = transform.position;
        originalRot = transform.rotation;
        rbs = GetComponentsInChildren<Rigidbody>();

        rbPositions = new Vector3[rbs.Length];
        rbRotations = new Quaternion[rbs.Length];

        for (int i = 0; i < rbs.Length; i++)
        {
            rbPositions[i] = rbs[i].position;
            rbRotations[i] = rbs[i].rotation;
        }
    }

    void FixedUpdate()
    {
        if (transform.position.y < originalPos.y - distanceToReset)
        {
            if (rbs != null)
            {
                for (int i = 0; i < rbs.Length; i++)
                {
                    rbs[i].position = rbPositions[i];
                    rbs[i].rotation = rbRotations[i];
                    rbs[i].velocity = Vector3.zero;
                    rbs[i].angularVelocity = Vector3.zero;
                }
            }
            else
            {
                transform.position = originalPos;
                transform.rotation = originalRot;
            }

            OnReset?.Invoke();
        }
    }
}
