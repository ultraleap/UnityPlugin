using Leap.Unity.Attributes;
using Leap.Unity.Query;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Streams {
  
  /// <summary>
  /// Fills received poses into a buffer, then fills a LineRenderer object with
  /// the received Pose data on Close().
  /// </summary>
  public class LineRendererPoseReceiver : MonoBehaviour,
                                          IStreamReceiver<Pose> {
    
    public LineRenderer lineRenderer;

    public const int POINTS_BUFFER_SIZE = 1024;
    private Vector3[] _pointsBuffer = new Vector3[POINTS_BUFFER_SIZE];
    private int _pointIdx = 0;

    private void Reset() {
      if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
    }
    private void OnValidate() {
      if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
    }

    public void Open() {
      _pointIdx = 0;
    }

    public void Receive(Pose data) {
      _pointsBuffer[_pointIdx++] = data.position;
    }

    public void Close() {
      lineRenderer.positionCount = _pointIdx;
      lineRenderer.SetPositions(_pointsBuffer);
    }

  }

}
