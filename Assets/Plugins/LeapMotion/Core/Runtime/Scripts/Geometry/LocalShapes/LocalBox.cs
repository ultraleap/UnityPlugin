/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Geometry {

  [System.Serializable]
  public struct LocalBox {

    public Vector3 center;
    public Vector3 radii;

    public Box With(Transform t) {
      return new Box(this, t);
    }

    public static LocalBox unit { get { return new LocalBox() {
      center = Vector3.zero, radii = Vector3.one
    };}}

  }

}
