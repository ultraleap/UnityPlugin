/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace UHI.Tracking.InteractionEngine.Examples
{

    [RequireComponent(typeof(Renderer))]
    public class SimpleRendererUtil : MonoBehaviour
    {

        public Color activationColor = Color.yellow;

        private Renderer _renderer;
        private Material _materialInstance;

        private Color _originalColor;

        void Start()
        {
            _renderer = GetComponent<Renderer>();
            _materialInstance = _renderer.material;
            _originalColor = _materialInstance.color;
        }

        public void SetToActivationColor()
        {
            _materialInstance.color = activationColor;
        }

        public void SetToOriginalColor()
        {
            _materialInstance.color = _originalColor;
        }

        public void ShowRenderer()
        {
            _renderer.enabled = true;
        }

        public void HideRenderer()
        {
            _renderer.enabled = false;
        }

    }

}