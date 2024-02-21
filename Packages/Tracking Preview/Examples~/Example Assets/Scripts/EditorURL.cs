/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Examples.Preview
{
    /// <summary>
    /// A script to display GUI displaying a URL in the Game window during editor
    /// </summary>
    [ExecuteAlways]
    public class EditorURL : MonoBehaviour
    {
        public string url = "https://docs.ultraleap.com/ultralab/";
        public GUISkin skin;
        public Texture logo;

        int fontSize = 25;
        float logoSize = 15;
        Color32 highlightColor = new Color32(0, 235, 133, 255);

        Texture2D background;

        void Awake()
        {
            background = new Texture2D(0, 0);
        }

        /// <summary>
        /// Draw GUI to the screen
        /// </summary>
        private void OnGUI()
        {
            if (skin == null)
            {
                return;
            }

            AdjustSkin(skin);
            DrawURLGUI(url);
            DrawLogo();
        }

        /// <summary>
        /// Adjust the GUI skin to display a transparent background
        /// </summary>
        /// <param name="skin"></param>
        void AdjustSkin(GUISkin skin)
        {
            skin.button.normal.background = background;
            skin.button.hover.background = background;
            skin.button.hover.textColor = highlightColor;
        }

        /// <summary>
        /// Draw a URL to the screen using GUI
        /// </summary>
        /// <param name="url"></param>
        void DrawURLGUI(string url)
        {
            GUILayout.BeginArea(CalculateURLRect());
            if (GUILayout.Button(url, skin.button))
            {
                Application.OpenURL(url);
            }
            GUILayout.EndArea();
        }

        /// <summary>
        /// Draw the logo to the screen using GUI
        /// </summary>
        void DrawLogo()
        {
            GUILayout.BeginArea(CalculateLogoRect());
            GUILayout.Label(logo);
            GUILayout.EndArea();
        }

        /// <summary>
        /// Calculate a Rect to display a URL
        /// </summary>
        /// <returns></returns>
        Rect CalculateURLRect()
        {
            var dif = Screen.width - Screen.height;
            if (dif >= 0)
            {
                skin.button.fontSize = Screen.height / fontSize;
                return new Rect(0, 0, Screen.width, Screen.height / (fontSize / 2));
            }
            else
            {
                skin.button.fontSize = Screen.width / fontSize;
                return new Rect(0, 0, Screen.width - Screen.height / fontSize, Screen.height / (fontSize / 2));
            }
        }

        /// <summary>
        /// Calculate a Rect to display a logo
        /// </summary>
        /// <returns></returns>
        Rect CalculateLogoRect()
        {
            var dif = Screen.width - Screen.height;
            if (dif >= 0)
            {
                return new Rect((Screen.width - Screen.width / logoSize), 0, Screen.width / logoSize, Screen.width / logoSize);

            }
            else
            {
                return new Rect((Screen.width - Screen.height / logoSize), 0, Screen.height / logoSize, Screen.height / logoSize);
            }
        }
    }
}