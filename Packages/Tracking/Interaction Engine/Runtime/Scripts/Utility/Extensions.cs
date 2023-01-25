/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Interaction.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction
{

    #region IInteractionBehaviour

    public static class IInteractionBehaviourExtensions
    {

        public static bool ShouldIgnoreHover(this IInteractionBehaviour intObj, InteractionController controller)
        {
            switch (intObj.ignoreHoverMode)
            {
                case IgnoreHoverMode.None: return false;
                case IgnoreHoverMode.Left: return !controller.isTracked || controller.isLeft;
                case IgnoreHoverMode.Right: return !controller.isTracked || controller.isRight;
                case IgnoreHoverMode.Both: default: return true;
            }
        }

        public static bool ShouldIgnoreGrasping(this IInteractionBehaviour intObj, InteractionController controller)
        {
            switch (intObj.ignoreGraspingMode)
            {
                case IgnoreHoverMode.None: return false;
                case IgnoreHoverMode.Left: return !controller.isTracked || controller.isLeft;
                case IgnoreHoverMode.Right: return !controller.isTracked || controller.isRight;
                case IgnoreHoverMode.Both: default: return true;
            }
        }

    }

    #endregion

    #region Vector3

    public static class Vector3Extensions
    {

        public static Vector3 ConstrainToSegment(this Vector3 position, Vector3 a, Vector3 b)
        {
            Vector3 ba = b - a;
            return Vector3.Lerp(a, b, Vector3.Dot(position - a, ba) / ba.sqrMagnitude);
        }

        public static float LargestComp(this Vector3 v)
        {
            return Mathf.Max(Mathf.Max(v.x, v.y), v.z);
        }

        public static int LargestCompIdx(this Vector3 v)
        {
            return v.x > v.y ?
                     v.x > v.z ?
                       0 // x > y, x > z
                     : 2 // x > y, z > x
                   : v.y > v.z ?
                     1   // y > x, y > z
                   : 2;  // y > x, z > y
        }

        public static Vector3 Abs(this Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

    }

    #endregion

}