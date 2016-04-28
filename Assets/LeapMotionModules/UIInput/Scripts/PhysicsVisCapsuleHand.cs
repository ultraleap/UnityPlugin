using UnityEngine;
using System.Collections.Generic;

namespace Leap.Unity {
  /** A basic Leap hand model constructed dynamically vs. using pre-existing geometry*/
  public class PhysicsVisCapsuleHand : IHandModel {

    private const int THUMB_BASE_INDEX = (int)Finger.FingerType.TYPE_THUMB * 4;
    private const int PINKY_BASE_INDEX = (int)Finger.FingerType.TYPE_PINKY * 4;

    private float SPHERE_RADIUS = 0.008f;
    private float CYLINDER_RADIUS = 0.008f;
    private float PALM_RADIUS = 0.015f;

    private static int _leftColorIndex = 0;
    private static int _rightColorIndex = 0;
    private static Color[] _leftColorList = { new Color(0.0f, 0.0f, 1.0f), new Color(0.2f, 0.0f, 0.4f), new Color(0.0f, 0.2f, 0.2f) };
    private static Color[] _rightColorList = { new Color(1.0f, 0.0f, 0.0f), new Color(1.0f, 1.0f, 0.0f), new Color(1.0f, 0.5f, 0.0f) };

    [SerializeField]
    private bool _showArm = true;

    [SerializeField]
    private Material _material;

    [SerializeField]
    private Mesh _sphereMesh;

    [SerializeField]
    private int _cylinderResolution = 10;

    //private Material jointMat;

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
    public override Chirality Handedness
    {
        get
        {
            return handedness;
        }
    }

    [SerializeField]
    public SkeletalHand HandToVisualize;

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
      /*
      if (_material != null) {
        jointMat = new Material(_material);
        jointMat.hideFlags = HideFlags.DontSaveInEditor;
      }
      */
       // handedness = HandToVisualize.GetLeapHand().IsRight ? Chirality.Right : Chirality.Left;

      _jointSpheres = new Transform[4 * 5];
      _armRenderers = new List<Renderer>();
      _capsuleTransforms = new List<Transform>();
      _sphereATransforms = new List<Transform>();
      _sphereBTransforms = new List<Transform>();

      CYLINDER_RADIUS *= transform.lossyScale.x;
      //SPHERE_RADIUS *= transform.lossyScale.x;
      //PALM_RADIUS *= transform.lossyScale.x;

      createSpheres();
      createCylinders();

      updateArmVisibility();
    }

    public override void BeginHand() {
      base.BeginHand();

      if (hand_.IsLeft) {
        //jointMat.color = _leftColorList[_leftColorIndex];
        _leftColorIndex = (_leftColorIndex + 1) % _leftColorList.Length;
      } else {
        //jointMat.color = _rightColorList[_rightColorIndex];
        _rightColorIndex = (_rightColorIndex + 1) % _rightColorList.Length;
      }
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
      for (int i = 0; i < HandToVisualize.fingers.Length; i++) {
          for (int j = 0; j < 4; j++) {
              if(j==0){
                  int key = getFingerJointIndex((int)HandToVisualize.fingers[i].fingerType, j);
                  Transform sphere = _jointSpheres[key];
                  SkeletalFinger finger = (SkeletalFinger)HandToVisualize.fingers[i];
                  sphere.position = finger.bones[1].position - (finger.bones[1].forward*(finger.GetBoneLength(1)/2f));
              }else{
                  int key = getFingerJointIndex((int)HandToVisualize.fingers[i].fingerType, j);
                  Transform sphere = _jointSpheres[key];
                  SkeletalFinger finger = (SkeletalFinger)HandToVisualize.fingers[i];
                  sphere.position = finger.bones[j].position + (finger.bones[j].forward*(finger.GetBoneLength(j)/2f));
              }
          }
      }

      palmPositionSphere.position = HandToVisualize.palm.position;

      Vector3 wristPos = HandToVisualize.palm.position;
      wristPositionSphere.position = wristPos;

      Transform thumbBase = _jointSpheres[THUMB_BASE_INDEX];

      Vector3 thumbBaseToPalm = thumbBase.position - HandToVisualize.palm.position;
      mockThumbJointSphere.position = HandToVisualize.palm.position + Vector3.Reflect(thumbBaseToPalm, HandToVisualize.palm.right);
    }

    private void updateArm() {
      var arm = hand_.Arm;
      Vector3 right = arm.Basis.xBasis.ToVector3().normalized * arm.Width * 0.7f * 0.5f;
      Vector3 wrist = arm.WristPosition.ToVector3();
      Vector3 elbow = arm.ElbowPosition.ToVector3();

      float armLength = Vector3.Distance(wrist, elbow);
      wrist -= arm.Direction.ToVector3() * armLength * 0.05f;

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

        MeshFilter filter = capsule.GetComponent<MeshFilter>();
        if (filter.sharedMesh == null) {
          filter.sharedMesh = generateCylinderMesh(delta.magnitude / transform.lossyScale.x);
        }

        capsule.position = sphereA.position;

        if (delta.sqrMagnitude <= Mathf.Epsilon) {
          //Two spheres are at the same location, no rotation will be found
          continue;
        }

        Vector3 perp;
        if (Vector3.Angle(delta, Vector3.up) > 170 || Vector3.Angle(delta, Vector3.up) < 10) {
          perp = Vector3.Cross(delta, Vector3.right);
        } else {
          perp = Vector3.Cross(delta, Vector3.up);
        }

        capsule.rotation = Quaternion.LookRotation(perp, delta);
        capsule.LookAt(sphereB);
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
      List<Finger> fingers = hand_.Fingers;
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

    private void createCylinders() {
      //Create cylinders between finger joints
      for (int i = 0; i < 5; i++) {
        for (int j = 0; j < 3; j++) {
          int keyA = getFingerJointIndex(i, j);
          int keyB = getFingerJointIndex(i, j + 1);

          Transform sphereA = _jointSpheres[keyA];
          Transform sphereB = _jointSpheres[keyB];

          createCylinder("Finger Joint", sphereA, sphereB);
        }
      }

      //Create cylinder between finger knuckles
      for (int i = 0; i < 4; i++) {
        int keyA = getFingerJointIndex(i, 0);
        int keyB = getFingerJointIndex(i + 1, 0);

        Transform sphereA = _jointSpheres[keyA];
        Transform sphereB = _jointSpheres[keyB];

        createCylinder("Hand Joints", sphereA, sphereB);
      }

      //Create the rest of the hand
      Transform thumbBase = _jointSpheres[THUMB_BASE_INDEX];
      Transform pinkyBase = _jointSpheres[PINKY_BASE_INDEX];
      createCylinder("Hand Bottom", thumbBase, mockThumbJointSphere);
      createCylinder("Hand Side", pinkyBase, mockThumbJointSphere);

      createCylinder("ArmFront", armFrontLeft, armFrontRight, true);
      createCylinder("ArmBack", armBackLeft, armBackRight, true);
      createCylinder("ArmLeft", armFrontLeft, armBackLeft, true);
      createCylinder("ArmRight", armFrontRight, armBackRight, true);
    }

    private int getFingerJointIndex(int fingerIndex, int jointIndex) {
      return fingerIndex * 4 + jointIndex;
    }

    private Transform createSphere(string name, float radius, bool isPartOfArm = false) {
      GameObject sphere = new GameObject(name);
      sphere.AddComponent<MeshFilter>().mesh = _sphereMesh;
      sphere.AddComponent<MeshRenderer>().sharedMaterial = _material;
      sphere.transform.parent = transform;
      sphere.transform.localScale = Vector3.one * radius * 2;

      sphere.hideFlags = HideFlags.DontSave;

      if (isPartOfArm) {
        _armRenderers.Add(sphere.GetComponent<Renderer>());
      }

      return sphere.transform;
    }

    private void createCylinder(string name, Transform jointA, Transform jointB, bool isPartOfArm = false) {
      GameObject cylinder = new GameObject(name);
      cylinder.AddComponent<MeshFilter>();
      cylinder.AddComponent<MeshRenderer>().sharedMaterial = _material;
      cylinder.transform.parent = transform;

      _capsuleTransforms.Add(cylinder.transform);
      _sphereATransforms.Add(jointA);
      _sphereBTransforms.Add(jointB);

      cylinder.hideFlags = HideFlags.DontSave;

      if (isPartOfArm) {
        _armRenderers.Add(cylinder.GetComponent<Renderer>());
      }
    }

    private Mesh generateCylinderMesh(float length) {
      Mesh mesh = new Mesh();
      mesh.name = "GeneratedCylinder";

      List<Vector3> verts = new List<Vector3>();
      List<Color> colors = new List<Color>();
      List<int> tris = new List<int>();

      Vector3 p0 = Vector3.zero;
      Vector3 p1 = Vector3.forward * length * transform.lossyScale.x;
      for (int i = 0; i < _cylinderResolution; i++) {
        float angle = (Mathf.PI * 2.0f * i) / _cylinderResolution;
        float dx = CYLINDER_RADIUS * Mathf.Cos(angle);
        float dy = CYLINDER_RADIUS * Mathf.Sin(angle);

        Vector3 spoke = new Vector3(dx, dy, 0);

        verts.Add(p0 + spoke);
        verts.Add(p1 + spoke);

        colors.Add(Color.white);
        colors.Add(Color.white);

        int triStart = verts.Count;
        int triCap = _cylinderResolution * 2;

        tris.Add((triStart + 0) % triCap);
        tris.Add((triStart + 2) % triCap);
        tris.Add((triStart + 1) % triCap);
        //
        tris.Add((triStart + 2) % triCap);
        tris.Add((triStart + 3) % triCap);
        tris.Add((triStart + 1) % triCap);
      }

      /*
      int pv0 = verts.Count;
      verts.Add(p0);
      colors.Add(Color.white);
      int pv1 = verts.Count;
      verts.Add(p1);
      colors.Add(Color.white);

      for (int i = 0; i < _cylinderResolution; i++) {
        int a0 = i * 2;
        int a1 = 2 * ((i + 1) % _cylinderResolution);

        int b0 = a0 + 1;
        int b1 = a1 + 1;

        //tris.Add(pv0);
        //tris.Add(a1);
        //tris.Add(a0);

        tris.Add(pv1);
        tris.Add(b0);
        tris.Add(b1);
      }
      */

      mesh.SetVertices(verts);
      //mesh.SetColors(colors);
      mesh.SetIndices(tris.ToArray(), MeshTopology.Triangles, 0);
      mesh.RecalculateBounds();
      mesh.RecalculateNormals();
      mesh.Optimize();
      mesh.UploadMeshData(true);

      return mesh;
    }
  }
}
