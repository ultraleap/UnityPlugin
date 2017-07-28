using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity.Query;

namespace Leap.Unity.Recording {

  public class HierarchyRecorder : MonoBehaviour {
    public static Action OnPreRecordFrame;

    public KeyCode beginRecordingKey = KeyCode.F5;
    public KeyCode finishRecordingKey = KeyCode.F6;

    private AnimationClip _clip;

    private List<PropertyRecorder> _recorders;
    private List<Transform> _transforms;

    private List<AudioSource> _audioSources;
    private Dictionary<AudioSource, RecordedAudio> _audioData;

    private Dictionary<EditorCurveBinding, AnimationCurve> _curves;
    private Dictionary<Transform, List<TransformData>> _transformData;

    private bool _isRecording = false;
    private float _startTime = 0;

    private void LateUpdate() {
      if (Input.GetKeyDown(beginRecordingKey)) {
        beginRecording();
      }

      if (Input.GetKeyDown(finishRecordingKey)) {
        finishRecording();
      }

      if (_isRecording) {
        recordData();
      }
    }

    private void beginRecording() {
      if (_isRecording) return;
      _isRecording = true;
      _startTime = Time.time;

      foreach (var transform in GetComponentsInChildren<Transform>(true)) {
        HashSet<string> takenNames = new HashSet<string>();
        for (int i = 0; i < transform.childCount; i++) {
          Transform child = transform.GetChild(i);
          if (takenNames.Contains(child.name)) {
            child.name = child.name + " " + Mathf.Abs(child.GetInstanceID());
          }
          takenNames.Add(child.name);
        }
      }

      _recorders = new List<PropertyRecorder>();
      _transforms = new List<Transform>();
      _audioSources = new List<AudioSource>();
      _audioData = new Dictionary<AudioSource, RecordedAudio>();
      _curves = new Dictionary<EditorCurveBinding, AnimationCurve>();
      _transformData = new Dictionary<Transform, List<TransformData>>();
    }

    private void finishRecording() {
      if (!_isRecording) return;
      _isRecording = false;

      GetComponentsInChildren(true, _recorders);
      foreach (var recorder in _recorders) {
        DestroyImmediate(recorder);
      }

      try {

        //Patch up renderer references to materials
        var allMaterials = Resources.FindObjectsOfTypeAll<Material>().
                                     Query().
                                     Where(AssetDatabase.IsMainAsset).
                                     ToList();
        foreach (var renderer in GetComponentsInChildren<Renderer>(includeInactive: true)) {
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

        int index;
        index = 0;
        foreach (var pair in _transformData) {
          var transform = pair.Key;
          var transformData = pair.Value;

          index++;
          if (EditorUtility.DisplayCancelableProgressBar("Saving Recording",
                                                         "Converting Transform Data: " + pair.Key.name,
                                                         index / (float)_transformData.Count)) {
            return;
          }

          string path = AnimationUtility.CalculateTransformPath(pair.Key, transform);
          Type type = typeof(Transform);


          bool isActivityConstant = true;
          bool isPositionConstant = true;
          bool isRotationConstant = true;
          bool isScaleConstant = true;

          {
            bool startEnabled = transformData[0].enabled;
            Vector3 startPosition = transformData[0].localPosition;
            Quaternion startRotation = transformData[0].localRotation;
            Vector3 startScale = transformData[0].localScale;
            for (int i = 1; i < transformData.Count; i++) {
              isActivityConstant &= transformData[i].enabled == startEnabled;
              isPositionConstant &= transformData[i].localPosition == startPosition;
              isRotationConstant &= transformData[i].localRotation == startRotation;
              isScaleConstant &= transformData[i].localScale == startScale;
            }
          }

          for (int i = 0; i < TransformData.CURVE_COUNT; i++) {
            string propertyName = TransformData.GetName(i);
            AnimationCurve curve = new AnimationCurve();

            switch (TransformData.GetDataType(i)) {
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
                break;
            }

            for (int j = 0; j < pair.Value.Count; j++) {
              curve.AddKey(pair.Value[j].time, pair.Value[j].GetFloat(i));
            }

            var binding = EditorCurveBinding.FloatCurve(path, type, propertyName);

            if (_curves.ContainsKey(binding)) {
              Debug.LogError("Duplicate object was created??");
              Debug.LogError("Named " + pair.Key.name + " : " + binding.propertyName);
            } else {
              _curves.Add(binding, curve);
            }
          }
        }

        index = 0;
        foreach (var pair in _curves) {
          index++;
          if (EditorUtility.DisplayCancelableProgressBar("Saving Recording",
                                                         "Compressing Data:\n" + pair.Key.path,
                                                         index / (float)_curves.Count)) {
            return;
          }

          EditorCurveBinding binding = pair.Key;
          AnimationCurve curve = pair.Value;

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
      } finally {
        EditorUtility.ClearProgressBar();
      }

      gameObject.AddComponent<HierarchyPostProcess>();

      GameObject myGameObject = gameObject;

      DestroyImmediate(this);

      PrefabUtility.CreatePrefab("Assets/LeapMotion/Modules/HierarchyRecording/RawRecording.prefab", myGameObject);
    }

    private void recordData() {
      if (OnPreRecordFrame != null) {
        OnPreRecordFrame();
      }

      using (new ProfilerSample("Get Components")) {
        GetComponentsInChildren(true, _recorders);
        GetComponentsInChildren(true, _transforms);
        GetComponentsInChildren(true, _audioSources);
      }

      using (new ProfilerSample("Update Audio Sources")) {
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

      using (new ProfilerSample("Record Properties")) {
        //Record all properties specified by recorders
        foreach (var recorder in _recorders) {
          foreach (var bindings in recorder.GetBindings(gameObject)) {
            if (!_curves.ContainsKey(bindings)) {
              _curves[bindings] = new AnimationCurve();
            }
          }
        }
      }

      using (new ProfilerSample("Sample Custom Data")) {
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

      using (new ProfilerSample("Update Transform List")) {
        //Record ALL transform and gameObject data, no matter what
        foreach (var transform in _transforms) {
          if (!_transformData.ContainsKey(transform)) {
            _transformData[transform] = new List<TransformData>();
          }
        }
      }

      using (new ProfilerSample("Record Transform Data")) {
        foreach (var pair in _transformData) {
          pair.Value.Add(new TransformData() {
            time = Time.time - _startTime,
            enabled = pair.Key.gameObject.activeInHierarchy,
            localPosition = pair.Key.localPosition,
            localRotation = pair.Key.localRotation,
            localScale = pair.Key.localScale
          });
        }
      }
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
    private Vector2 _guiSize    = new Vector2(175F, 60F);

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
      }
      else {
        GUILayout.Label("Recording.");
        if (GUILayout.Button("Stop Recording (" + finishRecordingKey.ToString() + ")",
                             GUILayout.ExpandHeight(true))) {
          finishRecording();
        }
      }

      GUILayout.EndVertical();
      GUILayout.EndArea();
    }

    #endregion

  }

}
