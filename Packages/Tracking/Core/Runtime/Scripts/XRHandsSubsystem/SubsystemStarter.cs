/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SubsystemsImplementation.Extensions;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.ProviderImplementation;

namespace Leap.Unity
{
    public class SubsystemStarter
    {
        static XRHandSubsystem m_Subsystem = null;
        static XRHandProviderUtility.SubsystemUpdater updater = null;
        static GameObject leapProviderGO = null;
        static XRHandSubsystemProvider subsystemProvider = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RunBeforeSceneLoad()
        {
            UltraleapSettings ultraleapSettings = UltraleapSettings.Instance;

            if (ultraleapSettings == null)
            {
                Debug.Log("There is no Ultraleap Settings object in the package. Subsystem will not be used.");
                return;
            }

            if (ultraleapSettings.leapSubsystemEnabled == false)
            {
                return;
            }

            Application.quitting -= OnQuit;
            Application.quitting += OnQuit;

            // Stop all existing subsystems and produce a new one
            List<LeapHandsSubsystem> subsystems = new List<LeapHandsSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            foreach (var subsystem in subsystems)
            {
                subsystem.Stop();
                subsystem.Destroy();
                subsystem.GetProvider().Stop();
                subsystem.GetProvider().Destroy();
            }

            List<XRHandSubsystemDescriptor> descriptors = new List<XRHandSubsystemDescriptor>();
            SubsystemManager.GetSubsystemDescriptors(descriptors);
            foreach (var descriptor in descriptors)
            {
                if (descriptor.id == "UL XR Hands")
                {
                    m_Subsystem = descriptor.Create();
                    break;
                }
            }

            if (m_Subsystem != null)
            {
                if (!m_Subsystem.running)
                {
                    m_Subsystem.Start();

                    updater = new XRHandProviderUtility.SubsystemUpdater(m_Subsystem);
                    updater.Start();
                }

                subsystemProvider = m_Subsystem.GetProvider();
            }
            else
            {
                Debug.Log("Hands Subsystem could not be started as it does not exist");
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RunAfterSceneLoad()
        {
            UltraleapSettings ultraleapSettings = UltraleapSettings.Instance;

            if (ultraleapSettings == null || ultraleapSettings.leapSubsystemEnabled == false)
            {
                return;
            }

            LeapProvider leapProvider = GameObject.FindAnyObjectByType<LeapXRServiceProvider>();

            // If there is no leap provider in the scene
            if (leapProvider == null)
            {
                Debug.Log("There are no LeapXRServiceProviders in the scene, automatically assigning one for use with Ultraleap Subsystem for XRHands");

                GameObject leapProviderGO = new GameObject("LeapXRServiceProvider");
                LeapXRServiceProvider leapXRServiceProvider = leapProviderGO.AddComponent<LeapXRServiceProvider>();
                leapXRServiceProvider.PositionDeviceRelativeToMainCamera = true;
                leapProvider = (LeapProvider)leapXRServiceProvider;
                GameObject.DontDestroyOnLoad(leapProviderGO);
            }
            else
            {
                Debug.Log("Ultraleap Subsystem for XRHands is using the existing LeapXRServiceProvider found in the current scene");
            }

            if (subsystemProvider != null)
            {
                LeapXRHandProvider leapXRHandProvider = (LeapXRHandProvider)subsystemProvider;
                leapXRHandProvider.TrackingProvider = leapProvider;
            }
        }

        private static void OnQuit()
        {
            Application.quitting -= OnQuit;

            List<LeapHandsSubsystem> subsystems = new List<LeapHandsSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);

            foreach (var subsystem in subsystems)
            {
                subsystem.Stop();
                subsystem.Destroy();
                subsystem.GetProvider().Stop();
                subsystem.GetProvider().Destroy();
            }

            updater?.Stop();
            updater?.Destroy();

            if (leapProviderGO != null)
            {
                GameObject.Destroy(leapProviderGO);
            }

            m_Subsystem = null;
            updater = null;
            leapProviderGO = null;
            subsystemProvider = null;
        }
    }
}