using UnityEngine;

namespace Game.Module.Lobby
{
    /// <summary>칸이 지금 가리키는 상품 id(챕터 돌파처럼 그때그때 바뀌는 칸).</summary>
    public sealed class ShopProductTag : MonoBehaviour
    {
        public string Id { get; set; }
    }
}
