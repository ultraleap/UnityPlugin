using UnityEngine;


namespace Leap.Unity.PhysicalHands
{
    public class HandFadeInAtDistanceFromRealData : MonoBehaviour
    {
        PhysicalHandsManager physManager;
        public Chirality chirality;
        public Renderer rendererToChange;

        HardContactHand hardContactHand;

        // Start is called before the first frame update
        public void Init()
        {
            physManager = FindObjectOfType<PhysicalHandsManager>();
            if (physManager.contactMode == PhysicalHandsManager.ContactMode.HardContact)
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

            rendererToChange.material.SetVector("_OutlineColor", new Color(0.2f, 0.2f, 0.2f, 1));
            rendererToChange.material.SetInt("_useFresnel", 0);
            rendererToChange.material.SetInt("_useLighting", 0);
        }

        bool firstFrame = true;

        // Update is called once per frame
        void Update()
        {
            if (physManager.contactMode == PhysicalHandsManager.ContactMode.HardContact)
            {
                if (hardContactHand == null)
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


                if (firstFrame)
                {
                    physManager = FindObjectOfType<PhysicalHandsManager>();
                    if (chirality == Chirality.Left)
                    {
                        hardContactHand = physManager.ContactParent.LeftHand as HardContactHand;
                    }
                    else if (chirality == Chirality.Right)
                    {
                        hardContactHand = physManager.ContactParent.RightHand as HardContactHand;
                    }
                    firstFrame = false;
                }

                var currentColor = rendererToChange.material.GetVector("_OutlineColor");
                var mappedData = Utils.map01(hardContactHand.DistanceFromDataHand, 0, (hardContactHand.contactParent as HardContactParent).teleportDistance);
                currentColor[3] = Mathf.Clamp01(mappedData)+0.05f;
                rendererToChange.material.SetVector("_OutlineColor", currentColor);

            }

        }
    }
}
