using UnityEngine;

namespace Leap.Unity.Splines {

  public interface ISpline<XType, dXType> {

    float minT { get; }
    float maxT { get; }

    XType ValueAt(float t);

    dXType DerivativeAt(float t);

    void ValueAndDerivativeAt(float t, out XType value, out dXType deltaValuePerT);

  }

}