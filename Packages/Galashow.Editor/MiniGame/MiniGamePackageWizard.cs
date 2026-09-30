using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Galashow.Editor.MiniGame
{
    /// <summary>
    /// 새 미니게임 임베디드 패키지(Packages/Galashow.{Name}) 뼈대를 만든다.
    /// 규칙은 docs/minigame-package.md를 따른다.
    /// </summary>
    public class MiniGamePackageWizard : EditorWindow
    {
        // 생성 패키지가 참조하는 어셈블리 GUID
        const string CoreAsmdefGuid = "a7d46dd5b7e984d47b857fd2c78ae0b4";
        const string RgfAsmdefGuid = "f8c3d2a1e9b7c4f4d8e6a5b3c2d1f0e9";
        const string FrameworkVersion = "0.1.0";

        static readonly Regex NamePattern = new Regex("^[A-Z][A-Za-z0-9]*$");

        string _name = "";
        string _displayName = "";
        string _description = "";

        [MenuItem("Galashow/미니게임 패키지 만들기...")]
        static void Open()
        {
            var window = GetWindow<MiniGamePackageWizard>(true, "미니게임 패키지 만들기");
            window.minSize = new Vector2(420, 220);
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Packages/Galashow.{이름} 임베디드 패키지를 만들고 플러그인을 GamePluginCatalog에 자동 등록합니다.", MessageType.Info);

            _name = EditorGUILayout.TextField("이름 (PascalCase)", _name).Trim();
            _displayName = EditorGUILayout.TextField("표시 이름", _displayName);
            _description = EditorGUILayout.TextField("설명", _description);

            var error = Validate(_name);
            var names = MiniGamePackageNames.From(_name);
            EditorGUILayout.Space();
            if (error == null)
            {
                EditorGUILayout.LabelField("폴더", names.Folder);
                EditorGUILayout.LabelField("패키지", names.PackageName);
                EditorGUILayout.LabelField("플러그인 ID", names.PluginId);
            }
            else if (_name.Length > 0)
            {
                EditorGUILayout.HelpBox(error, MessageType.Warning);
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(error != null || string.IsNullOrWhiteSpace(_displayName)))
            {
                if (GUILayout.Button("만들기"))
                {
                    Create(names, _displayName.Trim(), _description.Trim());
                    Close();
                }
            }
        }

        static string Validate(string name)
        {
            if (!NamePattern.IsMatch(name))
                return "영문 대문자로 시작하는 영문·숫자 이름을 입력하세요. 예: BalanceGame";

            var names = MiniGamePackageNames.From(name);
            if (Directory.Exists(names.Folder))
                return $"{names.Folder} 폴더가 이미 있습니다.";

            foreach (var dir in Directory.GetDirectories("Packages"))
            {
                var packageJson = Path.Combine(dir, "package.json");
                if (File.Exists(packageJson) && File.ReadAllText(packageJson).Contains($"\"{names.PackageName}\""))
                    return $"{names.PackageName} 패키지가 이미 있습니다.";
            }
            return null;
        }

        static void Create(MiniGamePackageNames names, string displayName, string description)
        {
            var files = new Dictionary<string, string>
            {
                ["package.json"] = PackageJson(names, displayName, description),
                [$"{names.AssemblyName}.asmdef"] = Asmdef(names),
                ["README.md"] = Readme(names, displayName, description),
                ["Runtime/AssemblyInfo.cs"] = AssemblyInfo(),
                [$"Runtime/{names.Name}PluginInstaller.cs"] = Installer(names, displayName),
                [$"Runtime/{names.Name}Plugin.cs"] = Plugin(names, displayName),
                [$"Runtime/{names.Name}GameData.cs"] = GameData(names),
                [$"Runtime/{names.Name}Stage.cs"] = Stage(names, displayName),
            };

            var utf8 = new UTF8Encoding(false);
            foreach (var file in files)
            {
                var path = Path.Combine(names.Folder, file.Key);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, file.Value.Replace("\n", "\r\n"), utf8);
            }

            AddToLinkXml(names.AssemblyName);

            AssetDatabase.Refresh();
            UnityEditor.PackageManager.Client.Resolve();
            Debug.Log($"[MiniGamePackageWizard] {names.Folder} 생성 완료. 플러그인 ID: {names.PluginId}");
        }

        const string LinkXmlPath = "Assets/link.xml";

        /// <summary>
        /// WebGL(IL2CPP) 스트리핑에서 gameData 등 JSON 모델이 지워지지 않게 Assets/link.xml에 어셈블리를 추가한다.
        /// 패키지 안의 link.xml은 빌드에 전달되지 않는다.
        /// </summary>
        static void AddToLinkXml(string assemblyName)
        {
            var line = $"    <assembly fullname=\"{assemblyName}\" preserve=\"all\"/>";
            if (!File.Exists(LinkXmlPath))
            {
                File.WriteAllText(LinkXmlPath, "<linker>\n" + line + "\n</linker>\n", new UTF8Encoding(false));
                return;
            }

            var text = File.ReadAllText(LinkXmlPath);
            if (text.Contains($"\"{assemblyName}\""))
            {
                return;
            }

            // Galashow 어셈블리 목록 끝(Galashow.Trolley 다음 등)에 넣는다. 못 찾으면 </linker> 앞에 넣는다.
            int anchor = text.LastIndexOf("<assembly fullname=\"Galashow.", System.StringComparison.Ordinal);
            int insertAt = anchor >= 0 ? text.IndexOf('\n', anchor) + 1 : text.LastIndexOf("</linker>", System.StringComparison.Ordinal);
            File.WriteAllText(LinkXmlPath, text.Insert(insertAt, line + "\n"), new UTF8Encoding(false));
        }

        #region Templates

        static string PackageJson(MiniGamePackageNames n, string displayName, string description) =>
$@"{{
  ""name"": ""{n.PackageName}"",
  ""version"": ""0.1.0"",
  ""displayName"": ""Galashow {Json(displayName)}"",
  ""description"": ""{Json(description.Length > 0 ? description : displayName + " 미니게임")} (plugin id: {n.PluginId})"",
  ""unity"": ""6000.3"",
  ""keywords"": [""galashow"", ""minigame""],
  ""dependencies"": {{
    ""com.galashow.core"": ""{FrameworkVersion}"",
    ""com.galashow.rgf"": ""{FrameworkVersion}""
  }}
}}
";

        static string Asmdef(MiniGamePackageNames n) =>
$@"{{
    ""name"": ""{n.AssemblyName}"",
    ""rootNamespace"": ""{n.AssemblyName}"",
    ""references"": [
        ""GUID:{CoreAsmdefGuid}"",
        ""GUID:{RgfAsmdefGuid}""
    ],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": false,
    ""precompiledReferences"": [],
    ""autoReferenced"": true,
    ""defineConstraints"": [],
    ""versionDefines"": [],
    ""noEngineReferences"": false
}}
";

        static string Readme(MiniGamePackageNames n, string displayName, string description) =>
$@"# {displayName}

{(description.Length > 0 ? description : "미니게임 설명을 작성한다.")}

| 항목 | 값 |
| --- | --- |
| 패키지 | `{n.PackageName}` |
| 플러그인 ID | `{n.PluginId}` |
| 어셈블리 | `{n.AssemblyName}` |
| 기획 문서 | 미작성 (`docs/minigame-{n.Lower}.md`) |

## 구성

| 파일 | 역할 |
| --- | --- |
| `Runtime/{n.Name}PluginInstaller.cs` | 로드 시점에 `GamePluginCatalog`에 플러그인 등록 |
| `Runtime/{n.Name}Plugin.cs` | `GamePluginBase` 상속. 단계별 진행, 입력(`OnPlayerInput`), 판정(`SetSurvived`) |
| `Runtime/{n.Name}GameData.cs` | 라운드 데이터 모델 (StartRound `gameData`) |
| `Runtime/{n.Name}Stage.cs` | 무대 프리팹 루트 컴포넌트 (연출·게임 전용 UI) |
| `Runtime/AssemblyInfo.cs` | WebGL 코드 스트리핑 방지 (어셈블리) |

## 무대 만들기

1. `Runtime/Resources/{n.Name}/{n.Name}Stage.prefab`을 만들고 루트에 `{n.Name}Stage`를 붙인다.
2. `{n.Name}Plugin`의 `StagePrefabPath` 주석을 푼다.

작성 규칙은 `docs/minigame-package.md`를 따른다.
";

        static string AssemblyInfo() =>
@"using UnityEngine.Scripting;

// 다른 어셈블리가 직접 참조하지 않아도 WebGL 빌드의 코드 스트리핑에서 제외되지 않게 한다.
[assembly: AlwaysLinkAssembly]
";

        static string Installer(MiniGamePackageNames n, string displayName) =>
$@"using UnityEngine;
using UnityEngine.Scripting;
using Galashow.RGF;

namespace {n.AssemblyName}
{{
    /// <summary>
    /// {displayName} 플러그인을 GamePluginCatalog에 등록
    /// </summary>
    [Preserve]
    public static class {n.Name}PluginInstaller
    {{
        /// <summary>
        /// 플러그인 ID. API game_data.pluginId, RegisterPlugin miniGameName에 이 값을 쓴다.
        /// </summary>
        public const string PluginId = ""{n.PluginId}"";

        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {{
            GamePluginCatalog.Register(PluginId, ""{CSharp(displayName)}"", () => new {n.Name}Plugin());
        }}
    }}
}}
";

        static string Plugin(MiniGamePackageNames n, string displayName) =>
$@"using System.Threading.Tasks;
using Galashow.Core;
using Galashow.RGF;

namespace {n.AssemblyName}
{{
    /// <summary>
    /// {displayName} 게임 플러그인
    /// gameData 변환, 입력 필터(INPUT 단계·생존자), 무대 생성·정리는 GamePluginBase가 처리한다.
    /// </summary>
    public class {n.Name}Plugin : GamePluginBase<{n.Name}GameData>
    {{
        public override string GameName => ""{CSharp(displayName)}"";

        // 무대 프리팹을 만든 뒤 주석을 푼다: Runtime/Resources/{n.Name}/{n.Name}Stage.prefab
        // protected override string StagePrefabPath => ""{n.Name}/{n.Name}Stage"";

        protected override void ValidateGameData({n.Name}GameData data)
        {{
            // 필수 값 검증. 잘못되면 예외를 던져 라운드 시작을 거부한다.
        }}

        protected override Task OnReady()
        {{
            GLog.Info($""[{n.Name}] Ready - Round {{State.CurrentRound}}, 참가 {{Participants.Count}}명"");
            return Task.CompletedTask;
        }}

        protected override Task OnPresent()
        {{
            // 문제·상황 제시: GetStage<{n.Name}Stage>()?....
            return Task.CompletedTask;
        }}

        protected override void OnPlayerInput(PlayerInput input)
        {{
            // INPUT 단계에 생존 참가자의 채팅만 들어온다. input.PlayerId, input.Message
        }}

        protected override Task OnExecute()
        {{
            // 판정: 참가자마다 SetSurvived(playerId, 생존 여부)
            foreach (var playerId in Participants)
            {{
                SetSurvived(playerId, true);
            }}
            return Task.CompletedTask;
        }}

        protected override Task OnReveal()
        {{
            // 결과 연출
            return Task.CompletedTask;
        }}
    }}
}}
";

        static string Stage(MiniGamePackageNames n, string displayName) =>
$@"using Galashow.RGF;

namespace {n.AssemblyName}
{{
    /// <summary>
    /// {displayName} 무대 (Runtime/Resources/{n.Name}/{n.Name}Stage.prefab 루트)
    /// 연출·게임 전용 UI 메서드를 둔다. 판정 로직은 {n.Name}Plugin에 둔다.
    /// </summary>
    public class {n.Name}Stage : GameStage
    {{
        public override void Initialize(GameState state)
        {{
        }}
    }}
}}
";

        static string GameData(MiniGamePackageNames n) =>
$@"namespace {n.AssemblyName}
{{
    /// <summary>
    /// {n.Name} 라운드 데이터 (StartRound gameData)
    /// </summary>
    public class {n.Name}GameData
    {{
        /// <summary>
        /// game_data 형식 버전
        /// </summary>
        public int SchemaVersion {{ get; set; }} = 1;
    }}
}}
";

        static string Json(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

        static string CSharp(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

        #endregion
    }

    /// <summary>
    /// 미니게임 패키지 이름 규칙
    /// </summary>
    public readonly struct MiniGamePackageNames
    {
        public readonly string Name;         // BalanceGame
        public readonly string Lower;        // balancegame
        public readonly string AssemblyName; // Galashow.BalanceGame
        public readonly string Folder;       // Packages/Galashow.BalanceGame
        public readonly string PackageName;  // com.galashow.balancegame
        public readonly string PluginId;     // galashow.balancegame

        MiniGamePackageNames(string name)
        {
            Name = name;
            Lower = name.ToLowerInvariant();
            AssemblyName = $"Galashow.{name}";
            Folder = $"Packages/Galashow.{name}";
            PackageName = $"com.galashow.{Lower}";
            PluginId = $"galashow.{Lower}";
        }

        public static MiniGamePackageNames From(string name) => new MiniGamePackageNames(name ?? "");
    }
}
