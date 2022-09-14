using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Glider : MonoBehaviour
{
    public Rigidbody rbody;
    public Renderer rend;

    Vector3 prevPos;

    Vector3 personlalGravity;
    Vector3 personalTurn;

    bool canTurn = false;

    private void Awake()
    {
        personlalGravity = new Vector3(0, Random.Range(-0.05f, -0.01f), 0);
        personalTurn = new Vector3(Random.Range(-5f, 5f), 0, 0);

        StartCoroutine(EnableTurnAfterWait());

        rend.material.color = Color.HSVToRGB(Random.Range(0f,1f), 0.4f, 0.8f);
    }

    void FixedUpdate()
    {
        if (rbody.velocity.magnitude > 0f)
        {
            if (prevPos != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(rbody.position - prevPos);
                rbody.rotation = Quaternion.Slerp(rbody.rotation, targetRotation, Time.deltaTime * 10);
                rbody.AddForce(personlalGravity);

                if (canTurn)
                {
                    rbody.AddRelativeForce(personalTurn);
                }
            }

            prevPos = rbody.position;
        }
    }

    IEnumerator EnableTurnAfterWait()
    {
        yield return new WaitForSeconds(Random.Range(1f, 3f));

        canTurn = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        personlalGravity *= 5;
    }
}
