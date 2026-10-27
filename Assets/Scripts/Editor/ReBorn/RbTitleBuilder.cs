// RE:BORN UI 리뉴얼(2026-10-23) — 타이틀 · 로딩 세우기. 시안 Screens/title.png · loading.png(1080 × 2340).
// 두 시안은 같은 바탕(흐린 밤 거리) + 같은 로고 자리다. 아래만 다르다 — 타이틀은 「시작하기」, 로딩은 LOADING · 막대 · 팁.
// 시안 오른쪽 위 「예시」 표는 시안 설명용이라 옮기지 않는다.
using Game.Module.Title;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Game.Editor.ReBorn.RbBuild;
using TA = TMPro.TextAlignmentOptions;

namespace Game.Editor.ReBorn
{
    public static class RbTitleBuilder
    {
        public const string TitlePath = "Assets/BundleResource/Prefabs/UI/Title/RbTitleUI.prefab";
        public const string LoadingPath = "Assets/BundleResource/Prefabs/UI/Common/RbLoadingView.prefab";

        [MenuItem("Tools/Game/RE:BORN 화면/타이틀 · 로딩 세우기")]
        public static void Build()
        {
            BuildTitle();
            BuildLoading();
        }

        private static void BuildTitle()
        {
            var root = Root("RbTitleUI");
            var rt = (RectTransform)root.transform;
            // 타이틀 한 장 그림(PD 2026-10-28) — 로고 · 유령 · 밤 도시 · 회사 로고가 그림에 다 들어 있다. 따로 얹던 로고는 뺀다
            var key = Stretch(Node(rt, "TitleBg"));
            var keyImg = Img(key, "Art/rb_art_title_key");
            keyImg.preserveAspect = false;
            var keyFit = key.gameObject.AddComponent<AspectRatioFitter>();
            keyFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;   // 검은 여백 없이 덮는다(20:9 는 양옆이 조금 잘린다)
            keyFit.aspectRatio = keyImg.sprite != null ? keyImg.sprite.rect.width / keyImg.sprite.rect.height : 941f / 1672f;
            // 화면 어디를 눌러도 시작 · 「TAP TO START」 는 화면 스크립트가 깜빡인다(PD 「tap to start 도 넣어 줘」) — 어두운 물 · 지붕 쪽
            HitArea(Stretch(Node(rt, "StartButton")));
            Text(Place(Node(rt, "TapToStartText"), Edge.Bottom, 140, 1905, 940, 1985), null, "TAP TO START", 60, White, TA.Center, number: true);   // 도시 불빛 위는 안 읽혀 어두운 물 · 지붕 쪽으로
            // 저작권 줄은 남긴다(원작 표기 — 지우지 않는다) · 회사 로고 밑 맨 아래 가장자리에 작게. 버전은 같은 줄 오른쪽
            Text(Place(Node(rt, "CopyrightText"), Edge.Bottom, 40, 2196, 760, 2226), null, "©1991 JALECO / CITY CONNECTION", 22, Dim, TA.Left);
            Text(Place(Node(rt, "VersionText"), Edge.Bottom, 780, 2196, 1040, 2226), null, "v1.0", 22, Dim, TA.Right, number: true);

            var ui = root.AddComponent<TitleMainUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("_reborn").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            Save(root, TitlePath);
            Register(TitlePath, "UI/Title/RbTitleUI", "label_title");
            Debug.Log($"[RE:BORN 화면] 타이틀 → {TitlePath}");
        }

        /// <summary>로딩 가림막의 RE:BORN 판 — 가림막(GhostLoadingView)이 받아 막대 · 팁만 움직인다.</summary>
        private static void BuildLoading()
        {
            var root = Root("RbLoadingView");
            var rt = (RectTransform)root.transform;
            Backdrop(rt, "RbLoadingBg").raycastTarget = true;   // 로딩 중 조작을 막는다
            Logo(rt, "RbLoadingLogo");
            Text(Place(Node(rt, "RbLoadingLabel"), Edge.Bottom, 99, 1738, 600, 1800), null, "LOADING", 60, Cyan, TA.Left, number: true);
            var bar = Place(Node(rt, "RbLoadingBar"), Edge.Bottom, 99, 1826, 981, 1850);
            Bar(bar, "RbLoadingBarFill", "Components/rb_bar_fill_cyan");
            var tip = Text(Place(Node(rt, "RbLoadingTip"), Edge.Bottom, 99, 1874, 981, 1990), null, "몸이 바뀌어도 레벨업 카드는 유지돼요", 42, White, TA.TopLeft, heavy: true);
            tip.enableWordWrapping = true;   // 팁은 언어에 따라 두 줄이 된다
            Save(root, LoadingPath);
            Register(LoadingPath, "UI/Common/RbLoadingView", "label_title");
            Debug.Log($"[RE:BORN 화면] 로딩 → {LoadingPath}");
        }

        /// <summary>흐린 밤 거리 — 화면을 덮는다(검은 여백 없이, 넘치는 쪽을 자른다).</summary>
        private static Image Backdrop(RectTransform rt, string name)
        {
            var bg = Stretch(Node(rt, name));
            var img = Img(bg, "Art/rb_art_bg_outgame_blur");
            img.preserveAspect = false;
            var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = img.sprite != null ? img.sprite.rect.width / img.sprite.rect.height : 1080f / 2340f;
            return img;
        }

        /// <summary>
        /// 로고 — 시안의 글자(잉크)가 90~990 × 583~1013. 그림(1024 × 480)은 둘레가 비어 잉크가 173~851 × 78~402 뿐이라
        /// 칸을 잉크 비율만큼 키운다: 폭 900 × 1024/678 = 1359 · 높이 637, 가운데 (540, 798).
        /// </summary>
        private static void Logo(RectTransform rt, string name)
        {
            Img(Place(Node(rt, name), Edge.Center, -139.5f, 479.5f, 1219.5f, 1116.5f), "Art/rb_art_logo_transparent");
        }
    }
}
