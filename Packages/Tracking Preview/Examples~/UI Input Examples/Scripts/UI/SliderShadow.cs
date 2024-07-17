/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.InputModule
{
    public class SliderShadow : MonoBehaviour
    {
        public Transform Slider;
        public Transform Handle;

        void Update()
        {
            Vector3 LocalHandle = Slider.InverseTransformPoint(Handle.position);
            Vector3 LocalShadow = Slider.InverseTransformPoint(transform.position);
            Vector3 NewLocalShadow = new Vector3(LocalHandle.x, LocalHandle.y, LocalShadow.z);
            transform.position = Slider.TransformPoint(NewLocalShadow);
        }
    }
}