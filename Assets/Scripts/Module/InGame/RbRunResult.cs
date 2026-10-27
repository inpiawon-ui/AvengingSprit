using Game.Module.Common;
using Game.Module.Common.Store;
using Game.Module.Common.UI;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// RE:BORN 결과 화면(시안 Screens/result-story · result-fail) — 스토리 판 클리어 · 실패 공통.
    /// 배경 그림 · 「CLEAR / FAILED」 워드아트 · 유령 · 기록 3줄(도달한 방 · 처치 · 플레이 시간) · 보상 3칸(골드 · 가장 많이 모은 조각 · 유령 경험치) · 로비로 / 다음.
    /// 클리어와 실패는 시안 배치가 다르다(칸 크기 · 자리 · 테 · 성장 추천 줄) — 빌더가 두 배치를 다 재어 <see cref="_slots"/> 에 담고 여기서 갈아 끼운다.
    /// 보상은 전투가 이미 넣었다 — 여기는 보여 주기만 한다. 「다음 챕터 · 다시 도전」 은 그 챕터를 골라 둔 채 로비로 간다(출전 값은 로비에서 낸다).
    /// 프리팹은 빌더(RbInGameBuilder)가 세운다 — 노드 이름이 곧 키다.
    /// </summary>
    public sealed partial class RbRunResult : MonoBehaviour
    {
        /// <summary>한 칸의 두 자리 — 클리어(시안 result-story) · 실패(result-fail). 글 크기는 0 이면 그대로 둔다.</summary>
        [System.Serializable]
        private struct Slot
        {
            public RectTransform Node;
            public Vector2 ClearPos, ClearSize, FailPos, FailSize;
            public float ClearFont, FailFont;
        }

        [SerializeField] private Sprite _bgClear, _bgFail, _wordClear, _wordFailed, _ghostCheer, _ghostSad;
        [Tooltip("스토리 클리어 배경 — 시안 .scene.quiet(흐림 14 · 밝기 0.3 · 채도 0.72 · 가장자리 어둡게)를 구운 그림. 탑 · 밤은 _bgClear 그대로")]
        [SerializeField] private Sprite _bgStory;
        [SerializeField] private Sprite _cellClear, _cellGold, _cellTeal, _expClear, _expFail;
        [Tooltip("몸 카드 등급 테 · 꼬리표 — HostGrade 순서(B · A · S = 화면 R · SR · SSR)")]
        [SerializeField] private Sprite[] _hostFrames = System.Array.Empty<Sprite>(), _gradeTags = System.Array.Empty<Sprite>();
        [SerializeField] private Slot[] _slots = System.Array.Empty<Slot>();
        [SerializeField] private float _clearHeight = 1280f, _failHeight = 1373f;

        private static readonly Color ChapterClear = new Color32(84, 220, 235, 255);
        private static readonly Color ChapterFail = new Color32(143, 208, 212, 255);

        private UIBinder _ui;
        private IPlayerDataService _player;
        private int _nextChapter;
        private string _tipHost;

        public void Show(StageFinishedEvent e, SpriteAtlas faces)
        {
            Bind();
            _mode = ResultMode.Story;
            CoreModule.TryGet(out _player);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();   // 06_ui — 열 때 맨 위

            bool clear = e.IsCleared;
            ApplyLayout(!clear);
            int ch = Mathf.Max(1, e.FinishedChapter);
            int chapters = _player != null ? Mathf.Max(1, _player.ChapterCount) : ch;
            _nextChapter = clear ? Mathf.Min(ch + 1, chapters) : ch;

            SetSprite("RbResultBg", clear ? (_bgStory != null ? _bgStory : _bgClear) : _bgFail);
            SetSprite("RbResultWord", clear ? _wordClear : _wordFailed);
            _ui.SetActive("RbResultWord", true);
            _ui.SetActive("RbResultHeadline", false);
            _ui.SetActive("RbResultExpTile", true);
            _ui.SetActive("RbResultGoldTile", true);
            SetSprite("RbResultGhost", clear ? _ghostCheer : _ghostSad);
            _ui.SetText("RbResultChapter", $"CHAPTER {ch}");
            bool unlocked = clear && ch < chapters;
            _ui.SetActive("RbResultNew", unlocked);
            if (unlocked) Chip(string.Format(Localize.FromTable("ui.rb.result.unlocked_line", "CH {0} 열림!"), ch + 1), true);

            // 기록
            int rooms = Mathf.Max(1, e.ChapterRooms);
            _ui.SetText("RbResultRoomValue", $"{Mathf.Clamp(e.ReachedRoom, 0, rooms)}/{rooms}");
            _ui.SetText("RbResultKillValue", e.Kills.ToString("N0"));
            int sec = Mathf.Max(0, Mathf.RoundToInt(e.PlaySeconds));
            _ui.SetText("RbResultTimeValue", $"{sec / 60:00}:{sec % 60:00}");

            // 보상 — 골드 · 유령 경험치 두 칸 + 받은 몸 조각 전부(몸 카드 줄)
            _ui.SetText("RbResultGoldValue", e.RewardGold.ToString("N0"));
            ShowHosts(e.ShardHostKeys, e.ShardCounts, faces);
            _ui.SetText("RbResultExpValue", e.RewardGhostExp.ToString("N0"));
            _ui.SetText("RbResultNextText", clear
                ? (ch < chapters ? Localize.FromTable("ui.rb.result.next", "다음 챕터 ▶") : Localize.FromTable("ui.rb.result.again", "다시 도전"))
                : Localize.FromTable("ui.rb.result.again", "다시 도전"));

            // 실패 — 성장 추천(가장 오래 쓴 몸) · 다시 도전 옆 행동력 값
            _tipHost = clear ? null : e.LongestHostKey;
            var tipEntry = _tipHost != null ? _player?.GetHost(_tipHost) : null;
            bool tip = tipEntry != null && !tipEntry.IsGhost;
            _ui.SetActive("RbResultTip", tip);
            if (tip)
            {
                _ui.SetText("RbResultTipSub", string.Format(Localize.FromTable("ui.rb.result.tip_sub", "가장 오래 쓴 호스트 · {0}"), tipEntry.DisplayName));
                SetFace("RbResultTipFace", _tipHost, faces);
            }
            bool cost = !clear;
            _ui.SetActive("RbResultNextCost", cost);
            if (cost) _ui.SetText("RbResultNextCostValue", CoreModule.TryGet<IStaminaService>(out var st) ? st.PerRun.ToString() : "5");
        }

        /// <summary>클리어 · 실패 배치를 갈아 끼운다(탑 · 밤 결과는 클리어 배치).</summary>
        private void ApplyLayout(bool fail)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                var s = _slots[i];
                if (s.Node == null) continue;
                s.Node.anchoredPosition = fail ? s.FailPos : s.ClearPos;
                s.Node.sizeDelta = fail ? s.FailSize : s.ClearSize;
                float font = fail ? s.FailFont : s.ClearFont;
                if (font > 0f && s.Node.TryGetComponent<TMP_Text>(out var t)) t.fontSizeMax = font;
            }
            if (_ui.Find("RbResultContent") is RectTransform c && c.TryGetComponent<RbFitScale>(out var fit))
                fit.DesignHeight = fail ? _failHeight : _clearHeight;
            // 칸 테 — 클리어는 어두운 칸, 실패는 금 · 청록 테(시안 .reward-slot)
            SetSprite("RbResultGoldTile", fail ? _cellGold : _cellClear);
            SetSprite("RbResultExpTile", fail ? _cellTeal : _cellClear);
            SetSprite("RbResultExpTileIcon", fail ? _expFail : _expClear);
            for (int i = 0; i < 3; i++) _ui.SetActive($"RbResultRecLine{i}", !fail);   // 줄 사이 가는 선은 클리어 시안에만
            if (_ui.Get<TMP_Text>("RbResultChapter") is TMP_Text chapter) chapter.color = fail ? ChapterFail : ChapterClear;
            _ui.SetActive("RbResultTip", false);
            _ui.SetActive("RbResultNextCost", false);
            _ui.SetActive("RbResultNewBadge", false);
        }

        /// <summary>가운데 판 한 줄(「CH 2 열림!」 · 「남은 입장 2/3」) — NEW 꼬리(도트 글)는 글 바로 뒤에 붙이고 둘을 판 가운데로.</summary>
        private void Chip(string text, bool isNew)
        {
            _ui.SetActive("RbResultNew", true);
            _ui.SetText("RbResultNewText", text);
            _ui.SetActive("RbResultNewBadge", isNew);
            if (_ui.Get<TMP_Text>("RbResultNewText") is not TMP_Text t) return;
            var tr = t.rectTransform;
            if (!isNew || _ui.Get<TMP_Text>("RbResultNewBadge") is not TMP_Text badge)
            {
                tr.anchoredPosition = new Vector2(0f, tr.anchoredPosition.y);
                t.alignment = TextAlignmentOptions.Center;
                return;
            }
            const float gap = 24f * 2f / 3f;   // 시안 「열림!」 과 NEW 사이(띄어쓰기 + 16)
            float tw = t.GetPreferredValues(t.text, 9999f, tr.sizeDelta.y).x;
            float bw = badge.GetPreferredValues(badge.text, 9999f, badge.rectTransform.sizeDelta.y).x;
            float left = -(tw + gap + bw) * 0.5f;
            tr.anchoredPosition = new Vector2(left + tr.sizeDelta.x * 0.5f, tr.anchoredPosition.y);
            t.alignment = TextAlignmentOptions.Left;
            var br = badge.rectTransform;
            br.anchoredPosition = new Vector2(left + tw + gap + br.sizeDelta.x * 0.5f, br.anchoredPosition.y);
            badge.alignment = TextAlignmentOptions.Left;
        }

        // 몸 카드 — 시안 190 x 280 · 간격 18(1080) → 720
        private const float HostCardW = 190f * 2f / 3f, HostCardGap = 18f * 2f / 3f, HostShardGap = 6f * 2f / 3f;
        private const float HostCardOverflowScale = 0.9f;   // 넘칠 때 — 5.4 장이 보인다
        private readonly System.Collections.Generic.List<RectTransform> _hostCards = new();

        /// <summary>
        /// 몸 카드 줄 — 몸 하나에 카드 한 장(등급 테 · 꼬리표 · 얼굴 · 이름 · 조각 ×n). 다 들어가면 가운데로 모으고, 넘치면 옆으로 민다.
        /// counts 가 없으면(탑 실패의 「이 층에 맞는 몸」) 조각 줄을 감춘다.
        /// </summary>
        private void ShowHosts(string[] keys, int[] counts, SpriteAtlas faces)
        {
            int n = keys != null ? keys.Length : 0;
            _ui.SetActive("RbResultHosts", n > 0);
            if (n == 0) return;
            if (_ui.Find("RbResultHostsContent") is not RectTransform content || _ui.Find("RbResultHostCard") is not RectTransform tpl) return;
            while (_hostCards.Count < n)
            {
                var copy = (RectTransform)Instantiate(tpl.gameObject, content).transform;
                copy.name = $"RbResultHostCard{_hostCards.Count}";
                _hostCards.Add(copy);
            }
            var view = (RectTransform)content.parent;
            // 다 들어가면 제 크기 · 넘치면 조금 줄여 다음 카드가 오른쪽 끝에 걸쳐 보이게 — 「옆으로 밀면 더 있다」가 읽히게
            //   (제 크기로는 5장이 칸에 딱 차서 6번째가 전혀 안 보였다 · 2026-10-27 6종 캡처)
            bool fits = n * HostCardW + (n - 1) * HostCardGap <= view.rect.width + 0.5f;
            float k = fits ? 1f : HostCardOverflowScale;
            for (int i = 0; i < _hostCards.Count; i++)
            {
                var card = _hostCards[i];
                bool on = i < n;
                card.gameObject.SetActive(on);
                if (!on) continue;
                card.localScale = new Vector3(k, k, 1f);
                card.anchoredPosition = new Vector2(i * (HostCardW * k + HostCardGap), card.anchoredPosition.y);
                FillHostCard(card, keys[i], counts != null && i < counts.Length ? counts[i] : -1, faces);
            }

            float width = n * HostCardW * k + (n - 1) * HostCardGap;
            content.sizeDelta = new Vector2(width, content.sizeDelta.y);
            content.anchoredPosition = new Vector2(fits ? (view.rect.width - width) * 0.5f : 0f, 0f);
            if (view.TryGetComponent<ScrollRect>(out var scroll)) { scroll.enabled = !fits; scroll.StopMovement(); }
        }

        private void FillHostCard(RectTransform card, string key, int count, SpriteAtlas faces)
        {
            var host = _player?.GetHost(key);
            int grade = host != null ? (int)host.Grade : 0;
            SetChild(card, "RbHostCardFrame", grade < _hostFrames.Length ? _hostFrames[grade] : null);
            SetChild(card, "RbHostCardTag", grade < _gradeTags.Length ? _gradeTags[grade] : null);
            SetChild(card, "RbHostCardFace", faces != null ? faces.GetSprite($"face_{key}") : null);
            if (card.Find("RbHostCardName") is Transform nameT && nameT.TryGetComponent<TMP_Text>(out var name))
                name.text = host != null ? host.DisplayName : key;
            bool hasCount = count > 0;
            var shard = card.Find("RbHostCardShard") as RectTransform;
            var countT = card.Find("RbHostCardCount") as RectTransform;
            if (shard != null) shard.gameObject.SetActive(hasCount);
            if (countT == null) return;
            countT.gameObject.SetActive(hasCount);
            if (!hasCount || shard == null || !countT.TryGetComponent<TMP_Text>(out var ct)) return;
            ct.text = $"×{count}";
            // 조각 아이콘 + 「×n」 을 한 덩어리로 카드 가운데에
            float tw = ct.GetPreferredValues(ct.text, 9999f, countT.sizeDelta.y).x;
            float left = (card.sizeDelta.x - (shard.sizeDelta.x + HostShardGap + tw)) * 0.5f;
            shard.anchoredPosition = new Vector2(left, shard.anchoredPosition.y);
            countT.anchoredPosition = new Vector2(left + shard.sizeDelta.x + HostShardGap, countT.anchoredPosition.y);
        }

        private static void SetChild(Transform card, string child, Sprite s)
        {
            if (card.Find(child) is not Transform t || !t.TryGetComponent<Image>(out var img)) return;
            img.sprite = s;
            img.enabled = s != null;
        }

        private void SetFace(string node, string hostKey, SpriteAtlas faces)
        {
            if (_ui.Get<Image>(node) is not Image face) return;
            face.sprite = hostKey != null && faces != null ? faces.GetSprite($"face_{hostKey}") : null;
            face.enabled = face.sprite != null;
        }

        private void SetSprite(string node, Sprite s)
        {
            if (_ui.Get<Image>(node) is not Image img) return;
            img.sprite = s;
            img.enabled = s != null;
        }

        private void Bind()
        {
            if (_ui != null) return;
            _ui = new UIBinder(transform);
            _ui.OnClick("RbResultLobby", () => Leave(false));
            _ui.OnClick("RbResultNext", () => Leave(true));
            _ui.OnClick("RbResultTipGo", GoGrowth);
        }

        private void Leave(bool next)
        {
            if (_mode != ResultMode.Story) { LeaveMode(next); return; }
            GameSound.Cue("ui.tap");
            if (next && _player != null) _player.SelectedChapter = _nextChapter;
            var main = GetComponentInParent<InGameMainUI>();
            if (main != null) main.LeaveToLobby();
        }

        // 「성장 ›」 — 로비로 가서 그 몸의 성장 화면을 연다
        private void GoGrowth()
        {
            if (string.IsNullOrEmpty(_tipHost)) return;
            LobbyRequest.GrowthHostKey = _tipHost;
            Leave(false);
        }
    }
}
