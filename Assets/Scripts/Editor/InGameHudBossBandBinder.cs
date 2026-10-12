using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// 보스 체력을 HUD 바로 아래 **가로 띠**로 — PD 2026-10-12 「보스 유아이가 내려와서 거슬린다, 우리 유아이에 들어가게」.
    /// 코덱스 시안 ②(띠안) + ①의 주황 잔상을 PD 가 골랐다: `_exchange/in/mock_bosshud_band.png` · `mock_bosshud_free.png`.
    ///
    /// 옛 큰 판(`BossGroup`)의 노드 이름은 그대로 쓰고(= 바인딩 키) 자리 · 그림만 바꾼다.
    ///   BossGroup      띠 전체(화면 위에서 128, 폭 688 · 높이 48 — 시안 941 폭을 720 으로 환산)
    ///   BossPlate      hud3_boss_band(코덱스 부품)
    ///   BossLabel      왼쪽 이름 · 단계
    ///   BossHpLabel    빨간 「HP」(유령 칸 HpLabelGhost 복제 — 같은 도트 글꼴)
    ///   BossHpBarBg    hud3_stage_track(ROOM 진행 홈과 같은 부품, 가로 9칸 늘림)
    ///     BossHpBarTrail hud3_fill_orange(코덱스 부품 — 막 깎인 만큼 잠깐 남는 잔상)
    ///     BossHpBarFill  hud3_fill_red
    ///   BossHpText     오른쪽 숫자(유령 칸 GhostHpText 와 같은 도트 글꼴)
    ///
    /// HUD 빌더(3차 꽂기)를 다시 돌리면 이 메뉴도 한 번 더 돌린다. 막대 폭이 바뀌면 `InGameMainUI.BossBarWidth` 도 같이.
    /// </summary>
    public static class InGameHudBossBandBinder
    {
        private const string Prefab = "Assets/BundleResource/Prefabs/UI/InGame/InGameMainUI.prefab";
        private const string Dir = "Assets/BaseResource/InGameMainUI/hud3/";

        // 띠 안 자리(띠 왼쪽 위 기준, 720 화면 px) — 시안 ② 를 720/941 로 환산
        // HUD 판이 화면 맞춤으로 조금 커져 보여서(ScreenFitShare) 띠도 시안보다 양옆을 12씩 줄였다 — 화면 끝에 닿지 않게
        private static readonly Rect Band = new(28f, 128f, 664f, 48f);
        // 이름 칸은 3언어 가장 긴 「Commando (Missile) P2」가 들어가게 넓혔다(Noto Bold 11pt 127px) — 막대가 시안보다 조금 짧다
        private static readonly Rect Name = new(26f, 0f, 142f, 48f);
        private static readonly Rect HpLabel = new(168f, 0f, 32f, 48f);
        private static readonly Rect Track = new(202f, 10f, 330f, 28f);
        private const float FillInsetX = 7f, FillInsetY = 7f;   // 홈 테두리 안쪽(ROOM 진행 막대와 같은 비율)
        private static readonly Rect Number = new(532f, 0f, 106f, 48f);   // 오른쪽 둥근 끝에 닿지 않게 26 띄운다
        private const string NameFont = "Assets/BaseResource/Fonts/NotoSansKR-Bold SDF.asset";
        private static readonly Color HpRed = new(1f, 0.25f, 0.25f);

        [MenuItem("Tools/Game/인게임 HUD 보스 띠 꽂기")]
        public static void Run()
        {
            var band = Sprite(Dir + "hud3_boss_band.png");
            var track = Sprite(Dir + "hud3_stage_track.png");
            var red = Sprite(Dir + "hud3_fill_red.png");
            var orange = Sprite(Dir + "hud3_fill_orange.png");
            if (band == null || track == null || red == null || orange == null)
            {
                Debug.LogError("[보스 띠] 부품 그림이 없다 — hud3_boss_band / hud3_fill_orange 를 먼저 넣을 것");
                return;
            }
            SetSideBorder(Dir + "hud3_stage_track.png", 12);

            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var group = Find(root.transform, "BossGroup") as RectTransform;
                if (group == null) { Debug.LogError("[보스 띠] BossGroup 없음"); return; }

                group.anchorMin = group.anchorMax = new Vector2(0f, 1f);
                group.pivot = new Vector2(0f, 1f);
                group.anchoredPosition = new Vector2(Band.x, -Band.y);
                group.sizeDelta = new Vector2(Band.width, Band.height);

                var plate = Place(group, "BossPlate", new Rect(0f, 0f, Band.width, Band.height));
                SetImage(plate, band, Image.Type.Simple);

                var label = Place(group, "BossLabel", Name);
                if (label.TryGetComponent<TMP_Text>(out var lt))
                {
                    // 도트 글꼴은 영어 긴 이름이 칸의 두 배라 7pt 까지 줄었다 — 굵은 본문 글꼴(언어별로 바뀐다)로
                    var nf = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NameFont);
                    if (nf != null) { lt.font = nf; lt.fontSharedMaterial = nf.material; }
                    lt.alignment = TextAlignmentOptions.Left;
                    lt.color = Color.white;
                    Fit(lt, 19f);   // 시안 이름 크기(짧은 이름은 19pt, 긴 영어 이름만 줄어든다)
                    lt.fontSizeMin = 9f;
                }

                var hpLabel = CloneText(root.transform, "HpLabelGhost", group, "BossHpLabel");
                Place(group, hpLabel.name, HpLabel);
                if (hpLabel.TryGetComponent<TMP_Text>(out var hl))
                {
                    hl.text = "HP";
                    hl.color = HpRed;
                    hl.alignment = TextAlignmentOptions.Center;
                    Fit(hl, 15f);
                }

                var bg = Place(group, "BossHpBarBg", Track);
                SetImage(bg, track, Image.Type.Sliced);
                var fillRect = new Rect(FillInsetX, FillInsetY, Track.width - FillInsetX * 2f, Track.height - FillInsetY * 2f);
                var fill = Place(bg, "BossHpBarFill", fillRect);
                SetImage(fill, red, Image.Type.Simple);
                var trail = Find(bg, "BossHpBarTrail") as RectTransform;
                if (trail == null)
                {
                    trail = (RectTransform)Object.Instantiate(fill.gameObject, bg).transform;
                    trail.name = "BossHpBarTrail";
                }
                Place(bg, "BossHpBarTrail", fillRect);
                SetImage(trail, orange, Image.Type.Simple);
                // 잔상은 빨강 **뒤** — 앞에 두면 막대 전체가 주황으로 보인다(2026-10-12 실측)
                trail.SetAsFirstSibling();
                fill.SetAsLastSibling();

                var num = Place(group, "BossHpText", Number);
                var srcNum = Find(root.transform, "GhostHpText");
                if (num.TryGetComponent<TMP_Text>(out var nt) && srcNum != null && srcNum.TryGetComponent<TMP_Text>(out var st))
                {
                    nt.font = st.font;
                    nt.fontSharedMaterial = st.fontSharedMaterial;
                    nt.color = Color.white;
                    nt.alignment = TextAlignmentOptions.Right;
                    Fit(nt, 15f);
                }

                group.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
                Debug.Log("[보스 띠] BossGroup 을 HUD 아래 띠로 꽂았다");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>부모 왼쪽 위 기준 자리(px)에 둔다.</summary>
        private static RectTransform Place(Transform parent, string name, Rect r)
        {
            var t = Find(parent, name) as RectTransform;
            if (t == null) { Debug.LogWarning($"[보스 띠] 노드 없음: {name}"); return null; }
            t.anchorMin = t.anchorMax = new Vector2(0f, 1f);
            t.pivot = new Vector2(0f, 1f);
            t.anchoredPosition = new Vector2(r.x, -r.y);
            t.sizeDelta = new Vector2(r.width, r.height);
            t.localScale = Vector3.one;
            return t;
        }

        private static RectTransform CloneText(Transform root, string src, Transform parent, string name)
        {
            var old = Find(parent, name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var s = Find(root, src);
            var go = Object.Instantiate(s.gameObject, parent);
            go.name = name;
            for (int i = go.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(go.transform.GetChild(i).gameObject);
            go.SetActive(true);
            return (RectTransform)go.transform;
        }

        private static void SetImage(Transform t, Sprite s, Image.Type type)
        {
            if (t == null || !t.TryGetComponent<Image>(out var img)) return;
            img.sprite = s;
            img.type = type;
            img.preserveAspect = false;
            img.color = Color.white;
            img.enabled = true;
        }

        /// <summary>3언어가 칸을 넘지 않게 자동 크기 — 최대는 정한 크기, 최소는 충분히 낮게.</summary>
        private static void Fit(TMP_Text t, float max)
        {
            t.enableAutoSizing = true;
            t.fontSizeMax = max;
            t.fontSizeMin = Mathf.Max(6f, max * 0.45f);
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            t.verticalAlignment = VerticalAlignmentOptions.Middle;
        }

        /// <summary>홈 그림을 가로로 늘려도 양 끝 테두리가 안 늘어나게 좌우 9칸 테두리를 준다(단순 표시인 HUD 진행 홈은 영향 없음).</summary>
        private static void SetSideBorder(string path, int px)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti) return;
            var b = new Vector4(px, 0f, px, 0f);
            if (ti.spriteBorder == b) return;
            ti.spriteBorder = b;
            ti.SaveAndReimport();
        }

        private static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = Find(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
