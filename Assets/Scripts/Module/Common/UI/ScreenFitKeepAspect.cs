using UnityEngine;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 「가로로 늘릴 때 **나는 찌그러뜨리지 마라**」 표시. <see cref="ScreenFitStretchX"/> 가 읽는다.
    ///
    /// 판·액자는 늘어나도 되지만 캐릭터 그림·아이콘은 옆으로 퍼지면 바로 티가 난다
    /// (2026-10-01 지적 — 4:3 에서 글자와 캐릭터가 퍼져 보였다).
    /// 글자(TMP)는 이 표시가 없어도 늘 지켜 준다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenFitKeepAspect : MonoBehaviour
    {
    }
}
