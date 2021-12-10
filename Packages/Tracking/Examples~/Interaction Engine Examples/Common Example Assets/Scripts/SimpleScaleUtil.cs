/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace UHI.Tracking.InteractionEngine.Examples
{

    [AddComponentMenu("")]
    public class SimpleScaleUtil : MonoBehaviour
    {

        public void SetLocalScale(float scale)
        {
            this.transform.localScale = Vector3.one * scale;
        }

    }

}