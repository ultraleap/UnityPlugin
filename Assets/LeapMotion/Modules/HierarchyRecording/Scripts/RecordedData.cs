using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Recording {

  [RecordingFriendly]
  public class RecordedData : MonoBehaviour {

    public List<EditorCurveBindingData> data = new List<EditorCurveBindingData>();

    [Serializable]
    public class EditorCurveBindingData {
      public string path;
      public string propertyName;
      public string typeName;
      public AnimationCurve curve;
    }
  }

}
