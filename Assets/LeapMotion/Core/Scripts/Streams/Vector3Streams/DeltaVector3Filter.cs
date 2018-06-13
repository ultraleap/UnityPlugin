using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Streams {

  [AddComponentMenu("Leap/Streams/Vector3/Delta Vector3 Filter")]
  public class DeltaVector3Filter : MonoBehaviour,
                                 IStream<Vector3>,
                                 IStreamReceiver<Vector3> {

    [Tooltip("Input positions will be subtracted by the world position of this transform.")]
    public Transform referencePositionSource = null;

    public Vector3 referencePosition { get { return referencePositionSource.transform.position; } }

    public event Action OnOpen = () => { };
    public event Action<Vector3> OnSend = (position) => { };
    public event Action OnClose = () => { };

    public void Close() { OnClose(); }

    public void Open() { OnOpen(); }

    public void Receive(Vector3 data) {
      var deltaPosition = -referencePosition + data;
      OnSend(deltaPosition);
    }
  }

}
