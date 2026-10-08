// ⚠ 자동 생성 — Projects/AVSR/Tools/fx_story.py export 가 만든다. 손으로 고치지 말 것(값은 fx_story.py 에서).
using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>방 오브젝트(회복 · 악마 · 상점) 연출 층 — 연출 시안 mock_fxstory_room_*_v1 (2026-10-08).</summary>
    public static class RoomPropFxTable
    {
        public static IReadOnlyList<PopupFxLayer> Get(string kind) => kind switch
        {
            "heal" => RoomHeal,
            "devil" => RoomDevil,
            "shop" => RoomShop,
            _ => null,
        };

        private static readonly PopupFxLayer[] RoomHeal =
        {
            new() { Frames = "room_heal_ring_ripple", Phase = "open", Back = true, X = 0f, Y = 140f, W = 250f, H = 96f, Step = 0.180f,
                    T0 = 0f, T1 = 0f, Loop = true, Additive = false, Tint = new Color(1f, 1f, 1f, 1f), Alpha = 0.420f,
                    Flip = false, Fade = 0.180f, FromAvatar = false },
            new() { Frames = "room_heal_capsule_glow", Phase = "open", Back = false, X = 0f, Y = -8f, W = 138f, H = 238f, Step = 0.160f,
                    T0 = 0f, T1 = 0f, Loop = true, Additive = false, Tint = new Color(1f, 1f, 1f, 1f), Alpha = 0.380f,
                    Flip = false, Fade = 0.180f, FromAvatar = false },
            new() { Frames = "room_heal_steam", Phase = "open", Back = false, X = -12f, Y = -174f, W = 126f, H = 150f, Step = 0.160f,
                    T0 = 0.200f, T1 = 0f, Loop = true, Additive = false, Tint = new Color(1f, 1f, 1f, 1f), Alpha = 0.420f,
                    Flip = false, Fade = 0.180f, FromAvatar = false },
            new() { Frames = "room_heal_pillar", Phase = "near", Back = false, X = 0f, Y = -158f, W = 124f, H = 300f, Step = 0.120f,
                    T0 = 0f, T1 = 0f, Loop = true, Additive = true, Tint = new Color(1f, 1f, 1f, 1f), Alpha = 0.700f,
                    Flip = false, Fade = 0.080f, FromAvatar = false },
        };

        private static readonly PopupFxLayer[] RoomDevil =
        {
            new() { Frames = "room_devil_fire_smoke", Phase = "open", Back = true, X = 0f, Y = -92f, W = 270f, H = 270f, Step = 0.180f,
                    T0 = 0f, T1 = 0f, Loop = true, Additive = false, Tint = new Color(1f, 1f, 1f, 1f), Alpha = 0.300f,
                    Flip = false, Fade = 0.180f, FromAvatar = false },
            new() { Frames = "room_devil_core_glow", Phase = "open", Back = false, X = 0f, Y = -2f, W = 104f, H = 208f, Step = 0.160f,
                    T0 = 0f, T1 = 0f, Loop = true, Additive = true, Tint = new Color(1f, 1f, 1f, 1f), Alpha = 0.480f,
                    Flip = false, Fade = 0.180f, FromAvatar = false },
            new() { Frames = "room_devil_horn_fire", Phase = "open", Back = false, X = -80f, Y = -146f, W = 34f, H = 68f, Step = 0.140f,
                    T0 = 0.100f, T1 = 0f, Loop = true, Additive = true, Tint = new Color(1f, 1f, 1f, 1f), Alpha = 0.660f,
                    Flip = false, Fade = 0.140f, FromAvatar = false },
            new() { Frames = "room_devil_horn_fire", Phase = "open", Back = false, X = 80f, Y = -146f, W = 34f, H = 68f, Step = 0.140f,
                    T0 = 0.170f, T1 = 0f, Loop = true, Additive = true, Tint = new Color(1f, 1f, 1f, 1f), Alpha = 0.660f,
                    Flip = true, Fade = 0.140f, FromAvatar = false },
            new() { Frames = "room_devil_eye_flash", Phase = "near", Back = false, X = 0f, Y = -48f, W = 54f, H = 54f, Step = 0.120f,
                    T0 = 0f, T1 = 0f, Loop = true, Additive = true, Tint = new Color(1f, 1f, 1f, 1f), Alpha = 0.620f,
                    Flip = false, Fade = 0.080f, FromAvatar = false },
            new() { Frames = "room_devil_smoke_ring", Phase = "accept", Back = true, X = 0f, Y = 52f, W = 360f, H = 150f, Step = 0.090f,
                    T0 = 0f, T1 = 0.540f, Loop = false, Additive = false, Tint = new Color(1f, 1f, 1f, 1f), Alpha = 0.480f,
                    Flip = false, Fade = 0.070f, FromAvatar = false },
            new() { Frames = "room_devil_chain_release", Phase = "accept", Back = false, X = 0f, Y = 6f, W = 410f, H = 205f, Step = 0.070f,
                    T0 = 0f, T1 = 0.420f, Loop = false, Additive = false, Tint = new Color(1f, 1f, 1f, 1f), Alpha = 0.900f,
                    Flip = false, Fade = 0.040f, FromAvatar = false },
        };

        private static readonly PopupFxLayer[] RoomShop =
        {
            new() { Frames = "room_shop_soul_flame", Phase = "open", Back = false, X = 0f, Y = 4f, W = 184f, H = 294f, Step = 0.160f,
                    T0 = 0f, T1 = 0f, Loop = true, Additive = true, Tint = new Color(1f, 1f, 1f, 1f), Alpha = 0.300f,
                    Flip = false, Fade = 0.150f, FromAvatar = false },
            new() { Frames = "room_shop_coin_float", Phase = "open", Back = false, X = 0f, Y = -18f, W = 390f, H = 220f, Step = 0.180f,
                    T0 = 0.100f, T1 = 0f, Loop = true, Additive = true, Tint = new Color(1f, 1f, 1f, 1f), Alpha = 0.620f,
                    Flip = false, Fade = 0.150f, FromAvatar = false },
            new() { Frames = "room_shop_lantern", Phase = "open", Back = false, X = -91f, Y = -49f, W = 34f, H = 34f, Step = 0.180f,
                    T0 = 0f, T1 = 0f, Loop = true, Additive = true, Tint = new Color(1f, 1f, 1f, 1f), Alpha = 0.580f,
                    Flip = false, Fade = 0.150f, FromAvatar = false },
            new() { Frames = "room_shop_lantern", Phase = "open", Back = false, X = 91f, Y = -49f, W = 34f, H = 34f, Step = 0.200f,
                    T0 = 0.180f, T1 = 0f, Loop = true, Additive = true, Tint = new Color(1f, 1f, 1f, 1f), Alpha = 0.580f,
                    Flip = false, Fade = 0.150f, FromAvatar = false },
            new() { Frames = "room_shop_lantern", Phase = "open", Back = false, X = -91f, Y = 23f, W = 34f, H = 34f, Step = 0.220f,
                    T0 = 0.360f, T1 = 0f, Loop = true, Additive = true, Tint = new Color(1f, 1f, 1f, 1f), Alpha = 0.580f,
                    Flip = false, Fade = 0.150f, FromAvatar = false },
            new() { Frames = "room_shop_lantern", Phase = "open", Back = false, X = 91f, Y = 23f, W = 34f, H = 34f, Step = 0.240f,
                    T0 = 0.540f, T1 = 0f, Loop = true, Additive = true, Tint = new Color(1f, 1f, 1f, 1f), Alpha = 0.580f,
                    Flip = false, Fade = 0.150f, FromAvatar = false },
            new() { Frames = "room_shop_eye_glow", Phase = "near", Back = false, X = 0f, Y = -71f, W = 48f, H = 24f, Step = 0.100f,
                    T0 = 0.040f, T1 = 0.460f, Loop = false, Additive = true, Tint = new Color(1f, 1f, 1f, 1f), Alpha = 0.780f,
                    Flip = false, Fade = 0.050f, FromAvatar = false },
            new() { Frames = "room_shop_coin_ring", Phase = "accept", Back = false, X = 0f, Y = 82f, W = 500f, H = 210f, Step = 0.080f,
                    T0 = 0f, T1 = 0.480f, Loop = false, Additive = true, Tint = new Color(1f, 1f, 1f, 1f), Alpha = 0.580f,
                    Flip = false, Fade = 0.060f, FromAvatar = false },
        };
    }
}
