using UnityEngine;

namespace Galashow.RGF
{
    /// <summary>
    /// 미니게임 무대(3D 오브젝트 + 게임 전용 UI) 프리팹의 루트 컴포넌트
    /// GamePluginBase가 SETUP에서 생성하고 CLEANUP·중단 시 제거한다.
    /// 게임별 연출은 이 클래스를 상속한 View에 메서드로 둔다. 판정 로직은 플러그인에 둔다.
    /// </summary>
    public class GameStage : MonoBehaviour
    {
        /// <summary>
        /// 생성 직후 호출
        /// </summary>
        public virtual void Initialize(GameState state)
        {
        }
    }
}
