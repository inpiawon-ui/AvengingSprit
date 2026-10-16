using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 인게임 버튼 셋 — 빙의 · 액티브 · 필살기 (PD 2026-10-16 「빙의 버튼이 따로 있어야」).
    ///
    /// 예전엔 버튼 하나(`PossessButton`)가 유령이면 빙의, 몸이 있으면 필살기였다. 이 도구는 그 버튼을 복제해
    /// 필살기 버튼(`UltButton`)을 원래 자리에 세우고, 빙의 버튼은 액티브 버튼 왼쪽으로 옮긴다.
    /// 그림 · 틀은 같은 것을 쓴다(필살기 전용 틀은 아직 없다). 이미 `UltButton` 이 있으면 아무것도 안 한다.
    ///
    /// 자식 이름: `PossessGhostIcon` → `UltIcon`, `PossessButtonLabel` → `UltButtonLabel`,
    /// `PossessButtonGlow` → `UltButtonGlow`, `PossessDeniedToast` → `UltDeniedToast`. 값 · 쿨다운 칸은 필살기에 없어 지운다.
    /// </summary>
    public static class InGameUltButtonSplit
    {
        private const string Prefab = "Assets/BundleResource/Prefabs/UI/InGame/InGameMainUI.prefab";
        private const float K = 720f / 1080f;
        // 빙의 버튼 새 자리 — 시안 1080 기준 왼쪽 끝(액티브 681 왼쪽, 8 띄움). 예전 자리 877
        private const float PossessShift = (483f - 877f) * K;

        private static readonly string[] DropFromUlt = { "PossessCooldown", "PossessCooldownText", "PossessCostText" };

        [MenuItem("Tools/Game/인게임 빙의 버튼 따로 꽂기")]
        public static void Run()
        {
            var root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var possess = Find(root.transform, "PossessButton") as RectTransform;
                if (possess == null) { Debug.LogError("[UltSplit] PossessButton 없음"); return; }
                if (Find(root.transform, "UltButton") != null) { Debug.Log("[UltSplit] 이미 나눠져 있다"); return; }

                // 버튼 밖(형제)에 있는 빛 — 버튼과 같이 복제 · 이동한다
                var glow = Find(root.transform, "PossessButtonGlow") as RectTransform;
                bool glowOutside = glow != null && !glow.IsChildOf(possess);

                var ult = Object.Instantiate(possess.gameObject, possess.parent);
                ult.name = "UltButton";
                ult.transform.SetSiblingIndex(possess.GetSiblingIndex() + 1);
                foreach (var n in DropFromUlt)
                    if (Find(ult.transform, n) is Transform t) Object.DestroyImmediate(t.gameObject);
                Rename(ult.transform);

                if (glowOutside)
                {
                    var g = Object.Instantiate(glow.gameObject, glow.parent);
                    g.name = "UltButtonGlow";
                    g.transform.SetSiblingIndex(glow.GetSiblingIndex() + 1);
                    glow.anchoredPosition += new Vector2(PossessShift, 0f);
                }
                possess.anchoredPosition += new Vector2(PossessShift, 0f);

                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
                Debug.Log($"[UltSplit] 완료 — UltButton 추가, PossessButton {PossessShift:0.0} 이동 (빛 바깥={glowOutside})");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void Rename(Transform t)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                var c = t.GetChild(i);
                c.name = c.name == "PossessGhostIcon" ? "UltIcon" : c.name.Replace("Possess", "Ult");
                Rename(c);
            }
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
                if (Find(root.GetChild(i), name) is Transform hit) return hit;
            return null;
        }
    }
}
