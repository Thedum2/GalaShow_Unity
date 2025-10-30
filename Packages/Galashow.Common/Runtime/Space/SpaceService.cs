using System.Collections.Generic;
using Galashow.Core;
using UnityEngine;

namespace Galashow.Common
{
    public class SpaceService : MonoBehaviour
    {
        public void PrepareForRound(object round)
        {
            // 씬/프리팹 로드, 풀 웜업 등 (프레임 분할 권장)
            GLog.Debug("Space PrepareForRound", tag: nameof(SpaceService));
        }
    }
}