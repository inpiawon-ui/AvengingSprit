using System.Collections.Generic;
using Game.Module.Common.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// UI 프리팹의 모든 TMP 컴포넌트에 `FontPolicy` 를 적용한다.
    ///
    /// 확정 사항 9 — 영문·숫자·타이틀은 비트맵 픽셀, 한글 본문은 고딕 벡터.
    ///
    /// ⚠ 이 도구는 **프리팹의 폰트를 통째로 덮어쓴다.** 픽셀 폰트를 확보하기 전에는
    ///   `PixelArcade SDF` 라는 없는 파일을 가리키고 있어서, 실행하면 원작 도트
    ///   폰트를 지우고 전부 고딕으로 되돌려 놓았다. 실제 경로로 고쳤다.
    /// </summary>
    public static class ApplyFonts
    {
        private const string GothicPath = "Assets/BaseResource/Fonts/NotoSansKR SDF.asset";
        private const string PixelFontPath = "Assets/BaseResource/Fonts/OriginalPixel SDF.asset";

        // ⚠️ 화면을 추가하면 여기에도 넣어야 한다. InGameMainUI 가 빠져 있어서
        //    인게임 글자만 외곽선도 볼드도 없이 남아 있었다 — 실기에서 안 읽혔다.
        private static readonly string[] Prefabs =
        {
            "Assets/BundleResource/Prefabs/UI/Title/TitleMainUI.prefab",
            "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab",
            "Assets/BundleResource/Prefabs/UI/HostSelect/HostSelectPanel.prefab",
            "Assets/BundleResource/Prefabs/UI/InGame/InGameMainUI.prefab",
        };

        [MenuItem("Tools/Game/Apply Fonts To UI Prefabs")]
        public static void Run()
        {
            var gothic = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(GothicPath);
            if (gothic == null)
            {
                Debug.LogError($"[ApplyFonts] 한글 폰트 없음: {GothicPath}");
                return;
            }
            var pixel = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontPath);
            if (pixel == null)
                Debug.LogWarning($"[ApplyFonts] 픽셀 폰트 미확보 — 전부 고딕으로 적용합니다. " +
                                 $"확보 시 {PixelFontPath} 에 배치하고 재실행하십시오.");

            EnsureKoreanGlyphs(gothic);

            int total = 0, pixelCount = 0;
            foreach (var path in Prefabs)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                if (root == null) { Debug.LogError($"[ApplyFonts] 로드 실패: {path}"); continue; }

                var texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in texts)
                {
                    FontPolicy.Apply(t, pixel, gothic);
                    // 도트 폰트는 비트맵이라 SDF 머티리얼을 씌우면 글자가 뭉개진다.
                    // 외곽선·그림자 속성도 비트맵 셰이더에는 아예 없다.
                    if (t.font == pixel) t.fontSharedMaterial = pixel.material;
                    else ApplyEffect(t, gothic);
                    if (FontPolicy.RoleOf(t.gameObject.name) == FontRole.Pixel) pixelCount++;
                    total++;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                Debug.Log($"[ApplyFonts] {System.IO.Path.GetFileNameWithoutExtension(path)} — TMP {texts.Length}개");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ApplyFonts] 완료 — TMP {total}개 (픽셀 대상 {pixelCount} / 고딕 {total - pixelCount})");
        }

        // ─────────────────────────────────────────────────────────
        // 글자 효과 — 목업의 텍스트는 어두운 외곽선 + 드롭섀도로 아트 위에서 읽힌다.
        // 폰트 페이스는 달라도 효과는 같게 맞춘다.

        /// <summary>효과 프리셋 — 외곽선 두께 · 그림자 진하기 · 글로우색(없으면 null)</summary>
        private readonly struct Fx
        {
            public readonly string Key;
            public readonly float Outline;
            public readonly float Shadow;
            public readonly string Glow;

            public Fx(string key, float outline, float shadow, string glow = null)
            {
                Key = key; Outline = outline; Shadow = shadow; Glow = glow;
            }
        }

        // ⚠️ TMP 의 _OutlineWidth 는 바깥으로 자라지 않고 **글자 안쪽을 깎는다.**
        //    두껍게 주면 획이 사라진다. _FaceDilate 로 살을 먼저 붙이고 외곽선은 얇게 준다.
        //
        // 0.15 로 통일했더니 **한글이 뭉갰다.** 한글은 획이 1~2px 라 그 두께면
        // 외곽선이 획을 통째로 먹는다. 획을 남기는 선에서 가장 두꺼운 값이 0.08 이다.
        // 어두운 배경에서의 가독성은 외곽선이 아니라 그림자로 받는다.
        private const float OutlineW = 0.08f;

        /// <summary>
        /// 외곽선이 깎아 먹을 만큼만 살을 붙인다. 외곽선에 비례해 붙이던 것을
        /// 고정값으로 바꿨다 — 비례로 두면 굵은 글자가 서로 들러붙는다.
        /// </summary>
        private const float FaceDilate = 0.05f;

        // 목업 실측 — 하단 3버튼 타이틀은 버튼 색과 같은 글로우를 두르고 있다.
        private static readonly Fx Title = new("Title", OutlineW, 0.95f);
        private static readonly Fx Label = new("Label", OutlineW, 0.85f);
        private static readonly Fx Soft = new("Soft", OutlineW, 0.55f);

        private static readonly Dictionary<string, Fx> Effects = new()
        {
            ["HostButtonTitleText"] = new("GlowPurple", OutlineW, 0.95f, "#8A46D8"),
            ["ChapterButtonTitleText"] = new("GlowGold", OutlineW, 0.95f, "#D08A14"),
            ["ShopButtonTitleText"] = new("GlowBlue", OutlineW, 0.95f, "#2A74C8"),
        };

        /// <summary>골드 버튼 위의 어두운 글자 — 목업에 외곽선이 없다</summary>
        private static readonly HashSet<string> NoEffect = new()
        {
            "ContinueButtonText", "ContinueCostText",
        };

        /// <summary>굵은 외곽선 대상 — 배경 아트 위에 직접 얹히는 글자</summary>
        private static readonly HashSet<string> TitleGrade = new()
        {
            "ChapterNameText", "TapToStartText", "HeaderTitleText", "HostNameEnText",
        };

        private static readonly HashSet<string> LabelGrade = new()
        {
            "HostButtonSubText", "ChapterButtonSubText", "ShopButtonSubText",
            "MissionTabLabel", "AchievementTabLabel", "RankingTabLabel",
            "InventoryTabLabel", "FriendsTabLabel",
            "HeaderDescText", "HostSlotNameText", "VersionText",
        };

        private static readonly Dictionary<string, Material> s_materials = new();

        private static void ApplyEffect(TextMeshProUGUI tmp, TMP_FontAsset gothic)
        {
            var name = tmp.gameObject.name;
            if (NoEffect.Contains(name))
            {
                tmp.fontSharedMaterial = gothic.material;
                return;
            }
            var fx = Effects.TryGetValue(name, out var e) ? e
                   : TitleGrade.Contains(name) ? Title
                   : LabelGrade.Contains(name) ? Label
                   : Soft;
            tmp.fontSharedMaterial = EnsureMaterial(gothic, fx);
        }

        /// <summary>폰트 아틀라스를 공유하는 머티리얼 프리셋. 텍스트마다 인스턴스를 만들지 않는다.</summary>
        private static Material EnsureMaterial(TMP_FontAsset gothic, Fx fx)
        {
            if (s_materials.TryGetValue(fx.Key, out var cached) && cached != null) return cached;

            var dir = System.IO.Path.GetDirectoryName(GothicPath).Replace('\\', '/');
            var path = $"{dir}/{gothic.name} - {fx.Key}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(gothic.material) { name = $"{gothic.name} - {fx.Key}" };
                AssetDatabase.CreateAsset(mat, path);
            }

            // 외곽선이 획을 깎아 먹지 않도록 글자를 먼저 살찌운다
            mat.SetFloat(ShaderUtilities.ID_FaceDilate, FaceDilate);
            mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, fx.Outline);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.02f, 0.03f, 0.06f, 1f));

            // 하드 드롭섀도 — 목업은 흐린 그림자가 아니라 또렷하게 어긋난 그림자다
            mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, fx.Shadow));
            // ⚠ 그림자도 얇아야 한다. 0.2 로 부풀린 그림자를 1px 어긋나게 두면
            //   작은 글자에서는 그림자가 글자만큼 굵어져 **겹쳐 보이는 잔상**이 된다.
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.6f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.05f);
            mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.10f);

            if (fx.Glow != null && ColorUtility.TryParseHtmlString(fx.Glow, out var glow))
            {
                mat.EnableKeyword(ShaderUtilities.Keyword_Glow);
                mat.SetColor(ShaderUtilities.ID_GlowColor, glow);
                mat.SetFloat(ShaderUtilities.ID_GlowPower, 0.40f);
                mat.SetFloat(ShaderUtilities.ID_GlowOuter, 0.16f);
                mat.SetFloat(ShaderUtilities.ID_GlowInner, 0.02f);
                mat.SetFloat(ShaderUtilities.ID_GlowOffset, 0f);
            }
            else
            {
                mat.DisableKeyword(ShaderUtilities.Keyword_Glow);
            }

            EditorUtility.SetDirty(mat);
            s_materials[fx.Key] = mat;
            return mat;
        }

        /// <summary>
        /// UI 에 실제로 쓰이는 한글을 미리 구워 넣는다.
        /// Dynamic 모드라 런타임에도 생성되지만, 첫 표시 때 프레임이 튀는 것을 막는다.
        /// </summary>
        private static void EnsureKoreanGlyphs(TMP_FontAsset gothic)
        {
            const string used =
                "빙의시작돌아가기호스트강화보유선택능력치미션업적랭킹인벤토리친구상점" +
                "게임진행보상준비중입니다유령으로사망시다른에할수있습니다첫번째를하세요" +
                "이동속도대시회피공격방식마다다릅니다양한경험해보세요도감패키지재화육성" +
                "챕터스테이지보스클리어달성잠금해제조건일일로그인이벤트배틀패스시즌";
            int before = gothic.characterTable.Count;
            gothic.TryAddCharacters(used, out _);
            EditorUtility.SetDirty(gothic);
            Debug.Log($"[ApplyFonts] 한글 글리프 프리베이크 {before} → {gothic.characterTable.Count}");
        }
    }
}
