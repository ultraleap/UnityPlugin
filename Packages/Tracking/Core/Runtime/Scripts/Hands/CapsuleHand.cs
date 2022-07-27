/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Attributes;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity
{
#pragma warning disable 0618
    /// <summary>
    /// The CapsuleHand is a basic Leap hand model that generates a set of spheres and 
    /// cylinders to render hands using Leap hand data.
    /// It is constructed dynamically rather than using pre-existing geometry which
    /// allows hand visuals to scale to the size of the users hand and 
    /// is a reliable way to visualize the raw tracking data.
    /// </summary>
    public class CapsuleHand : HandModelBase
    {
        private const int TOTAL_JOINT_COUNT = 4 * 5;
        private const float CYLINDER_MESH_RESOLUTION = 0.1f; //in centimeters, meshes within this resolution will be re-used
        private const int THUMB_BASE_INDEX = (int)Finger.FingerType.TYPE_THUMB * 4;
        private const int PINKY_BASE_INDEX = (int)Finger.FingerType.TYPE_PINKY * 4;

        private static int _leftColorIndex = 0;
        private static int _rightColorIndex = 0;
        private static Color[] _leftColorList = { new Color(0.0f, 0.0f, 1.0f), new Color(0.2f, 0.0f, 0.4f), new Color(0.0f, 0.2f, 0.2f) };
        private static Color[] _rightColorList = { new Color(1.0f, 0.0f, 0.0f), new Color(1.0f, 1.0f, 0.0f), new Color(1.0f, 0.5f, 0.0f) };

#pragma warning disable 0649
        [SerializeField]
        private Chirality handedness;

        [SerializeField]
        private bool _showArm = true;

        [SerializeField]
        private bool _showUpperArm = true;

        [SerializeField]
        private bool _showUpperBody = false;

        [SerializeField]
        private bool _castShadows = true;

        [SerializeField]
        private Material _material;
        private Material _backing_material;

        [SerializeField]
        private Mesh _sphereMesh;

        private Mesh _cylinderMesh;

        [MinValue(3)]
        [SerializeField]
        private int _cylinderResolution = 12;

        [MinValue(0)]
        [SerializeField]
        private float _jointRadius = 0.008f;

        [MinValue(0)]
        [SerializeField]
        private float _cylinderRadius = 0.006f;

        [MinValue(0)]
        [SerializeField]
        private float _palmRadius = 0.015f;

        [SerializeField]
        private bool _useCustomColors = false;

        [SerializeField]
        [DisableIf("_useCustomColors", isEqualTo: false)]
        private Color _sphereColor = Color.green, _cylinderColor = Color.white;
#pragma warning restore 0649

        private Material _sphereMat;
        private Hand _hand;
        private Vector3[] _spherePositions;
        private Matrix4x4[] _sphereMatrices = new Matrix4x4[64],
                            _cylinderMatrices = new Matrix4x4[64];
        private int _curSphereIndex = 0, _curCylinderIndex = 0;
        private Color _backingDefault = Color.white;

        private MaterialPropertyBlock _materialPropertyBlock;
        private Color[] _sphereColors;

        [HideInInspector]
        public bool SetIndividualSphereColors = false;
        public Color[] SphereColors
        {
            get
            {
                if (_sphereColors == null)
                {
                    _sphereColors = new Color[32];
                    Utils.Fill(_sphereColors, SphereColour);
                }
                return _sphereColors;
            }
            set
            {
                _sphereColors = value;
            }
        }

        /// <summary>
        /// The type of the Hand model (set to Graphics)
        /// </summary>
        public override ModelType HandModelType
        {
            get
            {
                return ModelType.Graphics;
            }
        }

        /// <summary>
        /// The chirality of handedness of this hand.
        /// This can be set in the inspector.
        /// </summary>
        public override Chirality Handedness
        {
            get
            {
                return handedness;
            }
            set { }
        }

        /// <summary>
        /// Returns whether or not this hand model supports editor persistence. 
        /// (set to true for the CapsuleHand)
        /// </summary>
        public override bool SupportsEditorPersistence()
        {
            return true;
        }

        /// <summary>
        /// Returns the Leap Hand object represented by this HandModelBase. 
        /// Note that any physical quantities and directions obtained from the Leap Hand object are 
        /// relative to the Leap Motion coordinate system, which uses a right-handed axes and units 
        /// of millimeters.
        /// </summary>
        /// <returns></returns>
        public override Hand GetLeapHand()
        {
            return _hand;
        }

        /// <summary>
        /// Assigns a Leap Hand object to this HandModelBase.
        /// </summary>
        /// <param name="hand"></param>
        public override void SetLeapHand(Hand hand)
        {
            _hand = hand;
        }

        /// <summary>
        /// This function is called when a new hand is detected by the Leap Motion device.
        /// Materials and Colors for the hand are set up.
        /// </summary>
        public override void InitHand()
        {
            if (_material != null && (_backing_material == null || !_backing_material.enableInstancing))
            {
                _backing_material = new Material(_material);
                _backing_material.hideFlags = HideFlags.DontSaveInEditor;
                _backingDefault = _backing_material.color;
                if (!Application.isEditor && !_backing_material.enableInstancing)
                {
                    Debug.LogError("Capsule Hand Material needs Instancing Enabled to render in builds!", this);
                }
                _backing_material.enableInstancing = true;
                _sphereMat = new Material(_backing_material);
                _sphereMat.hideFlags = HideFlags.DontSaveInEditor;
                if (_useCustomColors)
                {
                    _sphereMat.color = _sphereColor;
                    _backing_material.color = _cylinderColor;
                }
            }
        }

        /// <summary>
        /// The colour of the Capsule Hand's spheres.
        /// </summary>
        public Color SphereColour
        {
            get
            {
                if (_sphereMat == null) return _sphereColor;
                return _sphereMat.color;
            }
        }

        /// <summary>
        /// The colour of the Capsule Hand's cylinders.
        /// </summary>
        public Color CylinderColour
        {
            get
            {
                return _backing_material.color;
            }
        }

        /// <summary>
        /// In-code alternative to the editor properties for setting custom sphere/cylinder colours.
        /// </summary>
        /// <param name="sphere"></param>
        /// <param name="cylinder"></param>
        public void SetCustomColours(Color sphere, Color cylinder)
        {
            _useCustomColors = true;
            _sphereColor = sphere;
            _cylinderColor = cylinder;
            _sphereMat.color = _sphereColor;
            _backing_material.color = _cylinderColor;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _meshMap.Clear();
            if (_material == null || !_material.enableInstancing)
            {
                Debug.LogWarning("CapsuleHand's Material must have " +
                  "instancing enabled in order to work in builds! Replacing " +
                  "Material with a Default Material now...", this);
                _material = (Material)Resources.Load("InstancedCapsuleHand", typeof(Material));
            }
            if (_material != null && _backing_material != null && _sphereMat != null)
            {
                if(_useCustomColors && (_sphereMat.color != _sphereColor || _backing_material.color != _cylinderColor))
                {
                    _sphereMat.color = _sphereColor;
                    _backing_material.color = _cylinderColor;
                    if (!Application.isPlaying)
                    {
                        UpdateHand();
                    }
                }
                else if (!_useCustomColors
                    && (_sphereMat.color != (_hand.IsLeft ? _leftColorList[_leftColorIndex] : _rightColorList[_rightColorIndex])
                    || _backing_material.color != _backingDefault))
                {
                    _sphereMat.color = _hand.IsLeft ? _leftColorList[_leftColorIndex] : _rightColorList[_rightColorIndex];
                    _backing_material.color = _backingDefault;
                    if (!Application.isPlaying)
                    {
                        UpdateHand();
                    }
                }
            }
        }
#endif

        /// <summary>
        /// Called after the hand is initialised. 
        /// Calls the event OnBegin and sets isTracked to true.
        /// Assigns colors to materials for the hand.
        /// </summary>
        public override void BeginHand()
        {
            base.BeginHand();

            if (_useCustomColors)
            {
                _sphereMat.color = _sphereColor;
                _backing_material.color = _cylinderColor;
            }
            else
            {
                if (_hand.IsLeft)
                {
                    _sphereMat.color = _leftColorList[_leftColorIndex];
                    _leftColorIndex = (_leftColorIndex + 1) % _leftColorList.Length;
                }
                else
                {
                    _sphereMat.color = _rightColorList[_rightColorIndex];
                    _rightColorIndex = (_rightColorIndex + 1) % _rightColorList.Length;
                }
            }

        }

        /// <summary>
        /// Called once per frame when the LeapProvider calls the event OnUpdateFrame.
        /// Updates all joint sphere positions and draws the spheres and cylinders of the hand.
        /// </summary>
        public override void UpdateHand()
        {
            _curSphereIndex = 0;
            _curCylinderIndex = 0;

            if (_spherePositions == null || _spherePositions.Length != TOTAL_JOINT_COUNT)
            {
                _spherePositions = new Vector3[TOTAL_JOINT_COUNT];
            }

            if (_material != null && (_backing_material == null || !_backing_material.enableInstancing))
            {
                _backing_material = new Material(_material);
                _backing_material.hideFlags = HideFlags.DontSaveInEditor;
                _backing_material.enableInstancing = true;
                _sphereMat = new Material(_backing_material);
                _sphereMat.hideFlags = HideFlags.DontSaveInEditor;
            }

            //Update all joint spheres in the fingers
            foreach (var finger in _hand.Fingers)
            {
                for (int j = 0; j < 4; j++)
                {
                    int key = getFingerJointIndex((int)finger.Type, j);

                    Vector3 position = finger.Bone((Bone.BoneType)j).NextJoint.ToVector3();
                    _spherePositions[key] = position;

                    drawSphere(position);
                }
            }

            //Now we just have a few more spheres for the hands
            //PalmPos, WristPos, and mockThumbJointPos, which is derived and not taken from the frame obj

            Vector3 palmPosition = _hand.PalmPosition.ToVector3();
            drawSphere(palmPosition, _palmRadius);

            Vector3 thumbBaseToPalm = _spherePositions[THUMB_BASE_INDEX] - _hand.PalmPosition.ToVector3();
            Vector3 mockThumbJointPos = _hand.PalmPosition.ToVector3() + Vector3.Reflect(thumbBaseToPalm, _hand.Basis.xBasis.ToVector3());
            drawSphere(mockThumbJointPos);

            //If we want to show the arm, do the calculations and display the meshes
            if (_showArm)
            {
                DrawArm();
            }

            if (_showUpperArm)
            {
                DrawUpperArm();
            }

            if(_showUpperBody)
            {
                DrawUpperBody();
            }

            //Draw cylinders between finger joints
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    int keyA = getFingerJointIndex(i, j);
                    int keyB = getFingerJointIndex(i, j + 1);

                    Vector3 posA = _spherePositions[keyA];
                    Vector3 posB = _spherePositions[keyB];

                    drawCylinder(posA, posB);
                }
            }

            //Draw cylinders between finger knuckles
            for (int i = 0; i < 4; i++)
            {
                int keyA = getFingerJointIndex(i, 0);
                int keyB = getFingerJointIndex(i + 1, 0);

                Vector3 posA = _spherePositions[keyA];
                Vector3 posB = _spherePositions[keyB];

                drawCylinder(posA, posB);
            }

            //Draw the rest of the hand
            drawCylinder(mockThumbJointPos, THUMB_BASE_INDEX);
            drawCylinder(mockThumbJointPos, PINKY_BASE_INDEX);

            if (SetIndividualSphereColors)
            {
                if (_materialPropertyBlock == null)
                {
                    _materialPropertyBlock = new MaterialPropertyBlock();
                }

                for (int i = 0; i < _sphereMatrices.Length && i < _curSphereIndex; i++)
                {
                    _materialPropertyBlock.SetColor("_Color", SphereColors[i]);

                    Graphics.DrawMeshInstanced(_sphereMesh, 0, _sphereMat, new Matrix4x4[] { _sphereMatrices[i] }, 1, _materialPropertyBlock,
                      _castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);
                }
            }
            else
            {
                Graphics.DrawMeshInstanced(_sphereMesh, 0, _sphereMat, _sphereMatrices, _curSphereIndex, null,
                  _castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);
            }


            // Draw Cylinders
#if UNITY_EDITOR
            _cylinderMesh = getCylinderMesh(1f);
#else
            if (_cylinderMesh == null) { _cylinderMesh = getCylinderMesh(1f); }
#endif
            Graphics.DrawMeshInstanced(_cylinderMesh, 0, _backing_material, _cylinderMatrices, _curCylinderIndex, null,
              _castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);
        }

        void DrawArm()
        {
            var arm = _hand.Arm;

            Vector3 right = arm.Basis.xBasis.ToVector3() * arm.Width * 0.7f * 0.5f;
            Vector3 wrist = arm.WristPosition.ToVector3();
            Vector3 elbow = arm.ElbowPosition.ToVector3();

            float armLength = Vector3.Distance(wrist, elbow);
            wrist -= arm.Direction.ToVector3() * armLength * 0.05f;

            Vector3 armFrontRight = wrist + right;
            Vector3 armFrontLeft = wrist - right;
            Vector3 armBackRight = elbow + right;
            Vector3 armBackLeft = elbow - right;

            drawSphere(armFrontRight);
            drawSphere(armFrontLeft);
            drawSphere(armBackLeft);
            drawSphere(armBackRight);

            drawCylinder(armFrontLeft, armFrontRight);
            drawCylinder(armBackLeft, armBackRight);
            drawCylinder(armFrontLeft, armBackLeft);
            drawCylinder(armFrontRight, armBackRight);
        }

        void DrawUpperArm()
        {
            Vector3 shoulderPos = Camera.main.transform.TransformPoint(handedness == Chirality.Left ? -0.15f : 0.15f, -0.15f, -0.05f);

            var arm = _hand.Arm;

            Vector3 elbow = arm.ElbowPosition.ToVector3();
            Vector3 right = arm.Basis.xBasis.ToVector3() * arm.Width * 0.7f * 0.5f;

            Vector3 armFrontRight = elbow + right;
            Vector3 armFrontLeft = elbow - right;
            Vector3 armBackRight = shoulderPos + right;
            Vector3 armBackLeft = shoulderPos - right;

            drawSphere(armBackLeft);
            drawSphere(armBackRight);

            drawCylinder(armFrontLeft, armFrontRight);
            drawCylinder(armBackLeft, armBackRight);
            drawCylinder(armFrontLeft, armBackLeft);
            drawCylinder(armFrontRight, armBackRight);
        }

        void DrawUpperBody()
        {
            if ((leapProvider != null && leapProvider.CurrentFrame.Hands.Count == 1) || handedness == Chirality.Left)
            {
                // Head

                Vector3 headBottomLeftFront = Camera.main.transform.TransformPoint(0.06f, -0.05f, 0) + new Vector3(0, -0.05f, 0);
                Vector3 headBottomRightFront = Camera.main.transform.TransformPoint(-0.06f, -0.05f, 0) + new Vector3(0, -0.05f, 0);
                Vector3 headTopLeftFront = Camera.main.transform.TransformPoint(0.07f, 0.08f, 0) + new Vector3(0, -0.05f, 0);
                Vector3 headTopRightFront = Camera.main.transform.TransformPoint(-0.07f, 0.08f, 0) + new Vector3(0, -0.05f, 0);

                Vector3 headBottomLeftBack = Camera.main.transform.TransformPoint(0.06f, -0.05f, -0.1f) + new Vector3(0, -0.05f, 0);
                Vector3 headBottomRightBack = Camera.main.transform.TransformPoint(-0.06f, -0.05f, -0.1f) + new Vector3(0, -0.05f, 0);
                Vector3 headTopLeftBack = Camera.main.transform.TransformPoint(0.07f, 0.08f, -0.1f) + new Vector3(0, -0.05f, 0);
                Vector3 headTopRightBack = Camera.main.transform.TransformPoint(-0.07f, 0.08f, -0.1f) + new Vector3(0, -0.05f, 0);

                drawSphere(headBottomLeftFront);
                drawSphere(headBottomRightFront);
                drawSphere(headTopLeftFront);
                drawSphere(headTopRightFront);

                drawCylinder(headBottomLeftFront, headBottomRightFront);
                drawCylinder(headTopLeftFront, headTopRightFront);
                drawCylinder(headBottomLeftFront, headTopLeftFront);
                drawCylinder(headBottomRightFront, headTopRightFront);

                drawSphere(headBottomLeftBack);
                drawSphere(headBottomRightBack);
                drawSphere(headTopLeftBack);
                drawSphere(headTopRightBack);

                drawCylinder(headBottomLeftBack, headBottomRightBack);
                drawCylinder(headTopLeftBack, headTopRightBack);
                drawCylinder(headBottomLeftBack, headTopLeftBack);
                drawCylinder(headBottomRightBack, headTopRightBack);

                drawCylinder(headTopRightBack, headTopRightFront);
                drawCylinder(headTopLeftBack, headTopLeftFront);
                drawCylinder(headBottomLeftBack, headBottomLeftFront);
                drawCylinder(headBottomRightBack, headBottomRightFront);

                // Neck

                Vector3 neckBottomLeft = Camera.main.transform.TransformPoint(0.02f, -0.15f, -0.05f);
                Vector3 neckBottomRight = Camera.main.transform.TransformPoint(-0.02f, -0.15f, -0.05f);
                Vector3 neckTopLeft = (headBottomLeftBack + headBottomLeftFront) / 2;
                Vector3 neckTopRight = (headBottomRightBack + headBottomRightFront) / 2;

                drawSphere(neckBottomLeft);
                drawSphere(neckBottomRight);
                drawSphere(neckTopLeft);
                drawSphere(neckTopRight);

                drawCylinder(neckBottomLeft, neckBottomRight);
                drawCylinder(neckTopLeft, neckTopRight);
                drawCylinder(neckBottomLeft, neckTopLeft);
                drawCylinder(neckBottomRight, neckTopRight);

                // Torso

                Vector3 torsoBottomLeft = Camera.main.transform.TransformPoint(-0.1f, -0.3f, -0.05f);
                Vector3 torsoBottomRight = Camera.main.transform.TransformPoint(0.1f, -0.3f, -0.05f);
                Vector3 torsoTopLeft = Camera.main.transform.TransformPoint(-0.12f, -0.15f, -0.05f);
                Vector3 torsoTopRight = Camera.main.transform.TransformPoint(0.12f, -0.15f, -0.05f);

                drawSphere(torsoBottomLeft);
                drawSphere(torsoBottomRight);
                drawSphere(torsoTopLeft);
                drawSphere(torsoTopRight);

                drawCylinder(torsoBottomLeft, torsoBottomRight);
                drawCylinder(torsoTopLeft, torsoTopRight);
                drawCylinder(torsoBottomLeft, torsoTopLeft);
                drawCylinder(torsoBottomRight, torsoTopRight);

                Vector3 torsoLowerTopLeft = Camera.main.transform.TransformPoint(-0.05f, -0.3f, -0.05f);
                Vector3 torsoLowerTopRight = Camera.main.transform.TransformPoint(0.05f, -0.3f, -0.05f);
                Vector3 torsoLowerBottomLeft = torsoLowerTopLeft + new Vector3(0f, -0.2f, 0);
                Vector3 torsoLowerBottomRight = torsoLowerTopRight + new Vector3(0f, -0.2f, 0);

                drawSphere(torsoLowerBottomLeft);
                drawSphere(torsoLowerBottomRight);
                drawSphere(torsoLowerTopLeft);
                drawSphere(torsoLowerTopRight);

                drawCylinder(torsoLowerBottomLeft, torsoLowerBottomRight);
                drawCylinder(torsoLowerTopLeft, torsoLowerTopRight);
                drawCylinder(torsoLowerBottomLeft, torsoLowerTopLeft);
                drawCylinder(torsoLowerBottomRight, torsoLowerTopRight);
            }
        }

        private void drawSphere(Vector3 position)
        {
            drawSphere(position, _jointRadius);
        }

        private void drawSphere(Vector3 position, float radius)
        {
            if (isNaN(position)) { return; }

            //multiply radius by 2 because the default unity sphere has a radius of 0.5 meters at scale 1.
            _sphereMatrices[_curSphereIndex++] = Matrix4x4.TRS(position,
              Quaternion.identity, Vector3.one * radius * 2.0f * transform.lossyScale.x);
        }

        private void drawCylinder(Vector3 a, Vector3 b)
        {
            if (isNaN(a) || isNaN(b)) { return; }

            float length = (a - b).magnitude;

            if ((a - b).magnitude > 0.001f)
            {
                _cylinderMatrices[_curCylinderIndex++] = Matrix4x4.TRS(a,
                  Quaternion.LookRotation(b - a), new Vector3(transform.lossyScale.x, transform.lossyScale.x, length));
            }
        }

        private bool isNaN(Vector3 v)
        {
            return float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z);
        }

        private void drawCylinder(int a, int b)
        {
            drawCylinder(_spherePositions[a], _spherePositions[b]);
        }

        private void drawCylinder(Vector3 a, int b)
        {
            drawCylinder(a, _spherePositions[b]);
        }

        private int getFingerJointIndex(int fingerIndex, int jointIndex)
        {
            return fingerIndex * 4 + jointIndex;
        }

        private Dictionary<int, Mesh> _meshMap = new Dictionary<int, Mesh>();
        private Mesh getCylinderMesh(float length)
        {
            int lengthKey = Mathf.RoundToInt(length * 100 / CYLINDER_MESH_RESOLUTION);

            Mesh mesh;
            if (_meshMap.TryGetValue(lengthKey, out mesh))
            {
                return mesh;
            }

            mesh = new Mesh();
            mesh.name = "GeneratedCylinder";
            mesh.hideFlags = HideFlags.DontSave;

            List<Vector3> verts = new List<Vector3>();
            List<Color> colors = new List<Color>();
            List<int> tris = new List<int>();

            Vector3 p0 = Vector3.zero;
            Vector3 p1 = Vector3.forward * length;
            for (int i = 0; i < _cylinderResolution; i++)
            {
                float angle = (Mathf.PI * 2.0f * i) / _cylinderResolution;
                float dx = _cylinderRadius * Mathf.Cos(angle);
                float dy = _cylinderRadius * Mathf.Sin(angle);

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

                tris.Add((triStart + 2) % triCap);
                tris.Add((triStart + 3) % triCap);
                tris.Add((triStart + 1) % triCap);
            }

            mesh.SetVertices(verts);
            mesh.SetIndices(tris.ToArray(), MeshTopology.Triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.UploadMeshData(true);

            _meshMap[lengthKey] = mesh;

            return mesh;
        }
    }
#pragma warning restore 0618
}