/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity
{

    /**
     * The DetectorLogicGate detector observes other detectors and activates when
     * these other detectors match the specified logic.
     * 
     * A DetectorLogicGate can be configured as an AND gate or an OR gate. You can also
     * negate the output (creating a NAND or NOR gate).
     * 
     * Since a DetectorLogicGate is a Detector, it can observe other DetectorLogicGate instances.
     * However, before constructing complex logic chains, you should consider whether it is better 
     * to put such logic into a normal script.
     * 
     * @since 4.1.2
     */
    public class DetectorLogicGate : Detector
    {
        [SerializeField]
        [Tooltip("The list of observed detectors.")]
        private List<Detector> Detectors;
        /**
         * When true, all Detector components of the same game object
         * are added to the list of watched detectors on Awake. When false,
         * you must manually add the desired detectors.
         * 
         * If you have more than one DetectorLogicGate component on a game object,
         * do not enable this option on both. 
         * @since 4.1.2
         */
        [Tooltip("Add all detectors on this object automatically.")]
        public bool AddAllSiblingDetectorsOnAwake = true;

        /**
         * The type of logic for this gate: AND or OR.
         * @since 4.1.2
         */
        [Tooltip("The type of logic used to combine detector state.")]
        public LogicType GateType = LogicType.AndGate;

        /**
         * Whether to negate the output of the gate. AND becomes NAND; OR becomes NOR.
         * @since 4.1.2
         */
        [Tooltip("Whether to negate the gate output.")]
        public bool Negate = false;

        /**
         * Adds the specified detector to the list of observed detectors.
         * 
         * The same detector cannot be added more than once.
         * @param Detector the detector to watch.
         * @since 4.1.2
         */
        public void AddDetector(Detector detector)
        {
            if (!Detectors.Contains(detector))
            {
                Detectors.Add(detector);
                activateDetector(detector);
            }
        }

        /**
         * Removes the specified detector from the list of observed detectors;
         * 
         * @param Detector the detector to remove.
         * @since 4.1.2
         */
        public void RemoveDetector(Detector detector)
        {
            detector.OnActivate.RemoveListener(CheckDetectors);
            detector.OnDeactivate.RemoveListener(CheckDetectors);
            Detectors.Remove(detector);
        }

        /**
         * Adds all the other detectors on the same GameObject to the list of observed detectors.
         * 
         * Note: If you have more than one DetectorLogicGate instance on a game object, make sure that
         * both objects don't observe each other.
         * @since 4.1.2
         */
        public void AddAllSiblingDetectors()
        {
            Detector[] detectors = GetComponents<Detector>();
            for (int g = 0; g < detectors.Length; g++)
            {
                if (detectors[g] != this && detectors[g].enabled)
                {
                    AddDetector(detectors[g]);
                }
            }
        }

        private void Awake()
        {
            for (int d = 0; d < Detectors.Count; d++)
            {
                activateDetector(Detectors[d]);
            }
            if (AddAllSiblingDetectorsOnAwake)
            {
                AddAllSiblingDetectors();
            }
        }

        private void activateDetector(Detector detector)
        {
            detector.OnActivate.RemoveListener(CheckDetectors); //avoid double subscription
            detector.OnDeactivate.RemoveListener(CheckDetectors);
            detector.OnActivate.AddListener(CheckDetectors);
            detector.OnDeactivate.AddListener(CheckDetectors);
        }

        private void OnEnable()
        {
            CheckDetectors();
        }

        private void OnDisable()
        {
            Deactivate();
        }

        /**
         * Checks all the observed detectors, combines them with the specified type of logic
         * and calls the Activate() or Deactivate() function as appropriate.
         * @since 4.1.2
         */
        protected void CheckDetectors()
        {
            if (Detectors.Count < 1)
                return;
            bool state = Detectors[0].IsActive;
            for (int a = 1; a < Detectors.Count; a++)
            {
                if (GateType == LogicType.AndGate)
                {
                    state = state && Detectors[a].IsActive;
                }
                else
                {
                    state = state || Detectors[a].IsActive;
                }
            }

            if (Negate)
            {
                state = !state;
            }

            if (state)
            {
                Activate();
            }
            else
            {
                Deactivate();
            }
        }
    }

    /** The type of logic used to combine the watched detectors. */
    public enum LogicType { AndGate, OrGate }
}