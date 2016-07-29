using UnityEngine;
using UnityTest;

namespace Leap.Unity.Interaction.Testing {

  public class InteractionTestRunner : TestRunner {

    [SerializeField]
    protected GameObject _testPrefab;

    [SerializeField]
    protected float _timeScale = 10;

    private GameObject _spawned;

    protected override void StartNewTest() {
      _spawned = Instantiate(_testPrefab);

      Time.timeScale = _timeScale;

      base.StartNewTest();
    }

    protected override void FinishTest(TestResult.ResultType result) {
      base.FinishTest(result);

      DestroyImmediate(_spawned);
      _spawned = null;
    }

  }
}
