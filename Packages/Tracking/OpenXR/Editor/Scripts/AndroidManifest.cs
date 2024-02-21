using JetBrains.Annotations;
using System.Linq;
using System.Xml.Linq;

namespace Ultraleap.Tracking.OpenXR
{
    public class AndroidManifest
    {
        private readonly string _manifestPath;
        private readonly XDocument _manifest;
        private readonly XNamespace _android;

        [PublicAPI]
        public AndroidManifest(string path)
        {
            _manifestPath = path;
            _manifest = XDocument.Load(_manifestPath);
            _android = @"http://schemas.android.com/apk/res/android";
        }

        [PublicAPI]
        public void Save()
        {
            _manifest.Save(_manifestPath);
        }

        [PublicAPI]
        public void SaveAs(string path)
        {
            _manifest.Save(path);
        }

        [PublicAPI]
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
            if (queries.Elements("package").All(el => el.Attribute(_android + "name")?.Value != packageName))
            {
                queries.Add(
                    new XElement("package", new XAttribute(_android + "name", packageName))
                );
            }
        }

        [PublicAPI]
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

        [PublicAPI]
        public void AddUsesPermission(string name)
        {
            // Check if the uses-feature is already there.
            var feature = _manifest.Root!
                .Elements("uses-permission")
                .FirstOrDefault(el => el.Attribute(_android + "name")?.Value == name);

            // Create it if not.
            if (feature == null)
            {
                _manifest.Root!.Add(
                    new XElement("uses-permission", new XAttribute(_android + "name", name))
                );
            }
        }

        [PublicAPI]
        public void AddUsesFeature(string name, bool required)
        {
            // Check if the uses-feature is already there.
            var feature = _manifest.Root!
                .Elements("uses-feature")
                .FirstOrDefault(el => el.Attribute(_android + "name")?.Value == name);

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

        [PublicAPI]
        public void AddMetadata(string name, string value)
        {
            // Get the application element.
            var application = _manifest.Root!.Element("application")!;
            var metaData = application
                .Elements("meta-data")
                .FirstOrDefault(el => el.Attribute(_android + "name")?.Value == name);

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