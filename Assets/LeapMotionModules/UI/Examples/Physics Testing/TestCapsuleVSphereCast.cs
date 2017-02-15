using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Timing = System.Diagnostics;

public class TestCapsuleVSphereCast : MonoBehaviour {

  private const int DIMENSION_LENGTH = 7;
  private const float SEPARATION_M = 0.04F;

  void Start() {
    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    cube.transform.localScale = Vector3.one * 0.04F;
    cube.AddComponent<Rigidbody>();
    cube.GetComponent<Rigidbody>().useGravity = false;
    cube.GetComponent<Collider>().isTrigger = true;

    for (int i = 0; i < DIMENSION_LENGTH; i++) {
      for (int j = 0; j < DIMENSION_LENGTH; j++) {
        for (int k = 0; k < DIMENSION_LENGTH; k++) {
          if (i + j + k == 0) continue;
          GameObject newCube = GameObject.Instantiate(cube);
          newCube.transform.position = cube.transform.position + (Vector3.right * i * SEPARATION_M)
                                                               + (Vector3.up      * j * SEPARATION_M)
                                                               + (Vector3.forward * k * SEPARATION_M);
          newCube.AddComponent<RandomOscillator>();
        }
      }
    }

    RunTest();
  }

  int _count = 0;
  int _countsToRunTest = 100;

  void Update() {
    if (_count == 0) {
      RunTest();
    }
    _count = (_count + 1) % _countsToRunTest;
  }

  private long _capsuleElapsedTicks;
  private long _sphereCastElapsedTicks;
  private const int NUM_TEST_ITERATIONS = 1000;

  private Collider[] _colliderResults = new Collider[128];
  private int _colliderResultCount;
  private RaycastHit[] _raycastResults = new RaycastHit[128];
  private int _raycastResultCount;
  private Timing.Stopwatch _stopwatch = new Timing.Stopwatch();

  private void RunTest() {
    RunCapsuleCheckTest();
    RunSphereCastCheckTest();

    Debug.Log("Capsule: " + _capsuleElapsedTicks + " -- hits: " + _colliderResultCount);
    Debug.Log("SphereCast: " + _sphereCastElapsedTicks + " -- hits: " + _raycastResultCount);
  }

  private Vector3 _startPos = Vector3.zero;
  private Vector3 _endPos = Vector3.one * 0.4F;
  private float _radius = 0.2F;

  private void RunCapsuleCheckTest() {
    _stopwatch.Start();

    for (int i = 0; i < NUM_TEST_ITERATIONS; i++) {
      _colliderResultCount = Physics.OverlapCapsuleNonAlloc(_startPos, _endPos, _radius, _colliderResults); 
	  }

    _stopwatch.Stop();
    _capsuleElapsedTicks = _stopwatch.ElapsedMilliseconds;
    _stopwatch.Reset();
  }

  private void RunSphereCastCheckTest() {
    _stopwatch.Start();

    Vector3 direction = (_endPos - _startPos);
    float length = direction.magnitude;
    direction = direction.normalized;

    for (int i = 0; i < NUM_TEST_ITERATIONS; i++) {
      _raycastResultCount = Physics.SphereCastNonAlloc(_startPos, _radius, direction, _raycastResults, length);
    }

    _stopwatch.Stop();
    _sphereCastElapsedTicks = _stopwatch.ElapsedMilliseconds;
    _stopwatch.Reset();
  } 

}
