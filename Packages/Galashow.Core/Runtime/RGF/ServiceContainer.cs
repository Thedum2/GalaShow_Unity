using System;
using System.Collections.Generic;

namespace Galashow.Core
{
    /// <summary>
    /// 서비스 컨테이너
    /// 게임 실행 중 필요한 서비스들을 등록하고 관리하는 경량 DI 컨테이너
    /// </summary>
    public class ServiceContainer
    {
        /// <summary>
        /// 서비스 접근용 딕셔너리
        /// Key: 서비스 타입명, Value: 서비스 인스턴스
        /// </summary>
        private Dictionary<string, object> _services = new Dictionary<string, object>();

        /// <summary>
        /// 타입별 서비스 딕셔너리 (빠른 조회용)
        /// </summary>
        private Dictionary<Type, object> _servicesByType = new Dictionary<Type, object>();

        /// <summary>
        /// 서비스 등록
        /// </summary>
        public void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                GLog.Error($"[ServiceContainer] Cannot register null service of type {typeof(T).Name}");
                return;
            }

            var type = typeof(T);
            var typeName = type.Name;

            _services[typeName] = service;
            _servicesByType[type] = service;

            GLog.Debug($"[ServiceContainer] Service registered: {typeName}");
        }

        /// <summary>
        /// 서비스 조회 (타입으로)
        /// </summary>
        public T Get<T>() where T : class
        {
            var type = typeof(T);

            // 타입으로 먼저 검색 (더 빠름)
            if (_servicesByType.TryGetValue(type, out var serviceByType))
            {
                return serviceByType as T;
            }

            // 타입명으로 검색 (하위 호환성)
            var typeName = type.Name;
            if (_services.TryGetValue(typeName, out var service))
            {
                return service as T;
            }

            return null;
        }

        /// <summary>
        /// 서비스 존재 여부 확인
        /// </summary>
        public bool Contains<T>() where T : class
        {
            return _servicesByType.ContainsKey(typeof(T));
        }

        /// <summary>
        /// 서비스 제거
        /// </summary>
        public void Unregister<T>() where T : class
        {
            var type = typeof(T);
            var typeName = type.Name;

            _services.Remove(typeName);
            _servicesByType.Remove(type);

            GLog.Debug($"[ServiceContainer] Service unregistered: {typeName}");
        }

        /// <summary>
        /// 모든 서비스 제거
        /// </summary>
        public void Clear()
        {
            _services.Clear();
            _servicesByType.Clear();
            GLog.Debug("[ServiceContainer] All services cleared");
        }

        /// <summary>
        /// 등록된 서비스 개수
        /// </summary>
        public int Count => _services.Count;

        /// <summary>
        /// 서비스 가져오기 (없으면 생성)
        /// </summary>
        public T GetOrCreate<T>() where T : class, new()
        {
            var service = Get<T>();
            if (service == null)
            {
                service = new T();
                Register(service);
            }
            return service;
        }

        /// <summary>
        /// 서비스 가져오기 (없으면 팩토리 함수로 생성)
        /// </summary>
        public T GetOrCreate<T>(Func<T> factory) where T : class
        {
            var service = Get<T>();
            if (service == null)
            {
                service = factory();
                Register(service);
            }
            return service;
        }
    }
}
