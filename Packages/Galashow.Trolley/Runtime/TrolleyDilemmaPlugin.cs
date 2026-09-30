using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Galashow.Core;
using Galashow.RGF;

namespace Galashow.Trolley
{
    /// <summary>
    /// 트롤리 딜레마: 한 판으로 끝나는 미니게임 (docs/minigame-trolley.md)
    /// 참가자는 호스트가 지킬 선택지의 선로에 눕고, 트롤리는 호스트가 고른 반대편 선로로 간다 → 호스트와 같은 선택만 생존
    /// - 시청자: INPUT 단계에 채팅 1 또는 2, 마지막 입력 유지
    /// - 호스트: PRESENT~WAIT 동안 React 호스트 팝업(RGFManager_PromptOpened → RGFManager_HostInput)으로 SubmitHostChoice. 방송 화면에는 REVEAL 전까지 선택 여부만 표시
    /// - EXECUTE 시작 시 호스트 선택 확정, 없으면 rule.hostChoiceIfMissing
    /// </summary>
    public class TrolleyDilemmaPlugin : GamePluginBase<TrolleyGameData>
    {
        public override string GameName => "트롤리 딜레마";

        private readonly Dictionary<string, string> _votes = new Dictionary<string, string>();
        private readonly HashSet<string> _autoAssigned = new HashSet<string>();

        /// <summary>
        /// 무한 대기 WAIT에서 호스트가 고른 뒤 결과 공개 전까지 드럼롤을 보여 줄 시간(초)
        /// </summary>
        private const double MinimumWaitSeconds = 2.5;

        private readonly System.Diagnostics.Stopwatch _sinceHostChoice = new System.Diagnostics.Stopwatch();
        private readonly System.Random _random = new System.Random();
        private string _hostChoice;

        /// <summary>
        /// 이번 라운드 시청자 입력 (참가자 ID → 선택지 ID)
        /// </summary>
        public IReadOnlyDictionary<string, string> Votes => _votes;

        /// <summary>
        /// 호스트 선택 입력 여부 (선택 내용은 공개 전까지 화면에 쓰지 않는다)
        /// </summary>
        public bool HasHostChoice => _hostChoice != null;

        protected override void ValidateGameData(TrolleyGameData data)
        {
            if (data.Choices == null || data.Choices.Count != 2)
            {
                throw new FormatException("Trolley gameData requires exactly 2 choices");
            }

            if (data.Choices.Any(c => string.IsNullOrWhiteSpace(c.Id)) ||
                data.Choices.Select(c => c.Id.ToUpperInvariant()).Distinct().Count() != data.Choices.Count)
            {
                throw new FormatException("Trolley choices need unique ids");
            }

            var rule = data.Rule ?? (data.Rule = new TrolleyRule());
            if (rule.HostChoiceIfMissing != TrolleyRule.Random && rule.HostChoiceIfMissing != TrolleyRule.Abort)
            {
                throw new FormatException($"Unknown rule.hostChoiceIfMissing: {rule.HostChoiceIfMissing}");
            }
        }

        protected override GameObject CreateStageObject(GameState state)
        {
            // 월드 좌표로 배치하므로 씬 루트에 둔다 (CLEANUP·중단 시 기본 클래스가 제거)
            var go = new GameObject("TrolleyStage");
            go.AddComponent<TrolleyStage>();
            return go;
        }

        private TrolleyStage View => GetStage<TrolleyStage>();

        protected override Task OnReady()
        {
            if (Data == null)
            {
                throw new InvalidOperationException("[Trolley✗] gameData is required");
            }

            if (Data.InputTimeLimitMs > 0)
            {
                State.SetPhaseDuration(GamePhase.INPUT, Data.InputTimeLimitMs / 1000f);
            }

            _votes.Clear();
            _autoAssigned.Clear();
            _hostChoice = null;
            State.ResultData = null;

            GLog.Info($"[Trolley] Ready - Round {State.CurrentRound}: {Data.Title} ({Participants.Count}명)");
            return Task.CompletedTask;
        }

        protected override Task OnSetup()
        {
            if (View != null)
            {
                var participants = Participants
                    .Select(id => State.Players.TryGetValue(id, out var p) ? new TrolleyParticipant(id, p.Name, p.AvatarName) : new TrolleyParticipant(id, id, null))
                    .ToList();
                View.Build(Data, participants, IsPractice);
                View.HostKeyPressed += OnHostKey;
            }
            return Task.CompletedTask;
        }

        protected override Task OnPresent()
        {
            View?.ShowDilemma(Participants.Count);
            return Task.CompletedTask;
        }

        protected override Task OnInput()
        {
            View?.SetInputOpen(true);
            return Task.CompletedTask;
        }

        protected override void OnPlayerInput(PlayerInput input)
        {
            var choice = TrolleyRules.ParseChoice(input.Message, Data.Choices);
            if (choice == null)
            {
                return;
            }

            _votes[input.PlayerId] = choice;
            View?.SetVote(input.PlayerId, choice);
            View?.SetVoteCount(_votes.Count, Participants.Count);
        }

        protected override Task OnWait()
        {
            View?.SetInputOpen(false);
            AssignMissingVotes();

            // 입력 마감 후 호스트 선택 팝업(React)을 연다. 고르면 닫히고, EXECUTE에서도 닫는다
            OpenHostPrompt();

            // 무한 대기(phase_data WAIT -1): 호스트가 고를 때까지 기다리고, 고른 뒤 드럼롤 시간만큼 더 보여 준다
            if (State.IsInfinitePhase)
            {
                CompletePhaseWhen(() => _hostChoice != null && _sinceHostChoice.Elapsed.TotalSeconds >= MinimumWaitSeconds);
            }
            return Task.CompletedTask;
        }

        private void OpenHostPrompt()
        {
            var prompt = new HostPrompt
            {
                Command = "choice",
                Title = Data.Title,
                Description = Data.Description,
                ActionLabel = "지키기",
            };
            for (int i = 0; i < Data.Choices.Count; i++)
            {
                prompt.Options.Add(new HostPromptOption
                {
                    Id = Data.Choices[i].Id,
                    Number = i + 1,
                    Label = Data.Choices[i].Label,
                    Description = Data.Choices[i].Description,
                });
            }
            ShowHostPrompt(prompt);
        }

        /// <summary>
        /// 입력 마감: 입력하지 않은 참가자에게 1·2 중 하나를 자동으로 배정한다 (rule.noInput = random)
        /// </summary>
        private void AssignMissingVotes()
        {
            if (Data.Rule?.NoInput != TrolleyRule.Random)
            {
                return;
            }

            foreach (var playerId in Participants)
            {
                if (_votes.ContainsKey(playerId))
                {
                    continue;
                }

                var choice = Data.Choices[_random.Next(Data.Choices.Count)].Id;
                _votes[playerId] = choice;
                _autoAssigned.Add(playerId);
                View?.SetVote(playerId, choice, auto: true);
            }

            if (_autoAssigned.Count > 0)
            {
                View?.SetVoteCount(_votes.Count, Participants.Count);
                GLog.Info($"[Trolley] {_autoAssigned.Count}명 자동 선택");
            }
        }

        /// <summary>
        /// 호스트 선택 입력. PRESENT~WAIT 동안만 받고, 다시 보내면 바꾼다.
        /// 외부(향후 Trolley_HostChoice 메시지)에서도 호출할 수 있다.
        /// </summary>
        /// <returns>받아들였으면 true</returns>
        public bool SubmitHostChoice(string choiceId)
        {
            if (State == null || Data == null ||
                (State.CurrentPhase != GamePhase.PRESENT && State.CurrentPhase != GamePhase.INPUT && State.CurrentPhase != GamePhase.WAIT))
            {
                return false;
            }

            var choice = TrolleyRules.ParseHostChoice(choiceId, Data.Choices);
            if (choice == null)
            {
                return false;
            }

            _hostChoice = choice;
            _sinceHostChoice.Restart();
            View?.SetHostReady(true);
            GLog.Debug("[Trolley] Host choice received");
            return true;
        }

        /// <summary>
        /// React 호스트 입력: command "choice", value = 선택지 ID 또는 번호
        /// </summary>
        protected override bool OnHostInput(HostInput input)
        {
            return input.Command == "choice" && SubmitHostChoice(input.Value);
        }

        private void OnHostKey(int choiceNumber)
        {
            SubmitHostChoice(choiceNumber.ToString());
        }

        protected override Task OnExecute()
        {
            // 입력 마감: 호스트 선택 확정
            CloseHostPrompt();
            string source = TrolleyRules.HostSource;
            if (_hostChoice == null)
            {
                if (Data.Rule.HostChoiceIfMissing == TrolleyRule.Abort)
                {
                    throw new InvalidOperationException("[Trolley✗] Host did not choose (rule: abort)");
                }

                _hostChoice = Data.Choices[_random.Next(Data.Choices.Count)].Id;
                source = TrolleyRules.RandomSource;
            }

            var result = TrolleyRules.Judge(Data, Participants, _votes, _hostChoice, source, _autoAssigned);
            result.RoundNumber = State.CurrentRound;
            State.ResultData = result;

            foreach (var r in result.Results)
            {
                SetSurvived(r.ParticipantId, r.Survived);
            }

            GLog.Info($"[Trolley] Host {_hostChoice}({source}) - Survivors {result.Survivors.Count}, Eliminated {result.Eliminated.Count}");
            return Task.CompletedTask;
        }

        protected override Task OnReveal()
        {
            if (State.ResultData is TrolleyGameResult result)
            {
                View?.PlayReveal(result, State.PhaseDuration);
            }
            return Task.CompletedTask;
        }

        protected override Task OnCleanup()
        {
            _votes.Clear();
            _autoAssigned.Clear();
            _hostChoice = null;
            return Task.CompletedTask;
        }

        protected override void OnAborted()
        {
            _votes.Clear();
            _autoAssigned.Clear();
            _hostChoice = null;
        }
    }
}
