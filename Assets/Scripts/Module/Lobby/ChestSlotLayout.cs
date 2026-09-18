using UnityEngine;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 상자 칸 속 배치 — 상태마다 자리가 다르다 (로비 v3, 시안 lobby_hub_v2 그대로).
    ///
    /// 시안에서 「세는 중」 칸은 상자가 위에 있고 아래에 시간 판 · 젬 버튼이,
    /// 「완료」 칸은 위에 월계관 띠 · 가운데 상자 · 아래 금색 버튼이 온다. 상자와 버튼의
    /// 자리 · 크기가 두 상태에서 다르므로 칸마다 두 벌을 들고, `LobbyMainUI` 가 상태에 맞춰 옮긴다.
    ///
    /// 좌표는 칸 기준(왼쪽 위 원점, 아래로 음수) anchoredPosition · sizeDelta 다. 빌더가 채운다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ChestSlotLayout : MonoBehaviour
    {
        [SerializeField] private Rect _chestCounting;
        [SerializeField] private Rect _chestReady;
        [SerializeField] private Rect _buttonCounting;
        [SerializeField] private Rect _buttonReady;

        public void Set(Rect chestCounting, Rect chestReady, Rect buttonCounting, Rect buttonReady)
        {
            _chestCounting = chestCounting; _chestReady = chestReady;
            _buttonCounting = buttonCounting; _buttonReady = buttonReady;
        }

        public void Apply(RectTransform chest, RectTransform button, bool ready)
        {
            Place(chest, ready ? _chestReady : _chestCounting);
            Place(button, ready ? _buttonReady : _buttonCounting);
        }

        private static void Place(RectTransform rt, Rect r)
        {
            if (rt == null || r.size == Vector2.zero) return;
            rt.anchoredPosition = r.position;
            rt.sizeDelta = r.size;
        }
    }
}
