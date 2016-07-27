using UnityEngine;
using Leap.Unity.Playback;

namespace Leap.Unity.Interaction.Testing {

  public class InteractionTestProvider : PlaybackProvider {

    [Header("Interaction Test Settings")]
    [SerializeField]
    protected Transform _testRoot;

    public override Recording recording {
      set {
        base.recording = value;
      }
    }

    protected override void Start() {
      base.Start();
    }

    public void SpawnShapes() {
      if (base.recording is InteractionTestRecording) {
        var interactionRecording = base.recording as InteractionTestRecording;
        interactionRecording.CreateInitialShapes(_testRoot);
      }
    }

    public void DestroyShapes() {
      var objs = _testRoot.GetComponentsInChildren<Transform>(true);
      foreach (var obj in objs) {
        if (obj == _testRoot) continue;
        DestroyImmediate(obj.gameObject);
      }
    }
  }
}
