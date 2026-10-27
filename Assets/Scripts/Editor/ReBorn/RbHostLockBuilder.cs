// RE:BORN UI 리뉴얼(2026-10-24) — 미보유 호스트 쪽 + 새 몸 · 각성 보여주기 쪽. 시안 Screens/host-locked · host-unlock-ready · host-new · awaken-6 · awaken-10.png.
// 좌표는 시안 픽셀 상자 그대로(RbBuild.Place · PlaceIn). 이름 = 화면 스크립트(RbLockedPage · RbRevealPage) 키.
using Game.Module.Lobby;
using UnityEditor;
using UnityEngine;
using static Game.Editor.ReBorn.RbBuild;
using TA = TMPro.TextAlignmentOptions;

namespace Game.Editor.ReBorn
{
    public static class RbHostLockBuilder
    {
        private const float T = 430f;   // 미보유 쪽 스크롤 판의 위 줄(시안 y)

        private static readonly (string icon, string key, string ko)[] Stats =
        {
            ("rb_icon_stat_attack", "ui.stat.atk", "공격력"), ("rb_icon_stat_hp", "ui.stat.hp", "최대 HP"),
            ("rb_icon_stat_crit", "ui.stat.crit", "크리 확률"), ("rb_icon_stat_critdamage", "ui.stat.critdmg", "크리 피해"),
            ("rb_icon_stat_defense", "ui.stat.def", "방어력"), ("rb_icon_stat_attackspeed", "ui.stat.atkspeed", "공격 속도"),
            ("rb_icon_stat_range", "ui.stat.range", "사거리"), ("rb_icon_stat_movespeed", "ui.stat.movespeed", "이동 속도"),
        };

        public static void Build(RectTransform rt)
        {
            BuildLocked(rt);
            BuildReveal(rt);
        }

        /// <summary>미보유 호스트 — 실루엣 판 · 안내 두 줄 · 능력치(전부 잠김) · 조각 판 · 해금(모자라면 「조각 n개 부족」).</summary>
        private static void BuildLocked(RectTransform rt)
        {
            var page = RbHostBuilder.Page(rt, "PageLocked");
            RbHostBuilder.Head(page, "Lck", "ui.rb.lock.title", "미보유 호스트", 33, 280, 165, 410, 800, 915);
            var body = VScroll(page, "LckBody", T, 2000, 1890 - T);

            var hero = PlaceIn(Node(body, "LckHero"), T, 31, 452, 1048, 863);
            Img(hero, "Components/rb_panel_hero");
            Img(PlaceIn(Node(body, "LckHostArt"), T, 130, 490, 430, 840), "Art/rb_art_mascot_ghost_happy");
            var chip = PlaceIn(Node(body, "LckChip"), T, 542, 513, 793, 572);
            Img(chip, "Components/rb_info_chip").preserveAspect = false;
            Text(Stretch(Node(chip, "LckChipText")), null, "무기 · 원거리", 42, Cyan, TA.Center, heavy: true);
            Text(PlaceIn(Node(body, "LckName"), T, 540, 592, 1020, 664), null, "갱스터", 80, White, TA.Left, heavy: true);
            Text(PlaceIn(Node(body, "LckNeed"), T, 540, 688, 1020, 738), "ui.rb.lock.need", "해금 필요", 50, Dim, TA.Left, heavy: true);
            var stars = PlaceIn(Node(body, "LckStars"), T, 540, 760, 800, 808);
            for (int i = 0; i < 5; i++)
                Img(Local(Node(stars, $"LckStar{i}"), 540, 760, 800, 808, 543 + i * 52, 762, 589 + i * 52, 806), "Icons/rb_icon_star_off_48");
            var note = Text(PlaceIn(Node(body, "LckNote"), T, 90, 905, 990, 1020), "ui.rb.lock.note", "조각을 모아 해금하면\n스킬 · 능력치 강화가 열립니다", 46, White, TA.Center, heavy: true);
            note.enableWordWrapping = true;

            var stats = PlaceIn(Node(body, "LckStats"), T, 31, 1054, 1048, 1531);
            Img(stats, "Components/rb_panel_base").preserveAspect = false;
            Text(PlaceIn(Node(body, "LckStatsTitle"), T, 52, 1072, 500, 1128), "ui.rb.growth.stat_header", "◆ 능력치", 52, White, TA.Left, heavy: true);
            float[] rows = { 1200, 1291, 1381, 1471 };
            for (int i = 0; i < Stats.Length; i++)
            {
                // 두 칸 — 왼쪽 60~520 · 오른쪽 560~1020. 자물쇠는 제 칸 오른쪽 끝(예전엔 왼쪽 칸 자물쇠가 482 라 오른쪽 칸 아이콘 500 과 겹쳤다, PD 2026-10-28)
                float x0 = i % 2 == 0 ? 60 : 560, y = rows[i / 2];
                Img(PlaceIn(Node(body, $"LckStat{i}Icon"), T, x0, y - 24, x0 + 48, y + 24), $"Icons/{Stats[i].icon}_48");
                Text(PlaceIn(Node(body, $"LckStat{i}Name"), T, x0 + 60, y - 30, x0 + 360, y + 30), Stats[i].key, Stats[i].ko, 46, White, TA.Left, heavy: true);
                Img(PlaceIn(Node(body, $"LckStat{i}Lock"), T, x0 + 410, y - 22, x0 + 454, y + 22), "Icons/rb_icon_sys_lock_48");
            }

            var shard = PlaceIn(Node(body, "LckShard"), T, 31, 1574, 1048, 1883);
            Img(shard, "Components/rb_panel_base").preserveAspect = false;
            Text(PlaceIn(Node(body, "LckShardCount"), T, 60, 1640, 680, 1704), null, "조각 12 /20", 60, White, TA.Left, number: true);
            var where = PlaceIn(Node(body, "LckWhere"), T, 768, 1607, 1018, 1733);
            Btn(where, "Components/rb_btn_secondary");
            Text(Stretch(Node(where, "LckWhereText")), "ui.rb.growth.shard_where", "조각 획득처 ›", 40, White, TA.Center, heavy: true).margin = new Vector4(10f, 0f, 10f, 0f);
            Bar(PlaceIn(Node(body, "LckBar"), T, 60, 1764, 1020, 1784), "LckBarFill", "Components/rb_bar_fill_cyan");
            Text(PlaceIn(Node(body, "LckSources"), T, 60, 1806, 1020, 1852), "ui.rb.lock.sources", "얻는 곳: 소환 · 망령의 탑 · 망령의 밤", 42, Dim, TA.Left, heavy: true);

            var go = Place(Node(page, "LckUnlock"), Edge.Bottom, 28, 2028, 1052, 2167);
            Btn(go, "Components/rb_btn_primary");
            Text(Stretch(Node(go, "LckUnlockText")), null, "해금", 60, Ink, TA.Center, heavy: true);

            page.gameObject.AddComponent<RbLockedPage>();
            page.gameObject.SetActive(false);
        }

        /// <summary>새 몸 · 각성 — 빛 · 큰 몸 · 머리글(색이 종류를 말한다) · 이름 · 칩 또는 별 · 설명 · 확인.</summary>
        private static void BuildReveal(RectTransform rt)
        {
            var page = RbHostBuilder.Page(rt, "PageReveal");
            RbHostBuilder.Head(page, "Rvl", null, "새 몸!", 33, 280, 165, 410, 800, 915);
            page.Find("RvlHelp")?.gameObject.SetActive(false);
            Img(Place(Node(page, "RvlArt"), Edge.Center, 300, 600, 780, 1110), "Art/rb_art_mascot_ghost_happy");
            Text(Place(Node(page, "RvlHead"), Edge.Center, 60, 1264, 1020, 1340), null, "새 몸!", 66, Cyan, TA.Center, number: true);
            Text(Place(Node(page, "RvlName"), Edge.Center, 140, 1356, 940, 1410), null, "갱스터", 46, White, TA.Center, heavy: true);
            var chip = Place(Node(page, "RvlChip"), Edge.Center, 380, 1444, 700, 1500);
            Img(chip, "Components/rb_info_chip").preserveAspect = false;
            Text(Stretch(Node(chip, "RvlChipText")), null, "R · 무기 · 원거리", 36, Cyan, TA.Center, heavy: true);
            var stars = Place(Node(page, "RvlStars"), Edge.Center, 280, 1450, 800, 1556);
            for (int i = 0; i < 5; i++)
                Img(Local(Node(stars, $"RvlStar{i}"), 280, 1450, 800, 1556, 284 + i * 102, 1454, 382 + i * 102, 1552), "Icons/rb_icon_star_on_96");
            var desc = Text(Place(Node(page, "RvlDesc"), Edge.Center, 90, 1560, 990, 1690), null, "뒷골목을 주름잡던 두목.\n권총 한 자루로 길을 연다.", 40, White, TA.Top, heavy: true);
            desc.enableWordWrapping = true;
            var ok = Place(Node(page, "RvlOk"), Edge.Bottom, 30, 1996, 1050, 2166);
            Btn(ok, "Components/rb_btn_primary");
            Text(Stretch(Node(ok, "RvlOkText")), "ui.common.ok", "확인", 60, Ink, TA.Center, heavy: true);

            var view = page.gameObject.AddComponent<RbRevealPage>();
            var so = new SerializedObject(view);
            // 성급 별 — 5성마다 큰 금별 · 그 사이는 작은 은별(PD 2026-10-16 StarRank, 2026-10-26 되돌림)
            so.FindProperty("_rankGold").objectReferenceValue = KitSprite("Icons/rb_icon_rank_gold_64");
            so.FindProperty("_rankSilver").objectReferenceValue = KitSprite("Icons/rb_icon_rank_silver_64");
            so.ApplyModifiedPropertiesWithoutUndo();
            page.gameObject.SetActive(false);
        }
    }
}
