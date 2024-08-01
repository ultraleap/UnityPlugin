#if ULTRALEAP_OPENXR_PACKAGE_INSTALLED
using UnityEditor;
using UnityEditor.PackageManager;

namespace Leap.Tracking.OpenXR
{
    [InitializeOnLoad]
    public class RemoveDuplicateOpenXRPackage
    {
        static RemoveDuplicateOpenXRPackage()
        {
            Client.Remove("com.ultraleap.tracking.openxr");
        }
    }
}
#endif