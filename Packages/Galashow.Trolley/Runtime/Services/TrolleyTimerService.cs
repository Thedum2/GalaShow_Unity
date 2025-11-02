using System;
using UnityEngine;
using Galashow.Core;

namespace Galashow.Trolley
{
    /// <summary>
    /// Trolley 게임 타이머 서비스
    /// 입력 시간 제한, 카운트다운 등을 관리
    /// </summary>
    public class TrolleyTimerService
    {
        private float _startTime;
        private float _duration;
        private bool _isRunning;

        /// <summary>
        /// 타이머 시작 이벤트
        /// </summary>
        public event Action<float> OnTimerStarted;

        /// <summary>
        /// 타이머 종료 이벤트
        /// </summary>
        public event Action OnTimerExpired;

        /// <summary>
        /// 타이머 경고 이벤트 (남은 시간이 적을 때)
        /// </summary>
        public event Action<float> OnTimerWarning;

        /// <summary>
        /// 타이머가 실행 중인지 여부
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 경과 시간
        /// </summary>
        public float ElapsedTime => _isRunning ? Time.time - _startTime : 0;

        /// <summary>
        /// 남은 시간
        /// </summary>
        public float RemainingTime => _isRunning ? Mathf.Max(0, _duration - ElapsedTime) : 0;

        /// <summary>
        /// 진행률 (0~1)
        /// </summary>
        public float Progress => _duration > 0 ? Mathf.Clamp01(ElapsedTime / _duration) : 1f;

        /// <summary>
        /// 타이머 시작
        /// </summary>
        public void StartTimer(float duration)
        {
            _startTime = Time.time;
            _duration = duration;
            _isRunning = true;

            OnTimerStarted?.Invoke(duration);
            GLog.Info($"[TrolleyTimerService] 타이머 시작: {duration}초");
        }

        /// <summary>
        /// 타이머 정지
        /// </summary>
        public void StopTimer()
        {
            _isRunning = false;
            GLog.Info($"[TrolleyTimerService] 타이머 정지 (경과: {ElapsedTime:F2}초)");
        }

        /// <summary>
        /// 타이머 체크 (Update에서 호출)
        /// </summary>
        public void Update()
        {
            if (!_isRunning)
            {
                return;
            }

            // 타임아웃 체크
            if (ElapsedTime >= _duration)
            {
                _isRunning = false;
                OnTimerExpired?.Invoke();
                GLog.Warn($"[TrolleyTimerService] 타이머 만료!");
                return;
            }

            // 경고 체크 (남은 시간 5초 이하)
            if (RemainingTime <= 5f && RemainingTime > 4.9f)
            {
                OnTimerWarning?.Invoke(RemainingTime);
            }
        }

        /// <summary>
        /// 시간 연장
        /// </summary>
        public void ExtendTime(float additionalTime)
        {
            _duration += additionalTime;
            GLog.Info($"[TrolleyTimerService] 시간 연장: +{additionalTime}초 (총: {_duration}초)");
        }

        /// <summary>
        /// 타이머 리셋
        /// </summary>
        public void Reset()
        {
            _isRunning = false;
            _startTime = 0;
            _duration = 0;
            GLog.Info("[TrolleyTimerService] 타이머 리셋");
        }

        /// <summary>
        /// 포맷된 시간 문자열 가져오기
        /// </summary>
        public string GetFormattedTime()
        {
            float time = RemainingTime;
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
