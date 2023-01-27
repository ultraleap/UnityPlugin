/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.InputModule
{
    public class ScrollRectVolume : MonoBehaviour
    {
        public AudioSource source;
        public RectTransform content;
        public float Volume = 1f;
        RectTransform viewport;
        float volumeScalar = 0f;
        Vector2 currentPos = Vector3.zero;
        Vector2 prevPos = Vector3.zero;
        Vector2 viewportScale;
        float TimeLastSlid = 0f;

        void Start()
        {
            viewport = content.parent.GetComponent<RectTransform>();
            viewportScale = new Vector2(viewport.rect.size.x, viewport.rect.size.y);
        }

        void Update()
        {
            Vector2 localPos = new Vector2(content.localPosition.x, content.localPosition.y);
            localPos = new Vector2(localPos.x / viewportScale.x, localPos.y / viewportScale.y);

            if (localPos != currentPos)
            {
                prevPos = currentPos;
                currentPos = localPos;

                volumeScalar = Mathf.Lerp(volumeScalar, Mathf.Abs((currentPos - prevPos).magnitude) * 40f, 0.4f);

                source.volume = Mathf.Clamp(volumeScalar * Volume, 0f, Volume);

                if (!source.isPlaying)
                {
                    source.Play();
                }

                TimeLastSlid = Time.time;
            }
            else
            {
                if (Time.time - TimeLastSlid > Time.deltaTime * 5f)
                {
                    source.Stop();
                }
                else
                {
                    volumeScalar = Mathf.Lerp(volumeScalar, 0f, 0.4f);
                    source.volume = volumeScalar * Volume;
                }
            }
        }
    }
}