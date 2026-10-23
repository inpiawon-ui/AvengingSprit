// RE:BORN UI 리뉴얼 — 플레이 없이 화면 프리팹을 해상도별로 찍는다(빌더 직후 시안과 나란히 대조용).
// 화면 스크립트(Start)는 돌지 않는다 — 빌더가 넣어 둔 본보기 글자 · 그림 그대로 찍힌다(배치 대조용).
// 실제 값 · 언어 확인은 플레이 + 하네스로 한다(스킬 ui-mockup-to-game 4단계).
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor.ReBorn
{
    public static class RbPreview
    {
        public static readonly (int w, int h, string name)[] Sizes = { (720, 1280, "16x9"), (1080, 2400, "20x9"), (1536, 2048, "4x3") };

        /// <summary>프리팹을 찍어 `outDir/{이름}_{비율}.png` 로. 안전 영역은 없다고 본다(노치 없는 화면).</summary>
        public static void Shoot(string prefabPath, string outDir, string label)
        {
            Directory.CreateDirectory(outDir);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            foreach (var (w, h, name) in Sizes)
            {
                var camGo = new GameObject("RbPreviewCam") { hideFlags = HideFlags.HideAndDontSave };
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.orthographic = true;
                cam.cullingMask = 1 << 5;
                var rtex = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rtex;

                var canvasGo = new GameObject("RbPreviewCanvas", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave, layer = 5 };
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 10f;
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(720, 1280);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                // contain(규칙 05) — 작은 배율 쪽에 맞춘다
                scaler.matchWidthOrHeight = h / 1280f < w / 720f ? 1f : 0f;

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.hideFlags = HideFlags.HideAndDontSave;
                inst.transform.SetParent(canvasGo.transform, false);
                foreach (var t in inst.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 5;
                foreach (var mb in inst.GetComponentsInChildren<MonoBehaviour>(true))
                    if (mb.GetType().Namespace != null && mb.GetType().Namespace.StartsWith("Game.Module")) mb.enabled = false;

                // 캔버스 크기 계산을 위해 강제 갱신 — 화면 크기는 렌더 대상에서 온다
                Canvas.ForceUpdateCanvases();
                var crt = (RectTransform)canvasGo.transform;
                crt.sizeDelta = new Vector2(w, h);
                LayoutRebuilder.ForceRebuildLayoutImmediate(crt);
                Canvas.ForceUpdateCanvases();
                cam.Render();

                RenderTexture.active = rtex;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(outDir, $"{label}_{name}.png"), tex.EncodeToPNG());

                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(inst);
                Object.DestroyImmediate(canvasGo);
                cam.targetTexture = null;
                Object.DestroyImmediate(rtex);
                Object.DestroyImmediate(camGo);
            }
        }

        [MenuItem("Tools/Game/RE:BORN 화면/미리보기 — 로비")]
        public static void ShootLobby()
            => Shoot(RbLobbyBuilder.PrefabPath, "Projects/AVSR/_exchange/ref/ui_reborn_1023/preview", "lobby");
    }
}
