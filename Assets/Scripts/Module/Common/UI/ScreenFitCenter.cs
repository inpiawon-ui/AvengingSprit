using UnityEngine;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 「이 칸은 부모 가운데에 둬라」 표시. <see cref="ScreenFit"/> 이 읽는다.
    ///
    /// 나눠 갖는 판(<see cref="ScreenFitShare"/>) 안쪽은 기본이 **좌·우 둘로만** 가르는
    /// 것이다 — 넓은 줄(상단 HUD)에서는 그게 맞다. 그런데 상자 칸처럼 **작고 가운데로
    /// 모인 칸**에서는 그러면 안 된다. 태블릿에서 칸이 282 → 376 으로 넓어질 때
    /// 시계는 왼쪽 끝에, 남은 시간 글자는 오른쪽 끝에 붙어 한 줄이 두 동강 났다
    /// (2026-09-16 실제로 그랬다).
    ///
    /// ⚠ 규칙을 **그 오브젝트 위에** 둔다. 어딘가의 이름 표에 적어 두면 이름이 바뀌는
    ///   순간 조용히 풀리고, 아무도 못 알아챈다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ScreenFitCenter : MonoBehaviour
    {
    }
}
