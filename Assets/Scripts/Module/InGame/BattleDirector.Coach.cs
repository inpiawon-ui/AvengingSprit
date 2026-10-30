using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 따라 하는 튜토리얼(PD 2026-10-30 「처음에 유령으로 시작해서 … 필요한 곳에 포커싱하고 캐릭터를 눌러야 한다고 알려주고 누를 때까지 기다리는 거」).
    ///   ① 유령일 때 처음 빙의할 수 있는 적이 생기면 → 판을 세우고 그 적만 밝게 · 손가락 · 「눌러 빙의」 → 그 적을 누를 때까지 기다린다
    ///   ② 처음 보조 1 · 보조 2 를 데려올 수 있을 때 → 같은 방식으로 적을 짚고, 선택창이 뜨면 「보조 넣기」 줄을 짚는다
    /// 단계마다 평생 한 번(PlayerPrefs `tut.coach.{단계}`). 그림 설명 창(RbGuide)이 떠 있으면 닫힐 때까지 기다린다.
    /// 첫 보조 · 둘째 보조가 생기면 그림 설명(party · synergy)을 한 번씩 띄운다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const string CoachPrefix = "tut.coach.";
        // 인게임 캔버스는 선형 색이라 0.78 은 거의 안 어두웠다(2026-10-30 캡처) — 0.95 면 둘레가 확실히 가라앉는다
        private const float CoachDim = 0.95f;
        private const float CoachPad = 26f;

        private Unit _coachTarget;
        private string _coachStep;
        private bool _coachRowPending;
        private GameObject _coachRoot;
        private RectTransform _coachRt, _coachHand, _coachRing, _coachTap;
        private Image _coachTapImg;
        private readonly RectTransform[] _coachDark = new RectTransform[4];
        private Sprite _coachHandSprite, _coachRingSprite;
        private int _coachSubsSeen = -1;
        /// <summary>빙의 직후엔 다음 단계를 바로 잇지 않는다 — 몸이 들어가는 연출 위에 보조 안내가 겹쳤다(2026-10-30 시험).</summary>
        private float _coachQuietUntil;

        /// <summary>따라 하기 · 셋 고르기 화면이 떠 있나 — 화면이 그림 설명을 겹쳐 띄우지 않게 묻는다.</summary>
        public bool CoachShowing => _coachRoot != null;

        /// <summary>일시정지 창이 떠 있다 — 판 갱신을 세운다(RbPausePanel 이 켜고 끈다).</summary>
        public bool PausePanelOpen { get; set; }

        /// <summary>밤 시작 호스트 고르기 창이 떠 있다.</summary>
        public bool NightPickOpen { get; set; }

        /// <summary>셋 고르기 · 따라 하기 — 뒤로 키로 일시정지를 열지 않는다(검은 막 밑에 깔려 못 닫는다).</summary>
        public bool TutorialHolding => _coachRoot != null || _trio.Count > 0;

        /// <summary>같은 오브젝트의 화면 — 선택창을 한 번도 안 열었으면 아직 안 잡혀 있다(_popUi 는 PopupBoard 가 잡는다).</summary>
        private InGameMainUI Ui
        {
            get
            {
                if (_popUi == null) TryGetComponent(out _popUi);
                return _popUi;
            }
        }

        private static bool CoachSeen(string step) => PlayerPrefs.GetInt(CoachPrefix + step, 0) == 1;

        private static void CoachMark(string step)
        {
            PlayerPrefs.SetInt(CoachPrefix + step, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Update 에서 매 프레임 — 따라 하기 중이면 true(판을 세운다).</summary>
        private bool TickCoach()
        {
            if (TickTrio()) return true;   // 유령 출전 — 셋 중 하나를 고를 때까지(BattleDirector.StartTrio)
            if (_trioPending) return false;   // 셋이 서기 전(첫 판 그림 설명 대기) — 다른 안내를 끼우지 않는다
            if (_coachTarget != null)
            {
                // 선택창(메인 사망 등)이 뜨면 짚기를 거둔다 — 남아 있으면 시계가 서고 막이 창을 덮어 판이 멈춘다(본 것으로 치지 않음)
                if (_partyHold) { EndCoach(false); return false; }
                if (!_coachTarget.IsAlive || !_coachTarget.IsPossessable || !_enemies.Contains(_coachTarget)) { EndCoach(false); return false; }
                PlaceCoachOn((RectTransform)_coachTarget.transform, true);
                return true;
            }
            if (!PartyRule.Enabled || IsModeRun || !_running || _partyHold || _awaitingBuff || IsPresentationHeld
                || IsCastFrozen || IsChanneling || PartyLinking) return false;
            if (Time.unscaledTime < _coachQuietUntil) return false;
            NoteSubsForGuide();

            string step;
            Unit target;
            if (_host == null)
            {
                step = "main";
                if (CoachSeen(step) || _ghost == null) return false;
                target = NearestPossessableTo(_ghost.Position);
            }
            else
            {
                if (_subs.Count >= PartyRule.SubSlots || _ghostHp <= PartyRule.CostSub) return false;
                step = _subs.Count == 0 ? "sub1" : "sub2";
                if (CoachSeen(step)) return false;
                // 따라 하기 첫 판은 정해 둔 호스트를 세워 짚는다(무리 · 인연을 꼭 겪게) — 없으면 가까운 빙의 가능한 적
                target = SpawnTutorialSub() ?? (_tutorialSub != null && _tutorialSub.IsAlive ? null : NearestPossessable());
            }
            if (target == null || target == _channelBody || !OnScreen(target)) return false;   // 빙의가 들어가는 몸은 짚지 않는다
            StartCoach(step, target);
            return true;
        }

        private Unit NearestPossessableTo(Vector2 from)
        {
            Unit best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || !e.IsPossessable || !e.IsHostBody) continue;
                float d = (e.Position - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = e; }
            }
            return best;
        }

        private bool OnScreen(Unit u)
        {
            var canvas = _unitLayer != null ? _unitLayer.GetComponentInParent<Canvas>() : null;
            if (canvas == null) return false;
            var root = (RectTransform)canvas.rootCanvas.transform;
            var p = (Vector2)root.InverseTransformPoint(u.transform.position);
            var r = root.rect;
            return p.x > r.xMin + 40f && p.x < r.xMax - 40f && p.y > r.yMin + 120f && p.y < r.yMax - 120f;
        }

        private void StartCoach(string step, Unit target)
        {
            _coachStep = step;
            _coachTarget = target;
            BuildCoach(null, Tx(step == "main" ? "ui.tut.coach.tap_main" : "ui.tut.coach.tap_sub"));
            PlaceCoachOn((RectTransform)target.transform, true);
        }

        /// <summary>따라 하기 중 화면을 누름 — 짚은 적이면 받고, 다른 곳이면 삼킨다(다른 적 빙의 · 이동 막음).</summary>
        private bool CoachTap(Unit tapped)
        {
            if (tapped != _coachTarget) return true;
            string step = _coachStep;
            CoachMark(step);
            EndCoach(true);
            _coachQuietUntil = Time.unscaledTime + 2f;
            // 보조 단계 — 선택창이 뜨면 「보조 넣기」 줄을 짚는다(BuildChoice 끝에서)
            _coachRowPending = step != "main";
            return false;
        }

        /// <summary>선택창을 다 세운 뒤 — 보조 단계면 「보조 넣기」 줄만 밝게 남기고 누를 때까지 기다린다.</summary>
        private void CoachChoiceRow(int rowIndex)
        {
            if (!_coachRowPending || _partyPopup == null) return;
            _coachRowPending = false;
            var row = FindDeep(_partyPopup.transform, $"Option{rowIndex + 1}") as RectTransform;
            if (row == null) return;
            BuildCoach(_partyPopup.transform, Tx("ui.tut.coach.tap_row"));
            PlaceCoachOn(row, false);
        }

        private static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            for (int i = 0; i < t.childCount; i++)
                if (FindDeep(t.GetChild(i), name) is Transform f) return f;
            return null;
        }

        private void EndCoach(bool keepRow)
        {
            _coachTarget = null;
            _coachStep = null;
            if (!keepRow) _coachRowPending = false;
            if (_coachRoot != null) Destroy(_coachRoot);
            _coachRoot = null;
        }

        // ─────────────────────────────────────────
        // 화면 — 구멍 둘레 어둡게(네 조각) · 둘레 고리 · 손가락 · 안내 글
        // ─────────────────────────────────────────

        private void BuildCoach(Transform parent, string text)
        {
            if (_coachRoot != null) Destroy(_coachRoot);
            if (parent == null)
            {
                var canvas = _unitLayer != null ? _unitLayer.GetComponentInParent<Canvas>() : null;
                if (canvas == null) return;
                parent = canvas.rootCanvas.transform;
            }
            if (_coachHandSprite == null) _coachHandSprite = GetSprite("tut_hand");
            if (_coachRingSprite == null) _coachRingSprite = GetSprite("range_ring");
            if (_popLike == null)
                foreach (var t in parent.root.GetComponentsInChildren<TMP_Text>(true))
                    if (t.name == "RbNoBodyText") { _popLike = t; break; }

            _coachRoot = new GameObject("TutCoach", typeof(RectTransform));
            _coachRt = (RectTransform)_coachRoot.transform;
            _coachRt.SetParent(parent, false);
            _coachRt.anchorMin = Vector2.zero; _coachRt.anchorMax = Vector2.one;
            _coachRt.offsetMin = _coachRt.offsetMax = Vector2.zero;
            _coachRt.SetAsLastSibling();

            for (int i = 0; i < 4; i++)
            {
                var img = CoachRect($"Dark{i}").gameObject.AddComponent<Image>();
                img.color = new Color(0f, 0f, 0f, CoachDim);
                img.raycastTarget = true;   // 구멍 밖 누름은 막는다
                _coachDark[i] = img.rectTransform;
            }
            _coachRing = CoachRect("Ring");
            var ring = _coachRing.gameObject.AddComponent<Image>();
            ring.sprite = _coachRingSprite;
            ring.color = new Color(1f, 0.85f, 0.35f, 1f);
            ring.raycastTarget = false;
            ring.enabled = _coachRingSprite != null;

            _coachTap = CoachRect("Tap");
            _coachTapImg = _coachTap.gameObject.AddComponent<Image>();
            _coachTapImg.sprite = _coachRingSprite;
            _coachTapImg.raycastTarget = false;
            _coachTap.gameObject.SetActive(false);

            _coachHand = CoachRect("Hand");
            var hand = _coachHand.gameObject.AddComponent<Image>();
            hand.sprite = _coachHandSprite != null ? _coachHandSprite : _markArrow;
            hand.preserveAspect = true;
            hand.raycastTarget = false;
            _coachHand.sizeDelta = new Vector2(120f, 120f);

            var label = CoachRect("Label");
            label.sizeDelta = new Vector2(640f, 90f);
            var tmp = label.gameObject.AddComponent<TextMeshProUGUI>();
            if (_popLike != null) { tmp.font = _popLike.font; tmp.fontSharedMaterial = _popLike.fontSharedMaterial; }
            tmp.text = text;
            tmp.fontSize = 38f;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = 38f;
            tmp.fontSizeMin = 18f;
            tmp.enableWordWrapping = true;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = Color.black;
            tmp.raycastTarget = false;
            if (GameFramework.Core.Base.CoreModule.TryGet<Game.Module.Common.ILanguageService>(out var lang) && lang.IsReady)
                lang.ApplyFonts(_coachRt);
        }

        private RectTransform CoachRect(string name)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(_coachRt, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            return rt;
        }

        private readonly Vector3[] _coachCorners = new Vector3[4];

        private void GrowBounds(RectTransform target, ref Vector2 min, ref Vector2 max)
        {
            target.GetWorldCorners(_coachCorners);
            for (int i = 0; i < 4; i++)
            {
                Vector2 p = _coachRt.InverseTransformPoint(_coachCorners[i]);
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }
        }

        /// <summary>여럿(셋 고르기)을 한 구멍으로.</summary>
        private void PlaceCoachOnGroup(List<Unit> group)
        {
            if (_coachRt == null || group.Count == 0) return;
            Vector2 min = new(float.MaxValue, float.MaxValue), max = new(float.MinValue, float.MinValue);
            for (int i = 0; i < group.Count; i++)
                if (group[i] != null) GrowBounds((RectTransform)group[i].transform, ref min, ref max);
            // 손가락은 가운데 호스트를 누른다(구멍 모서리가 아니라 고를 대상을)
            var mid = group[group.Count / 2];
            Vector2 tip = mid != null ? (Vector2)_coachRt.InverseTransformPoint(mid.transform.position) : (min + max) * 0.5f;
            PlaceCoachBox(min - Vector2.one * CoachPad, max + Vector2.one * CoachPad, false, tip);
        }

        /// <summary>대상(적 · 선택창 줄)을 구멍으로 — 둘레 네 조각을 맞추고 손가락 · 글을 옆에 둔다.</summary>
        private void PlaceCoachOn(RectTransform target, bool round)
        {
            if (_coachRt == null || target == null) return;
            Vector2 min = new(float.MaxValue, float.MaxValue), max = new(float.MinValue, float.MinValue);
            GrowBounds(target, ref min, ref max);
            min -= Vector2.one * CoachPad;
            max += Vector2.one * CoachPad;
            if (round)
            {
                // 적은 정사각 구멍(고리가 둥글게 감싼다)
                var c = (min + max) * 0.5f;
                float h = Mathf.Max(max.x - min.x, max.y - min.y) * 0.5f;
                min = c - Vector2.one * h;
                max = c + Vector2.one * h;
            }
            PlaceCoachBox(min, max, round);
        }

        private void PlaceCoachBox(Vector2 min, Vector2 max, bool round, Vector2? tipAt = null)
        {
            var r = _coachRt.rect;
            SetBox(_coachDark[0], r.xMin, max.y, r.xMax, r.yMax);   // 위
            SetBox(_coachDark[1], r.xMin, r.yMin, r.xMax, min.y);   // 아래
            SetBox(_coachDark[2], r.xMin, min.y, min.x, max.y);     // 왼
            SetBox(_coachDark[3], max.x, min.y, r.xMax, max.y);     // 오른

            float t = Time.unscaledTime;
            var center = (min + max) * 0.5f;
            _coachRing.gameObject.SetActive(round);
            if (round)
            {
                float s = (max.x - min.x) * (1f + 0.06f * Mathf.Sin(t * 6f));
                _coachRing.anchoredPosition = center;
                _coachRing.sizeDelta = new Vector2(s, s);
            }
            // 손가락 까딱(PD 2026-10-30 「까딱까딱 하는 게 들어가야」) — 0.9초 한 번:
            //   다가감(0~0.35 · OutCubic) → 꾹 누름(0.35~0.5 · 살짝 눌림 + 손끝 파문) → 물러남(0.5~0.9)
            // 집게손가락 끝(그림 왼쪽 위)이 구멍 오른쪽 아래 모서리를 누른다
            float ph = t % 0.9f;
            float reach = ph < 0.35f ? Game.Module.Common.UI.Ease.OutCubic(ph / 0.35f) : ph < 0.5f ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (ph - 0.5f) / 0.4f);
            float press = ph >= 0.35f && ph < 0.5f ? Mathf.Sin((ph - 0.35f) / 0.15f * Mathf.PI) : 0f;
            // 넓은 구멍(선택창 줄 · 셋)은 오른쪽 끝이 화면 밖일 수 있다 — 손이 화면 안에 남게 손끝을 안쪽으로
            var tip = tipAt ?? new Vector2(Mathf.Min(max.x - 14f, r.xMax - 190f), min.y + 14f);
            var away = new Vector2(46f, -46f);
            _coachHand.anchoredPosition = tip + new Vector2(60f, -60f) + away * (1f - reach) + new Vector2(-6f, 6f) * press;
            _coachHand.localScale = Vector3.one * (1f - 0.12f * press);
            _coachHand.localRotation = Quaternion.Euler(0f, 0f, 8f * press);
            float tapAge = ph - 0.38f;
            bool tapOn = tapAge >= 0f && tapAge < 0.45f && _coachRingSprite != null;
            _coachTap.gameObject.SetActive(tapOn);
            if (tapOn)
            {
                float k = tapAge / 0.45f;
                _coachTap.anchoredPosition = tip;
                _coachTap.sizeDelta = Vector2.one * Mathf.Lerp(20f, 90f, Game.Module.Common.UI.Ease.OutCubic(k));
                _coachTapImg.color = new Color(1f, 1f, 1f, 1f - k);
            }

            // 글은 구멍 위(위가 좁으면 아래)
            var label = (RectTransform)_coachRt.Find("Label");
            // 셋 고르기는 위에 원래 안내 띠가 있다 — 글은 셋 아래로
            bool above = _trio.Count == 0 && max.y + 150f < r.yMax;
            label.anchoredPosition = new Vector2(Mathf.Clamp(center.x, r.xMin + 330f, r.xMax - 330f),
                                                 above ? max.y + 70f : min.y - (_trio.Count > 0 ? 60f : 170f));
        }

        private static void SetBox(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchoredPosition = new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f);
            rt.sizeDelta = new Vector2(Mathf.Max(0f, x1 - x0), Mathf.Max(0f, y1 - y0));
        }

        // ─────────────────────────────────────────
        // 그림 설명 — 첫 보조(party) · 둘째 보조(synergy)
        // ─────────────────────────────────────────

        private void NoteSubsForGuide()
        {
            int n = _subs.Count;
            if (n == _coachSubsSeen) return;
            bool grew = _coachSubsSeen >= 0 && n > _coachSubsSeen;
            _coachSubsSeen = n;
            if (!grew || Ui == null) return;
            if (n == 1) Ui.GuideLater("g.party", "party");
            else if (n == 2) Ui.GuideLater("g.synergy", "synergy");
        }
    }
}
