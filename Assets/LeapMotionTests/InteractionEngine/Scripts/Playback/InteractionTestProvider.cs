/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

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
