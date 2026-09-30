using System.Collections.Generic;
using Newtonsoft.Json;
using Galashow.RGF;

namespace Galashow.Trolley
{
    /// <summary>
    /// 트롤리 딜레마 라운드 데이터 (StartRound gameData, docs/minigame-trolley.md 6.1)
    /// </summary>
    public class TrolleyGameData
    {
        [JsonProperty("roundNumber")]
        public int RoundNumber { get; set; }

        /// <summary>
        /// 딜레마 ID (결과 기록용, 바꾸지 않음)
        /// </summary>
        [JsonProperty("dilemmaId")]
        public string DilemmaId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// 선택지. 현재 화면·입력은 2개(A/B)를 기준으로 한다.
        /// </summary>
        [JsonProperty("choices")]
        public List<TrolleyChoice> Choices { get; set; } = new List<TrolleyChoice>();

        /// <summary>
        /// 입력 시간(ms). 0 이하면 StartRound phaseDuration.INPUT을 그대로 쓴다.
        /// </summary>
        [JsonProperty("inputTimeLimitMs")]
        public int InputTimeLimitMs { get; set; }

        /// <summary>
        /// 판정 정책. 생략하면 기획 문서 4.2의 예시 값 (PRD 미정 사항)
        /// </summary>
        [JsonProperty("rule")]
        public TrolleyRule Rule { get; set; } = new TrolleyRule();
    }

    /// <summary>
    /// 선택지
    /// </summary>
    public class TrolleyChoice
    {
        /// <summary>
        /// "A" / "B". minigame_controls.key_name과 같다.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    /// <summary>
    /// 판정 정책 (docs/minigame-trolley.md 4.2 rule). 값은 예시이며 PRD에서 미정이다.
    /// </summary>
    public class TrolleyRule
    {
        public const string Eliminate = "eliminate";
        public const string Survive = "survive";
        public const string AllSurvive = "all_survive";
        public const string AllEliminated = "all_eliminated";
        public const string Random = "random";
        public const string Abort = "abort";

        /// <summary>
        /// 입력하지 않은 참가자: random(입력 마감 때 1·2 중 자동 선택, 기본) / eliminate(탈락) / survive(생존)
        /// </summary>
        [JsonProperty("noInput")]
        public string NoInput { get; set; } = Random;

        /// <summary>
        /// 전원이 틀렸을 때: all_survive(전원 생존) / all_eliminated(그대로 전원 탈락)
        /// </summary>
        [JsonProperty("allEliminated")]
        public string AllEliminatedPolicy { get; set; } = AllSurvive;

        /// <summary>
        /// 입력 마감까지 호스트가 고르지 않았을 때: random / abort
        /// </summary>
        [JsonProperty("hostChoiceIfMissing")]
        public string HostChoiceIfMissing { get; set; } = Random;
    }

    /// <summary>
    /// 참가자별 판정 (docs/minigame-trolley.md 7절)
    /// </summary>
    public class TrolleyPlayerResult
    {
        public string ParticipantId { get; set; }

        /// <summary>
        /// 선택지 ID. 미입력이면 null
        /// </summary>
        public string Choice { get; set; }

        /// <summary>
        /// 입력하지 않아 자동으로 고른 선택인지
        /// </summary>
        public bool AutoAssigned { get; set; }

        public bool Survived { get; set; }

        public string Reason { get; set; }
    }

    /// <summary>
    /// 트롤리 라운드 결과 (GameState.ResultData)
    /// </summary>
    public class TrolleyGameResult : GameResult
    {
        public string DilemmaId { get; set; }

        public string HostChoice { get; set; }

        /// <summary>
        /// 호스트 선택의 화면 번호 (1부터, 시청자가 채팅한 번호)
        /// </summary>
        public int HostChoiceNumber { get; set; }

        /// <summary>
        /// host(호스트 직접 선택) / random(미선택으로 무작위 결정)
        /// </summary>
        public string HostChoiceSource { get; set; }

        /// <summary>
        /// 선택지 ID별 인원. 미입력은 "none"
        /// </summary>
        public Dictionary<string, int> Distribution { get; set; } = new Dictionary<string, int>();

        public List<TrolleyPlayerResult> Results { get; set; } = new List<TrolleyPlayerResult>();

        /// <summary>
        /// 전원 탈락이라 정책(all_survive)에 따라 전원 생존 처리했는지
        /// </summary>
        public bool RescuedAllEliminated { get; set; }

        public override void Reset()
        {
            base.Reset();
            DilemmaId = null;
            HostChoice = null;
            HostChoiceSource = null;
            Distribution.Clear();
            Results.Clear();
            RescuedAllEliminated = false;
        }
    }
}
