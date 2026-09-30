using System.Collections.Generic;
using System.Linq;
using Galashow.Common;
using UnityEngine;

namespace Galashow.Trolley
{
    /// <summary>
    /// 참가자 캐릭터 배치
    /// - 입력 전: 출발점 양옆에 서서 대기
    /// - 입력: 고른 선택지의 선로로 옮겨 가 가로로 눕는다 (입력을 바꾸면 다른 선로로 이동)
    /// - 결과: 트롤리가 지나가는 선로의 캐릭터는 날아가고, 호스트가 지킨 선로의 캐릭터는 일어나 기뻐한다
    /// </summary>
    public class TrolleyCast : MonoBehaviour
    {
        public const float CharacterScale = 0.85f;
        const float MoveSpeed = 9f;
        const float TurnSpeed = 10f;
        const float FirstSlot = 2.2f;

        class Member
        {
            public CharacterActor Actor;
            public string Name;
            public string Choice;
            public Vector3 TargetPos;
            public Quaternion TargetRot;
            public Vector3 TagOffset;
            public bool Launched;
            public float PathDistance;
        }

        readonly Dictionary<string, Member> _members = new Dictionary<string, Member>();
        readonly List<string>[] _lanes = { new List<string>(), new List<string>() };
        TrolleyWorld _world;
        TrolleyGameData _data;

        public int Count => _members.Count;

        /// <summary>
        /// 선로 i에 누운 인원
        /// </summary>
        public int LaneCount(int lane) => _lanes[lane].Count;

        public void Build(TrolleyWorld world, TrolleyGameData data, IReadOnlyList<TrolleyParticipant> participants)
        {
            _world = world;
            _data = data;
            CharacterActor.NameTagCamera = world.Camera;

            for (int i = 0; i < participants.Count; i++)
            {
                var p = participants[i];
                var actor = CharacterActor.Spawn(world.Root, p.Id, p.Name, p.AvatarName, CharacterScale);
                var m = new Member { Actor = actor, Name = p.Name };
                _members[p.Id] = m;
                PlaceInCrowd(m, i, participants.Count);
                actor.transform.SetPositionAndRotation(m.TargetPos, m.TargetRot);
                actor.PlayBody("Idle_A");
                actor.PlayFace("Eyes_Blink");
            }
        }

        /// <summary>
        /// 참가자 입력 반영: 해당 선로로 옮겨 눕는다
        /// </summary>
        public void SetVote(string playerId, string choiceId, bool auto = false)
        {
            if (!_members.TryGetValue(playerId, out var m) || m.Choice == choiceId)
            {
                return;
            }

            int lane = _data.Choices.FindIndex(c => c.Id == choiceId);
            if (lane < 0) return;

            if (m.Choice != null)
            {
                _lanes[_data.Choices.FindIndex(c => c.Id == m.Choice)].Remove(playerId);
            }
            m.Choice = choiceId;
            _lanes[lane].Add(playerId);

            var color = lane == 0 ? TrolleyWorld.ColorA : TrolleyWorld.ColorB;
            m.Actor.SetNameTag(auto ? $"{m.Name}\n<size=70%>자동</size>" : m.Name, Color.Lerp(color, Color.white, 0.45f));
            m.Actor.PlayBody("Fear");
            m.Actor.PlayFace("Eyes_Trauma");
            LayoutLane(lane);
        }

        /// <summary>
        /// 트롤리가 지나갈 선로의 캐릭터를 경로 거리 순으로 (트롤리 경로 기준)
        /// </summary>
        public List<(string playerId, float distance)> Victims(int lane)
        {
            float offset = Vector3.Distance(TrolleyWorld.StartPoint, TrolleyWorld.JunctionPoint);
            return _lanes[lane].Select(id => (id, _members[id].PathDistance + offset)).OrderBy(v => v.Item2).ToList();
        }

        public Vector3 PositionOf(string playerId) =>
            _members.TryGetValue(playerId, out var m) ? m.Actor.transform.position : Vector3.zero;

        /// <summary>
        /// 트롤리에 치임
        /// </summary>
        public void Hit(string playerId, Vector3 trolleyForward)
        {
            if (!_members.TryGetValue(playerId, out var m) || m.Launched) return;
            m.Launched = true;
            m.Actor.SetNameTag($"{m.Name}\n<size=70%>탈락</size>", new Color(1f, 0.4f, 0.38f));

            // 트롤리 앞쪽 위로, 선로 바깥 방향으로 튕겨 나간다
            var right = Vector3.Cross(Vector3.up, trolleyForward).normalized;
            var toActor = m.Actor.transform.position - _world.Trolley.position;
            float side = Vector3.Dot(toActor, right) >= 0f ? 1f : -1f;
            if (Mathf.Abs(Vector3.Dot(toActor, right)) < 0.1f) side = Random.value < 0.5f ? -1f : 1f;
            m.Actor.Knockback(
                trolleyForward * Random.Range(3.5f, 5.5f) + Vector3.up * Random.Range(4.5f, 7f) + right * side * Random.Range(2f, 3.5f),
                new Vector3(Random.Range(-3f, 3f), Random.Range(-2f, 2f), Random.Range(-3f, 3f)));
        }

        /// <summary>
        /// 결과 표시: 생존자는 일어나 기뻐하고, 탈락자는 쓰러진다
        /// </summary>
        public void ShowOutcome(TrolleyGameResult result, bool practice = false)
        {
            foreach (var r in result.Results)
            {
                if (!_members.TryGetValue(r.ParticipantId, out var m)) continue;

                if (r.Survived)
                {
                    m.Actor.SetNameTag($"{m.Name}\n<size=70%>생존</size>", new Color(0.55f, 1f, 0.6f));
                    if (!m.Launched)
                    {
                        StandUp(m);
                        m.Actor.PlayBody(Random.value < 0.5f ? "Jump" : "Bounce");
                        m.Actor.PlayFace("Eyes_Happy");
                    }
                }
                else
                {
                    var label = (r.Choice == null ? "미입력 탈락" : "탈락") + (practice ? "(연습)" : "");
                    m.Actor.SetNameTag($"{m.Name}\n<size=70%>{label}</size>", new Color(1f, 0.4f, 0.38f));
                    if (!m.Launched)
                    {
                        m.Actor.PlayBody("Death");
                        m.Actor.PlayFace("Eyes_Dead");
                    }
                }
            }
        }

        void StandUp(Member m)
        {
            var toCamera = _world.Camera.transform.position - m.Actor.transform.position;
            toCamera.y = 0f;
            m.TargetRot = Quaternion.LookRotation(toCamera.sqrMagnitude > 0.01f ? toCamera.normalized : Vector3.back);
            if (m.Choice != null)
            {
                // 누운 자리에서 선로 옆으로 비켜 선다
                m.TargetPos = m.Actor.transform.position + m.Actor.transform.up * 0.6f;
                m.TargetPos.y = 0f;
            }
            m.TagOffset = Vector3.up * 1.45f * CharacterScale;
        }

        void PlaceInCrowd(Member m, int index, int total)
        {
            // 본선 양옆, 출발점~분기점 사이에 격자로 선다
            int perSide = Mathf.CeilToInt(total / 2f);
            int side = index % 2 == 0 ? -1 : 1;
            int k = index / 2;
            int columns = Mathf.Clamp(Mathf.CeilToInt(perSide / 5f), 1, 6);
            int row = k / columns;
            int col = k % columns;
            float x = side * (3.3f + col * 1.05f);
            float z = -7.5f + row * 1.2f;
            m.TargetPos = new Vector3(x, 0f, z);
            m.TargetRot = Quaternion.LookRotation(Vector3.back + Vector3.right * -side * 0.3f);
            m.TagOffset = Vector3.up * 1.45f * CharacterScale;
        }

        void LayoutLane(int lane)
        {
            var path = _world.Branch(lane);
            float length = TrolleyWorld.Length(path) - FirstSlot - 0.6f;
            var ids = _lanes[lane];
            float spacing = ids.Count > 0 ? Mathf.Clamp(length / ids.Count, 0.42f, 1.1f) : 1f;

            for (int i = 0; i < ids.Count; i++)
            {
                var m = _members[ids[i]];
                float d = FirstSlot + spacing * (i + 0.5f);
                TrolleyWorld.Sample(path, d, out var pos, out var dir);
                var right = Vector3.Cross(Vector3.up, dir).normalized;
                float head = i % 2 == 0 ? 1f : -1f;

                // 얼굴은 하늘, 머리는 선로 바깥쪽: 몸이 두 레일을 가로지르게 눕는다
                m.TargetRot = Quaternion.LookRotation(Vector3.up, right * head);
                m.TargetPos = pos - right * head * 0.5f + Vector3.up * 0.12f;
                m.TagOffset = Vector3.up * 1.05f + right * head * 0.5f;
                m.PathDistance = d;
            }
        }

        void Update()
        {
            foreach (var m in _members.Values)
            {
                if (m.Launched) continue;
                var t = m.Actor.transform;
                t.position = Vector3.MoveTowards(t.position, m.TargetPos, MoveSpeed * Time.deltaTime);
                t.rotation = Quaternion.Slerp(t.rotation, m.TargetRot, TurnSpeed * Time.deltaTime);
                m.Actor.SetNameTagOffset(m.TagOffset);
            }
        }
    }

    /// <summary>
    /// 무대에 세울 참가자
    /// </summary>
    public readonly struct TrolleyParticipant
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string AvatarName;

        public TrolleyParticipant(string id, string name, string avatarName)
        {
            Id = id;
            Name = name;
            AvatarName = avatarName;
        }
    }
}
