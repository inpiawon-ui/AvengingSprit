#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 에디터 전용 자동 조종 — 판 영상을 찍을 때(PD 2026-10-30 「1챕터 플레이 영상」). 빌드에는 안 들어간다.
    /// 가까운 적에게 다가가 사거리 안에서 멈춰 쏘고, 방이 비면 문으로 걷는다. 창은 첫 선택으로 넘긴다:
    /// 설명 · 따라 하기 · 셋 고르기 · 빙의 선택(추천 줄) · 메인 사망(첫 줄) · 레벨업(첫 카드) · 상점 · 이벤트 · 제단(닫기).
    /// 쓰러지지 않게 무적을 건다 — 화면에 보이는 것만 찍기 위한 것이다.
    /// `BattleDirector.AutoPilot = true` 로 켠다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        public static bool AutoPilot;
        /// <summary>끄면 맞고 죽는다 — 난이도를 잴 때(밸런스 측정 PD 2026-11-02). 영상은 켠 채로.</summary>
        public static bool AutoPilotInvuln = true;
        private float _apUiClock;
        private Vector2 _apLastPos;
        private float _apStuck;
        private float _apSideTime;
        private Vector2 _apSide;

        private static readonly string[] ApWindows = { "ShopPanel", "EventPanel", "ShrinePanel" };
        private static readonly string[] ApCloseWords = { "Close", "Leave", "Exit", "Skip", "Decline", "No", "Cancel", "Back" };

        private void TickAutoPilot()
        {
            if (!AutoPilot || !_running) return;
            if (AutoPilotInvuln) _invuln = Mathf.Max(_invuln, 0.5f);

            _apUiClock += Time.unscaledDeltaTime;
            if (_apUiClock >= 0.6f)
            {
                _apUiClock = 0f;
                if (ApUi()) { MoveInput = Vector2.zero; return; }
            }
            if (Game.Module.Common.UI.RbGuide.PausingOpen || _partyHold || _awaitingBuff || _trio.Count > 0 || _coachTarget != null)
            {
                MoveInput = Vector2.zero;
                return;
            }
            ApSteer();
        }

        /// <summary>떠 있는 창 하나를 넘겼으면 true.</summary>
        private bool ApUi()
        {
            var guide = Object.FindAnyObjectByType<Game.Module.Common.UI.RbGuide>();
            if (guide != null && guide.IsOpen) { guide.Close(); return true; }
            if (_trio.Count > 0) { PickTrio(_trio[Random.Range(0, _trio.Count)]); return true; }
            if (_coachTarget != null) { var t = _coachTarget; if (!CoachTap(t)) ApTake(t); return true; }
            if (_partyPopup != null) { ApClickPopup(); return true; }
            if (_awaitingBuff && GameObject.Find("BuffCard0") is GameObject card && card.TryGetComponent<Button>(out var cb))
            {
                cb.onClick.Invoke();
                return true;
            }
            foreach (var w in ApWindows)
                if (GameObject.Find(w) is GameObject win && win.activeInHierarchy && ApClose(win)) return true;
            if (GameObject.Find("RbNextRoom") is GameObject next && next.activeInHierarchy) { next.SetActive(false); TakeOnlyExit(); return true; }

            // 보조 자리가 비었고 데려올 수 있는 적이 있으면 데려온다(추천 줄)
            // 첫 판(정해 둔 코만도가 남았으면)은 따라 하기가 데려오게 둔다 — 다른 몸을 먼저 데려가면 무리 · 인연이 안 켜진다
            bool tutorialPending = _tutorialRun && (_tutorialSubs.Count > 0 || (_tutorialSub != null && _tutorialSub.IsAlive && _enemies.Contains(_tutorialSub)));
            if (!tutorialPending && _host != null && _subs.Count < PartyRule.SubSlots && PartyCanChoose() && NearestPossessable() is Unit sub
                && Vector2.Distance(sub.Position, _host.Position) < 500f)
            {
                OpenPartyChoice(sub);
                return true;
            }
            // 유령이면 가까운 빙의 가능한 적에게
            if (_host == null && _ghost != null && NearestPossessableTo(_ghost.Position) is Unit body)
            {
                ApTake(body);
                return true;
            }
            return false;
        }

        private void ApTake(Unit u)
        {
            if (u == null) return;
            if (_host == null) { _possessTarget = u; _repossessLock = 0f; TryPossess(); }
            else OpenPartyChoice(u);
        }

        private void ApClickPopup()
        {
            // 추천 배지가 있는 줄 → 없으면 켜진 첫 단추
            Button first = null;
            foreach (var b in _partyPopup.GetComponentsInChildren<Button>())
            {
                if (!b.interactable) continue;
                if (b.transform.Find("BestBadge") != null) { b.onClick.Invoke(); return; }
                if (first == null) first = b;
            }
            first?.onClick.Invoke();
        }

        private static bool ApClose(GameObject win)
        {
            Button last = null;
            foreach (var b in win.GetComponentsInChildren<Button>())
            {
                if (!b.interactable) continue;
                foreach (var w in ApCloseWords)
                    if (b.name.Contains(w)) { b.onClick.Invoke(); return true; }
                last = b;
            }
            if (last == null) return false;
            last.onClick.Invoke();
            return true;
        }

        private void ApSteer()
        {
            var me = Avatar;
            if (me == null) { MoveInput = Vector2.zero; return; }
            Unit target = _boss != null && _boss.IsAlive ? _boss : null;
            float best = float.MaxValue;
            if (target == null)
                for (int i = 0; i < _enemies.Count; i++)
                {
                    var e = _enemies[i];
                    if (e == null || !e.IsAlive) continue;
                    float d = (e.Position - me.Position).sqrMagnitude;
                    if (d < best) { best = d; target = e; }
                }
            Vector2 goal;
            float stop;
            if (target != null) { goal = target.Position; stop = Mathf.Max(80f, me.AttackRange * 0.75f); }
            else if (_exitOpen && _exits.Count > 0 && _exits[0].View != null) { goal = _exits[0].View.anchoredPosition; stop = 10f; }
            else { MoveInput = Vector2.zero; return; }
            var to = goal - me.Position;
            if (to.magnitude <= stop) { MoveInput = Vector2.zero; _apStuck = 0f; return; }
            // 막힘 — 걸으려는데 0.5초 동안 거의 안 움직였으면 0.8초 옆으로 비켜 걷는다(드럼통 · 상자)
            float dt = Time.deltaTime;
            if (_apSideTime > 0f) { _apSideTime -= dt; MoveInput = (_apSide + to.normalized * 0.3f).normalized; _apLastPos = me.Position; return; }
            _apStuck = (me.Position - _apLastPos).sqrMagnitude < 0.25f ? _apStuck + dt : 0f;
            _apLastPos = me.Position;
            if (_apStuck > 0.5f)
            {
                _apStuck = 0f;
                _apSideTime = 0.8f;
                var side = new Vector2(-to.y, to.x).normalized;
                _apSide = Random.value < 0.5f ? side : -side;
            }
            MoveInput = to.normalized;
        }
    }
}
#endif
