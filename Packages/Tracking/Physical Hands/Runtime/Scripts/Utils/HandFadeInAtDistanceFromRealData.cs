using UnityEngine;

namespace Leap.Unity.PhysicalHands
{
    public class HandFadeInAtDistanceFromRealData : MonoBehaviour
    {
        public Chirality chirality;

        PhysicalHandsManager physManager;
        Renderer rendererToChange;
        HardContactHand hardContactHand;

        public void Init()
        {
            FindContactHand();

            rendererToChange = GetComponentInChildren<Renderer>();

            Vector4 currentColor = rendererToChange.material.GetVector("_Color");
            currentColor[3] = 0;
            rendererToChange.material.SetVector("_Color", currentColor);
        }

        void Update()
        {
            FindContactHand();

            if(hardContactHand != null && rendererToChange != null)
            {
                Vector4 currentColor = rendererToChange.material.GetVector("_Color");
                float mappedData = Utils.map01(hardContactHand.DistanceFromDataHand, 0, (hardContactHand.contactParent as HardContactParent).teleportDistance);
                currentColor[3] = Mathf.Clamp01(mappedData) + 0.05f;
                rendererToChange.material.SetVector("_Color", currentColor);

                if(currentColor[3] < 0.1f)
                {
                    rendererToChange.enabled = false;
                }
                else
                {
                    rendererToChange.enabled = true;
                }
            }
        }

        void FindContactHand()
        {
            if(hardContactHand != null)
            {
                return;
            }

            if(physManager == null)
            {
               physManager = FindObjectOfType<PhysicalHandsManager>();
            }

            if(physManager != null)
            {
                if (chirality == Chirality.Left)
                {
                    hardContactHand = physManager.ContactParent.LeftHand as HardContactHand;
                }
                else if (chirality == Chirality.Right)
                {
                    hardContactHand = physManager.ContactParent.RightHand as HardContactHand;
                }
            }
        }
    }
}