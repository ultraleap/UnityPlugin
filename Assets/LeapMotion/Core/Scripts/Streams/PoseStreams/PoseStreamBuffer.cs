using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Streams {

  public class PoseStreamBuffer : MonoBehaviour, IStreamReceiver<Pose>,
                                                 IIndexable<Pose> {

    [SerializeField]
    private List<Pose> _poseBuffer;
    public List<Pose> poseBuffer {
      get {
        if (_poseBuffer == null) {
          _poseBuffer = new List<Pose>();
        }
        return _poseBuffer;
      }
    }

    public Pose this[int index] {
      get { return poseBuffer[index]; }
    }

    public int Count { get { return poseBuffer.Count; } }

    public void Open() {
      if (poseBuffer.Count > 0) poseBuffer.Clear();
    }

    public void Receive(Pose pose) {
      poseBuffer.Add(pose);
    }

    public void Close() { }

  }

}
