/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Leap.Unity.PhysicalHands.Examples
{
    public class SceneReset : MonoBehaviour
    {
        public Color ButtonActiveColor;
        public Color ButtonInActiveColor;

        public GameObject HardContactButton;
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
        }

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
