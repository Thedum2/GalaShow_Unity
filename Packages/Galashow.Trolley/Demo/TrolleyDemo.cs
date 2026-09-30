using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Galashow.Bridge;
using Galashow.Core;
using Galashow.RGF;

namespace Galashow.Trolley.Demo
{
    /// <summary>
    /// React 없이 트롤리 딜레마를 한 판 실행하는 데모 (끝나면 R 키로 다시)
    /// 실제 계약과 같은 경로(Initialize/RegisterPlugin/StartRound REQ, ChatInput NTY)로 BridgeManager에 메시지를 넣는다.
    /// 가상 시청자가 INPUT 동안 1 또는 2를 채팅한다. 호스트 선택은 데모 호스트 팝업 (Client에서는 React 팝업, 고르지 않으면 무작위).
    /// </summary>
    public class TrolleyDemo : MonoBehaviour
    {
        [Header("참가자")]
        [SerializeField, Range(2, 60)] private int playerCount = 16;

        [Tooltip("로비에서 고른 시청자 아바타 이름 (Admin viewer_avatars.name). 참가자에게 순서대로 배정된다")]
        [SerializeField] private string[] avatarNames = { "Colobus", "Gecko", "Herring", "Muskrat", "Pudu", "Sparrow", "Squid", "Taipan" };
        [SerializeField, Range(0f, 1f)] private float inputRate = 0.9f;
        [SerializeField, Range(0f, 1f)] private float preferA = 0.5f;

        [Header("단계 시간 (초, StartRound phaseDuration)")]
        [SerializeField] private int present = 6;
        [SerializeField] private int input = 12;
        [SerializeField] private int wait = 3;
        [SerializeField] private int reveal = 8;


        private string _pluginUuid;
        private int _round;
        private bool _finished;
        private int _messageSeq;
        private readonly System.Random _random = new System.Random();
        private readonly List<int> _dilemmaOrder = new List<int>();

        private void Start()
        {
            var adapter = RGFBridgeAdapter.Instance;
            adapter.PluginRegistered += OnPluginRegistered;

            SendInitialize();
            Send("REQ", "RGFManager_RegisterPlugin", new[] { new { miniGameIdx = 1, miniGameName = TrolleyPluginInstaller.PluginId } });
        }

        private void OnDestroy()
        {
            if (RGFBridgeAdapter.Instance != null)
            {
                RGFBridgeAdapter.Instance.PluginRegistered -= OnPluginRegistered;
            }
        }

        private void OnPluginRegistered(string name, string uuid)
        {
            if (name != TrolleyPluginInstaller.PluginId)
            {
                return;
            }

            _pluginUuid = uuid;
            StartCoroutine(RoundLoop());
        }

        private IEnumerator RoundLoop()
        {
            var manager = RGFManager.Instance;
            while (true)
            {
                _round = 1;
                StartRound(_round);
                StartCoroutine(SimulateViewers(_round));

                // StartRound가 거부되면(데이터 오류 등) 실행되지 않으므로 잠시만 기다린다
                float requested = Time.time;
                yield return new WaitUntil(() => manager.IsRunning || Time.time - requested > 2f);
                yield return new WaitUntil(() => !manager.IsRunning);

                // 한 판으로 끝. R 키로 새 참가자와 다시 시작
                _finished = true;
                yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.R));
                _finished = false;
                manager.ResetState();
                SendInitialize();
            }
        }

        private void StartRound(int round)
        {
            var dilemma = NextDilemma();
            var gameData = new
            {
                roundNumber = round,
                dilemmaId = dilemma.Id,
                title = dilemma.Title,
                description = dilemma.Description,
                choices = new[]
                {
                    new { id = "A", label = dilemma.A, description = dilemma.ADescription },
                    new { id = "B", label = dilemma.B, description = dilemma.BDescription }
                }
            };

            Send("REQ", "RGFManager_StartRound", new
            {
                miniGamePluginIdx = _pluginUuid,
                roundNumber = round,
                gameData,
                phaseDuration = new Dictionary<string, int>
                {
                    ["READY"] = 1, ["SETUP"] = 1, ["PRESENT"] = present, ["INPUT"] = input,
                    ["WAIT"] = wait, ["EXECUTE"] = 1, ["REVEAL"] = reveal, ["CLEANUP"] = 1
                }
            });
        }

        /// <summary>
        /// INPUT 동안 생존 참가자들이 무작위 시각에 A/B를 채팅한다 (일부는 마음을 바꾸거나 입력하지 않음)
        /// </summary>
        private IEnumerator SimulateViewers(int round)
        {
            var manager = RGFManager.Instance;
            yield return new WaitUntil(() => manager.State.CurrentPhase == GamePhase.INPUT || !manager.IsRunning);
            if (!manager.IsRunning)
            {
                yield break;
            }

            float duration = Mathf.Max(1f, manager.State.PhaseDuration - 0.5f);
            var messages = new List<(float time, int playerIdx, string message)>();
            foreach (var player in manager.State.GetAlivePlayers())
            {
                if (_random.NextDouble() > inputRate) continue;

                int idx = int.Parse(player.Id);
                messages.Add(((float)_random.NextDouble() * duration, idx, RandomVote()));
                if (_random.NextDouble() < 0.15)
                {
                    messages.Add(((float)_random.NextDouble() * duration, idx, RandomVote()));
                }
            }

            float start = Time.time;
            foreach (var m in messages.OrderBy(m => m.time))
            {
                while (Time.time - start < m.time) yield return null;
                if (manager.State.CurrentPhase != GamePhase.INPUT) yield break;

                Send("NTY", "RGFManager_ChatInput", new
                {
                    roundNumber = round,
                    inputEventTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    inputIdx = _messageSeq,
                    chatInfo = new[] { new { playerIdx = m.playerIdx, message = m.message } }
                });
            }
        }

        private string RandomVote()
        {
            return _random.NextDouble() < preferA ? "1" : "2";
        }

        private void SendInitialize()
        {
            var players = Enumerable.Range(1, playerCount).Select(i => new
            {
                playerIdx = i,
                playerType = "human",
                playerName = $"시청자{i:00}"
            });
            Send("REQ", "RGFManager_Initialize", new
            {
                sessionId = "trolley-demo",
                playerInfo = players,
                config = new { avatarNames }
            });
        }

        private TrolleySampleDilemmas.Dilemma NextDilemma()
        {
            if (_dilemmaOrder.Count == 0)
            {
                _dilemmaOrder.AddRange(Enumerable.Range(0, TrolleySampleDilemmas.All.Length).OrderBy(_ => _random.Next()));
            }

            int index = _dilemmaOrder[0];
            _dilemmaOrder.RemoveAt(0);
            return TrolleySampleDilemmas.All[index];
        }

        private void Send(string type, string route, object data)
        {
            _messageSeq++;
            var message = new
            {
                id = $"demo_{_messageSeq}",
                type,
                route,
                ok = true,
                data,
                timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
            };
            BridgeManager.Instance.ReceiveMessage(JsonConvert.SerializeObject(message));
        }

        private GUIStyle _promptStyle;
        private bool _hostPicked;
        private string _lastPromptId;

        /// <summary>
        /// React 없이 시험할 때 호스트 선택 팝업(HostPromptBus)을 대신 띄운다. 실제 Client에서는 React 팝업이 뜬다.
        /// </summary>
        private void DrawHostPrompt()
        {
            var prompt = HostPromptBus.Current;
            if (prompt == null)
            {
                return;
            }
            if (prompt.PromptId != _lastPromptId)
            {
                _lastPromptId = prompt.PromptId;
                _hostPicked = false;
            }

            _promptStyle ??= new GUIStyle(GUI.skin.box) { fontSize = 18, wordWrap = true, alignment = TextAnchor.UpperCenter };
            var rect = new Rect(Screen.width / 2f - 260, 130, 520, 90 + prompt.Options.Count * 44);
            var status = _hostPicked ? "선택 완료 (마감 전 변경 가능)" : prompt.Hint;
            GUI.Box(rect, $"[데모 호스트 팝업] {prompt.Title}\n{status}", _promptStyle);
            for (int i = 0; i < prompt.Options.Count; i++)
            {
                var option = prompt.Options[i];
                if (GUI.Button(new Rect(rect.x + 20, rect.y + 70 + i * 44, rect.width - 40, 38), $"{option.Number}. {option.Label} — {prompt.ActionLabel}"))
                {
                    _hostPicked = RGFManager.Instance.SubmitHostInput(new HostInput(prompt.Command, option.Id)) || _hostPicked;
                }
            }
        }

        private void OnGUI()
        {
            DrawHostPrompt();
            // 데모 안내 (실제 방송 화면에는 없음)
            GUI.Label(new Rect(12, Screen.height - 28, 1000, 24),
                _finished
                    ? "[데모] 게임 종료 - R 키로 다시 시작"
                    : "[데모] 호스트 선택: 화면 위 호스트 팝업 (트롤리는 반대편으로)  |  참가자 " + playerCount + "명");
        }
    }
}
