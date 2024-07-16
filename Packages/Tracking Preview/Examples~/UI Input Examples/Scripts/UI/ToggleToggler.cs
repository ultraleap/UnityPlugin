/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.UI;

namespace Leap.InputModule
{
    public class ToggleToggler : MonoBehaviour
    {
        public Text text;
        public UnityEngine.UI.Image image;
        public Color OnColor;
        public Color OffColor;

        public void SetToggle(Toggle toggle)
        {
            if (toggle.isOn)
            {
                text.text = "On";
                text.color = Color.white;
                image.color = OnColor;
            }
            else
            {
                text.text = "Off";
                text.color = new Color(0.3f, 0.3f, 0.3f);
                image.color = OffColor;
            }
        }
    }
}