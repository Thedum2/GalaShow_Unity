using System;
using System.Collections.Generic;
using System.Linq;
using Galashow.Core;
using UnityEngine;

namespace Galashow.Common
{
    /// <summary>
    /// 시청자 아바타 카탈로그: Admin viewer_avatars.name → 캐릭터 프리팹
    /// Resources/Galashow/CharacterCatalog 에셋을 쓴다. 이름은 공백·대소문자를 무시하고 비교하며 별칭을 여러 개 둘 수 있다.
    /// </summary>
    [CreateAssetMenu(menuName = "Galashow/Character Catalog", fileName = "CharacterCatalog")]
    public class CharacterCatalog : ScriptableObject
    {
        public const string ResourcePath = "Galashow/CharacterCatalog";

        [Serializable]
        public class Entry
        {
            [Tooltip("Admin 시청자 아바타 이름과 같게 둔다")]
            public string name;
            [Tooltip("같은 캐릭터로 인정할 다른 이름")]
            public string[] aliases = Array.Empty<string>();
            public GameObject prefab;
            [Tooltip("다른 캐릭터와 키를 맞추기 위한 배율")]
            public float scale = 1f;
        }

        public List<Entry> entries = new List<Entry>();

        static CharacterCatalog _instance;

        public static CharacterCatalog Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<CharacterCatalog>(ResourcePath);
                    if (_instance == null)
                    {
                        GLog.Warn($"[CharacterCatalog] Resources/{ResourcePath} not found");
                        _instance = CreateInstance<CharacterCatalog>();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 아바타 이름으로 찾는다. 없으면 null
        /// </summary>
        public Entry Find(string avatarName)
        {
            var key = Normalize(avatarName);
            if (key.Length == 0)
            {
                return null;
            }

            return entries.FirstOrDefault(e => e.prefab != null &&
                (Normalize(e.name) == key || (e.aliases != null && e.aliases.Any(a => Normalize(a) == key))));
        }

        /// <summary>
        /// 이름으로 찾고, 없으면 seed로 고정된 캐릭터를 돌려준다 (같은 참가자는 항상 같은 캐릭터)
        /// </summary>
        public Entry FindOrFallback(string avatarName, int seed)
        {
            var entry = Find(avatarName);
            if (entry != null)
            {
                return entry;
            }

            var available = entries.Where(e => e.prefab != null).ToList();
            if (available.Count == 0)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(avatarName))
            {
                GLog.Warn($"[CharacterCatalog] '{avatarName}' 캐릭터가 카탈로그에 없어 대체 캐릭터를 씁니다");
            }
            return available[(seed & int.MaxValue) % available.Count];
        }

        static string Normalize(string value) =>
            string.IsNullOrWhiteSpace(value) ? "" : new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
    }
}
