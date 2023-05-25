/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity
{
    /// <summary>
    /// Time-step independent exponential smoothing.
    /// </summary>
    /// <remarks>
    /// When moving at a constant speed: speed * delay = Value - SmoothedFloat.value.
    /// </remarks>
    [System.Serializable]
    public class SmoothedFloat
    {
        public float value = 0f; // Filtered value
        public float delay = 0f; // Mean delay
        public bool reset = true; // Reset on Next Update

        public SmoothedFloat(float blend = 0f, float deltaTime = 1f) { SetBlend(blend); }

        public void SetBlend(float blend, float deltaTime = 1f)
        {
            delay = deltaTime * blend / (1f - blend);
        }

        public float Update(float input, float deltaTime = 1f)
        {
            if (deltaTime > 0f && !reset)
            {
                float alpha = delay / deltaTime;
                float blend = alpha / (1f + alpha);
                // NOTE: If delay -> 0 then blend -> 0,
                // reducing the filter to this.value = value.
                // NOTE: If deltaTime -> 0 blend -> 1,
                // so the change in the filtered value will be suppressed
                value = Mathf.Lerp(value, input, 1f - blend);
            }
            else
            {
                value = input;
                reset = false;
            }
            return value;
        }
    }
}