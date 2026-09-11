using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// UI 그림에 **세로 그라데이션**을 곱한다 — 새 그림 없이 꼭짓점 색으로.
    ///
    /// 아래 색(`_bottom`)에서 위 색(`_top`)으로 이어진다. 그림 색과 곱해지므로 흰색이면 원래 그대로다.
    /// 방 위 구름(`BattleDirector.RoomCloud`)이 쓴다 — 위로 갈수록 어둡게 가라앉혀 멀어지는 깊이를 준다.
    ///
    /// ⚠ 네 꼭짓점 사이를 보간할 뿐이라 **`Image.Type.Simple`** 에서만 고르게 나온다.
    ///   `Sliced`·`Tiled` 는 꼭짓점이 여럿이라 칸마다 따로 계산돼도 결과는 같지만, 채움(Filled)은 쓰지 마라.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class VerticalGradient : BaseMeshEffect
    {
        [SerializeField] private Color _top = Color.white;
        [SerializeField] private Color _bottom = Color.white;

        /// <summary>위·아래 색을 바꾸고 다시 그린다.</summary>
        public void Set(Color top, Color bottom)
        {
            _top = top;
            _bottom = bottom;
            if (graphic != null) graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;

            var rect = graphic.rectTransform.rect;
            var vertex = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                float k = Mathf.InverseLerp(rect.yMin, rect.yMax, vertex.position.y);
                Color c = vertex.color;
                vertex.color = c * Color.Lerp(_bottom, _top, k);
                vh.SetUIVertex(vertex, i);
            }
        }
    }
}
