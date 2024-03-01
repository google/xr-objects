using System;
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils.Editor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace UnityEditor.XR.Interaction.Toolkit.Samples.ARStarterAssets
{
    /// <summary>
    /// Unity Editor class which registers Project Validation rules for the AR Starter Assets sample,
    /// checking that other required samples are installed.
    /// </summary>
    static class ARStarterAssetsSampleProjectValidation
    {
        const string k_SampleDisplayName = "AR Starter Assets";
        const string k_Category = "XR Interaction Toolkit";
        const string k_StarterAssetsSampleName = "Starter Assets";

        static readonly BuildTargetGroup[] s_BuildTargetGroups =
            ((BuildTargetGroup[])Enum.GetValues(typeof(BuildTargetGroup))).Distinct().ToArray();

        static readonly List<BuildValidationRule> s_BuildValidationRules = new List<BuildValidationRule>
        {
            new BuildValidationRule
            {
                IsRuleEnabled = () => s_ARFPackageAddRequest == null || s_ARFPackageAddRequest.IsCompleted,
                Message = $"[{k_SampleDisplayName}] AR Foundation (com.unity.xr.arfoundation) package must be installed to use this sample.",
                Category = k_Category,
                CheckPredicate = () => PackageVersionUtility.GetPackageVersion("com.unity.xr.arfoundation") >= new PackageVersion("4.2.8"),
                FixIt = () =>
                {
                    s_ARFPackageAddRequest = Client.Add("com.unity.xr.arfoundation");
                    if (s_ARFPackageAddRequest.Error != null)
                    {
                        Debug.LogError($"Package installation error: {s_ARFPackageAddRequest.Error}: {s_ARFPackageAddRequest.Error.message}");
                    }
                },
                FixItAutomatic = true,
                Error = true,
            },
            new BuildValidationRule
            {
                Message = $"[{k_SampleDisplayName}] {k_StarterAssetsSampleName} sample from XR Interaction Toolkit (com.unity.xr.interaction.toolkit) package must be imported or updated to use this sample.",
                Category = k_Category,
                CheckPredicate = () => TryFindSample("com.unity.xr.interaction.toolkit", string.Empty, k_StarterAssetsSampleName, out var sample) && sample.isImported,
                FixIt = () =>
                {
                    if (TryFindSample("com.unity.xr.interaction.toolkit", string.Empty, k_StarterAssetsSampleName, out var sample))
                    {
                        sample.Import(Sample.ImportOptions.OverridePreviousImports);
                    }
                },
                FixItAutomatic = true,
                Error = true,
            },
        };

        static AddRequest s_ARFPackageAddRequest;

        [InitializeOnLoadMethod]
        static void RegisterProjectValidationRules()
        {
            foreach (var buildTargetGroup in s_BuildTargetGroups)
            {
                BuildValidator.AddRules(buildTargetGroup, s_BuildValidationRules);
            }
        }

        static bool TryFindSample(string packageName, string packageVersion, string sampleDisplayName, out Sample sample)
        {
            sample = default;

            var packageSamples = Sample.FindByPackage(packageName, packageVersion);
            if (packageSamples == null)
            {
                Debug.LogError($"Couldn't find samples of the {ToString(packageName, packageVersion)} package; aborting project validation rule.");
                return false;
            }

            foreach (var packageSample in packageSamples)
            {
                if (packageSample.displayName == sampleDisplayName)
                {
                    sample = packageSample;
                    return true;
                }
            }

            Debug.LogError($"Couldn't find {sampleDisplayName} sample in the {ToString(packageName, packageVersion)} package; aborting project validation rule.");
            return false;
        }

        static string ToString(string packageName, string packageVersion)
        {
            return string.IsNullOrEmpty(packageVersion) ? packageName : $"{packageName}@{packageVersion}";
        }
    }
}
