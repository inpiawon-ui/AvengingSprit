using System.Linq;
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
    /// 픽셀 폰트 파일이 아직 없으므로 현재는 전부 고딕으로 떨어지고,
    /// 픽셀 대상만 자간을 벌려 임시로 아케이드 톤을 낸다.
    /// 폰트를 `PixelFontPath` 에 넣고 다시 실행하면 그때부터 적용된다.
    /// </summary>
    public static class ApplyFonts
    {
        private const string GothicPath = "Assets/BaseResource/Fonts/NotoSansKR SDF.asset";
        private const string PixelFontPath = "Assets/BaseResource/Fonts/PixelArcade SDF.asset";

        private static readonly string[] Prefabs =
        {
            "Assets/BundleResource/Prefabs/UI/Title/TitleMainUI.prefab",
            "Assets/BundleResource/Prefabs/UI/Lobby/LobbyMainUI.prefab",
            "Assets/BundleResource/Prefabs/UI/HostSelect/HostSelectPanel.prefab",
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
