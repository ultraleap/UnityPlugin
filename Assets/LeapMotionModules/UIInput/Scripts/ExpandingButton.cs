using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace UnityEngine.UI {
    [RequireComponent(typeof(RectTransform))]
    public class ExpandingButton : Button, ILeapWidget
    {
        [SerializeField]
        public GameObject LayerOne;
        [SerializeField]
        public GameObject LayerTwo;

        [SerializeField]
        public float LayerOneFloatDistance = 0.1f;
        [SerializeField]
        public float LayerTwoFloatDistance = 0.1f;

        [SerializeField]
        public float ExpandSpeed = 0.1f;
        [SerializeField]
        public float ContractSpeed = 0.1f;

        //How quickly the button layers are Lerping
        private float lerpSpeed = 0.1f;

        //CURRENT Floating Distances
        private float LayerOneFloatingDistance = 0f;
        private float LayerTwoFloatingDistance = 0f;

        //How far the finger is from the base of the button
        private float HoveringDistance = 0f;

        //Whether or not the button is currently in float mode
        private bool floating = false;

        private float LastTimeHovered = 0f;

        // Update is called once per frame
        void Update()
        {
            if (Time.time > LastTimeHovered + 0.1f)
            {
                HoveringDistance = 100f;
            }

            if (floating)
            {
                if (HoveringDistance < LayerOneFloatDistance)
                {
                    LayerTwoFloatingDistance = HoveringDistance;
                    LayerOneFloatingDistance = HoveringDistance;
                }
                else if (HoveringDistance < LayerTwoFloatDistance)
                {
                    LayerTwoFloatingDistance = HoveringDistance;
                    LayerOneFloatingDistance = LayerOneFloatDistance;
                }
                else
                {
                    LayerTwoFloatingDistance = LayerTwoFloatDistance;
                    LayerOneFloatingDistance = LayerOneFloatDistance;
                }
            }
            else
            {
                LayerTwoFloatingDistance = 0f;
                LayerOneFloatingDistance = 0f;
            }

            LayerTwoFloatingDistance = Mathf.Max(LayerTwoFloatingDistance, 0f);
            LayerOneFloatingDistance = Mathf.Max(LayerOneFloatingDistance, 0f);

            LayerOne.transform.localPosition = Vector3.Lerp(LayerOne.transform.localPosition, new Vector3(LayerOne.transform.localPosition.x, LayerOne.transform.localPosition.y, -LayerOneFloatingDistance / LayerOne.transform.lossyScale.z), lerpSpeed);
            LayerTwo.transform.localPosition = Vector3.Lerp(LayerTwo.transform.localPosition, new Vector3(LayerTwo.transform.localPosition.x, LayerTwo.transform.localPosition.y, -(LayerTwoFloatingDistance - LayerOneFloatingDistance) / LayerTwo.transform.lossyScale.z), lerpSpeed);
        }

        void OnApplicationQuit()
        {
            LayerOne.transform.localPosition = new Vector3(LayerOne.transform.localPosition.x, LayerOne.transform.localPosition.y, 0f);
            LayerTwo.transform.localPosition = new Vector3(LayerTwo.transform.localPosition.x, LayerTwo.transform.localPosition.y, 0f);
        }

        public void HoverDistance(float distance)
        {
            HoveringDistance = distance - 0.01f;

            LastTimeHovered = Time.time;
        }

        public void Expand()
        {
            floating = true;
            lerpSpeed = ExpandSpeed;
            LayerOneFloatingDistance = LayerOneFloatDistance;
            LayerTwoFloatingDistance = LayerTwoFloatDistance;
        }

        public void Retract()
        {
            floating = false;
            lerpSpeed = ContractSpeed;
            LayerOneFloatingDistance = 0f;
            LayerTwoFloatingDistance = 0f;
        }
    }
}
