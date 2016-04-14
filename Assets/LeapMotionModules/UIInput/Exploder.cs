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

	public void Explode (Transform button) {
        transform.position = button.position + new Vector3(0f,0f,-0.1f);
        particles.Play();
    }
}
