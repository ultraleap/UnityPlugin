using UnityEngine;

namespace Leap.Unity.Preview.Locomotion
{
    public class TeleportAnchor : MonoBehaviour
    {
        [SerializeField, Tooltip("The main teleport anchor mesh.")]
        private MeshRenderer _markerMesh = null;

        [SerializeField, Tooltip("The gameobject containing the objects which indicate which way the user will face")]
        private Transform _rotationIndicators = null;

        [SerializeField, ColorUsage(true, true)]
        private Color _idleColor = new Color(1, 1, 1, 0.25f);

        [SerializeField, ColorUsage(true, true)]
        private Color32 _highlightedColor = new Color(1, 1, 1, 0.25f);

        [SerializeField, Tooltip("A higher value will tile the texture more and appear smaller.")]
        private float _idleSize = 4f, _highlightedSize = 1f;

        [SerializeField, Tooltip("The speed at which the markers visuals will transition.")]
        private float _transitionTime = 0.2f;

        private float _oldTransition = 0f;
        private float _currentTransition = 0f;

        protected bool _isHighlighted = false;

        private Material _storedMaterial;

        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Quaternion initialRotationIndicatorsRotation;

        private void Awake()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            initialRotationIndicatorsRotation = _rotationIndicators.rotation;

            Material material = _markerMesh.sharedMaterial;
            if (material != null)
            {
                _storedMaterial = new Material(material);
                _markerMesh.sharedMaterial = _storedMaterial;
                UpdateMaterials();
            }
        }

        public void SetHighlighted(bool highlighted = true)
        {
            _isHighlighted = highlighted;

            if (!_isHighlighted)
            {
                ResetPoint();
            }
        }

        public virtual void Update()
        {
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            _currentTransition += Time.deltaTime * ((_isHighlighted ? 1f : -1f) / _transitionTime);
            if (_isHighlighted)
            {
                if (_currentTransition > 0.9999f)
                {
                    _currentTransition = 1f;
                }
            }
            else
            {
                if (_currentTransition < 1e-4)
                {
                    _currentTransition = 0f;
                }
            }
            if (_currentTransition != _oldTransition)
            {
                UpdateMaterials();
            }
            _oldTransition = _currentTransition;
        }

        private void UpdateMaterials()
        {
            _storedMaterial.SetColor("_MainColor", Color.Lerp(_idleColor, _highlightedColor, _currentTransition));
            _storedMaterial.mainTextureScale = new Vector2(1, Mathf.Lerp(_idleSize, _highlightedSize, _currentTransition));
        }

        public virtual void OnDisable()
        {
            ResetPoint();
        }

        public virtual bool IsValid()
        {
            return true;
        }

        protected virtual void ResetPoint()
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
            _rotationIndicators.rotation = initialRotationIndicatorsRotation;
        }

        public void IndicateRotation(Quaternion newRotation)
        {
            _rotationIndicators.rotation = newRotation;
        }
    }
}