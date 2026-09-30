using UnityEngine;

namespace Galashow.Trolley
{
    /// <summary>
    /// 코드로 만드는 파티클 (Sprites/Default: 빌드에 항상 포함, 입자 색 사용)
    /// </summary>
    public static class TrolleyParticles
    {
        static Material _soft;
        static Material _solid;
        static Mesh _cube;

        static Material Soft
        {
            get
            {
                if (_soft == null)
                {
                    _soft = new Material(Shader.Find("Sprites/Default")) { mainTexture = SoftCircle() };
                }
                return _soft;
            }
        }

        static Material Solid
        {
            get
            {
                if (_solid == null)
                {
                    _solid = new Material(Shader.Find("Sprites/Default"));
                }
                return _solid;
            }
        }

        static Mesh Cube
        {
            get
            {
                if (_cube == null)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    _cube = go.GetComponent<MeshFilter>().sharedMesh;
                    Object.Destroy(go);
                }
                return _cube;
            }
        }

        /// <summary>
        /// 바퀴 불꽃 (주행 중 계속 방출)
        /// </summary>
        public static ParticleSystem Sparks(Transform parent, Vector3 localPos)
        {
            var ps = Create("Sparks", parent, localPos, Solid, stretched: true);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.3f), new Color(1f, 0.45f, 0.1f));
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.05f;
            shape.rotation = new Vector3(-150f, 0f, 0f);
            return ps;
        }

        /// <summary>
        /// 굴뚝 연기
        /// </summary>
        public static ParticleSystem Smoke(Transform parent, Vector3 localPos)
        {
            var ps = Create("Smoke", parent, localPos, Soft);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            main.startColor = new Color(0.35f, 0.35f, 0.38f, 0.55f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 7f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 10f;
            shape.radius = 0.08f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.2f));
            FadeOut(ps);
            ps.Play();
            return ps;
        }

        /// <summary>
        /// 충돌 파편 (선택지 색 정육면체)
        /// </summary>
        public static ParticleSystem Debris(Transform parent, Vector3 position, Color color)
        {
            var ps = Create("Debris", parent, position, Solid);
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = Cube;
            var main = ps.main;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 14f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.35f);
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.4f));
            main.gravityModifier = 2.2f;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.6f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.separateAxes = true;
            rotation.x = new ParticleSystem.MinMaxCurve(-8f, 8f);
            rotation.y = new ParticleSystem.MinMaxCurve(-8f, 8f);
            rotation.z = new ParticleSystem.MinMaxCurve(-8f, 8f);
            var collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.Planes;
            collision.SetPlane(0, parent);
            collision.bounce = 0.35f;
            collision.dampen = 0.3f;
            return ps;
        }

        /// <summary>
        /// 치일 때 번쩍이는 별 조각
        /// </summary>
        public static ParticleSystem HitBurst(Transform parent, Vector3 position)
        {
            var ps = Create("HitBurst", parent, position, Solid, stretched: true);
            var main = ps.main;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(1f, 0.85f, 0.2f));
            main.gravityModifier = 0.5f;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;
            return ps;
        }

        /// <summary>
        /// 충돌 먼지 구름
        /// </summary>
        public static ParticleSystem Dust(Transform parent, Vector3 position)
        {
            var ps = Create("Dust", parent, position, Soft);
            var main = ps.main;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2.6f);
            main.startColor = new Color(0.78f, 0.7f, 0.58f, 0.7f);
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.8f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var velocity = ps.limitVelocityOverLifetime;
            velocity.enabled = true;
            velocity.dampen = 0.15f;
            velocity.limit = 0.5f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1.6f));
            FadeOut(ps);
            return ps;
        }

        /// <summary>
        /// 색종이 (결과 축하)
        /// </summary>
        public static ParticleSystem Confetti(Transform parent, Vector3 position, Color a, Color b)
        {
            var ps = Create("Confetti", parent, position, Solid);
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = Cube;
            var main = ps.main;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(7f, 13f);
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(0.12f, 0.2f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(0.01f, 0.02f);
            main.startSizeZ = new ParticleSystem.MinMaxCurve(0.2f, 0.3f);
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.gravityModifier = 0.6f;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(a, 0f), new GradientColorKey(Color.white, 0.33f), new GradientColorKey(b, 0.66f), new GradientColorKey(new Color(1f, 0.85f, 0.25f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(gradient) { mode = ParticleSystemGradientMode.RandomColor };
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 180), new ParticleSystem.Burst(0.25f, 120) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f;
            shape.radius = 1f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var rotation = ps.rotationOverLifetime;
            rotation.enabled = true;
            rotation.separateAxes = true;
            rotation.x = new ParticleSystem.MinMaxCurve(-10f, 10f);
            rotation.z = new ParticleSystem.MinMaxCurve(-10f, 10f);
            var velocity = ps.limitVelocityOverLifetime;
            velocity.enabled = true;
            velocity.dampen = 0.08f;
            velocity.limit = 2f;
            return ps;
        }

        static ParticleSystem Create(string name, Transform parent, Vector3 position, Material material, bool stretched = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.maxParticles = 600;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            if (stretched)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.velocityScale = 0.06f;
                renderer.lengthScale = 1f;
            }
            return ps;
        }

        static void FadeOut(ParticleSystem ps)
        {
            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
        }

        static Texture2D SoftCircle()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                float a = Mathf.Clamp01(1f - d);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
            texture.Apply();
            return texture;
        }
    }
}
