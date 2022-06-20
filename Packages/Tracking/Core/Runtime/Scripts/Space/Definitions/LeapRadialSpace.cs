/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Attributes;
using UnityEngine;

namespace Leap.Unity.Space
{
    [System.Obsolete("This code will be moved to a legacy package in the next major version of the plugin. If you believe that it needs to be kept in tracking, please open a discussion on the GitHub forum (https://github.com/ultraleap/UnityPlugin/discussions)")]
    public interface IRadialTransformer : ITransformer
    {
        Vector4 GetVectorRepresentation(Transform element);
    }

    [System.Obsolete("This code will be moved to a legacy package in the next major version of the plugin. If you believe that it needs to be kept in tracking, please open a discussion on the GitHub forum (https://github.com/ultraleap/UnityPlugin/discussions)")]
    public abstract class LeapRadialSpace : LeapSpace
    {

        [MinValue(0.001f)]
        [SerializeField]
        private float _radius = 1;

        public float radius
        {
            get
            {
                return _radius;
            }
            set
            {
                _radius = value;
            }
        }

        [System.Obsolete("This code will be moved to a legacy package in the next major version of the plugin. If you believe that it needs to be kept in tracking, please open a discussion on the GitHub forum (https://github.com/ultraleap/UnityPlugin/discussions)")]
        public override Hash GetSettingHash()
        {
            return new Hash() {
        _radius
      };
        }

        protected sealed override void UpdateTransformer(ITransformer transformer, ITransformer parent)
        {
            Vector3 anchorRectPos = transform.InverseTransformPoint(transformer.anchor.transform.position);
            Vector3 parentRectPos = transform.InverseTransformPoint(parent.anchor.transform.position);
            Vector3 delta = anchorRectPos - parentRectPos;
            UpdateRadialTransformer(transformer, parent, delta);
        }

        protected abstract void UpdateRadialTransformer(ITransformer transformer, ITransformer parent, Vector3 rectSpaceDelta
          );
    }
}