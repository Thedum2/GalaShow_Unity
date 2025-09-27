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

        public void WarmupPrefabs(IList<GameObject> prefabs, int perFrame = 20)
        {
            if (prefabs == null || prefabs.Count == 0) return;
            int i = 0;
            var token = new object();
            TaskRunner.Instance.Every(0f, () =>
            {
                int count = Mathf.Min(perFrame, prefabs.Count - i);
                for (int k = 0; k < count; k++)
                {
                    // TODO: ObjectPool.Warmup(prefabs[i++]);
                    i++;
                }
                if (i >= prefabs.Count) TaskRunner.Instance.CancelAll(token);
            }, owner: token);
        }
    }
}