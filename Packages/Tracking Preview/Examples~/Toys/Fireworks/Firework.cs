using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Firework : MonoBehaviour
{
    public ParticleSystem fireworkParticles;
    public Renderer rend;
    public Rigidbody rbody;

    private void Awake()
    {
        StartCoroutine(ExplodeAfterWait());
        rend.material.color = Color.HSVToRGB(Random.Range(0f, 1f), 0.4f, 0.8f);
    }

    IEnumerator ExplodeAfterWait()
    {
        yield return new WaitForSeconds(Random.Range(1f, 3f));
        Explode();
    }

    void Explode()
    {
        StopAllCoroutines();
        rend.enabled = false;

        var main = fireworkParticles.main;
        main.startColor = rend.material.color;
        main.startSpeed = new ParticleSystem.MinMaxCurve(rbody.velocity.magnitude * 5, rbody.velocity.magnitude * 5 * 1.2f);

        fireworkParticles.Play();
    }

    private void OnCollisionEnter(Collision collision)
    {
        Explode();
    }
}