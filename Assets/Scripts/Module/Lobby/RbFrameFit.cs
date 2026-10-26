using UnityEngine;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 카드 틀을 **몸통(테 선)** 기준으로 카드 칸에 맞춘다 — 틀 그림마다 왕관(SSR) · 바깥 장식 때문에 몸통 크기가 달라
    /// 그림째 칸에 늘리면 등급마다 틀 크기가 달라 보였다(PD 2026-10-26 「카드 프레임이 크기마다 다 다르고」).
    /// 몸통 상자는 빌더가 그림에서 재서 넣는다(그림 기준 0~1 · 왼 · 위 · 오른 · 아래). 장식은 칸 밖으로 나간다.
    /// </summary>
    public static class RbFrameFit
    {
        public static void Apply(RectTransform frame, Vector2 card, Sprite sprite, Sprite[] sprites, Vector4[] bodies)
        {
            if (frame == null || sprite == null || sprites == null || bodies == null) return;
            int at = System.Array.IndexOf(sprites, sprite);
            if (at < 0 || at >= bodies.Length) return;
            var body = bodies[at];
            float bw = Mathf.Max(0.01f, body.z - body.x), bh = Mathf.Max(0.01f, body.w - body.y);
            float w = card.x / bw, h = card.y / bh;
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.pivot = new Vector2(0.5f, 0.5f);
            frame.sizeDelta = new Vector2(w, h);
            // 몸통 가운데가 칸 가운데에 오게(그림 y 는 위에서 잰다)
            frame.anchoredPosition = new Vector2(-((body.x + body.z) * 0.5f - 0.5f) * w, ((body.y + body.w) * 0.5f - 0.5f) * h);
        }
    }
}
