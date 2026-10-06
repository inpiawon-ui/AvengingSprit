using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// 로비 왼쪽 위 «유령 프로필» 판 (A안, PD 확정 2026-10-06 · 시안 `_exchange/ref/lobby_profile/profile_A.png`).
    ///
    /// 로고 자리(로고는 배경 `base_top` 에서 지웠다)에 유령 얼굴 · LV · 경험치 막대 · 수치를 놓는다.
    /// 판 그림은 `Projects/AVSR/Tools/lobby_profile_cut.py` 가 «빈 판» 납품에서 잘라 둔다.
    /// 값은 `LobbyMainUI.RefreshProfile` 이 채운다. 판을 누르면 육성의 유령 탭이 열린다.
    ///
    /// 좌표는 시안 화면(720 x 1280) 그대로 — `LobbyV4Top` 는 화면 맨 위 720 칸이다.
    /// 로비 빌더(`LobbyV4Builder`)가 `LobbyV4Top` 을 통째로 다시 만들므로 빌더 끝에서 이것을 부른다.
    /// </summary>
    public static class LobbyProfileBinder
    {
        private const string Prefab = "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab";
        private const string Dir = "Assets/BaseResource/LobbyV4";
        // 얼굴 원의 유령 — 육성의 유령 초상(PD 기준 유령과 같은 그림, 512px 라 원을 키워도 또렷하다)
        private const string GhostFace = "Assets/BaseResource/Growth/Portraits/portrait_ghost.png";
        private const string PixelFont = "Assets/BaseResource/Fonts/OriginalPixel SDF.asset";

        // 판 안 좌표 — 시안(profile_A.png)의 판 칸 250 x 115 기준, 4배로 키워 잰 값
        private static readonly Rect FaceBox = Box(13, 23, 97, 103);      // 얼굴 원 안 — 원을 꽉 채운다
        private static readonly Rect LevelBox = Box(149, 28, 222, 51);    // 노란 LV 배지 안
        private static readonly Rect FillBox = Box(108, 64, 224, 74);     // 경험치 홈 안
        private static readonly Rect ExpTextBox = Box(104, 80, 228, 100); // 막대 아래 수치

        /// <summary>
        /// 화면에 놓는 판 칸. 시안의 판(0~250)은 배경에 그려진 금화 칸(화면 x 233 부터)과 12px 겹쳤다 —
        /// 시안은 금화 칸을 옆으로 밀었지만 게임의 금화 칸은 배경 그림이라 못 민다. 판을 같은 비율로 줄인다.
        /// </summary>
        private static readonly Rect PlateBox = Box(4, 6, 230, 110);
        private static float K => PlateBox.width / 250f;

        /// <summary>경험치 채움의 가득 찬 폭 — `LobbyMainUI.ProfileFillWidth` 와 같아야 한다.</summary>
        public const float FillWidth = 116f * 226f / 250f;   // 홈 폭 × 판 배율

        private static Rect Box(float x0, float y0, float x1, float y1) => new(x0, y0, x1 - x0, y1 - y0);

        [MenuItem("Tools/Game/로비 — 유령 프로필 판 꽂기")]
        public static void Run()
        {
            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                Bind(root);
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            Debug.Log("[LobbyProfile] 꽂기 끝");
        }

        public static void Bind(GameObject root)
        {
            var top = root.transform.Find("LobbyV4Top") as RectTransform;
            if (top == null) { Debug.LogError("[LobbyProfile] LobbyV4Top 이 없다"); return; }
            foreach (var f in new[] { $"{Dir}/profile_plate.png", $"{Dir}/profile_fill.png" }) EnsureSprite(f);

            var old = top.Find("LobbyProfile");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var plate = Node(top, "LobbyProfile", PlateBox);
            var plateImg = plate.gameObject.AddComponent<Image>();
            plateImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Dir}/profile_plate.png");
            plate.gameObject.AddComponent<Button>().targetGraphic = plateImg;

            var face = Node(plate, "ProfileGhostFace", Local(FaceBox));
            var faceImg = face.gameObject.AddComponent<Image>();
            faceImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GhostFace);
            faceImg.preserveAspect = true;
            faceImg.raycastTarget = false;

            var fill = Node(plate, "ProfileExpFill", Local(FillBox));
            var fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Dir}/profile_fill.png");
            fillImg.raycastTarget = false;

            var pixel = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFont);
            Text(plate, "ProfileLevelText", Local(LevelBox), 17f, new Color(1f, 0.84f, 0.2f), pixel);
            Text(plate, "ProfileExpText", Local(ExpTextBox), 12f, Color.white, pixel);
        }

        /// <summary>판 안 좌표로 바꾼다(판 왼쪽 위 기준).</summary>
        private static Rect Local(Rect r) => new(r.x * K, r.y * K, r.width * K, r.height * K);

        private static RectTransform Node(Transform parent, string name, Rect r)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(r.x, -r.y);
            rt.sizeDelta = r.size;
            return rt;
        }

        private static void Text(Transform parent, string name, Rect r, float size, Color color, TMP_FontAsset font)
        {
            var rt = Node(parent, name, r);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) { tmp.font = font; tmp.fontSharedMaterial = font.material; }
            tmp.text = string.Empty;
            tmp.alignment = TextAlignmentOptions.Midline;
            tmp.color = color;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = size;
            tmp.fontSizeMin = 7f;
            tmp.fontSize = size;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Truncate;   // 픽셀 글꼴에 「…」 가 없다 — 말줄임은 글자를 지운다
            tmp.characterSpacing = -10f;
            tmp.raycastTarget = false;
        }

        private static void EnsureSprite(string path)
        {
            AssetDatabase.ImportAsset(path);
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti) return;
            if (ti.textureType == TextureImporterType.Sprite && !ti.mipmapEnabled
                && ti.textureCompression == TextureImporterCompression.Uncompressed) return;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }
    }
}
