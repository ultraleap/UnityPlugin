using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Catapult : MonoBehaviour
{
    public Rigidbody rbod;
    public float maxSpawnVelocity = 10;

    public Transform projectilePrefab;
    public Transform currentProjectile;

    public LineRenderer rightRope;
    public LineRenderer leftRope;

    bool spawning = false;

    private void Update()
    {
        if (!spawning)
        {
            if (currentProjectile == null || (Vector3.Distance(transform.position, currentProjectile.position) > 0.5f && rbod.velocity.magnitude < maxSpawnVelocity))
            {
                StartCoroutine(Spawn());
            }
        }

        rightRope.SetPosition(1, transform.position - (Vector3.right * 0.04f));
        leftRope.SetPosition(1, transform.position + (Vector3.right * 0.04f));
    }

    IEnumerator Spawn()
    {
        spawning = true;
        yield return new WaitForSeconds(0.5f);
        currentProjectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        spawning = false;
    }
}
