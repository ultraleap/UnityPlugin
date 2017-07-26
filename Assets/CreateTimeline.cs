using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEditor;

public class CreateTimeline : MonoBehaviour {

  public AnimationCurve curve;
  public GameObject obj;

  private AnimationClip _clip;
  private List<PropertyRecorder> _recorders = new List<PropertyRecorder>();
  private Dictionary<EditorCurveBinding, AnimationCurve> _curves = new Dictionary<EditorCurveBinding, AnimationCurve>();

  private void LateUpdate() {
    GetComponentsInChildren(_recorders);

    foreach (var recorder in _recorders) {
      var transformPath = AnimationUtility.CalculateTransformPath(recorder.transform, transform);
      /*
      foreach (var properties in recorder.serializedComponents) {
        foreach (var bindingName in properties.bindings) {
          EditorCurveBinding binding = EditorCurveBinding.FloatCurve(transformPath, properties.component.GetType(), bindingName);
          if (!_curves.ContainsKey(binding)) {
            _curves[binding] = new AnimationCurve();
          }
        }
      }
      */
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

  [ContextMenu("constrain")]
  void constrin() {
    AnimationUtility.ConstrainToPolynomialCurve(curve);
  }

  [ContextMenu("bindings")]
  void getbindings() {
    var bindings = AnimationUtility.GetAnimatableBindings(obj, gameObject);
    foreach (var binding in bindings) {
      Debug.Log(binding.path + " : " + binding.propertyName);
    }
  }


  [ContextMenu("try do it")]
  void tryCreateTimeline() {
    var timeline = ScriptableObject.CreateInstance<TimelineAsset>();

    var track = timeline.CreateTrack<AnimationTrack>(null, "my animation");

    var clip = new AnimationClip();

    var timelineClip = track.CreateClip(clip);

    AssetDatabase.CreateAsset(timeline, "Assets/timeline.asset");
    AssetDatabase.AddObjectToAsset(track, timeline);
    AssetDatabase.AddObjectToAsset(clip, timeline);
  }



}
