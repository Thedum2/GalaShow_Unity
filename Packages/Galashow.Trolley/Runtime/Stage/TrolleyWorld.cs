using System.Collections.Generic;
using Galashow.Common;
using TMPro;
using UnityEngine;

namespace Galashow.Trolley
{
    /// <summary>
    /// 트롤리 무대의 3D 샘플 오브젝트 (프리미티브로 생성)
    /// 선로: 본선 → 분기점 → A(왼쪽)/B(오른쪽) 지선. 각 지선 끝에 선택지 대상물.
    /// </summary>
    public class TrolleyWorld
    {
        public static readonly Color ColorA = new Color(0.22f, 0.48f, 0.96f);
        public static readonly Color ColorB = new Color(0.96f, 0.52f, 0.16f);

        static readonly Vector3 Start = new Vector3(0f, 0f, -12f);
        static readonly Vector3 FarStart = new Vector3(0f, 0f, -64f);
        static readonly Vector3 Junction = new Vector3(0f, 0f, 0f);
        static readonly Vector3[] BranchBend = { new Vector3(-7f, 0f, 7f), new Vector3(7f, 0f, 7f) };
        static readonly Vector3[] BranchEnd = { new Vector3(-9f, 0f, 17f), new Vector3(9f, 0f, 17f) };
        static readonly Vector3[] TargetPos = { new Vector3(-8.1f, 0f, 12.5f), new Vector3(8.1f, 0f, 12.5f) };

        public Transform Root { get; }
        public Transform Trolley { get; private set; }
        public Transform LeverPivot { get; private set; }
        public Camera Camera { get; private set; }

        /// <summary>
        /// 카메라 이동용 (연출 코드가 움직인다)
        /// </summary>
        public Transform CameraRig { get; private set; }

        /// <summary>
        /// 흔들림 전용 (Feel 셰이커가 로컬 값을 흔들고 되돌린다)
        /// </summary>
        public Transform CameraShake { get; private set; }

        public Light Sun { get; private set; }
        public Light[] WarningLights { get; } = new Light[2];
        public Renderer[] WarningLamps { get; } = new Renderer[2];
        public Transform Chimney { get; private set; }
        public Transform WheelsFront { get; private set; }
        public Transform[] Targets { get; } = new Transform[2];

        public static readonly Vector3 ChaseCameraPosition = new Vector3(0f, 6.5f, -21f);
        public static readonly Vector3 ChaseCameraLookAt = new Vector3(0f, 0f, 4f);
        public static readonly Vector3 LeverPosition = new Vector3(2.4f, 0f, -0.8f);

        readonly TextMeshPro[] _signs = new TextMeshPro[2];

        public TrolleyWorld(Transform parent)
        {
            Root = new GameObject("World").transform;
            Root.SetParent(parent, false);
        }

        public void Build(IReadOnlyList<TrolleyChoice> choices)
        {
            BuildEnvironment();

            BuildTrack(FarStart, Start);
            BuildTrack(Start, Junction);
            for (int i = 0; i < 2; i++)
            {
                BuildTrack(Junction, BranchBend[i]);
                BuildTrack(BranchBend[i], BranchEnd[i]);
                BuildTargets(i, choices[i]);
            }

            BuildLever();
            BuildTrolley();
            Trolley.position = FarStart;
        }

        /// <summary>
        /// 트롤리 경로 (본선 + 선택 지선)
        /// </summary>
        public void SetSignsVisible(bool visible)
        {
            foreach (var sign in _signs)
            {
                if (sign != null) sign.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// 지선 i의 경로 (분기점 → 굽이 → 끝)
        /// </summary>
        public Vector3[] Branch(int i) => new[] { Junction, BranchBend[i], BranchEnd[i] };

        /// <summary>
        /// 경로 위 거리 d의 위치와 진행 방향
        /// </summary>
        public static void Sample(Vector3[] path, float distance, out Vector3 position, out Vector3 direction)
        {
            for (int i = 1; i < path.Length; i++)
            {
                float segment = Vector3.Distance(path[i - 1], path[i]);
                direction = (path[i] - path[i - 1]).normalized;
                if (distance <= segment || i == path.Length - 1)
                {
                    position = path[i - 1] + direction * Mathf.Min(distance, segment);
                    return;
                }
                distance -= segment;
            }
            position = path[0];
            direction = Vector3.forward;
        }

        public static float Length(Vector3[] path)
        {
            float total = 0f;
            for (int i = 1; i < path.Length; i++) total += Vector3.Distance(path[i - 1], path[i]);
            return total;
        }

        /// <summary>
        /// 트롤리 경로: 출발점 → 분기점 → 지선 끝
        /// </summary>
        public Vector3[] RunPath(int branch) => new[] { Start, Junction, BranchBend[branch], BranchEnd[branch] };

        public static Vector3 StartPoint => Start;

        /// <summary>
        /// 트롤리가 달려오기 시작하는 먼 지점
        /// </summary>
        public static Vector3 FarStartPoint => FarStart;

        /// <summary>
        /// 바퀴 (주행 속도에 맞춰 돌린다)
        /// </summary>
        public System.Collections.Generic.List<Transform> Wheels { get; } = new System.Collections.Generic.List<Transform>();
        public static Vector3 JunctionPoint => Junction;

        #region Builders

        void BuildEnvironment()
        {
            // 리그(이동) → 흔들림 → 카메라. 기본 구도: 트롤리 뒤·위에서 분기(왼쪽 A, 오른쪽 B)를 본다
            CameraRig = new GameObject("CameraRig").transform;
            CameraRig.SetParent(Root, false);
            CameraRig.position = ChaseCameraPosition;
            CameraRig.LookAt(ChaseCameraLookAt);
            CameraShake = new GameObject("CameraShake").transform;
            CameraShake.SetParent(CameraRig, false);
            var camGo = new GameObject("TrolleyCamera");
            camGo.transform.SetParent(CameraShake, false);
            Camera = camGo.AddComponent<Camera>();
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = new Color(0.98f, 0.62f, 0.42f);
            Camera.fieldOfView = 45f;
            Camera.depth = 50f;

            var lightGo = new GameObject("TrolleyLight");
            lightGo.transform.SetParent(Root, false);
            // 해질녘 역광 느낌
            lightGo.transform.rotation = Quaternion.Euler(28f, -35f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.86f, 0.7f);
            light.shadows = LightShadows.Soft;
            Sun = light;

            Box("Ground", new Vector3(0f, -0.1f, -20f), new Vector3(70f, 0.2f, 96f), new Color(0.47f, 0.72f, 0.36f));

            // 본선을 따라 선 전신주 (달려올 때 속도감)
            for (float z = -62f; z < -4f; z += 6f)
            {
                Primitive(PrimitiveType.Cylinder, "Pole", new Vector3(2.8f, 1.6f, z), new Vector3(0.14f, 1.6f, 0.14f), new Color(0.36f, 0.26f, 0.18f));
                Box("PoleBar", new Vector3(2.8f, 3f, z), new Vector3(1.1f, 0.08f, 0.08f), new Color(0.3f, 0.22f, 0.15f));
            }

            // 배경 장식: 나무(원기둥 + 구)
            var rng = new System.Random(7);
            for (int i = 0; i < 44; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                var pos = i < 18
                    ? new Vector3(side * (12f + (float)rng.NextDouble() * 9f), 0f, -6f + (float)rng.NextDouble() * 30f)
                    : new Vector3(side * (5f + (float)rng.NextDouble() * 14f), 0f, -64f + (float)rng.NextDouble() * 50f);
                Primitive(PrimitiveType.Cylinder, "Trunk", pos + Vector3.up * 0.7f, new Vector3(0.35f, 0.7f, 0.35f), new Color(0.45f, 0.3f, 0.18f));
                Primitive(PrimitiveType.Sphere, "Leaves", pos + Vector3.up * 2f, Vector3.one * (1.6f + (float)rng.NextDouble()), new Color(0.2f, 0.52f, 0.25f));
            }
        }

        void BuildTrack(Vector3 from, Vector3 to)
        {
            var dir = to - from;
            float length = dir.magnitude;
            var rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            var right = rotation * Vector3.right;
            var center = (from + to) * 0.5f;

            for (int side = -1; side <= 1; side += 2)
            {
                var rail = Box("Rail", center + right * (0.45f * side) + Vector3.up * 0.12f, new Vector3(0.09f, 0.12f, length + 0.1f), new Color(0.55f, 0.56f, 0.6f));
                rail.rotation = rotation;
            }

            for (float d = 0.3f; d < length; d += 0.85f)
            {
                var sleeper = Box("Sleeper", from + dir.normalized * d + Vector3.up * 0.04f, new Vector3(1.35f, 0.08f, 0.28f), new Color(0.42f, 0.28f, 0.17f));
                sleeper.rotation = rotation;
            }
        }

        void BuildTargets(int index, TrolleyChoice choice)
        {
            var color = index == 0 ? ColorA : ColorB;
            var basePos = TargetPos[index];
            Targets[index] = new GameObject($"Target{choice.Id}").transform;
            Targets[index].SetParent(Root, false);
            Targets[index].position = basePos;

            // 표지판
            basePos = BranchEnd[index] + new Vector3(index == 0 ? -2.2f : 2.2f, 0f, -0.5f);
            Box("SignPost", basePos + new Vector3(0f, 1.2f, 1.4f), new Vector3(0.12f, 2.4f, 0.12f), new Color(0.35f, 0.25f, 0.18f));
            Box("SignBoard", basePos + new Vector3(0f, 2.7f, 1.4f), new Vector3(3.6f, 1.3f, 0.12f), color);

            var textGo = new GameObject("SignText");
            textGo.transform.SetParent(Root, false);
            textGo.transform.position = basePos + new Vector3(0f, 2.7f, 1.32f);
            var tmp = textGo.AddComponent<TextMeshPro>();
            tmp.text = $"{index + 1}\n<size=60%>{choice.Label}</size>";
            tmp.fontSize = 6f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = new Vector2(3.4f, 1.2f);
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 1f;
            tmp.fontSizeMax = 6f;
            _signs[index] = tmp;
        }

        void BuildLever()
        {
            var basePos = LeverPosition;
            Box("LeverBase", basePos + Vector3.up * 0.2f, new Vector3(0.8f, 0.4f, 0.8f), new Color(0.3f, 0.3f, 0.33f));

            LeverPivot = new GameObject("LeverPivot").transform;
            LeverPivot.SetParent(Root, false);
            LeverPivot.position = basePos + Vector3.up * 0.4f;

            var handle = Primitive(PrimitiveType.Cylinder, "LeverHandle", Vector3.zero, new Vector3(0.12f, 0.75f, 0.12f), new Color(0.7f, 0.7f, 0.72f));
            handle.SetParent(LeverPivot, false);
            handle.localPosition = Vector3.up * 0.75f;
            var knob = Primitive(PrimitiveType.Sphere, "LeverKnob", Vector3.zero, Vector3.one * 0.32f, new Color(0.9f, 0.15f, 0.15f));
            knob.SetParent(LeverPivot, false);
            knob.localPosition = Vector3.up * 1.5f;

            // 분기점 경고등
            for (int i = 0; i < 2; i++)
            {
                var pos = new Vector3(i == 0 ? -1.8f : 1.8f, 0f, 1.2f);
                Box("LampPost", pos + Vector3.up * 1.1f, new Vector3(0.12f, 2.2f, 0.12f), new Color(0.2f, 0.2f, 0.22f));
                var lamp = Primitive(PrimitiveType.Sphere, "Lamp", pos + Vector3.up * 2.3f, Vector3.one * 0.38f, new Color(0.35f, 0.05f, 0.05f));
                WarningLamps[i] = lamp.GetComponent<Renderer>();
                var lightGo = new GameObject("LampLight");
                lightGo.transform.SetParent(lamp, false);
                var pl = lightGo.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.color = new Color(1f, 0.15f, 0.1f);
                pl.range = 6f;
                pl.intensity = 0f;
                WarningLights[i] = pl;
            }
        }

        void BuildTrolley()
        {
            Trolley = new GameObject("Trolley").transform;
            Trolley.SetParent(Root, false);

            Part(PrimitiveType.Cube, new Vector3(0f, 0.8f, 0f), new Vector3(1.3f, 0.8f, 2.3f), new Color(0.85f, 0.16f, 0.16f));
            Part(PrimitiveType.Cube, new Vector3(0f, 1.55f, -0.55f), new Vector3(1.15f, 0.75f, 1.0f), new Color(0.95f, 0.85f, 0.55f));
            Part(PrimitiveType.Cube, new Vector3(0f, 1.98f, -0.55f), new Vector3(1.3f, 0.12f, 1.15f), new Color(0.25f, 0.25f, 0.28f));
            Chimney = Part(PrimitiveType.Cylinder, new Vector3(0f, 1.6f, 0.75f), new Vector3(0.28f, 0.4f, 0.28f), new Color(0.2f, 0.2f, 0.22f));
            Part(PrimitiveType.Sphere, new Vector3(0f, 0.95f, 1.18f), Vector3.one * 0.3f, new Color(1f, 0.95f, 0.5f));

            WheelsFront = new GameObject("WheelsFront").transform;
            WheelsFront.SetParent(Trolley, false);
            WheelsFront.localPosition = new Vector3(0f, 0.05f, 0.72f);

            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                var wheel = Part(PrimitiveType.Cylinder, new Vector3(0.62f * x, 0.32f, 0.72f * z), new Vector3(0.52f, 0.06f, 0.52f), new Color(0.12f, 0.12f, 0.12f));
                wheel.localRotation = Quaternion.Euler(0f, 0f, 90f);
                // 바퀴살 (회전이 보이도록)
                var spoke = Primitive(PrimitiveType.Cube, "Spoke", Vector3.zero, new Vector3(0.9f, 2.4f, 0.12f), new Color(0.75f, 0.7f, 0.2f));
                spoke.SetParent(wheel, false);
                spoke.localPosition = Vector3.zero;
                Wheels.Add(wheel);
            }
        }

        Transform Part(PrimitiveType type, Vector3 localPos, Vector3 scale, Color color)
        {
            var t = Primitive(type, "TrolleyPart", Vector3.zero, scale, color);
            t.SetParent(Trolley, false);
            t.localPosition = localPos;
            return t;
        }

        Transform Box(string name, Vector3 pos, Vector3 scale, Color color, bool keepCollider = false)
        {
            return Primitive(PrimitiveType.Cube, name, pos, scale, color, keepCollider || name == "Ground");
        }

        Transform Primitive(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Color color, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (!keepCollider)
            {
                Object.Destroy(go.GetComponent<Collider>());
            }

            var t = go.transform;
            t.SetParent(Root, false);
            t.position = pos;
            t.localScale = scale;
            // 빌드에서 셰이더가 빠지지 않는 공용 머티리얼 (색별 공유)
            go.GetComponent<Renderer>().sharedMaterial = GalashowMaterials.Lit(color);
            return t;
        }

        #endregion
    }
}
