using Leap.Unity.Infix;
using System;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR;

namespace Ultraleap.Tracking.OpenXR
{
    public class HandTrackingFeatureBuildHooks : OpenXRFeatureBuildHooks
    {
        private const string OpenXRPackageRuntimeService = "org.khronos.openxr.OpenXRRuntimeService";

        private const string UltraleapPackageTrackingService = "com.ultraleap.tracking.service";
        private const string UltraleapPackageOpenXRApiLayer = "com.ultraleap.openxr.api_layer";

        private const string MetaHandTrackingFeature = "oculus.software.handtracking";
        private const string MetaHandTrackingPermission = "com.oculus.permission.HAND_TRACKING";
        private const string MetaHandTrackingFrequency = "com.oculus.handtracking.frequency";

        public override int callbackOrder => 1;
        public override Type featureType => typeof(HandTrackingFeature);

        protected override void OnPreprocessBuildExt(BuildReport report)
        {
        }

        protected override void OnPostGenerateGradleAndroidProjectExt(string path)
        {
            var manifest = new AndroidManifest(GetAndroidManifestPath(path));
            var feature = OpenXRSettings.ActiveBuildTargetInstance.GetFeature<HandTrackingFeature>();

            if (PlayerSettings.Android.minSdkVersion >= AndroidSdkVersions.AndroidApiLevel30)
            {
                // Intent query is required for OpenXR to work correctly.
                manifest.AddQueriesIntentAction(OpenXRPackageRuntimeService);

                // Allows Ultraleap hand-tracking service & api layer to be accessed.
                manifest.AddQueriesPackage(UltraleapPackageTrackingService);
                manifest.AddQueriesPackage(UltraleapPackageOpenXRApiLayer);
            }

            if (feature.metaPermissions)
            {
                // Adds the feature and permission to also work on Meta headsets that support hand-tracking.
                manifest.AddUsesFeature(MetaHandTrackingFeature, false);
                manifest.AddUsesPermission(MetaHandTrackingPermission);
            }

            if (feature.metaHighFrequency)
            {
                // Enable Meta high-frequency hand-tracking if requested.
                manifest.AddApplicationMetadata(MetaHandTrackingFrequency, "HIGH");
            }

            manifest.Save();
        }

        protected override void OnPostprocessBuildExt(BuildReport report)
        {
        }

        private string GetAndroidManifestPath(string projectPath)
        {
            return projectPath.PathCombine("src").PathCombine("main").PathCombine("AndroidManifest.xml");
        }

        private class AndroidManifest
        {
            private string _manifestPath;
            private XDocument _manifest;
            private XNamespace _android;

            public AndroidManifest(string path)
            {
                _manifestPath = path;
                _manifest = XDocument.Load(_manifestPath);
                _android = @"http://schemas.android.com/apk/res/android";
            }

            public void Save()
            {
                _manifest.Save(_manifestPath);
            }

            public void SaveAs(string path)
            {
                _manifest.Save(path);
            }

            public void AddQueriesPackage(string packageName)
            {
                // Get the queries element, creating it if it doesn't exist.
                var queries = _manifest.Root!.Element("queries");
                if (queries == null)
                {
                    queries = new XElement("queries");
                    _manifest.Root!.Add(queries);
                }

                // Check for the package statement and create it if doesn't exist
                if (queries.Elements("package").All(el => el.Attribute(_android + "name")?.Name != packageName))
                {
                    queries.Add(
                        new XElement("package", new XAttribute(_android + "name", packageName))
                    );
                }
            }

            public void AddQueriesIntentAction(string name)
            {
                // Get the queries element, creating it if it doesn't exist.
                var queries = _manifest.Root!.Element("queries");
                if (queries == null)
                {
                    queries = new XElement("queries");
                    _manifest.Root!.Add(queries);
                }

                // Refactor later if required
                queries.Add(
                    new XElement("intent",
                        new XElement("action",
                            new XAttribute(_android + "name", name))));
            }

            public void AddUsesPermission(string name)
            {
                // Check if the uses-permission is already there, and create it if not.
                if (_manifest.Root!.Elements("uses-permission")
                    .All(el => el.Attribute(_android + "name")?.Name != name))
                {
                    _manifest.Root!.Add(
                        new XElement("uses-permission", new XAttribute(_android + "name", name))
                    );
                }
            }

            public void AddUsesFeature(string name, bool required)
            {
                // Check if the uses-feature is already there.
                var feature = _manifest.Root!
                    .Elements("uses-feature")
                    .FirstOrDefault(el => el.Attribute(_android + "name")?.Name == name);

                // Add if it doesn't exist, or upgrade to required if it does and required was declared.
                if (feature == null)
                {
                    _manifest.Root!.Add(
                        new XElement("uses-feature",
                            new XAttribute(_android + "name", name),
                            new XAttribute(_android + "required", required)
                        )
                    );
                }
                else if (required)
                {
                    feature.SetAttributeValue(_android + "required", true);
                }
            }

            public void AddApplicationMetadata(string name, string value)
            {
                // Get the application element.
                var application = _manifest.Root!.Element("application")!;
                var metaData = application
                    .Elements("meta-data")
                    .FirstOrDefault(el => el.Attribute(_android + "name")?.Name == name);

                // Check for the meta-data element and create it if doesn't exist, or update if it does.
                if (metaData == null)
                {
                    application.Add(
                        new XElement("meta-data",
                            new XAttribute(_android + "name", name),
                            new XAttribute(_android + "value", value))
                    );
                }
                else
                {
                    metaData.SetAttributeValue(_android + "value", value);
                }
            }
        }
    }
}