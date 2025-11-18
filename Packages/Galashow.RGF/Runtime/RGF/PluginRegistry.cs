using System;
using System.Collections.Generic;
using Galashow.Core;

namespace Galashow.RGF
{
    /// <summary>
    /// 게임 플러그인 등록 및 관리 클래스
    /// 플러그인의 등록, 조회, 해제 등의 책임만 담당
    /// </summary>
    public class PluginRegistry
    {
        /// <summary>
        /// 등록된 플러그인 딕셔너리
        /// Key: UUID
        /// </summary>
        private Dictionary<string, IGamePlugin> _plugins = new Dictionary<string, IGamePlugin>();

        /// <summary>
        /// 게임 플러그인 등록
        /// </summary>
        /// <returns>플러그인 UUID</returns>
        public string Register(IGamePlugin plugin)
        {
            if (plugin == null)
            {
                GLog.Error("[PluginRegistry] Cannot register null plugin");
                return null;
            }
            
            string uuid = Guid.NewGuid().ToString();
            _plugins[uuid] = plugin;
            GLog.Info($"[PluginRegistry] Plugin registered: {uuid} ({plugin.GameName})");

            return uuid;
        }

        /// <summary>
        /// 게임 플러그인 등록 해제
        /// </summary>
        public void Unregister(string uuid)
        {
            if (_plugins.Remove(uuid))
            {
                GLog.Info($"[PluginRegistry] Plugin unregistered: {uuid}");
            }
        }

        /// <summary>
        /// 플러그인 조회
        /// </summary>
        public IGamePlugin Get(string uuid)
        {
            _plugins.TryGetValue(uuid, out var plugin);
            return plugin;
        }

        /// <summary>
        /// 플러그인 존재 여부 확인
        /// </summary>
        public bool Contains(string uuid)
        {
            return _plugins.ContainsKey(uuid);
        }

        /// <summary>
        /// 모든 플러그인 가져오기
        /// </summary>
        public IEnumerable<IGamePlugin> GetAll()
        {
            return _plugins.Values;
        }

        /// <summary>
        /// 등록된 플러그인 개수
        /// </summary>
        public int Count => _plugins.Count;

        /// <summary>
        /// 모든 플러그인 제거
        /// </summary>
        public void Clear()
        {
            _plugins.Clear();
            GLog.Info("[PluginRegistry] All plugins cleared");
        }
    }
}
