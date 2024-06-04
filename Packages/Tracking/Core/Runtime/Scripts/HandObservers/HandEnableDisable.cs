/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Attributes;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity
{
    /// <summary>
    /// A component to be attached to a HandModelBase to handle starting and ending of
    /// tracking. 
    /// The parent gameobjet is activated when tracking begins and deactivated when
    /// tracking ends.
    /// </summary>
    public class HandEnableDisable : MonoBehaviour
    {
        public LeapProvider leapProvider;
        public Chirality chirality;

        [Space, Tooltip("Should this GameObject begin disabled?")]
        public bool disableOnAwake = true;

        [Tooltip("When enabled, freezes the hand in its current active state")]
        public bool FreezeHandState = false;

        [Header("Fading")]
        public bool fadeOnHandFound = false;
        [Indent, Units("Seconds")]
        public float fadeInTime = 0.1f;

        public bool fadeOnHandLost = true;
        [Indent, Units("Seconds")]
        public float fadeOutTime = 0.1f;

        [Space, Tooltip("Show options to reference specific Renderers, Materials and Color parameters to fade. \n\nWhen not enabled, hand renderers are automatically detected.")]
        public bool customFadeRenderers;
        public RendererMaterialColorReference[] renderersToFade;

        bool fadingIn = false;
        bool fadingOut = false;

        float fadeEndTime;

        private void Awake()
        {
            if (leapProvider == null)
            {
                var handModelBase = GetComponent<HandModelBase>();

                if (handModelBase != null)
                {
                    leapProvider = handModelBase.leapProvider;
                    chirality = handModelBase.Handedness;
                }
            }

            leapProvider.OnHandFound -= HandFound;
            leapProvider.OnHandFound += HandFound;

            leapProvider.OnHandLost -= HandLost;
            leapProvider.OnHandLost += HandLost;

            this.gameObject.SetActive(disableOnAwake ? false : this.gameObject.activeInHierarchy);
        }

        private void Start()
        {
            if(!customFadeRenderers)
            {
                AutoPopulateFadeRenderers();
            }

            PopulateFadeDefaultAlphas();
        }

        private void OnDestroy()
        {
            if (leapProvider == null)
            {
                return;
            }

            leapProvider.OnHandFound -= HandFound;
            leapProvider.OnHandLost -= HandLost;
        }

        private void OnValidate()
        {
            if (leapProvider == null)
            {
                var handModelBase = GetComponent<HandModelBase>();

                if (handModelBase != null)
                {
                    leapProvider = handModelBase.leapProvider;
                    chirality = handModelBase.Handedness;
                }
            }
        }

        private void HandFound(Chirality foundChirality)
        {
            if (FreezeHandState || foundChirality != chirality)
            {
                return;
            }

            fadingOut = false;
            gameObject.SetActive(true);

            if (fadeOnHandFound)
            {
                fadingIn = true;
                CacheFadeStartAlphas();
                fadeEndTime = Time.time + fadeInTime;
            }
            else
            {
                if (fadeEndTime != 0)
                {
                    // We have faded at some point. Reset the fade
                    foreach (var _renderRef in renderersToFade)
                    {
                        for (int i = 0; i < _renderRef.colorParamNames.Length; i++)
                        {
                            Color _col = _renderRef.renderer.materials[_renderRef.materialID].GetColor(_renderRef.colorParamNames[i]);
                            _col.a = _renderRef.defaultAlphas[i];

                            _renderRef.renderer.materials[_renderRef.materialID].SetColor(_renderRef.colorParamNames[i], _col);
                        }
                    }
                }
            }
        }

        private void HandLost(Chirality lostChirality)
        {
            if (FreezeHandState || lostChirality != chirality)
            {
                return;
            }

            fadingIn = false;

            if (fadeOnHandLost)
            {
                fadingOut = true;
                CacheFadeStartAlphas();
                fadeEndTime = Time.time + fadeOutTime;
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if(fadingOut)
            {
                HandleFade(_fadeIn: false); // Handle the fade out

                if (Time.time > fadeEndTime)
                {
                    fadingOut = false;
                    gameObject.SetActive(false);
                }
            }

            if (fadingIn)
            {
                HandleFade(_fadeIn: true); // Handle the fade in

                if (Time.time > fadeEndTime)
                {
                    fadingIn = false;
                }
            }
        }

        private void HandleFade(bool _fadeIn = false)
        {
            foreach (var _renderRef in renderersToFade)
            {
                for (int i = 0; i < _renderRef.colorParamNames.Length; i++)
                {
                    float _t = 0;

                    if(_fadeIn)
                    {
                        _t = Utils.Map(fadeInTime - (fadeEndTime - Time.time), 0f, fadeInTime, 0f, 1f);
                    }
                    else
                    {
                        _t = Utils.Map(fadeOutTime - (fadeEndTime - Time.time), 0f, fadeOutTime, 0f, 1f);
                    }

                    Color _col = _renderRef.renderer.materials[_renderRef.materialID].GetColor(_renderRef.colorParamNames[i]);

                    if (_fadeIn)
                    {
                        _col.a = Mathf.Lerp(_renderRef.fadeStartAlphas[i], _renderRef.defaultAlphas[i], _t); // Fade to default alpha
                    }
                    else
                    {
                        _col.a = Mathf.Lerp(_renderRef.fadeStartAlphas[i], 0, _t); // Fade to 0
                    }

                    _renderRef.renderer.materials[_renderRef.materialID].SetColor(_renderRef.colorParamNames[i], _col);
                }
            }
        }

        private void CacheFadeStartAlphas()
        {
            foreach (var _renderRef in renderersToFade)
            {
                _renderRef.fadeStartAlphas = new float[_renderRef.colorParamNames.Length];

                for (int i = 0; i < _renderRef.colorParamNames.Length; i++)
                {
                    _renderRef.fadeStartAlphas[i] = _renderRef.renderer.materials[_renderRef.materialID].GetColor(_renderRef.colorParamNames[i]).a;
                }
            }
        }

        private void AutoPopulateFadeRenderers()
        {
            Renderer[] _renderers = gameObject.GetComponentsInChildren<Renderer>(true);

            renderersToFade = new RendererMaterialColorReference[_renderers.Length];

            for (int i = 0; i < _renderers.Length; i++)
            {
                renderersToFade[i] = new RendererMaterialColorReference();
                renderersToFade[i].renderer = _renderers[i];
                renderersToFade[i].materialID = 0;

                List<string> _suitableParams = new List<string>
                    {
                        "_Color","_MainColor","_OutlineColor","_FresnelColor"
                    };

                for (int _paramID = 0; _paramID < _suitableParams.Count; _paramID++)
                {
                    if (!_renderers[i].materials[0].HasColor(_suitableParams[_paramID]))
                    {
                        _suitableParams.RemoveAt(_paramID);
                        _paramID--;
                    }
                }

                renderersToFade[i].colorParamNames = _suitableParams.ToArray();
            }
        }

        private void PopulateFadeDefaultAlphas()
        {
            foreach (var _renderRef in renderersToFade)
            {
                _renderRef.defaultAlphas = new float[_renderRef.colorParamNames.Length];

                for (int i = 0; i < _renderRef.colorParamNames.Length; i++)
                {
                    _renderRef.defaultAlphas[i] = _renderRef.renderer.materials[_renderRef.materialID].GetColor(_renderRef.colorParamNames[i]).a;
                }
            }

            CacheFadeStartAlphas();
        }

        [System.Serializable]
        public class RendererMaterialColorReference
        {
            public Renderer renderer;
            public int materialID;
            public string[] colorParamNames;

            [HideInInspector]
            public float[] defaultAlphas;

            [HideInInspector]
            public float[] fadeStartAlphas; // The alpha values when the current fade started
        }
    }
}