/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine.Playables;

namespace Leap.Unity.Recording {

  public class RecordingMixerBehaviour : PlayableBehaviour {

    private Frame _frame;

    public override void OnGraphStart(Playable playable) {
      base.OnGraphStart(playable);

      _frame = new Frame();
    }

    // NOTE: This function is called at runtime and edit time.  Keep that in mind when setting the values of properties.
    public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
      LeapPlayableProvider provider = playerData as LeapPlayableProvider;

      if (!provider)
        return;

      int inputCount = playable.GetInputCount();

      for (int i = 0; i < inputCount; i++) {
        float inputWeight = playable.GetInputWeight(i);
        var inputPlayable = (ScriptPlayable<RecordingBehaviour>)playable.GetInput(i);
        var input = inputPlayable.GetBehaviour();

        if (inputWeight > 0 && input.recording != null) {
          if (input.recording.Sample((float)inputPlayable.GetTime(), _frame, clampTimeToValid: true)) {
            provider.SetCurrentFrame(_frame);
            break;
          }
        }
      }
    }
  }
}
