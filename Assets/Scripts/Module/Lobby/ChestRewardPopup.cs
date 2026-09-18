using System;
using Cysharp.Threading.Tasks;
using Game.Character;
using Game.Module.Common;
using Game.Module.Common.Chest;
using Game.Module.Common.UI;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Module.Resource;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 상자를 연 뒤 받은 것을 카드로 늘어놓는다 (기획 2026-09-18 · 시안 ui_new_chest_reward_v1 — 클래시로얄 식).
    ///
    /// 첫 카드는 골드, 나머지는 호스트 조각(호스트마다 한 장, 등급 테두리 B · A · S).
    /// 보상은 **이미 지급됐다** — 확인을 누르면 창만 닫힌다.
    ///
    /// 노드는 `ChapterScreensBuilder` 가 세운다. 그림은 발주 부품이 오면 같은 이름으로 갈아 끼운다.
    /// </summary>
    public sealed class ChestRewardPopup : MonoBehaviour
    {
        [Serializable]
        private struct ChestOpenArt
        {
            public string Key;
            public Sprite Sprite;
        }

        [SerializeField] private ChestOpenArt[] _openArts = Array.Empty<ChestOpenArt>();
        [SerializeField] private Sprite _cardGold;
        [SerializeField] private Sprite _cardB;
        [SerializeField] private Sprite _cardA;
        [SerializeField] private Sprite _cardS;
        [SerializeField] private Sprite _goldPile;

        /// <summary>카드 칸 수. 골드 1 + 백금 상자 호스트 최대 4 = 5 — 한 칸 여유.</summary>
        private const int CardSlots = 6;

        /// <summary>네 장까지는 2열(시안), 다섯 장부터는 3열로 줄여 액자 안에 담는다.</summary>
        // ⚠ 액자 안쪽 판이 **폭 404** 뿐이다(납품 실측). 카드(190)를 원래 크기로 두 장 놓으면
        //   벌써 판을 넘는다 — 두 줄은 0.9 배, 세 줄은 0.66 배로 줄여 판 안에 담는다(2026-09-18 지적).
        private const float CardScale2 = 0.9f, CardStep2 = 196f, RowStep2 = 190f;
        private const float CardScale3 = 0.66f, CardStep3 = 132f, RowStep3 = 142f;

        /// <summary>
        /// 카드 줄들의 가운데 — 액자 안쪽 판(y 268~735)의 한가운데.
        /// 줄 수와 상관없이 여기를 가운데로 모은다 — 위에 붙이면 두 장일 때 아래가 텅 빈다.
        /// </summary>
        private const float RowsCenter = -196f;

        private const string AtlasAddress = "atlas/hostselectpanel";

        private UIBinder _ui;
        private IPlayerDataService _player;
        private SpriteAtlas _hostAtlas;
        private ChestReward _shown;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            Localize.ApplyFonts(transform);
            CoreModule.TryGet<IPlayerDataService>(out _player);
            _ui.OnClick("RewardOkButton", Close);
        }

        public void Show(ChestReward reward)
        {
            _shown = reward;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();   // 06_ui 규약 — 활성화 시 최상단으로
            Fill();
            if (_hostAtlas == null) LoadAtlasAsync().Forget();   // fire-and-forget: 얼굴은 늦게 와도 된다
        }

        public void Close() => gameObject.SetActive(false);

        private async UniTaskVoid LoadAtlasAsync()
        {
            try { _hostAtlas = await CoreModule.Get<IResourceManager>().LoadAsync<SpriteAtlas>(AtlasAddress); }
            catch (Exception e) { Debug.LogWarning($"[ChestReward] 호스트 아틀라스 로드 실패 — {e.Message}"); }
            if (this != null && gameObject.activeSelf) Fill();
        }

        private void Fill()
        {
            if (_player == null) CoreModule.TryGet<IPlayerDataService>(out _player);

            var chest = _ui.Get<Image>("RewardChestArt");
            if (chest != null)
            {
                var art = OpenArtOf(_shown.ChestKey);
                if (art != null) chest.sprite = art;
            }
            _ui.SetText("RewardHeaderText", Localize.Get("ui.chest.reward.header"));
            _ui.SetText("RewardOkText", Localize.Get("ui.common.ok"));

            int hosts = _shown.ShardHostKeys != null ? _shown.ShardHostKeys.Length : 0;
            int count = Mathf.Min(CardSlots, 1 + hosts);
            bool wide = count > 4;
            int cols = wide ? 3 : 2;
            float step = wide ? CardStep3 : CardStep2;
            int rows = (count + cols - 1) / cols;
            float rowStep = wide ? RowStep3 : RowStep2;
            float rowTop = RowsCenter + (rows - 1) * rowStep * 0.5f;

            for (int i = 0; i < CardSlots; i++)
            {
                var card = _ui.Find($"RewardCard{i}") as RectTransform;
                if (card == null) continue;
                bool on = i < count;
                card.gameObject.SetActive(on);
                if (!on) continue;

                // 줄마다 가운데 맞춤 — 마지막 줄이 덜 차도 한쪽으로 쏠리지 않게
                int row = i / cols, col = i % cols;
                int inRow = Mathf.Min(cols, count - row * cols);
                float x = (col - (inRow - 1) * 0.5f) * step;
                card.anchoredPosition = new Vector2(x, rowTop - row * rowStep);
                card.localScale = Vector3.one * (wide ? CardScale3 : CardScale2);

                if (i == 0) BindCard(card, _cardGold, _goldPile, $"+{_shown.Gold:N0}");
                else
                {
                    string key = _shown.ShardHostKeys[i - 1];
                    var host = _player?.GetHost(key);
                    var grade = host != null ? host.Grade : HostGrade.B;
                    var face = _hostAtlas != null ? _hostAtlas.GetSprite($"hostslotportrait_{key}") : null;
                    BindCard(card, FrameOf(grade), face, $"×{_shown.ShardCounts[i - 1]}");
                }
            }
        }

        private void BindCard(Transform card, Sprite frame, Sprite icon, string countText)
        {
            var f = _ui.Find(card, "RewardCardFrame")?.GetComponent<Image>();
            if (f != null && frame != null) f.sprite = frame;
            var ic = _ui.Find(card, "RewardCardIcon")?.GetComponent<Image>();
            if (ic != null)
            {
                ic.sprite = icon;
                ic.enabled = icon != null;   // 얼굴이 아직 안 왔으면 흰 네모 대신 비워 둔다
            }
            var t = _ui.Find(card, "RewardCardCountText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (t != null) t.text = countText;
        }

        private Sprite FrameOf(HostGrade grade) => grade switch
        {
            HostGrade.S => _cardS,
            HostGrade.A => _cardA,
            _ => _cardB,
        };

        private Sprite OpenArtOf(string chestKey)
        {
            for (int i = 0; i < _openArts.Length; i++)
                if (_openArts[i].Key == chestKey) return _openArts[i].Sprite;
            return null;
        }
    }
}
