/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
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
        [SerializeField, Tooltip("Which objects should be toggled when 'ToggleMenu()' is called?")]
        List<GameObject> ExpandingMenuObjects = new List<GameObject>();

        bool ButtonsEnabled = false;

        // Start is called before the first frame update
        void Start()
        {
            foreach (var button in ExpandingMenuObjects)
            {
                button.SetActive(false);
            }
        }

        public void ToggleMenu()
        {
            foreach (var button in ExpandingMenuObjects)
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