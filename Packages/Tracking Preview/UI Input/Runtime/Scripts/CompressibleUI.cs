/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Leap.InputModule
{
    /// <summary>
    /// Supports layer-based, compressible animations that lend a 3D affordance to otherwise flat UI elements.
    /// The CompressibleUI script lets you separate the components of a UI and individual controls into 
    /// floating layers that depress when the user touches them. The CompressibleUI can make it easier for a 
    /// user to use a control. 
    /// </summary>
    public class CompressibleUI : MonoBehaviour, ILeapWidget
    {
        [Tooltip("A list of RectTransforms that are floated relative to this GameObject.")]
        public Layer[] Layers;

        [System.Serializable]
        public struct Layer
        {
            [Tooltip("The child UI Element to hover above the canvas")]
            public RectTransform LayerTransform;
            [Tooltip("The height above this (base) element that the Layer will float")]
            public float MaxFloatDistance;
            [Tooltip("The minimum height that this layer can be compressed to.")]
            public float MinFloatDistance;
            [Tooltip("OPTIONAL: If you have a dropshadow image that you would like to opacity fade on compression, add one here")]
            public UnityEngine.UI.Image Shadow;
            [Tooltip("If the shadow effect is not childed to this layer, but the layer above it (for masking purposes)")]
            public bool ShadowOnAboveLayer;
            [Tooltip("If the event is triggered upon touching this layer (useful for ratcheted sounds)")]
            public bool TriggerLayerEvent;

            [HideInInspector]
            public float MaxShadowOpacity;
            [HideInInspector]
            public float CurrentFloatingDistance;
            [HideInInspector]
            public bool touchingFinger;
            [HideInInspector]
            public float distanceToAboveLayer;
            [HideInInspector]
            public float maxDistanceToAboveLayer;
        }

        [Tooltip("The movement speed of this element when the expansion event is triggered; between 0-1")]
        public float ExpandSpeed = 0.1f;
        [Tooltip("The movement speed of this element when the compression event is triggered; between 0-1")]
        public float ContractSpeed = 0.1f;
        [Tooltip("Padding below the selection threshold that the element begins depressing")]
        public float PushPaddingDistance = 0.01f;

        [Tooltip("Triggered when the layers that have 'TriggerLayerEvent' enabled go from 'Expanded' to 'Partially Expanded'")]
        public UnityEvent LayerDepress;
        [Tooltip("Triggered when the layers that have 'TriggerLayerEvent' enabled go from 'Expanded' or 'Partially Expanded' to 'Collapsed'")]
        public UnityEvent LayerCollapse;
        [Tooltip("Triggered when the layers that have 'TriggerLayerEvent' enabled go from 'Collapsed' to 'Partially Expanded' or 'Expanded'")]
        public UnityEvent LayerExpand;

        //How quickly the button layers are Lerping
        private float curLerpSpeed = 0.1f;

        //How far the finger is from the base of the button
        private float HoveringDistance = 0f;

        //Whether or not the buttons are currently in float mode
        private bool currentlyFloating = false;

        private float TimeLastHovered = 0f;

        private void Start()
        {
            //Reset the Positions of the UI Elements on both Start and Quit
            for (var i = 0; i < Layers.Length; i++)
            {
                if (Layers[i].LayerTransform != null && Layers[i].LayerTransform != transform)
                {
                    Layers[i].LayerTransform.localPosition = new Vector3(Layers[i].LayerTransform.localPosition.x, Layers[i].LayerTransform.localPosition.y, 0f);

                    if (Layers[i].Shadow != null)
                    {
                        Layers[i].MaxShadowOpacity = Layers[i].Shadow.color.a;
                        Layers[i].Shadow.color = new Color(Layers[i].Shadow.color.r, Layers[i].Shadow.color.g, Layers[i].Shadow.color.b, 0f);
                    }
                }
                else
                {
                    Debug.LogWarning("Ensure that the layers that you have allotted are children of CompressibleUI object and have UI Elements in them!");
                }
            }

            Expand();
        }

        private void OnApplicationQuit()
        {
            for (int i = 0; i < Layers.Length; i++)
            {
                if (Layers[i].LayerTransform != null)
                {
                    Layers[i].LayerTransform.localPosition = new Vector3(Layers[i].LayerTransform.localPosition.x, Layers[i].LayerTransform.localPosition.y, 0f);
                }
            }
        }

        private void Update()
        {
            //Reset Hovering Distance when "HoverDistance" isn't being called
            if (Time.time > TimeLastHovered + 0.1f && HoveringDistance != 100f)
            {
                HoveringDistance = 100f;
            }
            for (int i = 0; i < Layers.Length; i++)
            {
                //Only float the UI Elements when a hand is near a set of buttons...
                if (currentlyFloating)
                {
                    if (Layers[i].LayerTransform != null)
                    {
                        if (HoveringDistance < Layers[i].MaxFloatDistance && HoveringDistance > Layers[i].MinFloatDistance)
                        {
                            Layers[i].CurrentFloatingDistance = Mathf.Lerp(Layers[i].CurrentFloatingDistance, HoveringDistance, 0.7f); //Set lower than 1f for delayed touching
                            if (Layers[i].TriggerLayerEvent && !Layers[i].touchingFinger)
                            {
                                Layers[i].touchingFinger = true;
                                LayerDepress.Invoke();
                            }
                        }
                        else if (HoveringDistance < Layers[i].MinFloatDistance)
                        {
                            if (Layers[i].TriggerLayerEvent)
                            {
                                if (!Layers[i].touchingFinger)
                                {
                                    Layers[i].touchingFinger = true;
                                }
                                if (Layers[i].CurrentFloatingDistance > Layers[i].MinFloatDistance)
                                {
                                    LayerCollapse.Invoke();
                                }
                            }
                            Layers[i].CurrentFloatingDistance = Layers[i].MinFloatDistance;
                        }
                        else
                        {
                            Layers[i].CurrentFloatingDistance = Mathf.Lerp(Layers[i].CurrentFloatingDistance, Layers[i].MaxFloatDistance, curLerpSpeed);
                            if (Layers[i].TriggerLayerEvent && Layers[i].touchingFinger)
                            {
                                Layers[i].touchingFinger = false;
                                LayerExpand.Invoke();
                            }
                        }
                    }
                }
                //Lay them flat so they're not bothering any cursors.
                else
                {
                    if (Layers[i].LayerTransform != null)
                    {
                        Layers[i].CurrentFloatingDistance = Mathf.Lerp(Layers[i].CurrentFloatingDistance, 0f, curLerpSpeed);
                        if (Layers[i].TriggerLayerEvent && Layers[i].touchingFinger)
                        {
                            Layers[i].touchingFinger = false;
                        }
                    }
                }

                //If we have a shadow, let's lerp its opacity based on this element's distance to the layer above.
                if (Layers[i].Shadow != null)
                {
                    if (Layers[i].ShadowOnAboveLayer)
                    {
                        if (i == 0)
                        {
                            Layers[0].distanceToAboveLayer = Layers[0].CurrentFloatingDistance;
                            Layers[0].maxDistanceToAboveLayer = Layers[0].MaxFloatDistance;
                        }
                        else
                        {
                            Layers[i].distanceToAboveLayer = Layers[i].CurrentFloatingDistance - Layers[i - 1].CurrentFloatingDistance;
                            Layers[i].maxDistanceToAboveLayer = Layers[i].MaxFloatDistance - Layers[i - 1].MaxFloatDistance;
                        }
                        Layers[i].Shadow.color = new Color(Layers[i].Shadow.color.r, Layers[i].Shadow.color.g, Layers[i].Shadow.color.b, Layers[i].distanceToAboveLayer.Remap(0f, Layers[i].maxDistanceToAboveLayer, 0f, Layers[i].MaxShadowOpacity));
                    }
                    else
                    {
                        Layers[i].Shadow.color = new Color(Layers[i].Shadow.color.r, Layers[i].Shadow.color.g, Layers[i].Shadow.color.b, Layers[i].CurrentFloatingDistance.Remap(Layers[i].MinFloatDistance, Layers[i].MaxFloatDistance, 0f, Layers[i].MaxShadowOpacity));
                    }
                }
                if (Layers[i].LayerTransform != null)
                {
                    Vector3 LocalPosition = Layers[i].LayerTransform.parent.InverseTransformPoint(transform.TransformPoint(new Vector3(0f, 0f, -Layers[i].CurrentFloatingDistance / transform.lossyScale.z)));
                    Layers[i].LayerTransform.localPosition = new Vector3(Layers[i].LayerTransform.localPosition.x, Layers[i].LayerTransform.localPosition.y, LocalPosition.z);
                }
            }
        }


        /// <summary>
        /// Manually sets the current hover distance.
        /// </summary>
        /// <param name="distance">Distance the distance above the base of the button in millimeters.</param>
        /// <returns></returns>
        public void HoverDistance(float distance)
        {
            HoveringDistance = distance - PushPaddingDistance;
            TimeLastHovered = Time.time;
        }

        /// <summary>
        /// Move the layer members to their, extended, floating positions.
        /// </summary>
        /// <returns></returns>
        public void Expand()
        {
            currentlyFloating = true;
            curLerpSpeed = ExpandSpeed;
        }

        /// <summary>
        /// Restore the layer members to their non-floating positions.
        /// </summary>
        /// <returns></returns>
        public void Retract()
        {
            //if (RetractWhenOutsideofTouchingDistance) {
            currentlyFloating = false;
            curLerpSpeed = ContractSpeed;
            //}
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="toggle"></param>
        public void DivideLayerHeightsOnToggle(Toggle toggle)
        {
            if (toggle.isOn)
            {
                for (int i = 0; i < Layers.Length; i++)
                {
                    Layers[i].MinFloatDistance /= 2f;
                    Layers[i].MaxFloatDistance /= 2f;
                }
            }
            else
            {
                for (int i = 0; i < Layers.Length; i++)
                {
                    Layers[i].MinFloatDistance *= 2f;
                    Layers[i].MaxFloatDistance *= 2f;
                }
            }
        }
    }

    public static class ExtensionMethods
    {
        public static float Remap(this float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
    }
}