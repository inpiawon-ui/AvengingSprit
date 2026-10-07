using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 내 공격 사거리 원 (2026-10-07) — **근접 몸**의 발밑에 옅은 흰 원을 깐다(원거리 · 유령은 안 그린다 — PD).
    /// 공격이 어디까지 닿는지 한눈에 읽히게(다른 게임들이 쓰는 방식). 몸을 바꾸면 그 몸의 사거리로 바뀐다.
    ///
    /// 그림은 발주본(`range_ring` — 정원 흰 선 한 줄)이고 여기서는 크기 · 자리 · 진하기만 맞춘다.
    /// 진하기는 `GameConfig._rangeRingAlpha`(0 이면 끔). 바닥 층(`_fieldLayer`)에 깔아 캐릭터 · 적 · 탄 아래에 있다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>
        /// 원 그림에서 선 가운데까지의 반지름 비(그림 반 폭 = 1). 선 가운데가 사거리에 오게 그림을 이만큼 더 크게 깐다.
        /// 발주본을 재서 정한 값이다(`Projects/AVSR/Tools/range_ring_fit.py` — 2026-10-07 실측 0.951).
        /// </summary>
        private const float RangeRingLineRatio = 0.951f;

        private RectTransform _rangeRing;
        private Image _rangeRingImage;

        private void TickRangeRing()
        {
            var me = Avatar;
            float alpha = _config != null ? _config.RangeRingAlpha : 0f;
            // ⚠ 공격 판정과 **같은 자**로 잰다(`TickPlayer` — EffectiveRange × 버프 배율). 유닛 값만 보면
            //   근접 몸의 최소 사거리 · 사거리 버프가 빠져 원과 실제로 닿는 거리가 어긋난다.
            float range = me != null ? EffectiveRange(me) * _buffs.RangeMul : 0f;
            // 근접 몸만 — 원거리 몸 · 유령까지 그리니 흰 원이 늘 떠 있어 과했다(PD 2026-10-07)
            bool melee = me != null && me.Profile != null && IsMeleeKind(me.Profile.Kind);
            bool show = alpha > 0f && melee && me.IsAlive && range > 0f && _fieldLayer != null;
            if (_rangeRing == null)
            {
                if (!show) return;
                var art = GetSprite("range_ring");
                if (art == null) return;   // 그림이 없으면 원을 그리지 않는다 — 코드로 대신 그리지 않는다
                var go = new GameObject("RangeRing", typeof(RectTransform));
                _rangeRing = (RectTransform)go.transform;
                _rangeRing.SetParent(_fieldLayer, false);
                _rangeRing.anchorMin = _rangeRing.anchorMax = new Vector2(0f, 1f);
                _rangeRing.pivot = new Vector2(0.5f, 0.5f);
                _rangeRingImage = go.AddComponent<Image>();
                _rangeRingImage.sprite = art;
                _rangeRingImage.raycastTarget = false;
                // 바닥 효과(장판 등) 아래 — 장판이 원에 가리면 장판이 안 읽힌다
                _rangeRing.SetAsFirstSibling();
            }
            if (_rangeRing.gameObject.activeSelf != show) _rangeRing.gameObject.SetActive(show);
            if (!show) return;

            // 유령(시작 몸)은 사거리가 방 전체(900px)라 원이 화면 밖으로 나간다 — 그것이 맞다(어디든 닿는다)
            float size = range * 2f / RangeRingLineRatio;
            _rangeRing.sizeDelta = new Vector2(size, size);
            _rangeRing.anchoredPosition = me.Position;
            var c = _rangeRingImage.color;
            if (!Mathf.Approximately(c.a, alpha)) _rangeRingImage.color = new Color(1f, 1f, 1f, alpha);
        }
    }
}
