using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using Galashow.RGF;
using Sfx = Galashow.Trolley.TrolleyAudio.Sfx;

namespace Galashow.Trolley
{
    /// <summary>
    /// 트롤리 딜레마 무대: 3D 샘플 월드 + 방송 UI + 연출(Feel)
    /// 판정은 TrolleyDilemmaPlugin이 하고, 이 컴포넌트는 표시·연출과 호스트 키 입력만 담당한다.
    /// 흐름: 문제 공개(항공샷→추적샷, 카드 진입) → 입력(카메라 접근, 마지막 N초 심장박동·카운트다운)
    ///      → 마감(레버 클로즈업, 드럼롤) → 결과(레버·선택 공개 → 질주 → 충돌 슬로모션 → 집계·축하)
    /// </summary>
    public class TrolleyStage : GameStage
    {
        /// <summary>
        /// 무대에서 호스트 선택 입력 (현재 호스트 선택은 React 팝업 → RGFManager_HostInput 경로를 쓴다)
        /// </summary>
        public event Action<int> HostKeyPressed;

        /// <summary>
        /// 입력 마감 때 트롤리가 멈춰 서는 지점 (분기점 앞)
        /// </summary>
        const float StopZ = -4.5f;
        const float WheelRadius = 0.26f;
        const float ShotLength = 3.4f;
        static readonly Color SkyCalm = new Color(0.98f, 0.62f, 0.42f);
        static readonly Color SkyTense = new Color(0.45f, 0.08f, 0.12f);
        static readonly Color SunCalm = new Color(1f, 0.86f, 0.7f);
        static readonly Color SunTense = new Color(1f, 0.35f, 0.3f);

        GameState _state;
        TrolleyGameData _data;
        TrolleyWorld _world;
        TrolleyHud _hud;
        TrolleyFx _fx;
        TrolleyAudio _audio;
        TrolleyCast _cast;
        TrolleyFxProfile _profile;
        ParticleSystem _smoke;
        ParticleSystem _sparks;

        TrolleyCameraDirector _camera;
        bool _approaching;
        float _approachStart;
        float _approachDuration;
        Vector3 _lastTrolleyPos;
        float _trolleySpeed;
        Coroutine _shotLoop;
        bool _revealing;
        bool _practice;
        bool _drumroll;
        bool _hostReady;
        int _lastSecond = -1;
        float _lastVoteSound;

        public override void Initialize(GameState state)
        {
            _state = state;
        }

        public void Build(TrolleyGameData data, System.Collections.Generic.IReadOnlyList<TrolleyParticipant> participants, bool practice = false)
        {
            _data = data;
            _practice = practice;
            _profile = TrolleyFxProfile.Load();

            _world = new TrolleyWorld(transform);
            _world.Build(data.Choices);
            _hud = new TrolleyHud(transform);
            _hud.Build(data);


            _audio = gameObject.AddComponent<TrolleyAudio>();
            _audio.Setup(_profile);
            _fx = gameObject.AddComponent<TrolleyFx>();
            _fx.Build(_world, _hud, _profile);

            _cast = gameObject.AddComponent<TrolleyCast>();
            _cast.Build(_world, data, participants);

            _smoke = TrolleyParticles.Smoke(_world.Chimney, new Vector3(0f, 0.6f, 0f));
            _sparks = TrolleyParticles.Sparks(_world.WheelsFront, Vector3.zero);

            _hud.QuestionPanel.SetActive(false);
            _hud.StatusGroup.SetActive(false);
            SetCardsVisible(false);

            // 시작 구도: 분기점 위 높은 항공샷에서 천천히 돈다
            _world.CameraRig.position = new Vector3(0f, 26f, -4f);
            _world.CameraRig.LookAt(new Vector3(0f, 0f, 7f));
            _camera = new TrolleyCameraDirector(_world.CameraRig, _world.Camera);
            _camera.Play(AerialShot, 3f, cut: true);
            _lastTrolleyPos = _world.Trolley.position;
        }

        #region Phase API (TrolleyDilemmaPlugin이 호출)

        public void ShowDilemma(int participants)
        {
            SetVoteCount(0, participants);
            SetHostReady(false, silent: true);

            // 문제 공개 ~ 입력 마감 동안 먼 곳에서 달려와 마감 시점에 분기점 앞에 멈춘다
            _approaching = true;
            _approachStart = Time.time;
            // 무한 대기(-1) 단계는 3초로 보고 계산한다
            float Timed(GamePhase p) { float d = _state.GetPhaseDuration(p); return d < 0f ? 3f : d; }
            _approachDuration = Mathf.Max(3f, Timed(GamePhase.PRESENT) + Timed(GamePhase.INPUT) + Timed(GamePhase.WAIT));
            StartCoroutine(IntroRoutine());
        }

        public void SetInputOpen(bool open)
        {
            foreach (var card in _hud.Cards)
            {
                card.Hint.gameObject.SetActive(open);
            }

            if (open)
            {
                _lastSecond = -1;
                _shotLoop = StartCoroutine(InputShots());
                StartCoroutine(Callout("예측 시작!", "호스트가 지킬 선로를 채팅으로! 1 또는 2", new Color(1f, 0.84f, 0.25f), 1.1f, Sfx.Slam));
            }
            else
            {
                if (_shotLoop != null) StopCoroutine(_shotLoop);
                StartCoroutine(InputClosedRoutine());
            }
        }

        public void SetVoteCount(int votes, int participants)
        {
            _hud.StatusText.text = $"입력 {votes} / {participants}명";
            if (votes == 0 || _fx == null) return;

            _fx.Vote();
            if (Time.unscaledTime - _lastVoteSound > 0.07f)
            {
                _lastVoteSound = Time.unscaledTime;
                _audio.Play(Sfx.Vote, _profile.voteVolume, UnityEngine.Random.Range(0.9f, 1.25f));
            }
        }

        public void SetHostReady(bool ready) => SetHostReady(ready, silent: false);

        /// <summary>
        /// 참가자 입력: 캐릭터가 고른 선로로 옮겨 눕는다
        /// </summary>
        public void SetVote(string playerId, string choiceId, bool auto = false) => _cast.SetVote(playerId, choiceId, auto);

        void SetHostReady(bool ready, bool silent)
        {
            _hostReady = ready;
            _hud.HostText.text = ready ? "호스트 선택 완료" : "호스트 선택 대기 중...";
            _hud.HostText.color = ready ? new Color(0.5f, 1f, 0.55f) : new Color(0.8f, 0.8f, 0.85f);
            if (ready && !silent)
            {
                _fx.HostReady();
                _audio.Play(Sfx.HostReady);
            }
        }

        public void PlayReveal(TrolleyGameResult result, float duration)
        {
            _approaching = false;
            _drumroll = false;
            _revealing = true;
            StartCoroutine(RevealRoutine(result, duration > 0f ? duration : 7f));
        }

        #endregion

        #region Update

        void Update()
        {
            if (_hud == null || _state == null)
            {
                return;
            }

            UpdateHeader();

            if (_approaching)
            {
                // 처음엔 빠르게, 도착할수록 느려진다
                float p = Mathf.Clamp01((Time.time - _approachStart) / _approachDuration);
                float eased = 1f - Mathf.Pow(1f - p, 2.2f);
                var from = TrolleyWorld.FarStartPoint;
                _world.Trolley.position = new Vector3(from.x, from.y, Mathf.Lerp(from.z, StopZ, eased));
                _audio.SetEngine(true, Mathf.Clamp(0.45f + _trolleySpeed * 0.09f, 0.45f, 1.6f));
                if (p >= 1f) _approaching = false;
            }
        }

        void LateUpdate()
        {
            if (_world == null) return;

            // 실제 이동 속도로 바퀴를 굴리고 연기 양을 바꾼다
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            var pos = _world.Trolley.position;
            float moved = Vector3.Distance(pos, _lastTrolleyPos);
            _lastTrolleyPos = pos;
            _trolleySpeed = Mathf.Lerp(_trolleySpeed, moved / dt, 0.3f);
            float degrees = moved / WheelRadius * Mathf.Rad2Deg;
            foreach (var wheel in _world.Wheels)
            {
                wheel.Rotate(Vector3.up, degrees, Space.Self);
            }
            var emission = _smoke.emission;
            emission.rateOverTime = 5f + _trolleySpeed * 3f;

            _camera.Tick(Time.deltaTime);
        }

        void UpdateHeader()
        {
            var phase = _state.CurrentPhase;
            _hud.PhaseText.text = _practice ? $"[연습] {PhaseLabel(phase)}" : PhaseLabel(phase);

            bool timed = (phase == GamePhase.PRESENT || phase == GamePhase.INPUT || phase == GamePhase.WAIT) && !_state.IsInfinitePhase;
            float remaining = Mathf.Max(0f, _state.PhaseDuration - (Time.time - _state.PhaseStartTime));
            int seconds = Mathf.CeilToInt(remaining);
            _hud.TimerText.text = timed && !_revealing ? seconds.ToString() : "";

            if (phase != GamePhase.INPUT)
            {
                return;
            }

            bool urgent = seconds <= _profile.urgentSeconds;
            _hud.TimerText.color = urgent ? new Color(1f, 0.3f, 0.25f) : Color.white;

            // 마감이 다가올수록 하늘·햇빛이 붉어진다
            float tension = Mathf.Clamp01(1f - remaining / Mathf.Max(1f, _profile.urgentSeconds + 3f));
            _world.Camera.backgroundColor = Color.Lerp(SkyCalm, SkyTense, tension);
            _world.Sun.color = Color.Lerp(SunCalm, SunTense, tension);

            if (seconds != _lastSecond && seconds > 0)
            {
                _lastSecond = seconds;
                if (urgent)
                {
                    _fx.Urgent();
                    _audio.Play(Sfx.Heartbeat, 0.9f);
                    _audio.Play(Sfx.TickUrgent, 0.7f);
                    if (seconds <= 3)
                    {
                        ShowCenter(seconds.ToString(), new Color(1f, 0.3f, 0.25f));
                        _fx.Slam();
                        _audio.Play(Sfx.Slam, 0.8f, 1.1f);
                    }
                }
                else
                {
                    _fx.Tick();
                    _audio.Play(Sfx.Tick, 0.5f);
                }
            }
        }

        static string PhaseLabel(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.READY:
                case GamePhase.SETUP: return "준비";
                case GamePhase.PRESENT: return "문제 공개";
                case GamePhase.INPUT: return "호스트가 지킬 쪽에 누워라!";
                case GamePhase.WAIT: return "입력 마감";
                case GamePhase.EXECUTE: return "판정 중";
                case GamePhase.REVEAL: return "결과 발표";
                default: return "";
            }
        }

        #endregion

        #region Present · Input · Wait

        IEnumerator IntroRoutine()
        {
            // 항공샷에서 멀리서 달려오는 트롤리로 날아간다
            _camera.BlendTo(FarChaseShot, 2.8f);
            _audio.Play(Sfx.Whoosh);
            _audio.Play(Sfx.Thunder, 0.5f);
            _audio.Play(Sfx.Horn, 0.9f);

            ShowCenter(_practice ? "연습 라운드" : "트롤리 딜레마", Color.white);
            if (_practice)
            {
                _hud.SubCenterText.gameObject.SetActive(true);
                _hud.SubCenterText.maxVisibleCharacters = 99999;
                _hud.SubCenterText.color = new Color(1f, 0.84f, 0.25f);
                _hud.SubCenterText.text = "탈락하지 않아요. 편하게 해 보세요!";
            }
            _fx.Slam();
            _audio.Play(Sfx.Slam);
            yield return new WaitForSeconds(_practice ? 1.6f : 1.1f);
            HideCenter();
            _hud.SubCenterText.gameObject.SetActive(false);
            _hud.SubCenterText.text = "";

            // 시청자가 고를 수 있게 딜레마와 선택지 카드를 보여 준다 (호스트 선택 팝업은 입력 마감 뒤 WAIT에 뜬다)
            _hud.QuestionPanel.SetActive(true);
            SetCardsVisible(true);
            _hud.StatusGroup.SetActive(true);
            _audio.Play(Sfx.Whoosh, 0.8f, 1.2f);
            yield return new WaitForSeconds(0.4f);
            _audio.Play(Sfx.CountPop, 0.8f);
            yield return new WaitForSeconds(0.1f);
            _audio.Play(Sfx.CountPop, 0.8f, 1.2f);
        }

        IEnumerator InputClosedRoutine()
        {
            _world.Camera.backgroundColor = SkyTense;
            ShowCenter("입력 마감!", new Color(1f, 0.3f, 0.25f));
            _fx.Slam();
            _audio.Play(Sfx.InputClosed);
            _audio.Play(Sfx.Slam);
            _audio.Play(Sfx.Horn, 0.8f, 0.9f);
            _hud.StatusGroup.SetActive(false);
            _hud.QuestionPanel.SetActive(false);

            yield return new WaitForSeconds(0.8f);
            HideCenter();

            // 레버 클로즈업. 무한 대기면 호스트가 고를 때까지 조용히 기다린 뒤 드럼롤
            _camera.BlendTo(LeverOrbitShot, 1.1f);
            if (_state.IsInfinitePhase)
            {
                yield return new WaitUntil(() => _hostReady || _revealing);
                if (_revealing) yield break;
            }
            _hud.SubCenterText.gameObject.SetActive(true);
            _hud.SubCenterText.color = Color.white;
            _fx.Typewrite("호스트의 선택은...?");
            _drumroll = true;
            StartCoroutine(DrumrollRoutine());
        }

        IEnumerator DrumrollRoutine()
        {
            float start = Time.time;
            float length = _state.IsInfinitePhase ? 3f : Mathf.Max(1f, _state.PhaseDuration);
            while (_drumroll)
            {
                float t = Mathf.Clamp01((Time.time - start) / length);
                _audio.Play(Sfx.Snare, Mathf.Lerp(0.35f, 1f, t), UnityEngine.Random.Range(0.95f, 1.05f));
                _fx.Rumble(Mathf.Lerp(0.3f, 1.6f, t));
                yield return new WaitForSeconds(Mathf.Lerp(0.16f, 0.045f, t));
            }
        }

        #endregion

        #region Reveal

        IEnumerator RevealRoutine(TrolleyGameResult result, float duration)
        {
            float s = Mathf.Clamp(duration / 7f, 0.6f, 1.6f);
            int saved = Mathf.Max(0, _data.Choices.FindIndex(c => c.Id == result.HostChoice));
            int lane = 1 - saved;   // 트롤리는 호스트가 지킨 쪽의 반대 선로로 간다
            var savedChoice = _data.Choices[saved];
            var laneChoice = _data.Choices[lane];
            var savedColor = saved == 0 ? TrolleyWorld.ColorA : TrolleyWorld.ColorB;
            var laneColor = lane == 0 ? TrolleyWorld.ColorA : TrolleyWorld.ColorB;

            _hud.QuestionPanel.SetActive(false);
            _hud.StatusGroup.SetActive(false);
            SetCardsVisible(false);
            StartCoroutine(Letterbox(110f, 0.35f));
            if (string.IsNullOrEmpty(_hud.SubCenterText.text))
            {
                _fx.Typewrite("호스트의 선택은...?");
            }
            _hud.SubCenterText.gameObject.SetActive(true);

            yield return new WaitForSeconds(0.5f * s);

            // 레버를 트롤리 쪽으로 당기며 호스트가 지킨 선택 공개
            StartCoroutine(PullLever(lane, 0.22f));
            _fx.Lever();
            _audio.Play(Sfx.Lever);
            _audio.Play(Sfx.Cymbal);
            ShowCenter((saved + 1).ToString(), savedColor);
            _fx.Slam();
            _audio.Play(Sfx.Slam);
            _hud.SubCenterText.text = result.HostChoiceSource == TrolleyRules.RandomSource
                ? $"{savedChoice.Label} (무작위)\n<size=80%>트롤리는 {lane + 1}번 선로로!</size>"
                : $"{savedChoice.Label}\n<size=80%>트롤리는 {lane + 1}번 선로로!</size>";
            _hud.SubCenterText.maxVisibleCharacters = 99999;
            _hud.SubCenterText.color = Color.Lerp(savedColor, Color.white, 0.35f);

            yield return new WaitForSeconds(1.1f * s);
            HideCenter();
            _hud.SubCenterText.gameObject.SetActive(false);

            // 질주: 반대 선로에 누운 캐릭터를 차례로 친다 (전원 구제면 첫 캐릭터 앞에서 급정거)
            yield return RunTrolley(lane, result, laneColor, 2.4f * s);

            // 전체 구도로 물러나 결과 확인
            _audio.SetEngine(false);
            SetSparks(false);
            _camera.BlendTo(CraneShot, 1.4f);
            _cast.ShowOutcome(result, _practice);
            yield return new WaitForSeconds(0.8f * s);

            StartCoroutine(Letterbox(0f, 0.35f));
            StartCoroutine(RestoreSky(1.2f));
            _hud.RevealPanel.SetActive(true);
            _hud.RevealTitle.text = $"호스트가 지킨 쪽: {saved + 1}번";
            _hud.RevealTitle.color = Color.Lerp(savedColor, Color.white, 0.25f);
            _hud.RevealResult.text = savedChoice.Label;
            _hud.RevealNote.text = result.HostChoiceSource == TrolleyRules.RandomSource ? "호스트가 고르지 않아 무작위로 정했습니다" : "";
            SetCardsVisible(true);
            HighlightCard(saved);
            _fx.CardWin(saved);

            float countTime = 1.2f;
            _fx.CountResult(result.Survivors.Count, result.Eliminated.Count, countTime);
            StartCoroutine(FillBars(result, countTime));
            StartCoroutine(CountTicks(countTime));
            yield return new WaitForSecondsRealtime(countTime);

            if (_practice)
            {
                _hud.RevealNote.text = $"연습이라 아무도 탈락하지 않습니다 · 실전이었다면 {result.Eliminated.Count}명 탈락";
            }

            if (result.Survivors.Count == 0)
            {
                _fx.GameOver();
                _audio.Play(Sfx.AllEliminated);
                _audio.Play(Sfx.Thunder);
                if (!_practice) _hud.RevealNote.text = "전원 탈락!";
            }
            else
            {
                _audio.Play(Sfx.Award);
                _audio.Play(Sfx.Cymbal, 0.6f);
                var savedPath = _world.Branch(saved);
                TrolleyWorld.Sample(savedPath, TrolleyWorld.Length(savedPath) * 0.45f, out var confettiAt, out _);
                TrolleyParticles.Confetti(_world.Root, confettiAt + Vector3.up * 0.3f, savedColor, Color.white).Play();
                if (result.RescuedAllEliminated && !_practice)
                {
                    _hud.RevealNote.text = "전원 탈락 위기! 트롤리가 멈춰 모두 살아남습니다";
                }
            }
        }

        IEnumerator RunTrolley(int lane, TrolleyGameResult result, Color laneColor, float time)
        {
            var path = _world.RunPath(lane);
            path[0] = _world.Trolley.position;
            float total = TrolleyWorld.Length(path);
            float junctionDistance = Vector3.Distance(path[0], path[1]);
            float startOffset = Vector3.Distance(TrolleyWorld.StartPoint, path[0]);

            var victims = _cast.Victims(lane);
            bool rescued = result.RescuedAllEliminated && victims.Count > 0;
            float stopAt = rescued ? Mathf.Max(junctionDistance, victims[0].distance - startOffset - 2f) : total;
            int nextVictim = 0;
            bool firstHit = true;

            _audio.SetEngine(true, 1f);
            _audio.Play(Sfx.Whoosh);
            _audio.Play(Sfx.Horn);
            SetSparks(true);
            _camera.Play(FollowShot, 6f);
            bool screeched = false;

            for (float t = 0f; t < time; t += Time.deltaTime)
            {
                float k = t / time;
                // 가속 후 (구제 시) 급정거
                float distance = rescued ? stopAt * (1f - (1f - k) * (1f - k)) : total * k * k;
                PlaceOnPath(path, distance);
                _audio.SetEngine(true, Mathf.Lerp(0.9f, 1.8f, rescued ? 1f - k : k));

                if (!screeched && (distance >= junctionDistance - 0.5f || (rescued && k > 0.6f)))
                {
                    screeched = true;
                    _audio.Play(Sfx.Screech);
                    _fx.Rumble(1.5f);
                }

                while (!rescued && nextVictim < victims.Count && distance + startOffset >= victims[nextVictim].distance - 0.9f)
                {
                    var id = victims[nextVictim++].playerId;
                    var at = _cast.PositionOf(id);
                    try
                    {
                        _cast.Hit(id, _world.Trolley.forward);
                    }
                    catch (Exception ex)
                    {
                        // 캐릭터 하나의 오류로 트롤리가 멈추지 않게 한다
                        Galashow.Core.GLog.Error($"[Trolley✗] Hit failed for {id}: {ex.Message}");
                    }
                    TrolleyParticles.HitBurst(_world.Root, at + Vector3.up * 0.6f).Play();
                    TrolleyParticles.Dust(_world.Root, at).Play();
                    if (firstHit)
                    {
                        firstHit = false;
                        TrolleyParticles.Debris(_world.Root, at + Vector3.up * 0.5f, laneColor).Play();
                        _fx.Impact();
                        _camera.Kick(14f, 0.8f);
                        _audio.Play(Sfx.Impact);
                        _audio.Play(Sfx.Boom);
                    }
                    else
                    {
                        _fx.Rumble(1.2f);
                        _audio.Play(Sfx.Impact, 0.45f, UnityEngine.Random.Range(1.1f, 1.4f));
                    }
                }

                yield return null;
            }
            PlaceOnPath(path, rescued ? stopAt : total);

            if (rescued)
            {
                _fx.Slam();
                ShowCenter("끼이익!", Color.white);
                yield return new WaitForSeconds(0.6f);
                HideCenter();
            }
        }

        void PlaceOnPath(Vector3[] path, float distance)
        {
            for (int i = 1; i < path.Length; i++)
            {
                float segment = Vector3.Distance(path[i - 1], path[i]);
                if (distance <= segment || i == path.Length - 1)
                {
                    var dir = (path[i] - path[i - 1]).normalized;
                    _world.Trolley.position = path[i - 1] + dir * Mathf.Min(distance, segment);
                    _world.Trolley.rotation = Quaternion.Slerp(_world.Trolley.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);
                    return;
                }
                distance -= segment;
            }
        }

        IEnumerator PullLever(int branch, float time)
        {
            float target = branch == 0 ? 40f : -40f;
            for (float t = 0f; t < time; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / time);
                _world.LeverPivot.localRotation = Quaternion.Euler(0f, 0f, target * k);
                yield return null;
            }
            _world.LeverPivot.localRotation = Quaternion.Euler(0f, 0f, target);
        }

        IEnumerator FillBars(TrolleyGameResult result, float time)
        {
            int total = Mathf.Max(1, result.Distribution.Values.Sum());
            foreach (var card in _hud.Cards) card.Bar.SetActive(true);
            for (float t = 0f; t <= time; t += Time.unscaledDeltaTime)
            {
                SetBars(result, total, Mathf.Clamp01(t / time));
                yield return null;
            }
            SetBars(result, total, 1f);
        }

        void SetBars(TrolleyGameResult result, int total, float k)
        {
            float eased = 1f - (1f - k) * (1f - k);
            for (int i = 0; i < _hud.Cards.Count; i++)
            {
                result.Distribution.TryGetValue(_data.Choices[i].Id, out var count);
                float ratio = (float)count / total;
                var card = _hud.Cards[i];
                card.BarFill.anchorMax = new Vector2(ratio * eased, 1f);
                card.BarFill.sizeDelta = Vector2.zero;
                card.BarText.text = $"{Mathf.RoundToInt(count * eased)}명 · {Mathf.RoundToInt(ratio * 100f * eased)}%";
            }
        }

        IEnumerator CountTicks(float time)
        {
            int ticks = 10;
            for (int i = 0; i < ticks; i++)
            {
                _audio.Play(Sfx.CountPop, 0.5f, 0.9f + i * 0.06f);
                yield return new WaitForSecondsRealtime(time / ticks);
            }
        }

        void HighlightCard(int branch)
        {
            for (int i = 0; i < _hud.Cards.Count; i++)
            {
                var card = _hud.Cards[i];
                var c = card.Background.color;
                card.Background.color = new Color(c.r, c.g, c.b, i == branch ? 1f : 0.4f);
            }
        }

        #endregion

        #region Camera Shots

        Vector3 T => _world.Trolley.position;

        /// <summary>
        /// 입력 동안 컷을 바꿔 가며 달려오는 트롤리와 선로 위 캐릭터를 번갈아 보여 준다.
        /// 마감 N초 전부터는 트롤리 정면으로 밀고 들어가는 긴장 샷으로 고정한다.
        /// </summary>
        IEnumerator InputShots()
        {
            var shots = new (Func<float, TrolleyCameraDirector.Pose> shot, bool cut)[]
            {
                (OverviewShot, false),
                (SideTrackingShot, true),
                (JunctionFrontShot, true),
                (OverviewShot, false),
                (ChaseCloseShot, false),
                (VictimPovShot, true),
            };

            int index = 0;
            while (true)
            {
                float remaining = _state.PhaseDuration - (Time.time - _state.PhaseStartTime);
                if (remaining <= _profile.urgentSeconds + 0.3f)
                {
                    float length = Mathf.Max(1f, remaining);
                    _camera.Play(t => TensionPushShot(t, length), 5f, cut: true);
                    _audio.Play(Sfx.Whoosh, 0.9f, 0.8f);
                    yield break;
                }

                var (shot, cut) = shots[index++ % shots.Length];
                if (cut)
                {
                    _camera.Play(shot, 5f, cut: true);
                    _audio.Play(Sfx.Whoosh, 0.5f, UnityEngine.Random.Range(1f, 1.3f));
                }
                else
                {
                    _camera.BlendTo(shot, 1.1f);
                }

                float hold = Mathf.Min(ShotLength, remaining - _profile.urgentSeconds - 0.3f);
                yield return new WaitForSeconds(Mathf.Max(0.5f, hold));
            }
        }

        TrolleyCameraDirector.Pose AerialShot(float t)
        {
            float a = t * 0.08f;
            return new TrolleyCameraDirector.Pose(new Vector3(Mathf.Sin(a) * 6f, 26f, -4f - Mathf.Cos(a) * 2f), new Vector3(0f, 0f, 7f), 50f);
        }

        TrolleyCameraDirector.Pose FarChaseShot(float t)
        {
            // 멀리서 달려오는 트롤리 뒤를 흔들리며 쫓는다
            var sway = new Vector3(2.8f + Mathf.Sin(t * 0.7f) * 0.8f, 2.4f + Mathf.Sin(t * 1.3f) * 0.25f, -8f);
            return new TrolleyCameraDirector.Pose(T + sway, T + new Vector3(0f, 1.2f, 12f), 52f, Mathf.Sin(t * 0.9f) * 2f);
        }

        TrolleyCameraDirector.Pose OverviewShot(float t)
        {
            // 분기점과 두 선로(누운 캐릭터)를 크게 돌며 보여 준다
            var center = new Vector3(0f, 0f, 5f);
            float a = (-30f + t * 9f) * Mathf.Deg2Rad;
            var pos = center + new Vector3(Mathf.Sin(a) * 20f, 12.5f, -Mathf.Cos(a) * 20f);
            return new TrolleyCameraDirector.Pose(pos, center + Vector3.forward * 1.5f, 46f);
        }

        TrolleyCameraDirector.Pose SideTrackingShot(float t)
        {
            // 레일 높이에서 옆을 나란히 달리다가 트롤리가 앞질러 간다
            var pos = T + new Vector3(4.2f, 0.9f, 2.5f - t * 1.4f);
            return new TrolleyCameraDirector.Pose(pos, T + new Vector3(0f, 1.1f, 0.5f), 40f, -4f);
        }

        TrolleyCameraDirector.Pose JunctionFrontShot(float t)
        {
            // 두 선로 사이에서 다가오는 트롤리를 정면으로 (양옆에 누운 캐릭터)
            var pos = new Vector3(0.6f, 1.7f + t * 0.05f, 9.5f - t * 0.35f);
            return new TrolleyCameraDirector.Pose(pos, T + new Vector3(0f, 1.2f, 0f), 40f - t * 1.6f);
        }

        TrolleyCameraDirector.Pose ChaseCloseShot(float t)
        {
            var pos = T + new Vector3(Mathf.Sin(t * 0.9f) * 1.3f, 3f, -6.5f);
            return new TrolleyCameraDirector.Pose(pos, T + new Vector3(0f, 1f, 9f), 56f, Mathf.Sin(t * 1.1f) * 3f);
        }

        TrolleyCameraDirector.Pose VictimPovShot(float t)
        {
            // 선로에 누운 캐릭터 눈높이에서 트롤리를 올려다본다 (손떨림)
            var lane = _world.Branch(_cast.LaneCount(0) >= _cast.LaneCount(1) ? 0 : 1);
            TrolleyWorld.Sample(lane, 2.6f, out var at, out var dir);
            var right = Vector3.Cross(Vector3.up, dir);
            var shake = new Vector3(Mathf.Sin(t * 13f), Mathf.Sin(t * 17f), 0f) * 0.03f;
            var pos = at + Vector3.up * 0.45f - right * 0.8f + shake;
            return new TrolleyCameraDirector.Pose(pos, T + new Vector3(0f, 1.4f, 0f), 36f, 8f);
        }

        TrolleyCameraDirector.Pose TensionPushShot(float t, float length)
        {
            // 트롤리 정면으로 밀고 들어가며 화각을 좁힌다
            float k = Mathf.Clamp01(t / length);
            var pos = T + new Vector3(0.5f, 1.3f, Mathf.Lerp(11f, 4.5f, k * k));
            return new TrolleyCameraDirector.Pose(pos, T + new Vector3(0f, 1.2f, 0f), Mathf.Lerp(42f, 28f, k), Mathf.Sin(t * 2.2f) * 5f * k);
        }

        TrolleyCameraDirector.Pose LeverOrbitShot(float t)
        {
            var center = TrolleyWorld.LeverPosition + Vector3.up * 1f;
            float a = (35f + t * 7f) * Mathf.Deg2Rad;
            var pos = center + new Vector3(Mathf.Sin(a) * 4.4f, 1.2f, -Mathf.Cos(a) * 4.4f);
            return new TrolleyCameraDirector.Pose(pos, center + new Vector3(-1f, 0f, 0.8f), 40f);
        }

        TrolleyCameraDirector.Pose FollowShot(float t)
        {
            var trolley = _world.Trolley;
            var pos = trolley.position + trolley.rotation * new Vector3(0f, 3.2f, -7.5f);
            return new TrolleyCameraDirector.Pose(pos, trolley.position + trolley.forward * 6f + Vector3.up, 55f);
        }

        TrolleyCameraDirector.Pose CraneShot(float t)
        {
            var pos = new Vector3(Mathf.Sin(t * 0.15f) * 3f, 15f + t * 0.25f, -9f);
            return new TrolleyCameraDirector.Pose(pos, new Vector3(0f, 0f, 8f), 46f);
        }

        #endregion

        #region Helpers

        IEnumerator Callout(string title, string subtitle, Color color, float hold, Sfx sfx)
        {
            ShowCenter(title, color);
            _hud.SubCenterText.gameObject.SetActive(true);
            _hud.SubCenterText.text = subtitle;
            _hud.SubCenterText.maxVisibleCharacters = 99999;
            _hud.SubCenterText.color = Color.white;
            _fx.Slam();
            _audio.Play(sfx);
            yield return new WaitForSeconds(hold);
            HideCenter();
            _hud.SubCenterText.gameObject.SetActive(false);
        }

        void ShowCenter(string text, Color color)
        {
            _hud.CenterText.gameObject.SetActive(true);
            _hud.CenterText.text = text;
            _hud.CenterText.color = color;
        }

        void HideCenter() => _hud.CenterText.gameObject.SetActive(false);

        IEnumerator Letterbox(float height, float time)
        {
            float from = _hud.LetterboxTop.sizeDelta.y;
            for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
            {
                float h = Mathf.Lerp(from, height, Mathf.SmoothStep(0f, 1f, t / time));
                _hud.LetterboxTop.sizeDelta = new Vector2(0f, h);
                _hud.LetterboxBottom.sizeDelta = new Vector2(0f, h);
                yield return null;
            }
            _hud.LetterboxTop.sizeDelta = new Vector2(0f, height);
            _hud.LetterboxBottom.sizeDelta = new Vector2(0f, height);
        }

        IEnumerator RestoreSky(float time)
        {
            var sky = _world.Camera.backgroundColor;
            var sun = _world.Sun.color;
            for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
            {
                float k = t / time;
                _world.Camera.backgroundColor = Color.Lerp(sky, SkyCalm, k);
                _world.Sun.color = Color.Lerp(sun, SunCalm, k);
                yield return null;
            }
        }

        void SetSparks(bool on)
        {
            var emission = _sparks.emission;
            emission.rateOverTime = on ? 90f : 0f;
            if (on && !_sparks.isPlaying) _sparks.Play();
        }

        void SetCardsVisible(bool visible)
        {
            foreach (var card in _hud.Cards)
            {
                card.Root.gameObject.SetActive(visible);
            }
        }

        #endregion
    }
}
