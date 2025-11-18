namespace Galashow.RGF
{
    /// <summary>
    /// RGF의 8단계 게임 생명주기 Phase
    /// </summary>
    public enum GamePhase
    {
        /// 게임 준비 단계 (플레이어 준비 확인)
        READY,
        
        /// 게임 데이터 구축 단계 (초기화)
        SETUP,
        
        /// 문제/상황 제시 단계
        PRESENT,
        
        /// 플레이어 입력 수집 단계
        INPUT,
        
        /// 입력 마감 대기 단계
        WAIT,

        /// 결과 계산 단계
        EXECUTE,

        /// 결과 연출 단계
        REVEAL,

        /// 정리 및 다음 라운드 준비 단계
        CLEANUP
    }
}
