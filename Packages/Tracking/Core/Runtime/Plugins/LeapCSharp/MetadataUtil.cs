/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Leap
{
    public static class MetadataUtil
    {
        [System.Serializable]
        private struct Analytics
        {
            public Telemetry telemetry;
        }

        [System.Serializable]
        private struct Telemetry
        {
            public string app_name;
            public string app_type;
            public string engine_name;
            public string engine_version;
            public string plugin_version;
            public string installation_source;
            public string interaction_system;
            public string render_pipeline;
        }

        public static string GetMetaData()
        {
            Analytics analytics = new Analytics();
            analytics.telemetry = new Telemetry();

            analytics.telemetry.app_name = Application.productName;
            analytics.telemetry.app_type = GetAppType();
            analytics.telemetry.engine_name = "Unity";
            analytics.telemetry.engine_version = Application.unityVersion;
            analytics.telemetry.plugin_version = Leap.Unity.UltraleapSettings.Instance.PluginVersion;
            analytics.telemetry.installation_source = Leap.Unity.UltraleapSettings.Instance.PluginSource;
            analytics.telemetry.interaction_system = GetInteractionSystem();
            analytics.telemetry.render_pipeline = GetRenderPipeline();

            string json = JsonUtility.ToJson(analytics, true);
            return json;
        }

        static string GetAppType()
        {
            string appType = "Build";

#if UNITY_EDITOR
            appType = "Editor";
#endif

            return appType;
        }

        static string GetRenderPipeline()
        {
            string renderPipeline = "Built In";

            if (QualitySettings.renderPipeline != null)
            {
                renderPipeline = QualitySettings.renderPipeline.GetType().ToString().Split(".").Last();
            }
            else if (GraphicsSettings.currentRenderPipeline != null)
            {
                renderPipeline = GraphicsSettings.currentRenderPipeline.GetType().ToString().Split(".").Last();
            }

            return renderPipeline;
        }

        static string GetInteractionSystem()
        {
            // Physical Hands
            if (GameObject.Find("Physical Hands Manager") ||
                GameObject.Find("Left HardContactHand") ||
                GameObject.Find("Left SoftContactHand") ||
                GameObject.Find("Left NoContactHand"))
            {
                return "Physical Hands";
            }

            // Interaction Engine
            if (GameObject.Find("Interaction Hand (Left)"))
            {
                return "Interaction Engine";
            }

            // XR Hands
            if (Leap.Unity.UltraleapSettings.Instance.leapSubsystemEnabled ||
                Leap.Unity.UltraleapSettings.Instance.updateLeapInputSystem ||
                Leap.Unity.UltraleapSettings.Instance.updateMetaInputSystem)
            {
                return "UL XR Hands";
            }

            return "Unknown";
        }
    }
}