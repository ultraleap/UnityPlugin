/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
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
