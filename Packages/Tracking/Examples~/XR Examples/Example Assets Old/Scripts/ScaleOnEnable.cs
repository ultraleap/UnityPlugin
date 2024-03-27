/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Examples
{
    public class ScaleOnEnable : MonoBehaviour
    {
        [Tooltip("The local scale values to scale from")]
        public Vector3 scaleFrom = Vector3.zero;

        [Tooltip("The local scale values to scale to")]
        public Vector3 scaleTo = Vector3.one;

        [Space]
        public float scaleTimeSeconds = 0.3f;

        [Space, Tooltip("Adds a delay to the beginning of the scale")]
        public float delay = 0;

        [Space, Tooltip("Change this curve to adjust the rate of scaling over time. Ensure time ranges from 0 to 1")]
        public AnimationCurve smoothCurve = AnimationCurve.Linear(0, 0, 1, 1);

        float curScaleTime;

        private void OnEnable()
        {
            // By setting the scale as -delay, we force the lerp in Update to clamp the scale to scaleFrom until it reaches 0
            curScaleTime = -delay;
        }

        private void OnDisable()
        {
            // This ensures the scale is reset in preparation for the next OnEnable
            transform.localScale = scaleFrom;
        }

        private void Update()
        {
            // Scaling only occurs while curScaleTime is actively below the maximum time
            if (curScaleTime < scaleTimeSeconds)
            {
                curScaleTime += Time.deltaTime;

                if (curScaleTime < scaleTimeSeconds)
                {
                    // Scaling is progressing, scale based on smoothCurve
                    transform.localScale = Vector3.Lerp(scaleFrom, scaleTo, smoothCurve.Evaluate(curScaleTime / scaleTimeSeconds));
                }
                else
                {
                    // The scaling is complete
                    transform.localScale = scaleTo;
                }
            }
        }
    }
}