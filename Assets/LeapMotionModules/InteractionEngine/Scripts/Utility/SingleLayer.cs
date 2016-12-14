using System;

namespace Leap.Unity.Interaction {

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
