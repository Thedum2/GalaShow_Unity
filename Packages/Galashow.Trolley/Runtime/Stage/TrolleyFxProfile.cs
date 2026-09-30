using UnityEngine;

namespace Galashow.Trolley
{
    /// <summary>
    /// 트롤리 연출 설정 (효과음·강도). Resources/Trolley/TrolleyFxProfile 에셋을 불러오고, 없으면 기본값을 쓴다.
    /// 효과음이 비어 있으면 TrolleySynth가 만든 합성음으로 대신한다.
    /// </summary>
    [CreateAssetMenu(menuName = "Galashow/Trolley FX Profile", fileName = "TrolleyFxProfile")]
    public class TrolleyFxProfile : ScriptableObject
    {
        public const string ResourcePath = "Trolley/TrolleyFxProfile";

        [Header("긴장 (입력)")]
        public AudioClip tick;
        public AudioClip tickUrgent;
        public AudioClip heartbeat;
        public AudioClip vote;
        public AudioClip hostReady;
        [Tooltip("기차 경적. 비우면 합성음")]
        public AudioClip horn;

        [Header("마감·드럼롤")]
        public AudioClip inputClosed;
        public AudioClip snare;
        public AudioClip slam;
        public AudioClip whoosh;

        [Header("결과")]
        public AudioClip lever;
        public AudioClip engine;
        public AudioClip screech;
        public AudioClip impact;
        public AudioClip boom;
        public AudioClip cymbal;
        public AudioClip countPop;
        public AudioClip award;
        public AudioClip allEliminated;
        public AudioClip thunder;

        [Header("볼륨")]
        [Range(0f, 1f)] public float masterVolume = 0.8f;
        [Range(0f, 1f)] public float voteVolume = 0.25f;
        [Range(0f, 1f)] public float engineVolume = 0.5f;

        [Header("강도")]
        [Range(0f, 3f)] public float shakeIntensity = 1f;
        [Range(0.05f, 1f)] public float slowMotionScale = 0.3f;
        [Range(0f, 2f)] public float slowMotionDuration = 0.7f;
        [Range(0f, 0.3f)] public float freezeFrameDuration = 0.09f;
        [Tooltip("입력 마감 전 긴장 연출(심장박동·붉은 비네트)을 시작할 남은 초")]
        public int urgentSeconds = 5;

        public static TrolleyFxProfile Load()
        {
            var profile = Resources.Load<TrolleyFxProfile>(ResourcePath);
            return profile != null ? profile : CreateInstance<TrolleyFxProfile>();
        }
    }
}
