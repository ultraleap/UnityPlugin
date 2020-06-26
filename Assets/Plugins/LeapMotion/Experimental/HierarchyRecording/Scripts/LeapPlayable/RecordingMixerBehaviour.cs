/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
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
