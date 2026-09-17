using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// 인게임 HUD 퀄업 2차 (2026-09-17, 시안 `ui_new_hud_v2` · 판 `ui_new_hud_v2_parts`).
    ///
    /// 칸마다 테두리를 늘려 붙이던 1차(`InGameHudArtBinder`)와 달리, 이번에는 시안에서
    /// **글자·아이콘·바 색만 뺀 판**을 통째로 받아 잘라 붙인다.
    ///
    ///   위 HUD 전체     → `hudbackdrop_v2`   (판 0~243 을 제 크기로)
    ///   방향 패드 받침  → `hud_dpad_base_v2`
    ///   ACTION 액자     → `hud_action_frame_v2` (버튼 두 칸이 그려져 있다)
    ///   보스 명판       → `hud_boss_plate_v3`   (재발주 — 보스방에서만 켜지는 `BossGroup` 뒤에 깐다)
    ///
    /// 칸 테두리가 판에 이미 그려져 있으므로 **칸마다 붙이던 테두리 그림은 끈다.**
    /// 시안의 칸은 예전 칸보다 조금씩 크고 자리도 몇 픽셀씩 달라서,
    /// 배지 · 바 · 초상 · 글자 자리를 판에 맞춰 옮긴다(좌표는 부모 왼쪽 위 기준).
    ///
    /// ⚠ 바 폭이 바뀌면 `InGameMainUI` 의 바 폭 상수(`GhostBarWidth` 등)도 같이 바꿔야 한다.
    /// </summary>
    public static class InGameHudV2Binder
    {
        private const string Prefab = "Assets/BundleResource/Prefabs/UI/InGame/InGameMainUI.prefab";
        private const string Res = "Assets/BaseResource/InGameMainUI";

        /// <summary>판에 그려져 있어서 끄는 테두리 그림.</summary>
        private static readonly string[] HideImages =
        {
            "PlayerSoulPanel", "RunResourcePanel", "CurrentHostPanel", "ChapterGroup",
            "GoldCell", "GemCell", "GhostLevelBadge", "HostLevelBadge", "HostPortraitFrame",
        };

        /// <summary>
        /// 시안(720×1280) **절대 좌표**를 그대로 적는다. 부모 왼쪽 위(<see cref="ParentOrigin"/>)를 빼서 넣는다.
        /// ⚠ 1차에는 판(높이 243)을 기존 받침 230 에 맞춰 세로로 줄였더니 글자 · 바가 5~10 px 어긋났다.
        ///   판을 제 크기로 깔고 시안 좌표를 그대로 쓴다.
        /// 값은 게임 스샷을 시안과 겹쳐 요소마다 잰 어긋남(에지 맞춤)을 되돌려 다듬었다.
        /// 글자는 픽셀 폰트(`FontPolicy`)라 시안의 고딕보다 넓어서 좌우는 완전히 같지 않다.
        /// </summary>
        private static readonly (string node, float x, float y, float w, float h)[] Rects =
        {
            // PLAYER SOUL 칸 (부모 원점 6,6)
            ("GhostHudIcon", 25f, 25f, 66f, 64f),
            ("PlayerSoulLabel", 98f, 20f, 200f, 18f),
            ("GhostNameText", 96f, 33f, 180f, 32f),
            ("HpLabelGhost", 97f, 67f, 24f, 16f),
            ("GhostHpBarBg", 124f, 69f, 155f, 11f),
            ("GhostHpBarFill", 124f, 69f, 155f, 11f),
            ("GhostHpText", 277f, 67f, 95f, 20f),
            ("GhostSubText", 98f, 88f, 272f, 16f),
            ("GhostLevelBadge", 283f, 23f, 97f, 29f),
            ("LevelText", 279f, 27f, 97f, 25f),
            // RUN RESOURCES 칸 (부모 원점 388,6) — 칸 안 아이콘 · 숫자는 칸을 따라간다
            ("RunResourceLabel", 414f, 20f, 200f, 18f),
            ("GoldCell", 405f, 41f, 110f, 54f),
            ("GemCell", 517f, 42f, 107f, 54f),
            ("GemIcon", 529f, 53f, 28f, 27f),   // 예전 22×30 은 시안 보석보다 작았다
            ("PauseButton", 642f, 12f, 67f, 68f),
            // CURRENT HOST 칸 (부모 원점 6,106)
            ("HostPortraitFrame", 22f, 125f, 82f, 101f),
            ("HostPortraitImage", 24f, 125f, 78f, 90f),
            ("HostLabel", 122f, 129f, 220f, 17f),
            ("HostNameEnText", 119f, 147f, 170f, 32f),
            ("HostNameKrText", 122f, 170f, 150f, 20f),
            ("HpLabelHost", 121f, 198f, 24f, 16f),
            ("HostHpBarBg", 150f, 200f, 129f, 11f),
            ("HostHpBarFill", 150f, 200f, 129f, 11f),
            ("HostHpText", 276f, 197f, 95f, 20f),
            ("HostLevelBadge", 286f, 128f, 94f, 29f),
            ("HostLevelText", 286f, 131f, 94f, 23f),
            // CHAPTER 칸 (부모 원점 388,106)
            ("ChapterLabel", 410f, 123f, 240f, 26f),
            ("ChapterNameText", 413f, 150f, 260f, 22f),
            ("RoomLabel", 414f, 187f, 60f, 22f),
            ("RoomNumberText", 472f, 171f, 64f, 36f),
            ("RoomTotalText", 534f, 183f, 90f, 22f),
            ("RoomProgressBg", 413f, 215f, 219f, 11f),
            ("RoomProgressFill", 413f, 215f, 219f, 11f),
            // 아래 조작 (부모 원점 0,1050)
            ("ActionFrame", 476f, 1087f, 244f, 157f),
            ("DPadBase", 5f, 1098f, 150f, 134f),
            ("FreeMoveLabel", 20f, 1232f, 200f, 18f),
            ("ActionLabel", 486f, 1070f, 216f, 18f),
        };

        /// <summary>노드의 부모가 시안 좌표에서 어디서 시작하는가. 표에 없는 부모는 (0,0).</summary>
        private static readonly (string parent, float x, float y)[] ParentOrigin =
        {
            ("PlayerSoulPanel", 6f, 6f),
            ("RunResourcePanel", 388f, 6f),
            ("GemCell", 517f, 42f),
            ("CurrentHostPanel", 6f, 106f),
            ("ChapterGroup", 388f, 106f),
            ("GhostHpBarBg", 124f, 69f),
            ("HostHpBarBg", 150f, 200f),
            ("RoomProgressBg", 413f, 215f),
            ("ControlGroup", 0f, 1050f),
        };

        [MenuItem("Tools/Game/인게임 HUD 퀄업 2차 판 꽂기")]
        public static void Run()
        {
            foreach (var f in new[] { "hudbackdrop_v2", "hud_dpad_base_v2", "hud_action_frame_v2", "hud_boss_plate_v3" })
                EnsureSprite($"{Res}/{f}.png");

            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                SetSprite(root, "HudBackdrop", "hudbackdrop_v2");
                SetSprite(root, "DPadBase", "hud_dpad_base_v2");
                SetSprite(root, "ActionFrame", "hud_action_frame_v2");

                foreach (var n in HideImages)
                {
                    var t = Find(root.transform, n);
                    if (t != null && t.TryGetComponent<Image>(out var img)) img.enabled = false;
                }

                // 일시정지 그림은 판에 있다. 버튼은 눌려야 하므로 그림만 투명하게 둔다.
                var pause = Find(root.transform, "PauseButton");
                if (pause != null && pause.TryGetComponent<Image>(out var pi)) pi.color = new Color(1f, 1f, 1f, 0f);

                // 받침 판은 제 높이(243)로 깐다 — 줄이면 칸 안 글자와 어긋난다.
                // ⚠ 묶음(`TopHudGroup`)도 243 으로 늘린다. 묶음이 230 이면 `ScreenFit` 이 받침을
                //   「묶음을 꽉 채우는 판」으로 보고 런타임에 230 으로 다시 눌렀다(실측 2026-09-17).
                if (Find(root.transform, "TopHudGroup") is RectTransform topGroup)
                    topGroup.sizeDelta = new Vector2(topGroup.sizeDelta.x, 243f);
                Place(root, "HudBackdrop", 0f, 0f, 720f, 243f);
                foreach (var (node, x, y, w, h) in Rects) PlaceAbs(root, node, x, y, w, h);

                // 방향 패드 손잡이 — 받침 그림의 가운데 원(받침 안 75,67)에 맞춘다
                var knob = Find(root.transform, "DPadKnob") as RectTransform;
                if (knob != null)
                {
                    knob.anchorMin = knob.anchorMax = new Vector2(0f, 1f);
                    knob.anchoredPosition = new Vector2(75f - 23f + knob.pivot.x * 46f,
                                                        -(67f - 23f + (1f - knob.pivot.y) * 46f));
                }

                // 버튼 두 칸 — 줄 맞춤을 끄고 **자리를 고정**한다. 액자에 칸이 그려져 있어서,
                // 몸이 없어 스킬 버튼이 꺼져도 빙의 버튼이 가운데로 미끄러지면 칸과 어긋난다.
                var row = Find(root.transform, "ButtonRow");
                if (row != null && row.TryGetComponent<HorizontalLayoutGroup>(out var hl)) hl.enabled = false;
                // 액자 안 두 칸(시안 503~600 · 607~707, 위 1107) 가운데에 버튼 100×112
                Place(root, "SkillButton", 3f, 9f, 100f, 112f);
                Place(root, "PossessButton", 108f, 9f, 100f, 112f);
                // 버튼 이름표는 칸 아래쪽 띠에 (시안 실측 — 버튼 위에서 99)
                Place(root, "SkillButtonLabel", 0f, 99f, 100f, 18f);
                Place(root, "PossessButtonLabel", 0f, 99f, 100f, 18f);

                // 설명 한 줄이 칸 오른쪽 테두리 밖으로 넘쳤다 — 칸 안에서 글자 크기를 줄여 맞춘다
                if (Find(root.transform, "GhostSubText") is Transform sub
                    && sub.TryGetComponent<TMPro.TMP_Text>(out var subText))
                {
                    subText.enableAutoSizing = true;
                    subText.fontSizeMax = subText.fontSize;
                    subText.fontSizeMin = 7f;
                    subText.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                }

                // HP 숫자는 칸 오른쪽 끝(371)에 붙인다. 픽셀 폰트가 시안 글씨보다 넓어서
                // 왼쪽 정렬로 두면 숫자 머리가 바 끝을 덮었다.
                foreach (var n in new[] { "GhostHpText", "HostHpText" })
                    if (Find(root.transform, n) is Transform tt && tt.TryGetComponent<TMPro.TMP_Text>(out var tmp))
                    {
                        tmp.alignment = TMPro.TextAlignmentOptions.Right;
                        tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                    }

                // 보석 칸을 키웠으니 그림 비율은 지킨다(원본 22×30)
                if (Find(root.transform, "GemIcon") is Transform gem && gem.TryGetComponent<Image>(out var gemImg))
                    gemImg.preserveAspect = true;

                BindBossPlate(root);

                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            Debug.Log("[HudV2] 판 꽂기 끝");
        }

        /// <summary>
        /// 보스 명판. `BossGroup` 은 가로로 늘어나는 줄이라 **가운데 기준**으로 놓는다.
        /// 명판 720×110 (v3 재발주 — v2 는 이름 · 숫자 칸에 테두리가 없어 검은 네모로 튀어나왔다).
        /// </summary>
        private const float BossPlateDrop = 14f;   // BossGroup 윗변 236 → 명판 윗변 250

        private static void BindBossPlate(GameObject root)
        {
            var group = Find(root.transform, "BossGroup") as RectTransform;
            if (group == null) return;
            group.sizeDelta = new Vector2(group.sizeDelta.x, BossPlateDrop + 110f);

            var plateT = group.Find("BossPlate");
            if (plateT == null)
            {
                var go = new GameObject("BossPlate", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(group, false);
                plateT = go.transform;
            }
            plateT.SetAsFirstSibling();   // 이름 · 바 · 숫자보다 뒤에
            var plate = (RectTransform)plateT;
            plate.anchorMin = plate.anchorMax = new Vector2(0.5f, 1f);
            plate.pivot = new Vector2(0.5f, 1f);
            // HUD 받침이 243 으로 길어졌다 — 명판을 그 아래로 내린다(보스방 시안의 명판 위치)
            plate.anchoredPosition = new Vector2(0f, -BossPlateDrop);
            plate.sizeDelta = new Vector2(720f, 110f);
            var pimg = plateT.GetComponent<Image>();
            pimg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Res}/hud_boss_plate_v3.png");
            pimg.raycastTarget = false;

            if (group.Find("BossHpBarBg") is RectTransform bar)
            {
                bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 1f);
                bar.pivot = new Vector2(0.5f, 1f);
                bar.anchoredPosition = new Vector2(-1f, -44f - BossPlateDrop);
                bar.sizeDelta = new Vector2(594f, 24f);
                if (bar.Find("BossHpBarFill") is RectTransform fill) fill.sizeDelta = new Vector2(594f, 24f);
            }
            if (group.Find("BossLabel") is RectTransform label)
            {
                label.anchoredPosition = new Vector2(0f, -6f - BossPlateDrop);
                label.sizeDelta = new Vector2(label.sizeDelta.x, 24f);
            }
            if (group.Find("BossHpText") is RectTransform hp)
            {
                hp.anchoredPosition = new Vector2(0f, -80f - BossPlateDrop);
                hp.sizeDelta = new Vector2(hp.sizeDelta.x, 24f);
            }
        }

        private static void SetSprite(GameObject root, string node, string file)
        {
            var t = Find(root.transform, node);
            if (t == null || !t.TryGetComponent<Image>(out var img)) return;
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Res}/{file}.png");
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.color = Color.white;
            img.enabled = true;
        }

        /// <summary>
        /// 부모 왼쪽 위 기준 자리 · 크기. 앵커는 왼쪽 위로 맞추고 **기준점(pivot)은 그대로 둔다** —
        /// 방향 패드처럼 코드가 기준점을 전제로 옮기는 노드가 있다.
        /// </summary>
        private static void Place(GameObject root, string node, float x, float y, float w, float h)
        {
            if (Find(root.transform, node) is not RectTransform rt)
            {
                Debug.LogWarning($"[HudV2] 노드 없음: {node}");
                return;
            }
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x + rt.pivot.x * w, -(y + (1f - rt.pivot.y) * h));
        }

        /// <summary>시안 절대 좌표로 놓는다 — 부모 원점을 빼서 <see cref="Place"/> 에 넘긴다.</summary>
        private static void PlaceAbs(GameObject root, string node, float x, float y, float w, float h)
        {
            var t = Find(root.transform, node);
            if (t == null) { Debug.LogWarning($"[HudV2] 노드 없음: {node}"); return; }
            float ox = 0f, oy = 0f;
            foreach (var (parent, px, py) in ParentOrigin)
                if (t.parent != null && t.parent.name == parent) { ox = px; oy = py; break; }
            Place(root, node, x - ox, y - oy, w, h);
        }

        private static void EnsureSprite(string path)
        {
            AssetDatabase.ImportAsset(path);
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti) return;
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
