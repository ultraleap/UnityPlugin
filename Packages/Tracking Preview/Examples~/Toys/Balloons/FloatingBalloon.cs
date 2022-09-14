using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingBalloon : MonoBehaviour
{
    public Rigidbody rbody;
    public Renderer rend;

    bool floating = true;

    Vector3 forceChange;

    private IEnumerator Start()
    {
        rend.material.color = Color.HSVToRGB(Random.Range(0f, 1f), 0.4f, 0.8f);

        float timeChange = Random.Range(0.4f, 0.6f);
        int directionChangeCount = Random.Range(3, 5);

        WaitForSeconds wait = new WaitForSeconds(timeChange);

        while (directionChangeCount > 0)
        {
            yield return wait;

            forceChange = new Vector3(Random.Range(-0.5f, 0.5f), 0.1f, Random.Range(-0.5f, 0.5f));

            directionChangeCount--;
        }

        floating = false;
    }

    private void FixedUpdate()
    {
        if (floating)
        {
            rbody.AddForce(forceChange);
        }
        else
        {
            if(rbody.velocity.magnitude > 0)
            {
                rbody.velocity *= 0.99f;

                if (rbody.velocity.magnitude <= 0.01f)
                {
                    rbody.velocity = Vector3.zero;
                    rbody.isKinematic = true;
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        var arrow = collision.collider.GetComponent<Arrow>();

        if (arrow != null)
        {
            Destroy(gameObject);
        }
    }
}