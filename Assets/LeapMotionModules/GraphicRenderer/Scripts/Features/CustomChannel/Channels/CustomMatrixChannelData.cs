using UnityEngine;
using System;

namespace Leap.Unity.GraphicalRenderer {

  [LeapGraphicTag("Matrix Channel")]
  [Serializable]
  public class CustomMatrixChannelData : CustomChannelDataBase<Matrix4x4> { }
}
