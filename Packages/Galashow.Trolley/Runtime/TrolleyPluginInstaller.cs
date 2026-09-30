using UnityEngine;
using UnityEngine.Scripting;
using Galashow.RGF;

namespace Galashow.Trolley
{
    /// <summary>
    /// 트롤리 딜레마 플러그인을 GamePluginCatalog에 등록
    /// </summary>
    [Preserve]
    public static class TrolleyPluginInstaller
    {
        /// <summary>
        /// 플러그인 ID. API game_data.pluginId, RegisterPlugin miniGameName에 이 값을 쓴다.
        /// </summary>
        public const string PluginId = "galashow.trolley";

        [Preserve]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {
            GamePluginCatalog.Register(PluginId, "트롤리 딜레마", () => new TrolleyDilemmaPlugin());
        }
    }
}
