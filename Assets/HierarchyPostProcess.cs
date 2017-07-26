using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEditor;
using Leap.Unity;
using Leap.Unity.Query;
using Leap.Unity.Attributes;

public class HierarchyPostProcess : MonoBehaviour {

  [MinValue(0)]
  public float positionMaxError = 0.005f;

  [Range(0, 90)]
  public float rotationMaxError = 1;

  [Range(0, 1)]
  public float colorHueMaxError = 0.05f;

  [Range(0, 1)]
  public float colorSaturationMaxError = 0.05f;

  [Range(0, 1)]
  public float colorValueMaxError = 0.05f;

  [Range(0, 1)]
  public float colorAlphaMaxError = 0.05f;

  [MinValue(0)]
  public float genericMaxError = 0.05f;

  public void BuildPlaybackPrefab() {
    var timeline = ScriptableObject.CreateInstance<TimelineAsset>();

    var track = timeline.CreateTrack<AnimationTrack>(null, "Playback Animation");

    var bindingMap = new Dictionary<EditorCurveBinding, AnimationCurve>();

    var clip = new AnimationClip();
    try {
      var recordings = GetComponentsInChildren<RecordedData>(includeInactive: true);

      for (int i = 0; i < recordings.Length; i++) {
        var recordingData = recordings[i];
        var toCompress = new Dictionary<EditorCurveBinding, AnimationCurve>();

        foreach (var bindingData in recordingData.data) {
          Type type = recordingData.GetComponents<Component>().
                                    Query().
                                    First(t => t.GetType().Name == bindingData.typeName).
                                    GetType();

          var binding = EditorCurveBinding.FloatCurve(bindingData.path, type, bindingData.propertyName);
          toCompress[binding] = bindingData.curve;
        }

        var propertyToMaxError = new Dictionary<string, float>();
        {
          Transform currTransform = recordingData.transform;
          while (currTransform != null) {
            var compressionSettings = currTransform.GetComponent<PropertyCompression>();
            if (compressionSettings != null) {
              foreach (var setting in compressionSettings.compressionOverrides) {
                if (!propertyToMaxError.ContainsKey(setting.propertyName)) {
                  propertyToMaxError.Add(setting.propertyName, setting.maxError);
                }
              }
            }
            currTransform = currTransform.parent;
          }
        }

        //First do all rotations
        while (true) {
          var wMaybe = toCompress.Keys.Query().FirstOrNone(b => b.propertyName.EndsWith(".w"));
          wMaybe.Match(wBinding => {
            string property = wBinding.propertyName.Substring(0, wBinding.propertyName.Length - 2);
            string xProp = property + ".x";
            string yProp = property + ".y";
            string zProp = property + ".z";

            var xMaybe = toCompress.Keys.Query().FirstOrNone(t => t.propertyName == xProp);
            var yMaybe = toCompress.Keys.Query().FirstOrNone(t => t.propertyName == yProp);
            var zMaybe = toCompress.Keys.Query().FirstOrNone(t => t.propertyName == zProp);

            Maybe.MatchAll(xMaybe, yMaybe, zMaybe, (xBinding, yBinding, zBinding) => {
              float errorRate;
              if (!propertyToMaxError.TryGetValue(property, out errorRate)) {
                errorRate = rotationMaxError;
              }




            });
          });
        }



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
