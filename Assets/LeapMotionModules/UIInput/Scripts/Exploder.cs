using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Exploder : MonoBehaviour {
    ParticleSystem particles;
	// Use this for initialization
	void Start () {
        particles = GetComponent<ParticleSystem>();
        particles.Stop();
	}

	public void Explode (Vector3 button) {
        transform.position = button;
        transform.rotation = Quaternion.LookRotation(Camera.main.transform.position - transform.position);
        transform.position += transform.forward *0.03f;
        particles.Play();
    }
}
