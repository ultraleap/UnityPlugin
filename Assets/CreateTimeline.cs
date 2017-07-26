using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEditor;

public class CreateTimeline : MonoBehaviour {

  private AnimationClip _clip;
  private List<PropertyRecorder> _recorders = new List<PropertyRecorder>();
  private Dictionary<EditorCurveBinding, AnimationCurve> _curves = new Dictionary<EditorCurveBinding, AnimationCurve>();

  private void LateUpdate() {
    GetComponentsInChildren(_recorders);

    foreach (var recorder in _recorders) {
      foreach (var bindings in recorder.GetBindings(gameObject)) {
        if (!_curves.ContainsKey(bindings)) {
          _curves[bindings] = new AnimationCurve();
        }
      }
    }

    foreach (var pair in _curves) {
      float value;
      bool didIt = AnimationUtility.GetFloatValue(gameObject, pair.Key, out value);
      if (didIt) {
        pair.Value.AddKey(Time.time, value);
      } else {
        Debug.Log(pair.Key.path + " : " + pair.Key.propertyName + " : " + pair.Key.type.Name);
      }
    }

    if (Input.GetKeyDown(KeyCode.Space)) {
      var timeline = ScriptableObject.CreateInstance<TimelineAsset>();

      var track = timeline.CreateTrack<AnimationTrack>(null, "my animation");

      var clip = new AnimationClip();
      int index = 0;

      try {
        foreach (var pair in _curves) {
          EditorUtility.DisplayProgressBar("Generating animation clips", "Compressing clip: " + pair.Key.path + pair.Key.propertyName, index / (float)_curves.Count);
          index++;

          AnimationUtility.SetEditorCurve(clip, pair.Key, AnimationCurveUtil.Compress(pair.Value));
        }
      } finally {
        EditorUtility.ClearProgressBar();
      }

      var timelineClip = track.CreateClip(clip);
      timelineClip.asset = clip;
      timelineClip.underlyingAsset = clip;

      AssetDatabase.CreateAsset(timeline, "Assets/timeline.asset");
      AssetDatabase.AddObjectToAsset(track, timeline);
      AssetDatabase.AddObjectToAsset(clip, timeline);
      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();
      enabled = false;
    }
  }
}
