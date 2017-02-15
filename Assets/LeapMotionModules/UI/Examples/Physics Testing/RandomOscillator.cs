using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;

public class RandomOscillator : MonoBehaviour {

  private float _freq;
  private float _amp;
  private Vector3 _direction;

  void Start() {
    _freq = Random.value.Map(0F, 1F, 1F, 0.1F);
    _amp = Random.value.Map(0F, 1F, 0.001F, 0.01F);
    _direction = Random.onUnitSphere;
  }

  void Update() {
    this.transform.position += _direction * Mathf.Sin(Time.time * 2F * Mathf.PI * _freq) * _amp;
  }

}
