// RE:BORN UI 리뉴얼(2026-10-24) — 챕터 선택 쪽. 시안 Screens/chapter-select.png(1080 × 2340).
// 줄은 틀 하나(ChsRow)만 세우고 실행 때 챕터 수만큼 복제한다 — 챕터는 100개 넘게 늘어난다(번호를 박지 않는다).
using Game.Module.Lobby;
using UnityEngine;
using UnityEngine.UI;
using static Game.Editor.ReBorn.RbBuild;
using TA = TMPro.TextAlignmentOptions;

namespace Game.Editor.ReBorn
{
    public static class RbChapterBuilder
    {
        private const float T = 430f;                                    // 스크롤 판 위 줄(시안 y)
        private const float X0 = 30, Y0 = 458, X1 = 1050, Y1 = 692;      // 첫 줄 상자 — 줄 간격 264(RbChapterPage.RowPitch)

        public static void Build(RectTransform rt)
        {
            var page = RbHostBuilder.Page(rt, "PageChapters");
            RbHostBuilder.Head(page, "Chs", "ui.rb.chs.title", "챕터 선택", 35, 280, 165, 410, 800, 915);
            var body = VScroll(page, "ChsBody", T, 2230, 264f * 6f + 60f);

            var row = PlaceIn(Node(body, "ChsRow"), T, X0, Y0, X1, Y1);
            Btn(row, "Components/rb_list_row").GetComponent<Image>().preserveAspect = false;
            Img(Stretch(Node(row, "ChsRowSel")), "Components/rb_list_row_selected").preserveAspect = false;
            // 섬네일 — 무대 그림(1080 × 2520)의 거리 · 물 반사 쪽을 네모로 잘라 보인다
            var thumb = Local(Node(row, "ChsRowThumb"), X0, Y0, X1, Y1, 52, 495, 212, 655);
            thumb.gameObject.AddComponent<RectMask2D>();
            var art = Node(thumb, "ChsRowThumbArt");
            art.anchorMin = art.anchorMax = new Vector2(0.5f, 0.5f); art.pivot = new Vector2(0.5f, 0.5f);
            art.sizeDelta = new Vector2(160f * S, 373f * S); art.anchoredPosition = new Vector2(0f, -60f * S);
            Img(art, "Art/rb_art_bg_stage_ch01").preserveAspect = false;
            Text(Local(Node(row, "ChsRowNo"), X0, Y0, X1, Y1, 218, 495, 420, 555), null, "CH 1", 52, new Color(0.42f, 0.85f, 0.92f), TA.Left, number: true);
            Text(Local(Node(row, "ChsRowName"), X0, Y0, X1, Y1, 318, 492, 860, 556), null, "쓰레기 집적장", 56, White, TA.Left, heavy: true);
            Text(Local(Node(row, "ChsRowState"), X0, Y0, X1, Y1, 760, 490, 1028, 538), "ui.rb.chs.open", "도전 가능", 46, new Color(0.42f, 0.85f, 0.92f), TA.Right, heavy: true);
            Img(Local(Node(row, "ChsRowLockIcon"), X0, Y0, X1, Y1, 846, 492, 890, 536), "Icons/rb_icon_sys_lock_48");
            Text(Local(Node(row, "ChsRowLockText"), X0, Y0, X1, Y1, 896, 490, 1028, 538), "ui.rb.chs.locked", "잠김", 46, Dim, TA.Right, heavy: true);
            Text(Local(Node(row, "ChsRowDesc"), X0, Y0, X1, Y1, 220, 558, 1020, 604), null, "도시가 버린 것들이 쌓이는 곳", 44, White, TA.Left, heavy: true);
            Text(Local(Node(row, "ChsRowBest"), X0, Y0, X1, Y1, 220, 618, 1020, 664), null, "최고 7/30방", 44, Dim, TA.Left, heavy: true);

            page.gameObject.AddComponent<RbChapterPage>();
            page.gameObject.SetActive(false);
        }
    }
}
