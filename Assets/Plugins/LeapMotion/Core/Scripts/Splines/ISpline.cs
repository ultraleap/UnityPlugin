/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 * Ultraleap proprietary and confidential.                                    *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Animation {

  public interface ISpline<XType, dXType> {

    float minT { get; }
    float maxT { get; }

    XType ValueAt(float t);

    dXType DerivativeAt(float t);

    void ValueAndDerivativeAt(float t, out XType value, out dXType deltaValuePerT);

  }

}
