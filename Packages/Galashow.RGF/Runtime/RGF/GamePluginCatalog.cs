using System;
using System.Collections.Generic;
using Galashow.Core;

namespace Galashow.RGF
{
    /// <summary>
    /// 미니게임 플러그인 정보
    /// </summary>
    public sealed class GamePluginDescriptor
    {
        /// <summary>
        /// 플러그인 ID (예: galashow.trolley). API game_data.pluginId, RegisterPlugin miniGameName과 같은 값
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// 표시 이름
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// 플러그인 인스턴스 생성 함수
        /// </summary>
        public Func<IGamePlugin> Factory { get; }

        public GamePluginDescriptor(string id, string displayName, Func<IGamePlugin> factory)
        {
            Id = id;
            DisplayName = displayName;
            Factory = factory;
        }
    }

    /// <summary>
    /// 미니게임 플러그인 카탈로그
    /// 각 미니게임 패키지가 로드 시점([RuntimeInitializeOnLoadMethod])에 자신을 등록하고,
    /// RGF는 ID로 인스턴스를 생성한다. RGF가 미니게임 패키지를 직접 참조하지 않기 위한 연결점이다.
    /// </summary>
    public static class GamePluginCatalog
    {
        private static readonly Dictionary<string, GamePluginDescriptor> _descriptors =
            new Dictionary<string, GamePluginDescriptor>(StringComparer.Ordinal);

        /// <summary>
        /// 등록된 플러그인 정보 목록
        /// </summary>
        public static IEnumerable<GamePluginDescriptor> All => _descriptors.Values;

        /// <summary>
        /// 플러그인 등록. 같은 ID를 다시 등록하면 덮어쓴다 (도메인 리로드 없는 플레이 모드 대응)
        /// </summary>
        public static void Register(string id, string displayName, Func<IGamePlugin> factory)
        {
            if (string.IsNullOrWhiteSpace(id) || factory == null)
            {
                GLog.Error("[PluginCatalog] Invalid plugin registration");
                return;
            }

            _descriptors[id] = new GamePluginDescriptor(id, displayName, factory);
            GLog.Debug($"[PluginCatalog] Registered: {id} ({displayName})");
        }

        /// <summary>
        /// 등록 여부 확인
        /// </summary>
        public static bool Contains(string id)
        {
            return id != null && _descriptors.ContainsKey(id);
        }

        /// <summary>
        /// ID로 새 플러그인 인스턴스 생성
        /// </summary>
        public static bool TryCreate(string id, out IGamePlugin plugin)
        {
            plugin = null;
            if (id == null || !_descriptors.TryGetValue(id, out var descriptor))
            {
                return false;
            }

            plugin = descriptor.Factory();
            return plugin != null;
        }
    }
}
