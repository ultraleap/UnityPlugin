using UnityEngine;
using UnityTest;

namespace Leap.Unity.Interaction.Testing {

  public class PrefabTestRunner : TestRunner {

    [SerializeField]
    protected GameObject _testPrefab;

    private GameObject _spawned;

    protected override void StartNewTest() {
      _spawned = Instantiate(_testPrefab);

      base.StartNewTest();
    }

    protected override void FinishTest(TestResult.ResultType result) {
      base.FinishTest(result);

      DestroyImmediate(_spawned);
      _spawned = null;
    }

  }
}
