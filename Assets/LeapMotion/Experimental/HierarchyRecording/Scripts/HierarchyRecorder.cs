/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Query;

namespace Leap.Unity.Recording {
  using Attributes;

  public class HierarchyRecorder : MonoBehaviour {
    public static Action OnPreRecordFrame;
    public static Action OnBeginRecording;
    public static HierarchyRecorder instance;

    [EnumFlags]
    public RecordOn recordWhen = 0;
    public string recordingName;
    public AssetFolder targetFolder;

    [Header("Leap Recording Settings")]
    public LeapProvider provider;
    public bool recordLeapData = false;

    [Header("Key Bindings")]
    public KeyCode beginRecordingKey = KeyCode.F5;
    public KeyCode finishRecordingKey = KeyCode.F6;

    protected AnimationClip _clip;

    protected List<Component> _components;
    protected List<PropertyRecorder> _recorders;

    protected List<Behaviour> _tempBehaviour = new List<Behaviour>();
    protected List<Renderer> _tempRenderer = new List<Renderer>();
    protected List<Collider> _tempCollider = new List<Collider>();

    protected HashSet<string> _takenNames = new HashSet<string>();

    protected bool _isRecording = false;
    protected float _startTime = 0;
    protected int _startFrame = 0;

    public bool isRecording {
      get { return _isRecording; }
    }

    public float recordingTime {
      get { return Time.time - _startTime; }
    }

    [Flags]
    public enum RecordOn {
      Start = 0x01,
      HMDPresence = 0x02,
      HandPresence = 0x04
    }

#if UNITY_EDITOR
    protected List<Frame> _leapData;
    protected List<CurveData> _curves;
    protected Dictionary<AudioSource, RecordedAudio> _audioData;
    protected Dictionary<Transform, List<TransformData>> _transformData;
    protected Dictionary<Component, SerializedObject> _initialComponentData;
    protected Dictionary<Component, List<ActivityData>> _behaviourActivity;

    protected void Reset() {
      recordingName = gameObject.name;
    }

    protected void Start() {
      instance = this;

      if ((recordWhen & RecordOn.Start) != 0) {
        BeginRecording();
      }
    }

    protected void LateUpdate() {
      if (!_isRecording && (recordWhen & RecordOn.HMDPresence) != 0 && XRSupportUtil.IsUserPresent()) {
        BeginRecording();
      }

      if ((Hands.Left != null || Hands.Right != null) && (recordWhen & RecordOn.HandPresence) != 0) {
        BeginRecording();
      }

      if (Input.GetKeyDown(beginRecordingKey)) {
        BeginRecording();
      }

      if (Input.GetKeyDown(finishRecordingKey)) {
        finishRecording(new ProgressBar());
      }

      if (_isRecording) {
        recordData();
      }
    }

    /// <summary>
    /// Starts recording with this Hierarchy Recorder.
    /// </summary>
    public void BeginRecording() {
      if (_isRecording) return;
      _isRecording = true;
      _startTime = Time.time;
      _startFrame = Time.frameCount;

      _components = new List<Component>();
      _recorders = new List<PropertyRecorder>();
      _leapData = new List<Frame>();

      _curves = new List<CurveData>();
      _audioData = new Dictionary<AudioSource, RecordedAudio>();
      _transformData = new Dictionary<Transform, List<TransformData>>();
      _initialComponentData = new Dictionary<Component, SerializedObject>();
      _behaviourActivity = new Dictionary<Component, List<ActivityData>>();

      if (OnBeginRecording != null) {
        OnBeginRecording();
      }
    }

    /// <summary>
    /// Finishes recording with this Hierarchy Recorder.
    /// </summary>
    public void FinishRecording() {
      finishRecording(new ProgressBar());
    }

    protected void finishRecording(ProgressBar progress) {

      string targetFolderPath = targetFolder.Path;
      if (targetFolderPath == null) {
        if (gameObject.scene.IsValid() && !string.IsNullOrEmpty(gameObject.scene.path)) {
          string sceneFolder = Path.GetDirectoryName(gameObject.scene.path);
          targetFolderPath = Path.Combine(sceneFolder, "Recordings");
        } else {
          targetFolderPath = Path.Combine("Assets", "Recordings");
        }
      }

      int folderSuffix = 1;
      string finalSubFolder;
      do {
        finalSubFolder = Path.Combine(targetFolderPath, recordingName + " " + folderSuffix.ToString().PadLeft(2, '0'));
        folderSuffix++;
      } while (Directory.Exists(finalSubFolder));

      string dataDirectory = Path.Combine(finalSubFolder, "_Data");

      Directory.CreateDirectory(dataDirectory);
      Directory.CreateDirectory(finalSubFolder);
      AssetDatabase.Refresh();

      progress.Begin(6, "Saving Recording", "", () => {
        if (!_isRecording) return;
        _isRecording = false;

        //Turn on auto-pushing for all auto-proxy components
        foreach (var autoProxy in GetComponentsInChildren<AutoValueProxy>()) {
          autoProxy.autoPushingEnabled = true;
        }

        progress.Begin(3, "", "Reverting Scene State", () => {
          //For all of our transform data, revert to the first piece recorded
          progress.Begin(_transformData.Count, "", "", () => {
            foreach (var pair in _transformData) {
              progress.Step();
              var transform = pair.Key;
              var data = pair.Value;

              if (transform == null || data.Count == 0) {
                continue;
              }

              data[0].ApplyTo(transform);
            }
          });

          //For all recorded curves, revert to start of curve
          progress.Begin(_curves.Count, "", "", () => {
            AnimationClip tempClip = new AnimationClip();
            foreach (var data in _curves) {
              progress.Step();
              AnimationUtility.SetEditorCurve(tempClip, data.binding, data.curve);
            }
            tempClip.SampleAnimation(gameObject, 0);
          });

          //For all non-transform components, revert to original serialized values
          progress.Begin(_initialComponentData.Count, "", "", () => {
            foreach (var pair in _initialComponentData) {
              progress.Step();
              var component = pair.Key;
              var sobj = pair.Value;

              if (component == null || component is Transform) {
                continue;
              }


              //We don't want to revert method recordings!
              if (component is MethodRecording ||
                  component is RecordedAudio) {
                continue;
              }

              var flags = sobj.FindProperty("m_ObjectHideFlags");
              if (flags == null) {
                Debug.LogError("Could not find hide flags for " + component);
                continue;
              }

              //We have to dirty the serialized object somehow
              //apparently there is no api to do this
              //all objects have hide flags so we just touch them and revert them
              int originalFlags = flags.intValue;
              flags.intValue = ~originalFlags;
              flags.intValue = originalFlags;

              try {
                //Applies previous state of entire component
                sobj.ApplyModifiedProperties();
              } catch (Exception e) {
                Debug.LogError("Exception when trying to apply properties to " + component);
                Debug.LogException(e);
              }
            }
          });
        });

        progress.Begin(1, "", "Patching Materials: ", () => {
          GetComponentsInChildren(true, _recorders);
          foreach (var recorder in _recorders) {
            DestroyImmediate(recorder);
          }

          //Patch up renderer references to materials
          var allMaterials = Resources.FindObjectsOfTypeAll<Material>().
                                       Query().
                                       Where(AssetDatabase.IsMainAsset).
                                       ToList();

          var renderers = GetComponentsInChildren<Renderer>(includeInactive: true);

          progress.Begin(renderers.Length, "", "", () => {
            foreach (var renderer in renderers) {
              progress.Step(renderer.name);

              var materials = renderer.sharedMaterials;
              for (int i = 0; i < materials.Length; i++) {
                var material = materials[i];
                if (material == null) {
                  continue;
                }

                if (!AssetDatabase.IsMainAsset(material)) {
                  var matchingMaterial = allMaterials.Query().FirstOrDefault(m => material.name.Contains(m.name) &&
                                                                                  material.shader == m.shader);

                  if (matchingMaterial != null) {
                    materials[i] = matchingMaterial;
                  }
                }
              }
              renderer.sharedMaterials = materials;
            }
          });
        });

        progress.Begin(_behaviourActivity.Count, "", "Converting Activity Data: ", () => {
          foreach (var pair in _behaviourActivity) {
            var targetBehaviour = pair.Key;
            var activityData = pair.Value;

            if (targetBehaviour == null) {
              continue;
            }

            progress.Step(targetBehaviour.name);

            string path = AnimationUtility.CalculateTransformPath(targetBehaviour.transform, transform);
            Type type = targetBehaviour.GetType();
            string propertyName = "m_Enabled";

            AnimationCurve curve = new AnimationCurve();
            foreach (var dataPoint in activityData) {
              int index = curve.AddKey(dataPoint.time, dataPoint.enabled ? 1 : 0);
              AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Constant);
              AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Constant);
            }

            if (curve.IsConstant()) {
              continue;
            }

            var binding = EditorCurveBinding.FloatCurve(path, type, propertyName);

            if (_curves.Query().Any(c => c.binding == binding)) {
              Debug.LogError("Binding already existed?");
              Debug.LogError(binding.path + " : " + binding.propertyName);
              continue;
            }

            _curves.Add(new CurveData() {
              binding = binding,
              curve = curve
            });
          }
        });

        progress.Begin(_transformData.Count, "", "Converting Transform Data: ", () => {
          foreach (var pair in _transformData) {
            var targetTransform = pair.Key;
            var targetData = pair.Value;

            progress.Step(targetTransform.name);

            string path = AnimationUtility.CalculateTransformPath(targetTransform, transform);

            bool isActivityConstant = true;
            bool isPositionConstant = true;
            bool isRotationConstant = true;
            bool isScaleConstant = true;

            {
              bool startEnabled = targetData[0].enabled;
              Vector3 startPosition = targetData[0].localPosition;
              Quaternion startRotation = targetData[0].localRotation;
              Vector3 startScale = targetData[0].localScale;
              for (int i = 1; i < targetData.Count; i++) {
                isActivityConstant &= targetData[i].enabled == startEnabled;
                isPositionConstant &= targetData[i].localPosition == startPosition;
                isRotationConstant &= targetData[i].localRotation == startRotation;
                isScaleConstant &= targetData[i].localScale == startScale;
              }
            }

            for (int i = 0; i < TransformData.CURVE_COUNT; i++) {
              string propertyName = TransformData.GetName(i);
              Type type = typeof(Transform);

              AnimationCurve curve = new AnimationCurve();
              var dataType = TransformData.GetDataType(i);

              switch (dataType) {
                case TransformDataType.Position:
                  if (isPositionConstant) continue;
                  break;
                case TransformDataType.Rotation:
                  if (isRotationConstant) continue;
                  break;
                case TransformDataType.Scale:
                  if (isScaleConstant) continue;
                  break;
                case TransformDataType.Activity:
                  if (isActivityConstant) continue;
                  type = typeof(GameObject);
                  break;
              }

              for (int j = 0; j < targetData.Count; j++) {
                int index = curve.AddKey(targetData[j].time, targetData[j].GetFloat(i));
                if (dataType == TransformDataType.Activity) {
                  AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Constant);
                  AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Constant);
                }
              }

              var binding = EditorCurveBinding.FloatCurve(path, type, propertyName);

              if (_curves.Query().Any(c => c.binding == binding)) {
                Debug.LogError("Duplicate object was created??");
                Debug.LogError("Named " + targetTransform.name + " : " + binding.path + " : " + binding.propertyName);
              } else {
                _curves.Add(new CurveData() {
                  binding = binding,
                  curve = curve
                });
              }
            }
          }
        });

        progress.Begin(_curves.Count, "", "Compressing Data: ", () => {
          _curves.Sort((a, b) => a.binding.propertyName.CompareTo(b.binding.propertyName));
          foreach (var data in _curves) {
            using (new ProfilerSample("A")) {
              EditorCurveBinding binding = data.binding;
              AnimationCurve curve = data.curve;

              progress.Step(binding.propertyName);

              GameObject animationGameObject;
              {
                var animatedObj = AnimationUtility.GetAnimatedObject(gameObject, binding);
                if (animatedObj is GameObject) {
                  animationGameObject = animatedObj as GameObject;
                } else {
                  animationGameObject = (animatedObj as Component).gameObject;
                }
              }

              bool isMatBinding = binding.propertyName.StartsWith("material.") &&
                                  binding.type.IsSubclassOf(typeof(Renderer));

              //But if the curve is constant, just get rid of it!
              //Except for material curves, which we always need to keep
              if (curve.IsConstant() && !isMatBinding) {
                //Check to make sure there are no other matching curves that are
                //non constant.  If X and Y are constant but Z is not, we need to 
                //keep them all :(
                if (_curves.Query().Where(p => p.binding.path == binding.path &&
                                               p.binding.type == binding.type &&
                                               p.binding.propertyName.TrimEnd(2) == binding.propertyName.TrimEnd(2)).
                                    All(k => k.curve.IsConstant())) {
                  continue;
                }
              }

              //First do a lossless compression
              using (new ProfilerSample("B")) {
                curve = AnimationCurveUtil.Compress(curve, Mathf.Epsilon, checkSteps: 3);
              }

              Transform targetTransform = null;
              var targetObj = AnimationUtility.GetAnimatedObject(gameObject, binding);
              if (targetObj is GameObject) {
                targetTransform = (targetObj as GameObject).transform;
              } else if (targetObj is Component) {
                targetTransform = (targetObj as Component).transform;
              } else {
                Debug.LogError("Target obj was of type " + targetObj.GetType().Name);
              }
            }
          }
        });
      });

      progress.Begin(4, "Finalizing Assets", "", () => {
        var postProcessComponent = gameObject.AddComponent<HierarchyPostProcess>();

        GameObject myGameObject = gameObject;

        DestroyImmediate(this);

        //Create all the files for the method recording
        progress.Step("Creating Method Recording Files...");
        var methodRecordings = myGameObject.GetComponentsInChildren<MethodRecording>();
        for (int i = 0; i < methodRecordings.Length; i++) {
          var methodRecording = methodRecordings[i];
          string fullPath = Path.Combine(finalSubFolder, "MethodRecording_" + i + ".asset");
          methodRecording.ExitRecordingMode(fullPath);
        }

        postProcessComponent.dataFolder = new AssetFolder(dataDirectory);

        //Create the asset that holds all of the curve data
        progress.Begin(_curves.Count, "", "", () => {
          string curveFile = Path.Combine(dataDirectory, "Curves.data");
          using (var writer = File.CreateText(curveFile)) {
            foreach (var data in _curves) {
              progress.Step(data.binding.propertyName);

              var bindingData = new EditorCurveBindingData() {
                path = data.binding.path,
                propertyName = data.binding.propertyName,
                typeName = data.binding.type.Name,
                curve = data.curve
              };

              writer.WriteLine(JsonUtility.ToJson(bindingData));
            }
          }
        });

        //Create the asset that holds all of the leap data
        if (_leapData.Count > 0) {
          progress.Begin(_leapData.Count, "", "", () => {
            string leapFile = Path.Combine(dataDirectory, "Frames.data");

            using (var writer = File.CreateText(leapFile)) {
              for (int i = 0; i < _leapData.Count; i++) {
                progress.Step("Frame " + i);
                writer.WriteLine(JsonUtility.ToJson(_leapData[i]));
              }
            }
          });
        }

        progress.Step("Creating Final Prefab...");
        //Init the post process component
        postProcessComponent.recordingName = recordingName;
        postProcessComponent.assetFolder = new AssetFolder(finalSubFolder);

        string prefabPath = Path.Combine(finalSubFolder, recordingName + " Raw.prefab");
        PrefabUtility.CreatePrefab(prefabPath.Replace('\\', '/'), myGameObject);

        AssetDatabase.Refresh();

        EditorApplication.isPlaying = false;
      });
    }

    protected void recordData() {
      using (new ProfilerSample("Dispatch PreRecord Event")) {
        if (OnPreRecordFrame != null) {
          OnPreRecordFrame();
        }
      }

      using (new ProfilerSample("Search For New Components")) {
        using (new ProfilerSample("Get Components In Children")) {
          GetComponentsInChildren(true, _components);
        }

        for (int i = 0; i < _components.Count; i++) {
          var component = _components[i];

          if (!_initialComponentData.ContainsKey(component)) {
            using (new ProfilerSample("Handle New Component")) {
              _initialComponentData[component] = new SerializedObject(component);

              //First time experiencing a gameobject
              if (component is Transform) {
                using (new ProfilerSample("Handle New Transform")) {
                  var transform = component as Transform;

                  _transformData[transform] = new List<TransformData>();

                  var parent = transform.parent;
                  if (parent != null) {
                    var newName = transform.name;

                    for (int j = 0; j < parent.childCount; j++) {
                      var sibling = parent.GetChild(j);
                      if (sibling != transform && sibling.name == transform.name) {
                        transform.name = transform.name + " " + transform.gameObject.GetInstanceID();
                        break;
                      }
                    }
                  }
                }
              }

              if (component is AudioSource) {
                using (new ProfilerSample("Handle New AudioSource")) {
                  var source = component as AudioSource;
                  var data = source.gameObject.AddComponent<RecordedAudio>();
                  data.target = source;
                  _audioData[source] = data;
                }
              }

              if (component is PropertyRecorder) {
                using (new ProfilerSample("Handle New PropertyRecorder")) {
                  var recorder = component as PropertyRecorder;
                  foreach (var binding in recorder.GetBindings(gameObject)) {
                    _curves.Add(new CurveData() {
                      binding = binding,
                      curve = new AnimationCurve(),
                      accessor = new PropertyAccessor(gameObject, binding, failureIsZero: true)
                    });
                  }
                }
              }

              if (((component is Behaviour) || (component is Renderer) || (component is Collider)) &&
                  !(component is PropertyRecorder)) {
                using (new ProfilerSample("Handle New Behaviour")) {
                  _behaviourActivity[component] = new List<ActivityData>();
                }
              }
            }
          }

          if (component is IValueProxy) (component as IValueProxy).OnPullValue();
        }
      }

      using (new ProfilerSample("Record Custom Data")) {
        float time = Time.time - _startTime;
        for (int i = 0; i < _curves.Count; i++) {
          _curves[i].SampleNow(time);
        }
      }

      using (new ProfilerSample("Record Transform Data")) {
        foreach (var pair in _transformData) {
          var list = pair.Value;
          var transform = pair.Key;

          //If we have no data for this object BUT we also are not
          //on the first frame of recording, this object must have
          //been spawned, make sure to record a frame with it being
          //disabled right before this
          if (list.Count == 0 && Time.time > _startTime) {
            list.Add(new TransformData() {
              time = Time.time - _startTime - Time.deltaTime,
              enabled = false,
              localPosition = transform.localPosition,
              localRotation = transform.localRotation,
              localScale = transform.localScale
            });
          }

          list.Add(new TransformData() {
            time = Time.time - _startTime,
            enabled = transform.gameObject.activeSelf,
            localPosition = transform.localPosition,
            localRotation = transform.localRotation,
            localScale = transform.localScale
          });
        }
      }

      using (new ProfilerSample("Record Behaviour Activity Data")) {
        foreach (var pair in _behaviourActivity) {
          pair.Value.Add(new ActivityData() {
            time = Time.time - _startTime,
            enabled = EditorUtility.GetObjectEnabled(pair.Key) == 1
          });
        }
      }

      if (provider != null && recordLeapData) {
        using (new ProfilerSample("Record Leap Data")) {
          Frame newFrame = new Frame();
          newFrame.CopyFrom(provider.CurrentFrame);
          newFrame.CurrentFramesPerSecond = 1.0f / Time.smoothDeltaTime;
          newFrame.Timestamp = (long)((Time.time - _startTime) * 1e6);
          newFrame.Id = Time.frameCount - _startFrame;
          _leapData.Add(newFrame);
        }
      }
    }

    protected struct ActivityData {
      public float time;
      public bool enabled;
    }

    protected enum TransformDataType {
      Activity,
      Position,
      Rotation,
      Scale
    }

    protected struct CurveData {
      public EditorCurveBinding binding;
      public AnimationCurve curve;
      public PropertyAccessor accessor;

      public void SampleNow(float time) {
        curve.AddKey(time, accessor.Access());
      }
    }

    protected struct TransformData {
      public const int CURVE_COUNT = 11;

      public float time;

      public bool enabled;
      public Vector3 localPosition;
      public Quaternion localRotation;
      public Vector3 localScale;

      public float GetFloat(int index) {
        switch (index) {
          case 0:
          case 1:
          case 2:
            return localPosition[index];
          case 3:
          case 4:
          case 5:
          case 6:
            return localRotation[index - 3];
          case 7:
          case 8:
          case 9:
            return localScale[index - 7];
          case 10:
            return enabled ? 1 : 0;
        }
        throw new Exception();
      }

      public static string GetName(int index) {
        switch (index) {
          case 0:
          case 1:
          case 2:
            return "m_LocalPosition." + "xyz"[index];
          case 3:
          case 4:
          case 5:
          case 6:
            return "m_LocalRotation." + "xyzw"[index - 3];
          case 7:
          case 8:
          case 9:
            return "m_LocalScale" + "xyz"[index - 7];
          case 10:
            return "m_IsActive";
        }
        throw new Exception();
      }

      public static TransformDataType GetDataType(int index) {
        switch (index) {
          case 0:
          case 1:
          case 2:
            return TransformDataType.Position;
          case 3:
          case 4:
          case 5:
          case 6:
            return TransformDataType.Rotation;
          case 7:
          case 8:
          case 9:
            return TransformDataType.Scale;
          case 10:
            return TransformDataType.Activity;
        }
        throw new Exception();
      }

      public void ApplyTo(Transform transform) {
        transform.localPosition = localPosition;
        transform.localRotation = localRotation;
        transform.localScale = localScale;
        transform.gameObject.SetActive(enabled);
      }
    }

    #region GUI

    protected Vector2 _guiMargins = new Vector2(5F, 5F);
    protected Vector2 _guiSize = new Vector2(175F, 60F);

    protected void OnGUI() {
      var guiRect = new Rect(_guiMargins.x, _guiMargins.y, _guiSize.x, _guiSize.y);

      GUI.Box(guiRect, "");

      GUILayout.BeginArea(guiRect.PadInner(5F));
      GUILayout.BeginVertical();

      if (!_isRecording) {
        GUILayout.Label("Ready to record.");
        if (GUILayout.Button("Start Recording (" + beginRecordingKey.ToString() + ")",
                             GUILayout.ExpandHeight(true))) {
          BeginRecording();
        }
      } else {
        GUILayout.Label("Recording.");
        if (GUILayout.Button("Stop Recording (" + finishRecordingKey.ToString() + ")",
                             GUILayout.ExpandHeight(true))) {
          finishRecording(new ProgressBar());
        }
      }

      GUILayout.EndVertical();
      GUILayout.EndArea();
    }

    #endregion

#endif
  }
}
