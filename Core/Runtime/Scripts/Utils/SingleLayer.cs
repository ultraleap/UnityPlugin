/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;

namespace Leap.Unity {

  /// <summary>
  /// An object you can use to represent a single Unity layer 
  /// as a dropdown in the inspector.  Can be converted back and 
  /// forth between the integer representation Unity usually
  /// uses in its own methods.
  /// </summary>
  [Serializable]
  public struct SingleLayer : IEquatable<SingleLayer> {
    public int layerIndex;

    public int layerMask {
      get {
        return 1 << layerIndex;
      }
      set {
        if (value == 0) {
          throw new ArgumentException("Single layer can only represent exactly one layer.  The provided mask represents no layers (mask was zero).");
        }

        int newIndex = 0;
        while ((value & 1) == 0) {
          value = value >> 1;
          newIndex++;
        }

        if (value != 1) {
          throw new ArgumentException("Single layer can only represent exactly one layer.  The provided mask represents more than one layer.");
        }

        layerIndex = newIndex;
      }
    }

    public static implicit operator int(SingleLayer singleLayer) {
      return singleLayer.layerIndex;
    }

    public static implicit operator SingleLayer(int layerIndex) {
      SingleLayer singleLayer = new SingleLayer();
      singleLayer.layerIndex = layerIndex;
      return singleLayer;
    }

    public bool Equals(SingleLayer other) {
      return this.layerIndex == other.layerIndex;
    }
  }
}
