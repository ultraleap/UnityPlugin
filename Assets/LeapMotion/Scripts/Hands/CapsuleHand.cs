using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap;
using System;

public class CapsuleHand : IHandModel {

  private const int THUMB_BASE_INDEX = (int)Finger.FingerType.TYPE_THUMB * 4 + (int)Finger.FingerJoint.JOINT_MCP;
  private const int PINKY_BASE_INDEX = (int)Finger.FingerType.TYPE_PINKY * 4 + (int)Finger.FingerJoint.JOINT_MCP;

  private const float SPHERE_RADIUS = 0.008f;
  private const float CYLINDER_RADIUS = 0.006f;
  private const float PALM_RADIUS = 0.015f;

  private static int _colorIndex = 0;
  private static Color[] _colorList = { Color.blue, Color.green, Color.magenta, Color.cyan, Color.red, Color.yellow };

  [SerializeField]
  private bool _showArm = true;

  [SerializeField]
  private Material _material;

  private Material jointMat;

  private Transform[] _jointSpheres;
  private Transform mockThumbJointSphere;
  private Transform palmPositionSphere;

  private Transform wristPositionSphere;

  private List<Renderer> _armRenderers;
  private List<Transform> _capsuleTransforms;
  private List<Transform> _sphereATransforms;
  private List<Transform> _sphereBTransforms;

  private Transform armFrontLeft, armFrontRight, armBackLeft, armBackRight;
  private Hand hand_;

  public override ModelType HandModelType {
    get {
      return ModelType.Graphics;
    }
  }
  [SerializeField]
  private Chirality handedness;
  public override Chirality Handedness {
    get {
      return handedness;
    }
  }

  public override Hand GetLeapHand() {
    return hand_;
  }

  public override void SetLeapHand(Hand hand) {
    hand_ = hand;
  }

  void OnValidate() {
    //Update visibility on validate so that the user can toggle in real-time
    if (_armRenderers != null) {
      updateArmVisibility();
    }
  }

  public override void InitHand() {
    if (_material != null) {
      jointMat = new Material(_material);
      jointMat.hideFlags = HideFlags.DontSaveInEditor;
      jointMat.color = _colorList[_colorIndex];
      _colorIndex = (_colorIndex + 1) % _colorList.Length;
    }

    _jointSpheres = new Transform[4 * 5];
    _armRenderers = new List<Renderer>();
    _capsuleTransforms = new List<Transform>();
    _sphereATransforms = new List<Transform>();
    _sphereBTransforms = new List<Transform>();

    createSpheres();
    createCapsules();

    updateArmVisibility();
  }

  public override void UpdateHand() {
    //Update the spheres first
    updateSpheres();

    //Update Arm only if we need to
    if (_showArm) {
      updateArm();
    }

    //The capsule transforms are deterimined by the spheres they are connected to
    updateCapsules();
  }

  //Transform updating methods

  private void updateSpheres() {
    //Update all spheres
    FingerList fingers = hand_.Fingers;
    for (int i = 0; i < fingers.Count; i++) {
      Finger finger = fingers[i];
      for (int j = 0; j < 4; j++) {
        int key = getFingerJointIndex((int)finger.Type, j);
        Transform sphere = _jointSpheres[key];
        sphere.position = finger.JointPosition((Finger.FingerJoint)j).ToUnityScaled();

      }
    }

    palmPositionSphere.position = hand_.PalmPosition.ToUnity();

    Vector3 wristPos = hand_.PalmPosition.ToUnity();
    wristPositionSphere.position = wristPos;

    Transform thumbBase = _jointSpheres[THUMB_BASE_INDEX];

    Vector3 thumbBaseToPalm = thumbBase.position - hand_.PalmPosition.ToUnity();
    mockThumbJointSphere.position = hand_.PalmPosition.ToUnity() + Vector3.Reflect(thumbBaseToPalm, hand_.Basis.xBasis.ToUnity().normalized);
  }

  private void updateArm() {
    var arm = hand_.Arm;
    Vector3 right = arm.Basis.xBasis.ToUnity().normalized * arm.Width * 0.7f * 0.5f;
    Vector3 wrist = arm.WristPosition.ToUnityScaled();
    Vector3 elbow = arm.ElbowPosition.ToUnityScaled();

    float armLength = Vector3.Distance(wrist, elbow);
    wrist -= arm.Direction.ToUnity() * armLength * 0.05f;

    armFrontRight.position = wrist + right;
    armFrontLeft.position = wrist - right;
    armBackRight.position = elbow + right;
    armBackLeft.position = elbow - right;
  }

  private void updateCapsules() {
    for (int i = 0; i < _capsuleTransforms.Count; i++) {
      Transform capsule = _capsuleTransforms[i];
      Transform sphereA = _sphereATransforms[i];
      Transform sphereB = _sphereBTransforms[i];

      Vector3 delta = sphereA.position - sphereB.position;

      Vector3 scale = capsule.localScale;
      scale.x = CYLINDER_RADIUS * 2;
      scale.y = delta.magnitude * 0.5f / transform.lossyScale.x;
      scale.z = CYLINDER_RADIUS * 2;

      capsule.localScale = scale;

      capsule.position = (sphereA.position + sphereB.position) / 2;

      if (delta.sqrMagnitude <= Mathf.Epsilon) {
        //Two spheres are at the same location, no rotation will be found
        continue;
      }

      Vector3 perp;
      if (Vector3.Angle(delta, Vector3.up) > 170 || Vector3.Angle(delta, Vector3.up) < 10) {
        perp = Vector3.Cross(delta, Vector3.right);
      }
      else {
        perp = Vector3.Cross(delta, Vector3.up);
      }

      capsule.rotation = Quaternion.LookRotation(perp, delta);
    }
  }

  private void updateArmVisibility() {
    for (int i = 0; i < _armRenderers.Count; i++) {
      _armRenderers[i].enabled = _showArm;
    }
  }

  //Geometry creation methods

  private void createSpheres() {
    //Create spheres for finger joints
    FingerList fingers = hand_.Fingers;
    for (int i = 0; i < fingers.Count; i++) {
      Finger finger = fingers[i];
      for (int j = 0; j < 4; j++) {
        int key = getFingerJointIndex((int)finger.Type, j);
        _jointSpheres[key] = createSphere("Joint", SPHERE_RADIUS);
      }
    }

    mockThumbJointSphere = createSphere("MockJoint", SPHERE_RADIUS);
    palmPositionSphere = createSphere("PalmPosition", PALM_RADIUS);
    wristPositionSphere = createSphere("WristPosition", SPHERE_RADIUS);

    armFrontLeft = createSphere("ArmFrontLeft", SPHERE_RADIUS, true);
    armFrontRight = createSphere("ArmFrontRight", SPHERE_RADIUS, true);
    armBackLeft = createSphere("ArmBackLeft", SPHERE_RADIUS, true);
    armBackRight = createSphere("ArmBackRight", SPHERE_RADIUS, true);
  }

  private void createCapsules() {
    //Create capsules between finger joints
    for (int i = 0; i < 5; i++) {
      for (int j = 0; j < 3; j++) {
        int keyA = getFingerJointIndex(i, j);
        int keyB = getFingerJointIndex(i, j + 1);

        Transform sphereA = _jointSpheres[keyA];
        Transform sphereB = _jointSpheres[keyB];

        createCapsule("Finger Joint", sphereA, sphereB);
      }
    }

    //Create capsule between finger knuckles
    for (int i = 0; i < 4; i++) {
      int keyA = getFingerJointIndex(i, 0);
      int keyB = getFingerJointIndex(i + 1, 0);

      Transform sphereA = _jointSpheres[keyA];
      Transform sphereB = _jointSpheres[keyB];

      createCapsule("Hand Joints", sphereA, sphereB);
    }

    //Create the rest of the hand
    Transform thumbBase = _jointSpheres[THUMB_BASE_INDEX];
    Transform pinkyBase = _jointSpheres[PINKY_BASE_INDEX];
    createCapsule("Hand Bottom", thumbBase, mockThumbJointSphere);
    createCapsule("Hand Side", pinkyBase, mockThumbJointSphere);

    createCapsule("ArmFront", armFrontLeft, armFrontRight, true);
    createCapsule("ArmBack", armBackLeft, armBackRight, true);
    createCapsule("ArmLeft", armFrontLeft, armBackLeft, true);
    createCapsule("ArmRight", armFrontRight, armBackRight, true);
  }

  private int getFingerJointIndex(int fingerIndex, int jointIndex) {
    return fingerIndex * 4 + jointIndex;
  }

  private Transform createSphere(string name, float radius, bool isPartOfArm = false) {
    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    DestroyImmediate(sphere.GetComponent<Collider>());
    sphere.transform.parent = transform;
    sphere.transform.localScale = Vector3.one * radius * 2;
    sphere.GetComponent<Renderer>().sharedMaterial = jointMat;

    sphere.name = name;
    sphere.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy | HideFlags.HideInInspector;

    if (isPartOfArm) {
      _armRenderers.Add(sphere.GetComponent<Renderer>());
    }

    return sphere.transform;
  }

  private void createCapsule(string name, Transform jointA, Transform jointB, bool isPartOfArm = false) {
    GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
    DestroyImmediate(capsule.GetComponent<Collider>());
    capsule.name = name;
    capsule.transform.parent = transform;
    capsule.transform.localScale = Vector3.one * CYLINDER_RADIUS * 2;
    capsule.GetComponent<Renderer>().sharedMaterial = _material;

    _capsuleTransforms.Add(capsule.transform);
    _sphereATransforms.Add(jointA);
    _sphereBTransforms.Add(jointB);

    capsule.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy | HideFlags.HideInInspector;

    if (isPartOfArm) {
      _armRenderers.Add(capsule.GetComponent<Renderer>());
    }
  }
}
