using System.Collections.Generic;

namespace Galashow.RGF
{
    /// <summary>
    /// 게임 결과 베이스 클래스
    /// 모든 미니게임의 결과는 이 클래스를 상속받아 구현
    /// </summary>
    public class GameResult
    {
        /// <summary>
        /// 라운드 번호
        /// </summary>
        public int RoundNumber { get; set; }

        /// <summary>
        /// 생존자 목록
        /// </summary>
        public List<string> Survivors { get; set; } = new List<string>();

        /// <summary>
        /// 탈락자 목록
        /// </summary>
        public List<string> Eliminated { get; set; } = new List<string>();

        /// <summary>
        /// 전체 생존율 (0~1)
        /// </summary>
        public float SurvivalRate { get; set; }

        /// <summary>
        /// 총 걸린 시간 (초)
        /// </summary>
        public float TotalPlayTime { get; set; }

        /// <summary>
        /// 라운드 시작 시간 (Time.time 기준)
        /// </summary>
        public float StartTime { get; set; }

        /// <summary>
        /// 라운드 종료 시간 (Time.time 기준)
        /// </summary>
        public float EndTime { get; set; }


        /// <summary>
        /// 추가 커스텀 데이터
        /// </summary>
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 결과 초기화
        /// </summary>
        public virtual void Reset()
        {
            Survivors.Clear();
            Eliminated.Clear();
            SurvivalRate = 0;
            TotalPlayTime = 0;
            StartTime = 0;
            EndTime = 0;
            CustomData.Clear();
        }

        /// <summary>
        /// 총 플레이어 수
        /// </summary>
        public int TotalPlayers => Survivors.Count + Eliminated.Count;

        /// <summary>
        /// 생존자 비율 계산 및 업데이트
        /// </summary>
        public void CalculateSurvivalRate()
        {
            int total = TotalPlayers;
            SurvivalRate = total > 0 ? (float)Survivors.Count / total : 0;
        }

        /// <summary>
        /// 총 걸린 시간 계산 및 업데이트
        /// </summary>
        public void CalculateTotalPlayTime()
        {
            TotalPlayTime = EndTime - StartTime;
        }
    }
}
