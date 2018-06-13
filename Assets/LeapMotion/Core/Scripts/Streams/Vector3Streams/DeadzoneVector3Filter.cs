using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Streams {

  [AddComponentMenu("Leap/Streams/Vector3/Deadzone Vector3 Filter")]
  public class DeadzoneVector3Filter : MonoBehaviour,
                                       IStream<Vector3>,
                                       IStreamReceiver<Vector3> {

    [Tooltip("Vector3s whose magnitudes are less than this radius will not be streamed.")]
    [MinValue(0f)]
    public float deadzoneRadius = 0.02f;
    public float deadzoneRadiusSqr { get { return deadzoneRadius * deadzoneRadius; } }

    public event Action OnOpen = () => { };
    public event Action<Vector3> OnSend = (position) => { };
    public event Action OnClose = () => { };

    public void Close() { OnClose(); }

    public void Open() { OnOpen(); }

    public void Receive(Vector3 position) {
      if (position.sqrMagnitude > deadzoneRadiusSqr) {
        OnSend(position);
      }
    }
  }

}
