using System.Collections.Generic;
using Game.Module.Common.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 빙의 파티 위 HUD 채우기(PD 2026-10-29 확정 틀 S1b · 시안 `_exchange/in/mock_partyui_S1b.png` · `F_states`).
    /// 노드는 에디터 도구 `Tools/Game/RE:BORN 화면/빙의 파티 HUD 꽂기`(`RbPartyHudBuilder`)가 세운다 — 여기서는 그림 · 글자만 바꾼다.
    ///   · 메인 칸: 얼굴(옛 `HostPortraitImage`) · 이름 · 체력(옛 노드 그대로) — 메인이 없으면 「빙의할 호스트를 터치」
    ///   · 보조 칸 둘: 얼굴만(체력 없음) — 비면 「+ −10」
    ///   · 왼쪽 시너지 열: 맨 위 금빛 인연 칸 · 아래로 켜진 태그 칸(「무도가 2/3」) — 누르면 설명 창(게임 멈춤)
    /// 파티가 바뀐 프레임에만 다시 채운다(매 프레임 글자를 새로 만들지 않는다).
    /// </summary>
    public sealed partial class InGameMainUI
    {
        private const int PartySynCells = 4;
        private bool _partyHudReady;
        private int _partyHudVersion = int.MinValue;
        private TextMeshProUGUI _partyMainName;
        private readonly Image[] _partySubFace = new Image[2];
        private readonly GameObject[] _partySubFilled = new GameObject[2];
        private readonly GameObject[] _partySubEmpty = new GameObject[2];
        private readonly TextMeshProUGUI[] _partySubCost = new TextMeshProUGUI[2];
        private RectTransform _partySynColumn;
        private GameObject _partyInyeonCell;
        private Image _partyInyeonFace;
        private TextMeshProUGUI _partyInyeonLabel;
        private readonly Image[] _partySynBg = new Image[PartySynCells];
        private readonly Image[] _partySynIcon = new Image[PartySynCells];
        private readonly TextMeshProUGUI[] _partySynName = new TextMeshProUGUI[PartySynCells];
        private readonly TextMeshProUGUI[] _partySynLabel = new TextMeshProUGUI[PartySynCells];
        private readonly List<BattleDirector.PartySynCell> _partyCells = new();

        private void EnsurePartyHud()
        {
            if (_partyHudReady || _ui == null) return;
            _partyHudReady = true;
            _partyMainName = _ui.Get<TextMeshProUGUI>("PartyMainName");
            for (int i = 0; i < 2; i++)
            {
                _partySubFace[i] = _ui.Get<Image>($"PartySub{i}Face");
                _partySubFilled[i] = _ui.Get<RectTransform>($"PartySub{i}Frame")?.gameObject;
                _partySubEmpty[i] = _ui.Get<RectTransform>($"PartySub{i}Empty")?.gameObject;
                _partySubCost[i] = _ui.Get<TextMeshProUGUI>($"PartySub{i}Cost");
                int slot = i;
                var btn = _ui.Get<Button>($"PartySub{i}Frame");
                if (btn != null) btn.onClick.AddListener(() => _battle?.PartyTapSub(slot));
            }
            _partySynColumn = _ui.Get<RectTransform>("PartySynColumn");
            var colBtn = _ui.Get<Button>("PartySynColumn");
            if (colBtn != null) colBtn.onClick.AddListener(() => _battle?.PartyOpenSynergyDetail());
            _partyInyeonCell = _ui.Get<RectTransform>("PartyInyeonCell")?.gameObject;
            _partyInyeonFace = _ui.Get<Image>("PartyInyeonFace");
            _partyInyeonLabel = _ui.Get<TextMeshProUGUI>("PartyInyeonLabel");
            for (int i = 0; i < PartySynCells; i++)
            {
                _partySynBg[i] = _ui.Get<Image>($"PartySynCell{i}");
                _partySynIcon[i] = _ui.Get<Image>($"PartySynIcon{i}");
                _partySynName[i] = _ui.Get<TextMeshProUGUI>($"PartySynName{i}");
                _partySynLabel[i] = _ui.Get<TextMeshProUGUI>($"PartySynLabel{i}");
            }
        }

        // ── 인연 켜짐 — 스킬 버튼(가이드 `_exchange/guide_inyeon_fx.md` 박자표) ─────────────────
        // 켜지는 순간(강): 버튼 뒤 금빛 번짐이 크게 번쩍 → 0.6초에 제자리. 켜져 있는 동안(약): 번짐이 은은하게 숨쉬고 버튼 아래 「인연」 꼬리표.
        // 전투는 멈추지 않는다 · 화면 가운데 판 없음.
        private const float InyeonBurstSeconds = 0.6f;
        private static readonly Color InyeonGoldUi = new(1f, 0.84f, 0.42f);
        private RectTransform _inyeonGlow;
        private Image _inyeonGlowImg;
        private GameObject _inyeonTag;
        private string _inyeonShown;
        private float _inyeonBurst;

        private void TickInyeonButton()
        {
            string on = _battle.InyeonActiveKey;
            if (on != _inyeonShown)
            {
                if (on != null) _inyeonBurst = InyeonBurstSeconds;
                _inyeonShown = on;
            }
            if (on == null)
            {
                if (_inyeonGlow != null && _inyeonGlow.gameObject.activeSelf) _inyeonGlow.gameObject.SetActive(false);
                if (_inyeonTag != null && _inyeonTag.activeSelf) _inyeonTag.SetActive(false);
                return;
            }
            EnsureInyeonButtonFx();
            if (_inyeonGlow == null) return;
            if (!_inyeonGlow.gameObject.activeSelf) _inyeonGlow.gameObject.SetActive(true);
            if (_inyeonTag != null && !_inyeonTag.activeSelf) _inyeonTag.SetActive(true);

            float scale, alpha;
            if (_inyeonBurst > 0f)
            {
                _inyeonBurst -= Time.unscaledDeltaTime;
                float k = 1f - Mathf.Clamp01(_inyeonBurst / InyeonBurstSeconds);   // 0 → 1
                scale = Mathf.Lerp(1.9f, 1.25f, Ease.OutCubic(k));
                alpha = Mathf.Lerp(1f, 0.55f, k);
            }
            else
            {
                float breath = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.2f);
                scale = 1.25f + 0.05f * breath;
                alpha = 0.38f + 0.17f * breath;
            }
            _inyeonGlow.localScale = Vector3.one * scale;
            _inyeonGlowImg.color = new Color(1f, 1f, 1f, alpha);
        }

        private void EnsureInyeonButtonFx()
        {
            if (_inyeonGlow != null) return;
            var button = _ui.Find("SkillButton") as RectTransform;
            if (button == null) return;
            var go = new GameObject("PartyInyeonGlow", typeof(RectTransform), typeof(Image));
            _inyeonGlow = (RectTransform)go.transform;
            _inyeonGlow.SetParent(button, false);
            _inyeonGlow.SetAsFirstSibling();   // 버튼 그림 뒤 — 아이콘을 가리지 않는다
            _inyeonGlow.anchorMin = _inyeonGlow.anchorMax = new Vector2(0.5f, 0.5f);
            _inyeonGlow.sizeDelta = button.rect.size;
            _inyeonGlowImg = go.GetComponent<Image>();
            _inyeonGlowImg.sprite = _battle.PartySprite("syn_icon_glow");
            _inyeonGlowImg.raycastTarget = false;

            var tagGo = new GameObject("PartyInyeonButtonTag", typeof(RectTransform), typeof(TextMeshProUGUI));
            var tagRt = (RectTransform)tagGo.transform;
            tagRt.SetParent(button, false);
            tagRt.anchorMin = tagRt.anchorMax = new Vector2(0.5f, 0f);
            tagRt.pivot = new Vector2(0.5f, 0.5f);
            tagRt.anchoredPosition = new Vector2(0f, 6f);
            tagRt.sizeDelta = new Vector2(70f, 22f);
            var t = tagGo.GetComponent<TextMeshProUGUI>();
            var like = _ui.Get<TextMeshProUGUI>("PartyMainName");
            if (like != null) { t.font = like.font; t.fontSharedMaterial = like.fontSharedMaterial; }
            t.text = Game.Module.Common.Localize.Get("ui.party.hud.inyeon_tag");
            t.fontSize = 15f;
            t.fontStyle = FontStyles.Bold;
            t.color = InyeonGoldUi;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            t.outlineWidth = 0.25f;
            t.outlineColor = new Color32(40, 20, 0, 255);
            _inyeonTag = tagGo;
        }

        private void TickPartyHud()
        {
            if (_battle == null || !_battle.PartyEnabled) return;
            EnsurePartyHud();
            TickInyeonButton();
            int v = _battle.PartyHudVersion;
            if (v == _partyHudVersion) return;
            _partyHudVersion = v;

            string main = _battle.PartyMainKey;
            if (_partyMainName != null)
                _partyMainName.text = main != null ? _battle.PartyHostName(main) : Game.Module.Common.Localize.Get("ui.party.hud.touch_host");

            for (int i = 0; i < 2; i++)
            {
                string key = _battle.PartySubKey(i);
                bool has = key != null;
                if (_partySubFilled[i] != null) _partySubFilled[i].SetActive(has);
                if (_partySubEmpty[i] != null) _partySubEmpty[i].SetActive(!has);
                if (_partySubCost[i] != null) _partySubCost[i].text = $"−{_battle.PartySubCost}";
                if (has && _partySubFace[i] != null)
                {
                    _partySubFace[i].sprite = FaceOf(key);
                    _partySubFace[i].enabled = _partySubFace[i].sprite != null;
                }
            }

            // 인연 칸 — 켜졌을 때만(맨 위 · 금빛 · 메인 얼굴 + 인연 이름)
            string iy = _battle.InyeonActiveKey;
            if (_partyInyeonCell != null) _partyInyeonCell.SetActive(iy != null);
            if (iy != null)
            {
                if (_partyInyeonFace != null) { _partyInyeonFace.sprite = FaceOf(iy); _partyInyeonFace.enabled = _partyInyeonFace.sprite != null; }
                if (_partyInyeonLabel != null) _partyInyeonLabel.text = BattleDirector.InyeonName(iy);
            }

            _battle.FillSynergyCells(_partyCells);
            for (int i = 0; i < PartySynCells; i++)
            {
                bool on = i < _partyCells.Count;
                if (_partySynBg[i] != null) _partySynBg[i].gameObject.SetActive(on);
                if (!on) continue;
                var c = _partyCells[i];
                if (_partySynIcon[i] != null) _partySynIcon[i].sprite = _battle.PartySprite(c.Icon);
                if (_partySynName[i] != null) _partySynName[i].text = c.Name;
                if (_partySynLabel[i] != null) _partySynLabel[i].text = $"{c.Count}/{c.Next}";
            }
            if (_partySynColumn != null) _partySynColumn.gameObject.SetActive(iy != null || _partyCells.Count > 0);
        }
    }
}
