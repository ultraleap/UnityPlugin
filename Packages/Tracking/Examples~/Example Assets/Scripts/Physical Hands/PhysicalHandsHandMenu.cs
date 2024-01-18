/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.PhysicalHands.Examples
{
    public class PhysicalHandsHandMenu : MonoBehaviour
    {
        [SerializeField]
        List<GameObject> ExpandingButtons = new List<GameObject>();
        Vector3 buttonScale = new Vector3();

        bool ButtonsEnabled = false;

        // Start is called before the first frame update
        void Start()
        {
            foreach (var button in ExpandingButtons)
            {
                button.SetActive(false);
                buttonScale = button.transform.localScale;
            }
        }

        public void ToggleMenu()
        {
            foreach (var button in ExpandingButtons)
            {
                if (ButtonsEnabled)
                {
                    button.SetActive(false);
                }
                else if (!ButtonsEnabled)
                {
                    button.SetActive(true);
                }
            }
            ButtonsEnabled = !ButtonsEnabled;
        }
    }
}
