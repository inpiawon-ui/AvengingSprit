using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 키 큰 방 바닥 — 3단계 2026-10-22.
    ///
    /// 원작(궁수의 전설)에서 옮긴 방은 세로 15줄(16 m) 또는 21줄(21.5 m)이다. 무대 바닥 그림은 16 m(720 × 1152) 한 장이라
    /// 21.5 m 방에 그대로 늘려 붙이면(`Image.Type.Simple`) 바닥 판 · 문 · 난간이 1.34배로 세로로 늘어난다.
    ///
    /// 그래서 그림을 셋으로 잘라 쌓는다 — **위 문 띠 · 가운데 바닥 판(필요한 만큼 되풀이) · 아래 입구 난간.**
    /// 늘리지 않으므로 무늬가 안 일그러진다. 마지막 가운데 조각은 위에서부터 채워(`Filled`) 남는 만큼 자른다.
    /// 21줄 방 전용 바닥 그림이 오면 그것이 이긴다(키가 맞는 그림은 이 경로를 안 탄다).
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>무대 바닥 그림이 그린 방 높이(m).</summary>
        private const float FloorArtMeters = 16f;

        /// <summary>그림 위 문 띠 · 아래 입구 난간의 높이(그림 높이 대비) — junkyard 바닥에서 쟀다(위 2.4 m · 아래 2.9 m).</summary>
        private const float FloorTopBandFrac = 2.4f / 16f;
        private const float FloorBottomBandFrac = 2.9f / 16f;

        private Sprite _floorArt;
        private Sprite[] _floorSlices;        // 위 · 가운데 · 아래 — 그림이 바뀌면 다시 자른다
        private Sprite _floorSlicesOf;
        private readonly List<GameObject> _tallFloorParts = new();

        /// <summary>바닥 그림이 바뀌었거나 방 높이가 바뀌었다 — 키 큰 방이면 잘라 쌓고, 아니면 한 장 그대로.</summary>
        private void RefreshTallFloor()
        {
            for (int i = 0; i < _tallFloorParts.Count; i++)
                if (_tallFloorParts[i] != null) Destroy(_tallFloorParts[i]);
            _tallFloorParts.Clear();
            if (_floorImage == null || _floor == null) return;

            float roomMeters = _pxPerMeter > 0f ? _roomSize.y / _pxPerMeter : FloorArtMeters;
            bool tall = _floorArt != null && _floorImage.sprite == _floorArt && roomMeters > FloorArtMeters + 0.5f;
            _floorImage.enabled = !tall;
            if (!tall) return;

            var s = SlicesOf(_floorArt);
            float px = _pxPerMeter;
            float topH = FloorArtMeters * FloorTopBandFrac * px;
            float botH = FloorArtMeters * FloorBottomBandFrac * px;
            float midH = FloorArtMeters * (1f - FloorTopBandFrac - FloorBottomBandFrac) * px;
            float w = _roomSize.x, h = _roomSize.y;

            AddFloorPart(s[0], 0f, topH, w, 1f);
            float y = topH, end = h - botH;
            while (y < end - 0.5f)
            {
                float piece = Mathf.Min(midH, end - y);
                AddFloorPart(s[1], y, piece, w, piece / midH);   // 마지막 조각은 위에서부터 남는 만큼만
                y += piece;
            }
            AddFloorPart(s[2], end, botH, w, 1f);
        }

        /// <summary>바닥 조각 하나 — 방 위에서 `top` 픽셀 아래에 높이 `height` 로. `fill` &lt; 1 이면 위쪽만 그린다.</summary>
        private void AddFloorPart(Sprite sprite, float top, float height, float width, float fill)
        {
            var go = new GameObject("TallFloor", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_floor, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            // 조각이 잘린 것이면 그림 원래 높이로 두고 채움으로 자른다 — 늘어나지 않게
            float full = fill < 1f ? height / Mathf.Max(0.01f, fill) : height;
            rt.sizeDelta = new Vector2(width, full);
            rt.anchoredPosition = new Vector2(0f, -top);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            img.color = _floorImage.color;
            if (fill < 1f)
            {
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Vertical;
                img.fillOrigin = (int)Image.OriginVertical.Top;
                img.fillAmount = fill;
            }
            go.transform.SetAsFirstSibling();   // 바닥 레이어 안 맨 뒤 — 다른 바닥 장식보다 아래
            _tallFloorParts.Add(go);
        }

        /// <summary>바닥 그림을 위 · 가운데 · 아래로 자른 조각(같은 그림이면 한 번만 자른다).</summary>
        private Sprite[] SlicesOf(Sprite art)
        {
            if (_floorSlicesOf == art && _floorSlices != null) return _floorSlices;
            if (_floorSlices != null)
                for (int i = 0; i < _floorSlices.Length; i++)
                    if (_floorSlices[i] != null) Destroy(_floorSlices[i]);

            var r = art.textureRect;   // 텍스처 좌표는 아래가 0
            float top = r.height * FloorTopBandFrac, bot = r.height * FloorBottomBandFrac;
            float mid = r.height - top - bot;
            Sprite Cut(float y, float hh) => Sprite.Create(art.texture, new Rect(r.x, r.y + y, r.width, hh),
                                                          new Vector2(0.5f, 0.5f), art.pixelsPerUnit);
            _floorSlices = new[] { Cut(r.height - top, top), Cut(bot, mid), Cut(0f, bot) };
            _floorSlicesOf = art;
            return _floorSlices;
        }
    }
}
