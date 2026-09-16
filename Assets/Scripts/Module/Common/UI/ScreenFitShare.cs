using UnityEngine;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 「이 판은 부모의 **남는 폭을 형제와 나눠 갖는다**」 표시. <see cref="ScreenFit"/> 이 읽는다.
    ///
    /// 붙이지 않으면 판은 그린 크기를 지키고 한쪽 가장자리로 붙기만 한다. 그러면 상단 HUD 처럼
    /// 두 줄로 된 곳에서 **줄마다 끝이 안 맞는다** — 1줄은 화면 끝까지 벌어지는데
    /// 2줄은 가로 레이아웃 그룹이 가운데로 모아 두기 때문이다.
    ///
    /// 붙이면 늘어난 폭을 **그린 폭의 비율대로** 나눠 갖는다. 틀은 9-슬라이스라 테두리가
    /// 깨지지 않고, 판 안쪽 것들은 <see cref="ScreenFit"/> 이 왼쪽/오른쪽으로 다시 붙인다.
    /// 그래서 9:16 이든 3:4 든 **같은 모양이 폭만 달라진 채로** 보인다.
    ///
    /// ⚠ 같은 부모 안의 표시된 형제끼리만 나눈다. 사이 간격은 그린 값을 그대로 지킨다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ScreenFitShare : MonoBehaviour
    {
        [SerializeField] private bool _spaceOnly;

        /// <summary>
        /// 켜면 **폭은 그린 값 그대로 두고 사이 간격만** 벌린다.
        ///
        /// ⚠ 늘리면 안 되는 그림이 있다. 로비 모드 카드는 기운 사다리꼴 낱장이라
        ///   9-슬라이스가 안 되고, 폭을 늘리면 테가 뭉개진다(2026-09-16).
        ///   그런 줄은 카드를 그대로 두고 **벌어지게** 하는 것이 맞다.
        /// </summary>
        public bool SpaceOnly => _spaceOnly;

        /// <summary>에디터 도구가 켠다.</summary>
        public void SetSpaceOnly(bool on) => _spaceOnly = on;
    }
}
