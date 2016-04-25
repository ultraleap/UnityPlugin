using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(Selectable))]
public class CompressibleUI : MonoBehaviour, ILeapWidget
{
    public Layer[] Layers;
    [System.Serializable]
    public struct Layer
    {
        public RectTransform LayerTransform;
        public float MaxFloatDistance;
        public float MinFloatDistance;

        [HideInInspector]
        public float CurrentFloatingDistance;
    }

    [SerializeField]
    public float ExpandSpeed = 0.1f;
    [SerializeField]
    public float ContractSpeed = 0.1f;
    [SerializeField]
    public float PushPaddingDistance = 0.01f;

    //How quickly the button layers are Lerping
    private float curLerpSpeed = 0.1f;

    //How far the finger is from the base of the button
    private float HoveringDistance = 0f;

    //Whether or not the buttons are currently in float mode
    private bool currentlyFloating = false;

    private float TimeLastHovered = 0f;

    //Reset the Positions of the UI Elements on both Start and Quit
    //Because for some reason they're persistent between editor restarts?
    void Start()
    {
        for (int i = 0; i < Layers.Length; i++)
        {
            if (Layers[i].LayerTransform != null)
            {
                Vector3 LocalPosition = transform.InverseTransformPoint(Layers[i].LayerTransform.position);
                Layers[i].LayerTransform.position = transform.TransformPoint(new Vector3(LocalPosition.x, LocalPosition.y, 0f));
            }
            else
            {
                Debug.LogWarning("Ensure that the layers that you have allotted have UI Elements in them!");
            }
        }
    }

    void OnApplicationQuit()
    {
        for (int i = 0; i < Layers.Length; i++)
        {
            if (Layers[i].LayerTransform != null)
            {
                Vector3 LocalPosition = transform.InverseTransformPoint(Layers[i].LayerTransform.position);
                Layers[i].LayerTransform.position = transform.TransformPoint(new Vector3(LocalPosition.x, LocalPosition.y, 0f));
            }
        }
    }

    void Update()
    {
        //Reset Hovering Distance when "HoverDistance" isn't being called
        if (Time.time > TimeLastHovered + 0.1f && HoveringDistance != 100f)
        {
            HoveringDistance = 100f;
        }

        //Only float the UI Elements when a hand is near a set of buttons...
        if (currentlyFloating)
        {
            for (int i = 0; i < Layers.Length; i++)
            {
                if (Layers[i].LayerTransform != null)
                {
                    if (HoveringDistance < Layers[i].MaxFloatDistance && HoveringDistance > Layers[i].MinFloatDistance)
                    {
                        Layers[i].CurrentFloatingDistance = Mathf.Lerp(Layers[i].CurrentFloatingDistance, HoveringDistance, 0.2f); //Set to 1f for responsive touching...
                    }
                    else if (HoveringDistance < Layers[i].MinFloatDistance)
                    {
                        Layers[i].CurrentFloatingDistance = Mathf.Lerp(Layers[i].CurrentFloatingDistance, Layers[i].MinFloatDistance, curLerpSpeed);
                    }
                    else
                    {
                        Layers[i].CurrentFloatingDistance = Mathf.Lerp(Layers[i].CurrentFloatingDistance, Layers[i].MaxFloatDistance, curLerpSpeed);
                    }

                    Vector3 LocalPosition = transform.InverseTransformPoint(Layers[i].LayerTransform.position);
                    Layers[i].LayerTransform.position = transform.TransformPoint(new Vector3(LocalPosition.x, LocalPosition.y, -Layers[i].CurrentFloatingDistance / transform.lossyScale.z));
                }
            }
        }
        else
        {
            for (int i = 0; i < Layers.Length; i++)
            {
                if (Layers[i].LayerTransform != null)
                {
                    Layers[i].CurrentFloatingDistance = Mathf.Lerp(Layers[i].CurrentFloatingDistance, 0f, curLerpSpeed);

                    Vector3 LocalPosition = transform.InverseTransformPoint(Layers[i].LayerTransform.position);
                    Layers[i].LayerTransform.position = transform.TransformPoint(new Vector3(LocalPosition.x, LocalPosition.y, -Layers[i].CurrentFloatingDistance / transform.lossyScale.z));
                }
            }
        }
    }

    public void HoverDistance(float distance)
    {
        HoveringDistance = distance - PushPaddingDistance;
        TimeLastHovered = Time.time;
    }

    public void Expand()
    {
        currentlyFloating = true;
        curLerpSpeed = ExpandSpeed;
    }

    public void Retract()
    {
        currentlyFloating = false;
        curLerpSpeed = ContractSpeed;
    }
}
