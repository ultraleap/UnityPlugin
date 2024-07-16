using Leap;
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class BuildValidation : IPreprocessBuildWithReport
{
    enum ValidationPopupType
    {
        ANDROID_ARCHITECTURE
    }

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
        {
            if (!UltraleapSettings.Instance.showAndroidBuildArchitectureWarning)
            {
                UnityEngine.Debug.Log("Some Ultraleap Android architecture build warnings are being muted. To unmute, re-enable in the Ultraleap Settings tab in the Project panel");
                return;
            }
            else
            {
                DoValidationPopup($"Target architecture for an Android build including Ultraleap hand tracking must be ARM64.{Environment.NewLine}" +
                                  $"To fix this, go to 'Edit -> Project Settings -> Player -> Other Settings -> Configuration'.{Environment.NewLine}" +
                                  "Set 'Scripting Backend' to 'IL2CPP' and 'Target Architectures' to only 'ARM64'.", ValidationPopupType.ANDROID_ARCHITECTURE);
            }
        }
    }

    private void DoValidationPopup(string error, ValidationPopupType validationType)
    {
        if (EditorUtility.DisplayDialog("Ultraleap build validation",
            $"Validation issue encountered: {error} Do you want to continue with the build anyway?", "Continue Building",
            "Stop Build"))
        {
            switch (validationType)
            {
                case ValidationPopupType.ANDROID_ARCHITECTURE:
                    UltraleapSettings.Instance.showAndroidBuildArchitectureWarning = false;
                    UnityEngine.Debug.LogWarning("Ultraleap Android architecture warning has been muted. To unmute, re-enable in the Ultraleap Settings tab in the Project panel");
                    break;
            }

            return;
        }

        throw new BuildFailedException($"Build cancelled - {error}");
    }
}