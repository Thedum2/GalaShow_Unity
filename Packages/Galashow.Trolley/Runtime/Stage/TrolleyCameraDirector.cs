using System;
using UnityEngine;

namespace Galashow.Trolley
{
    /// <summary>
    /// 샷 단위 카메라 연출. 샷은 경과 시간 → 카메라 자세 함수이며, 리그(이동)만 움직인다.
    /// 흔들림은 하위 CameraShake(Feel 셰이커)가 따로 더한다. 화각도 여기서만 바꾼다(킥 포함).
    /// </summary>
    public class TrolleyCameraDirector
    {
        public struct Pose
        {
            public Vector3 Position;
            public Vector3 LookAt;
            public float Fov;   // 0이면 기본 화각
            public float Roll;  // 도(더치 앵글)

            public Pose(Vector3 position, Vector3 lookAt, float fov = 0f, float roll = 0f)
            {
                Position = position;
                LookAt = lookAt;
                Fov = fov;
                Roll = roll;
            }

            public static Pose Lerp(Pose a, Pose b, float t)
            {
                float fa = a.Fov > 0f ? a.Fov : DefaultFov;
                float fb = b.Fov > 0f ? b.Fov : DefaultFov;
                return new Pose(Vector3.Lerp(a.Position, b.Position, t), Vector3.Lerp(a.LookAt, b.LookAt, t),
                    Mathf.Lerp(fa, fb, t), Mathf.Lerp(a.Roll, b.Roll, t));
            }
        }

        public const float DefaultFov = 45f;

        readonly Transform _rig;
        readonly Camera _camera;
        Func<float, Pose> _shot;
        float _time;
        float _sharpness;
        bool _snap;
        Pose _last;

        float _kickAmount;
        float _kickDuration;
        float _kickTime = -1f;

        public TrolleyCameraDirector(Transform rig, Camera camera)
        {
            _rig = rig;
            _camera = camera;
            _last = new Pose(rig.position, rig.position + rig.forward * 10f, camera.fieldOfView);
        }

        /// <summary>
        /// 현재 카메라 자세 (블렌드 시작점으로 쓴다)
        /// </summary>
        public Pose Current => _last;

        /// <summary>
        /// 샷 시작. cut이면 첫 프레임에 바로 옮기고, 아니면 sharpness로 따라간다(클수록 빠름).
        /// </summary>
        public void Play(Func<float, Pose> shot, float sharpness = 4f, bool cut = false)
        {
            _shot = shot;
            _time = 0f;
            _sharpness = sharpness;
            _snap = cut;
        }

        /// <summary>
        /// 현재 자세에서 target 샷으로 time 동안 부드럽게 넘어간다 (target이 움직여도 따라간다)
        /// </summary>
        public void BlendTo(Func<float, Pose> target, float time, float sharpness = 12f)
        {
            var from = _last;
            Play(t =>
            {
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / time));
                return Pose.Lerp(from, target(t), k);
            }, sharpness);
        }

        /// <summary>
        /// 화각 순간 확대 (충돌 등)
        /// </summary>
        public void Kick(float amount, float duration)
        {
            _kickAmount = amount;
            _kickDuration = duration;
            _kickTime = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (_shot == null)
            {
                return;
            }

            _time += deltaTime;
            var pose = _shot(_time);
            float k = _snap ? 1f : 1f - Mathf.Exp(-_sharpness * deltaTime);
            _snap = false;

            _rig.position = Vector3.Lerp(_rig.position, pose.Position, k);
            var forward = pose.LookAt - pose.Position;
            if (forward.sqrMagnitude > 0.0001f)
            {
                var rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, 0f, pose.Roll);
                _rig.rotation = Quaternion.Slerp(_rig.rotation, rotation, k);
            }

            float fov = pose.Fov > 0f ? pose.Fov : DefaultFov;
            float current = Mathf.Lerp(_camera.fieldOfView - KickValue(), fov, k);
            if (_kickTime >= 0f)
            {
                _kickTime += Time.unscaledDeltaTime;
                if (_kickTime > _kickDuration) _kickTime = -1f;
            }
            _camera.fieldOfView = current + KickValue();

            _last = new Pose(_rig.position, _rig.position + _rig.forward * Vector3.Distance(pose.Position, pose.LookAt), current, pose.Roll);
        }

        float KickValue()
        {
            if (_kickTime < 0f) return 0f;
            float t = Mathf.Clamp01(_kickTime / _kickDuration);
            // 빠르게 벌어졌다가 천천히 돌아온다
            return _kickAmount * (t < 0.15f ? t / 0.15f : 1f - (t - 0.15f) / 0.85f);
        }
    }
}
