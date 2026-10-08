using System;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 팝업 연출 한 벌 — 창(패널)마다 붙는다. 값은 <c>InGamePopupCLayout</c> 이 <c>Projects/AVSR/Tools/popup_fx.json</c> 에서 굽는다
    /// (합성 미리보기 <c>fx_story.py</c> 와 같은 자를 쓴다 — 여기에 숫자를 손으로 적지 않는다).
    ///
    /// 좌표는 패널(720x1280) 왼쪽 위 기준 화면 좌표. 시간은 창이 열린 뒤(<see cref="PopupFxLayer.Phase"/> = open)
    /// 또는 고른 순간(accept) 뒤의 초.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PopupFxSpec : MonoBehaviour
    {
        [SerializeField] private PopupFxLayer[] _layers = Array.Empty<PopupFxLayer>();
        /// <summary>더하기 섞기 재질(<c>UI/AdditiveSprite</c>). 프리팹이 쥐고 있어야 빌드에 셰이더가 들어간다.</summary>
        [SerializeField] private Material _additive;

        public PopupFxLayer[] Layers => _layers;
        public Material Additive => _additive;

        public void Set(PopupFxLayer[] layers, Material additive)
        {
            _layers = layers ?? Array.Empty<PopupFxLayer>();
            _additive = additive;
        }
    }

    [Serializable]
    public sealed class PopupFxLayer
    {
        /// <summary>그림 이름 앞부분 — 아틀라스의 <c>fx_{Frames}_{n}</c>.</summary>
        public string Frames;
        /// <summary>open = 창이 떠 있는 동안 · accept = 고른 순간(창이 닫혀도 위 덮개에서 끝까지 돈다).</summary>
        public string Phase = "open";
        /// <summary>back = 창 틀 뒤 · front = 창 위.</summary>
        public bool Back;
        public float X, Y, W, H;
        public float Step = 0.1f;
        public float T0;
        /// <summary>0 이하이면 창이 닫힐 때까지.</summary>
        public float T1;
        public bool Loop = true;
        public bool Additive = true;
        public Color Tint = Color.white;
        public float Alpha = 1f;
        public bool Flip;
        public float Fade = 0.15f;
        /// <summary>빔: (X,Y) → (X2,Y2) 로 늘인다. <see cref="FromAvatar"/> 면 출발점이 플레이어 몸.</summary>
        public bool Beam;
        public float X2, Y2;
        public bool FromAvatar;
        /// <summary>이 이름의 노드 가운데에 붙는다(비면 X,Y 그대로). 상점 산 칸처럼 자리가 매번 바뀌는 것.</summary>
        public string AtNode;
        /// <summary>고른 순간 거둔다(악마 수락 버튼 빛 — 수락 뒤엔 버튼 재질의 테두리만).</summary>
        public bool UntilAccept;
        /// <summary>
        /// <see cref="AtNode"/> 칸 **안**, 칸 그림 바로 위 · 칸 안 글자 아래에 그린다.
        /// 칸에 붙는 빛이 이름 · 가격 · 등급 글자를 덮지 않게(PD 2026-10-08 「게임 정보는 가리면 안 돼」).
        /// </summary>
        public bool InNode;
    }
}
