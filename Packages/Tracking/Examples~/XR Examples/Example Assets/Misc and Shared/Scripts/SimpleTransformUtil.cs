/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Examples
{
    public class SimpleTransformUtil : MonoBehaviour
    {
        public void SetParentTo(Transform t)
        {
            this.transform.SetParent(t, true);
        }

        public void ClearParentTransform()
        {
            this.transform.SetParent(null, true);
        }
    }
}