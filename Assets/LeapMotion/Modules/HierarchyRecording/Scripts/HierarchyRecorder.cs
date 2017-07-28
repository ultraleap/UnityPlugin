using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity.Query;
using Leap.Unity.GraphicalRenderer;

namespace Leap.Unity.Recording {

  public class HierarchyRecorder : MonoBehaviour {
    public static Action OnPreRecordFrame;

    public bool recordOnStart = false;
    public KeyCode beginRecordingKey = KeyCode.F5;
    public KeyCode finishRecordingKey = KeyCode.F6;

    private AnimationClip _clip;

    private List<Component> _components;

    private List<Transform> _transforms;
    private List<Component> _behaviours;
    private List<AudioSource> _audioSources;
    private List<PropertyRecorder> _recorders;

    private Dictionary<EditorCurveBinding, AnimationCurve> _curves;
    private Dictionary<AudioSource, RecordedAudio> _audioData;
    private Dictionary<Transform, List<TransformData>> _transformData;
    private Dictionary<Component, List<ActivityData>> _behaviourActivity;

    private HashSet<string> _takenNames = new HashSet<string>();

    private bool _isRecording = false;
    private float _startTime = 0;

    private void Start() {
      if (recordOnStart) {
        beginRecording();
      }
    }

    private void LateUpdate() {
      if (Input.GetKeyDown(beginRecordingKey)) {
        beginRecording();
      }

      if (Input.GetKeyDown(finishRecordingKey)) {
        finishRecording(new ProgressBar());
      }

      if (_isRecording) {
        recordData();
      }
    }

    private void beginRecording() {
      if (_isRecording) return;
      _isRecording = true;
      _startTime = Time.time;

      _components = new List<Component>();

      _transforms = new List<Transform>();
      _behaviours = new List<Component>();
      _recorders = new List<PropertyRecorder>();
      _audioSources = new List<AudioSource>();

      _curves = new Dictionary<EditorCurveBinding, AnimationCurve>();
      _audioData = new Dictionary<AudioSource, RecordedAudio>();
      _transformData = new Dictionary<Transform, List<TransformData>>();
      _behaviourActivity = new Dictionary<Component, List<ActivityData>>();
    }

    private void finishRecording(ProgressBar progress) {
      progress.Begin(4, "Saving Recording", "", () => {
        if (!_isRecording) return;
        _isRecording = false;

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
            _curves.Add(binding, curve);
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

              if (_curves.ContainsKey(binding)) {
                Debug.LogError("Duplicate object was created??");
                Debug.LogError("Named " + targetTransform.name + " : " + binding.path + " : " + binding.propertyName);
              } else {
                _curves.Add(binding, curve);
              }
            }
          }
        });

        progress.Begin(_curves.Count, "", "Compressing Data: ", () => {
          foreach (var pair in _curves) {
            EditorCurveBinding binding = pair.Key;
            AnimationCurve curve = pair.Value;

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

            //But if the curve is constant, just get rid of it!
            if (curve.IsConstant()) {
              //Check to make sure there are no other matching curves that are
              //non constant.  If X and Y are constant but Z is not, we need to 
              //keep them all :(
              if (_curves.Query().Where(p => p.Key.path == binding.path &&
                                             p.Key.type == binding.type &&
                                             p.Key.propertyName.TrimEnd(2) == binding.propertyName.TrimEnd(2)).
                                  All(k => k.Value.IsConstant())) {
                continue;
              }
            }

            //First do a lossless compression
            curve = AnimationCurveUtil.Compress(curve, Mathf.Epsilon);

            //If the curve controls a proxy object, convert the binding to the playback
            //type and spawn the playback component
            if (AnimationProxyAttribute.IsAnimationProxy(binding.type)) {
              Type playbackType = AnimationProxyAttribute.ConvertToPlaybackType(binding.type);

              //If we have not yet spawned the playback component, spawn it now
              if (animationGameObject.GetComponent(playbackType) == null) {
                animationGameObject.AddComponent(playbackType);
              }

              binding = new EditorCurveBinding() {
                path = binding.path,
                propertyName = binding.propertyName,
                type = playbackType
              };
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

            var dataRecorder = targetTransform.GetComponent<RecordedData>();
            if (dataRecorder == null) {
              dataRecorder = targetTransform.gameObject.AddComponent<RecordedData>();
            }

            dataRecorder.data.Add(new RecordedData.EditorCurveBindingData() {
              path = binding.path,
              propertyName = binding.propertyName,
              typeName = binding.type.Name,
              curve = curve
            });
          }
        });

        progress.Step("Finalizing Prefab...");

        gameObject.AddComponent<HierarchyPostProcess>();

        GameObject myGameObject = gameObject;

        DestroyImmediate(this);

        PrefabUtility.CreatePrefab("Assets/LeapMotion/Modules/HierarchyRecording/RawRecording.prefab", myGameObject);
      });
    }

    private void recordData() {
      using (new ProfilerSample("Dispatch PreRecord Event")) {
        if (OnPreRecordFrame != null) {
          OnPreRecordFrame();
        }
      }

      using (new ProfilerSample("Get Components In Hierarchy")) {
        GetComponentsInChildren(true, _components);

        _transforms.Clear();
        _audioSources.Clear();
        _recorders.Clear();
        _behaviours.Clear();
        for (int i = 0; i < _components.Count; i++) {
          var component = _components[i];

          if (component is Transform) _transforms.Add(component as Transform);
          if (component is AudioSource) _audioSources.Add(component as AudioSource);
          if (component is PropertyRecorder) _recorders.Add(component as PropertyRecorder);

          if (component is Behaviour) _behaviours.Add(component);
          if (component is Renderer) _behaviours.Add(component);
          if (component is Collider) _behaviours.Add(component);
        }
      }

      using (new ProfilerSample("Ensure Names Are Unique")) {
        foreach (var transform in _transforms) {
          for (int i = 0; i < transform.childCount; i++) {
            Transform child = transform.GetChild(i);
            if (_takenNames.Contains(child.name)) {
              child.name = child.name + " " + Mathf.Abs(child.GetInstanceID());
            }
            _takenNames.Add(child.name);
          }
          _takenNames.Clear();
        }
      }

      using (new ProfilerSample("Discover Audio Sources")) {
        //Update all audio sources
        foreach (var source in _audioSources) {
          RecordedAudio data;
          if (!_audioData.TryGetValue(source, out data)) {
            data = source.gameObject.AddComponent<RecordedAudio>();
            data.target = source;
            data.recordingStartTime = _startTime;
            _audioData[source] = data;
          }
        }
      }

      using (new ProfilerSample("Discover Property Recorders")) {
        //Record all properties specified by recorders
        foreach (var recorder in _recorders) {
          foreach (var bindings in recorder.GetBindings(gameObject)) {
            if (!_curves.ContainsKey(bindings)) {
              _curves[bindings] = new AnimationCurve();
            }
          }
        }
      }

      using (new ProfilerSample("Record Custom Data")) {
        foreach (var pair in _curves) {
          float value;
          bool gotValue = AnimationUtility.GetFloatValue(gameObject, pair.Key, out value);
          if (gotValue) {
            pair.Value.AddKey(Time.time - _startTime, value);
          } else {
            Debug.Log(pair.Key.path + " : " + pair.Key.propertyName + " : " + pair.Key.type.Name);
          }
        }
      }

      using (new ProfilerSample("Discover Transforms")) {
        //Record ALL transform and gameObject data, no matter what
        foreach (var transform in _transforms) {
          if (!_transformData.ContainsKey(transform)) {
            _transformData[transform] = new List<TransformData>();
          }
        }
      }

      using (new ProfilerSample("Record Transform Data")) {
        foreach (var pair in _transformData) {
          //If we have no data for this object BUT we also are not
          //on the first frame of recording, this object must have
          //been spawned, make sure to record a frame with it being
          //disabled right before this
          if (pair.Value.Count == 0 && Time.time > _startTime) {
            pair.Value.Add(new TransformData() {
              time = Time.time - _startTime - Time.deltaTime,
              enabled = false,
              localPosition = pair.Key.localPosition,
              localRotation = pair.Key.localRotation,
              localScale = pair.Key.localScale
            });
          }

          pair.Value.Add(new TransformData() {
            time = Time.time - _startTime,
            enabled = pair.Key.gameObject.activeInHierarchy,
            localPosition = pair.Key.localPosition,
            localRotation = pair.Key.localRotation,
            localScale = pair.Key.localScale
          });
        }
      }

      using (new ProfilerSample("Discover Behaviours")) {
        foreach (var behaviour in _behaviours) {
          if (!_behaviourActivity.ContainsKey(behaviour)) {
            _behaviourActivity[behaviour] = new List<ActivityData>();
          }
        }
      }

      using (new ProfilerSample("Record Behaviour Activity Data")) {
        foreach (var pair in _behaviourActivity) {
          //Same logic as above, if this is the first frame for a spawned
          //object make sure to also record a disabled frame previously
          if (pair.Value.Count == 0 && Time.time > _startTime) {
            pair.Value.Add(new ActivityData() {
              time = Time.time - _startTime - Time.deltaTime,
              enabled = false
            });
          }

          pair.Value.Add(new ActivityData() {
            time = Time.time - _startTime,
            enabled = EditorUtility.GetObjectEnabled(pair.Key) == 1
          });
        }
      }
    }

    private struct ActivityData {
      public float time;
      public bool enabled;
    }

    private enum TransformDataType {
      Activity,
      Position,
      Rotation,
      Scale
    }

    private struct TransformData {
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
    }

    #region GUI

    private Vector2 _guiMargins = new Vector2(5F, 5F);
    private Vector2 _guiSize = new Vector2(175F, 60F);

    private void OnGUI() {
      var guiRect = new Rect(_guiMargins.x, _guiMargins.y, _guiSize.x, _guiSize.y);

      GUI.Box(guiRect, "");

      GUILayout.BeginArea(guiRect.PadInner(5F));
      GUILayout.BeginVertical();

      if (!_isRecording) {
        GUILayout.Label("Ready to record.");
        if (GUILayout.Button("Start Recording (" + beginRecordingKey.ToString() + ")",
                             GUILayout.ExpandHeight(true))) {
          beginRecording();
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

  }

}
