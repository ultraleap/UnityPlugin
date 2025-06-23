/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Attributes;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Leap
{
    /// <summary>
    /// Fingertip representation
    /// </summary>
    public enum TipRepresentation
    {
        Default,
        Cone
    }

    /// <summary>
    /// The CapsuleHand is a basic Leap hand model that generates a set of spheres and 
    /// cylinders to render hands using Leap hand data.
    /// It is constructed dynamically rather than using pre-existing geometry which
    /// allows hand visuals to scale to the size of the users hand and 
    /// is a reliable way to visualize the raw tracking data.
    /// </summary>
    public class CapsuleHand : HandModelBase
    {
        public enum CapsuleHandPreset
        {
            Default,
            Minimal,
            Ultraleap,
            XRHandDebugHandLike,
            DefaultThin,
        }

        private CapsuleHandPreset _preset;
        [SerializeField]
        public CapsuleHandPreset Preset
        {
            get
            {
                return _preset;
            }
            set
            {
                _preset = value;
                ChangePreset(_preset);
            }
        }

        private const int TOTAL_JOINT_COUNT = 26;
        private const float CYLINDER_MESH_RESOLUTION = 0.1f; //in centimeters, meshes within this resolution will be re-used
        private const int PINKY_BASE_INDEX = (int)Finger.FingerType.PINKY * 4;

        private static int _leftColorIndex = 0;
        private static int _rightColorIndex = 0;
        private static Color[] _leftColorList = { new Color(0.0f, 0.0f, 1.0f), new Color(0.2f, 0.0f, 0.4f), new Color(0.0f, 0.2f, 0.2f) };
        private static Color[] _rightColorList = { new Color(1.0f, 0.0f, 0.0f), new Color(1.0f, 1.0f, 0.0f), new Color(1.0f, 0.5f, 0.0f) };

        [SerializeField]
        private Chirality handedness;

        [Space, SerializeField]
        private bool _showArm = true;

        [SerializeField, Tooltip("Shows the upper arm. Best in XR, assumes the camera is positioned at the users head")]
        private bool _showUpperArm = false;

        [Space, SerializeField]
        private bool _showPalmJoint = true;

        [SerializeField]
        private bool _scalePalmJointToPalmRadius = true;

        [SerializeField]
        private bool _showFingertipPosition = true;

        [SerializeField]
        private TipRepresentation _tipRepresentation = TipRepresentation.Cone;

        [SerializeField]
        private bool _showAllMetacarpals = false;

        [SerializeField]
        private bool _showPinkyMetacarpal = true;

        [SerializeField]
        private bool _joinFingerProximals = true;

        [SerializeField]
        private bool _joinThumbProximal = true;

        [Space,SerializeField]
        private bool _showJointOrientation = true;

        [Space, SerializeField]
        private bool _castShadows = true;


        [Space, SerializeField]
        private Material _material;
        private Material _backing_material;

        [SerializeField]
        private Mesh _sphereMesh;
        private Mesh _cylinderMesh;
        private Mesh _coneMesh;

        [MinValue(3)]
        [SerializeField]
        private int _cylinderResolution = 12;

        [MinValue(3)]
        [SerializeField]
        private int _coneResolution = 12;

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

        private Material _sphereMat;
        private Hand _hand;
        private Vector3[] _spherePositions;
        private Matrix4x4[] _sphereMatrices = new Matrix4x4[64],
                            _cylinderMatrices = new Matrix4x4[64],
                            _jointOrientationMatrices_forward = new Matrix4x4[64],
                            _jointOrientationMatrices_up = new Matrix4x4[64],
                            _jointOrientationMatrices_right = new Matrix4x4[64],
                            _fingertipMatrices = new Matrix4x4[64];


        private int _curSphereIndex = 0, _curCylinderIndex = 0, _curJointOrientationIndex = 0, _curFingertipIndex = 0;
        private Color _backingDefault = Color.white;

        private MaterialPropertyBlock _materialPropertyBlock;
        private Color[] _sphereColors;

        private float currentLossyScaleX;

        [HideInInspector]
        public bool SetIndividualSphereColors = false;
        public Color[] SphereColors
        {
            get
            {
                if (_sphereColors == null)
                {
                    _sphereColors = new Color[32];
                    Leap.Utils.Fill(_sphereColors, SphereColour);
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
            if (hand != null)
            {
                if (_hand == null)
                {
                    _hand = new Hand();
                }
                _hand = _hand.CopyFrom(hand);
            }
            else
            {
                _hand = null;
            }
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

            if (_sphereMat != null)
                _sphereMat.color = _sphereColor;
            if (_backing_material != null)
                _backing_material.color = _cylinderColor;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _cylinderMeshMap.Clear();
            _coneMeshMap.Clear();

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
        /// Creates a mesh based representation of the capsule hand, designed for export
        /// </summary>
        /// <returns>A GameObject that is the root of the hand mesh</returns>
        public GameObject CaptureMesh()
        {
            if (Application.isPlaying)
            {
                CalculateHandPrimitives();

#if UNITY_EDITOR
                _cylinderMesh = getCylinderMesh(1f);
                _coneMesh = getConeMesh(1f);
#else
                if (_cylinderMesh == null) { _cylinderMesh = getCylinderMesh(1f); }
                if (_coneMesh == null) { _coneMesh = getConeMesh(1f); }
#endif
                // Convert matrix data to mesh representations ...
                GameObject _meshParent = new GameObject($"{_hand.GetChirality().ToString()}_Hand");
                _meshParent.transform.parent = transform;

                GameObject _sphereMeshParentGO = new GameObject($"SphereParent");
                _sphereMeshParentGO.transform.parent = transform;

                // Spheres
                ConvertPrimitivesToMesh(_meshParent, _sphereMatrices, null, _sphereMesh, new Material(_sphereMat));

                // Orientation data
                if (_showJointOrientation)
                {
                    ConvertPrimitivesToMesh(_meshParent, _jointOrientationMatrices_forward, null, _coneMesh, new Material(_sphereMat));
                    ConvertPrimitivesToMesh(_meshParent, _jointOrientationMatrices_right, null, _coneMesh, new Material(_sphereMat));
                    ConvertPrimitivesToMesh(_meshParent, _jointOrientationMatrices_up, null, _coneMesh, new Material(_sphereMat));
                }

                // Cylinders
                ConvertPrimitivesToMesh(_meshParent, _cylinderMatrices, null, _cylinderMesh, _backing_material);

                // Cone tips, if selected
                if (_tipRepresentation == TipRepresentation.Cone)
                {
                    ConvertPrimitivesToMesh(_meshParent, _fingertipMatrices, null, _coneMesh, _backing_material);
                }

                return _meshParent;
            }

            return null;

            void ConvertPrimitivesToMesh(GameObject parent, Matrix4x4[] meshMatrixArray, Vector3[] lossyScale, Mesh meshPrefab, Material meshMaterial)
            {
                GameObject meshParent = new GameObject($"MeshParent");
                meshParent.transform.parent = parent.transform;
                for (int i = 0; i < meshMatrixArray.Length; i++)
                {
                    if (meshMatrixArray[i].ValidTRS())
                    {
                        GameObject meshGO = new GameObject($"Mesh{i}");
                        Mesh meshClone = Instantiate(meshPrefab);
                        var mf = meshGO.AddComponent<MeshFilter>();
                        mf.mesh = meshClone;
                        var mr = meshGO.AddComponent<MeshRenderer>();
                        mr.material = meshMaterial;
                        meshGO.transform.parent = meshParent.transform;
                        meshGO.transform.SetPose(meshMatrixArray[i].GetPose());

                        if (lossyScale == null)
                        {
                            meshGO.transform.SetLossyScale(meshMatrixArray[i].lossyScale);
                        }
                        else
                        {
                            meshGO.transform.SetLossyScale(lossyScale[i]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called once per frame when the LeapProvider calls the event OnUpdateFrame.
        /// Updates all joint sphere positions and draws the spheres and cylinders of the hand.
        /// </summary>
        public override void UpdateHand()
        {
            CalculateHandPrimitives();
            RenderHand();
        }

        private void CalculateHandPrimitives()
        {
            currentLossyScaleX = transform.lossyScale.x;

            _curSphereIndex = 0;
            _curCylinderIndex = 0;
            _curJointOrientationIndex = 0;
            _curFingertipIndex = 0;

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

            // Fingers
            foreach (var _finger in _hand.fingers)
            {
                // Calculate the joint spheres positions
                if (!_showAllMetacarpals)
                {
                    if (_finger.Type == Finger.FingerType.PINKY || _finger.Type == Finger.FingerType.INDEX)
                    {
                        CalculateSphereMatrixForJoint(_finger.Metacarpal);
                    }
                }
                else
                {
                    CalculateSphereMatrixForJoint(_finger.Metacarpal);
                }

                if (_showAllMetacarpals || _finger.Type != Finger.FingerType.THUMB)
                {
                    CalculateSphereMatrixForJoint(_finger.Proximal);
                }

                CalculateSphereMatrixForJoint(_finger.Intermediate);
                CalculateSphereMatrixForJoint(_finger.Distal);

                if (_tipRepresentation == TipRepresentation.Default)
                {
                    CalculateSphereMatrix(_finger.TipPosition);
                }

                // Calculate the cylinder bones positions
                if (_tipRepresentation == TipRepresentation.Default)
                {
                    CalculateMatrixForPrimitive(_finger.TipPosition, _finger.Distal.PrevJoint);
                }
                else if (_tipRepresentation == TipRepresentation.Cone)
                {
                    // Replace the end sphere and cylinder with a cone that ends at the tip
                    CalculateMatrixForPrimitive(_finger.Distal.PrevJoint, _finger.TipPosition,
                        _fingertipMatrices, ref _curFingertipIndex, true);
                }

                CalculateMatrixForPrimitive(_finger.Intermediate.PrevJoint, _finger.Distal.PrevJoint);

                if (_finger.Type == Finger.FingerType.THUMB && !_showAllMetacarpals)
                {
                    // The traditional leap motion hand joins the thumb intermediate to the index metacarpal
                    CalculateMatrixForPrimitive(_hand.Index.Metacarpal.PrevJoint, _hand.Thumb.Intermediate.PrevJoint);
                }
                else
                {
                    CalculateMatrixForPrimitive(_finger.Proximal.PrevJoint, _finger.Intermediate.PrevJoint);
                }
            }

            // Calculate the cylinders making up the palm area

            // Top
            if (_joinFingerProximals)
            {
                CalculateMatrixForPrimitive(_hand.Index.Proximal.PrevJoint, _hand.Middle.Proximal.PrevJoint);
                CalculateMatrixForPrimitive(_hand.Middle.Proximal.PrevJoint, _hand.Ring.Proximal.PrevJoint);
                CalculateMatrixForPrimitive(_hand.Ring.Proximal.PrevJoint, _hand.Pinky.Proximal.PrevJoint);
            }

            // 'Sides'
            // Calculate metacarpal bones that make up the sides of the palm 'rectangle'
            if (_showAllMetacarpals)
            {
                CalculateMatrixForPrimitive(_hand.Index.Metacarpal.PrevJoint, _hand.Index.Proximal.PrevJoint);
                CalculateMatrixForPrimitive(_hand.Middle.Metacarpal.PrevJoint, _hand.Middle.Proximal.PrevJoint);
                CalculateMatrixForPrimitive(_hand.Ring.Metacarpal.PrevJoint, _hand.Ring.Proximal.PrevJoint);
                CalculateMatrixForPrimitive(_hand.Pinky.Metacarpal.PrevJoint, _hand.Pinky.Proximal.PrevJoint);
            }
            else
            {
                if (_joinThumbProximal)
                {
                    CalculateMatrixForPrimitive(_hand.Index.Metacarpal.PrevJoint, _hand.Index.Proximal.PrevJoint);
                }

                if (_showPinkyMetacarpal)
                {
                    CalculateMatrixForPrimitive(_hand.Pinky.Metacarpal.PrevJoint, _hand.Pinky.Proximal.PrevJoint);
                }
            }

            // Calculate cylinder bone(s) across the bottom of the palm
            if (_showAllMetacarpals)
            {
                Vector3 wristPosition = _hand.WristPosition;
                CalculateSphereMatrix(wristPosition);

                CalculateMatrixForPrimitive(wristPosition, _hand.Thumb.Metacarpal.PrevJoint);
                CalculateMatrixForPrimitive(wristPosition, _hand.Index.Metacarpal.PrevJoint);
                CalculateMatrixForPrimitive(wristPosition, _hand.Middle.Metacarpal.PrevJoint);
                CalculateMatrixForPrimitive(wristPosition, _hand.Ring.Metacarpal.PrevJoint);
                CalculateMatrixForPrimitive(wristPosition, _hand.Pinky.Metacarpal.PrevJoint);
            }
            else if (_showPinkyMetacarpal)
            {
                CalculateMatrixForPrimitive(_hand.Index.Metacarpal.PrevJoint, _hand.Pinky.Metacarpal.PrevJoint);
            }

            if (_showPalmJoint && !_showAllMetacarpals)
            {
                Vector3 palmPosition = _hand.PalmPosition;

                if (_scalePalmJointToPalmRadius)
                {
                    CalculateSphereMatrix(palmPosition, _palmRadius);
                }
                else
                {
                    CalculateSphereMatrix(palmPosition);
                }
            }

            // If we want to show the arm, do the calculations and display the meshes.
            // Note, the wrist position for the arm is the *arm* wrist position, not the *hand*
            // wrist position, which is different. When all metacarpals are drawn, the hand
            // wrist position overlaps with the arm wrist position, so we don't draw the arm
            if (!_showAllMetacarpals)
            {
                if (_showArm)
                {
                    DrawArm();
                }

                if (_showUpperArm)
                {
                    DrawUpperArm();
                }
            }
        }

        private void RenderHand()
        {
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
            _coneMesh = getConeMesh(1f);
#else
            if (_cylinderMesh == null) { _cylinderMesh = getCylinderMesh(1f); }
            if (_coneMesh == null) { _coneMesh = getConeMesh(1f); }
#endif

            if (_materialPropertyBlock == null)
            {
                _materialPropertyBlock = new MaterialPropertyBlock();
            }

            if (_showJointOrientation)
            {
                _materialPropertyBlock.SetColor("_Color", Color.red);

                Graphics.DrawMeshInstanced(_coneMesh, 0, _backing_material, _jointOrientationMatrices_forward, _curJointOrientationIndex, _materialPropertyBlock,
                  _castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);

                _materialPropertyBlock.SetColor("_Color", Color.green);

                Graphics.DrawMeshInstanced(_coneMesh, 0, _backing_material, _jointOrientationMatrices_up, _curJointOrientationIndex, _materialPropertyBlock,
                  _castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);

                _materialPropertyBlock.SetColor("_Color", Color.blue);

                Graphics.DrawMeshInstanced(_coneMesh, 0, _backing_material, _jointOrientationMatrices_right, _curJointOrientationIndex, _materialPropertyBlock,
                  _castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);
            }

            if (_showFingertipPosition)
            {
                if (_tipRepresentation == TipRepresentation.Cone)
                {
                    _materialPropertyBlock.SetColor("_Color", _cylinderColor);

                    Graphics.DrawMeshInstanced(_coneMesh, 0, _backing_material, _fingertipMatrices, _curFingertipIndex, _materialPropertyBlock,
                      _castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);
                }
            }

            _materialPropertyBlock.SetColor("_Color", _cylinderColor);

            Graphics.DrawMeshInstanced(_cylinderMesh, 0, _backing_material, _cylinderMatrices, _curCylinderIndex, _materialPropertyBlock,
              _castShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);
        }

        private void CaptureBoneOrientation(Bone bone)
        {
            CaptureBoneOrientation(bone.PrevJoint, bone.Rotation);
        }

        private void CaptureBoneOrientation(Vector3 position, Quaternion orientation)
        {
            float cachedScale = currentLossyScaleX;

            LeapTransform t = new LeapTransform(position, orientation);
            currentLossyScaleX = 2 * (_cylinderRadius / 0.006f); // 0.006f is the default cylinder radius
                
            CalculateMatrixForPrimitive(position, position + t.xBasis.normalized * 0.015f, ref _jointOrientationMatrices_forward, ref _curJointOrientationIndex);
            CalculateMatrixForPrimitive(position, position + t.yBasis.normalized * 0.015f, ref _jointOrientationMatrices_right, ref _curJointOrientationIndex);
            CalculateMatrixForPrimitive(position, position + t.zBasis.normalized * 0.015f, ref _jointOrientationMatrices_up, ref _curJointOrientationIndex);

            currentLossyScaleX = cachedScale;
            _curJointOrientationIndex++;
        }

        private void CalculateMatrixForPrimitive(Vector3 a, Vector3 b, ref Matrix4x4[] targetMatrix, ref int targetIndex, bool incrementIndex = false)
        {
            if (isNaN(a) || isNaN(b)) { return; }

            float length = (a - b).magnitude;

            if ((a - b).magnitude > 0.001f)
            {
                targetMatrix[targetIndex] = Matrix4x4.TRS(a,
                  Quaternion.LookRotation(b - a), new Vector3(currentLossyScaleX, currentLossyScaleX, length));
            }

            if (incrementIndex)
            {
                targetIndex++;
            }
        }

        private void CalculateSphereMatrixForJoint(Bone joint)
        {
            _spherePositions[_curSphereIndex] = joint.PrevJoint;

            CalculateSphereMatrix(joint.PrevJoint);

            if (_showJointOrientation)
            {
                CaptureBoneOrientation(joint);
            }
        }

        private void CalculateSphereMatrix(Vector3 position)
        {
            CalculateSphereMatrix(position, _jointRadius);
        }

        private void CalculateSphereMatrix(Vector3 position, float radius)
        {
            if (isNaN(position)) { return; }

            //multiply radius by 2 because the default unity sphere has a radius of 0.5 meters at scale 1.
            _sphereMatrices[_curSphereIndex++] = Matrix4x4.TRS(position,
              Quaternion.identity, Vector3.one * radius * 2.0f * currentLossyScaleX);
        }

        private void CalculateMatrixForPrimitive(Vector3 a, Vector3 b)
        {
            CalculateMatrixForPrimitive(a, b, _cylinderMatrices, ref _curCylinderIndex, true);
        }

        private void CalculateMatrixForPrimitive(Vector3 a, Vector3 b, Matrix4x4[] targetMatrix, ref int targetIndex, bool incrementIndex = false)
        {
            if (isNaN(a) || isNaN(b)) { return; }

            float length = (a - b).magnitude;

            if ((a - b).magnitude > 0.001f)
            {
                targetMatrix[targetIndex] = Matrix4x4.TRS(a,
                  Quaternion.LookRotation(b - a), new Vector3(currentLossyScaleX, currentLossyScaleX, length));
            }

            if (incrementIndex)
            {
                targetIndex++;
            }
        }

        void DrawArm()
        {
            var arm = _hand.Arm;

            Vector3 right = arm.Basis.xBasis * arm.Width * 0.7f * 0.5f;
            Vector3 wrist = arm.WristPosition;
            Vector3 elbow = arm.ElbowPosition;

            float armLength = Vector3.Distance(wrist, elbow);
            wrist -= arm.Direction * armLength * 0.05f;

            Vector3 armFrontRight = wrist + right;
            Vector3 armFrontLeft= wrist - right;
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

            if (_showJointOrientation)
            {
                CaptureBoneOrientation(armFrontLeft, arm.Rotation);
                CaptureBoneOrientation(armFrontRight, arm.Rotation);
                CaptureBoneOrientation(armBackLeft, arm.Rotation);
                CaptureBoneOrientation(armBackRight, arm.Rotation);
            }
        }

        void DrawUpperArm()
        {
            Vector3 shoulderPos = Camera.main.transform.TransformPoint(handedness == Chirality.Left ? -0.15f : 0.15f, -0.15f, -0.05f);

            var arm = _hand.Arm;

            Vector3 elbow = arm.ElbowPosition;
            Vector3 right = arm.Basis.xBasis * arm.Width * 0.7f * 0.5f;

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

        private void drawSphere(Vector3 position)
        {
            drawSphere(position, _jointRadius);
        }

        private void drawSphere(Vector3 position, float radius)
        {
            if (isNaN(position)) { return; }

            //multiply radius by 2 because the default unity sphere has a radius of 0.5 meters at scale 1.
            _sphereMatrices[_curSphereIndex++] = Matrix4x4.TRS(position,
              Quaternion.identity, Vector3.one * radius * 2.0f * currentLossyScaleX);
        }

        private void drawCylinder(Vector3 a, Vector3 b)
        {
            if (isNaN(a) || isNaN(b)) { return; }

            float length = (a - b).magnitude;

            if ((a - b).magnitude > 0.001f)
            {
                _cylinderMatrices[_curCylinderIndex++] = Matrix4x4.TRS(a,
                  Quaternion.LookRotation(b - a), new Vector3(currentLossyScaleX, currentLossyScaleX, length));
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

        private Dictionary<int, Mesh> _cylinderMeshMap = new Dictionary<int, Mesh>();
        private Mesh getCylinderMesh(float length)
        {
            int lengthKey = Mathf.RoundToInt(length * 100 / CYLINDER_MESH_RESOLUTION);

            Mesh mesh;
            if (_cylinderMeshMap.TryGetValue(lengthKey, out mesh))
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

            _cylinderMeshMap[lengthKey] = mesh;

            return mesh;
        }

        private Dictionary<int, Mesh> _coneMeshMap = new Dictionary<int, Mesh>();
        private Mesh getConeMesh(float length)
        {
            int lengthKey = Mathf.RoundToInt(length * 100 / CYLINDER_MESH_RESOLUTION);

            Mesh mesh;
            if (_coneMeshMap.TryGetValue(lengthKey, out mesh))
            {
                return mesh;
            }

            mesh = new Mesh();
            mesh.name = "GeneratedCone";
            mesh.hideFlags = HideFlags.DontSave;

            List<Vector3> verts = new List<Vector3>();
            List<Color> colors = new List<Color>();
            List<int> tris = new List<int>();

            Vector3 p0 = Vector3.zero;
            Vector3 p1 = Vector3.forward * length;

      
            for (int i = 0; i < _coneResolution; i++)
            {
                float angle = (Mathf.PI * 2.0f * i) / _coneResolution;
                float dx = _cylinderRadius * Mathf.Cos(angle);
                float dy = _cylinderRadius * Mathf.Sin(angle);

                Vector3 spoke = new Vector3(dx, dy, 0);

                int triStart = verts.Count;
                verts.Add(p0 + spoke);
             
                colors.Add(Color.white);
                colors.Add(Color.white);

                int triCap = _coneResolution;

                tris.Add(_coneResolution);
                tris.Add((triStart) % (triCap));
                tris.Add((triStart + 1) % (triCap));
            }

            verts.Add(p1);

            mesh.SetVertices(verts);
            mesh.SetIndices(tris.ToArray(), MeshTopology.Triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.UploadMeshData(true);

            _coneMeshMap[lengthKey] = mesh;

            return mesh;
        }


        public void ChangePreset(CapsuleHandPreset preset)
        {
            _preset = preset;

            switch (preset)
            {
                case CapsuleHandPreset.Default:
                    _showArm = true;
                    _showPalmJoint = true;
                    _showAllMetacarpals = false;
                    _showPinkyMetacarpal = true;
                    _joinFingerProximals = true;
                    _joinThumbProximal = true;
                    _showJointOrientation = false;
                    _tipRepresentation = TipRepresentation.Default;
                    _castShadows = true;
                    _cylinderResolution = 12;
                    _jointRadius = 0.008f;
                    _cylinderRadius = 0.006f;
                    _palmRadius = 0.015f;
                    _useCustomColors = false;
                    _cylinderColor = Color.white;
                    _leftColorList = new Color[] { new Color(0.0f, 0.0f, 1.0f), new Color(0.2f, 0.0f, 0.4f), new Color(0.0f, 0.2f, 0.2f) };
                    _rightColorList = new Color[] { new Color(1.0f, 0.0f, 0.0f), new Color(1.0f, 1.0f, 0.0f), new Color(1.0f, 0.5f, 0.0f) };
                    break;

                case CapsuleHandPreset.DefaultThin:
                    _showArm = true;
                    _showPalmJoint = true;
                    _showAllMetacarpals = false;
                    _showPinkyMetacarpal = true;
                    _joinFingerProximals = true;
                    _joinThumbProximal = true;
                    _showJointOrientation = false;
                    _tipRepresentation = TipRepresentation.Default;
                    _castShadows = true;
                    _cylinderResolution = 12;
                    _jointRadius = 0.004f;
                    _cylinderRadius = 0.002f;
                    _palmRadius = 0.008f;
                    _useCustomColors = false;
                    _cylinderColor = Color.white;
                    _leftColorList = new Color[] { new Color(0.0f, 0.0f, 1.0f), new Color(0.2f, 0.0f, 0.4f), new Color(0.0f, 0.2f, 0.2f) };
                    _rightColorList = new Color[] { new Color(1.0f, 0.0f, 0.0f), new Color(1.0f, 1.0f, 0.0f), new Color(1.0f, 0.5f, 0.0f) };
                    break;

                case CapsuleHandPreset.XRHandDebugHandLike:
                    _showArm = false;
                    _showPalmJoint = false;
                    _showAllMetacarpals = true;
                    _joinFingerProximals = false;
                    _showJointOrientation = false;
                    _tipRepresentation = TipRepresentation.Default;
                    _castShadows = true;
                    _cylinderResolution = 12;
                    _jointRadius = 0.005f;
                    _cylinderRadius = 0.0025f;
                    _palmRadius = 0.005f;
                    _useCustomColors = false;
                    _cylinderColor = Color.white;
                    _leftColorList = new Color[] { new Color(0.0f, 0.0f, 1.0f), new Color(0.2f, 0.0f, 0.4f), new Color(0.0f, 0.2f, 0.2f) };
                    _rightColorList = new Color[] { new Color(1.0f, 0.0f, 0.0f), new Color(1.0f, 1.0f, 0.0f), new Color(1.0f, 0.5f, 0.0f) };
                    break;

                case CapsuleHandPreset.Minimal:
                    _showArm = true;
                    _showPalmJoint = false;
                    _showPinkyMetacarpal = false;
                    _showAllMetacarpals = false;
                    _joinFingerProximals = true;
                    _joinThumbProximal = true;
                    _showJointOrientation = false;
                    _tipRepresentation = TipRepresentation.Default;
                    _castShadows = true;
                    _cylinderResolution = 12;
                    _jointRadius = 0.006f;
                    _cylinderRadius = 0.006f;
                    _useCustomColors = true;
                    _sphereColor = Color.white;
                    _cylinderColor = Color.white;
                    break;

                case CapsuleHandPreset.Ultraleap:
                    _showArm = true;
                    _showPalmJoint = false;
                    _showPinkyMetacarpal = true;
                    _showAllMetacarpals = false;
                    _joinFingerProximals = true;
                    _joinThumbProximal = true;
                    _showJointOrientation = false;
                    _castShadows = true;
                    _cylinderResolution = 12;
                    _jointRadius = 0.006f;
                    _cylinderRadius = 0.006f;
                    _useCustomColors = true;
                    _sphereColor = new Color(0.0f, 0.87f, 0.68f);
                    _cylinderColor = new Color(0.0f, 0.87f, 0.68f);
                    break;
            }

#if UNITY_EDITOR
            OnValidate();
#endif
        }
    }
}