using System.Collections.Generic;
using Game.Module.Common;
using Game.Module.Common.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// RE:BORN 상자 열기 — 시안 Screens/chest-opening · chest-open.png. 상점에서 상자를 사면 보상 창 대신 이 쪽.
    /// 박자 1: 닫힌 상자 · 「금 상자 열기!」 · 「화면을 눌러 계속」 → 박자 2: 머리줄 · 「획득!」 · 받은 것 칸(가운데로) · 확인.
    /// 보상은 이미 들어왔다 — 보여 주기만 한다. 상자 등급 연출(빛 · 터짐)은 연출 단계(rbfx_chest_*)가 얹는다.
    /// </summary>
    public sealed class RbChestOpen : MonoBehaviour
    {
        private const int Cells = 4;
        // 시안 칸 폭 290 · 간격 318(1080 기준) × 2/3 — 넷이면 화면 폭에 들어가게 줄인다
        private const float CellPitch = 318f * 2f / 3f;
        private const float FourScale = 0.8f;

        private UIBinder _ui;
        private RbLobbyUI _shell;
        private readonly List<(string kind, string key, int amount)> _lines = new();
        private Sprite[] _icons;
        private bool _opened;

        public bool IsOpen => gameObject.activeSelf;

        private void Init()
        {
            if (_ui != null) return;
            _ui = new UIBinder(transform);
            _shell = GetComponentInParent<RbLobbyUI>(true);
            _ui.OnClick("RbChestOpenTap", Advance);
            _ui.OnClick("RbChestOpenGo", Advance);
            _ui.OnClick("RbChestOpenBack", () => gameObject.SetActive(false));
            _ui.OnClick("RbChestOpenHelp", () => _shell?.ShowGuide("growth", "summon"));   // 조각은 성급 · 소환으로 이어진다
        }

        /// <summary>상자 키(silver · gold · platinum) · 받은 줄 · 아이콘 목록(RbNotice 와 같은 키트 아이콘).</summary>
        public void Show(string chestKey, List<(string kind, string key, int amount)> lines, Sprite[] icons)
        {
            Init();
            _icons = icons;
            _lines.Clear();
            _lines.AddRange(lines);
            _opened = false;
            gameObject.SetActive(true);
            string title = string.Format(Localize.FromTable("ui.rb.chest.open", "{0} 열기!"), Localize.Get($"store.chest_{chestKey}.name"));
            _ui.SetText("RbChestOpenTitle", title);
            _ui.SetText("RbChestOpenHeadTitle", title);
            if (_ui.Get<Image>("RbChestOpenChest") is Image chest)
            {
                var s = RbRewards.Find(icons, $"rb_icon_chest_{chestKey}_96");
                if (s != null) chest.sprite = s;
            }
            Phase();
        }

        private void Advance()
        {
            if (!_opened)
            {
                _opened = true;
                GameSound.Cue("run.card");
                Phase();
                return;
            }
            gameObject.SetActive(false);
        }

        private void Phase()
        {
            _ui.SetActive("RbChestOpenTitle", !_opened);
            _ui.SetActive("RbChestOpenChest", !_opened);
            _ui.SetActive("RbChestOpenHead", _opened);
            _ui.SetActive("RbChestOpenGot", _opened);
            _ui.SetText("RbChestOpenGoText", _opened ? Localize.FromTable("ui.common.ok", "확인") : Localize.FromTable("ui.rb.chest.tap", "화면을 눌러 계속"));
            int n = _opened ? Mathf.Min(_lines.Count, Cells) : 0;
            float scale = n > 3 ? FourScale : 1f;
            for (int i = 0; i < Cells; i++)
            {
                var cell = _ui.Find($"RbChestOpenCell{i}") as RectTransform;
                if (cell == null) continue;
                cell.gameObject.SetActive(i < n);
                if (i >= n) continue;
                var (kind, key, amount) = _lines[i];
                cell.localScale = new Vector3(scale, scale, 1f);
                cell.anchoredPosition = new Vector2((i - (n - 1) * 0.5f) * CellPitch * scale, cell.anchoredPosition.y);
                if (_ui.Find(cell, $"RbChestOpenCell{i}Icon")?.GetComponent<Image>() is Image icon) icon.sprite = RbRewards.IconOf(_icons, kind, key);
                // 조각 칸은 누구 조각인지 얼굴을 곁들인다(시안 chest-open)
                if (_ui.Find(cell, $"RbChestOpenCell{i}Face")?.GetComponent<Image>() is Image face)
                {
                    face.sprite = kind == "shard" && _shell != null ? _shell.Face(key) : null;
                    face.enabled = face.sprite != null;
                }
                if (_ui.Find(cell, $"RbChestOpenCell{i}Count")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI c) c.text = amount.ToString("N0");
                if (_ui.Find(cell, $"RbChestOpenCell{i}Label")?.GetComponent<TextMeshProUGUI>() is TextMeshProUGUI l) l.text = RbRewards.LabelOf(kind, key);
            }
        }
    }
}
