using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace BaoZuPo.Editor
{
    /// <summary>
    /// Unity CI/CD 构建入口。
    /// 供 CI 系统（例如 GitHub Actions、Jenkins）调用，无头构建游戏。
    ///
    /// 命令行用法：
    ///   unity -projectPath <project> -executeMethod BaoZuPo.Editor.UnityCiBuildEntry.BuildStandaloneWindows64
    ///          -buildOutput <output_path> [-buildName <name>] [-buildVersion <version>]
    ///
    /// 参数说明：
    /// - -buildOutput：必需，输出目录的根路径
    /// - -buildName：可选，构建名称（默认取 PlayerSettings.productName）
    /// - -buildVersion：可选，版本号（默认取 PlayerSettings.bundleVersion）
    ///
    /// 工作流程：
    /// 1. 解析命令行参数
    /// 2. 从 EditorBuildSettings 读取启用的场景
    /// 3. 创建输出目录
    /// 4. 调用 BuildPipeline.BuildPlayer
    /// 5. 检查构建结果，失败则抛异常
    ///
    /// 输出目录结构：
    ///   buildOutput/
    ///     GameName_1.0.0/
    ///       GameName.exe
    ///       GameName_Data/
    ///       ...
    /// </summary>
    public static class UnityCiBuildEntry
    {
        private const string BuildOutputArgument = "-buildOutput";
        private const string BuildNameArgument = "-buildName";
        private const string BuildVersionArgument = "-buildVersion";

        /// <summary>
        /// 构建 Windows 64 位独立游戏。
        /// 从命令行参数读取输出路径、游戏名、版本号，执行构建。
        /// </summary>
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
