using UnityEngine;

namespace Leap.Unity.GraphicalRenderer {

  public abstract class CustomChannelDataBase : LeapFeatureData { }

  public abstract class CustomChannelDataBase<T> : CustomChannelDataBase {

    [SerializeField]
    private T _value;

    public T value {
      get {
        return _value;
      }
      set {
        MarkFeatureDirty();
        _value = value;
      }
    }
  }
}
