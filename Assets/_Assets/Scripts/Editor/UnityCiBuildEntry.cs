using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace BaoZuPo.Editor
{
    public static class UnityCiBuildEntry
    {
        private const string BuildOutputArgument = "-buildOutput";
        private const string BuildNameArgument = "-buildName";
        private const string BuildVersionArgument = "-buildVersion";

        public static void BuildStandaloneWindows64()
        {
            string buildOutputRoot = RequireArgument(BuildOutputArgument);
            string buildName = GetArgumentOrDefault(BuildNameArgument, PlayerSettings.productName);
            string buildVersion = GetArgumentOrDefault(BuildVersionArgument, PlayerSettings.bundleVersion);

            string[] enabledScenes = GetEnabledScenes();
            string outputDirectory = Path.GetFullPath(
                Path.Combine(
                    buildOutputRoot,
                    ComposeBuildDirectoryName(buildName, buildVersion)));

            string executableName = $"{SanitizeFileName(buildName)}.exe";
            string executablePath = Path.Combine(outputDirectory, executableName);

            Directory.CreateDirectory(outputDirectory);

            var buildOptions = new BuildPlayerOptions
            {
                scenes = enabledScenes,
                locationPathName = executablePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.StrictMode,
            };

            // EditorBuildSettings is the single source of truth for CI scene selection.
            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"[UnityCiBuildEntry] Windows build failed with result {report.summary.result}.");
            }
        }

        private static string[] GetEnabledScenes()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new BuildFailedException(
                    "[UnityCiBuildEntry] No enabled scenes found in EditorBuildSettings.");
            }

            return scenes;
        }

        private static string ComposeBuildDirectoryName(string buildName, string buildVersion)
        {
            string sanitizedBuildName = SanitizeFileName(buildName);
            string sanitizedBuildVersion = SanitizeFileName(buildVersion);

            if (string.IsNullOrWhiteSpace(sanitizedBuildVersion))
            {
                return sanitizedBuildName;
            }

            return $"{sanitizedBuildName}_{sanitizedBuildVersion}";
        }

        private static string RequireArgument(string argumentName)
        {
            if (TryGetArgumentValue(argumentName, out string value))
            {
                return value;
            }

            throw new BuildFailedException(
                $"[UnityCiBuildEntry] Missing required command line argument: {argumentName}.");
        }

        private static string GetArgumentOrDefault(string argumentName, string defaultValue)
        {
            if (TryGetArgumentValue(argumentName, out string value))
            {
                return value;
            }

            return defaultValue;
        }

        private static bool TryGetArgumentValue(string argumentName, out string value)
        {
            string[] commandLineArguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < commandLineArguments.Length; i++)
            {
                if (!string.Equals(commandLineArguments[i], argumentName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (i + 1 >= commandLineArguments.Length)
                {
                    break;
                }

                value = commandLineArguments[i + 1];
                return true;
            }

            value = null;
            return false;
        }

        private static string SanitizeFileName(string value)
        {
            string safeValue = string.IsNullOrWhiteSpace(value) ? "build" : value.Trim();
            HashSet<char> invalidChars = Path.GetInvalidFileNameChars().ToHashSet();

            return new string(safeValue
                .Select(character => invalidChars.Contains(character) ? '_' : character)
                .ToArray());
        }
    }
}
