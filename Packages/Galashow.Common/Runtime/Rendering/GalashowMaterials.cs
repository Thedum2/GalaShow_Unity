using System.Collections.Generic;
using UnityEngine;

namespace Galashow.Common
{
    /// <summary>
    /// 코드로 만드는 오브젝트용 머티리얼.
    /// CreatePrimitive의 기본 머티리얼(Standard)은 씬에 Standard 머티리얼이 없으면 빌드에서 셰이더가 빠져 마젠타로 보인다.
    /// Resources/Galashow/LitDefault(Standard) 에셋을 복제해 쓰므로 셰이더가 항상 빌드에 포함된다.
    /// </summary>
    public static class GalashowMaterials
    {
        const string LitPath = "Galashow/LitDefault";

        static Material _lit;
        static readonly Dictionary<Color32, Material> _cache = new Dictionary<Color32, Material>();

        /// <summary>
        /// 색별로 공유하는 불투명 머티리얼
        /// </summary>
        public static Material Lit(Color color)
        {
            var key = (Color32)color;
            if (_cache.TryGetValue(key, out var material) && material != null)
            {
                return material;
            }

            if (_lit == null)
            {
                _lit = Resources.Load<Material>(LitPath);
            }

            material = _lit != null ? new Material(_lit) : new Material(Shader.Find("Standard"));
            material.color = color;
            _cache[key] = material;
            return material;
        }
    }
}
