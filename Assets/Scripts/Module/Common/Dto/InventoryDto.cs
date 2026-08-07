using System;
using System.Collections.Generic;

namespace Game.Module.Common.Dto
{
    // 최상위 배열 직렬화 불가 → items wrapper 패턴 필수
    [Serializable]
    public struct InventoryDto
    {
        public List<ItemDto> items;
    }

    // JsonUtility 직렬화 규칙: [Serializable] + public 필드만 사용 (프로퍼티 불가)
    [Serializable]
    public struct ItemDto
    {
        public string itemId;
        public int    count;
    }
}
