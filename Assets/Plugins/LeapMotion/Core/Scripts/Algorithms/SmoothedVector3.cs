/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity {
  /// <summary>
  /// Time-step independent exponential smoothing.
  /// </summary>
  /// <remarks>
  /// When moving at a constant speed: speed * delay = Value - SmoothedVector3.value.
  /// </remarks>
  [System.Serializable]
  public class SmoothedVector3 {
    public Vector3 value = Vector3.zero; // Filtered value
    public float delay = 0f; // Mean delay
    public bool reset = true; // Reset on Next Update

    public void SetBlend(float blend, float deltaTime = 1f) {
      delay = deltaTime * blend / (1f - blend);
    }

    public Vector3 Update(Vector3 input, float deltaTime = 1f) {
      if (deltaTime > 0f && !reset) {
        float alpha = delay / deltaTime;
        float blend = alpha / (1f + alpha);
        // NOTE: If delay -> 0 then blend -> 0,
        // reducing the filter to this.value = value.
        // NOTE: If deltaTime -> 0 blend -> 1,
        // so the change in the filtered value will be suppressed
        value = Vector3.Lerp(this.value, input, 1f - blend);
      } else {
        value = input;
        reset = false;
      }
      return value;
    }
  }
}
