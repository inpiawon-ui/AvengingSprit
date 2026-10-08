using Game.Module.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 적이 없는 방(회복·상점)의 **가운데 물건**.
    ///
    /// 예전에는 방에 들어서는 순간 회복이 되고 상점이 열렸다. 그러면 방이
    /// 화면 한 장으로 끝나서 「지나가는 복도」로 읽힌다 — 걸어가서 만져야
    /// 방에 들어온 이유가 생긴다(기획 2026-09-08).
    ///
    /// 물건은 하나뿐이고 한 번만 작동한다. 출구는 들어서자마자 열려 있으므로
    /// 그냥 지나칠 수도 있다 — **쓸지 말지가 선택**이다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>물건 **발밑**에 이만큼 다가서면 작동한다(px). 발밑이 겹칠 필요는 없다.</summary>
        private const float RoomPropTouchRadius = 110f;

        // ── C 「원혼 회로」 물건(2026-10-08) ─────────────────────────
        // PD 「맵에 하나만 서는데 너무 작아 티가 안 난다 · 디자인도 새로」 — 그림 칸 300x380(납품)을 이 크기로 세운다.
        // 예전 192 px 는 유닛(144 px)과 덩치가 같아 방 장식으로 읽혔다. 셋 다 같은 크기 — 덩치가 아니라 그림으로 갈린다.
        // ⚠ 330 px 로 세웠더니 다가설 때 카메라가 플레이어를 따라 내려가 머리 위 표식이 HUD 밑에 가렸다 — 300 px · 표식을 바짝.
        private const float RoomPropWidth = 236f;
        private const float RoomPropHeight = 300f;
        /// <summary>그림 맨 아래(다리 끝)에서 바닥 링 가운데까지 올린 거리.</summary>
        private const float RoomPropRingLift = 14f;
        private const float RoomPropRingWidth = 230f;
        private const float RoomPropRingHeight = 86f;
        private const float RoomPropMarkSize = 52f;
        /// <summary>머리 위 표식이 그림 꼭대기에서 떠 있는 높이와 오르내림 폭 · 주기.</summary>
        private const float RoomPropMarkGap = 12f;
        private const float RoomPropMarkBob = 6f;
        private const float RoomPropMarkBobSeconds = 1.4f;

        /// <summary>물건이 서는 자리 — 방 가로 한가운데, 세로로는 조금 위.</summary>
        private const float RoomPropYRatio = 0.42f;

        private RectTransform _roomProp;
        private Image _roomPropImg;
        private RectTransform _roomPropRing;
        private RectTransform _roomPropMark;
        private Image _roomPropRingImg;
        private Image _roomPropMarkImg;
        private float _roomPropBobTime;
        private bool _roomPropUsed;

        /// <summary>회복 제단을 세운다. 그림이 없으면 아무것도 안 세운다.</summary>
        private void SpawnHealShrine() => SpawnRoomProp("obj_heal_shrine", "heal");

        /// <summary>상점 가판을 세운다.</summary>
        private void SpawnShopStall() => SpawnRoomProp("obj_shop_stall", "shop");

        /// <summary>
        /// 악마의 제단을 세운다. 회복 제단(천사)과 **같은 크기**로 선다 —
        /// 004 는 둘 중 하나가 서는 자리라, 크기가 다르면 어느 쪽이 왔는지가
        /// 그림이 아니라 덩치로 먼저 읽힌다.
        /// </summary>
        private void SpawnDevilAltar() => SpawnRoomProp("obj_devil_altar", "devil");

        /// <summary>
        /// 중간보스를 잡은 방의 악마의 제단 — **방 한가운데**에 선다(기획 2026-09-18).
        /// 이 방은 전투방이라 방 종류로는 제단인 줄 모른다 — 따로 표시해 둔다.
        /// </summary>
        private void SpawnDevilAltarCenter()
        {
            SpawnDevilAltar();
            _devilAltarHere = _roomProp != null;
            if (_roomProp != null) PlaceRoomProp();
        }

        /// <summary>이 방의 물건이 전투 뒤에 선 악마의 제단인가.</summary>
        private bool _devilAltarHere;

        /// <param name="kind">heal · devil · shop — 바닥 링(`obj_ring_{kind}`) · 머리 위 표식(`obj_mark_{kind}`) 그림 이름.</param>
        private void SpawnRoomProp(string artKey, string kind)
        {
            ClearRoomProp();
            if (_unitLayer == null) return;

            var art = GetSprite(artKey);
            // ⚠ 그림이 아직 없으면 **세우지 않는다.** 흰 네모를 세워 두면
            //   플레이어가 그걸 물건으로 알고 다가온다 — 없는 편이 낫다.
            if (art == null) return;

            // 바닥 링이 몸보다 **먼저**(뒤에) 깔려야 다리가 링 위에 선다
            _roomPropRing = MakePropImage($"RoomPropRing_{kind}", GetSprite($"obj_ring_{kind}"),
                                          RoomPropRingWidth, RoomPropRingHeight, out _roomPropRingImg);
            _roomProp = MakePropImage($"RoomProp_{artKey}", art, RoomPropWidth, RoomPropHeight, out _roomPropImg);
            _roomPropMark = MakePropImage($"RoomPropMark_{kind}", GetSprite($"obj_mark_{kind}"),
                                          RoomPropMarkSize, RoomPropMarkSize, out _roomPropMarkImg);
            _roomPropBobTime = 0f;
            PlaceRoomProp();
            _roomPropUsed = false;
        }

        private RectTransform MakePropImage(string name, Sprite sprite, float w, float h, out Image img)
        {
            img = null;
            if (sprite == null) return null;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_unitLayer, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            img.preserveAspect = true;
            return rt;
        }

        /// <summary>몸 · 바닥 링 · 머리 위 표식을 물건 자리에 함께 놓는다.</summary>
        private void PlaceRoomProp()
        {
            var at = RoomPropAt();
            if (_roomProp != null) _roomProp.anchoredPosition = at;
            if (_roomPropRing != null) _roomPropRing.anchoredPosition = RoomPropFeet() + Vector2.up * RoomPropRingLift;
            if (_roomPropMark != null)
                _roomPropMark.anchoredPosition = at + Vector2.up * (RoomPropHeight * 0.5f + RoomPropMarkGap);
        }

        /// <summary>물건 발밑 — 그림이 커서(300 px) 가운데로 재면 다가서도 닿지 않는다. 닿기 판정은 여기로 잰다.</summary>
        private Vector2 RoomPropFeet() => RoomPropAt() + Vector2.down * (RoomPropHeight * 0.5f);

        /// <summary>
        /// 물건 자리. 보스를 잡은 방의 악마의 제단은 **방 한가운데**에 선다(기획 2026-09-17) —
        /// 보스가 사라진 아레나 복판이 곧 보상 자리다.
        /// </summary>
        private Vector2 RoomPropAt()
            => new(_roomSize.x * 0.5f, -_roomSize.y * (_devilAltarHere ? 0.5f : RoomPropYRatio));

        private void ClearRoomProp()
        {
            if (_roomProp != null) Destroy(_roomProp.gameObject);
            if (_roomPropRing != null) Destroy(_roomPropRing.gameObject);
            if (_roomPropMark != null) Destroy(_roomPropMark.gameObject);
            _roomProp = null;
            _roomPropImg = null;
            _roomPropRing = null;
            _roomPropRingImg = null;
            _roomPropMark = null;
            _roomPropMarkImg = null;
            _roomPropUsed = false;
            _devilAltarHere = false;
        }

        /// <summary>
        /// 물건에 다가섰는가. 다가섰으면 그 방의 일을 한 번 하고 문을 연다.
        ///
        /// ⚠ 한 번 쓰면 `_roomPropUsed` 로 잠근다. 안 잠그면 물건 옆에 서 있는
        ///   동안 매 프레임 회복이 들어와 체력이 끝없이 찬다.
        /// </summary>
        private void TickRoomProp()
        {
            if (_roomProp == null || _roomPropUsed) return;
            if (_roomPropMark != null)
            {
                // 머리 위 표식이 천천히 오르내린다 — 멈춰 있으면 그림의 일부로 읽힌다
                _roomPropBobTime += Time.deltaTime;
                float bob = Mathf.Sin(_roomPropBobTime * (2f * Mathf.PI / RoomPropMarkBobSeconds)) * RoomPropMarkBob;
                _roomPropMark.anchoredPosition = RoomPropAt()
                    + Vector2.up * (RoomPropHeight * 0.5f + RoomPropMarkGap + bob);
            }
            var me = Avatar;
            if (me == null) return;
            if (Vector2.Distance(me.Position, RoomPropFeet()) > RoomPropTouchRadius) return;

            _roomPropUsed = true;
            if (_roomKind == RoomKind.Rest) OpenShrine();
            else if (_roomKind == RoomKind.Shop) OpenShop();
            // 악마의 제단 — 중간보스를 잡은 방에 선다(예전 004 이벤트 방 규칙도 남겨 둔다)
            else if (_roomKind == RoomKind.Event || _devilAltarHere) OfferEvent();

            // 다 쓴 물건은 흐릿하게 남긴다. 지우면 "내가 뭘 했더라" 가 된다.
            if (_roomPropImg != null) _roomPropImg.color = new Color(1f, 1f, 1f, 0.45f);
            if (_roomPropRingImg != null) _roomPropRingImg.color = new Color(1f, 1f, 1f, 0.3f);
            // 표식은 「아직 쓸 수 있다」는 뜻이라 다 쓰면 내린다
            if (_roomPropMark != null) _roomPropMark.gameObject.SetActive(false);
        }

        /// <summary>
        /// 제단에 닿았다. 몸과 유령을 함께 돌려준다.
        ///
        /// 정본 v3.3 REST_MASTER — 챕터가 깊어질수록 덜 돌려준다.
        ///   CH1 몸 25% / 유령 22%   CH2 22 / 20   CH3~ 19 / 18
        /// </summary>
        private void UseHealShrine()
        {
            int restCh = Mathf.Clamp(_runChapter, 1, 3);
            int hostPct = restCh == 1 ? 25 : restCh == 2 ? 22 : 19;
            int ghostPct = restCh == 1 ? 22 : restCh == 2 ? 20 : 18;

            var at = RoomPropAt();

            int ghostGain = GhostHpMax * ghostPct / 100;
            int before = _ghostHp;
            _ghostHp = Mathf.Min(GhostHpMax, _ghostHp + ghostGain);
            int ghostReal = _ghostHp - before;

            int hostReal = 0;
            if (_host != null)
            {
                int room = _host.HpMax - _host.Hp;
                int gain = Mathf.Max(1, _host.HpMax * hostPct / 100);
                hostReal = Mathf.Min(gain, room);
                _host.Heal(gain);
            }

            PublishHp();

            // **얼마나 찼는지 보여야 한다.** 막대만 움직이면 얼마를 받았는지 모른다.
            PlayFx("heal_plus", at, 128f, loop: false);
            if (hostReal > 0) ShowHeal(at, hostReal);
            else if (ghostReal > 0) ShowHeal(at, ghostReal);
        }
    }
}
