// RE:BORN UI 리뉴얼(2026-10-24) — 랜덤 출전 공개 쪽. 시안 Screens/random-shuffle · random-reveal · hidden-reveal.png(1080 × 2340).
// 한 쪽에 두 박자 — 「RANDOM · 어떤 몸과 만날까요? · ? · 공개하기」 → 「RANDOM(HIDDEN!) · 이번 판의 호스트 · 등급 이름 · 칩 · 전투 시작」.
using Game.Module.Lobby;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Game.Editor.ReBorn.RbBuild;
using TA = TMPro.TextAlignmentOptions;

namespace Game.Editor.ReBorn
{
    public static class RbRandomBuilder
    {
        public const int Chips = 2;

        public static void Build(RectTransform rt)
        {
            var page = Stretch(Node(rt, "PageRandom"));
            var bg = Stretch(Node(page, "RndBg"));
            var img = Img(bg, "Art/rb_art_bg_stage_ch01");
            img.preserveAspect = false;
            img.raycastTarget = true;   // 뒤 로비를 누르지 못하게
            var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 1080f / 2520f;
            var fade = Stretch(Node(page, "RndFade"));
            Img(fade, "Components/rb_fade_bottom").preserveAspect = false;
            page.gameObject.AddComponent<RectMask2D>();

            Img(Place(Node(page, "RndWord"), Edge.Top, 300, 330, 780, 470), "WordArt/rb_word_word_random");
            Text(Place(Node(page, "RndSub"), Edge.Top, 90, 495, 990, 565), null, "어떤 몸과 만날까요?", 52, White, TA.Center, heavy: true);
            Img(Place(Node(page, "RndFace"), Edge.Center, 390, 760, 690, 1060), "Icons/rb_icon_mode_random_144");
            Text(Place(Node(page, "RndQuestion"), Edge.Center, 440, 1270, 640, 1420), null, "?", 140, Yellow, TA.Center, number: true);
            Text(Place(Node(page, "RndHint"), Edge.Center, 90, 1430, 990, 1485), "ui.rb.rnd.hint", "몸은 판이 시작될 때 정해져요.", 40, Dim, TA.Center, heavy: true);
            Text(Place(Node(page, "RndName"), Edge.Center, 90, 1255, 990, 1325), null, "SSR 영매", 60, White, TA.Center, heavy: true);
            Text(Place(Node(page, "RndLine"), Edge.Center, 90, 1365, 990, 1420), null, "내가 키운 능력치로 출전", 42, White, TA.Center, heavy: true);
            for (int i = 0; i < Chips; i++)
            {
                var chip = Place(Node(page, $"RndChip{i}"), Edge.Center, 380 + i * 170, 1478, 540 + i * 170, 1532);
                Img(chip, "Components/rb_info_chip").preserveAspect = false;
                Text(Stretch(Node(chip, $"RndChip{i}Text")), null, "조각 +2", 36, Green, TA.Center, heavy: true);
            }
            var go = Place(Node(page, "RndGo"), Edge.Bottom, 32, 1920, 1048, 2088);
            Btn(go, "Components/rb_btn_primary");
            Text(Stretch(Node(go, "RndGoText")), null, "공개하기", 64, Ink, TA.Center, heavy: true);

            var view = page.gameObject.AddComponent<RbRandomPage>();
            var so = new SerializedObject(view);
            so.FindProperty("_wordRandom").objectReferenceValue = KitSprite("WordArt/rb_word_word_random");
            so.FindProperty("_wordHidden").objectReferenceValue = KitSprite("WordArt/rb_word_word_hidden");
            so.FindProperty("_mystery").objectReferenceValue = KitSprite("Icons/rb_icon_mode_random_144");
            so.ApplyModifiedPropertiesWithoutUndo();
            page.gameObject.SetActive(false);
        }
    }
}
