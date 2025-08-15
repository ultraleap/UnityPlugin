/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2025.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.HandsModule
{
    /// <summary>
    /// Applies hand tracking data to a skinned mesh renderer, given a binding
    /// </summary>
    public class HandDataToRigTransformer : HandModelBase
    {
        public BindingMap bindingMap;

        public override Chirality Handedness { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public override ModelType HandModelType => ModelType.Graphics;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public override void UpdateHand()
        {
            throw new System.NotImplementedException();
        }

        public override Hand GetLeapHand()
        {
            throw new System.NotImplementedException();
        }

        public override void SetLeapHand(Hand hand)
        {
            throw new System.NotImplementedException();
        }
    }
}