using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// 인게임 HUD 3차 — D안(2026-10-06 확정, 시안 `_exchange/ref/ingame_ui/hud_draft_D2*.png`).
    ///
    /// 금속 틀을 벗고 로비와 한 벌(남색 · 하늘색 네온 · 둥근 알약). 코덱스가 그린 «빈 판»
    /// (`hud3_clean_host/ghost.png`)을 `Projects/AVSR/Tools/hud3_cut.py` 가 부품으로 오려
    /// `Assets/BaseResource/InGameMainUI/hud3/` 에 둔다. 이 도구는 그 부품을 깔고, 글자 · 막대 · 얼굴을
    /// **시안과 같은 자리**에 옮긴다.
    ///
    /// 좌표는 전부 **시안 1080 x 1920 기준**으로 적고 <see cref="K"/>(720/1080)를 곱해 넣는다 —
    /// 시안 위에서 잰 숫자를 그대로 옮겨 적기 위해서다.
    ///
    /// ⚠ 바 폭이 바뀌면 `InGameMainUI` 의 `GhostBarWidth` · `HostBarWidth` · `RoomProgressWidth` 도 같이.
    /// </summary>
    public static class InGameHudV3Binder
    {
        private const string Prefab = "Assets/BundleResource/Prefabs/UI/InGame/InGameMainUI.prefab";
        private const string Dir = "Assets/BaseResource/InGameMainUI/hud3";
        private const float K = 720f / 1080f;
        private const string PixelFont = "Assets/BaseResource/Fonts/OriginalPixel SDF.asset";

        // ── 판 · 덧판 (시안 좌표 x, y, w, h — hud3_rects.json 에서) ──────────────
        private static readonly (string node, string sprite, float x, float y, float w, float h)[] Plates =
        {
            ("Hud3GhostPill", "hud3_ghost_pill", 16, 38, 434, 155),
            ("Hud3HostPill", "hud3_host_pill", 630, 37, 435, 157),
            ("Hud3StageTrack", "hud3_stage_track", 458, 153, 166, 36),
            ("Hud3GoldChip", "hud3_gold_chip", 28, 211, 197, 85),
            // 상성 링 — 몸 알약 **안**, 얼굴 오른쪽 아래에 배지로(PD 2026-10-08 「UI 를 벗어나 오른쪽 밑에 있다, 작게 해서 안에」).
            //   예전 자리(907, 213, 144)는 알약 밖 아래였다. 크기 144 → 84
            ("Hud3AffRing", "hud3_aff_ring", 980, 114, 84, 84),
            // 유령 상태에서만 켠다(`InGameMainUI.ShowNoHost`) — 판의 얼굴 테두리 · 막대 홈 위를 덮는다
            ("Hud3HostEmptyFace", "hud3_host_empty_face", 937, 71, 103, 103),
            ("Hud3HostEmptyBar", "hud3_host_empty_bar", 663, 139, 264, 44),
        };

        /// <summary>판에 그려져 있어서 그림을 끄는 노드(자리 · 자식은 그대로 쓴다).</summary>
        private static readonly string[] HideImages =
        {
            "HudBackdrop", "PlayerSoulPanel", "RunResourcePanel", "CurrentHostPanel", "ChapterGroup",
            "GoldCell", "GhostLevelBadge", "HostLevelBadge", "HostPortraitFrame", "ActionFrame",
        };

        /// <summary>D안에 없는 것 — 다이아(런 중에 못 얻는다) · 칸 이름 · 설명 한 줄 · 액자 글자.</summary>
        private static readonly string[] Off =
        {
            "GemCell", "RunResourceLabel", "GhostSubText", "FreeMoveLabel", "ActionLabel", "HostAffinityIcon",
        };

        // ── 글자 · 막대 · 얼굴 (시안 좌표) ───────────────
        // ⚠ 원작 픽셀 글꼴은 시안 속 글자보다 옆으로 넓다(한 글자 ≈ 글자 크기 1.0 배). 시안 글자 폭에 칸을 맞추면
        //   「PLAYER SOUL」이 6px 로 줄어 안 읽혔다(2026-10-06 실측). 글자 칸은 옆의 빈자리(배지 · 얼굴 테두리 앞)까지 넓혔다.
        private static readonly (string node, float x, float y, float w, float h)[] Rects =
        {
            // ⚠ 막대 · HP 숫자는 **판의 홈을 픽셀로 잰 값**이다(hud3_clean_host.png : 유령 · 몸 홈 테두리 y 142 · 161,
            //   방 진행 홈 y 160 · 181). 채움은 홈 안쪽, 숫자는 홈 아래 — 시안 눈대중으로 놓았더니 채움이 홈 위로 뜨고
            //   숫자가 홈 테두리를 덮었다(2026-10-06).
            // 유령 알약
            ("GhostHudIcon", 40, 70, 92, 90),
            ("PlayerSoulLabel", 158, 52, 160, 28),
            ("GhostNameText", 158, 82, 160, 38),
            ("LevelText", 343, 80, 69, 25),
            ("HpLabelGhost", 164, 143, 36, 18),
            ("GhostHpBarBg", 202, 145, 209, 14),
            ("GhostHpBarFill", 202, 145, 209, 14),
            ("GhostHpText", 300, 163, 110, 20),
            // 몸 알약
            ("HostPortraitImage", 942, 64, 102, 104),
            ("HostLabel", 760, 52, 158, 28),
            ("HostNameEnText", 760, 82, 158, 32),
            ("HostNameKrText", 760, 114, 158, 26),
            ("HostLevelText", 668, 81, 69, 25),
            ("HpLabelHost", 668, 143, 36, 18),
            ("HostHpBarBg", 708, 145, 208, 14),
            ("HostHpBarFill", 708, 145, 208, 14),
            ("HostHpText", 800, 163, 112, 20),
            // 가운데 스테이지
            ("ChapterLabel", 464, 52, 150, 26),
            ("ChapterNameText", 464, 86, 150, 30),
            ("RoomLabel", 464, 124, 58, 24),
            ("RoomNumberText", 524, 118, 44, 32),
            ("RoomTotalText", 569, 124, 45, 24),
            ("RoomProgressBg", 467, 163, 146, 16),
            ("RoomProgressFill", 467, 163, 146, 16),
            // 금화 · 일시정지 · 상성
            ("GoldIcon", 55, 228, 50, 50),
            ("GoldText", 112, 226, 93, 52),
            ("PauseButton", 234, 204, 97, 98),
            ("AffinityTriangle", 989, 124, 66, 64),   // 링 안 — 링과 같은 비율로 줄였다
            // 아래 조작 — 묶음(ButtonRow) 기준이 아니라 화면 기준으로 적는다(부모 원점을 빼서 넣는다)
            ("DPadBase", 9, 1611, 260, 255),
            ("ButtonRow", 681, 1630, 386, 235),
            ("SkillButton", 681, 1630, 196, 235),
            ("SkillIcon", 712, 1660, 134, 134),
            ("SkillCooldown", 705, 1655, 148, 146),
            ("SkillSealIcon", 747, 1695, 64, 64),
            ("SkillButtonLabel", 714, 1817, 134, 40),
            ("PossessButton", 877, 1630, 190, 235),
            ("PossessGhostIcon", 920, 1672, 104, 100),
            ("PossessCooldown", 900, 1655, 145, 146),
            ("PossessCooldownText", 877, 1690, 190, 70),
            ("PossessCostText", 900, 1762, 145, 40),
            ("PossessButtonLabel", 902, 1817, 140, 40),
            ("PossessButtonGlow", 857, 1610, 230, 275),
        };

        private static readonly Color Yellow = new(1f, 0.82f, 0.24f);
        private static readonly Color Cyan = new(0.28f, 0.88f, 1f);
        private static readonly Color LightCyan = new(0.55f, 0.85f, 1f);
        private static readonly Color Red = new(1f, 0.25f, 0.25f);
        private static readonly Color Purple = new(0.72f, 0.25f, 1f);

        /// <summary>글자 — 크기(720 기준) · 정렬 · 색. 크기는 시안 글자 높이에서 잰 값.</summary>
        private static readonly (string node, float size, TextAlignmentOptions align, Color color)[] Texts =
        {
            ("PlayerSoulLabel", 13f, TextAlignmentOptions.Left, Yellow),
            ("GhostNameText", 23f, TextAlignmentOptions.Left, Color.white),
            ("LevelText", 18f, TextAlignmentOptions.Center, Yellow),
            ("HpLabelGhost", 15f, TextAlignmentOptions.Left, Red),
            ("GhostHpText", 13f, TextAlignmentOptions.Right, Color.white),
            ("HostLabel", 13f, TextAlignmentOptions.Left, Cyan),
            ("HostNameEnText", 23f, TextAlignmentOptions.Left, Color.white),
            ("HostNameKrText", 15f, TextAlignmentOptions.Left, LightCyan),
            ("HostLevelText", 18f, TextAlignmentOptions.Center, Yellow),
            ("HpLabelHost", 15f, TextAlignmentOptions.Left, Purple),
            ("HostHpText", 13f, TextAlignmentOptions.Right, Color.white),
            ("ChapterLabel", 15f, TextAlignmentOptions.Left, Cyan),
            ("ChapterNameText", 16f, TextAlignmentOptions.Left, Color.white),
            ("RoomLabel", 12f, TextAlignmentOptions.Left, LightCyan),
            ("RoomNumberText", 19f, TextAlignmentOptions.Left, Color.white),
            ("RoomTotalText", 13f, TextAlignmentOptions.Left, LightCyan),
            ("GoldText", 30f, TextAlignmentOptions.Left, Color.white),
            ("SkillButtonLabel", 15f, TextAlignmentOptions.Center, Yellow),
            ("PossessButtonLabel", 15f, TextAlignmentOptions.Center, Color.white),
            ("PossessCostText", 24f, TextAlignmentOptions.Center, LightCyan),
        };

        /// <summary>좁은 칸에 긴 영문이 드는 작은 이름표 — 더 좁혀야 읽히는 크기가 나온다.</summary>
        private static readonly HashSet<string> TightLabels = new()
        {
            "PlayerSoulLabel", "HostLabel", "RoomLabel", "RoomTotalText", "HpLabelGhost", "HpLabelHost",
        };

        [MenuItem("Tools/Game/인게임 HUD 3차(D안) 꽂기")]
        public static void Run()
        {
            var sprites = new Dictionary<string, Sprite>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Dir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                EnsureSprite(path);
                sprites[System.IO.Path.GetFileNameWithoutExtension(path)] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var top = Find(root.transform, "TopHudGroup") as RectTransform;

                // 줄 맞춤을 끈다 — 자리는 시안 좌표로 고정한다. 칸이 비어도 남은 것이 미끄러지면 판과 어긋난다.
                foreach (var row in new[] { "HudRow2", "ButtonRow" })
                    if (Find(root.transform, row) is Transform r && r.TryGetComponent<HorizontalLayoutGroup>(out var hl))
                        hl.enabled = false;
                // 두 번째 줄 묶음을 HUD 맨 위로 — 그래야 그 안의 칸도 시안 절대 좌표로 놓인다
                if (Find(root.transform, "HudRow2") is RectTransform row2)
                {
                    row2.anchoredPosition = new Vector2(row2.anchoredPosition.x, 0f);
                    row2.sizeDelta = new Vector2(row2.sizeDelta.x, top != null ? top.sizeDelta.y : 243f);
                }

                foreach (var n in HideImages)
                    if (Find(root.transform, n) is Transform t)
                        foreach (var img in t.GetComponents<Image>()) img.enabled = false;
                // 칸 테두리 그림의 안쪽 조각(Inner · Accent)도 끈다
                foreach (var n in new[] { "PlayerSoulPanel", "RunResourcePanel", "CurrentHostPanel", "ChapterGroup",
                                          "GhostLevelBadge", "HostLevelBadge", "GoldCell", "RoomProgressBg" })
                    if (Find(root.transform, n) is Transform t)
                        for (int i = 0; i < t.childCount; i++)
                        {
                            var c = t.GetChild(i);
                            if ((c.name == "Inner" || c.name == "Accent") && c.TryGetComponent<Image>(out var ci)) ci.enabled = false;
                        }
                foreach (var n in Off)
                    if (Find(root.transform, n) is Transform t) t.gameObject.SetActive(false);

                // 판과 덧판 — HUD 묶음 맨 뒤에 깐다(칸 안 글자 · 막대보다 아래)
                int sibling = 0;
                foreach (var (node, spr, x, y, w, h) in Plates)
                {
                    var t = top.Find(node);
                    if (t == null)
                    {
                        var go = new GameObject(node, typeof(RectTransform), typeof(Image));
                        go.transform.SetParent(top, false);
                        t = go.transform;
                    }
                    t.SetSiblingIndex(sibling++);
                    var img = t.GetComponent<Image>();
                    img.sprite = sprites.TryGetValue(spr, out var s) ? s : null;
                    img.raycastTarget = false;
                    img.type = Image.Type.Simple;
                    img.preserveAspect = false;
                    img.color = Color.white;
                    PlaceAbs((RectTransform)t, x, y, w, h);
                }
                // HUD 뒤 배경 띠(밤 도시 그림)는 쓰지 않는다 — 챕터와 상관없이 같은 그림이 HUD 뒤를 덮었다.
                // HUD 뒤는 뚫려 인게임 배경이 보이고 그 위에 판이 얹힌 느낌이 기획이다(PD 2026-10-08).
                // 예전에 깐 띠가 프리팹에 남아 있으면 지운다.
                if (root.transform.Find("Hud3TopBand") is Transform oldBand) Object.DestroyImmediate(oldBand.gameObject);
                // 덧판은 몸이 없을 때만 — 기본은 끈다. 판은 유령으로 시작하므로 코드가 바로 켠다.
                top.Find("Hud3HostEmptyFace").gameObject.SetActive(false);
                top.Find("Hud3HostEmptyBar").gameObject.SetActive(false);

                // 그림 바꿔 끼우기
                SetSprite(root, "PauseButton", sprites, "hud3_pause");
                SetSprite(root, "DPadBase", sprites, "hud3_pad_base");
                SetSprite(root, "DPadKnob", sprites, "hud3_pad_knob");
                SetSprite(root, "SkillButton", sprites, "hud3_skill_frame");
                SetSprite(root, "PossessButton", sprites, "hud3_possess_frame");
                SetSprite(root, "GhostHpBarFill", sprites, "hud3_fill_red");
                SetSprite(root, "HostHpBarFill", sprites, "hud3_fill_purple");
                SetSprite(root, "RoomProgressFill", sprites, "hud3_fill_cyan");
                // 막대 홈은 판에 그려져 있다 — 채움의 부모(Bg)는 투명하게 두고 자리만 쓴다
                foreach (var n in new[] { "GhostHpBarBg", "HostHpBarBg", "RoomProgressBg" })
                    if (Find(root.transform, n) is Transform t && t.TryGetComponent<Image>(out var bi))
                        bi.color = new Color(1f, 1f, 1f, 0f);

                foreach (var (node, x, y, w, h) in Rects)
                {
                    if (Find(root.transform, node) is RectTransform rt) PlaceAbs(rt, x, y, w, h);
                    else Debug.LogWarning($"[HudV3] 노드 없음: {node}");
                }
                // 조이스틱 손잡이 — 받침 가운데에(받침 안 좌표)
                if (Find(root.transform, "DPadKnob") is RectTransform knob)
                {
                    knob.anchorMin = knob.anchorMax = new Vector2(0f, 1f);
                    float kw = 96f * K, bw = 260f * K, bh = 255f * K;
                    knob.sizeDelta = new Vector2(kw, kw);
                    knob.anchoredPosition = new Vector2(bw / 2f - kw / 2f + knob.pivot.x * kw,
                                                        -(bh / 2f - kw / 2f + (1f - knob.pivot.y) * kw));
                }

                // 영문 · 숫자는 원작 픽셀 글꼴(시안 확정). 한글 칸(무대 이름 · 몸 현지 이름)은 고딕 그대로.
                var pixel = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFont);
                foreach (var (node, size, align, color) in Texts)
                {
                    if (Find(root.transform, node) is not Transform t || !t.TryGetComponent<TMP_Text>(out var tmp)) continue;
                    if (pixel != null && node != "ChapterNameText" && node != "HostNameKrText")
                    {
                        tmp.font = pixel;
                        tmp.fontSharedMaterial = pixel.material;
                        tmp.fontStyle = FontStyles.Normal;
                        // 원작 픽셀 글꼴은 글자 사이가 넓다 — 조금 좁혀 같은 칸에서 글자를 키운다(시안의 글자 간격에 가깝게)
                        tmp.characterSpacing = TightLabels.Contains(node) ? -22f : -12f;
                    }
                    // 픽셀 글꼴은 고정폭이라 시안 고딕보다 넓다 — 칸 폭에 맞춰 줄어들게 한다(최대는 시안 크기)
                    tmp.enableAutoSizing = true;
                    tmp.fontSizeMax = size;
                    tmp.fontSizeMin = 7f;   // 7 아래는 실기에서 안 읽힌다 — 그 위로는 칸에 맞춰 얼마든지 줄어든다
                    tmp.fontSize = size;
                    tmp.alignment = align switch
                    {
                        TextAlignmentOptions.Left => TextAlignmentOptions.MidlineLeft,
                        TextAlignmentOptions.Right => TextAlignmentOptions.MidlineRight,
                        _ => TextAlignmentOptions.Midline,
                    };
                    tmp.color = color;
                    tmp.textWrappingMode = TextWrappingModes.NoWrap;
                    // 가장 작게 줄여도 넘치면 칸 밖으로 삐져나가지 말고 말줄임(…)으로 자른다(PD 2026-10-06 :
                    // 「폰트들이 UI 를 삐져나간 게 너무 많다」). 긴 이름(일본어 몸 이름 등)도 칸 안에서 끝난다.
                    // ⚠ 말줄임(Ellipsis)은 쓰지 않는다 — 픽셀 글꼴에 「…」 가 없어서, 넘치는 순간 글자가 **통째로 사라졌다**
                    //   (금화 「99,999」 실측). 칸 끝에서 자른다.
                    tmp.overflowMode = TextOverflowModes.Truncate;
                    tmp.margin = Vector4.zero;
                }

                foreach (var n in new[] { "GhostHudIcon", "HostPortraitImage", "PossessGhostIcon", "AffinityTriangle",
                                          "SkillIcon", "GoldIcon" })
                    if (Find(root.transform, n) is Transform t && t.TryGetComponent<Image>(out var pi)) pi.preserveAspect = true;

                // 스킬 버튼 틀 두 장을 화면 코드에 넘긴다(몸 있음 / 비움)
                var ui = root.GetComponentInChildren<Game.Module.InGame.InGameMainUI>(true);
                if (ui != null)
                {
                    var so = new SerializedObject(ui);
                    so.FindProperty("_skillFrame").objectReferenceValue = sprites.GetValueOrDefault("hud3_skill_frame");
                    so.FindProperty("_skillFrameEmpty").objectReferenceValue = sprites.GetValueOrDefault("hud3_skill_empty");
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            Debug.Log("[HudV3] D안 꽂기 끝");
        }

        /// <summary>
        /// 시안 절대 좌표(1080 기준)로 놓는다. 부모의 화면 속 왼쪽 위를 거슬러 올라가 빼서 넣는다.
        /// 기준점(pivot)은 건드리지 않는다 — 조이스틱처럼 코드가 기준점을 전제로 움직이는 노드가 있다.
        /// </summary>
        private static void PlaceAbs(RectTransform rt, float x, float y, float w, float h)
        {
            var origin = AbsTopLeft(rt.parent as RectTransform);
            float px = x * K - origin.x, py = y * K - origin.y, pw = w * K, ph = h * K;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(pw, ph);
            rt.anchoredPosition = new Vector2(px + rt.pivot.x * pw, -(py + (1f - rt.pivot.y) * ph));
        }

        /// <summary>
        /// 화면(720 x 1280 기준) 속 왼쪽 위. HUD 묶음(`TopHudGroup`)은 화면 맨 위 · 가운데 720 칸이고,
        /// 조작 묶음(`ControlGroup`)은 위에서 1050 이다. 그 아래는 부모 기준 왼쪽 위 앵커를 따라 더한다.
        /// </summary>
        private static Vector2 AbsTopLeft(RectTransform t)
        {
            if (t == null) return Vector2.zero;
            if (t.name == "TopHudGroup") return Vector2.zero;
            if (t.name == "ControlGroup") return new Vector2(0f, -t.anchoredPosition.y);
            var parent = AbsTopLeft(t.parent as RectTransform);
            float parentW = t.parent is RectTransform pr ? pr.rect.width : 720f;
            float left = t.anchorMin.x * parentW + t.offsetMin.x;
            float topY = -t.offsetMax.y;   // anchorMax.y 가 1 인 노드만 다룬다(이 HUD 는 전부 그렇다)
            return new Vector2(parent.x + left, parent.y + topY);
        }

        private static void SetSprite(GameObject root, string node, Dictionary<string, Sprite> sprites, string file)
        {
            if (Find(root.transform, node) is not Transform t || !t.TryGetComponent<Image>(out var img)) return;
            if (!sprites.TryGetValue(file, out var s) || s == null) { Debug.LogWarning($"[HudV3] 그림 없음: {file}"); return; }
            img.sprite = s;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.color = Color.white;
            img.enabled = true;
        }

        private static void EnsureSprite(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti) return;
            if (ti.textureType == TextureImporterType.Sprite && !ti.mipmapEnabled
                && ti.textureCompression == TextureImporterCompression.Uncompressed) return;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }

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
