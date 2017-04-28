/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace Leap.Unity.InputModule {
  /** Supports layer-based, compressible animations that lend a 3D affordance to otherwise flat UI elements.
    * The CompressibleUI script lets you separate the components of a UI and individual controls into 
    * floating layers that depress when the user touches them. The CompressibleUI can make it easier for a 
    * user to use a control. 
    */
  public class CompressibleUI : MonoBehaviour, ILeapWidget {
    [Tooltip("A list of RectTransforms that are floated relative to this GameObject.")]
    /** The layers created for this CompressibleUI instance. All layers move together, but can have different float distances. */
    public Layer[] Layers;

    /** The properties of a CompressibleUI layer. */
    [System.Serializable]
    public struct Layer {
      [HideInInspector]
      /** A name for the layer. */
      public string Label;

      [Tooltip("The child UI Element to hover above the canvas")]
      /** The child UI Element to float above the parent element. */
      public RectTransform LayerTransform;
      [Tooltip("The height above this (base) element that the Layer will float")]
      /** The height above this (base) element that the Layer will float. */
      public float MaxFloatDistance;
      [Tooltip("The minimum height that this layer can be compressed to.")]
      /** The minimum height that this layer can be compressed to. */
      public float MinFloatDistance;
      [Tooltip("OPTIONAL: If you have a dropshadow image that you would like to opacity fade on compression, add one here")]
      /** An optional dropshadow image component. */
      public UnityEngine.UI.Image Shadow;
      [Tooltip("If the shadow effect is not childed to this layer, but the layer above it (for masking purposes)")]
      /** Whether the shadow effect is not a child of this layer, but rather, a child of the layer above it (for masking purposes). */
      public bool ShadowOnAboveLayer;
      [Tooltip("If the event is triggered upon touching this layer (useful for ratcheted sounds)")]
      /** Whether a layer event is triggered upon touching this layer. */
      public bool TriggerLayerEvent;

      [HideInInspector]
      /** The maximum value used for drop shadow opacity. */
      public float MaxShadowOpacity;
      [HideInInspector]
      /** The current distance at which members of the layer are floating. */
      public float CurrentFloatingDistance;
      [HideInInspector]
      /** Whether or not a finger is touching members of this layer. */
      public bool touchingFinger;
      [HideInInspector]
      /** The distance to the parent layer. */
      public float distanceToAboveLayer;
      [HideInInspector]
      /** The maximum allowed distance to the parent layer. */
      public float maxDistanceToAboveLayer;
    }

    [Tooltip("The movement speed of this element when the expansion event is triggered; between 0-1")]
    /** How fast the layer will move to its distended position. */
    public float ExpandSpeed = 0.1f;
    [Tooltip("The movement speed of this element when the compression event is triggered; between 0-1")]
    /** How fast the layer will move to its retracted position. */
    public float ContractSpeed = 0.1f;
    [Tooltip("Padding below the selection threshold that the element begins depressing")]
    /** Padding added before a control enters the hovered state. */
    public float PushPaddingDistance = 0.01f;
    //public bool RetractWhenOutsideofTouchingDistance = false;

    [Tooltip("Triggered when the layers that have 'TriggerLayerEvent' enabled go from 'Expanded' to 'Partially Expanded'")]
    /** Dispatched when the layer is depressed, if TriggerLayerEvent is set to true. */
    public UnityEvent LayerDepress;
    [Tooltip("Triggered when the layers that have 'TriggerLayerEvent' enabled go from 'Expanded' or 'Partially Expanded' to 'Collapsed'")]
    /** Dispatched when the layer retracts, if TriggerLayerEvent is set to true. */
    public UnityEvent LayerCollapse;
    [Tooltip("Triggered when the layers that have 'TriggerLayerEvent' enabled go from 'Collapsed' to 'Partially Expanded' or 'Expanded'")]
    /** Dispatched when the layer expands, if TriggerLayerEvent is set to true. */
    public UnityEvent LayerExpand;

    //How quickly the button layers are Lerping
    private float curLerpSpeed = 0.1f;

    //How far the finger is from the base of the button
    private float HoveringDistance = 0f;

    //Whether or not the buttons are currently in float mode
    private bool currentlyFloating = false;

    private float TimeLastHovered = 0f;

    //Reset the Positions of the UI Elements on both Start and Quit
    void Start() {
      for (int i = 0; i < Layers.Length; i++) {
        if (Layers[i].LayerTransform != null && Layers[i].LayerTransform != transform) {
          Layers[i].LayerTransform.localPosition = new Vector3(Layers[i].LayerTransform.localPosition.x, Layers[i].LayerTransform.localPosition.y, 0f);

          if (Layers[i].Shadow != null) {
            Layers[i].MaxShadowOpacity = Layers[i].Shadow.color.a;
            Layers[i].Shadow.color = new Color(Layers[i].Shadow.color.r, Layers[i].Shadow.color.g, Layers[i].Shadow.color.b, 0f);
          }
        } else {
          Debug.LogWarning("Ensure that the layers that you have allotted are children of CompressibleUI object and have UI Elements in them!");
        }
      }
      //if (!RetractWhenOutsideofTouchingDistance) {
        Expand();
      //}
    }

    void OnApplicationQuit() {
      for (int i = 0; i < Layers.Length; i++) {
        if (Layers[i].LayerTransform != null) {
          Layers[i].LayerTransform.localPosition = new Vector3(Layers[i].LayerTransform.localPosition.x, Layers[i].LayerTransform.localPosition.y, 0f);
        }
      }
    }

    void Update() {
      //Reset Hovering Distance when "HoverDistance" isn't being called
      if (Time.time > TimeLastHovered + 0.1f && HoveringDistance != 100f) {
        HoveringDistance = 100f;
      }
      for (int i = 0; i < Layers.Length; i++) {
        //Only float the UI Elements when a hand is near a set of buttons...
        if (currentlyFloating) {
          if (Layers[i].LayerTransform != null) {
            if (HoveringDistance < Layers[i].MaxFloatDistance && HoveringDistance > Layers[i].MinFloatDistance) {
              Layers[i].CurrentFloatingDistance = Mathf.Lerp(Layers[i].CurrentFloatingDistance, HoveringDistance, 0.7f); //Set lower than 1f for delayed touching
              if (Layers[i].TriggerLayerEvent && !Layers[i].touchingFinger) {
                Layers[i].touchingFinger = true;
                LayerDepress.Invoke();
              }
            } else if (HoveringDistance < Layers[i].MinFloatDistance) {
              if (Layers[i].TriggerLayerEvent) {
                if (!Layers[i].touchingFinger) {
                  Layers[i].touchingFinger = true;
                }
                if (Layers[i].CurrentFloatingDistance > Layers[i].MinFloatDistance) {
                  LayerCollapse.Invoke();
                }
              }
              Layers[i].CurrentFloatingDistance = Layers[i].MinFloatDistance;
            } else {
              Layers[i].CurrentFloatingDistance = Mathf.Lerp(Layers[i].CurrentFloatingDistance, Layers[i].MaxFloatDistance, curLerpSpeed);
              if (Layers[i].TriggerLayerEvent && Layers[i].touchingFinger) {
                Layers[i].touchingFinger = false;
                LayerExpand.Invoke();
              }
            }
          }
          //else Just lay them flat so they're not bothering any cursors.
        } else {
          if (Layers[i].LayerTransform != null) {
            Layers[i].CurrentFloatingDistance = Mathf.Lerp(Layers[i].CurrentFloatingDistance, 0f, curLerpSpeed);
            if (Layers[i].TriggerLayerEvent && Layers[i].touchingFinger) {
              Layers[i].touchingFinger = false;
            }
          }
        }

        //If we have a shadow, let's lerp its opacity based on this element's distance to the layer above.
        if (Layers[i].Shadow != null) {
          if (Layers[i].ShadowOnAboveLayer) {
            if (i == 0) {
              Layers[0].distanceToAboveLayer = Layers[0].CurrentFloatingDistance;
              Layers[0].maxDistanceToAboveLayer = Layers[0].MaxFloatDistance;
            } else {
              Layers[i].distanceToAboveLayer = Layers[i].CurrentFloatingDistance - Layers[i - 1].CurrentFloatingDistance;
              Layers[i].maxDistanceToAboveLayer = Layers[i].MaxFloatDistance - Layers[i - 1].MaxFloatDistance;
            }
            Layers[i].Shadow.color = new Color(Layers[i].Shadow.color.r, Layers[i].Shadow.color.g, Layers[i].Shadow.color.b, Layers[i].distanceToAboveLayer.Remap(0f, Layers[i].maxDistanceToAboveLayer, 0f, Layers[i].MaxShadowOpacity));
          } else {
            Layers[i].Shadow.color = new Color(Layers[i].Shadow.color.r, Layers[i].Shadow.color.g, Layers[i].Shadow.color.b, Layers[i].CurrentFloatingDistance.Remap(Layers[i].MinFloatDistance, Layers[i].MaxFloatDistance, 0f, Layers[i].MaxShadowOpacity));
          }
        }
        if (Layers[i].LayerTransform != null) {
          Vector3 LocalPosition = Layers[i].LayerTransform.parent.InverseTransformPoint(transform.TransformPoint(new Vector3(0f, 0f, -Layers[i].CurrentFloatingDistance / transform.lossyScale.z)));
          Layers[i].LayerTransform.localPosition = new Vector3(Layers[i].LayerTransform.localPosition.x, Layers[i].LayerTransform.localPosition.y, LocalPosition.z);
        }
      }
    }

    /** Manually sets the current hover distance.  
     *  @param distance the distance above the base of the button in millimeters.
     */
    public void HoverDistance(float distance) {
      HoveringDistance = distance - PushPaddingDistance;
      TimeLastHovered = Time.time;
    }

    /** Move the layer members to their, extended, floating positions. */
    public void Expand() {
      currentlyFloating = true;
      curLerpSpeed = ExpandSpeed;
    }

    /** Restore the layer members to their non-floating positions. */
    public void Retract() {
      //if (RetractWhenOutsideofTouchingDistance) {
        currentlyFloating = false;
        curLerpSpeed = ContractSpeed;
      //}
    }

    public void DivideLayerHeightsOnToggle(Toggle toggle) {
      if (toggle.isOn) {
        for (int i = 0; i < Layers.Length; i++) {
          Layers[i].MinFloatDistance /= 2f;
          Layers[i].MaxFloatDistance /= 2f;
        }
      } else {
        for (int i = 0; i < Layers.Length; i++) {
          Layers[i].MinFloatDistance *= 2f;
          Layers[i].MaxFloatDistance *= 2f;
        }
      }
    }

    void OnValidate() {
      for (int i = 0; i < Layers.Length; i++) {
        if (Layers[i].LayerTransform != null) {
          Layers[i].Label = Layers[i].LayerTransform.gameObject.name + " Layer";
        }
      }
    }
  }

  public static class ExtensionMethods {
    public static float Remap(this float value, float from1, float to1, float from2, float to2) {
      return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
  }
}
