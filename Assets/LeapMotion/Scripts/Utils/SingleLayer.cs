using System;

namespace Leap.Unity {

  /// <summary>
  /// An object you can use to represent a single layer as a
  /// dropdown in the inspector.  Can be converted back and 
  /// forth between the integer representation Unity usually
  /// uses in it's own methods.
  /// </summary>
  [Serializable]
  public struct SingleLayer {
    public int layer;

    public static implicit operator int (SingleLayer singleLayer) {
      return singleLayer.layer;
    }

    public static implicit operator SingleLayer(int layerIndex) {
      SingleLayer singleLayer = new SingleLayer();
      singleLayer.layer = layerIndex;
      return singleLayer;
    }
  }
}
