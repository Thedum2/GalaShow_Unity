using TMPro;
using UnityEngine;

namespace Galashow.Common
{
    /// <summary>
    /// 참가자 캐릭터 한 명: 카탈로그 프리팹 + 이름표
    /// 애니메이터 상태 이름은 Quirky Series 기준 (Idle_A, Fear, Death, Hit, Jump, Bounce … / 눈: Eyes_Trauma, Eyes_Dead, Eyes_Happy …)
    /// </summary>
    public class CharacterActor : MonoBehaviour
    {
        const int BodyLayer = 0;
        const int FaceLayer = 1;

        /// <summary>
        /// 이름표가 바라볼 카메라 (없으면 Camera.main)
        /// </summary>
        public static Camera NameTagCamera { get; set; }

        public string PlayerId { get; private set; }
        public Transform Model { get; private set; }
        public Animator Animator { get; private set; }
        public TextMeshPro NameTag { get; private set; }

        float _tagHeight = 1.4f;

        /// <summary>
        /// 캐릭터 생성. avatarName이 카탈로그에 없으면 참가자별로 고정된 대체 캐릭터를 쓴다.
        /// </summary>
        public static CharacterActor Spawn(Transform parent, string playerId, string displayName, string avatarName, float scale = 1f)
        {
            var go = new GameObject($"Character_{playerId}");
            go.transform.SetParent(parent, false);
            var actor = go.AddComponent<CharacterActor>();
            actor.PlayerId = playerId;

            var entry = CharacterCatalog.Instance.FindOrFallback(avatarName, playerId?.GetHashCode() ?? 0);
            if (entry != null)
            {
                actor.Model = Instantiate(entry.prefab, go.transform, false).transform;
                actor.Model.localScale *= entry.scale * scale;
                actor.Animator = actor.Model.GetComponentInChildren<Animator>();
                if (actor.Animator != null)
                {
                    actor.Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }
            }
            else
            {
                // 카탈로그가 비어 있으면 캡슐로 대신한다
                var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Destroy(capsule.GetComponent<Collider>());
                capsule.transform.SetParent(go.transform, false);
                capsule.transform.localPosition = Vector3.up * 0.5f * scale;
                capsule.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f) * scale;
                capsule.GetComponent<Renderer>().sharedMaterial = GalashowMaterials.Lit(new Color(0.8f, 0.8f, 0.85f));
                actor.Model = capsule.transform;
            }

            actor._tagHeight = 1.45f * scale;
            actor.CreateNameTag(displayName);
            return actor;
        }

        void CreateNameTag(string text)
        {
            var tagGo = new GameObject("NameTag");
            tagGo.transform.SetParent(transform, false);
            tagGo.transform.localPosition = Vector3.up * _tagHeight;
            NameTag = tagGo.AddComponent<TextMeshPro>();
            NameTag.text = text;
            NameTag.fontSize = 4.5f;
            NameTag.alignment = TextAlignmentOptions.Center;
            NameTag.textWrappingMode = TextWrappingModes.NoWrap;
            NameTag.outlineWidth = 0.25f;
            NameTag.outlineColor = new Color32(0, 0, 0, 230);
            NameTag.rectTransform.sizeDelta = new Vector2(4f, 0.6f);
        }

        /// <summary>
        /// 이름표 높이 기준점 (누웠을 때 등 자세가 바뀌면 조정)
        /// </summary>
        public void SetNameTagOffset(Vector3 worldOffset)
        {
            if (NameTag != null)
            {
                NameTag.transform.position = transform.position + worldOffset;
            }
        }

        public void SetNameTag(string text, Color color)
        {
            if (NameTag == null) return;
            if (text != null) NameTag.text = text;
            NameTag.color = color;
        }

        /// <summary>
        /// 몸 동작 (없는 상태면 무시)
        /// </summary>
        public void PlayBody(string state, float fade = 0.15f) => Play(BodyLayer, state, fade);

        /// <summary>
        /// 눈 표정 (없는 상태면 무시)
        /// </summary>
        public void PlayFace(string state, float fade = 0.1f) => Play(FaceLayer, state, fade);

        void Play(int layer, string state, float fade)
        {
            if (Animator == null || layer >= Animator.layerCount) return;
            int hash = Animator.StringToHash(state);
            if (Animator.HasState(layer, hash))
            {
                Animator.CrossFadeInFixedTime(hash, fade, layer);
            }
        }

        /// <summary>
        /// 치여서 날아간다: 찌그러짐 → Hit 동작·겁먹은 눈 → 회전하며 날아감 → Death·쓰러진 눈
        /// 이름표는 맞은 자리에 남는다.
        /// </summary>
        public void Knockback(Vector3 force, Vector3 torque, float deathDelay = 0.4f)
        {
            PlayBody("Hit", 0.02f);
            PlayFace("Eyes_Trauma", 0.02f);

            // Unity 오브젝트는 ?? 로 null 검사를 하면 안 된다 (에디터의 가짜 null)
            if (!TryGetComponent<Rigidbody>(out var body))
            {
                body = gameObject.AddComponent<Rigidbody>();
            }
            if (!TryGetComponent<Collider>(out _))
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.4f, 0.3f);
                box.size = new Vector3(0.6f, 0.8f, 0.8f);
            }

            body.mass = 0.5f;
            body.linearDamping = 0.15f;
            body.angularDamping = 0.4f;
            body.AddForce(force, ForceMode.Impulse);
            body.AddTorque(torque, ForceMode.Impulse);

            if (NameTag != null)
            {
                NameTag.transform.SetParent(transform.parent, true);
            }

            StartCoroutine(HitReaction(deathDelay));
        }

        System.Collections.IEnumerator HitReaction(float deathDelay)
        {
            if (Model != null)
            {
                // 충격 순간 납작하게 찌그러졌다가 늘어나며 튕긴다
                var baseScale = Model.localScale;
                yield return ScaleTo(Vector3.Scale(baseScale, new Vector3(1.4f, 0.5f, 1.4f)), 0.05f);
                yield return ScaleTo(Vector3.Scale(baseScale, new Vector3(0.8f, 1.35f, 0.8f)), 0.09f);
                yield return ScaleTo(baseScale, 0.14f);
            }

            yield return new WaitForSeconds(Mathf.Max(0f, deathDelay - 0.28f));
            PlayBody("Death", 0.1f);
            PlayFace("Eyes_Dead", 0.1f);
        }

        System.Collections.IEnumerator ScaleTo(Vector3 target, float time)
        {
            var from = Model.localScale;
            for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
            {
                Model.localScale = Vector3.Lerp(from, target, t / time);
                yield return null;
            }
            Model.localScale = target;
        }

        void LateUpdate()
        {
            if (NameTag == null) return;
            var cam = NameTagCamera != null ? NameTagCamera : Camera.main;
            if (cam != null)
            {
                NameTag.transform.rotation = cam.transform.rotation;
            }
        }

        void OnDestroy()
        {
            if (NameTag != null && NameTag.transform.parent != transform)
            {
                Destroy(NameTag.gameObject);
            }
        }
    }
}
