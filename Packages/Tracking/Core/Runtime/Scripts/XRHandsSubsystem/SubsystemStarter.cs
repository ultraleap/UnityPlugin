/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.ProviderImplementation;
using UnityEngine.SubsystemsImplementation.Extensions;

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
                if(!m_Subsystem.running)
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

            LeapProvider leapProvider = Hands.Provider;

            // If there is no leap provider in the scene
            if (leapProvider == null)
            {
                Debug.Log("There are no Leap Providers in the scene, automatically assigning one for use with Leap XRHands");
                leapProviderGO = new GameObject("LeapXRServiceProvider");
                LeapXRServiceProvider leapXRServiceProvider = leapProviderGO.AddComponent<LeapXRServiceProvider>();
                leapXRServiceProvider.PositionDeviceRelativeToMainCamera = false;
                leapProvider = (LeapProvider)leapXRServiceProvider;
                GameObject.DontDestroyOnLoad(leapProviderGO);
            }
            else
            {
                Debug.LogWarning("We recommend that you do not add a Leap Provider to scenes using the Leap Subsystem for XR Hands. This can cause unwanted behaviour.");
            }

            if(leapProvider is LeapXRServiceProvider)
            {
                LeapXRServiceProvider leapXRServiceProvider = (LeapXRServiceProvider)leapProvider;

                if(leapXRServiceProvider.PositionDeviceRelativeToMainCamera == true)
                {
                    leapXRServiceProvider.PositionDeviceRelativeToMainCamera = false;
                    leapProvider.transform.position = Vector3.zero;
                    leapProvider.transform.rotation = Quaternion.identity;
                    leapProvider.transform.localScale = Vector3.one;
                    leapXRServiceProvider.deviceOffsetMode = LeapXRServiceProvider.DeviceOffsetMode.Transform;
                    leapXRServiceProvider.deviceOrigin = leapProvider.transform;
                }
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