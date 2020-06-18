/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Attributes;

namespace Leap.Unity.Recording {

  public class PropertyCompression : MonoBehaviour {

    public NamedCompression[] compressionOverrides;

    [Serializable]
    public class NamedCompression {
      public string propertyName;

      [MinValue(0)]
      public float maxError;
    }
  }

}
