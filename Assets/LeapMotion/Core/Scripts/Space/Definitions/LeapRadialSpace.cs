/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using Leap.Unity.Attributes;

namespace Leap.Unity.Space {

  public interface IRadialTransformer : ITransformer {
    Vector4 GetVectorRepresentation(Transform element);
  }

  public abstract class LeapRadialSpace : LeapSpace {

    [MinValue(0.001f)]
    [SerializeField]
    private float _radius = 1;

    public float radius {
      get {
        return _radius;
      }
      set {
        _radius = value;
      }
    }

    public override Hash GetSettingHash() {
      return new Hash() {
        _radius
      };
    }

    protected sealed override void UpdateTransformer(ITransformer transformer, ITransformer parent) {
      Vector3 anchorRectPos = transform.InverseTransformPoint(transformer.anchor.transform.position);
      Vector3 parentRectPos = transform.InverseTransformPoint(parent.anchor.transform.position);
      Vector3 delta = anchorRectPos - parentRectPos;
      UpdateRadialTransformer(transformer, parent, delta);
    }

    protected abstract void UpdateRadialTransformer(ITransformer transformer, ITransformer parent, Vector3 rectSpaceDelta
      );
  }
}
