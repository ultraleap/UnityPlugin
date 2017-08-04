using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Leap.Unity.Recording {

  public class RecordingMixerBehaviour : PlayableBehaviour {

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

        // Use the above variables to process each frame of this playable.

      }
    }
  }
}
