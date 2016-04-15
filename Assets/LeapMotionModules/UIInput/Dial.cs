using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityEngine.UI {
    [AddComponentMenu("UI/Dial", 33)]
    [RequireComponent(typeof(RectTransform))]
    public class Dial : Selectable, IDragHandler, IInitializePotentialDragHandler, ICanvasElement {

        [Serializable]
        public class DialEvent : UnityEvent<float> { }

        [SerializeField]
        private RectTransform m_FillRect;
        public RectTransform fillRect { get { return m_FillRect; } set { if (SetPropertyUtility.SetClass(ref m_FillRect, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        [SerializeField]
        private RectTransform m_HandleRect;
        public RectTransform handleRect { get { return m_HandleRect; } set { if (SetPropertyUtility.SetClass(ref m_HandleRect, value)) { UpdateCachedReferences(); UpdateVisuals(); } } }

        [Space]

        [SerializeField]
        private float m_MinValue = 0;
        public float minValue { get { return m_MinValue; } set { if (SetPropertyUtility.SetStruct(ref m_MinValue, value)) { Set(m_Value); UpdateVisuals(); } } }

        [SerializeField]
        private float m_MaxValue = 1;
        public float maxValue { get { return m_MaxValue; } set { if (SetPropertyUtility.SetStruct(ref m_MaxValue, value)) { Set(m_Value); UpdateVisuals(); } } }

        [SerializeField]
        private bool m_WholeNumbers = false;
        public bool wholeNumbers { get { return m_WholeNumbers; } set { if (SetPropertyUtility.SetStruct(ref m_WholeNumbers, value)) { Set(m_Value); UpdateVisuals(); } } }

        [SerializeField]
        protected float m_Value;
        public virtual float value
        {
            get
            {
                if (wholeNumbers)
                    return Mathf.Round(m_Value);
                return m_Value;
            }
            set
            {
                Set(value);
            }
        }

        public float normalizedValue
        {
            get
            {
                if (Mathf.Approximately(minValue, maxValue))
                    return 0;
                return Mathf.InverseLerp(minValue, maxValue, value);
            }
            set
            {
                this.value = Mathf.Lerp(minValue, maxValue, value);
            }
        }

        [Space]

        // Allow for delegate-based subscriptions for faster events than 'eventReceiver', and allowing for multiple receivers.
        [SerializeField]
        private DialEvent m_OnValueChanged = new DialEvent();
        public DialEvent onValueChanged { get { return m_OnValueChanged; } set { m_OnValueChanged = value; } }

        // Private fields

        private Image m_FillImage;
        private Transform m_FillTransform;
        private RectTransform m_FillContainerRect;
        private Transform m_HandleTransform;
        private RectTransform m_HandleContainerRect;

        // The offset from handle position to mouse down position
        private float m_Offset = 0f;

        private DrivenRectTransformTracker m_Tracker;

        // Size of each step.
        float stepSize { get { return wholeNumbers ? 1 : (maxValue - minValue) * 0.1f; } }

        protected Dial() { }

#if UNITY_EDITOR
        protected override void OnValidate() {
            base.OnValidate();

            if (wholeNumbers) {
                m_MinValue = Mathf.Round(m_MinValue);
                m_MaxValue = Mathf.Round(m_MaxValue);
            }

            //Onvalidate is called before OnEnabled. We need to make sure not to touch any other objects before OnEnable is run.
            if (IsActive()) {
                UpdateCachedReferences();
                Set(m_Value, false);
                // Update rects since other things might affect them even if value didn't change.
                UpdateVisuals();
            }

            var prefabType = UnityEditor.PrefabUtility.GetPrefabType(this);
            if (prefabType != UnityEditor.PrefabType.Prefab && !Application.isPlaying)
                CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
        }

#endif // if UNITY_EDITOR

        public virtual void Rebuild(CanvasUpdate executing) {
#if UNITY_EDITOR
            if (executing == CanvasUpdate.Prelayout)
                onValueChanged.Invoke(value);
#endif
        }

        public virtual void LayoutComplete() { }

        public virtual void GraphicUpdateComplete() { }

        protected override void OnEnable() {
            base.OnEnable();
            UpdateCachedReferences();
            Set(m_Value, false);
            // Update rects since they need to be initialized correctly.
            UpdateVisuals();
        }

        protected override void OnDisable() {
            m_Tracker.Clear();
            base.OnDisable();
        }

        protected override void OnDidApplyAnimationProperties() {
            // Has value changed? Various elements of the slider have the old normalisedValue assigned, we can use this to perform a comparison.
            // We also need to ensure the value stays within min/max.
            m_Value = ClampValue(m_Value);
            float oldNormalizedValue = normalizedValue;
            if (m_FillContainerRect != null) {
                if (m_FillImage != null && m_FillImage.type == Image.Type.Filled)
                    oldNormalizedValue = m_FillImage.fillAmount;
                else
                    oldNormalizedValue = (reverseValue ? 1f : 0f);
            } else if (m_HandleContainerRect != null)
                oldNormalizedValue = (reverseValue ? 1f : 0f);

            UpdateVisuals();

            if (oldNormalizedValue != normalizedValue)
                onValueChanged.Invoke(m_Value);
        }

        void UpdateCachedReferences() {
            if (m_FillRect) {
                m_FillTransform = m_FillRect.transform;
                m_FillImage = m_FillRect.GetComponent<Image>();
                if (m_FillTransform.parent != null)
                    m_FillContainerRect = m_FillTransform.parent.GetComponent<RectTransform>();
            } else {
                m_FillContainerRect = null;
                m_FillImage = null;
            }

            if (m_HandleRect) {
                m_HandleTransform = m_HandleRect.transform;
                if (m_HandleTransform.parent != null)
                    m_HandleContainerRect = m_HandleTransform.parent.GetComponent<RectTransform>();
            } else {
                m_HandleContainerRect = null;
            }
        }

        float ClampValue(float input) {
            float newValue = Mathf.Clamp(input, minValue, maxValue);
            if (wholeNumbers)
                newValue = Mathf.Round(newValue);
            return newValue;
        }

        // Set the valueUpdate the visible Image.
        void Set(float input) {
            Set(input, true);
        }

        protected virtual void Set(float input, bool sendCallback) {
            // Clamp the input
            float newValue = ClampValue(input);

            // If the stepped value doesn't match the last one, it's time to update
            if (m_Value == newValue)
                return;

            m_Value = newValue;
            UpdateVisuals();
            if (sendCallback)
                m_OnValueChanged.Invoke(newValue);
        }

        protected override void OnRectTransformDimensionsChange() {
            base.OnRectTransformDimensionsChange();

            //This can be invoked before OnEnabled is called. So we shouldn't be accessing other objects, before OnEnable is called.
            if (!IsActive())
                return;

            UpdateVisuals();
        }

        //Axis axis { get { return (m_Direction == Direction.LeftToRight || m_Direction == Direction.RightToLeft) ? Axis.Horizontal : Axis.Vertical; } }
        public bool reverseValue;

        // Force-update the slider. Useful if you've changed the properties and want it to update visually.
        private void UpdateVisuals() {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UpdateCachedReferences();
#endif

            m_Tracker.Clear();
            m_HandleRect.localRotation = Quaternion.Euler(new Vector3(m_HandleRect.localRotation.x, m_HandleRect.localRotation.y, (-normalizedValue * 360f) % 360f));

            /*
            if (m_FillContainerRect != null) {
                m_Tracker.Add(this, m_FillRect, DrivenTransformProperties.Anchors);
                float anchorMin = 0f;
                float anchorMax = 1f;

                if (m_FillImage != null && m_FillImage.type == Image.Type.Filled) {
                    m_FillImage.fillAmount = normalizedValue;
                } else {
                    if (reverseValue)
                        anchorMin = 1 - normalizedValue;
                    else
                        anchorMax = normalizedValue;
                }

                m_FillRect.anchorMin = new Vector2(anchorMin, 1f);
                m_FillRect.anchorMax = new Vector2(anchorMax, 1f);
            }

            if (m_HandleContainerRect != null) {
                m_Tracker.Add(this, m_HandleRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;
                anchorMin[0] = anchorMax[0] = (reverseValue ? (1 - normalizedValue) : normalizedValue);
                m_HandleRect.anchorMin = anchorMin;
                m_HandleRect.anchorMax = anchorMax;
            }
            */
        }

        // Update the slider's position based on the mouse.
        void UpdateDrag(PointerEventData eventData, Camera cam) {
            RectTransform clickRect = m_HandleRect ?? m_HandleContainerRect ?? m_FillContainerRect;
            if (clickRect != null && clickRect.rect.size[0] > 0) {
                Vector2 localCursor;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(clickRect, eventData.position, cam, out localCursor))
                    return;
                //localCursor -= clickRect.rect.position;

                float val = Mathf.Clamp01(Mathf.Rad2Deg*(Mathf.Atan2(localCursor.x,localCursor.y)-m_Offset + ((float)Math.PI)) / 360f);
                normalizedValue = (reverseValue ? 1f - val : val);
            }
        }

        private bool MayDrag(PointerEventData eventData) {
            return IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left;
        }

        public override void OnPointerDown(PointerEventData eventData) {
            if (!MayDrag(eventData))
                return;

            base.OnPointerDown(eventData);

            m_Offset = 0f;
            if (m_HandleContainerRect != null && RectTransformUtility.RectangleContainsScreenPoint(m_HandleRect, eventData.position, eventData.enterEventCamera)) {
                Vector2 localMousePos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_HandleRect, eventData.position, eventData.pressEventCamera, out localMousePos)) {
                    m_Offset = Mathf.Rad2Deg*(Mathf.Atan2(localMousePos.x, localMousePos.y) + ((float)Math.PI))/360f;
                }
            }
        }

        public virtual void OnDrag(PointerEventData eventData) {
            if (!MayDrag(eventData))
                return;
            UpdateDrag(eventData, eventData.pressEventCamera);
        }

        public override void OnMove(AxisEventData eventData) {
            if (!IsActive() || !IsInteractable()) {
                base.OnMove(eventData);
                return;
            }

            if (FindSelectableOnLeft() == null)
                Set(reverseValue ? value + stepSize : value - stepSize);
            else
                base.OnMove(eventData);
        }

        public override Selectable FindSelectableOnLeft() {
            if (navigation.mode == Navigation.Mode.Automatic)
                return null;
            return base.FindSelectableOnLeft();
        }

        public override Selectable FindSelectableOnRight() {
            if (navigation.mode == Navigation.Mode.Automatic)
                return null;
            return base.FindSelectableOnRight();
        }

        public override Selectable FindSelectableOnUp() {
            if (navigation.mode == Navigation.Mode.Automatic)
                return null;
            return base.FindSelectableOnUp();
        }

        public override Selectable FindSelectableOnDown() {
            if (navigation.mode == Navigation.Mode.Automatic)
                return null;
            return base.FindSelectableOnDown();
        }

        public virtual void OnInitializePotentialDrag(PointerEventData eventData) {
            eventData.useDragThreshold = false;
        }
    }
}