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

  public class InteractionTestRecorder : PlaybackRecorder {

    public override void StartRecording() {
      var testRecording = ScriptableObject.CreateInstance<InteractionTestRecording>();
      testRecording.CaptureCurrentShapes();
      _currentRecording = testRecording;
    }

    public override Recording EndRecording() {
      var testRecording = _currentRecording as InteractionTestRecording;
      testRecording.TrimStartOfEmptyFrames(2);
      testRecording.TrimEndOfEmptyFrames(2);

      return base.EndRecording();
    }
  }
}
