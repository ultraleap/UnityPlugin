using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cannon : MonoBehaviour
{
    List<Rigidbody> innerObjects = new List<Rigidbody> ();

    public float launchTimer = 2;

    public float explosionForce = 100;

    float timer = 0;

    private void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            timer = launchTimer;
            Launch();
        }
    }

    public void Launch()
    {
        foreach (var obj in innerObjects)
        {
            obj.AddForce(transform.up * explosionForce);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var rbod = other.attachedRigidbody;

        if(rbod != null && !innerObjects.Contains(rbod))
        {
            innerObjects.Add(rbod);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var rbod = other.attachedRigidbody;

        if (rbod != null && innerObjects.Contains(rbod))
        {
            innerObjects.Remove(rbod);
        }
    }
}
