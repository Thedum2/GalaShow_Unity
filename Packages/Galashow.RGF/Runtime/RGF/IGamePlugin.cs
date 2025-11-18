using System.Threading.Tasks;

namespace Galashow.RGF
{
    /// <summary>
    /// 게임 플러그인 인터페이스
    /// 모든 미니게임은 이 인터페이스를 구현하여 8단계 생명주기를 처리합니다.
    /// </summary>
    public interface IGamePlugin
    {
        /// <summary>
        /// 게임 표시 이름
        /// </summary>
        string GameName { get; }

        /// <summary>
        /// READY Phase: 게임 준비 단계
        /// 플레이어 준비 확인, 초기 UI 표시 등
        /// </summary>
        /// <param name="state">게임 상태 (실행 컨텍스트 포함)</param>
        Task OnReadyAsync(GameState state);

        /// <summary>
        /// SETUP Phase: 게임 데이터 구축 단계
        /// 리소스 로드, 게임 오브젝트 생성 등
        /// </summary>
        /// <param name="state">게임 상태 (실행 컨텍스트 포함)</param>
        Task OnSetupAsync(GameState state);

        /// <summary>
        /// PRESENT Phase: 문제/상황 제시 단계
        /// 문제 화면 표시, 애니메이션 재생 등
        /// </summary>
        /// <param name="state">게임 상태 (실행 컨텍스트 포함)</param>
        Task OnPresentAsync(GameState state);

        /// <summary>
        /// INPUT Phase: 플레이어 입력 수집 단계
        /// 선택지 활성화, 입력 대기 등
        /// </summary>
        /// <param name="state">게임 상태 (실행 컨텍스트 포함)</param>
        Task OnInputAsync(GameState state);

        /// <summary>
        /// WAIT Phase: 입력 마감 단계
        /// 입력 UI 비활성화, 최종 확인 등
        /// </summary>
        /// <param name="state">게임 상태 (실행 컨텍스트 포함)</param>
        Task OnWaitAsync(GameState state);

        /// <summary>
        /// EXECUTE Phase: 결과 계산 단계
        /// 게임 로직 실행, 생존/탈락 판정 등
        /// </summary>
        /// <param name="state">게임 상태 (실행 컨텍스트 포함)</param>
        Task OnExecuteAsync(GameState state);

        /// <summary>
        /// REVEAL Phase: 결과 연출 단계
        /// 결과 화면 표시, 애니메이션, 효과음 등
        /// </summary>
        /// <param name="state">게임 상태 (실행 컨텍스트 포함)</param>
        Task OnRevealAsync(GameState state);

        /// <summary>
        /// CLEANUP Phase: 정리 및 다음 준비 단계
        /// 리소스 정리, 상태 초기화 등
        /// </summary>
        /// <param name="state">게임 상태 (실행 컨텍스트 포함)</param>
        Task OnCleanupAsync(GameState state);

        /// <summary>
        /// Phase 전환 시 호출 (선택적 구현)
        /// </summary>
        /// <param name="from">이전 Phase</param>
        /// <param name="to">다음 Phase</param>
        Task OnPhaseTransitionAsync(GamePhase from, GamePhase to);
    }
}
