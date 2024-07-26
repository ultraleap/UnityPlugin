/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.PhysicalHands;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Leap.Examples
{
    public class KeyboardManager : MonoBehaviour
    {
        private Dictionary<PhysicalHandsButton, char> keysAndCharacters = new Dictionary<PhysicalHandsButton, char>();

        public TextMeshProUGUI textMeshProText;

        public List<PhysicalHandsButton> Buttons;
        public PhysicalHandsButton Backspace;
        public PhysicalHandsButton Shift;
        public PhysicalHandsButton Clear;

        public string EnteredText = "";


#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Buttons == null || Buttons.Count == 0)
            {
                foreach (KeyboardKey button in this.GetComponentsInChildren<KeyboardKey>())
                {
                    Buttons.Add(button.GetComponent<PhysicalHandsButton>());
                }
            }
        }
#endif

        void Start()
        {
            for (int i = 0; i < Buttons.Count; i++)
            {
                keysAndCharacters.Add(Buttons[i], Buttons[i].GetComponent<KeyboardKey>().keyCharacter);
            }

            foreach (var buttonCharPair in keysAndCharacters)
            {
                buttonCharPair.Key.OnButtonPressed.AddListener(delegate { AcceptKeyInput(buttonCharPair.Value); });
            }

            if (Backspace != null)
            {
                Backspace.OnButtonPressed.AddListener(DeleteCharacter);
            }

            if (Clear != null)
            {
                Clear.OnButtonPressed.AddListener(ClearText);
            }
        }

        void AcceptKeyInput(char character)
        {
            character = char.ToLower(character);

            if (Shift != null && Shift.IsPressed)
            {
                character = char.ToUpper(character);
            }

            EnteredText = EnteredText + character;

            AppendText();
        }

        private void DeleteCharacter()
        {
            int toDelete = EnteredText.Length - 1;
            if (toDelete < 0)
            {
                toDelete = 0;
            }
            EnteredText = EnteredText.Remove(toDelete);

            AppendText();
        }

        private void ClearText()
        {
            EnteredText = "";

            AppendText();
        }

        private void AppendText()
        {
            textMeshProText.SetText(EnteredText);
        }
    }
}