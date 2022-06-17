/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
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
        public Vector3 scaleFrom = Vector3.zero;
        public Vector3 scaleTo = Vector3.one;

        [Space]
        public float scaleTimeSeconds = 0.3f;

        [Space]
        public float delay = 0;

        [Space]
        public AnimationCurve smoothCurve = AnimationCurve.Linear(0, 0, 1, 1);

        float curScaleTime;

        private void OnEnable()
        {
            curScaleTime = -delay;
        }

        private void OnDisable()
        {
            transform.localScale = scaleFrom;
        }

        private void Update()
        {
            if (curScaleTime < scaleTimeSeconds)
            {
                curScaleTime += Time.deltaTime;

                if (curScaleTime < scaleTimeSeconds)
                {
                    transform.localScale = Vector3.Lerp(scaleFrom, scaleTo, smoothCurve.Evaluate(curScaleTime / scaleTimeSeconds));
                }
                else
                {
                    transform.localScale = scaleTo;
                }
            }
        }
    }
}