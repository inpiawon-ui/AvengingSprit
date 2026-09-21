using UnityEngine;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 상자 카드 속 상자 그림의 자리 — 등급마다 다르다 (로비 v4, 시안 lobby_hub_v3_jp 그대로).
    ///
    /// 시안의 세 카드는 은 · 금 · 백금 상자를 한 장씩 담고 있고, 상자마다 크기 · 자리가 조금씩 다르다.
    /// 어느 칸에 어느 등급이 올지는 매번 달라서, 칸마다 세 등급의 자리를 다 들고 `LobbyMainUI` 가 골라 옮긴다.
    ///
    /// 좌표는 칸 기준(왼쪽 위 원점, 아래로 음수) anchoredPosition · sizeDelta 다. 빌더가 채운다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ChestSlotLayout : MonoBehaviour
    {
        [System.Serializable]
        private struct GradeRect
        {
            public string Key;
            public Rect Rect;
        }

        [SerializeField] private GradeRect[] _chests = System.Array.Empty<GradeRect>();

        public void Set(string[] keys, Rect[] rects)
        {
            _chests = new GradeRect[keys.Length];
            for (int i = 0; i < keys.Length; i++) _chests[i] = new GradeRect { Key = keys[i], Rect = rects[i] };
        }

        public void Apply(RectTransform chest, string chestKey)
        {
            if (chest == null) return;
            for (int i = 0; i < _chests.Length; i++)
            {
                if (_chests[i].Key != chestKey) continue;
                chest.anchoredPosition = _chests[i].Rect.position;
                chest.sizeDelta = _chests[i].Rect.size;
                return;
            }
        }
    }
}
