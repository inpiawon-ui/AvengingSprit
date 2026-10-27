using UnityEngine;

namespace Game.Module.Common.UI
{
    /// <summary>
    /// 「이 판은 가로로 늘리지 마라」 표시. <see cref="ScreenFit"/> 이 읽는다 — 폭은 그린 그대로 두고 가운데에 둔다.
    /// 세로는 평소대로 위 · 아래 변에 붙는다(20:9 에서 조작 판이 화면 아래로 내려가는 것은 그대로).
    ///
    /// 인게임 상단 HUD · 하단 조작 판은 720 폭으로 그려 「부모를 꽉 채운다」로 읽혀 화면 끝까지 늘었다.
    /// 그런데 방(필드)은 늘 720 폭이라, 16:10 · 4:3 처럼 넓은 화면에서 Lv 줄 · 필살기 버튼이
    /// 방 밖 검은 띠로 삐져나갔다(PD 2026-10-27).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ScreenFitKeepWidth : MonoBehaviour
    {
    }
}
