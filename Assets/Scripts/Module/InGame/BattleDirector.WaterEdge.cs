using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 원작 방 물 칸의 둑 — 3단계 2026-10-23.
    ///
    /// 원작(궁수의 전설)의 물은 칸이 여럿 이어져 웅덩이 · 강 · 십자가 된다. 칸마다 수면 한 장(`obj_{무대}_water_1/2`)을 깔고,
    /// **옆 칸이 물이 아닌 쪽에만** 둑(`water_edge_n/s/w/e`)을, 두 쪽이 다 땅인 모서리에 귀퉁이(`water_corner_nw/ne/sw/se`)를 얹는다.
    /// 둑 그림은 칸 크기 한 장에 그 변 · 모서리만 그려져 있다 — 수면 그림의 자식으로 칸에 꽉 채워 얹으면 된다.
    /// 그림이 아직 없는 무대는 아무것도 얹지 않는다(수면만).
    /// </summary>
    public sealed partial class BattleDirector
    {
        private readonly HashSet<Vector2Int> _waterCells = new();

        private static Vector2Int OriginCellOf(Vector2 at)
            => new(Mathf.FloorToInt(at.x / OriginCellMeters), Mathf.FloorToInt(at.y / OriginCellMeters));

        private void CollectWaterCells(RoomEntry room)
        {
            _waterCells.Clear();
            if (!IsOriginRoom(room)) return;
            var list = room.Objects;
            for (int i = 0; i < list.Count; i++)
                if (list[i].Kind == "WATER") _waterCells.Add(OriginCellOf(list[i].At));
        }

        /// <summary>물 칸 하나에 둑 · 귀퉁이를 얹는다. y 는 위가 큰 쪽(방 좌표).</summary>
        private void AddWaterEdges(GameObject water, Vector2 at)
        {
            var c = OriginCellOf(at);
            bool n = !_waterCells.Contains(c + Vector2Int.up);
            bool s = !_waterCells.Contains(c + Vector2Int.down);
            bool w = !_waterCells.Contains(c + Vector2Int.left);
            bool e = !_waterCells.Contains(c + Vector2Int.right);
            if (n) AddWaterPart(water, "obj_water_edge_n");
            if (s) AddWaterPart(water, "obj_water_edge_s");
            if (w) AddWaterPart(water, "obj_water_edge_w");
            if (e) AddWaterPart(water, "obj_water_edge_e");
            if (n && w) AddWaterPart(water, "obj_water_corner_nw");
            if (n && e) AddWaterPart(water, "obj_water_corner_ne");
            if (s && w) AddWaterPart(water, "obj_water_corner_sw");
            if (s && e) AddWaterPart(water, "obj_water_corner_se");
        }

        private void AddWaterPart(GameObject water, string art)
        {
            var sprite = EnvSprite(art);
            if (sprite == null) return;
            var go = new GameObject(art, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(water.transform, false);   // 자식 — 수면과 함께 줄 서고 함께 치워진다
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
        }
    }
}
