using UnityEngine;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 「세로로 남는 만큼을 형제들과 나눠 벌려라」 표시. <see cref="ScreenFit"/> 이 읽는다.
    ///
    /// 가로는 <see cref="ScreenFitShare"/> 가 맡는다. 세로는 사정이 다르다 —
    /// 20:9 처럼 길쭉한 폰은 기준(1280)보다 **320 px 이 남는데**, 그 몫이 지금은
    /// 통째로 한 곳(상단 HUD 아래)에 고여 거기만 텅 비어 보였다(2026-09-16 지적).
    ///
    /// 이 표시가 붙은 형제들은 **크기를 그대로 두고** 남는 세로를 칸 사이에
    /// **고르게 나눠** 벌린다. 첫째는 제자리, 막내는 화면 끝에 닿는다.
    ///
    /// ⚠ 남는 것이 없으면(9:16) 그린 값 그대로다 — 목업 대조가 틀어지지 않는다.
    /// ⚠ 규칙을 **그 오브젝트 위에** 둔다. 어딘가의 이름 표에 적어 두면 이름이 바뀌는
    ///   순간 조용히 풀리고, 아무도 못 알아챈다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ScreenFitSpread : MonoBehaviour
    {
    }
}
