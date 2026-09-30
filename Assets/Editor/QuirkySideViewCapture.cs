using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quirky Series 캐릭터 프리팹의 측면 모습을 1:1 투명 배경 PNG로 저장한다.
/// 기본 상태(Idle) 애니메이션의 첫 프레임 포즈, LOD0, 직교 카메라로 촬영한다.
/// 리그 스케일 때문에 렌더러 바운드를 신뢰할 수 없어서, 실제 렌더 결과의 알파 영역으로 프레이밍한다.
/// </summary>
public static class QuirkySideViewCapture
{
    const string PrefabFolder = "Assets/Quirky Series Ultimate/FREE/Prefabs";
    const string OutputFolder = "Screenshots/QuirkySideViews";
    const int Size = 1024;
    const int ProbeSize = 256;
    const float Padding = 0.08f;
    const int MaxProbeIterations = 12;

    [MenuItem("Tools/GalaShow/Capture Quirky Side Views (Face Right)")]
    static void CaptureFacingRight() => CaptureAll(Vector3.right, "right");

    [MenuItem("Tools/GalaShow/Capture Quirky Side Views (Face Left)")]
    static void CaptureFacingLeft() => CaptureAll(Vector3.left, "left");

    // cameraSide: 카메라를 둘 방향. 캐릭터 정면이 +Z일 때 +X에서 보면 화면 오른쪽을 바라본다.
    static void CaptureAll(Vector3 cameraSide, string suffix)
    {
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
        var paths = new List<string>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            // 하위 폴더(Single LODs)는 제외한다.
            if (Path.GetDirectoryName(path).Replace('\\', '/') == PrefabFolder)
                paths.Add(path);
        }

        var outDir = Path.Combine(Directory.GetCurrentDirectory(), OutputFolder);
        Directory.CreateDirectory(outDir);

        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            for (var i = 0; i < paths.Count; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
                EditorUtility.DisplayProgressBar("Quirky Side Views", prefab.name, (float)i / paths.Count);
                var png = Capture(prefab, scene, cameraSide);
                if (png == null)
                {
                    Debug.LogWarning($"[QuirkySideViewCapture] {prefab.name}: 화면에서 캐릭터를 찾지 못했습니다.");
                    continue;
                }
                File.WriteAllBytes(Path.Combine(outDir, $"{prefab.name}_side_{suffix}.png"), png);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            EditorSceneManager.ClosePreviewScene(scene);
        }

        Debug.Log($"[QuirkySideViewCapture] 저장 위치: {outDir}");
        EditorUtility.RevealInFinder(outDir);
    }

    static byte[] Capture(GameObject prefab, Scene scene, Vector3 cameraSide)
    {
        var instance = Object.Instantiate(prefab);
        SceneManager.MoveGameObjectToScene(instance, scene);
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        var lights = new List<GameObject>();
        var cameraGo = new GameObject("CaptureCamera");

        try
        {
            ApplyDefaultPose(instance);

            var lodGroup = instance.GetComponent<LODGroup>();
            if (lodGroup != null)
                lodGroup.ForceLOD(0);

            foreach (var skinned in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                skinned.updateWhenOffscreen = true;

            SceneManager.MoveGameObjectToScene(cameraGo, scene);
            var cam = cameraGo.AddComponent<Camera>();
            cam.scene = scene;
            cam.cameraType = CameraType.Preview;
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.allowHDR = false;
            cam.allowMSAA = true;
            cam.aspect = 1f;

            var viewDir = -cameraSide;
            var rotation = Quaternion.LookRotation(viewDir, Vector3.up);
            var right = rotation * Vector3.right;

            lights.Add(CreateLight(scene, Quaternion.Euler(35f, -40f, 0f) * rotation, 1.1f));
            lights.Add(CreateLight(scene, Quaternion.Euler(-20f, 140f, 0f) * rotation, 0.45f));

            // 초기 추정값: 렌더러 바운드(부정확할 수 있음). 이후 렌더 결과로 보정한다.
            var bounds = RoughBounds(instance);
            var center = bounds.center;
            var orthoSize = Mathf.Max(bounds.extents.magnitude, 0.5f) * 2f;

            var framed = false;
            for (var i = 0; i < MaxProbeIterations; i++)
            {
                Place(cam, center, viewDir, rotation, orthoSize);
                var probe = Render(cam, scene, ProbeSize);
                var found = FindOpaqueRect(probe, out var min, out var max);
                Object.DestroyImmediate(probe);

                if (!found || min.x <= 0 || min.y <= 0 || max.x >= ProbeSize - 1 || max.y >= ProbeSize - 1)
                {
                    // 비었거나 가장자리에 걸리면 시야를 넓혀 다시 찍는다.
                    orthoSize *= 2f;
                    continue;
                }

                // 픽셀 영역을 월드 좌표로 바꿔 중심과 크기를 다시 맞춘다.
                var worldPerPixel = orthoSize * 2f / ProbeSize;
                var pixelCenter = (min + max + Vector2.one) * 0.5f - new Vector2(ProbeSize, ProbeSize) * 0.5f;
                center += right * (pixelCenter.x * worldPerPixel) + Vector3.up * (pixelCenter.y * worldPerPixel);
                var extent = Mathf.Max(max.x - min.x + 1, max.y - min.y + 1) * worldPerPixel * 0.5f;
                orthoSize = extent / (1f - Padding * 2f);
                framed = true;
                break;
            }

            if (!framed)
                return null;

            Place(cam, center, viewDir, rotation, orthoSize);
            var tex = Render(cam, scene, Size);
            var png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            return png;
        }
        finally
        {
            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(cameraGo);
            foreach (var light in lights)
                Object.DestroyImmediate(light);
        }
    }

    static void Place(Camera cam, Vector3 center, Vector3 viewDir, Quaternion rotation, float orthoSize)
    {
        var distance = Mathf.Max(orthoSize * 20f, 10f);
        cam.orthographicSize = orthoSize;
        cam.transform.SetPositionAndRotation(center - viewDir * distance, rotation);
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = distance * 2f;
    }

    static Texture2D Render(Camera cam, Scene scene, int size)
    {
        var rt = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 8);
        cam.targetTexture = rt;

        Unsupported.SetOverrideLightingSettings(scene);
        try
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.45f);
            RenderSettings.fog = false;
            cam.Render();
        }
        finally
        {
            Unsupported.RestoreOverrideLightingSettings();
        }

        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        tex.Apply();
        RenderTexture.active = previous;

        cam.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
        return tex;
    }

    // 알파가 있는 픽셀들의 경계 사각형(픽셀 좌표, 좌하단 원점)을 구한다.
    static bool FindOpaqueRect(Texture2D tex, out Vector2 min, out Vector2 max)
    {
        var pixels = tex.GetPixels32();
        int w = tex.width, h = tex.height;
        int minX = w, minY = h, maxX = -1, maxY = -1;
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            if (pixels[y * w + x].a < 8)
                continue;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        min = new Vector2(minX, minY);
        max = new Vector2(maxX, maxY);
        return maxX >= 0;
    }

    static GameObject CreateLight(Scene scene, Quaternion rotation, float intensity)
    {
        var go = new GameObject("CaptureLight");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.rotation = rotation;
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = intensity;
        light.shadows = LightShadows.None;
        return go;
    }

    // Animator 기본 상태 클립의 첫 프레임을 적용해 바인드 포즈 대신 자연스러운 자세로 찍는다.
    static void ApplyDefaultPose(GameObject instance)
    {
        var animator = instance.GetComponentInChildren<Animator>();
        if (animator == null || !(animator.runtimeAnimatorController is AnimatorController controller))
            return;
        if (controller.layers.Length == 0)
            return;

        var state = controller.layers[0].stateMachine.defaultState;
        if (state != null && state.motion is AnimationClip clip)
            clip.SampleAnimation(animator.gameObject, 0f);
    }

    static Bounds RoughBounds(GameObject instance)
    {
        var renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(instance.transform.position, Vector3.one);

        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers)
            bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
}
