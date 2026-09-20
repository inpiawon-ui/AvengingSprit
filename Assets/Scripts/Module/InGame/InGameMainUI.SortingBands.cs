using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 화면을 **정렬 밴드**로 나눈다 (2026-09-20).
    ///
    /// ── 왜 필요한가 ──────────────────────────────────────────
    /// 예전에는 캔버스 하나(Screen Space - Overlay)에 방·유닛·HUD·패널이 통째로 들어가
    /// **형제 순서**만으로 겹침을 정했다. 그러면 파티클을 못 쓴다 —
    /// 파티클은 캔버스가 그리는 물건이 아니라서 형제 순서에 끼어들 수 없고,
    /// Overlay 캔버스는 **언제나 카메라가 그린 모든 것 위에** 올라가기 때문이다.
    ///
    /// 그래서 씬의 캔버스를 **Screen Space - Camera** 로 바꿨다(GameScene.unity).
    /// 이제 캔버스도 카메라가 그리는 물건이 되었고, 캔버스와 파티클은 같은 자(sortingOrder)로 잰다.
    ///
    /// ── 밴드 ────────────────────────────────────────────────
    ///   0   방 · 바닥 · 유닛 · 탄 · 터짐   (루트 캔버스)
    ///  10   **파티클**                      (<see cref="ParticleOrder"/>)
    ///  20   HUD · 조작 · 패널 · 팝업        (<see cref="HudBandOrder"/>)
    ///  50   화면 덮개                       (<see cref="CoverBandOrder"/>)
    ///  900+ 로딩 · SystemPopup              (각자 Overlay 캔버스 — 언제나 맨 위)
    ///
    /// ── 규칙 ────────────────────────────────────────────────
    /// **`RoomField` 뒤에 오는 형제는 전부 HUD 밴드다.** 이름을 하나씩 적어 두면
    /// 패널이 늘 때마다 빠뜨린다 — 자리로 정한다.
    ///
    /// ⚠ 같은 밴드 안에서는 **형제 순서가 그대로 살아 있다.** sortingOrder 를 하나씩
    ///   다르게 주면 `SetAsLastSibling()` 로 패널을 앞으로 올리는 규약(06_ui)이 죽는다.
    ///   그래서 밴드 안은 **전부 같은 값**이다.
    ///
    /// ⚠ `overrideSorting` 캔버스는 **제 GraphicRaycaster 가 있어야** 입력을 받는다.
    ///   없으면 버튼이 눌리지 않는다.
    /// </summary>
    public sealed partial class InGameMainUI
    {
        /// <summary>파티클 렌더러가 쓰는 정렬 값. 방 위 · HUD 아래.</summary>
        public const int ParticleOrder = 10;

        private const int HudBandOrder = 20;
        private const int CoverBandOrder = 50;

        /// <summary>방 뒤에 오는 형제를 전부 HUD 밴드로 올린다. `Awake` 에서 한 번.</summary>
        private void SetupSortingBands()
        {
            var field = _ui.Find("RoomField");
            int from = field != null ? field.GetSiblingIndex() + 1 : 0;
            for (int i = from; i < transform.childCount; i++)
                LiftToBand(transform.GetChild(i), HudBandOrder);
        }

        /// <summary>런타임에 만든 층(덮개 등)을 밴드에 올린다.</summary>
        private static void LiftToBand(Transform t, int order)
        {
            if (t == null) return;
            var canvas = t.GetComponent<Canvas>();
            if (canvas == null) canvas = t.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = order;
            if (t.GetComponent<GraphicRaycaster>() == null)
                t.gameObject.AddComponent<GraphicRaycaster>();
        }
    }
}
