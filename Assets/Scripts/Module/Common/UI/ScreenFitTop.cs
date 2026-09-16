using UnityEngine;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 「세로는 판의 **위쪽 변**에 붙여라」 표시. <see cref="ScreenFit"/> 이 읽는다.
    ///
    /// 판이 세로로 커질 때(<see cref="ScreenFitSpread"/> 의 「키운다」) 판 속 글자는
    /// 위에 그대로 있어야 한다. 그냥 두면 위쪽 글자는 위에, 아래쪽 글자는 아래에 붙어
    /// **한 덩어리였던 글이 위아래로 찢어진다** — 유령 수색 판에서 제목은 위에 남고
    /// 설명글만 금화 더미까지 내려갔다(2026-09-16).
    ///
    /// 가로는 건드리지 않는다. 좌우는 `ScreenFit` 이 알아서 가른다.
    ///
    /// ⚠ 규칙을 **그 오브젝트 위에** 둔다. 어딘가의 이름 표에 적어 두면 이름이 바뀌는
    ///   순간 조용히 풀리고, 아무도 못 알아챈다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ScreenFitTop : MonoBehaviour
    {
    }
}
