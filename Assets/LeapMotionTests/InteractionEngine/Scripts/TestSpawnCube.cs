using System;
using UnityEngine;
using UnityTest;
using Leap.Unity.Interaction;

namespace Leap.Unity.InteractionTest {

  public class TestSpawnCube : UnityTest.TestComponent {
    public InteractionBehaviour cube;
    public float delay = 2.0f;

    private float startTime = 0.0f;

    public void Start() {
      InteractionBehaviour instance = (InteractionBehaviour)Instantiate(cube, new Vector3(0, 0, 0), Quaternion.identity);
      instance.transform.parent = transform;
      startTime = Time.time;
    }

    public void Update() {
      if ((Time.time - startTime) > delay) {
        IntegrationTest.Pass(gameObject);
      }
    }
  };

}; // namespace Leap.Unity.Interaction

