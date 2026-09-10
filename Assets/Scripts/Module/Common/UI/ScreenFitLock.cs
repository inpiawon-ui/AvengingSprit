using UnityEngine;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 「이 판은 넓히지 마라」 표시. <see cref="ScreenFit"/> 이 읽는다.
    ///
    /// 폭이 화면을 채운다고 다 배경인 것은 아니다. 인게임 플레이 필드는 방이
    /// 10 m × 13 m 고정이라 **720 px 여야만 한다** — 늘리면 픽셀/미터가 달라져
    /// 사거리·이동 속도·캐릭터 크기가 전부 어긋난다.
    ///
    /// ⚠ 규칙을 **그 오브젝트 위에** 둔다. 어딘가의 이름 표에 적어 두면
    ///   이름이 바뀌는 순간 조용히 풀리고, 아무도 못 알아챈다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ScreenFitLock : MonoBehaviour
    {
    }
}
