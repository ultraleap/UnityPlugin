/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Recording {
  using Query;

  public class TimelinePostProcess : MonoBehaviour {

    public TimelineAsset[] assets = new TimelineAsset[0];
    public string headPositionPath = "Leap Rig/Main Camera";

    public bool lerpStartingPosition = true;
    public Vector3 startHeadPosition;
    public bool lerpEndingPosition = true;
    public Vector3 endHeadPosition;

    public bool cropAnimation = false;

    public string[] allBindings;

#if UNITY_EDITOR
    private void OnValidate() {
      allBindings = allClips.SelectMany(c => AnimationUtility.GetCurveBindings(c.Value)).
                             Select(b => b.path).
                             Distinct().
                             OrderBy(p => p).
                             ToArray();
    }

    [ContextMenu("Perform Post Process")]
    public void PerformPostProcess() {
      Dictionary<AnimationClip, TimeRange> ranges = new Dictionary<AnimationClip, TimeRange>();

      float total = allClips.Count();
      int index = 0;

      foreach (var pair in allClips) {
        var clip = pair.Key;
        var animClip = pair.Value;

        EditorUtility.DisplayCancelableProgressBar("Post processing clips", "Processing " + animClip.name, index / total);

        TimeRange range;
        if (!ranges.TryGetValue(animClip, out range)) {
          range = new TimeRange() {
            start = (float)clip.clipIn,
            end = (float)(clip.clipIn + clip.duration)
          };
        } else {
          range.start = Mathf.Min(range.start, (float)clip.clipIn);
          range.end = Mathf.Max(range.end, (float)(clip.clipIn + clip.duration));
        }

        ranges[animClip] = range;
      }

      try {
        foreach (var pair in allClips) {
          var clip = pair.Key;
          var animClip = pair.Value;

          index++;
          EditorUtility.DisplayCancelableProgressBar("Post-processing clips", "Processing " + animClip.name, index / total);

          BlendHeadPosition(clip, animClip);

          if (cropAnimation) {
            CropAnimation(animClip, (float)clip.clipIn, (float)(clip.clipIn + clip.duration));
          }
        }

        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();
      } finally {
        EditorUtility.ClearProgressBar();
      }
    }

    public void CropAnimation(AnimationClip clip, float start, float end) {
      var bindings = AnimationUtility.GetCurveBindings(clip);

      foreach (var binding in bindings) {
        var curve = AnimationUtility.GetEditorCurve(clip, binding);
        var cropped = AnimationCurveUtil.GetCropped(curve, start, end, slideToStart: true);
        AnimationUtility.SetEditorCurve(clip, binding, cropped);
      }
    }

    public void BlendHeadPosition(TimelineClip clip, AnimationClip animClip) {
      var bindings = AnimationUtility.GetCurveBindings(animClip);

      float startTime = (float)clip.clipIn;
      float endTime = (float)(clip.clipIn + clip.duration);

      var xBinding = bindings.Query().FirstOrNone(b => b.path == headPositionPath && b.propertyName == "m_LocalPosition.x");
      var yBinding = bindings.Query().FirstOrNone(b => b.path == headPositionPath && b.propertyName == "m_LocalPosition.y");
      var zBinding = bindings.Query().FirstOrNone(b => b.path == headPositionPath && b.propertyName == "m_LocalPosition.z");

      if (!xBinding.hasValue || !yBinding.hasValue || !zBinding.hasValue) {
        return;
      }

      xBinding.Match(xB => {
        yBinding.Match(yB => {
          zBinding.Match(zB => {
            bindings = new EditorCurveBinding[] { xB, yB, zB };
          });
        });
      });

      for (int i = 0; i < 3; i++) {
        var binding = bindings[i];
        var curve = AnimationUtility.GetEditorCurve(animClip, binding);

        float startPos = curve.Evaluate(startTime);
        float endPos = curve.Evaluate(endTime);

        float startOffset = startHeadPosition[i] - startPos;
        float endOffset = endHeadPosition[i] - endPos;

        if (!lerpStartingPosition) startOffset = 0;
        if (!lerpEndingPosition) endOffset = 0;

        var keys = curve.keys;
        for (int j = 0; j < keys.Length; j++) {
          var key = keys[j];

          float percent = (key.time - startTime) / (endTime - startTime);
          float offset = Mathf.LerpUnclamped(startOffset, endOffset, percent);
          key.value += offset;

          curve.MoveKey(j, key);
        }

        AnimationUtility.SetEditorCurve(animClip, binding, curve);
      }
    }

    private IEnumerable<KeyValuePair<TimelineClip, AnimationClip>> allClips {
      get {
        foreach (var timeline in assets) {
          if (timeline == null) continue;

          foreach (var track in timeline.GetOutputTracks()) {
            var animTrack = track as AnimationTrack;
            if (animTrack != null) {
              foreach (var clip in animTrack.GetClips()) {
                var animClip = clip.animationClip;
                yield return new KeyValuePair<TimelineClip, AnimationClip>(clip, animClip);
              }
            }
          }
        }
      }
    }

    private struct TimeRange {
      public float start, end;
    }
#endif
  }
}
