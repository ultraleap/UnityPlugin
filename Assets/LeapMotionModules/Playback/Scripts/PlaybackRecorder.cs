using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.IO;

namespace Leap.Unity.Playback {

  public class PlaybackRecorder : MonoBehaviour {

    public enum RecordTime {
      Update,
      FixedUpdate
    }

    public enum SaveType {
      None,
      UnityAsset
    }

    [SerializeField]
    protected LeapProvider _provider;

    [SerializeField]
    protected RecordTime _recordTime = RecordTime.Update;

    [Header("Editor Settings")]
    [SerializeField]
    protected KeyCode _startRecording = KeyCode.F5;

    [SerializeField]
    protected KeyCode _endRecording = KeyCode.F6;

    [SerializeField]
    protected SaveType _saveType = SaveType.None;

    [SerializeField]
    protected string _unityAssetSavePath = "Assets/Recording";

    protected Recording _currentRecording;

    public virtual  void StartRecording() {
      _currentRecording = ScriptableObject.CreateInstance<Recording>();
    }

    public virtual Recording EndRecording() {
      Recording finishedRecording = _currentRecording;
      _currentRecording = null;

      switch (_saveType) {
        case SaveType.None:
          break;
        case SaveType.UnityAsset:
#if UNITY_EDITOR
          Directory.CreateDirectory(_unityAssetSavePath);
          string path = AssetDatabase.GenerateUniqueAssetPath(_unityAssetSavePath + ".asset");
          AssetDatabase.CreateAsset(finishedRecording, path);
          AssetDatabase.SaveAssets();
#else
              throw new Exception("Cannot save unity assets outside of Unity Editor");
#endif
          break;
        default:
          break;
      }

      return finishedRecording;
    }

    protected virtual void Update() {
      if (_currentRecording != null) {
        if (_recordTime == RecordTime.Update) {
          Frame frame = _provider.CurrentFrame;
          if (frame != null) {
            _currentRecording.frames.Add(frame);
          }
        }

        if (Input.GetKeyDown(_endRecording)) {
          EndRecording();
        }
      } else {
        if (Input.GetKeyDown(_startRecording)) {
          StartRecording();
        }
      }
    }

    protected virtual void FixedUpdate() {
      if (_currentRecording != null && _recordTime == RecordTime.FixedUpdate) {
        Frame frame = _provider.CurrentFixedFrame;
        if (frame != null) {
          _currentRecording.frames.Add(frame);
        }
      }
    }
  }
}
