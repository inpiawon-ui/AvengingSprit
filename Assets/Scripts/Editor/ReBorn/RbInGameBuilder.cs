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
                BuildShop(root.transform);
                BuildEvent(root.transform);
                BuildShrine(root.transform);
                BuildResult(root.transform);
                BuildPause(root.transform);
                RbExtraScreensBuilder.BuildInGame((RectTransform)root.transform);   // 보스 경고 · 다음 방 · 밤 몸 고르기 · 안내
                // 상단 HUD · 하단 조작 · 무대 글자는 방(늘 720 폭)을 넘어 화면 끝까지 늘리지 않는다 — 16:10 · 4:3 에서 Lv 줄 · 필살기가 방 밖으로 나갔다(PD 2026-10-27)
                foreach (var n in new[] { "TopHudGroup", "ControlGroup", "StageText" })
                    if (root.transform.Find(n) is Transform t && t.GetComponent<Game.Module.Common.UI.ScreenFitKeepWidth>() == null)
                        t.gameObject.AddComponent<Game.Module.Common.UI.ScreenFitKeepWidth>();
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

        /// <summary>
        /// 탑 · 밤 판 — 위 막대 밑 한 줄(규칙 · 남은 적 / 무리 · 다음 일) + 알림 띠. 시안 tower-hud · night-hud.png(위 430~572 · 640~800).
        /// 스토리 판에서는 꺼 둔다(InGameMainUI.ApplyModeHud 가 켠다). 글자는 켠 채 붙이고 마지막에 끈다.
        /// </summary>
        private static void BuildModeHud(RectTransform hud)
        {
            var bar = Node(hud, "RbModeBar");
            Put(bar, hud, HudTop, 30, 430, 1050, 572);
            Skin(bar, "Components/rb_panel_base");
            Text(Local(Node(bar, "RbModeBarLeft"), 30, 430, 1050, 572, 60, 468, 600, 536), null, "빙의는 한 번", 44, White, TA.Left, heavy: true);
            Text(Local(Node(bar, "RbModeBarRight"), 30, 430, 1050, 572, 600, 468, 1015, 536), null, "남은 적 12 · 빙의 남음 1", 40, White, TA.Right, heavy: true);
            var band = Node(hud, "RbModeBanner");
            Put(band, hud, HudTop, 60, 640, 1020, 800);
            Skin(band, "Components/rb_toast");
            Text(Local(Node(band, "RbModeBannerText"), 60, 640, 1020, 800, 130, 664, 990, 776), null, "이 층은 나갈 수 없다!", 54, Cyan, TA.Center, number: true);
            bar.gameObject.SetActive(false);
            band.gameObject.SetActive(false);
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
            // 위 판 — 시안 .hud(28~1052 x 120~320 · rgba(8,13,23,.85) · 둥근 24 · 위 안쪽 가는 금선). 테두리 없는 어두운 판이라
            //   보상 칸 판(rb_result_cell — 같은 색 · 같은 위 금선)을 깐다. 옛 네온 판(rb_hud_top_bar)은 아래 테두리 홈에 막대가 끼어
            //   숫자와 멀리 떨어졌다(PD 2026-10-27 「체력 막대가 너무 아래 · 시안보다도 올려 줘」)
            var bar = hud.Find("RbTopBar") as RectTransform ?? Node(hud, "RbTopBar");
            Put(bar, hud, HudTop, 28, 120, 1052, 320);
            Skin(bar, "Components/rb_result_cell");
            // 불투명 — 인게임 캔버스는 선형 색이라 0.92 로도 판이 뿌옇게 비쳤다(시안 #0E1119 · 게임 #392E35, 2026-10-27 실측)
            bar.GetComponent<Image>().color = Color.white;
            bar.SetSiblingIndex(1);

            // 숫자 · 막대 한 묶음을 판 가운데(220)에 — 숫자 184~228(도트 36) · 막대 242~256(높이 14). 유령 · 몸 막대는 같은 줄 · 같은 간격
            // 막대는 시안 .hpbar(바탕 #222A40 · 둥근 7 · 채움 청록 #48BDCD / 초록 #4AC28B) — 평평한 키트 막대(rb_bar_track · rb_bar_fill_cyan)
            // 얼굴은 막대 위로 올라오게 88 로 줄여 146~234 에(시안 96 · 155~251 그대로면 올린 막대와 겹친다)
            Put(F(root, "GhostHudIcon"), hud, HudTop, 52, 146, 140, 234);
            Put(F(root, "GhostHpText"), hud, HudTop, 156, 184, 340, 228);
            Style(F(root, "GhostHpText"), 36, White, TA.Left, number: true);
            var gbg = Put(F(root, "GhostHpBarBg"), hud, HudTop, 52, 242, 316, 256);
            Skin(gbg, "Components/rb_bar_track");
            var gfill = F(root, "GhostHpBarFill");
            Skin(gfill, "Components/rb_bar_fill_cyan");
            FillLeft(gfill);

            // 가운데 「1-07」(방 진행 칸은 뺐다)
            Put(F(root, "ChapterLabel"), hud, HudTop, 340, 176, 572, 236);
            Style(F(root, "ChapterLabel"), 44, White, TA.Center, number: true);

            // 몸 — 얼굴 · 「232/300」 · 막대(유령과 같은 줄 · 같은 간격 — PD 같은 날 「유령 간격만큼 호스트도」)
            Put(F(root, "HostPortraitImage"), hud, HudTop, 596, 146, 692, 234);
            Put(F(root, "HostHpText"), hud, HudTop, 708, 184, 890, 228);
            Style(F(root, "HostHpText"), 36, White, TA.Left, number: true);
            var hbg = Put(F(root, "HostHpBarBg"), hud, HudTop, 596, 242, 860, 256);
            Skin(hbg, "Components/rb_bar_track");
            var hfill = F(root, "HostHpBarFill");
            Skin(hfill, "Components/rb_bar_fill_cyan");
            hfill.GetComponent<Image>().color = new Color32(255, 255, 172, 255);   // 청록 채움을 시안 초록(#4AC28B)으로 — 파랑만 덜어 낸다
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
            // 시안 .progress 134~902 x 372~388(높이 16 · 바탕 #252C40 · 청록 채움) — 평평한 키트 막대
            Put(F(root, "LevelText"), row, HudTop, 28, 355, 128, 405);
            Style(F(root, "LevelText"), 40, White, TA.Left, number: true);
            var xbg = row.Find("RunExpBg") as RectTransform ?? Node(row, "RunExpBg");
            Put(xbg, row, HudTop, 134, 372, 902, 388);
            Skin(xbg, "Components/rb_bar_track");
            var xfill = xbg.Find("RunExpFill") as RectTransform ?? Node(xbg, "RunExpFill");
            Skin(xfill, "Components/rb_bar_fill_cyan");
            FillLeft(xfill);

            // 골드 — 줄 오른쪽 끝(보스 때도 남는다)
            Put(F(root, "GoldIcon"), hud, HudTop, 918, 356, 966, 404);
            Put(F(root, "GoldText"), hud, HudTop, 982, 355, 1060, 405);
            Style(F(root, "GoldText"), 40, White, TA.Left, number: true);

            // 상성 삼각판(방에 들어설 때 크게 떴다 작아진다) — 몸 얼굴 왼쪽 빈 곳
            Put(F(root, "AffinityTriangle"), hud, HudTop, 534, 162, 588, 216);

            BuildBossBar(root, hud);
            BuildModeHud(hud);

            // 몸 없음(시안 ghost-hud, PD 2026-10-25) — 몸 칸 자리 「몸 없음」 · 「Lv」 줄 아래 안내 줄. 화면 스크립트가 켜고 끈다
            foreach (var n in new[] { "RbNoBodyText", "RbNoBodyHint" })
                if (hud.Find(n) is Transform oldNode) Object.DestroyImmediate(oldNode.gameObject);
            Text(Put(Node(hud, "RbNoBodyText"), hud, HudTop, 612, 176, 895, 232), "ui.rb.hud.no_body", "몸 없음", 44, Dim, TA.Left, heavy: true);
            var hint = Put(Node(hud, "RbNoBodyHint"), hud, HudTop, 60, 430, 1020, 490);
            Skin(hint, "Components/rb_info_chip");
            Text(Stretch(Node(hint, "RbNoBodyHintText")), "ui.rb.hud.no_body_hint", "몸 없이 떠 있는 동안 유령 체력이 줄어요", 36, White, TA.Center, heavy: true);

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

            // 두 묶음(PD 2026-10-27) — 몸이 있으면 스킬 묶음(액티브 · 필살기)만, 몸을 잃으면 빙의 묶음만(크게 · 강조).
            //   화면 스크립트(InGameMainUI.RbExtras OnRbHp)가 몸 상태로 묶음을 켜고 끈다. 버튼끼리는 같은 자리를 나눠 쓴다
            var skills = group.Find("RbSkillGroup") as RectTransform ?? Node(group, "RbSkillGroup");
            Stretch(skills);
            var souls = group.Find("RbPossessGroup") as RectTransform ?? Node(group, "RbPossessGroup");
            Stretch(souls);
            souls.gameObject.SetActive(false);

            // 빙의 — 몸을 잃었을 때 필살기 자리에 크게(시안 230 원 · 강조 빛)
            var possess = PutPx(F(root, "PossessButton"), group, 628f, 122f, 150f, 120f);
            possess.localScale = new Vector3(PossessBig, PossessBig, 1f);
            Skin(possess, "Hud/rb_btn_possess", false);
            SwapPressed(possess, "Hud/rb_btn_possess_pressed");
            PutPx(F(root, "PossessButtonGlow"), group, 628f, 122f, 166f, 133f).localScale = new Vector3(PossessBig, PossessBig, 1f);
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

            // 액티브 — 단순한 파란 고리 · 작게(필살기 왼쪽 아래)
            var skill = PutPx(F(root, "SkillButton"), group, 496f, 160f, 100f, 100f);
            skill.localScale = new Vector3(SkillSmall, SkillSmall, 1f);
            Skin(skill, "Hud/rb_btn_skill", false);
            SwapPressed(skill, "Hud/rb_btn_skill_pressed");
            RoundIcon(skill, F(root, "SkillIcon"), "SkillIconMask");
            // 쿨 덮개 — 아이콘 둥근 마스크 안에 넣어 원 전체를 덮는다. 예전엔 빨간 점 그림(21 칸 중 원 16 칸)을 써서
            //   아이콘 가운데만 어두워져 「아이콘을 하나 더 넣은 것」처럼 보였다(PD 2026-10-27). 채우기 방식은 화면 스크립트가 준다
            var scd = F(root, "SkillCooldown");
            if (scd != null && skill.Find("SkillIconMask") is RectTransform scdMask)
            {
                scd.SetParent(scdMask, false);
                Stretch(scd);
                scd.SetAsLastSibling();   // 아이콘 위
                if (scd.TryGetComponent<Image>(out var scdImg))
                {
                    // 흰 판 — 마스크가 원으로 자른다. 원형 채우기는 그림이 있어야 돈다(그림 없으면 통째로 덮인다)
                    scdImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                    scdImg.type = Image.Type.Filled;
                    scdImg.fillMethod = Image.FillMethod.Radial360;
                    scdImg.color = new Color(0.02f, 0.03f, 0.06f, 0.82f);
                }
            }
            // 남은 초 — 아이콘 가운데 숫자
            if (skill.Find("SkillCooldownText") is Transform oldCd) Object.DestroyImmediate(oldCd.gameObject);
            var scdText = Node(skill, "SkillCooldownText");
            Inside(scdText, 50f, 50f, 78f, 50f);
            Text(scdText, null, "6", 40, White, TA.Center, number: true);
            scdText.gameObject.SetActive(false);
            Inside(F(root, "SkillSealIcon"), 50f, 50f, 40f, 40f);
            Mute(root, "SkillButtonLabel");

            // 필살기 — 크게 · 모양부터 다르게(왕관 · 날개 장식 금테 rb_btn_ult_frame). 빙의 버튼이 있던 오른쪽 큰 자리
            //   테는 아이콘 위에 얹는다(가운데 구멍으로 아이콘이 보인다) — 구멍 지름 150/260 · 중심이 그림 가운데보다 10/260 아래
            var ult = PutPx(F(root, "UltButton"), group, 628f, 118f, 100f, 100f);
            ult.localScale = new Vector3(UltBig, UltBig, 1f);
            Skin(ult, "Hud/rb_btn_ult", false);
            RoundIcon(ult, F(root, "UltIcon"), "UltIconMask");
            Mute(root, "UltButtonLabel");
            var frameSprite = KitSprite("Hud/rb_btn_ult_frame");
            if (frameSprite != null)
            {
                const float frameSize = 78f * 260f / 150f;   // 구멍이 아이콘 원판(78)과 맞는 크기
                var frame = ult.Find("UltFrame") as RectTransform ?? Node(ult, "UltFrame");
                Inside(frame, 50f, 50f - frameSize * 10f / 260f, frameSize, frameSize);
                var fi = frame.GetComponent<Image>() ?? frame.gameObject.AddComponent<Image>();
                fi.sprite = frameSprite; fi.preserveAspect = true; fi.raycastTarget = false;
                frame.SetSiblingIndex(F(root, "UltIconMask").GetSiblingIndex() + 1);
                // 옛 노란 고리는 누름 칸으로만(보이지 않게) · 누르면 테가 어두워진다
                if (ult.TryGetComponent<Image>(out var ui)) ui.color = new Color(1f, 1f, 1f, 0f);
                if (ult.TryGetComponent<Button>(out var ub)) { ub.targetGraphic = fi; ub.transition = Selectable.Transition.ColorTint; }
            }
            else SwapPressed(ult, "Hud/rb_btn_ult_pressed");
            // 준비 빛 — 테 모양이 달라 둥근 고리 빛 대신 부드러운 금빛 번짐을 뒤에
            var ug = PutPx(F(root, "UltButtonGlow"), group, 628f, 112f, 170f, 170f);
            ug.localScale = new Vector3(UltBig, UltBig, 1f);
            Skin(ug, "Components/rb_card_glow", false);
            ug.GetComponent<Image>().color = new Color32(255, 196, 70, 150);

            // 몸 없음(시안 ghost-hud) — 액티브 · 필살기 단추 가운데 「몸 없음」. 화면 스크립트가 켜고 끈다
            foreach (var (btn, n) in new[] { (skill, "SkillNoBodyText"), (ult, "UltNoBodyText") })
            {
                if (btn.Find(n) is Transform oldNode) Object.DestroyImmediate(oldNode.gameObject);
                var t = Node(btn, n);
                Inside(t, 50f, 50f, 92f, 40f);
                Text(t, "ui.rb.hud.no_body", "몸 없음", 30, Dim, TA.Center, heavy: true);
            }
            Mute(root, "ActionFrame", "ActionLabel", "FreeMoveLabel");
            var rowT = F(root, "ButtonRow");
            if (rowT != null && rowT.TryGetComponent<HorizontalLayoutGroup>(out var hl)) Object.DestroyImmediate(hl);
            // 묶음으로 옮긴다(자리 값은 같은 판 기준이라 그대로) — 준비 빛은 버튼 **뒤**
            foreach (var n in new[] { "UltButtonGlow", "UltButton", "SkillButton" }) F(root, n)?.SetParent(skills, false);
            foreach (var n in new[] { "PossessButtonGlow", "PossessButton" }) F(root, n)?.SetParent(souls, false);
            F(root, "UltButtonGlow")?.SetSiblingIndex(0);
            F(root, "PossessButtonGlow")?.SetSiblingIndex(0);
        }

        // 버튼 크기 — 액티브는 작고 단순하게, 필살기 · 빙의는 크게(PD 2026-10-27)
        private const float SkillSmall = 0.9f, UltBig = 1.32f, PossessBig = 1.15f;

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

        // 시안 levelup-selected(Core/ui.css .choice) — 카드 1024 x 300 · 목록 위 510 · 간격 28 · 왼쪽 28. 판 위 = 시안 140 줄
        // 카드 줄은 시안보다 135 아래 — 시안 자리면 제목 · 부제가 게임 HUD(체력 판 0~135 · Lv 줄 180) 밑에 깔린다(PD 2026-10-27)
        public const float LvShift = 135f;
        public const float CardTop0 = (510f - 140f) * S + LvShift, CardStep = 328f * S, CardW = 1024f * S, CardH = 300f * S;

        private static void BuildLevelUp(Transform root)
        {
            // 판(720×1280)은 화면 가운데 · 딤은 위아래로 늘린 덧판이 마저 덮는다 — 옛 판은 위 1280 만 덮어 20:9 아래가 훤했다.
            // 판을 화면 전체로 늘리지 않는 까닭: 창 연출(PopupFxSpec)이 판 왼쪽 위 기준 좌표라 20:9 에서 어긋난다
            var panel = F(root, "BuffChoicePanel");
            panel.gameObject.SetActive(true);   // 켠 채 글자를 붙인다(꺼진 곳의 TMP 는 실행 때 크기가 풀린다) — Awake 가 끈다
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(720f, 1280f);
            panel.anchoredPosition = Vector2.zero;
            var ext = panel.Find("BuffDimExtend") as RectTransform ?? Node(panel, "BuffDimExtend");
            ext.anchorMin = Vector2.zero; ext.anchorMax = Vector2.one; ext.pivot = new Vector2(0.5f, 0.5f);
            ext.offsetMin = new Vector2(-400f, -800f); ext.offsetMax = new Vector2(400f, 800f);
            var extImg = ext.GetComponent<Image>() ?? ext.gameObject.AddComponent<Image>();
            // 화면 전체를 이 한 겹이 어둡게 한다(판 자체는 투명 — InGameMainUI Awake). 인게임 캔버스는 선형 색이라 0.95 로는 덜 어두워 0.99
            extImg.color = new Color(0.02f, 0.03f, 0.06f, RbExtraScreensBuilder.InGameDim);
            extImg.raycastTarget = true;
            ext.SetAsFirstSibling();
            Lock(panel);
            // 20:9 는 위 여백을 시안(140 줄)까지만 — 가운데 두면 카드 · 버튼이 시안보다 아래로 처진다
            if (panel.GetComponent<Game.Module.Common.UI.RbFitScale>() == null) panel.gameObject.AddComponent<Game.Module.Common.UI.RbFitScale>();
            FixLevelUpFx(panel);
            Mute(root, "BuffFrame", "BuffTitleText", "BuffNoteText");
            // 제목 워드아트 284~796 x 200~348 · 부제 42 px(#ABB2CE) 378~431 — 시안 levelup-selected 를 브라우저에서 잰 값
            var title = F(root, "BuffTitleArt");
            PutPx(title, panel, 360f, 250f, 512f * S, 148f * S);   // Lv 줄(180) 아래
            Skin(title, "WordArt/rb_word_word_level_up", false);
            var sub = F(root, "BuffSubText");
            PutPx(sub, panel, 360f, 340f, 1024f * S, 53f * S);
            Style(sub, 42, LvMuted, TA.Center);

            for (int i = 0; i < 3; i++) BuildLevelUpCard(root, panel, i);
            // 시안의 맨 아래 「골라서 확정」 버튼은 없다 — 카드를 누르면 바로 적용(PD 2026-10-27 「투스텝은 필요 없어」)
        }

        // 시안 카드 글자 색(Core/ui.css) — 흐린 글 #ABB2CE · 이름 #EDF0F6 · 효과 #6BDAA1
        private static readonly Color LvMuted = new Color32(171, 178, 206, 255);
        private static readonly Color LvName = new Color32(237, 240, 246, 255);
        private static readonly Color LvDelta = new Color32(107, 218, 161, 255);

        /// <summary>
        /// 카드 한 장 — 몸통(그러데이션 판) · 왼쪽 등급 띠 · 아이콘 칸 · 등급 · 이름 · 설명 · 효과 줄 · 오른쪽 위 꼬리표.
        /// 카드 칸(버튼)은 투명 — 그림은 자식들이 그린다. 자리는 카드 위 왼쪽 기준(시안 px × 2/3).
        /// </summary>
        private static void BuildLevelUpCard(Transform root, RectTransform panel, int i)
        {
            var card = PutPx(F(root, $"BuffCard{i}"), panel, 360f, CardTop0 + CardStep * i + CardH * 0.5f, CardW, CardH);
            var hit = card.GetComponent<Image>() ?? card.gameObject.AddComponent<Image>();
            hit.sprite = null; hit.color = new Color(1f, 1f, 1f, 0f); hit.raycastTarget = true;
            if (card.TryGetComponent<Button>(out var cb)) cb.transition = Selectable.Transition.None;

            // 납품 판은 둘레에 2 px(시안) 여백이 있다 — 그만큼 넓혀 깐다
            const float pad = 2f * S;
            var body = LvLayer(card, $"BuffCard{i}Body", "Components/rb_lv_card", -pad, -pad, CardW + pad * 2f, CardH + 8f * S + pad * 2f, true);
            body.color = new Color(0.82f, 0.82f, 0.82f, 1f);   // 납품이 시안보다 밝다(#1C2744 → 시안 #172038) — 밝기만 맞춘다
            LvLayer(card, $"BuffCard{i}Strip", "Components/rb_lv_strip", -pad, -pad, CardW + pad * 2f, CardH + pad * 2f, true).color = new Color32(140, 149, 166, 255);
            LvLayer(card, $"BuffCard{i}Well", "Components/rb_lv_artwell", 24f * S - pad, 30f * S - pad, 240f * S + pad * 2f, 240f * S + pad * 2f, true);

            var icon = F(root, $"BuffCard{i}Icon");
            Corner(icon, 24f * S + 16f, 30f * S + 16f, 240f * S - 32f, 240f * S - 32f);
            icon.SetAsLastSibling();
            float tx = 292f * S, tw = 708f * S;   // 글자 줄 x 320(카드 안 292) ~ 1028
            var rare = card.Find($"BuffCard{i}Rarity") as RectTransform ?? Node(card, $"BuffCard{i}Rarity");
            if (rare.GetComponent<TextMeshProUGUI>() == null) rare.gameObject.AddComponent<TextMeshProUGUI>().text = "EPIC";
            Corner(rare, tx, 26f * S, tw, 43f * S);
            Style(rare, 36, LvMuted, TA.Left, heavy: true);
            var name = F(root, $"BuffCard{i}Name");
            Corner(name, tx, 83f * S, tw, 62f * S);
            Style(name, 52, LvName, TA.Left, heavy: true);
            var desc = F(root, $"BuffCard{i}Desc");
            Corner(desc, tx, 159f * S, tw, 43f * S);
            Style(desc, 36, LvMuted, TA.Left, heavy: true);
            var delta = card.Find($"BuffCard{i}Delta") as RectTransform ?? Node(card, $"BuffCard{i}Delta");
            if (delta.GetComponent<TextMeshProUGUI>() == null) Text(delta, null, "공격력 +8%", 44, LvDelta, TA.Left, number: true);
            Corner(delta, tx, 216f * S, tw, 58f * S);
            foreach (var r in new[] { rare, name, desc, delta }) r.SetAsLastSibling();

            // 꼬리표 「Lv 1→2」 · 「NEW」 — 오른쪽 끝 1028(카드 안 1000) · 위 18 · 높이 60. 폭은 글에 맞춰 화면 스크립트가 늘린다
            var chip = F(root, $"BuffCard{i}Chip");
            Corner(chip, 1000f * S - 104f * S, 18f * S, 104f * S, 60f * S);
            Skin(chip, "Components/rb_lv_tag");
            if (chip.TryGetComponent<Image>(out var ci)) { ci.color = new Color(1f, 1f, 1f, 0.79f); ci.raycastTarget = false; }
            var chipText = F(root, $"BuffCard{i}ChipText");
            if (chipText.parent != chip) chipText.SetParent(chip, false);
            chipText.anchorMin = Vector2.zero; chipText.anchorMax = Vector2.one; chipText.sizeDelta = Vector2.zero; chipText.anchoredPosition = Vector2.zero;
            Style(chipText, 36, LvName, TA.Center, number: true);
            chip.SetAsLastSibling();
        }

        /// <summary>카드 안 그림 한 겹(위 왼쪽 기준 · 게임 px). 앞쪽 형제로 깔아 글자 · 아이콘보다 뒤에 그린다.</summary>
        private static Image LvLayer(RectTransform card, string name, string sprite, float x, float y, float w, float h, bool sliced)
        {
            var rt = card.Find(name) as RectTransform ?? Node(card, name);
            Corner(rt, x, y, w, h);
            Skin(rt, sprite, sliced);
            var img = rt.GetComponent<Image>();
            img.raycastTarget = false;
            if (!sliced) img.preserveAspect = false;
            rt.SetAsLastSibling();
            return img;
        }

        // ── 방 안 창: 상인 · 이벤트 · 제단 ───────────────────────

        /// <summary>창 판을 화면 가운데 720×1280 으로 · 딤 덧판으로 위아래까지 덮는다(레벨업과 같은 방식).</summary>
        private static RectTransform CenterPanel(Transform root, string name)
        {
            var panel = F(root, name);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(720f, 1280f);
            panel.anchoredPosition = Vector2.zero;
            var ext = panel.Find(name + "DimExtend") as RectTransform ?? Node(panel, name + "DimExtend");
            ext.anchorMin = Vector2.zero; ext.anchorMax = Vector2.one; ext.pivot = new Vector2(0.5f, 0.5f);
            ext.offsetMin = new Vector2(-400f, -800f); ext.offsetMax = new Vector2(400f, 800f);
            var img = ext.GetComponent<Image>() ?? ext.gameObject.AddComponent<Image>();
            img.color = new Color(0.02f, 0.03f, 0.06f, 0.86f);   // InGameMainUI Awake 의 창 딤 색과 같게
            img.raycastTarget = true;
            ext.SetAsFirstSibling();
            Lock(panel);
            return panel;
        }

        /// <summary>
        /// 가운데 720×1280 판은 화면 비율 맞추기(루트의 ScreenFit)에서 뺀다 — 안 그러면 판 안 요소를 화면 양끝으로 벌린다
        /// (2026-10-23 4:3 결과 캡처: 골드 칸 · 로비로 버튼이 왼쪽 끝으로 · 20:9 레벨업 제목이 위 끝으로).
        /// </summary>
        private static void Lock(RectTransform rt)
        {
            if (rt.GetComponent<Game.Module.Common.UI.ScreenFitLock>() == null)
                rt.gameObject.AddComponent<Game.Module.Common.UI.ScreenFitLock>();
        }

        /// <summary>창 연출에서 옛 틀에 붙어 있던 장식(관 · 배관 · 사슬 · 촛불 · 거품 · 보석)을 뺀다 — 새 판에는 그 자리가 없다.</summary>
        private static void DropFrameFx(RectTransform panel, params string[] keys)
        {
            var spec = panel.GetComponent<Game.Module.InGame.PopupFxSpec>();
            if (spec == null) return;
            var so = new SerializedObject(spec);
            var layers = so.FindProperty("_layers");
            for (int i = layers.arraySize - 1; i >= 0; i--)
            {
                string frames = layers.GetArrayElementAtIndex(i).FindPropertyRelative("Frames").stringValue;
                foreach (var k in keys) if (frames.Contains(k)) { layers.DeleteArrayElementAtIndex(i); break; }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform TitlePlate(RectTransform panel, RectTransform title, float x0, float y0, float x1, float y1)
        {
            var plate = panel.Find(title.name + "Plate") as RectTransform ?? Node(panel, title.name + "Plate");
            PutPx(plate, panel, (x0 + x1) * 0.5f, (y0 + y1) * 0.5f, x1 - x0, y1 - y0);
            Skin(plate, "Components/rb_popup_title_plate");
            PutPx(title, panel, (x0 + x1) * 0.5f, (y0 + y1) * 0.5f, x1 - x0 - 24f, y1 - y0 - 10f);
            Style(title, 46, White, TA.Center, heavy: true);
            plate.SetSiblingIndex(title.GetSiblingIndex());
            return plate;
        }

        private static void BuildShop(Transform root)
        {
            var panel = CenterPanel(root, "ShopPanel");
            DropFrameFx(panel, "tube", "pipe");
            var box = PutPx(F(root, "ShopBox"), panel, 360f, 660f, 648f, 920f);
            Skin(box, "Components/rb_modal");
            var title = F(root, "ShopTitleText");
            TitlePlate(panel, title, 220f, 176f, 500f, 236f);   // 키트 모달의 위 가운데 턱에 얹는다
            // 옛 제목(「상점」 ui.shop.title)이 이미 고정 글자로 붙어 있다 — 키만 「떠돌이 상인」으로 바꾼다
            var loc = title.GetComponent<Game.Module.Common.LocalizedText>() ?? title.gameObject.AddComponent<Game.Module.Common.LocalizedText>();
            loc.SetKey("ui.rb.shop.title");
            // 상인 — 그림 칸 · 말(시안 merchant: 「필요한 걸 골라 봐!」). 그림은 키트 안내 그림에서 상인만 보이게 틀로 자른다
            foreach (var n in new[] { "ShopMerchant", "ShopHello", "ShopNever" })
                if (panel.Find(n) is Transform oldNode) Object.DestroyImmediate(oldNode.gameObject);
            var face = PutPx(Node((RectTransform)panel, "ShopMerchant"), panel, 150f, 300f, 190f, 130f);
            Skin(face, "Components/rb_list_row");
            if (face.TryGetComponent<Image>(out var fi)) fi.pixelsPerUnitMultiplier = 2f;
            face.gameObject.AddComponent<RectMask2D>();
            var art = Node(face, "ShopMerchantArt");
            art.anchorMin = art.anchorMax = new Vector2(0.5f, 0.5f);
            art.sizeDelta = new Vector2(330f, 220f);           // 그림 720 × 480 — 상인 얼굴 · 짐이 틀 가운데 오게
            art.anchoredPosition = new Vector2(8f, -14f);
            Img(art, "Art/rb_art_guide_merchant");
            var hello = PutPx(Node((RectTransform)panel, "ShopHello"), panel, 445f, 300f, 330f, 64f);
            Skin(hello, "Components/rb_info_chip");
            Text(Stretch(Node(hello, "ShopHelloText")), "ui.rb.shop.hello", "필요한 걸 골라 봐!", 40, White, TA.Center, heavy: true);

            // 골드 — 동전 + 숫자(시안 상인 칸 아래 왼쪽)
            var pill = PutPx(F(root, "ShopGoldPill"), panel, 140f, 404f, 170f, 44f);
            Skin(pill, "Components/rb_info_chip");   // 재화 알약(rb_currency_pill)은 오른쪽에 + 칸이 붙어 있어 안 맞는다
            var gold = PutPx(F(root, "ShopGoldText"), panel, 156f, 404f, 120f, 40f);
            Style(gold, 46, Yellow, TA.Left, number: true);
            var coin = panel.Find("ShopGoldCoin") as RectTransform ?? Node(panel, "ShopGoldCoin");
            PutPx(coin, panel, 80f, 404f, 36f, 36f);
            Img(coin, "Icons/rb_icon_currency_gold_48");
            Mute(root, "ShopLimitText", "ShopResultText");

            // 4칸 — 2열 × 2줄(카드 2 · 몸 또는 소모품 · 회복, BattleDirector.MerchantSlots). 칸 = 아이콘 · 이름 · 설명 · 값 알약
            // 5 · 6 째 칸은 안 쓴다(옛 6칸 판) — 꺼 둔다
            for (int i = 4; i < 6; i++) F(root, $"ShopItem{i}")?.gameObject.SetActive(false);
            for (int i = 0; i < 4; i++)
            {
                float cx = i % 2 == 0 ? 205f : 515f, top = 448f + 262f * (i / 2);
                var cell = PutPx(F(root, $"ShopItem{i}"), panel, cx, top + 112f, 300f, 224f);
                if (cell.TryGetComponent<Image>(out var ci)) { ci.sprite = null; ci.color = new Color(1, 1, 1, 0); }
                Inside(F(root, $"ShopItem{i}Icon"), 150f, 46f, 72f, 72f);
                var name = F(root, $"ShopItem{i}Name");
                Inside(name, 150f, 106f, 288f, 40f);
                Style(name, 40, White, TA.Center, heavy: true);
                var desc = F(root, $"ShopItem{i}Desc");
                Inside(desc, 150f, 144f, 288f, 34f);
                Style(desc, 32, Dim, TA.Center);
                var pl = cell.Find($"ShopItem{i}Pill") as RectTransform ?? Node(cell, $"ShopItem{i}Pill");
                Inside(pl, 150f, 194f, 150f, 52f);
                Skin(pl, "Components/rb_btn_price");
                pl.SetSiblingIndex(0);
                var pc = cell.Find($"ShopItem{i}Coin") as RectTransform ?? Node(cell, $"ShopItem{i}Coin");
                Inside(pc, 108f, 194f, 34f, 34f);
                Img(pc, "Icons/rb_icon_currency_gold_48");
                var price = F(root, $"ShopItem{i}Price");
                price.SetAsLastSibling();
                Inside(price, 168f, 194f, 90f, 40f);
                Style(price, 44, White, TA.Left, number: true);
            }
            var leave = PutPx(F(root, "ShopLeaveButton"), panel, 360f, 1018f, 580f, 74f);
            Skin(leave, "Components/rb_btn_secondary");
            SwapPressed(leave, "Components/rb_btn_secondary_pressed");
            var leaveText = F(root, "ShopLeaveText");
            Style(leaveText, 44, White, TA.Center, heavy: true);
            if (!leaveText.TryGetComponent<Game.Module.Common.LocalizedText>(out var leaveLoc)) leaveLoc = leaveText.gameObject.AddComponent<Game.Module.Common.LocalizedText>();
            leaveLoc.SetKey("ui.rb.shop.leave");   // 시안 「지나간다」
            Text(PutPx(Node((RectTransform)panel, "ShopNever"), panel, 360f, 1082f, 580f, 36f), "ui.rb.shop.never", "상인은 다시 오지 않아요", 32, Dim, TA.Center);
        }

        // 이벤트 판 — 「내준다 / 받는다」 두 줄(PD 2026-10-27 「지금 시안이 좋은데 시안대로」 · 시안 _exchange/mock_altar/mock_event_deal.png).
        // 자리 숫자는 시안 판(360 × 640)을 두 배로 옮긴 게임 px. 대가가 없으면 화면 스크립트가 받는다 줄 · 버튼을 위로 당긴다(InGameMainUI.RbEvent).
        private const float DealRowX = 44f, DealRowW = 632f, DealGiveTop = 644f, DealRowH = 124f, DealGap = 12f;

        private static void BuildEvent(Transform root)
        {
            var panel = CenterPanel(root, "EventPanel");
            DropFrameFx(panel, "chain", "flame");
            var box = PutPx(F(root, "EventBox"), panel, 360f, 684f, 680f, 1000f);
            Skin(box, "Components/rb_modal");
            TitlePlate(panel, F(root, "EventTitleText"), 56f, 154f, 584f, 218f);
            // 닫기 = 지나간다(시안 event.png 오른쪽 위 X)
            var close = panel.Find("EventCloseButton") as RectTransform ?? Node(panel, "EventCloseButton");
            PutPx(close, panel, 668f, 185f, 72f, 70f);
            Skin(close, "Components/rb_btn_icon_round");
            if (close.GetComponent<Button>() == null) close.gameObject.AddComponent<Button>();
            var closeIcon = close.Find("EventCloseIcon") as RectTransform ?? Node(close, "EventCloseIcon");
            Inside(closeIcon, 36f, 35f, 34f, 34f);
            Img(closeIcon, "Icons/rb_icon_sys_close_48");
            // 이벤트 그림(키트 rb_art_ev_*) — 화면 스크립트가 이벤트마다 꽂는다
            var art = panel.Find("EventArt") as RectTransform ?? Node(panel, "EventArt");
            PutPx(art, panel, 360f, 386f, 632f, 300f);
            var artImg = art.GetComponent<Image>() ?? art.gameObject.AddComponent<Image>();
            artImg.preserveAspect = true;
            artImg.raycastTarget = false;
            art.SetSiblingIndex(box.GetSiblingIndex() + 1);
            var body = PutPx(F(root, "EventBodyText"), panel, 360f, 594f, 624f, 84f);
            Style(body, 36, White, TA.TopLeft);
            if (body.TryGetComponent<TextMeshProUGUI>(out var bt)) bt.enableWordWrapping = true;
            // 옛 알약 두 줄(받는 것 · 대가)은 두 줄 판이 대신한다 — 화면 스크립트도 RE:BORN 이면 켜지 않는다
            Mute(root, "EventRewardPill", "EventRewardText", "EventCostPill", "EventCostText");
            DealRow(panel, "EventGive", DealGiveTop, true);
            DealRow(panel, "EventGet", DealGiveTop + DealRowH + DealGap, false);
            float buttons = DealGiveTop + (DealRowH + DealGap) * 2f + 16f;
            var accept = PutPx(F(root, "EventAcceptButton"), panel, 360f, buttons + 40f, DealRowW, 80f);
            Skin(accept, "Components/rb_btn_primary");
            SwapPressed(accept, "Components/rb_btn_primary_pressed");
            Style(F(root, "EventAcceptText"), 44, new Color(0.17f, 0.1f, 0f), TA.Center, heavy: true);
            var decline = PutPx(F(root, "EventDeclineButton"), panel, 360f, buttons + 136f, DealRowW, 80f);
            Skin(decline, "Components/rb_btn_secondary");
            SwapPressed(decline, "Components/rb_btn_secondary_pressed");
            Style(F(root, "EventDeclineText"), 44, White, TA.Center, heavy: true);
            if (F(root, "EventDeclineText")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI dt) dt.richText = true;
            PutPx(F(root, "EventResultText"), panel, 360f, buttons + 40f, DealRowW, 80f);
            Style(F(root, "EventResultText"), 38, White, TA.Center);
        }

        /// <summary>
        /// 거래 한 줄 — 왼쪽 띠(내준다 빨강 · 받는다 초록) · 아이콘 · 제목 · 아래 줄 · 오른쪽 위 표(확률 · 이 판 내내).
        /// 노드 이름 = 화면 스크립트 바인딩 키: {name}Row · Key · Icon · Title · Sub · Tag · TagText.
        /// </summary>
        private static void DealRow(RectTransform panel, string name, float top, bool give)
        {
            // 글자 컴포넌트를 새로 붙이므로 다시 세울 때마다 통째로 새로 만든다(같은 곳에 두 번 붙이면 실패한다)
            if (panel.Find(name + "Row") is Transform old) Object.DestroyImmediate(old.gameObject);
            var row = Node(panel, name + "Row");
            PutPx(row, panel, DealRowX + DealRowW * 0.5f, top + DealRowH * 0.5f, DealRowW, DealRowH);
            Skin(row, "Components/rb_panel_base");
            // 판 그림 남색에 붉은 기(내준다) · 푸른 기(받는다)만 곱한다
            if (row.TryGetComponent<Image>(out var ri)) ri.color = give ? new Color(1f, 0.8f, 0.88f) : new Color(0.8f, 1f, 0.95f);
            var line = row.Find(name + "Line") as RectTransform ?? Node(row, name + "Line");
            Stretch(line);
            Skin(line, "Components/rb_host_frame_ghost");
            if (line.TryGetComponent<Image>(out var li)) li.color = give ? new Color(0.62f, 0.25f, 0.34f) : new Color(0.2f, 0.52f, 0.43f);
            var key = row.Find(name + "Key") as RectTransform ?? Node(row, name + "Key");
            key.anchorMin = new Vector2(0f, 0f); key.anchorMax = new Vector2(0f, 1f); key.pivot = new Vector2(0f, 0.5f);
            key.sizeDelta = new Vector2(68f, -6f); key.anchoredPosition = new Vector2(3f, 0f);
            Skin(key, "Components/rb_panel_base");
            if (key.TryGetComponent<Image>(out var ki)) ki.color = give ? new Color(1.6f, 0.75f, 1f) : new Color(0.7f, 1.5f, 1.25f);
            var keyText = key.Find(name + "KeyText") as RectTransform ?? Node(key, name + "KeyText");
            Stretch(keyText);
            Text(keyText, null, give ? "내\n준\n다" : "받\n는\n다", 30,   // 글자는 화면 스크립트가 넣는다(대가 없음 줄은 가로로)
                 give ? new Color(1f, 0.54f, 0.61f) : new Color(0.42f, 0.94f, 0.75f), TA.Center, heavy: true);
            var icon = row.Find(name + "Icon") as RectTransform ?? Node(row, name + "Icon");
            Inside(icon, 124f, 62f, 80f, 80f);
            var ib = icon.GetComponent<Image>() ?? icon.gameObject.AddComponent<Image>();
            ib.preserveAspect = true; ib.raycastTarget = false;
            float textW = DealRowW - 196f, textX = 180f + textW * 0.5f;
            var title = row.Find(name + "Title") as RectTransform ?? Node(row, name + "Title");
            Inside(title, textX, 38f, textW, 40f);
            Text(title, null, "", 44, White, TA.Left, heavy: true);
            var sub = row.Find(name + "Sub") as RectTransform ?? Node(row, name + "Sub");
            Inside(sub, textX, 84f, textW, 32f);
            Text(sub, null, "", 33, Dim, TA.Left);
            if (sub.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI st) st.richText = true;
            if (give)
            {
                // 부서질 수 있는 카드 아이콘 줄(부서진 제단) — 아래 줄 자리에 44 칸 8개
                var cands = Node(row, name + "Cands");
                Inside(cands, textX, 82f, textW, 44f);
                for (int i = 0; i < 8; i++)
                {
                    var c = Node(cands, $"{name}Cand{i}");
                    c.anchorMin = c.anchorMax = c.pivot = new Vector2(0f, 0.5f);
                    c.sizeDelta = new Vector2(44f, 44f);
                    c.anchoredPosition = new Vector2(i * 50f, 0f);
                    Skin(c, "Components/rb_panel_base");
                    var ci = Node(c, $"{name}Cand{i}Icon");
                    Stretch(ci);
                    var cimg = ci.gameObject.AddComponent<Image>();
                    cimg.preserveAspect = true; cimg.raycastTarget = false;
                }
                cands.gameObject.SetActive(false);
            }
            var tag = row.Find(name + "Tag") as RectTransform ?? Node(row, name + "Tag");
            tag.anchorMin = tag.anchorMax = tag.pivot = new Vector2(1f, 1f);
            tag.sizeDelta = new Vector2(120f, 34f); tag.anchoredPosition = new Vector2(-14f, -16f);
            Skin(tag, "Components/rb_panel_base");
            var tagText = tag.Find(name + "TagText") as RectTransform ?? Node(tag, name + "TagText");
            Stretch(tagText);
            Text(tagText, null, "", 30, Yellow, TA.Center, heavy: true);
            tag.gameObject.SetActive(false);
        }

        private static void BuildShrine(Transform root)
        {
            var panel = CenterPanel(root, "ShrinePanel");
            // 프리팹에서 꺼져 있으면 정렬 밴드 캔버스(InGameMainUI.SetupSortingBands)가 순서 0 으로 남아 HUD(20)가 창 위에 그려졌다
            // (2026-10-23 캡처 기록). 켜 두면 Awake 가 밴드를 건 뒤 스스로 끈다
            panel.gameObject.SetActive(true);
            DropFrameFx(panel, "tube", "bubble", "gem");
            Mute(root, "ShrineBox", "ShrineHintPill", "ShrineBodyText");
            var title = PutPx(F(root, "ShrineTitleText"), panel, 360f, 150f, 600f, 64f);
            Style(title, 60, White, TA.Center, heavy: true);
            var hint = PutPx(F(root, "ShrineHintText"), panel, 360f, 204f, 600f, 40f);
            Style(hint, 36, Dim, TA.Center);
            for (int i = 0; i < 3; i++)
            {
                var row = PutPx(F(root, $"ShrineChoice{i}"), panel, 360f, 340f + 220f * i, 660f, 200f);
                Skin(row, "Components/rb_list_row");
                if (row.TryGetComponent<Button>(out var b))
                {
                    b.transition = Selectable.Transition.SpriteSwap;
                    b.spriteState = new SpriteState { pressedSprite = KitSprite("Components/rb_list_row_selected") };
                }
                var t = F(root, $"ShrineChoice{i}Text");
                Inside(t, 400f, 74f, 460f, 52f);
                Style(t, 48, White, TA.Left, heavy: true);
                var d = F(root, $"ShrineChoice{i}Desc");
                Inside(d, 400f, 128f, 460f, 44f);
                Style(d, 36, Dim, TA.Left);
            }
            PutPx(F(root, "ShrineResultText"), panel, 360f, 1060f, 600f, 60f);
            Style(F(root, "ShrineResultText"), 38, White, TA.Center);
            // 시안의 「골라서 확정」 버튼은 없다 — 칸을 누르면 바로 적용(PD 2026-10-27, 레벨업과 같이)
        }

        // ── 결과 화면(스토리 클리어 · 실패) ─────────────────────────

        private static void BuildResult(Transform root)
        {
            // 화면 전체 — 맨 뒤 형제(정렬 밴드 20). 켠 채 저장한다: InGameMainUI Awake 가 밴드를 건 뒤 끈다
            var old = root.Find("RbResultPanel");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var page = Stretch(Node((RectTransform)root, "RbResultPanel"));
            var dim = page.gameObject.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.03f, 0.06f, 1f);
            dim.raycastTarget = true;
            var bg = Stretch(Node(page, "RbResultBg"));
            var bgImg = Img(bg, "Art/rb_art_result_clear_bg");
            bgImg.preserveAspect = false;
            var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 1080f / 2340f;

            // 가운데 판 — 위 = 시안 140 줄. 클리어(result-story)는 2060 줄까지(1280), 실패(result-fail)는 버튼이 2166 까지라 2200 줄까지(1373).
            // 화면보다 길면 같은 배율로 줄인다(RbFitScale) — 16:9 실패에서만 0.93 배
            var c = Node(page, "RbResultContent");
            Lock(c);
            c.anchorMin = c.anchorMax = c.pivot = new Vector2(0.5f, 0.5f);
            c.sizeDelta = new Vector2(720f, ResultClearH);
            c.gameObject.AddComponent<Game.Module.Common.UI.RbFitScale>();
            var slots = new System.Collections.Generic.List<ResultSlot>();

            // 머리 — CHAPTER 1(40 · #54DCEB / 실패 42 · #8FD0D4) · CLEAR / FAILED 워드아트 · 유령
            var chapter = Two(slots, Node(c, "RbResultChapter"), CBox(28, 170, 1052, 220), CBox(40, 170, 1040, 229), 40f, 42f);
            Text(chapter, null, "CHAPTER 1", 40, RsCyan, TA.Center);
            Img(Two(slots, Node(c, "RbResultWord"), CBox(360, 250, 720, 398), CBox(344, 264, 736, 411)), "WordArt/rb_word_word_clear");
            // 탑 · 밤 결과의 머리글(「2F CLEAR」 · 「128 KILLS」) — 워드아트 자리에 글자로(시안 result-tower · result-night)
            Text(Two(slots, Node(c, "RbResultHeadline"), CBox(28, 250, 1052, 398), CBox(28, 250, 1052, 398)), null, "2F CLEAR", 120, White, TA.Center, number: true);
            Img(Two(slots, Node(c, "RbResultGhost"), CBox(444, 440, 636, 632), CBox(444, 431, 636, 623)), "Art/rb_art_mascot_ghost_cheer");

            // 「CH 2 열림!  NEW」 판 180~900 x 690~810 — 글 42 · #54DCEB, NEW 는 도트 글 · #EFC667
            var nw = Two(slots, Node(c, "RbResultNew"), CBox(180, 690, 900, 810), CBox(180, 690, 900, 810));
            Img(nw, "Components/rb_panel_base");
            var nwText = Node(nw, "RbResultNewText");
            Center(nwText, 0f, 680f * S, 58f * S);
            Text(nwText, null, "CH 2 열림!", 42, RsCyan, TA.Center);
            var badge = Node(nw, "RbResultNewBadge");
            Center(badge, 0f, 120f * S, 56f * S);
            Text(badge, null, "NEW", 42, RsGold, TA.Left, number: true);

            // 기록 판 — 클리어 28~1052 x 866~1106(줄 72 · 줄 아래 가는 선) / 실패 40~1040 x 800~1078(줄 85 · 선 없음). 이름 42 · 값 도트 42
            Img(Two(slots, Node(c, "RbResultRecord"), CBox(28, 866, 1052, 1106), CBox(40, 800, 1040, 1078)), "Components/rb_panel_base");
            string[] keys = { "ui.rb.result.rooms", "ui.rb.result.kills", "ui.rb.result.time" };
            string[] ko = { "도달한 방", "처치", "플레이 시간" };
            string[] vals = { "RbResultRoomValue", "RbResultKillValue", "RbResultTimeValue" };
            float[] failTop = { 828f, 913f, 997f };
            for (int i = 0; i < 3; i++)
            {
                float y = 887f + 72f * i, fy = failTop[i];
                Text(Two(slots, Node(c, $"RbResultRecLabel{i}"), CBox(58, y, 600, y + 53), CBox(75, fy, 600, fy + 52)), keys[i], ko[i], 42, RsMuted, TA.Left);
                Text(Two(slots, Node(c, vals[i]), CBox(600, y, 1022, y + 53), CBox(600, fy - 2, 1005, fy + 54)), null, "16/16", 42, RsInk, TA.Right, number: true);
                float ly = 949.5f + 72f * i;
                var line = Two(slots, Node(c, $"RbResultRecLine{i}"), CBox(58, ly - 0.75f, 1022, ly + 0.75f), CBox(58, ly - 0.75f, 1022, ly + 0.75f));
                Img(line, "Components/rb_divider_line").preserveAspect = false;
            }

            // 획득 보상 — 골드 · 유령 경험치 가로 두 칸 + 아래 몸 카드 줄(PD 2026-10-27 A안 「몸 카드 줄」 · 시안 _exchange/ref/shard_mock/shard_option_1.png).
            // 여러 몸의 조각을 함께 받는다 — 카드 한 장이 몸 하나(등급 테 · 꼬리표 · 얼굴 · 이름 · 조각 ×n). 5장이 넘으면 옆으로 민다.
            // 「몸 조각 N종 · 합계」 줄은 없다(PD 같은 날 「제거해줘」)
            Text(Two(slots, Node(c, "RbResultRewardTitle"), CBox(28, 1146, 600, 1204), CBox(40, 1113, 600, 1173)), "ui.rb.result.rewards", "획득 보상", 48, RsInk, TA.Left, heavy: true);
            Cell(slots, c, "RbResultGoldTile", CBox(28, 1222, 528, 1372), CBox(40, 1190, 528, 1340), "Icons/rb_icon_currency_gold_144", "RbResultGoldValue", "RbResultGoldLabel", "ui.rb.result.gold", "골드");
            Cell(slots, c, "RbResultExpTile", CBox(552, 1222, 1052, 1372), CBox(552, 1190, 1040, 1340), "Icons/rb_icon_stat_attack_144", "RbResultExpValue", "RbResultExpLabel", "ui.rb.result.ghost_exp", "유령 경험치");
            BuildHostCards(slots, c);

            // 실패 — 성장 추천 판 40~1040 x 1720~1908(얼굴 · 「공격력 강화 추천」 · 「가장 오래 쓴 몸 · 아마존」 · 성장 ›)
            var tip = Two(slots, Node(c, "RbResultTip"), CBox(40, 1720, 1040, 1908), CBox(40, 1720, 1040, 1908));
            Img(tip, "Components/rb_panel_base");
            Img(Inside2(Node(tip, "RbResultTipFace"), 23f * S, 30f * S, 128f * S, 128f * S), "Icons/rb_icon_mode_ghost_96");
            Text(Inside2(Node(tip, "RbResultTipTitle"), 172f * S, 42f * S, 600f * S, 50f * S), "ui.rb.result.tip_title", "공격력 강화 추천", 40, RsInk, TA.Left, heavy: true);
            Text(Inside2(Node(tip, "RbResultTipSub"), 172f * S, 99f * S, 600f * S, 43f * S), null, "가장 오래 쓴 몸 · 아마존", 36, RsMuted2, TA.Left);
            var tipGo = Inside2(Node(tip, "RbResultTipGo"), 795f * S, 21f * S, 183f * S, 144f * S);
            Btn(tipGo, "Components/rb_btn_secondary");
            Text(Stretch(Node(tipGo, "RbResultTipGoText")), "ui.rb.result.tip_go", "성장 ›", 44, RsBtnText, TA.Center, heavy: true);

            // 로비로 · 다음 챕터 ▶(다시 도전 | ⚡5)
            var lobby = Two(slots, Node(c, "RbResultLobby"), CBox(28, 1870, 380, 2030), CBox(28, 1993, 514, 2163));
            Btn(lobby, "Components/rb_btn_secondary");
            Text(Stretch(Node(lobby, "RbResultLobbyText")), "ui.rb.result.lobby", "로비로", 44, RsBtnText, TA.Center, heavy: true);
            var next = Two(slots, Node(c, "RbResultNext"), CBox(404, 1870, 1052, 2030), CBox(534, 1990, 1052, 2166));
            Btn(next, "Components/rb_btn_primary");
            Text(Two(slots, Node(next, "RbResultNextText"), TBox(0, 0, 648, 160), TBox(0, 0, 290, 176), 56f, 44f), null, "다음 챕터 ▶", 56, RsBtnInk, TA.Center, heavy: true);
            var cost = Node(next, "RbResultNextCost");
            Corner(cost, 0f, 0f, 518f * S, 176f * S);
            var costLine = Inside2(Node(cost, "RbResultNextCostLine"), 290f * S, 53f * S, 3f * S, 70f * S);
            Img(costLine, "Components/rb_divider_line").preserveAspect = false;
            costLine.GetComponent<Image>().color = new Color32(80, 40, 0, 64);   // 시안 border-left 3px rgba(80,40,0,.25)
            Img(Inside2(Node(cost, "RbResultNextCostIcon"), 313f * S, 64f * S, 48f * S, 48f * S), "Icons/rb_icon_currency_energy_48");
            Text(Inside2(Node(cost, "RbResultNextCostValue"), 367f * S, 51f * S, 120f * S, 74f * S), null, "5", 56, RsBtnInk, TA.Left, number: true);

            // 실패 배치를 한 번 입혀 보고 잰 뒤 클리어 배치로 되돌린다 — 둘 다 RbRunResult 에 담긴다
            var view = page.gameObject.AddComponent<Game.Module.InGame.RbRunResult>();
            var so = new SerializedObject(view);
            so.FindProperty("_bgClear").objectReferenceValue = KitSprite("Art/rb_art_result_clear_bg");
            so.FindProperty("_bgStory").objectReferenceValue = KitSprite("Art/rb_art_result_clear_bg_quiet");
            so.FindProperty("_bgFail").objectReferenceValue = KitSprite("Art/rb_art_result_fail_bg");
            so.FindProperty("_wordClear").objectReferenceValue = KitSprite("WordArt/rb_word_word_clear");
            so.FindProperty("_wordFailed").objectReferenceValue = KitSprite("WordArt/rb_word_word_failed");
            so.FindProperty("_ghostCheer").objectReferenceValue = KitSprite("Art/rb_art_mascot_ghost_cheer");
            so.FindProperty("_ghostSad").objectReferenceValue = KitSprite("Art/rb_art_mascot_ghost_sad");
            so.FindProperty("_cellClear").objectReferenceValue = KitSprite("Components/rb_result_cell");
            so.FindProperty("_cellGold").objectReferenceValue = KitSprite("Components/rb_cell_line_gold");
            so.FindProperty("_cellTeal").objectReferenceValue = KitSprite("Components/rb_cell_line_teal");
            so.FindProperty("_expClear").objectReferenceValue = KitSprite("Icons/rb_icon_stat_attack_144");
            so.FindProperty("_expFail").objectReferenceValue = KitSprite("Icons/rb_icon_ghost_xp_144");
            // 몸 카드 등급 — HostGrade 순서(B · A · S = 화면 R · SR · SSR)
            string[] grades = { "r", "sr", "ssr" };
            var frames = so.FindProperty("_hostFrames");
            var tags = so.FindProperty("_gradeTags");
            frames.arraySize = tags.arraySize = grades.Length;
            for (int g = 0; g < grades.Length; g++)
            {
                frames.GetArrayElementAtIndex(g).objectReferenceValue = KitSprite($"Components/rb_host_frame_{grades[g]}");
                tags.GetArrayElementAtIndex(g).objectReferenceValue = KitSprite($"Components/rb_rarity_{grades[g]}");
            }
            so.FindProperty("_clearHeight").floatValue = ResultClearH;
            so.FindProperty("_failHeight").floatValue = ResultFailH;
            var sp = so.FindProperty("_slots");
            sp.arraySize = slots.Count;
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                var e = sp.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Node").objectReferenceValue = s.Rt;
                e.FindPropertyRelative("ClearPos").vector2Value = s.Clear.pos;
                e.FindPropertyRelative("ClearSize").vector2Value = s.Clear.size;
                e.FindPropertyRelative("FailPos").vector2Value = s.Fail.pos;
                e.FindPropertyRelative("FailSize").vector2Value = s.Fail.size;
                e.FindPropertyRelative("ClearFont").floatValue = s.ClearFont * S;
                e.FindPropertyRelative("FailFont").floatValue = s.FailFont * S;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            tip.gameObject.SetActive(false);
            cost.gameObject.SetActive(false);
            badge.gameObject.SetActive(false);
        }

        // 결과 시안 색(브라우저에서 잰 값) — 시안 #54DCEB · NEW #EFC667 · 흐린 글 #ABB2CE / 실패 #AEB4C8 · 흰 글 #EDF0F6 · 버튼 #F2F4FF · 노란 버튼 글 #302008
        private static readonly Color RsCyan = new Color32(84, 220, 235, 255);
        private static readonly Color RsGold = new Color32(239, 198, 103, 255);
        private static readonly Color RsMuted = new Color32(171, 178, 206, 255);
        private static readonly Color RsMuted2 = new Color32(174, 180, 200, 255);
        private static readonly Color RsInk = new Color32(237, 240, 246, 255);
        private static readonly Color RsBtnText = new Color32(242, 244, 255, 255);
        private static readonly Color RsBtnInk = new Color32(48, 32, 8, 255);
        private const float ResultClearH = (2060f - 140f) * S, ResultFailH = (2200f - 140f) * S;

        private struct ResultSlot
        {
            public RectTransform Rt;
            public (Vector2 pos, Vector2 size) Clear, Fail;
            public float ClearFont, FailFont;
        }

        /// <summary>판 안 상자(시안 1080 · 판 위 = 140 줄) → 위 왼쪽 앵커 · 가운데 피벗 자리.</summary>
        private static (Vector2 pos, Vector2 size) CBox(float x0, float y0, float x1, float y1)
            => (new Vector2((x0 + x1) * 0.5f * S, -((y0 + y1) * 0.5f - 140f) * S), new Vector2((x1 - x0) * S, (y1 - y0) * S));

        /// <summary>칸 안 상자(칸 위 왼쪽 기준 시안 px) → 위 왼쪽 앵커 · 위 왼쪽 피벗 자리.</summary>
        private static (Vector2 pos, Vector2 size) TBox(float x, float y, float w, float h)
            => (new Vector2(x * S, -y * S), new Vector2(w * S, h * S));

        /// <summary>두 배치를 담고 클리어 자리에 놓는다. CBox 는 가운데 피벗, TBox 는 위 왼쪽 피벗 — 자리 값의 뜻에 맞춰 피벗을 고른다.</summary>
        private static RectTransform Two(System.Collections.Generic.List<ResultSlot> slots, RectTransform rt,
                                         (Vector2 pos, Vector2 size) clear, (Vector2 pos, Vector2 size) fail,
                                         float clearFont = 0f, float failFont = 0f)
        {
            bool corner = rt.parent != null && rt.parent.name != "RbResultContent";
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = corner ? new Vector2(0f, 1f) : new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.anchoredPosition = clear.pos;
            rt.sizeDelta = clear.size;
            slots.Add(new ResultSlot { Rt = rt, Clear = clear, Fail = fail, ClearFont = clearFont, FailFont = failFont });
            return rt;
        }

        /// <summary>부모 가운데에 놓는다(가로 offset · 폭 · 높이).</summary>
        private static void Center(RectTransform rt, float x, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, 0f);
        }

        // ── 일시정지 · 포기 확인 ─────────────────────────────────

        // 일시정지 카드 칸 테두리(납품 254 × 259)를 132 × 152 칸에 — 9분할 경계를 반으로 줄여 그린다(모서리 보석이 시안 크기)
        private const float PauseFrameScale = 1.92f;

        private static void BuildPause(Transform root)
        {
            var old = root.Find("RbPausePanel");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            // 켠 채 저장(정렬 밴드) — InGameMainUI Awake 가 끈다
            var page = Stretch(Node((RectTransform)root, "RbPausePanel"));
            var dim = page.gameObject.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.03f, 0.06f, RbExtraScreensBuilder.InGameDim);   // 0.86 은 인게임에서 거의 안 어두워졌다
            dim.raycastTarget = true;
            var c = Node(page, "RbPauseContent");
            c.anchorMin = c.anchorMax = c.pivot = new Vector2(0.5f, 0.5f);
            c.sizeDelta = new Vector2(720f, 1280f);
            Lock(c);

            // 자리는 시안 mock_pause_panel_free_peak(720 × 1280 그대로) 를 잰 값 — 가운데 x · y, 폭 · 높이
            var box = PutPx(Node(c, "RbPauseBox"), c, 360f, 699f, 640f, 942f);
            Img(box, "Components/rb_modal");
            var title = Node(c, "RbPauseTitle");
            TitlePlate(c, title, 222f, 200f, 498f, 256f);
            if (title.GetComponent<TextMeshProUGUI>() == null) Text(title, "ui.rb.pause.title", "일시정지", 46, White, TA.Center, heavy: true);
            CloseX(c, "RbPauseClose", 636f, 228f);

            Text(PutPx(Node(c, "RbPauseStage"), c, 200f, 292f, 260f, 56f), null, "1-07 /30", 66, White, TA.Left, number: true);
            Text(PutPx(Node(c, "RbPauseMode"), c, 560f, 295f, 184f, 36f), "ui.rb.pause.story", "스토리", 32, Dim, TA.Right);

            // ① 지금 빌드 — 카드 4 × 2(132 × 152, 간격 147 · 168). 등급 칸 · 위 등급 글자 · 그림 · 이름 · 오른쪽 아래 Lv
            Img(PutPx(Node(c, "RbPauseBuildBox"), c, 361f, 515f, 606f, 380f), "Components/rb_panel_base");
            Img(PutPx(Node(c, "RbPauseBuildStar"), c, 88f, 345f, 26f, 26f), "Icons/rb_icon_star_on_48");
            Text(PutPx(Node(c, "RbPauseBuildTitle"), c, 190f, 345f, 160f, 36f), "ui.rb.pause.build", "지금 빌드", 38, White, TA.Left, heavy: true);
            for (int i = 0; i < 8; i++)
            {
                float x0 = 76f + 147f * (i % 4), y0 = 370f + 168f * (i / 4);
                var slot = PutPx(Node(c, $"RbPauseCard{i}"), c, x0 + 66f, y0 + 76f, 132f, 152f);
                var empty = Stretch(Node(slot, $"RbPauseCard{i}Empty"));
                var emptyImg = Img(empty, "Components/rb_pause_card_frame_rare");
                emptyImg.color = new Color(0.55f, 0.6f, 0.7f, 0.3f);   // 시안 빈 칸 — 같은 카드 칸을 흐리게
                emptyImg.pixelsPerUnitMultiplier = PauseFrameScale;
                Img(Inside2(Node(empty, $"RbPauseCard{i}Plus"), 42f, 52f, 48f, 48f), "Icons/rb_icon_sys_plus_48").color = new Color(0.55f, 0.6f, 0.72f, 0.35f);
                var full = Stretch(Node(slot, $"RbPauseCard{i}Full"));
                // 카드 칸 테두리 — 시안 그대로 발주한 rb_pause_card_frame(등급 넷 · 위 등급 이름판 · 모서리 보석)
                Img(Stretch(Node(full, $"RbPauseCard{i}Frame")), "Components/rb_pause_card_frame_rare").pixelsPerUnitMultiplier = PauseFrameScale;
                Text(Inside2(Node(full, $"RbPauseCard{i}Rarity"), 24f, 4f, 84f, 18f), null, "RARE", 26, LvMuted, TA.Center, heavy: true).fontSizeMin = 8f;   // 「LEGENDARY」 도 이름판 안에
                Img(Inside2(Node(full, $"RbPauseCard{i}Icon"), 22f, 24f, 88f, 88f), "Icons/rb_icon_card_up_96");
                Text(Inside2(Node(full, $"RbPauseCard{i}Name"), 8f, 108f, 116f, 20f), null, "수호 방패", 30, White, TA.Center, heavy: true).fontSizeMin = 12f;   // Lv 배지(위 128)와 안 겹치게
                var lvTag = Inside2(Node(full, $"RbPauseCard{i}LvTag"), 82f, 128f, 54f, 27f);
                Img(lvTag, "Components/rb_pause_lv_badge").pixelsPerUnitMultiplier = 1.6f;
                Text(Stretch(Node(lvTag, $"RbPauseCard{i}Lv")), null, "Lv 2", 26, White, TA.Center, number: true);
            }

            // ② 호스트 — 큰 얼굴(등급 테두리) · 이름 + 등급 · 체력 줄 · 속성 꼬리표 · 액티브 / 필살기
            Img(PutPx(Node(c, "RbPauseBodyBox"), c, 361f, 851f, 606f, 267f), "Components/rb_panel_base");
            Img(PutPx(Node(c, "RbPauseBodyStar"), c, 88f, 738f, 26f, 26f), "Icons/rb_icon_star_on_48");
            Text(PutPx(Node(c, "RbPauseBodyTitle"), c, 190f, 738f, 160f, 36f), "ui.rb.pause.body", "호스트", 38, White, TA.Left, heavy: true);
            // 얼굴 칸 — 어두운 바탕 · 얼굴 · 시안 그대로 발주한 테두리(rb_pause_host_frame · 모서리 보석)
            Img(PutPx(Node(c, "RbPauseHostBack"), c, 176f, 868f, 188f, 188f), "Components/rb_panel_base");
            Img(PutPx(Node(c, "RbPauseHostFace"), c, 176f, 868f, 184f, 184f), "Icons/rb_icon_mode_ghost_96");
            Img(PutPx(Node(c, "RbPauseHostFrame"), c, 176f, 868f, 212f, 212f), "Components/rb_pause_host_frame_r");
            // 얼굴 모서리 — 왼쪽 위 속성 · 오른쪽 위 등급(PD 2026-10-27)
            Img(PutPx(Node(c, "RbPauseHostAffCorner"), c, 100f, 792f, 52f, 52f), "Icons/rb_icon_aff_force_128");   // 크게 — PD 2026-10-27 「너무 안 보인다」
            Img(PutPx(Node(c, "RbPauseHostGradeSmall"), c, 246f, 790f, 62f, 40f), "Components/rb_rarity_r");
            Text(PutPx(Node(c, "RbPauseHostName"), c, 477f, 775f, 342f, 40f), null, "갱스터", 42, White, TA.Left, heavy: true);   // 등급은 얼굴 오른쪽 위로 옮겼다 — 이름 칸을 넓게
            var hp = PutPx(Node(c, "RbPauseHp"), c, 477f, 816f, 342f, 34f);   // 시안 체력 줄 — 높이 30 · 숫자가 줄 안에 크게
            Img(Inside2(Node(hp, "RbPauseHpHeart"), -2f, -1f, 36f, 36f), "Icons/rb_icon_stat_hp_48");
            Img(Inside2(Node(hp, "RbPauseHpTrack"), 38f, 2f, 304f, 30f), "Components/rb_pause_hp_track").preserveAspect = false;   // 시안 그대로 발주한 막대(두툼한 평면 빨강)
            var fill = Img(Inside2(Node(hp, "RbPauseHpFill"), 41f, 5f, 298f, 24f), "Components/rb_pause_hp_fill");
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.preserveAspect = false;
            Text(Inside2(Node(hp, "RbPauseHpText"), 52f, 0f, 280f, 34f), null, "182 / 207", 38, White, TA.Left, number: true);   // 시안은 막대 안 왼쪽
            var aff = PutPx(Node(c, "RbPauseAff"), c, 477f, 863f, 342f, 43f);
            Img(aff, "Components/rb_info_chip");
            Img(Inside2(Node(aff, "RbPauseAffIcon"), 12f, 4f, 35f, 35f), "Icons/rb_icon_aff_force_128");
            Text(Inside2(Node(aff, "RbPauseAffText"), 58f, 4f, 270f, 35f), null, "파워", 32, White, TA.Left, heavy: true);
            for (int k = 0; k < 2; k++)
            {
                var sb = PutPx(Node(c, $"RbPauseSkillBox{k}"), c, k == 0 ? 389f : 565f, 933f, 166f, 77f);
                Img(sb, "Components/rb_panel_base");
                var icon = Inside2(Node(sb, $"RbPauseSkill{k}"), 8f, 8f, 61f, 61f);
                Img(icon, "Icons/rb_icon_skill_base_144");
                Text(Inside2(Node(sb, $"RbPauseSkill{k}Label"), 76f, 8f, 84f, 61f), k == 0 ? "ui.rb.growth.act" : "ui.rb.growth.ult",
                     k == 0 ? "액티브" : "필살기", 32, White, TA.Left, heavy: true);
            }

            var settings = PutPx(Node(c, "RbPauseSettings"), c, 221f, 1031f, 242f, 54f);
            Btn(settings, "Components/rb_pause_btn_secondary").image.pixelsPerUnitMultiplier = 2f;   // 시안 그대로 발주한 버튼 판(납품이 칸의 2배)
            Text(Stretch(Node(settings, "RbPauseSettingsText")), "ui.rb.rail.settings", "설정", 40, White, TA.Center, heavy: true);
            var abandon = PutPx(Node(c, "RbPauseAbandon"), c, 498f, 1031f, 242f, 54f);
            Btn(abandon, "Components/rb_pause_btn_secondary").image.pixelsPerUnitMultiplier = 2f;
            Text(Stretch(Node(abandon, "RbPauseAbandonText")), "ui.rb.pause.abandon", "포기하기", 40, Red, TA.Center, heavy: true);
            var cont = PutPx(Node(c, "RbPauseContinue"), c, 360f, 1110f, 524f, 70f);
            Btn(cont, "Components/rb_pause_btn_primary").image.pixelsPerUnitMultiplier = 1.9f;
            Text(Stretch(Node(cont, "RbPauseContinueText")), "ui.rb.pause.continue", "계속하기", 60, Ink, TA.Center, heavy: true);   // 시안 글자 크기

            // 포기 확인 — 한 번 더 묻는다
            var ab = Stretch(Node(c, "RbAbandon"));
            var abDim = ab.gameObject.AddComponent<Image>();
            abDim.color = new Color(0.02f, 0.03f, 0.06f, 0.6f);
            abDim.raycastTarget = true;
            Img(PutPx(Node(ab, "RbAbandonBox"), ab, 360f, 640f, 560f, 250f), "Components/rb_modal");
            var at = Node(ab, "RbAbandonTitle");
            TitlePlate(ab, at, 210f, 500f, 510f, 556f);
            if (at.GetComponent<TextMeshProUGUI>() == null) Text(at, "ui.rb.pause.abandon_title", "스테이지 포기", 42, White, TA.Center, heavy: true);
            CloseX(ab, "RbAbandonClose", 606f, 528f);
            Text(PutPx(Node(ab, "RbAbandonText"), ab, 360f, 610f, 500f, 50f), "ui.rb.pause.abandon_ask", "스테이지를 포기하고 로비로 돌아갈까요?", 36, White, TA.Center);
            var no = PutPx(Node(ab, "RbAbandonConfirm"), ab, 230f, 696f, 200f, 72f);
            Btn(no, "Components/rb_btn_secondary");
            Text(Stretch(Node(no, "RbAbandonConfirmText")), "ui.rb.pause.give_up", "포기", 40, White, TA.Center, heavy: true);
            var yes = PutPx(Node(ab, "RbAbandonCancel"), ab, 460f, 696f, 240f, 80f);
            Btn(yes, "Components/rb_btn_primary");
            Text(Stretch(Node(yes, "RbAbandonCancelText")), "ui.rb.pause.keep", "계속", 46, Ink, TA.Center, heavy: true);
            ab.gameObject.SetActive(false);

            var panel = page.gameObject.AddComponent<Game.Module.InGame.RbPausePanel>();
            // 등급 그림 — 카드(일반 · 희귀 · 영웅 · 전설) · 몸 테두리 · 등급 꼬리표(R · SR · SSR) · 속성(없음 · 무기 · 파워 · 마법)
            var pso = new SerializedObject(panel);
            void Many(string prop, params string[] paths)
            {
                var arr = pso.FindProperty(prop);
                arr.arraySize = paths.Length;
                for (int i = 0; i < paths.Length; i++)
                    arr.GetArrayElementAtIndex(i).objectReferenceValue = paths[i] == null ? null : KitSprite(paths[i]);
            }
            Many("_cardFrames", "Components/rb_pause_card_frame_common", "Components/rb_pause_card_frame_rare",
                 "Components/rb_pause_card_frame_epic", "Components/rb_pause_card_frame_legendary");
            Many("_hostFrames", "Components/rb_pause_host_frame_r", "Components/rb_pause_host_frame_sr", "Components/rb_pause_host_frame_ssr");
            Many("_gradeTags", "Components/rb_rarity_r", "Components/rb_rarity_sr", "Components/rb_rarity_ssr");
            Many("_affinityIcons", null, "Icons/rb_icon_aff_blade_128", "Icons/rb_icon_aff_force_128", "Icons/rb_icon_aff_magic_128");
            pso.FindProperty("_ghostFrame").objectReferenceValue = KitSprite("Components/rb_host_frame_ghost");
            pso.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>오른쪽 위 닫기(X) — 키트 둥근 버튼 + 닫기 아이콘.</summary>
        private static void CloseX(RectTransform parent, string name, float cx, float cy)
        {
            var x = PutPx(Node(parent, name), parent, cx, cy, 66f, 66f);
            Btn(x, "Components/rb_btn_icon_round");
            Img(Inside2(Node(x, name + "Icon"), 15f, 15f, 36f, 36f), "Icons/rb_icon_sys_close_48");
        }

        /// <summary>재화 칸(가로 긴 판) — 아이콘 120 왼쪽 · 값 도트 56 · 이름 36. 클리어 500 x 150 / 실패 488 · 500 x 150(시안 A안).</summary>
        private static RectTransform Cell(System.Collections.Generic.List<ResultSlot> slots, RectTransform c, string name,
                                          (Vector2 pos, Vector2 size) clear, (Vector2 pos, Vector2 size) fail,
                                          string icon, string value, string label, string key, string korean)
        {
            var t = Two(slots, Node(c, name), clear, fail);
            Img(t, "Components/rb_result_cell");
            Img(Inside2(Node(t, name + "Icon"), 40f * S, 15f * S, 120f * S, 120f * S), icon);
            Text(Inside2(Node(t, value), 190f * S, 22f * S, 290f * S, 66f * S), null, "432", 56, RsInk, TA.Left, number: true);
            Text(Inside2(Node(t, label), 192f * S, 92f * S, 290f * S, 44f * S), key, korean, 36, RsMuted, TA.Left);
            return t;
        }

        // 몸 카드 — 시안 1080 px 190 x 280 · 간격 18
        public const float HostCardW = 190f * S, HostCardH = 280f * S, HostCardGap = 18f * S;

        /// <summary>
        /// 몸 카드 줄 — 옆으로 미는 보기 칸(RectMask2D) + 내용 + 꺼 둔 카드 본(RbResultHostCard). 화면 스크립트가 몸 수만큼 복제해 채운다.
        /// 카드: 어두운 칸 · 등급 테(R · SR · SSR) · 꼬리표 · 얼굴 118 · 이름 34 · 조각 아이콘 + 「×n」 도트 36.
        /// </summary>
        private static void BuildHostCards(System.Collections.Generic.List<ResultSlot> slots, RectTransform c)
        {
            var view = Two(slots, Node(c, "RbResultHosts"), CBox(28, 1400, 1052, 1680), CBox(40, 1366, 1040, 1646));
            view.gameObject.AddComponent<RectMask2D>();
            var hit = view.gameObject.AddComponent<Image>(); hit.color = new Color(1f, 1f, 1f, 0f);   // 밀기를 받을 칸(보이지 않음)
            var content = Node(view, "RbResultHostsContent");
            content.anchorMin = new Vector2(0f, 0f); content.anchorMax = new Vector2(0f, 1f); content.pivot = new Vector2(0f, 0.5f);
            content.sizeDelta = new Vector2(HostCardW, 0f); content.anchoredPosition = Vector2.zero;
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content; scroll.viewport = view; scroll.horizontal = true; scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.inertia = true;

            var card = Node(content, "RbResultHostCard");
            Corner(card, 0f, 0f, HostCardW, HostCardH);
            Img(Stretch(Node(card, "RbHostCardBg")), "Components/rb_result_cell");
            Img(Stretch(Node(card, "RbHostCardFrame")), "Components/rb_host_frame_r");
            Img(Inside2(Node(card, "RbHostCardTag"), 10f * S, 10f * S, 71f * S, 48f * S), "Components/rb_rarity_r");
            Img(Inside2(Node(card, "RbHostCardFace"), 36f * S, 44f * S, 118f * S, 118f * S), "Icons/rb_icon_mode_ghost_96");
            // 긴 이름(영어 「Dark Magician」 · 일본어 「ギャングスター」)도 카드 안에 — 줄일 수 있는 하한을 24 까지 낮춘다
            Text(Inside2(Node(card, "RbHostCardName"), 6f * S, 170f * S, 178f * S, 44f * S), null, "아마존", 34, RsInk, TA.Center, heavy: true).fontSizeMin = 24f * S;
            Img(Inside2(Node(card, "RbHostCardShard"), 50f * S, 220f * S, 40f * S, 40f * S), "Icons/rb_icon_shard_48");
            Text(Inside2(Node(card, "RbHostCardCount"), 96f * S, 216f * S, 90f * S, 48f * S), null, "×3", 36, RsInk, TA.Left, number: true);
            card.gameObject.SetActive(false);
        }

        /// <summary>부모 위 왼쪽 기준 상자(게임 px) — Corner 와 같지만 돌려준다.</summary>
        private static RectTransform Inside2(RectTransform rt, float x, float y, float w, float h)
        {
            Corner(rt, x, y, w, h);
            return rt;
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
                if (frames.Contains("emblem")) l.FindPropertyRelative("Y").floatValue = 250f;   // 제목 그림 뒤 — 제목과 같이 Lv 줄 아래로
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
            so.FindProperty("_rbSkillReady").objectReferenceValue = KitSprite("Hud/rb_btn_skill_ready");
            so.FindProperty("_rbPrice").objectReferenceValue = KitSprite("Components/rb_btn_price");
            so.FindProperty("_rbPricePoor").objectReferenceValue = KitSprite("Components/rb_btn_price_poor");
            var arts = AssetDatabase.FindAssets("rb_art_ev_ t:Sprite", new[] { $"{Kit}/Art" });
            var ev = so.FindProperty("_rbEventArts");
            ev.arraySize = arts.Length;
            for (int i = 0; i < arts.Length; i++)
                ev.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(arts[i]));
            // 이벤트 「내준다 / 받는다」 줄 아이콘 — 이름으로 찾는다(InGameMainUI.RbEvent DealIcon)
            string[] deal = { "heal_ghost", "heal_host", "stat_hp", "currency_gold", "room_battle", "room_elite", "card_up",
                              "reward_chest", "possess_range", "shop_discount", "boss_shield", "mode_ghost", "sys_warning" };
            var di = so.FindProperty("_rbDealIcons");
            di.arraySize = deal.Length;
            for (int i = 0; i < deal.Length; i++) di.GetArrayElementAtIndex(i).objectReferenceValue = KitSprite($"Icons/rb_icon_{deal[i]}_96");
            // 다음 방 창의 방 아이콘(rb_icon_room_*_96)
            var rooms = AssetDatabase.FindAssets("rb_icon_room_ t:Sprite", new[] { $"{Kit}/Icons" });
            var ri = so.FindProperty("_rbRoomIcons");
            int n = 0;
            ri.arraySize = rooms.Length;
            foreach (var g in rooms)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (!path.EndsWith("_96.png")) continue;
                ri.GetArrayElementAtIndex(n++).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            ri.arraySize = n;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
