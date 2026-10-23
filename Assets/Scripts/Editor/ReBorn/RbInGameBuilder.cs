// RE:BORN UI 리뉴얼(2026-10-23) — 인게임 HUD · 조작부 · 레벨업 카드(시안 Screens/fight.png · levelup.png).
// 인게임 화면 스크립트(InGameMainUI, 3천 줄)는 노드 이름으로 묶여 있다 — 옛 프리팹을 새 경로로 복사해 **같은 이름 그대로**
// 그림 · 자리만 바꾼다(이름 = 바인딩 키). 싸움판(RoomField 720×800)은 그대로 — 판 크기는 방 배치가 정한다.
// 상점 · 악마 · 제단 · 결과 창은 다음 차례(각자 시안). 화면 스크립트에는 `_reborn` 을 켠다.
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Game.Editor.ReBorn.RbBuild;
using TA = TMPro.TextAlignmentOptions;

namespace Game.Editor.ReBorn
{
    public static class RbInGameBuilder
    {
        private const string OldPath = "Assets/BundleResource/Prefabs/UI/InGame/InGameMainUI.prefab";
        public const string PrefabPath = "Assets/BundleResource/Prefabs/UI/InGame/RbInGameMainUI.prefab";
        /// <summary>전투 시안은 맨 위 띠(예시 표 · 노치)를 뺀 110 줄부터 HUD 가 선다.</summary>
        private const float HudTop = 110f;

        [MenuItem("Tools/Game/RE:BORN 화면/인게임 세우기")]
        public static void Build()
        {
            var root = PrefabUtility.LoadPrefabContents(OldPath);
            try
            {
                root.name = "RbInGameMainUI";
                BuildHud(root.transform);
                BuildControls(root.transform);
                BuildLevelUp(root.transform);
                Wire(root.GetComponent<Game.Module.InGame.InGameMainUI>());
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Register(PrefabPath, "UI/InGame/RbInGameMainUI", "label_ingame");
            Debug.Log($"[RE:BORN 화면] 인게임 → {PrefabPath}");
        }

        // ── 도구 ────────────────────────────────────────────

        private static RectTransform F(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t as RectTransform;
            Debug.LogWarning($"[RE:BORN 인게임] 노드 없음: {name}");
            return null;
        }

        /// <summary>부모를 옮기고 자리를 잡는다 — 부모(묶음)는 위 가운데 기준, 좌표는 시안 1080(위 기준 줄 top).</summary>
        private static RectTransform Put(RectTransform rt, RectTransform parent, float top, float x0, float y0, float x1, float y1)
        {
            if (rt == null) return null;
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2((x1 - x0) * S, (y1 - y0) * S);
            rt.anchoredPosition = new Vector2(((x0 + x1) * 0.5f - 540f) * S, -((y0 + y1) * 0.5f - top) * S);
            return rt;
        }

        /// <summary>게임 px 로 바로(묶음 위 왼쪽 기준 중심 cx · cy).</summary>
        private static RectTransform PutPx(RectTransform rt, RectTransform parent, float cx, float cy, float w, float h)
        {
            if (rt == null) return null;
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(cx, -cy);
            return rt;
        }

        /// <summary>막대 채움 — 홈(부모) 왼쪽에 붙어 폭만 바뀐다(InGameMainUI SetFill 은 sizeDelta.x 만 쓴다).</summary>
        private static void FillLeft(RectTransform fill)
        {
            if (fill == null) return;
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(0f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.anchoredPosition = Vector2.zero;
            var parent = (RectTransform)fill.parent;
            fill.sizeDelta = new Vector2(parent.sizeDelta.x, 0f);
        }

        private static void Skin(RectTransform rt, string path, bool sliced = true)
        {
            if (rt == null) return;
            var img = rt.GetComponent<Image>() ?? rt.gameObject.AddComponent<Image>();
            img.sprite = KitSprite(path);
            img.color = Color.white;
            img.enabled = true;
            img.type = sliced && img.sprite != null && img.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.preserveAspect = img.type == Image.Type.Simple;
        }

        /// <summary>그림만 끈다 — 화면 스크립트가 SetActive(true) 로 켜도 아무것도 안 그려진다.</summary>
        private static void Mute(Transform root, params string[] names)
        {
            foreach (var n in names)
            {
                var t = F(root, n);
                if (t == null) continue;
                foreach (var g in t.GetComponents<Graphic>()) g.enabled = false;
            }
        }

        private static void Off(Transform root, params string[] names)
        {
            foreach (var n in names) { var t = F(root, n); if (t != null) t.gameObject.SetActive(false); }
        }

        private static void Style(RectTransform rt, float size1080, Color color, TA align, bool heavy = false, bool number = false)
        {
            if (rt == null || !rt.TryGetComponent<TextMeshProUGUI>(out var t)) return;
            t.font = number ? Number : heavy ? Heavy : Body;
            t.fontSharedMaterial = t.font != null ? t.font.material : t.fontSharedMaterial;
            t.color = color;
            t.alignment = align;
            t.enableAutoSizing = true;
            t.fontSizeMax = size1080 * S;
            t.fontSizeMin = Mathf.Min(size1080, 28f) * S;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            t.enabled = true;
            t.characterSpacing = 0f;
            t.fontStyle = FontStyles.Normal;
        }

        // ── HUD ─────────────────────────────────────────────

        private static void BuildHud(Transform root)
        {
            var hud = F(root, "TopHudGroup");
            hud.anchorMin = hud.anchorMax = new Vector2(0.5f, 1f);
            hud.pivot = new Vector2(0.5f, 1f);
            hud.anchoredPosition = Vector2.zero;
            hud.sizeDelta = new Vector2(720f, 230f);

            // 띠 바탕(싸움판 위 230) — 남색 한 판. 그 위에 키트 위 막대(rb_hud_top_bar)
            var back = F(root, "HudBackdrop");
            var backImg = back.GetComponent<Image>();
            backImg.sprite = null;
            backImg.color = new Color(0.035f, 0.045f, 0.085f, 1f);
            back.SetAsFirstSibling();
            var bar = hud.Find("RbTopBar") as RectTransform ?? Node(hud, "RbTopBar");
            Put(bar, hud, HudTop, 28, 120, 1052, 318);
            Skin(bar, "Hud/rb_hud_top_bar");
            bar.SetSiblingIndex(1);

            // 유령 — 얼굴 · 「92/100」 · 막대
            Put(F(root, "GhostHudIcon"), hud, HudTop, 50, 150, 150, 255);
            Put(F(root, "GhostHpText"), hud, HudTop, 162, 180, 330, 230);
            Style(F(root, "GhostHpText"), 46, White, TA.Left, number: true);
            var gbg = Put(F(root, "GhostHpBarBg"), hud, HudTop, 50, 266, 318, 290);
            Skin(gbg, "Hud/rb_hp_ghost_base");
            var gfill = F(root, "GhostHpBarFill");
            Skin(gfill, "Hud/rb_hp_ghost_fill");
            FillLeft(gfill);

            // 가운데 「1-07」(방 진행 칸은 뺐다)
            Put(F(root, "ChapterLabel"), hud, HudTop, 370, 172, 540, 232);
            Style(F(root, "ChapterLabel"), 60, White, TA.Center, number: true);

            // 몸 — 얼굴 · 「232/300」 · 막대
            Put(F(root, "HostPortraitImage"), hud, HudTop, 612, 160, 708, 248);
            Put(F(root, "HostHpText"), hud, HudTop, 728, 180, 895, 230);
            Style(F(root, "HostHpText"), 46, White, TA.Left, number: true);
            var hbg = Put(F(root, "HostHpBarBg"), hud, HudTop, 595, 282, 862, 306);
            Skin(hbg, "Hud/rb_hp_host_base");
            var hfill = F(root, "HostHpBarFill");
            Skin(hfill, "Hud/rb_hp_host_fill");
            FillLeft(hfill);

            // 일시정지 — 키트 원 + 아이콘
            var pause = Put(F(root, "PauseButton"), hud, HudTop, 895, 160, 1015, 280);
            Skin(pause, "Hud/rb_btn_pause", false);
            if (pause.TryGetComponent<Button>(out var pb))
            {
                pb.transition = Selectable.Transition.SpriteSwap;
                pb.spriteState = new SpriteState { pressedSprite = KitSprite("Hud/rb_btn_pause_pressed") };
            }
            var pauseIcon = pause.Find("PauseIcon") as RectTransform ?? Node(pause, "PauseIcon");
            Local(pauseIcon, 895, 160, 1015, 280, 925, 190, 985, 250);
            Img(pauseIcon, "Icons/rb_icon_sys_pause_96");

            // 「Lv 3 ━━━━」 줄 — 보스가 나오면 이 줄 자리에 보스 막대가 선다(InGameMainUI.ShiftUnderBossBand)
            var row = hud.Find("RunExpRow") as RectTransform ?? Node(hud, "RunExpRow");
            row.anchorMin = row.anchorMax = new Vector2(0.5f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.sizeDelta = new Vector2(720f, 230f);
            row.anchoredPosition = Vector2.zero;
            Put(F(root, "LevelText"), row, HudTop, 22, 358, 128, 410);
            Style(F(root, "LevelText"), 50, White, TA.Left, number: true);
            var xbg = row.Find("RunExpBg") as RectTransform ?? Node(row, "RunExpBg");
            Put(xbg, row, HudTop, 136, 368, 902, 392);
            Skin(xbg, "Hud/rb_xp_base");
            var xfill = xbg.Find("RunExpFill") as RectTransform ?? Node(xbg, "RunExpFill");
            Skin(xfill, "Hud/rb_xp_fill");
            FillLeft(xfill);

            // 골드 — 줄 오른쪽 끝(보스 때도 남는다)
            Put(F(root, "GoldIcon"), hud, HudTop, 914, 354, 968, 408);
            Put(F(root, "GoldText"), hud, HudTop, 974, 358, 1060, 410);
            Style(F(root, "GoldText"), 50, White, TA.Left, number: true);

            // 상성 삼각판(방에 들어설 때 크게 떴다 작아진다) — 몸 얼굴 왼쪽 빈 곳
            Put(F(root, "AffinityTriangle"), hud, HudTop, 548, 172, 606, 230);

            BuildBossBar(root, hud);

            // 옛 판 · 이름표 · 칸 — 그림만 끄거나 통째로 끈다(화면 스크립트가 켜도 비어 있게)
            Off(root, "Hud3GhostPill", "Hud3HostPill", "Hud3StageTrack", "Hud3GoldChip", "Hud3AffRing", "GhostLabel");
            Mute(root, "Hud3HostEmptyFace", "Hud3HostEmptyBar", "PlayerSoulPanel", "PlayerSoulLabel", "GhostNameText",
                 "GhostSubText", "HpLabelGhost", "GhostLevelBadge", "RunResourcePanel", "RunResourceLabel", "GoldCell", "GemCell",
                 "CurrentHostPanel", "HostPortraitFrame", "HostLabel", "HostNameEnText", "HostNameKrText", "HostLevelBadge",
                 "HostLevelText", "HpLabelHost", "ChapterGroup", "ChapterNameText", "RoomLabel", "RoomNumberText", "RoomTotalText",
                 "RoomProgressBg", "RoomProgressFill");
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == "Inner" || t.name == "Accent")
                    foreach (var g in t.GetComponents<Graphic>()) g.enabled = false;
            // 줄 맞춤 묶음(HudRow2)은 자식을 옮겼으니 줄 맞춤을 뺀다 — 남은 칸이 가운데로 미끄러지지 않게
            var row2 = F(root, "HudRow2");
            if (row2 != null && row2.TryGetComponent<HorizontalLayoutGroup>(out var hl)) Object.DestroyImmediate(hl);
        }

        private static void BuildBossBar(Transform root, RectTransform hud)
        {
            // 보스 막대 — 키트 해골 판(rb_hud_boss_bar). 「Lv」 줄 자리(x 16~604 · y 170~210 게임 px)
            var group = F(root, "BossGroup");
            PutPx(group, hud, 310f, 186f, 588f, 40f);
            var plate = F(root, "BossPlate");
            plate.anchorMin = Vector2.zero; plate.anchorMax = Vector2.one; plate.sizeDelta = Vector2.zero; plate.anchoredPosition = Vector2.zero;
            plate.pivot = new Vector2(0.5f, 0.5f);
            Skin(plate, "Hud/rb_hud_boss_bar");
            // 홈 — 판을 40 높이로 줄이면 테두리가 0.625 배: 홈 x 41~(폭-12) · y 15~30
            var bg = F(root, "BossHpBarBg");
            bg.anchorMin = bg.anchorMax = new Vector2(0f, 1f);
            bg.pivot = new Vector2(0f, 1f);
            bg.anchoredPosition = new Vector2(41f, -15f);
            bg.sizeDelta = new Vector2(588f - 41f - 12f, 15f);
            if (bg.TryGetComponent<Image>(out var bgImg)) bgImg.enabled = false;
            var trail = F(root, "BossHpBarTrail");
            Skin(trail, "Components/rb_bar_fill_yellow");
            FillLeft(trail);
            var fill = F(root, "BossHpBarFill");
            Skin(fill, "Components/rb_bar_fill_red");
            FillLeft(fill);
            var label = F(root, "BossLabel");
            label.anchorMin = label.anchorMax = new Vector2(0f, 1f); label.pivot = new Vector2(0f, 0.5f);
            label.anchoredPosition = new Vector2(52f, -22.5f); label.sizeDelta = new Vector2(300f, 22f);
            Style(label, 30, White, TA.Left, heavy: true);
            var hp = F(root, "BossHpText");
            hp.anchorMin = hp.anchorMax = new Vector2(1f, 1f); hp.pivot = new Vector2(1f, 0.5f);
            hp.anchoredPosition = new Vector2(-18f, -22.5f); hp.sizeDelta = new Vector2(180f, 22f);
            Style(hp, 30, White, TA.Right, number: true);
            Mute(root, "BossHpLabel");
        }

        // ── 조작부 ───────────────────────────────────────────

        private static void BuildControls(Transform root)
        {
            var group = F(root, "ControlGroup");
            group.anchorMin = group.anchorMax = new Vector2(0.5f, 1f);
            group.pivot = new Vector2(0.5f, 1f);
            group.anchoredPosition = new Vector2(0f, -1050f);
            group.sizeDelta = new Vector2(720f, 230f);

            // 조이스틱 — 키트 원판 · 손잡이(가운데)
            var pad = PutPx(F(root, "DPadBase"), group, 125f, 118f, 190f, 185f);
            Skin(pad, "Hud/rb_joystick_base", false);
            var knob = F(root, "DPadKnob");
            knob.anchorMin = knob.anchorMax = knob.pivot = new Vector2(0.5f, 0.5f);
            knob.anchoredPosition = Vector2.zero;
            knob.sizeDelta = new Vector2(76f, 78f);
            Skin(knob, "Hud/rb_joystick_knob", false);

            // 빙의(큰 원) · 액티브(아래) · 필살기(위) — 230 띠 안에 시안 배치(필살기는 빙의 위)를 접어 넣는다
            var possess = PutPx(F(root, "PossessButton"), group, 630f, 128f, 150f, 120f);
            Skin(possess, "Hud/rb_btn_possess", false);
            SwapPressed(possess, "Hud/rb_btn_possess_pressed");
            PutPx(F(root, "PossessButtonGlow"), group, 630f, 128f, 166f, 133f);
            Skin(F(root, "PossessButtonGlow"), "Hud/rb_btn_possess_ready", false);
            Inside(F(root, "PossessGhostIcon"), 75f, 50f, 60f, 58f);
            // 쿨다운 덮개는 버튼과 같은 모양(그림 없는 네모로 두면 버튼 둘레에 짙은 네모가 보였다 — 첫 캡처)
            var pcd = F(root, "PossessCooldown");
            Inside(pcd, 75f, 60f, 150f, 120f);
            if (pcd != null && pcd.TryGetComponent<Image>(out var pcdImg)) { pcdImg.sprite = KitSprite("Hud/rb_btn_possess"); pcdImg.preserveAspect = false; }
            Inside(F(root, "PossessCooldownText"), 75f, 60f, 120f, 50f);
            Inside(F(root, "PossessCostText"), 75f, 92f, 110f, 22f);
            Inside(F(root, "PossessButtonLabel"), 75f, 94f, 120f, 24f);
            Style(F(root, "PossessButtonLabel"), 30, White, TA.Center, number: true);

            var skill = PutPx(F(root, "SkillButton"), group, 487f, 170f, 100f, 100f);
            Skin(skill, "Hud/rb_btn_skill", false);
            SwapPressed(skill, "Hud/rb_btn_skill_pressed");
            RoundIcon(skill, F(root, "SkillIcon"), "SkillIconMask");
            var scd = F(root, "SkillCooldown");
            Inside(scd, 50f, 50f, 78f, 78f);
            // 둥근 덮개 — 테두리 없는 키트 원(빨간 점)을 짙게 칠해 쓴다(일시정지 원은 제 테두리 빛이 보였다)
            if (scd != null && scd.TryGetComponent<Image>(out var scdImg))
            {
                scdImg.sprite = KitSprite("Components/rb_dot_red");
                scdImg.preserveAspect = false;
                scdImg.color = new Color(0.03f, 0.04f, 0.08f, 0.78f);
            }
            Inside(F(root, "SkillSealIcon"), 50f, 50f, 40f, 40f);
            Mute(root, "SkillButtonLabel");

            var ult = PutPx(F(root, "UltButton"), group, 487f, 58f, 100f, 100f);
            Skin(ult, "Hud/rb_btn_ult", false);
            SwapPressed(ult, "Hud/rb_btn_ult_pressed");
            PutPx(F(root, "UltButtonGlow"), group, 487f, 58f, 112f, 112f);
            Skin(F(root, "UltButtonGlow"), "Hud/rb_btn_ult_ready", false);
            RoundIcon(ult, F(root, "UltIcon"), "UltIconMask");
            Mute(root, "UltButtonLabel");

            Mute(root, "ActionFrame", "ActionLabel", "FreeMoveLabel");
            var rowT = F(root, "ButtonRow");
            if (rowT != null && rowT.TryGetComponent<HorizontalLayoutGroup>(out var hl)) Object.DestroyImmediate(hl);
            // 준비 됨 빛 — 키트 ready 판을 조금 크게 버튼 **뒤**에 깐다(밝은 테두리만 둘레로 보인다 · 아이콘을 가리지 않게)
            F(root, "PossessButtonGlow")?.SetSiblingIndex(possess.GetSiblingIndex());
            F(root, "UltButtonGlow")?.SetSiblingIndex(ult.GetSiblingIndex());
        }

        /// <summary>버튼 안 자식 — 버튼 위 왼쪽 기준 중심(게임 px).</summary>
        private static void Inside(RectTransform rt, float cx, float cy, float w, float h)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(cx, -cy);
        }

        /// <summary>
        /// 고리 안 둥근 아이콘 — 네모 아이콘의 모서리가 고리 밖으로 삐져나왔다(첫 캡처). 고리 안쪽 지름(78)의 원판으로 가린다.
        /// 원판은 키트 일시정지 원(rb_btn_pause)을 가림 모양으로만 쓴다(보이지 않음). 아이콘은 이름 그대로 그 아래로 옮긴다.
        /// </summary>
        private static void RoundIcon(RectTransform button, RectTransform icon, string maskName)
        {
            if (button == null || icon == null) return;
            var mask = button.Find(maskName) as RectTransform ?? Node(button, maskName);
            Inside(mask, 50f, 50f, 78f, 78f);
            var img = mask.GetComponent<Image>() ?? mask.gameObject.AddComponent<Image>();
            img.sprite = KitSprite("Hud/rb_btn_pause");
            img.raycastTarget = false;
            var m = mask.GetComponent<Mask>() ?? mask.gameObject.AddComponent<Mask>();
            m.showMaskGraphic = false;
            mask.SetSiblingIndex(icon.GetSiblingIndex());
            icon.SetParent(mask, false);
            icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0.5f, 0.5f);
            icon.anchoredPosition = Vector2.zero;
            icon.sizeDelta = new Vector2(78f, 78f);
        }

        private static void SwapPressed(RectTransform rt, string pressed)
        {
            if (rt == null || !rt.TryGetComponent<Button>(out var b)) return;
            b.transition = Selectable.Transition.SpriteSwap;
            b.spriteState = new SpriteState { pressedSprite = KitSprite(pressed) };
        }

        // ── 레벨업 ───────────────────────────────────────────

        public const float CardTop0 = 215f, CardStep = 255f, CardW = 680f, CardH = 245f;

        private static void BuildLevelUp(Transform root)
        {
            // 판(720×1280)은 화면 가운데 · 딤은 위아래로 늘린 덧판이 마저 덮는다 — 옛 판은 위 1280 만 덮어 20:9 아래가 훤했다.
            // 판을 화면 전체로 늘리지 않는 까닭: 창 연출(PopupFxSpec)이 판 왼쪽 위 기준 좌표라 20:9 에서 어긋난다
            var panel = F(root, "BuffChoicePanel");
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(720f, 1280f);
            panel.anchoredPosition = Vector2.zero;
            var ext = panel.Find("BuffDimExtend") as RectTransform ?? Node(panel, "BuffDimExtend");
            ext.anchorMin = Vector2.zero; ext.anchorMax = Vector2.one; ext.pivot = new Vector2(0.5f, 0.5f);
            ext.offsetMin = new Vector2(-400f, -800f); ext.offsetMax = new Vector2(400f, 800f);
            var extImg = ext.GetComponent<Image>() ?? ext.gameObject.AddComponent<Image>();
            extImg.color = new Color(0.02f, 0.03f, 0.06f, 0.95f);   // InGameMainUI Awake 의 딤 색과 같게
            extImg.raycastTarget = true;
            ext.SetAsFirstSibling();
            FixLevelUpFx(panel);
            Mute(root, "BuffFrame", "BuffTitleText", "BuffNoteText");
            var title = F(root, "BuffTitleArt");
            PutPx(title, panel, 360f, 92f, 330f, 95f);
            Skin(title, "WordArt/rb_word_word_level_up", false);
            var sub = F(root, "BuffSubText");
            PutPx(sub, panel, 360f, 166f, 640f, 40f);
            Style(sub, 44, Dim, TA.Center);

            for (int i = 0; i < 3; i++)
            {
                var card = PutPx(F(root, $"BuffCard{i}"), panel, 360f, CardTop0 + CardStep * i + CardH * 0.5f, CardW, CardH);
                Skin(card, "Components/rb_card_common");
                Corner(F(root, $"BuffCard{i}Icon"), 30f, 66f, 113f, 113f);
                var rare = card.Find($"BuffCard{i}Rarity") as RectTransform ?? Node(card, $"BuffCard{i}Rarity");
                if (rare.GetComponent<TextMeshProUGUI>() == null) rare.gameObject.AddComponent<TextMeshProUGUI>().text = "EPIC";
                Corner(rare, 172f, 36f, 280f, 30f);
                Style(rare, 36, Yellow, TA.Left, number: true);
                Corner(F(root, $"BuffCard{i}Name"), 172f, 66f, 470f, 44f);
                Style(F(root, $"BuffCard{i}Name"), 52, White, TA.Left, heavy: true);
                var desc = F(root, $"BuffCard{i}Desc");
                Corner(desc, 172f, 114f, 488f, 100f);
                Style(desc, 38, Dim, TA.TopLeft);
                if (desc.TryGetComponent<TextMeshProUGUI>(out var dt)) { dt.enableWordWrapping = true; dt.overflowMode = TextOverflowModes.Overflow; }
                var chip = F(root, $"BuffCard{i}Chip");
                Corner(chip, 540f, 8f, 128f, 40f);
                var chipText = F(root, $"BuffCard{i}ChipText");
                chipText.anchorMin = Vector2.zero; chipText.anchorMax = Vector2.one; chipText.sizeDelta = Vector2.zero; chipText.anchoredPosition = Vector2.zero;
                Style(chipText, 38, White, TA.Right, number: true);
            }

            // 맨 아래 「카드를 골라 주세요」 — 누를 수 없는 안내 판(시안)
            var hint = panel.Find("BuffHint") as RectTransform ?? Node(panel, "BuffHint");
            PutPx(hint, panel, 360f, 1040f, 680f, 94f);
            Skin(hint, "Components/rb_btn_secondary_disabled");
            var ht = hint.Find("BuffHintText") as RectTransform ?? Node(hint, "BuffHintText");
            if (ht.GetComponent<TextMeshProUGUI>() == null)
                Text(Stretch(ht), "ui.rb.levelup.hint", "카드를 골라 주세요", 40, Dim, TA.Center);
        }

        /// <summary>
        /// 레벨업 창 연출 — 제목 빛(emblem)은 새 제목 자리(y 92)로 옮기고, 옛 창 틀의 관(tube) · 배관(pipe) 빛은 뺀다
        /// (틀이 없어 빈 자리에 노란 세로줄만 떠 있었다 — 첫 캡처). 카드 빛(LevelUpFx)은 카드 크기를 따라가 그대로 둔다.
        /// </summary>
        private static void FixLevelUpFx(RectTransform panel)
        {
            var spec = panel.GetComponent<Game.Module.InGame.PopupFxSpec>();
            if (spec == null) return;
            var so = new SerializedObject(spec);
            var layers = so.FindProperty("_layers");
            for (int i = layers.arraySize - 1; i >= 0; i--)
            {
                var l = layers.GetArrayElementAtIndex(i);
                string frames = l.FindPropertyRelative("Frames").stringValue;
                if (frames.Contains("tube") || frames.Contains("pipe")) { layers.DeleteArrayElementAtIndex(i); continue; }
                if (frames.Contains("emblem")) l.FindPropertyRelative("Y").floatValue = 92f;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>카드 안 — 카드 위 왼쪽 기준(게임 px).</summary>
        private static void Corner(RectTransform rt, float x, float y, float w, float h)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, -y);
        }

        private static void Wire(Game.Module.InGame.InGameMainUI ui)
        {
            var so = new SerializedObject(ui);
            so.FindProperty("_reborn").boolValue = true;
            so.FindProperty("_skillFrame").objectReferenceValue = KitSprite("Hud/rb_btn_skill");
            so.FindProperty("_skillFrameEmpty").objectReferenceValue = KitSprite("Hud/rb_btn_skill_cooldown");
            var panels = so.FindProperty("_rbCardPanels");
            string[] p = { "rb_card_common", "rb_card_rare", "rb_card_epic", "rb_card_top" };
            panels.arraySize = p.Length;
            for (int i = 0; i < p.Length; i++) panels.GetArrayElementAtIndex(i).objectReferenceValue = KitSprite($"Components/{p[i]}");
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
