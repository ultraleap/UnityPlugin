/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Leap.PhysicalHands.Examples
{
    /// <summary>
    /// This class is very specific for the Ultraleap demo scene and should not be used in your own scenes
    /// </summary>
    public class SceneReset : MonoBehaviour
    {
        [SerializeField, Tooltip("When button is active, which colour should be used?")]
        private Color ButtonActiveColor;
        [SerializeField, Tooltip("When button is inactive, which colour should be used?")]
        private Color ButtonInActiveColor;

        [SerializeField, Tooltip("Hard contact button which should change color")]
        public GameObject HardContactButton;
        [SerializeField, Tooltip("Soft contact button which should change color")]
        public GameObject SoftContactButton;

        public enum SceneContactMode
        {
            HardContact = 0,
            SoftContact = 1
        }

        /// <summary>
        /// Get the active scene and reload it.
        /// </summary>
        public void ResetScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name);
        }

        public void ExitApplication()
        {
            Application.Quit();
        }

        private void Start()
        {
            SceneActiveContactModeChanged((int)SceneContactMode.HardContact);
            HardContactButton.GetComponent<PhysicalHandsButtonToggle>().SetTogglePressed();
        }

        /// <summary>
        /// Set colors for the buttons to show which one is currently active
        /// </summary>
        /// <param name="contactMode"></param>
        public void SceneActiveContactModeChanged(int contactMode)
        {
            switch (contactMode)
            {
                case (int)SceneContactMode.HardContact:
                    {
                        HardContactButton.GetComponent<MeshRenderer>().material.color = ButtonActiveColor;
                        SoftContactButton.GetComponent<MeshRenderer>().material.color = ButtonInActiveColor;

                        break;
                    }
                case (int)SceneContactMode.SoftContact:
                    {
                        SoftContactButton.GetComponent<MeshRenderer>().material.color = ButtonActiveColor;
                        HardContactButton.GetComponent<MeshRenderer>().material.color = ButtonInActiveColor;

                        break;
                    }
            }
        }

    }
}