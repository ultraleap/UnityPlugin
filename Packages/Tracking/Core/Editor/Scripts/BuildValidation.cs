using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

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
            DoValidationPopup($"Target architecture for an Android build including Ultraleap hand tracking must be ARM64.{Environment.NewLine}" +
                              $"To fix this, go to 'Edit -> Project Settings -> Player -> Other Settings -> Configuration'.{Environment.NewLine}" +
                              "Set 'Scripting Backend' to 'IL2CPP' and 'Target Architectures' to only 'ARM64'.");
    }

    private void DoValidationPopup(string error)
    {
        if (EditorUtility.DisplayDialog("Ultraleap build validation",
            $"Validation issue encountered: {error} Do you want to continue with the build anyway?", "Continue Building",
            "Stop Build"))
            return;

        throw new BuildFailedException($"Build cancelled - {error}");
    }
}