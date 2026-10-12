using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// 챕터·호스트 선택 창 — 카드 줄 맨 왼쪽 「?」 카드 + 아래 「고른 카드 설명 칸」을 꽂는다 (PD 2026-10-12).
    ///
    /// ⚠ `ChapterHostBuilder` 는 다시 돌리지 않는다(빌더에 없는 상성 노드 · 고정 배치가 사라진다).
    ///   이 도구는 **있는 프리팹에 노드를 더하기만** 한다 — 이미 있으면 자리 · 그림만 다시 맞춘다. 여러 번 돌려도 같다.
    ///   `FixedBarsBinder`(위 · 아래 고정 꽂기)를 다시 돌리면 그 뒤에 이것도 한 번 돌린다.
    ///
    /// 자리값은 **화면 px(720 x 1280, 위가 0)** — `FixedBarsBinder` 와 같은 자다.
    /// 설명 칸은 예전 랜덤 칸 자리 그대로(가운데 358,972 · 667 x 155) — 도전하기 버튼 자리를 지킨다.
    /// 시안: `Projects/AVSR/_exchange/in/mock_hostinfo_v6.png` (스탯 줄 · 「5%」 는 뺀다)
    /// </summary>
    public static class ChapterHostInfoBinder
    {
        private const string LobbyPrefab = "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab";
        private const string Ch = "Assets/BaseResource/ChapterHost";
        private const string SkillIconDir = "Assets/BaseResource/HostSelectPanel";
        private const string Portraits = "Assets/BaseResource/Growth/Portraits";
        // 카드 · 육성 화면과 같은 흉상(원작 그림). 작은 아이콘(64px)은 키우면 뭉개져 실루엣이 안 읽혔다
        private const string DeathPortrait = Portraits + "/portrait_death.png";
        private const string AdditiveMat = "Assets/BaseResource/Shaders/UIAdditiveSprite.mat";
        private const string SilhouetteMat = "Assets/BaseResource/Shaders/UISilhouette.mat";

        /// <summary>`UI/Silhouette` 재질 — 없으면 만든다. 그림의 모양만 쓰고 색은 Image.color 로 칠한다.</summary>
        private static Material Silhouette()
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(SilhouetteMat);
            if (m != null) return m;
            var sh = Shader.Find("UI/Silhouette");
            if (sh == null) { Debug.LogError("[챕터·호스트 설명] UI/Silhouette 셰이더 없음"); return null; }
            m = new Material(sh) { name = "UISilhouette" };
            AssetDatabase.CreateAsset(m, SilhouetteMat);
            return m;
        }

        [MenuItem("Tools/Game/챕터·호스트 — 「?」 카드 · 고른 카드 설명 칸 꽂기")]
        public static void Run()
        {
            PillBorder();
            var root = PrefabUtility.LoadPrefabContents(LobbyPrefab);
            try
            {
                var panel = root.transform.Find("ChapterHostPanel");
                var box = panel != null ? panel.Find("CHContent") : null;
                if (box == null) { Debug.LogError("[챕터·호스트 설명] CHContent 없음"); return; }
                var font = box.Find("CHRandomDesc1")?.GetComponent<TMP_Text>();

                // ── 「?」 카드 — v6 시안 구성: 가린 사신(판 코드가 CardThumb 에 색을 입힌다)을 크게, 그 앞 아래에 주사위 + 「?」.
                //   랜덤은 별 · 값이 없다 — 이름을 별 자리로 내리고 그 위를 그림이 채운다
                var card0 = box.Find("CHHostViewport/CHHostContent")?.GetChild(0);
                if (card0 != null)
                {
                    if (card0.Find("CardThumb") is RectTransform th) Local(th, 0, 40, 108, 120);
                    var dice = Img(card0, "RandomDice", Spr(RandomDice));
                    dice.preserveAspect = true;
                    Local(dice.rectTransform, 2, -14, 100, 75);
                    dice.transform.SetSiblingIndex(card0.Find("CardThumb") is Transform th2 ? th2.GetSiblingIndex() + 1 : dice.transform.GetSiblingIndex());
                    if (card0.Find("CardName") is RectTransform nm) Local(nm, 0, -72, 109, 26);
                }

                // ── 설명 칸 — 왼쪽 네모(초상 / 가린 사신 + 주사위). 세로 구분선(배경)은 x 190 언저리
                C(Img(box, "CHInfoPortrait", null).rectTransform, 110, 952, 128, 120);
                var infoDice = Img(box, "CHInfoRandomDice", Spr(RandomDice));
                infoDice.preserveAspect = true;
                C(infoDice.rectTransform, 114, 1010, 104, 78);

                // 랜덤 글자 — 제목 · 「랜덤」 배지 · 설명 2줄을 위로 모은다(아래에 실루엣 줄 자리)
                Move(box, "CHRandomTitle", 227, 912, 340, 30);
                // 「랜덤」 배지는 칸 오른쪽 끝 — 제목 뒤에 붙이면 일본어 · 영어 제목이 길어 가려졌다
                var pill = Img(box, "CHRandomPill", Spr($"{Ch}/ch_pill.png"));
                pill.type = Image.Type.Sliced;
                C(pill.rectTransform, 622, 912, 116, 28);
                var pillDice = Img(pill.transform, "CHRandomPillDice", Spr(RandomDice));
                pillDice.preserveAspect = true;
                Local(pillDice.rectTransform, -32, 0, 30, 22);
                var pillText = Txt(pill.transform, "CHRandomPillText", font, 15f, Color.white, true);
                LocalLeft(pillText.rectTransform, -12, 0, 62, 26);
                Line(pillText, 10f);
                Move(box, "CHRandomDesc1", 227, 943, 455, 26);
                Move(box, "CHRandomDesc2", 227, 967, 455, 26);

                // 실루엣 줄 — 주사위 + 카드와 같은 원작 흉상에 푸른 빛만 입힌 것 + 반짝 별(기존 입자 그림)
                Strip(box, panel);

                // 이름 · 상성 · 설명
                // ⚠ 한 줄 글자 칸은 글꼴 한 줄 높이보다 넉넉히 — 모자라면 말줄임이 글자를 통째로 지운다(2026-10-12 이름이 안 보였다)
                Line(L(Txt(box, "CHInfoName", font, 24f, Color.white, true), 227, 916, 360, 40), 14f);
                C(Img(box, "CHInfoKindIcon", null).rectTransform, 610, 916, 26, 26);
                Line(L(Txt(box, "CHInfoKindText", font, 17f, new Color(0.95f, 0.86f, 0.55f), true), 627, 916, 64, 34), 11f);
                var desc = Txt(box, "CHInfoDesc", font, 15f, new Color(0.80f, 0.86f, 0.95f), false);
                L(desc, 227, 949, 458, 38);
                desc.enableAutoSizing = true;
                desc.fontSizeMin = 11f;
                desc.fontSizeMax = 15f;
                desc.lineSpacing = -8f;

                // 액티브 · 패시브 — 빈 네온 알약 틀(`ch_pill`, 9분할)
                Skill(box, "CHInfoSkillA", 338, 1011, font);
                Skill(box, "CHInfoSkillP", 573, 1011, font);

                Bind(panel);
                PrefabUtility.SaveAsPrefabAsset(root, LobbyPrefab);
                Debug.Log("[챕터·호스트 설명] 꽂음 — 「?」 카드 · 설명 칸");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private const string RandomDice = Ch + "/ch_random_dice.png";
        private const string Fx = "Assets/BaseResource/ParticleFx";

        /// <summary>실루엣 줄에 서는 몸 — 원작 초상 그대로(색만 입힌다). 순서는 시안(유령 · 갱스터 · …).</summary>
        private static readonly string[] StripHosts = { "ghost", "gangster", "thug", "amazon", "hopper" };

        private static void Strip(Transform box, Transform panel)
        {
            var strip = box.Find("CHRandomStrip") as RectTransform;
            if (strip == null)
            {
                var go = new GameObject("CHRandomStrip", typeof(RectTransform));
                go.layer = box.gameObject.layer;
                strip = (RectTransform)go.transform;
                strip.SetParent(box, false);
            }
            C(strip, 445, 1013, 480, 64);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(AdditiveMat);

            // 빛 · 반짝 별은 입자 텍스처(스프라이트가 아니다)를 가산으로 — Image 에 넣으면 빈 네모가 그려졌다(2026-10-12)
            //   주사위 뒤 빛(glow.png)은 뺐다 — 터지는 모양이라 폭발처럼 보였다
            if (strip.Find("StripDiceGlow") is Transform oldGlow) Object.DestroyImmediate(oldGlow.gameObject);
            var dice = Img(strip, "StripDice", Spr(RandomDice));
            dice.preserveAspect = true;
            Local(dice.rectTransform, -200, 0, 78, 58);

            // 푸른 단색 실루엣 — 원작 흉상의 모양만(음영 없이). 색만 곱하면 안쪽 그림이 그대로 남았다(PD 2026-10-12)
            var tint = new Color(0.42f, 0.70f, 1f, 0.95f);
            var sil = Silhouette();
            for (int i = 0; i < StripHosts.Length; i++)
            {
                // 카드에 쓰는 흉상과 같은 그림(판의 _hostArts) — portrait_* 은 전신이라 작게 줄 서 보였다
                var face = Img(strip, $"StripHost{i}", ThumbOf(panel, StripHosts[i]));
                face.preserveAspect = true;
                face.color = tint;
                face.material = sil;
                Local(face.rectTransform, -125 + i * 76, 0, 68, 64);
            }
            var stars = new (float x, float y, float s)[]
            {
                (-250, 22, 18), (-150, 26, 14), (-95, -22, 12), (-15, 28, 16), (60, -24, 12), (135, 26, 16), (205, -18, 14), (232, 24, 12),
            };
            for (int i = 0; i < stars.Length; i++)
            {
                var st = Raw(strip, $"StripStar{i}", Tex($"{Fx}/star4.png"), mat);
                st.color = new Color(1f, 0.95f, 0.70f, 0.85f);
                Local(st.rectTransform, stars[i].x, stars[i].y, stars[i].s, stars[i].s);
            }
        }

        private static Sprite ThumbOf(Transform panel, string key)
        {
            var comp = panel.GetComponent<Game.Module.Lobby.ChapterHostPanel>();
            if (comp == null) return null;
            var arts = new SerializedObject(comp).FindProperty("_hostArts");
            for (int i = 0; i < arts.arraySize; i++)
            {
                var a = arts.GetArrayElementAtIndex(i);
                if (a.FindPropertyRelative("Key").stringValue == key)
                    return a.FindPropertyRelative("Thumb").objectReferenceValue as Sprite;
            }
            return null;
        }

        private static RawImage Raw(Transform parent, string name, Texture tex, Material mat)
        {
            var t = parent.Find(name);
            if (t == null)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.layer = parent.gameObject.layer;
                t = go.transform;
                t.SetParent(parent, false);
            }
            if (t.GetComponent<Image>() is Image old) Object.DestroyImmediate(old);   // 예전에 Image 로 꽂은 것
            var raw = t.GetComponent<RawImage>();
            if (raw == null) raw = t.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.material = mat;
            raw.raycastTarget = false;
            return raw;
        }

        private static Texture Tex(string path) => AssetDatabase.LoadAssetAtPath<Texture>(path);

        /// <summary>있는 글자 칸을 옮긴다(왼쪽 끝 x · 가운데 y, 화면 px).</summary>
        private static void Move(Transform box, string name, float x, float y, float w, float h)
        {
            if (box.Find(name) is RectTransform r && r.TryGetComponent<TMP_Text>(out var t)) L(t, x, y, w, h);
        }

        private static void Skill(Transform box, string name, float x, float y, TMP_Text font)
        {
            var frame = Img(box, name, Spr($"{Ch}/ch_pill.png"));
            frame.type = Image.Type.Sliced;
            C(frame.rectTransform, x, y, 230, 64);
            var icon = Img(frame.transform, $"{name}Icon", null);
            icon.preserveAspect = true;
            Local(icon.rectTransform, -88, 0, 42, 42);
            var title = Txt(frame.transform, $"{name}Name", font, 15f, new Color(1f, 0.85f, 0.32f), true);
            LocalLeft(title.rectTransform, -61, 13, 170, 26);
            Line(title, 10f);
            var text = Txt(frame.transform, $"{name}Desc", font, 12f, new Color(0.82f, 0.88f, 0.96f), false);
            LocalLeft(text.rectTransform, -61, -9, 170, 30);
            text.enableAutoSizing = true;
            text.fontSizeMin = 9f;
            text.fontSizeMax = 12f;
            text.lineSpacing = -10f;
        }

        /// <summary>판 값 — 가린 사신 그림 · 호스트마다 액티브 · 패시브 아이콘(육성 화면과 같은 그림).</summary>
        private static void Bind(Transform panel)
        {
            var comp = panel.GetComponent<Game.Module.Lobby.ChapterHostPanel>();
            if (comp == null) return;
            var so = new SerializedObject(comp);
            so.FindProperty("_randomDeath").objectReferenceValue = Spr(DeathPortrait);
            so.FindProperty("_silhouette").objectReferenceValue = Silhouette();
            var arts = so.FindProperty("_hostArts");
            for (int i = 0; i < arts.arraySize; i++)
            {
                var a = arts.GetArrayElementAtIndex(i);
                string key = a.FindPropertyRelative("Key").stringValue;
                a.FindPropertyRelative("Skill").objectReferenceValue = Spr($"{SkillIconDir}/ultimateicon_{key}.png");
                a.FindPropertyRelative("Passive").objectReferenceValue = Spr($"{SkillIconDir}/passiveicon_{key}.png");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>`ch_pill` 을 9분할로 — 늘려도 둥근 끝 · 테두리 두께가 그대로다.</summary>
        private static void PillBorder()
        {
            string path = $"{Ch}/ch_pill.png";
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti) return;
            var want = new Vector4(30, 22, 30, 22);
            if (ti.spriteBorder == want) return;
            ti.spriteBorder = want;
            ti.SaveAndReimport();
        }

        // ── 도우미 ──────────────────────────────────────────

        private static Image Img(Transform parent, string name, Sprite sprite)
        {
            var t = parent.Find(name);
            if (t == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.layer = parent.gameObject.layer;
                t = go.transform;
                t.SetParent(parent, false);
            }
            var img = t.GetComponent<Image>();
            if (img == null) img = t.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            if (sprite != null) img.sprite = sprite;
            return img;
        }

        private static TMP_Text Txt(Transform parent, string name, TMP_Text like, float size, Color color, bool bold)
        {
            var t = parent.Find(name);
            if (t == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                go.layer = parent.gameObject.layer;
                t = go.transform;
                t.SetParent(parent, false);
            }
            var tmp = t.GetComponent<TMP_Text>();
            if (like != null)
            {
                tmp.font = like.font;
                tmp.fontSharedMaterial = like.fontSharedMaterial;
            }
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            return tmp;
        }

        /// <summary>한 줄 글자 — 줄바꿈 없이, 넘치면 크기를 줄인다(최소 <paramref name="min"/>).</summary>
        private static void Line(TMP_Text t, float min)
        {
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.enableAutoSizing = true;
            t.fontSizeMax = t.fontSize;
            t.fontSizeMin = min;
        }

        /// <summary>가운데 기준(화면 px · 위가 0) — `FixedBarsBinder.C` 와 같은 자.</summary>
        private static void C(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x - 360f, 640f - y);
            r.sizeDelta = new Vector2(w, h);
        }

        /// <summary>왼쪽 끝 기준 글자 칸(화면 px) — 왼쪽 끝 x, 가운데 y.</summary>
        private static TMP_Text L(TMP_Text t, float x, float y, float w, float h)
        {
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(x - 360f, 640f - y);
            r.sizeDelta = new Vector2(w, h);
            return t;
        }

        /// <summary>부모 가운데 기준(위가 +).</summary>
        private static void Local(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
        }

        private static void LocalLeft(RectTransform r, float x, float y, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
        }

        private static Sprite Spr(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
