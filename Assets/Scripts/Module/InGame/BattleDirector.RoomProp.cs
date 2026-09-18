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
        /// <summary>물건에 이만큼 다가서면 작동한다(px). 발밑이 겹칠 필요는 없다.</summary>
        private const float RoomPropTouchRadius = 96f;

        /// <summary>물건이 서는 자리 — 방 가로 한가운데, 세로로는 조금 위.</summary>
        private const float RoomPropYRatio = 0.42f;

        private RectTransform _roomProp;
        private Image _roomPropImg;
        private bool _roomPropUsed;

        /// <summary>회복 제단을 세운다. 그림이 없으면 아무것도 안 세운다.</summary>
        private void SpawnHealShrine() => SpawnRoomProp("obj_heal_shrine", 192f, 192f);

        /// <summary>상점 가판을 세운다.</summary>
        private void SpawnShopStall() => SpawnRoomProp("obj_shop_stall", 224f, 192f);

        /// <summary>
        /// 악마의 제단을 세운다. 회복 제단(천사)과 **같은 크기**로 선다 —
        /// 004 는 둘 중 하나가 서는 자리라, 크기가 다르면 어느 쪽이 왔는지가
        /// 그림이 아니라 덩치로 먼저 읽힌다.
        /// </summary>
        private void SpawnDevilAltar() => SpawnRoomProp("obj_devil_altar", 192f, 192f);

        /// <summary>
        /// 중간보스를 잡은 방의 악마의 제단 — **방 한가운데**에 선다(기획 2026-09-18).
        /// 이 방은 전투방이라 방 종류로는 제단인 줄 모른다 — 따로 표시해 둔다.
        /// </summary>
        private void SpawnDevilAltarCenter()
        {
            SpawnDevilAltar();
            _devilAltarHere = _roomProp != null;
            if (_roomProp != null) _roomProp.anchoredPosition = RoomPropAt();
        }

        /// <summary>이 방의 물건이 전투 뒤에 선 악마의 제단인가.</summary>
        private bool _devilAltarHere;

        private void SpawnRoomProp(string artKey, float w, float h)
        {
            ClearRoomProp();
            if (_unitLayer == null) return;

            var art = GetSprite(artKey);
            // ⚠ 그림이 아직 없으면 **세우지 않는다.** 흰 네모를 세워 두면
            //   플레이어가 그걸 물건으로 알고 다가온다 — 없는 편이 낫다.
            if (art == null) return;

            var go = new GameObject($"RoomProp_{artKey}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_unitLayer, false);
            _roomProp = (RectTransform)go.transform;
            _roomProp.anchorMin = _roomProp.anchorMax = new Vector2(0f, 1f);
            _roomProp.pivot = new Vector2(0.5f, 0.5f);
            _roomProp.sizeDelta = new Vector2(w, h);
            _roomProp.anchoredPosition = RoomPropAt();

            _roomPropImg = go.GetComponent<Image>();
            _roomPropImg.sprite = art;
            _roomPropImg.raycastTarget = false;
            _roomPropImg.preserveAspect = true;
            _roomPropUsed = false;
        }

        /// <summary>
        /// 물건 자리. 보스를 잡은 방의 악마의 제단은 **방 한가운데**에 선다(기획 2026-09-17) —
        /// 보스가 사라진 아레나 복판이 곧 보상 자리다.
        /// </summary>
        private Vector2 RoomPropAt()
            => new(_roomSize.x * 0.5f, -_roomSize.y * (_devilAltarHere ? 0.5f : RoomPropYRatio));

        private void ClearRoomProp()
        {
            if (_roomProp != null) Destroy(_roomProp.gameObject);
            _roomProp = null;
            _roomPropImg = null;
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
            var me = Avatar;
            if (me == null) return;
            if (Vector2.Distance(me.Position, RoomPropAt()) > RoomPropTouchRadius) return;

            _roomPropUsed = true;
            if (_roomKind == RoomKind.Rest) OpenShrine();
            else if (_roomKind == RoomKind.Shop) OpenShop();
            // 악마의 제단 — 중간보스를 잡은 방에 선다(예전 004 이벤트 방 규칙도 남겨 둔다)
            else if (_roomKind == RoomKind.Event || _devilAltarHere) OfferEvent();

            // 다 쓴 물건은 흐릿하게 남긴다. 지우면 "내가 뭘 했더라" 가 된다.
            if (_roomPropImg != null) _roomPropImg.color = new Color(1f, 1f, 1f, 0.45f);
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
