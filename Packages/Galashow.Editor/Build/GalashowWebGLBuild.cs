using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Galashow.Editor.Build
{
    /// <summary>
    /// React Client용 WebGL 빌드.
    /// 메뉴: Galashow → 빌드 → WebGL (Client에 복사)
    /// 배치: Unity -batchmode -quit -projectPath . -executeMethod Galashow.Editor.Build.GalashowWebGLBuild.BuildFromCommandLine [-clientBuildDir &lt;경로&gt;]
    /// 결과: Builds/WebGL/Build/WebGL.{loader.js,data,framework.js,wasm} (CI와 같은 이름)
    /// </summary>
    public static class GalashowWebGLBuild
    {
        const string OutputDir = "Builds/WebGL";
        const string ProductFileName = "WebGL";

        /// <summary>
        /// Client가 읽는 로컬 빌드 폴더 (워크스페이스 기준 기본값)
        /// </summary>
        public const string DefaultClientBuildDir = "../Client/build/unity";

        [MenuItem("Galashow/빌드/WebGL (Client에 복사)")]
        static void BuildFromMenu()
        {
            if (Build(DefaultClientBuildDir))
            {
                EditorUtility.RevealInFinder(Path.GetFullPath(OutputDir));
            }
        }

        /// <summary>
        /// 배치 모드 진입점. 실패하면 종료 코드 1
        /// </summary>
        public static void BuildFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-clientBuildDir");
            var clientDir = i >= 0 && i + 1 < args.Length ? args[i + 1] : DefaultClientBuildDir;
            bool ok = Build(clientDir);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        static bool Build(string clientBuildDir)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[GalashowBuild] Build Settings에 켜진 씬이 없습니다");
                return false;
            }

            // React 계약: 압축 없음, 해시 없는 파일 이름 (CI validate-project.py와 같은 조건)
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            // Unity 스플래시 없이 바로 Main 씬 (Unity 6은 무료 라이선스도 끌 수 있다). React가 시작 화면을 덮는다
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            var outputPath = Path.GetFullPath(Path.Combine(OutputDir));
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            Debug.Log($"[GalashowBuild] WebGL build start: {string.Join(", ", scenes)} → {outputPath}");
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[GalashowBuild] Build failed: {report.summary.result}, errors {report.summary.totalErrors}");
                return false;
            }

            // Unity는 출력 폴더 이름을 파일 이름으로 쓰므로 CI와 같은 WebGL.* 이름으로 맞춘다
            var buildDir = Path.Combine(outputPath, "Build");
            var folderName = new DirectoryInfo(outputPath).Name;
            if (folderName != ProductFileName)
            {
                foreach (var file in Directory.GetFiles(buildDir, folderName + ".*"))
                {
                    var target = Path.Combine(buildDir, ProductFileName + Path.GetFileName(file).Substring(folderName.Length));
                    File.Copy(file, target, true);
                    File.Delete(file);
                }
            }

            foreach (var name in new[] { "loader.js", "data", "framework.js", "wasm" })
            {
                var path = Path.Combine(buildDir, $"{ProductFileName}.{name}");
                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                {
                    Debug.LogError($"[GalashowBuild] Missing build file: {path}");
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(clientBuildDir))
            {
                var clientDir = Path.GetFullPath(clientBuildDir);
                Directory.CreateDirectory(clientDir);
                foreach (var file in Directory.GetFiles(buildDir, ProductFileName + ".*"))
                {
                    File.Copy(file, Path.Combine(clientDir, Path.GetFileName(file)), true);
                }
                Debug.Log($"[GalashowBuild] Copied to Client: {clientDir}");
            }

            Debug.Log($"[GalashowBuild] Build succeeded ({report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalTime})");
            return true;
        }
    }
}
