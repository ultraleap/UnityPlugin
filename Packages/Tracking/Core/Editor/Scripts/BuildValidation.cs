using Leap.Unity;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildValidation : IPreprocessBuildWithReport
{
    public int callbackOrder { get; }
    public void OnPreprocessBuild(BuildReport report)
    {
        switch (report.summary.platformGroup)
        {
            case BuildTargetGroup.Android:
                AndroidBuild();
                break;
        }
    }

    private void AndroidBuild()
    {
        if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64) // Must be only arm64 so direct equality check, not checking for IL2CPP explicitly because it's required to select ARM64 anyway...
            DoValidationPopup($"Android target architecture for an android build including Ultraleap hand tracking must be ARM64. To fix this, go to 'Edit -> Project Settings -> Player -> Other Settings -> Configuration' and select 'Scripting Backend' 'IL2CPP' and only 'ARM64' for 'Target Architectures'.");
    }

    private void DoValidationPopup(string error)
    {
        if (EditorUtility.DisplayDialog("Ultraleap build validation",
            $"Validation issue encountered: {error} Do you want to continue with the build?", "Continue Building",
            "Stop Build", DialogOptOutDecisionType.ForThisSession, "UltraleaBuildValidationDialog"))
            return;

        throw new BuildFailedException($"Build cancelled - {error}");
    }
}
