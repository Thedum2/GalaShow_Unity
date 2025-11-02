using System.Collections.Generic;
using Galashow.Core;

namespace Galashow.Trolley
{
    /// <summary>
    /// 트롤리 딜레마 게임 데이터
    /// </summary>
    public class TrolleyGameData
    {
        /// <summary>
        /// 라운드 번호
        /// </summary>
        public int RoundNumber { get; set; }

        /// <summary>
        /// 딜레마 제목
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 딜레마 설명
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 선택지 목록
        /// </summary>
        public List<TrolleyChoice> Choices { get; set; } = new List<TrolleyChoice>();

        /// <summary>
        /// 입력 제한 시간 (초)
        /// </summary>
        public float InputTimeLimit { get; set; } = 5f;

        /// <summary>
        /// 난이도
        /// </summary>
        public string Difficulty { get; set; } = "normal";

        /// <summary>
        /// 정답 선택지 인덱스 (-1이면 정답 없음)
        /// </summary>
        public int CorrectChoiceIndex { get; set; } = -1;
    }

    /// <summary>
    /// 트롤리 선택지
    /// </summary>
    public class TrolleyChoice
    {
        /// <summary>
        /// 선택지 ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 선택지 텍스트
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 선택지 설명
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 선택지 아이콘/이미지 경로
        /// </summary>
        public string IconPath { get; set; }

        /// <summary>
        /// 생존율 (0~1)
        /// </summary>
        public float SurvivalRate { get; set; } = 0.5f;

        /// <summary>
        /// 이 선택지를 선택한 플레이어 ID 목록
        /// </summary>
        public List<string> SelectedPlayers { get; set; } = new List<string>();
    }

    /// <summary>
    /// 트롤리 게임 결과
    /// GameResult를 상속받아 트롤리 게임 전용 데이터 추가
    /// </summary>
    public class TrolleyGameResult : GameResult
    {
        /// <summary>
        /// 선택지별 통계
        /// </summary>
        public Dictionary<string, ChoiceStatistics> ChoiceStats { get; set; } = new Dictionary<string, ChoiceStatistics>();

        /// <summary>
        /// 평균 선택 시간 (초)
        /// </summary>
        public float AverageSelectionTime { get; set; }

        /// <summary>
        /// 결과 초기화 (오버라이드)
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            ChoiceStats.Clear();
            AverageSelectionTime = 0;
        }
    }

    /// <summary>
    /// 선택지 통계
    /// </summary>
    public class ChoiceStatistics
    {
        /// <summary>
        /// 선택한 플레이어 수
        /// </summary>
        public int SelectionCount { get; set; }

        /// <summary>
        /// 선택 비율 (0~1)
        /// </summary>
        public float SelectionRate { get; set; }

        /// <summary>
        /// 이 선택지를 선택한 플레이어 중 생존자 수
        /// </summary>
        public int SurvivorCount { get; set; }

        /// <summary>
        /// 이 선택지의 실제 생존율
        /// </summary>
        public float ActualSurvivalRate { get; set; }
    }
}
