using Leap.Unity.Attributes;
using Leap.Unity.Query;
using UnityEngine;

namespace Leap.Unity {

  /// <summary>
  /// Include this script on a GameObject in your scene to upload tracked hand
  /// data to shader globals for use in custom vertex or pixel shaders. If you
  /// are using HandData.cginc anywhere in your shaders, you'll need this script
  /// (or something like it) somewhere in your scene.
  /// </summary>
  [ExecuteInEditMode]
  public class HandShaderGlobalSupport : MonoBehaviour {

    public LeapProvider provider;

    private const int NUM_FINGERS = 5;

    public const string LEFT_FINGERTIPS_UNIFORM_NAME  = "_Leap_LH_Fingertips";
    public const string RIGHT_FINGERTIPS_UNIFORM_NAME = "_Leap_RH_Fingertips";

    public const string LEFT_PALM_TRIANGLES_UNIFORM_NAME  = "_Leap_LH_PalmTriangles";
    public const string RIGHT_PALM_TRIANGLES_UNIFORM_NAME = "_Leap_RH_PalmTriangles";

    public const string LEFT_FINGER_SEGMENTS_UNIFORM_NAME  = "_Leap_LH_FingerSegments";
    public const string RIGHT_FINGER_SEGMENTS_UNIFORM_NAME = "_Leap_RH_FingerSegments";

    [SerializeField, Disable]
    private int _leftFingertipsUniformParamId = 0;
    [SerializeField, Disable]
    private int _rightFingertipsUniformParamId = 0;

    [SerializeField, Disable]
    private int _leftPalmTrisUniformParamId = 0;
    [SerializeField, Disable]
    private int _rightPalmTrisUniformParamId = 0;

    [SerializeField, Disable]
    private int _leftFingerSegmentsUniformParamId = 0;
    [SerializeField, Disable]
    private int _rightFingerSegmentsUniformParamId = 0;

    private Vector4[] _leftFingertips = new Vector4[5];
    private Vector4[] _rightFingertips = new Vector4[5];

    private Vector4[] _leftPalmTris = new Vector4[15];
    private Vector4[] _rightPalmTris = new Vector4[15];

    private Vector4[] _leftFingerSegments = new Vector4[28];
    private Vector4[] _rightFingerSegments = new Vector4[28];

    private void OnValidate() {
      refreshParamIDs();

      uploadEditTimeFingerAtTransform();
    }

    private void Reset() {
      if (provider == null) provider = Hands.Provider;
    }

    private void Start() {
      if (provider == null) provider = Hands.Provider;

      refreshParamIDs();
    }

    private void refreshParamIDs() {
      _leftFingertipsUniformParamId = Shader.PropertyToID(LEFT_FINGERTIPS_UNIFORM_NAME);
      _rightFingertipsUniformParamId = Shader.PropertyToID(RIGHT_FINGERTIPS_UNIFORM_NAME);


      _leftPalmTrisUniformParamId = Shader.PropertyToID(LEFT_PALM_TRIANGLES_UNIFORM_NAME);
      _rightPalmTrisUniformParamId = Shader.PropertyToID(RIGHT_PALM_TRIANGLES_UNIFORM_NAME);


      _leftFingerSegmentsUniformParamId = Shader.PropertyToID(LEFT_FINGER_SEGMENTS_UNIFORM_NAME);
      _rightFingerSegmentsUniformParamId = Shader.PropertyToID(RIGHT_FINGER_SEGMENTS_UNIFORM_NAME);
    }

    private void uploadEditTimeFingerAtTransform() {
      _leftFingertips[0] = this.transform.position.WithW(1);
      _rightFingertips[0] = this.transform.position.WithW(1);
      Shader.SetGlobalVectorArray(_leftFingertipsUniformParamId, _leftFingertips);
      Shader.SetGlobalVectorArray(_rightFingertipsUniformParamId, _rightFingertips);
    }

    private void Update() {

      var leftHand = provider.CurrentFrame.Hands.Query()
                     .FirstOrDefault(h => h.IsLeft);
      if (leftHand != null) {

        // Fingertips
        for (int i = 0; i < NUM_FINGERS; i++) {
          _leftFingertips[i] = leftHand.Fingers[i].TipPosition.ToVector3().WithW(1);
        }

        // Palm Tris
        {
          var hand = leftHand;
          var palmTris = _leftPalmTris;
          int i = 0;

          // Left Hand Palm Tris
          {
            Finger thumb = hand.GetThumb(), index = hand.GetIndex(),
            middle = hand.GetMiddle(), ring = hand.GetRing(), pinky = hand.GetPinky();

            // Thumb Tri
            palmTris[i++] = thumb.bones[1].NextJoint.ToVector3().WithW(1);
            palmTris[i++] = thumb.bones[1].PrevJoint.ToVector3().WithW(1);
            palmTris[i++] = Vector3.Lerp(index.bones[0].PrevJoint.ToVector3(), index.bones[0].NextJoint.ToVector3(), 0.8f).WithW(1);

            // Index Tri
            palmTris[i++] = index.bones[0].NextJoint.ToVector3().WithW(1);
            palmTris[i++] = index.bones[0].PrevJoint.ToVector3().WithW(1);
            palmTris[i++] = pinky.bones[0].PrevJoint.ToVector3().WithW(1);

            // Middle Tri
            palmTris[i++] = middle.bones[0].NextJoint.ToVector3().WithW(1);
            palmTris[i++] = middle.bones[0].PrevJoint.ToVector3().WithW(1);
            palmTris[i++] = pinky.bones[0].PrevJoint.ToVector3().WithW(1);

            // Ring Tri
            palmTris[i++] = ring.bones[0].NextJoint.ToVector3().WithW(1);
            palmTris[i++] = ring.bones[0].PrevJoint.ToVector3().WithW(1);
            palmTris[i++] = pinky.bones[0].PrevJoint.ToVector3().WithW(1);

            // Pinky Tri
            palmTris[i++] = ring.bones[0].NextJoint.ToVector3().WithW(1);
            palmTris[i++] = ring.bones[0].PrevJoint.ToVector3().WithW(1);
            palmTris[i++] = pinky.bones[0].PrevJoint.ToVector3().WithW(1);
          }
        }

        // Finger Segments
        {
          var hand = leftHand;
          var segments = _leftFingerSegments;
          int i = 0;
          
          {
            Finger thumb = hand.GetThumb(), index = hand.GetIndex(),
              middle = hand.GetMiddle(), ring = hand.GetRing(), pinky = hand.GetPinky();

            // Thumb segments
            segments[i++] = thumb.bones[2].PrevJoint.ToVector3().WithW(1);
            segments[i++] = thumb.bones[2].NextJoint.ToVector3().WithW(1);
            segments[i++] = thumb.bones[3].PrevJoint.ToVector3().WithW(1);
            segments[i++] = thumb.bones[3].NextJoint.ToVector3().WithW(1);

            // Index segments
            segments[i++] = index.bones[1].PrevJoint.ToVector3().WithW(1);
            segments[i++] = index.bones[1].NextJoint.ToVector3().WithW(1);
            segments[i++] = index.bones[2].PrevJoint.ToVector3().WithW(1);
            segments[i++] = index.bones[2].NextJoint.ToVector3().WithW(1);
            segments[i++] = index.bones[3].PrevJoint.ToVector3().WithW(1);
            segments[i++] = index.bones[3].NextJoint.ToVector3().WithW(1);

            // Middle segments
            segments[i++] = middle.bones[1].PrevJoint.ToVector3().WithW(1);
            segments[i++] = middle.bones[1].NextJoint.ToVector3().WithW(1);
            segments[i++] = middle.bones[2].PrevJoint.ToVector3().WithW(1);
            segments[i++] = middle.bones[2].NextJoint.ToVector3().WithW(1);
            segments[i++] = middle.bones[3].PrevJoint.ToVector3().WithW(1);
            segments[i++] = middle.bones[3].NextJoint.ToVector3().WithW(1);

            // Ring segments
            segments[i++] = ring.bones[1].PrevJoint.ToVector3().WithW(1);
            segments[i++] = ring.bones[1].NextJoint.ToVector3().WithW(1);
            segments[i++] = ring.bones[2].PrevJoint.ToVector3().WithW(1);
            segments[i++] = ring.bones[2].NextJoint.ToVector3().WithW(1);
            segments[i++] = ring.bones[3].PrevJoint.ToVector3().WithW(1);
            segments[i++] = ring.bones[3].NextJoint.ToVector3().WithW(1);

            // Pinky segments
            segments[i++] = pinky.bones[1].PrevJoint.ToVector3().WithW(1);
            segments[i++] = pinky.bones[1].NextJoint.ToVector3().WithW(1);
            segments[i++] = pinky.bones[2].PrevJoint.ToVector3().WithW(1);
            segments[i++] = pinky.bones[2].NextJoint.ToVector3().WithW(1);
            segments[i++] = pinky.bones[3].PrevJoint.ToVector3().WithW(1);
            segments[i++] = pinky.bones[3].NextJoint.ToVector3().WithW(1);
          }
        }

      }
      else {
        for (int i = 0; i < _leftFingertips.Length; i++) {
          _leftFingertips[i] = (Vector3.one * 100000000f).WithW(1);
        }
        for (int i = 0; i < _leftPalmTris.Length; i++) {
          _leftPalmTris[i] = (Vector3.one * 100000000f).WithW(1);
        }
        for (int i = 0; i < _leftFingerSegments.Length; i++) {
          _leftFingerSegments[i] = (Vector3.one * 100000000f).WithW(1);
        }
      }
      Shader.SetGlobalVectorArray(_leftFingertipsUniformParamId, _leftFingertips);
      Shader.SetGlobalVectorArray(_leftPalmTrisUniformParamId, _leftPalmTris);
      Shader.SetGlobalVectorArray(_leftFingerSegmentsUniformParamId, _leftFingerSegments);

      var rightHand = provider.CurrentFrame.Hands.Query()
                      .FirstOrDefault(h => !h.IsLeft);
      if (rightHand != null) {

        // Fingertips
        for (int i = 0; i < NUM_FINGERS; i++) {
          _rightFingertips[i] = rightHand.Fingers[i].TipPosition.ToVector3().WithW(1);
        }

        // Palm Tris
        {
          var hand = rightHand;
          var palmTris = _rightPalmTris;
          int i = 0;
          
          {
            Finger thumb = hand.GetThumb(), index = hand.GetIndex(),
              middle = hand.GetMiddle(), ring = hand.GetRing(), pinky = hand.GetPinky();

            // Thumb Tri
            palmTris[i++] = thumb.bones[1].NextJoint.ToVector3().WithW(1);
            palmTris[i++] = thumb.bones[1].PrevJoint.ToVector3().WithW(1);
            palmTris[i++] = Vector3.Lerp((index.bones[0].PrevJoint.ToVector3() + hand.RadialAxis() * 0.000f - hand.DistalAxis() * 0.015f), index.bones[0].NextJoint.ToVector3(), 0.8f).WithW(1);

            // Index Tri
            palmTris[i++] = index.bones[0].NextJoint.ToVector3().WithW(1);
            palmTris[i++] = (index.bones[0].PrevJoint.ToVector3() + hand.RadialAxis() * 0.000f - hand.DistalAxis() * 0.015f).WithW(1);
            palmTris[i++] = (pinky.bones[0].PrevJoint.ToVector3() - hand.RadialAxis() * 0.000f - hand.DistalAxis() * 0.015f).WithW(1);

            // Middle Tri
            palmTris[i++] = middle.bones[0].NextJoint.ToVector3().WithW(1);
            palmTris[i++] = index.bones[0].NextJoint.ToVector3().WithW(1);
            palmTris[i++] = (pinky.bones[0].PrevJoint.ToVector3() - hand.RadialAxis() * 0.000f - hand.DistalAxis() * 0.015f).WithW(1);

            // Ring Tri
            palmTris[i++] = ring.bones[0].NextJoint.ToVector3().WithW(1);
            palmTris[i++] = middle.bones[0].NextJoint.ToVector3().WithW(1);
            palmTris[i++] = (pinky.bones[0].PrevJoint.ToVector3() - hand.RadialAxis() * 0.000f - hand.DistalAxis() * 0.015f).WithW(1);

            // Pinky Tri
            palmTris[i++] = pinky.bones[0].NextJoint.ToVector3().WithW(1);
            palmTris[i++] = ring.bones[0].NextJoint.ToVector3().WithW(1);
            palmTris[i++] = (pinky.bones[0].PrevJoint.ToVector3() - hand.RadialAxis() * 0.000f - hand.DistalAxis() * 0.015f).WithW(1f);
          }
        }

        // Finger Segments
        {
          var hand = rightHand;
          var segments = _rightFingerSegments;
          int i = 0;

          {
            Finger thumb = hand.GetThumb(), index = hand.GetIndex(),
              middle = hand.GetMiddle(), ring = hand.GetRing(), pinky = hand.GetPinky();

            // Thumb segments
            segments[i++] = thumb.bones[2].PrevJoint.ToVector3().WithW(1);
            segments[i++] = thumb.bones[2].NextJoint.ToVector3().WithW(1);
            segments[i++] = thumb.bones[3].PrevJoint.ToVector3().WithW(1);
            segments[i++] = thumb.bones[3].NextJoint.ToVector3().WithW(1);

            // Index segments
            segments[i++] = index.bones[1].PrevJoint.ToVector3().WithW(1);
            segments[i++] = index.bones[1].NextJoint.ToVector3().WithW(1);
            segments[i++] = index.bones[2].PrevJoint.ToVector3().WithW(1);
            segments[i++] = index.bones[2].NextJoint.ToVector3().WithW(1);
            segments[i++] = index.bones[3].PrevJoint.ToVector3().WithW(1);
            segments[i++] = index.bones[3].NextJoint.ToVector3().WithW(1);

            // Middle segments
            segments[i++] = middle.bones[1].PrevJoint.ToVector3().WithW(1);
            segments[i++] = middle.bones[1].NextJoint.ToVector3().WithW(1);
            segments[i++] = middle.bones[2].PrevJoint.ToVector3().WithW(1);
            segments[i++] = middle.bones[2].NextJoint.ToVector3().WithW(1);
            segments[i++] = middle.bones[3].PrevJoint.ToVector3().WithW(1);
            segments[i++] = middle.bones[3].NextJoint.ToVector3().WithW(1);

            // Ring segments
            segments[i++] = ring.bones[1].PrevJoint.ToVector3().WithW(1);
            segments[i++] = ring.bones[1].NextJoint.ToVector3().WithW(1);
            segments[i++] = ring.bones[2].PrevJoint.ToVector3().WithW(1);
            segments[i++] = ring.bones[2].NextJoint.ToVector3().WithW(1);
            segments[i++] = ring.bones[3].PrevJoint.ToVector3().WithW(1);
            segments[i++] = ring.bones[3].NextJoint.ToVector3().WithW(1);

            // Pinky segments
            segments[i++] = pinky.bones[1].PrevJoint.ToVector3().WithW(1);
            segments[i++] = pinky.bones[1].NextJoint.ToVector3().WithW(1);
            segments[i++] = pinky.bones[2].PrevJoint.ToVector3().WithW(1);
            segments[i++] = pinky.bones[2].NextJoint.ToVector3().WithW(1);
            segments[i++] = pinky.bones[3].PrevJoint.ToVector3().WithW(1);
            segments[i++] = pinky.bones[3].NextJoint.ToVector3().WithW(1);
          }
        }

      }
      else {
        for (int i = 0; i < NUM_FINGERS; i++) {
          _rightFingertips[i] = (Vector3.one * 100000000f).WithW(1);
        }
        for (int i = 0; i < _rightPalmTris.Length; i++) {
          _rightPalmTris[i] = (Vector3.one * 100000000f).WithW(1);
        }
        for (int i = 0; i < _rightFingerSegments.Length; i++) {
          _rightFingerSegments[i] = (Vector3.one * 100000000f).WithW(1);
        }
      }
      Shader.SetGlobalVectorArray(_rightFingertipsUniformParamId, _rightFingertips);
      Shader.SetGlobalVectorArray(_rightPalmTrisUniformParamId, _rightPalmTris);
      Shader.SetGlobalVectorArray(_rightFingerSegmentsUniformParamId, _rightFingerSegments);

      if (!Application.isPlaying) {
        uploadEditTimeFingerAtTransform();
      }
    }

  }

}
