using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
        private bool _opened, _busy;
        private System.Threading.CancellationTokenSource _openCts;
        private string _chestKey;
        private readonly List<RectTransform> _shown = new();

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
            _busy = false;
            _chestKey = chestKey;
            CancelOpen();
            gameObject.SetActive(true);
            string title = string.Format(Localize.FromTable("ui.rb.chest.open", "{0} 열기!"), Localize.Get($"store.chest_{chestKey}.name"));
            _ui.SetText("RbChestOpenTitle", title);
            _ui.SetText("RbChestOpenHeadTitle", title);
            var fx = _shell != null ? _shell.Fx : null;
            if (_ui.Get<Image>("RbChestOpenChest") is Image chest)
            {
                if (fx != null) fx.ResetChest(chest);
                var s = RbRewards.Find(icons, $"rb_icon_chest_{chestKey}_96");
                if (s != null) chest.sprite = s;
            }
            Phase();
            // 머묾 — 닫힌 상자가 흔들리며 틈으로 빛이 샌다(등급 계단은 RbOutFx 표)
            if (fx != null && _ui.Get<Image>("RbChestOpenChest") is Image idle) fx.ChestIdle(idle, chestKey);
        }

        private void Advance()
        {
            if (_busy) return;
            if (!_opened)
            {
                OpenAsync().Forget();   // fire-and-forget: 뚜껑 열림 연출(0.8초) 뒤 받은 것 쪽으로
                return;
            }
            gameObject.SetActive(false);
        }

        // 열리는 중에 창을 다시 열면(다음 상자) 앞 열기가 끝나며 새 창을 받은 것 쪽으로 넘겨 버렸다 — 앞 것을 끊는다
        private void CancelOpen()
        {
            _openCts?.Cancel();
            _openCts?.Dispose();
            _openCts = null;
        }

        private void OnDisable() => CancelOpen();

        private async UniTaskVoid OpenAsync()
        {
            _busy = true;
            GameSound.Cue("run.card");
            _openCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            var ct = _openCts.Token;
            var fx = _shell != null ? _shell.Fx : null;
            if (fx != null && _ui.Get<Image>("RbChestOpenChest") is Image chest)
                await fx.ChestOpenAsync(chest, _chestKey, ct);
            if (this == null || ct.IsCancellationRequested) return;
            _opened = true;
            _busy = false;
            Phase();
            // 받은 칸이 차례로 열리고 금화는 위 막대 골드 칩으로(돈 → 물건 방향)
            if (fx != null && _shown.Count > 0)
                fx.RewardCells(_shown, _lines.Count > 0 && _lines[0].kind == "gold" ? _shown[0] : null, _shell.GoldChip);
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
            _shown.Clear();
            for (int i = 0; i < Cells; i++)
            {
                var cell = _ui.Find($"RbChestOpenCell{i}") as RectTransform;
                if (cell == null) continue;
                cell.gameObject.SetActive(i < n);
                if (i >= n) continue;
                var (kind, key, amount) = _lines[i];
                cell.localScale = new Vector3(scale, scale, 1f);
                _shown.Add(cell);
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
