using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Galashow.Trolley
{
    /// <summary>
    /// Feel(MMF_Player) 연출 묶음. 무대가 만들어질 때 코드로 조립한다.
    /// UI·카메라 피드백은 슬로모션 중에도 움직이도록 Unscaled 시간으로 돈다.
    /// </summary>
    public class TrolleyFx : MonoBehaviour
    {
        static readonly AnimationCurve PunchCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0f));
        static readonly AnimationCurve SlamCurve = new AnimationCurve(
            new Keyframe(0f, 2.8f), new Keyframe(0.5f, 0.9f), new Keyframe(0.72f, 1.07f), new Keyframe(1f, 1f));
        static readonly AnimationCurve PopInCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.6f, 1.1f), new Keyframe(1f, 1f));
        static readonly AnimationCurve EaseOutBack = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 3.5f), new Keyframe(0.7f, 1.06f), new Keyframe(1f, 1f));
        static readonly AnimationCurve Pulse = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.12f, 1f), new Keyframe(1f, 0f));

        TrolleyFxProfile _profile;
        MMF_Player _tick, _urgent, _vote, _host, _slam, _intro, _rumble, _lever, _impact, _count, _gameOver;
        readonly MMF_Player[] _cardWin = new MMF_Player[2];
        MMF_TMPCountTo _countAlive, _countOut;
        MMF_TMPTextReveal _typewriter;
        MMF_Player _type;

        public void Build(TrolleyWorld world, TrolleyHud hud, TrolleyFxProfile profile)
        {
            _profile = profile;
            float k = profile.shakeIntensity;

            // 리스너: 카메라 흔들림, 화면 플래시, 시간 관리자 (화각은 카메라 디렉터가 담당)
            var posShaker = world.CameraShake.gameObject.AddComponent<MMPositionShaker>();
            var rotShaker = world.CameraShake.gameObject.AddComponent<MMRotationShaker>();
            if (FindAnyObjectByType<MMTimeManager>() == null)
            {
                gameObject.AddComponent<MMTimeManager>();
            }

            hud.Flash.color = Color.white;
            var flashGroup = hud.Flash.gameObject.AddComponent<CanvasGroup>();
            flashGroup.alpha = 0f;
            flashGroup.blocksRaycasts = false;
            var flash = hud.Flash.gameObject.AddComponent<MMFlash>();

            // 초 단위 틱: 타이머 펀치 + 경고등
            _tick = Player("Tick");
            Punch(_tick, hud.TimerText.transform, 0.25f, 0.2f);
            foreach (var lamp in world.WarningLights) LightPulse(_tick, lamp, 3f, 0.35f);
            Done(_tick);

            // 마감 임박: 큰 펀치 + 붉은 비네트 + 미세 흔들림 + 경고등
            _urgent = Player("Urgent");
            Punch(_urgent, hud.TimerText.transform, 0.55f, 0.3f);
            ImagePulse(_urgent, hud.Vignette, 0.65f, 0.9f);
            Shake(_urgent, posShaker, 0.07f * k, 30f, 0.3f);
            foreach (var lamp in world.WarningLights) LightPulse(_urgent, lamp, 7f, 0.5f);
            Done(_urgent);

            _vote = Player("Vote");
            Punch(_vote, hud.StatusText.transform, 0.12f, 0.12f);
            Done(_vote);

            _host = Player("HostReady");
            Punch(_host, hud.HostText.transform, 0.35f, 0.35f);
            Done(_host);

            // 중앙 문구 쾅
            _slam = Player("Slam");
            ScaleCurve(_slam, hud.CenterText.transform, SlamCurve, 0.45f);
            Shake(_slam, posShaker, 0.2f * k, 35f, 0.35f);
            Flash(_slam, flash, Color.white, 0.25f, 0.2f);
            Done(_slam);

            // 문제 공개: 딜레마 패널 팝인, 카드 좌우에서 진입
            _intro = Player("Intro");
            ScaleCurve(_intro, hud.Question, PopInCurve, 0.5f, 0.1f);
            SlideIn(_intro, hud.Cards[0].Root, new Vector3(-900f, 0f, 0f), 0.6f, 0.35f);
            SlideIn(_intro, hud.Cards[1].Root, new Vector3(900f, 0f, 0f), 0.6f, 0.45f);
            Done(_intro);

            // 드럼롤 진동 (세기는 재생 시 intensity로)
            _rumble = Player("Rumble");
            Shake(_rumble, posShaker, 0.08f * k, 40f, 0.2f);
            Done(_rumble);

            _lever = Player("Lever");
            Shake(_lever, posShaker, 0.3f * k, 30f, 0.45f);
            RotShake(_lever, rotShaker, 3f * k, 25f, 0.45f);
            foreach (var lamp in world.WarningLights) LightPulse(_lever, lamp, 9f, 0.6f);
            Flash(_lever, flash, new Color(1f, 0.95f, 0.8f), 0.35f, 0.25f);
            Done(_lever);

            // 충돌: 정지 프레임 → 슬로모션, 강한 흔들림, 섬광, 태양 빛 번쩍 (화각 킥은 카메라 디렉터)
            _impact = Player("Impact");
            var freeze = (MMF_FreezeFrame)_impact.AddFeedback(typeof(MMF_FreezeFrame));
            freeze.FreezeFrameDuration = profile.freezeFrameDuration;
            var slowmo = (MMF_TimescaleModifier)_impact.AddFeedback(typeof(MMF_TimescaleModifier));
            slowmo.Mode = MMF_TimescaleModifier.Modes.Shake;
            slowmo.TimeScale = profile.slowMotionScale;
            slowmo.TimeScaleDuration = profile.slowMotionDuration;
            slowmo.Timing.InitialDelay = profile.freezeFrameDuration;
            Unscaled(slowmo);
            slowmo.Active = profile.slowMotionDuration > 0f;
            Shake(_impact, posShaker, 1.1f * k, 38f, 0.9f);
            RotShake(_impact, rotShaker, 7f * k, 30f, 0.8f);
            Flash(_impact, flash, Color.white, 0.95f, 0.4f);
            LightPulse(_impact, world.Sun, 2.5f, 0.6f);
            Done(_impact);

            // 전원 탈락: 붉은 섬광 + 비네트
            _gameOver = Player("GameOver");
            Flash(_gameOver, flash, new Color(1f, 0.1f, 0.1f), 0.6f, 0.6f);
            ImagePulse(_gameOver, hud.Vignette, 0.9f, 2f);
            Shake(_gameOver, posShaker, 0.4f * k, 20f, 0.8f);
            Done(_gameOver);

            // 결과 숫자 카운트업
            _count = Player("Count");
            _countAlive = CountTo(_count, hud.SurvivorNumber);
            _countOut = CountTo(_count, hud.EliminatedNumber);
            Punch(_count, hud.SurvivorNumber.transform, 0.3f, 0.35f, 1.2f);
            Punch(_count, hud.EliminatedNumber.transform, 0.3f, 0.35f, 1.2f);
            Done(_count);

            for (int i = 0; i < 2; i++)
            {
                _cardWin[i] = Player($"CardWin{i}");
                Punch(_cardWin[i], hud.Cards[i].Root, 0.18f, 0.5f);
                var cardShaker = hud.Cards[i].Root.gameObject.AddComponent<MMRotationShaker>();
                cardShaker.Mode = MMRotationShaker.Modes.RectTransform;
                RotShake(_cardWin[i], cardShaker, 6f, 25f, 0.5f);
                Done(_cardWin[i]);
            }

            // 타자기 문구 ("호스트의 선택은...?")
            _type = Player("Typewriter");
            _typewriter = (MMF_TMPTextReveal)_type.AddFeedback(typeof(MMF_TMPTextReveal));
            _typewriter.TargetTMPText = hud.SubCenterText;
            _typewriter.ReplaceText = true;
            _typewriter.DurationMode = MMF_TMPTextReveal.DurationModes.TotalDuration;
            _typewriter.RevealDuration = 0.7f;
            Unscaled(_typewriter);
            Done(_type);
        }

        #region Play

        public void Tick() => _tick.PlayFeedbacks();
        public void Urgent() => _urgent.PlayFeedbacks();
        public void Vote() => _vote.PlayFeedbacks();
        public void HostReady() => _host.PlayFeedbacks();
        public void Slam() => _slam.PlayFeedbacks();
        public void Intro() => _intro.PlayFeedbacks();
        public void Rumble(float intensity) => _rumble.PlayFeedbacks(transform.position, intensity);
        public void Lever() => _lever.PlayFeedbacks();
        public void Impact() => _impact.PlayFeedbacks();
        public void GameOver() => _gameOver.PlayFeedbacks();
        public void CardWin(int index) => _cardWin[index].PlayFeedbacks();

        public void Typewrite(string text)
        {
            _typewriter.NewText = text;
            _type.PlayFeedbacks();
        }

        public void CountResult(int survivors, int eliminated, float duration)
        {
            _countAlive.CountTo = survivors;
            _countOut.CountTo = eliminated;
            _countAlive.Duration = duration;
            _countOut.Duration = duration;
            _count.PlayFeedbacks();
        }

        #endregion

        #region Builders

        MMF_Player Player(string name)
        {
            var go = new GameObject($"FX_{name}");
            go.transform.SetParent(transform, false);
            var player = go.AddComponent<MMF_Player>();
            player.InitializationMode = MMFeedbacks.InitializationModes.Script;
            return player;
        }

        static void Done(MMF_Player player) => player.Initialization();

        static void Unscaled(MMF_Feedback feedback) => feedback.Timing.TimescaleMode = TimescaleModes.Unscaled;

        static void Punch(MMF_Player player, Transform target, float amount, float duration, float delay = 0f)
        {
            var f = (MMF_Scale)player.AddFeedback(typeof(MMF_Scale));
            f.AnimateScaleTarget = target;
            f.Mode = MMF_Scale.Modes.Additive;
            f.AnimateScaleDuration = duration;
            f.RemapCurveZero = 0f;
            f.RemapCurveOne = amount;
            f.UniformScaling = true;
            f.AnimateScaleTweenX = new MMTweenType(PunchCurve);
            f.Timing.InitialDelay = delay;
            Unscaled(f);
        }

        static void ScaleCurve(MMF_Player player, Transform target, AnimationCurve curve, float duration, float delay = 0f)
        {
            var f = (MMF_Scale)player.AddFeedback(typeof(MMF_Scale));
            f.AnimateScaleTarget = target;
            f.Mode = MMF_Scale.Modes.Absolute;
            f.AnimateScaleDuration = duration;
            f.RemapCurveZero = 0f;
            f.RemapCurveOne = 1f;
            f.UniformScaling = true;
            f.AnimateScaleTweenX = new MMTweenType(curve);
            f.Timing.InitialDelay = delay;
            Unscaled(f);
        }

        static void SlideIn(MMF_Player player, RectTransform target, Vector3 from, float duration, float delay)
        {
            var f = (MMF_Position)player.AddFeedback(typeof(MMF_Position));
            f.AnimatePositionTarget = target.gameObject;
            f.Space = MMF_Position.Spaces.RectTransform;
            f.Mode = MMF_Position.Modes.AtoB;
            f.RelativePosition = true;
            f.InitialPosition = from;
            f.DestinationPosition = Vector3.zero;
            f.AnimatePositionDuration = duration;
            f.AnimatePositionTween = new MMTweenType(EaseOutBack);
            f.Timing.InitialDelay = delay;
            Unscaled(f);
        }

        static void Shake(MMF_Player player, MMPositionShaker shaker, float range, float speed, float duration)
        {
            var f = (MMF_PositionShake)player.AddFeedback(typeof(MMF_PositionShake));
            f.TargetShaker = shaker;
            f.ShakeRange = range;
            f.ShakeSpeed = speed;
            f.Duration = duration;
            f.RandomizeDirection = true;
            Unscaled(f);
        }

        static void RotShake(MMF_Player player, MMRotationShaker shaker, float degrees, float speed, float duration)
        {
            var f = (MMF_RotationShake)player.AddFeedback(typeof(MMF_RotationShake));
            f.TargetShaker = shaker;
            f.ShakeRange = degrees;
            f.ShakeSpeed = speed;
            f.Duration = duration;
            f.ShakeMainDirection = Vector3.forward;
            Unscaled(f);
        }

        static void Flash(MMF_Player player, MMFlash target, Color color, float alpha, float duration)
        {
            var f = (MMF_Flash)player.AddFeedback(typeof(MMF_Flash));
            f.TargetFlash = target;
            f.FlashColor = color;
            f.FlashAlpha = alpha;
            f.FlashDuration = duration;
            Unscaled(f);
        }

        static void LightPulse(MMF_Player player, Light light, float intensity, float duration)
        {
            var f = (MMF_Light)player.AddFeedback(typeof(MMF_Light));
            f.BoundLight = light;
            f.Mode = MMF_Light.Modes.OverTime;
            f.Duration = duration;
            f.StartsOff = false;
            f.RelativeValues = true;
            f.ModifyColor = false;
            f.ModifyRange = false;
            f.ModifyShadowStrength = false;
            f.ModifyIntensity = true;
            f.RemapIntensityZero = 0f;
            f.RemapIntensityOne = intensity;
            Unscaled(f);
        }

        static void ImagePulse(MMF_Player player, Image image, float alpha, float duration)
        {
            var f = (MMF_ImageAlpha)player.AddFeedback(typeof(MMF_ImageAlpha));
            f.BoundImage = image;
            f.Mode = MMF_ImageAlpha.Modes.OverTime;
            f.Duration = duration;
            f.Curve = new MMTweenType(Pulse);
            f.CurveRemapZero = 0f;
            f.CurveRemapOne = alpha;
            Unscaled(f);
        }

        static MMF_TMPCountTo CountTo(MMF_Player player, TMP_Text text)
        {
            var f = (MMF_TMPCountTo)player.AddFeedback(typeof(MMF_TMPCountTo));
            f.TargetTMPText = text;
            f.CountFrom = 0f;
            f.CountTo = 0f;
            f.Duration = 1.2f;
            f.Format = "0";
            f.CountingCurve = new MMTweenType(new AnimationCurve(new Keyframe(0f, 0f, 0f, 2.5f), new Keyframe(1f, 1f, 0f, 0f)));
            Unscaled(f);
            return f;
        }

        #endregion

        void OnDestroy()
        {
            // 중단·정리 중 슬로모션이 남지 않게 한다
            Time.timeScale = 1f;
        }
    }
}
