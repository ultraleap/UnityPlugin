using UnityEngine;
using UnityTest;

namespace Leap.Unity.Interaction.Testing {

  public class InteractionTestRunner : TestRunner {

    [SerializeField]
    protected GameObject _testPrefab;

    [SerializeField]
    protected float _timeScale = 10;

    private GameObject _spawned;

    public void SpawnObjects(float scale) {
      transform.localScale = Vector3.one * scale;

      _spawned = Instantiate(_testPrefab, transform) as GameObject;
      _spawned.transform.localPosition = Vector3.zero;
      _spawned.transform.localRotation = Quaternion.identity;
      _spawned.transform.localScale = Vector3.one;

      Time.timeScale = _timeScale;
    }

    protected override void FinishTest(TestResult.ResultType result) {
      base.FinishTest(result);

      DestroyImmediate(_spawned);
      _spawned = null;
    }
  }
}
