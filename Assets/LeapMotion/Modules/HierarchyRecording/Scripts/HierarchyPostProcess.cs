/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Query;
using Leap.Unity.Attributes;
using Leap.Unity.GraphicalRenderer;

namespace Leap.Unity.Recording {

  [RecordingFriendly]
  public class HierarchyPostProcess : MonoBehaviour {

    [Header("Recording Settings")]
    public string recordingName;
    public AssetFolder assetFolder;

    [Header("Leap Data")]
    public List<Frame> leapData;

    [SerializeField, ImplementsTypeNameDropdown(typeof(LeapRecording))]
    private string _leapRecordingType;

    [Header("Compression Settings")]
    [MinValue(0)]
    public float positionMaxError = 0.005f;

    [Range(0, 90)]
    public float rotationMaxError = 1;

    [MinValue(1)]
    public float scaleMaxError = 1.02f;

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

    [Header("Clear Settings")]
    [Tooltip("Deletes all scripts not marked as Recording Friendly.")]
    public bool clearUnfriendlyComponents = true;

    [Tooltip("Deletes all empty transforms that have no interesting children.")]
    public bool clearLeafEmpties = true;

    [Tooltip("Deletes all transforms that have the identity transformation.")]
    public bool collapseIdentityTransforms = true;

#if UNITY_EDITOR
    public void ClearComponents() {
      Transform[] transforms = GetComponentsInChildren<Transform>(includeInactive: true);

      foreach (var transform in transforms) {
        if (clearUnfriendlyComponents) {
          var scripts = transform.GetComponents<Component>().
                                  Query().
                                  Where(t => !RecordingFriendlyAttribute.IsRecordingFriendly(t)).
                                  ToList();
          do {
            foreach (var script in scripts) {
              DestroyImmediate(script);
            }
          } while (scripts.Query().ValidUnityObjs().Any());
        }
      }

      if (clearLeafEmpties) {
        while (true) {
          transforms = GetComponentsInChildren<Transform>(includeInactive: true);
          var empty = transforms.Query().FirstOrDefault(t => t.childCount == 0 &&
                                                             t.GetComponents<Component>().Length == 1);

          if (empty == null) {
            break;
          }

          DestroyImmediate(empty.gameObject);
        }
      }

      if (collapseIdentityTransforms) {
        while (true) {
          transforms = GetComponentsInChildren<Transform>(includeInactive: true);
          var empty = transforms.Query().FirstOrDefault(t => t.GetComponents<Component>().Length == 1 &&
                                                             t.localPosition == Vector3.zero &&
                                                             t.localRotation == Quaternion.identity &&
                                                             t.localScale == Vector3.one);
          if (empty == null) {
            break;
          }

          List<Transform> children = new List<Transform>();
          for (int i = 0; i < empty.childCount; i++) {
            children.Add(empty.GetChild(i));
          }

          foreach (var child in children) {
            child.SetParent(empty.parent, worldPositionStays: true);
          }

          DestroyImmediate(empty.gameObject);
        }
      }
    }

    public void BuildPlaybackPrefab(ProgressBar progress) {
      var timeline = ScriptableObject.CreateInstance<TimelineAsset>();

      var animationTrack = timeline.CreateTrack<AnimationTrack>(null, "Playback Animation");

      var clip = generateCompressedClip(progress);

      var timelineClip = animationTrack.CreateClip(clip);
      timelineClip.duration = clip.length;
      timelineClip.asset = clip;
      timelineClip.underlyingAsset = clip;

      //Try to generate a leap recording if we have leap data
      RecordingTrack recordingTrack = null;
      LeapRecording leapRecording = null;
      if (leapData.Count > 0) {
        leapRecording = ScriptableObject.CreateInstance(_leapRecordingType) as LeapRecording;
        if (leapRecording != null) {
          leapRecording.LoadFrames(leapData);
        } else {
          Debug.LogError("Unable to create Leap recording: Invalid type specification for "
                       + "LeapRecording implementation.", this);
        }
      }

      string assetPath = Path.Combine(assetFolder.Path, recordingName + ".asset");
      AssetDatabase.CreateAsset(timeline, assetPath);
      AssetDatabase.AddObjectToAsset(animationTrack, timeline);
      AssetDatabase.AddObjectToAsset(clip, timeline);

      //If we do have a leap recording, create a recording track to house it
      if (leapRecording != null) {
        recordingTrack = timeline.CreateTrack<RecordingTrack>(null, "Leap Recording");

        var recordingClip = recordingTrack.CreateDefaultClip();
        recordingClip.duration = leapRecording.length;

        var recordingAsset = recordingClip.asset as RecordingClip;
        recordingAsset.recording = leapRecording;

        AssetDatabase.AddObjectToAsset(leapRecording, timeline);
      }

      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      foreach (var recording in GetComponentsInChildren<RecordedData>(includeInactive: true)) {
        DestroyImmediate(recording);
      }

      //Create the playable director and link it to the new timeline
      var director = gameObject.AddComponent<PlayableDirector>();
      director.playableAsset = timeline;

      //Create the animator and link it to the animation track
      var animator = gameObject.AddComponent<Animator>();
      director.SetGenericBinding(animationTrack.outputs.Query().First().sourceObject, animator);

      //Destroy existing provider
      var provider = gameObject.GetComponentInChildren<LeapProvider>();
      if (provider != null) {
        GameObject providerObj = provider.gameObject;
        DestroyImmediate(provider);
        //If a leap recording track exists, spawn a playable provider and link it to the track
        if (recordingTrack != null) {
          var playableProvider = providerObj.AddComponent<LeapPlayableProvider>();
          director.SetGenericBinding(recordingTrack.outputs.Query().First().sourceObject, playableProvider);
        }
      }

      buildAudioTracks(progress, director, timeline);

      progress.Begin(1, "", "Finalizing Prefab", () => {
        GameObject myGameObject = gameObject;
        DestroyImmediate(this);

        string prefabPath = Path.Combine(assetFolder.Path, recordingName + ".prefab");
        PrefabUtility.CreatePrefab(prefabPath.Replace('\\', '/'), myGameObject);
      });
    }

    private void buildAudioTracks(ProgressBar progress, PlayableDirector director, TimelineAsset timeline) {
      var audioData = GetComponentsInChildren<RecordedAudio>(includeInactive: true);
      var sourceToData = audioData.Query().ToDictionary(a => a.target, a => a);

      progress.Begin(sourceToData.Count, "", "Building Audio Track: ", () => {
        foreach (var pair in sourceToData) {
          var track = timeline.CreateTrack<AudioTrack>(null, pair.Value.name);
          director.SetGenericBinding(track.outputs.Query().First().sourceObject, pair.Key);

          progress.Begin(pair.Value.data.Count, "", "", () => {
            foreach (var clipData in pair.Value.data) {
              progress.Step(clipData.clip.name);

              var clip = track.CreateClip(clipData.clip);
              clip.start = clipData.startTime;
              clip.timeScale = clipData.pitch;
              clip.duration = clipData.clip.length;
            }
          });
        }
      });
    }

    private AnimationClip generateCompressedClip(ProgressBar progress) {
      var clip = new AnimationClip();

      var bindingMap = new Dictionary<EditorCurveBinding, AnimationCurve>();
      var recordings = GetComponentsInChildren<RecordedData>(includeInactive: true);

      progress.Begin(2, "", "", () => {
        progress.Begin(recordings.Length, "", "Compressing: ", () => {
          for (int i = 0; i < recordings.Length; i++) {
            progress.Begin(2, "", "", () => {
              var recordingData = recordings[i];

              progress.Step(recordingData.name);

              var toCompress = new Dictionary<EditorCurveBinding, AnimationCurve>();

              progress.Begin(recordingData.data.Count, "", "", () => {
                foreach (var bindingData in recordingData.data) {
                  progress.Step(recordingData.name + " : " + bindingData.propertyName);

                  Type type = recordingData.GetComponents<Component>().
                                            Query().
                                            Select(c => c.GetType()).
                                            Concat(typeof(GameObject)).
                                            FirstOrDefault(t => t.Name == bindingData.typeName);

                  if (type == null) {
                    //If could not find the type, the component must have been deleted
                    continue;
                  }

                  var binding = EditorCurveBinding.FloatCurve(bindingData.path, type, bindingData.propertyName);
                  toCompress[binding] = bindingData.curve;
                }
              });

              doCompression(progress, recordingData, toCompress, bindingMap);
            });
          }
        });

        progress.Begin(bindingMap.Count, "", "Assigning Curves: ", () => {
          foreach (var binding in bindingMap) {
            progress.Step(binding.Key.propertyName);
            AnimationUtility.SetEditorCurve(clip, binding.Key, binding.Value);
          }
        });
      });

      return clip;
    }

    private void doCompression(ProgressBar progress,
                               RecordedData recordingData,
                               Dictionary<EditorCurveBinding, AnimationCurve> toCompress,
                               Dictionary<EditorCurveBinding, AnimationCurve> bindingMap) {
      var propertyToMaxError = calculatePropertyErrors(recordingData);

      List<EditorCurveBinding> bindings;

      progress.Begin(6, "", "", () => {

        //First do rotations
        bindings = toCompress.Keys.Query().ToList();
        progress.Begin(bindings.Count, "", "", () => {
          foreach (var wBinding in bindings) {
            progress.Step();

            if (!wBinding.propertyName.EndsWith(".w")) {
              continue;
            }

            string property = wBinding.propertyName.Substring(0, wBinding.propertyName.Length - 2);
            string xProp = property + ".x";
            string yProp = property + ".y";
            string zProp = property + ".z";

            var xMaybe = toCompress.Keys.Query().FirstOrNone(t => t.propertyName == xProp);
            var yMaybe = toCompress.Keys.Query().FirstOrNone(t => t.propertyName == yProp);
            var zMaybe = toCompress.Keys.Query().FirstOrNone(t => t.propertyName == zProp);

            Maybe.MatchAll(xMaybe, yMaybe, zMaybe, (xBinding, yBinding, zBinding) => {
              float maxAngleError;
              if (!propertyToMaxError.TryGetValue(property, out maxAngleError)) {
                maxAngleError = rotationMaxError;
              }

              AnimationCurve compressedX, compressedY, compressedZ, compressedW;
              AnimationCurveUtil.CompressRotations(toCompress[xBinding],
                                                   toCompress[yBinding],
                                                   toCompress[zBinding],
                                                   toCompress[wBinding],
                                               out compressedX,
                                               out compressedY,
                                               out compressedZ,
                                               out compressedW,
                                                   maxAngleError);

              bindingMap[xBinding] = compressedX;
              bindingMap[yBinding] = compressedY;
              bindingMap[zBinding] = compressedZ;
              bindingMap[wBinding] = compressedW;

              toCompress.Remove(xBinding);
              toCompress.Remove(yBinding);
              toCompress.Remove(zBinding);
              toCompress.Remove(wBinding);
            });
          }
        });

        //Next do scales
        bindings = toCompress.Keys.Query().ToList();
        progress.Begin(bindings.Count, "", "", () => {
          foreach (var binding in bindings) {
            progress.Step();

            if (!binding.propertyName.EndsWith(".x") &&
                !binding.propertyName.EndsWith(".y") &&
                !binding.propertyName.EndsWith(".z")) {
              continue;
            }

            if (!binding.propertyName.Contains("LocalScale")) {
              continue;
            }

            bindingMap[binding] = AnimationCurveUtil.CompressScale(toCompress[binding], scaleMaxError);
            toCompress.Remove(binding);
          }
        });

        //Next do positions
        bindings = toCompress.Keys.Query().ToList();
        progress.Begin(bindings.Count, "", "", () => {
          foreach (var xBinding in bindings) {
            progress.Step();

            if (!xBinding.propertyName.EndsWith(".x")) {
              continue;
            }

            string property = xBinding.propertyName.Substring(0, xBinding.propertyName.Length - 2);
            string yProp = property + ".y";
            string zProp = property + ".z";

            var yMaybe = toCompress.Keys.Query().FirstOrNone(t => t.propertyName == yProp);
            var zMaybe = toCompress.Keys.Query().FirstOrNone(t => t.propertyName == zProp);

            Maybe.MatchAll(yMaybe, zMaybe, (yBinding, zBinding) => {
              float maxDistanceError;
              if (!propertyToMaxError.TryGetValue(property, out maxDistanceError)) {
                maxDistanceError = positionMaxError;
              }

              AnimationCurve compressedX, compressedY, compressedZ;
              AnimationCurveUtil.CompressPositions(toCompress[xBinding],
                                                    toCompress[yBinding],
                                                    toCompress[zBinding],
                                                out compressedX,
                                                out compressedY,
                                                out compressedZ,
                                                    maxDistanceError);

              bindingMap[xBinding] = compressedX;
              bindingMap[yBinding] = compressedY;
              bindingMap[zBinding] = compressedZ;

              toCompress.Remove(xBinding);
              toCompress.Remove(yBinding);
              toCompress.Remove(zBinding);
            });
          }
        });

        //Next do colors
        bindings = toCompress.Keys.Query().ToList();
        progress.Begin(bindings.Count, "", "", () => {
          foreach (var rBinding in bindings) {
            progress.Step();

            if (!rBinding.propertyName.EndsWith(".r")) {
              continue;
            }

            string property = rBinding.propertyName.Substring(0, rBinding.propertyName.Length - 2);
            string gProp = property + ".g";
            string bProp = property + ".b";

            var gMaybe = toCompress.Keys.Query().FirstOrNone(t => t.propertyName == gProp);
            var bMaybe = toCompress.Keys.Query().FirstOrNone(t => t.propertyName == bProp);

            Maybe.MatchAll(gMaybe, bMaybe, (gBinding, bBinding) => {
              AnimationCurve compressedR, compressedG, compressedB;
              AnimationCurveUtil.CompressColorsHSV(toCompress[rBinding],
                                                   toCompress[gBinding],
                                                   toCompress[bBinding],
                                               out compressedR,
                                               out compressedG,
                                               out compressedB,
                                                   colorHueMaxError,
                                                   colorSaturationMaxError,
                                                   colorValueMaxError);

              bindingMap[rBinding] = compressedR;
              bindingMap[gBinding] = compressedG;
              bindingMap[bBinding] = compressedB;
            });
          }
        });

        //Then do color alpha
        bindings = toCompress.Keys.Query().ToList();
        progress.Begin(bindings.Count, "", "", () => {
          foreach (var aBinding in bindings) {
            progress.Step();

            if (!aBinding.propertyName.EndsWith(".a")) {
              continue;
            }

            var compressedA = AnimationCurveUtil.Compress(toCompress[aBinding], colorAlphaMaxError);

            toCompress.Remove(aBinding);
            bindingMap[aBinding] = compressedA;
          }
        });

        //Then everything else
        bindings = toCompress.Keys.Query().ToList();
        progress.Begin(bindings.Count, "", "", () => {
          foreach (var binding in bindings) {
            progress.Step();

            float maxError;
            if (!propertyToMaxError.TryGetValue(binding.propertyName, out maxError)) {
              maxError = genericMaxError;
            }

            var compressedCurve = AnimationCurveUtil.Compress(toCompress[binding], maxError);

            toCompress.Remove(binding);
            bindingMap[binding] = compressedCurve;
          }
        });
      });
    }

    private Dictionary<string, float> calculatePropertyErrors(RecordedData recordingData) {
      var propertyToMaxError = new Dictionary<string, float>();
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

      return propertyToMaxError;
    }
#endif
  }
}
