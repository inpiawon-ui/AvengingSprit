using System.Collections.Generic;
using Game.Module.Common;
using Game.Module.Common.UI;
using Game.User;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// RE:BORN 챕터 선택 — 시안 Screens/chapter-select.png. 로비 챕터 카드를 누르면 열린다.
    /// 줄 = 섬네일 · CH n · 이름 · 설명 · 최고 기록(또는 여는 조건) · 도전 가능 / 잠김. 열린 줄을 누르면 그 챕터로 로비에 돌아간다.
    /// 줄은 틀(ChsRow) 하나를 챕터 수만큼 복제한다 — 챕터 수를 코드에 적지 않는다.
    /// </summary>
    public sealed class RbChapterPage : MonoBehaviour
    {
        private const float RowPitch = 264f * 2f / 3f, ListPad = 40f, NameGap = 14f;
        private const int RoomsPerChapter = 30;

        private UIBinder _ui;
        private RbLobbyUI _shell;
        private readonly List<Transform> _rows = new();

        private void Awake()
        {
            if (_ui != null) return;
            _ui = new UIBinder(transform);
            _shell = GetComponentInParent<RbLobbyUI>(true);
            _ui.OnClick("ChsBack", () => _shell.SelectTab("Battle"));
            _ui.OnClick("ChsHelp", () => SystemPopup.Show(Localize.FromTable("ui.rb.chs.help", "챕터를 깨면 다음 챕터가 열려요.\n깬 챕터도 다시 도전할 수 있어요."), null, Localize.Get("ui.common.ok"), null));
        }

        public void Open(IPlayerDataService player, Sprite[] stageBgs, int selected)
        {
            Awake();
            gameObject.SetActive(true);
            int count = Mathf.Max(1, player.ChapterCount);
            EnsureRows(count);
            for (int i = 0; i < _rows.Count; i++) Fill(_rows[i], i + 1, player, stageBgs, selected);
            if (_ui.Find("ChsBody") is RectTransform view && view.TryGetComponent<ScrollRect>(out var sr))
            {
                if (sr.content != null) sr.content.sizeDelta = new Vector2(sr.content.sizeDelta.x, count * RowPitch + ListPad);
                // 고른 챕터가 보이게 — 위에서 몇 번째 줄인지로 비율을 잡는다
                float range = Mathf.Max(1f, sr.content.rect.height - view.rect.height);
                sr.verticalNormalizedPosition = 1f - Mathf.Clamp01(((selected - 1) * RowPitch - RowPitch) / range);
            }
        }

        private void EnsureRows(int count)
        {
            if (_rows.Count == 0 && _ui.Find("ChsRow") is Transform tpl) _rows.Add(tpl);
            if (_rows.Count == 0) return;
            var first = (RectTransform)_rows[0];
            while (_rows.Count < count)
            {
                var copy = (RectTransform)Instantiate(first, first.parent);
                copy.name = $"ChsRow{_rows.Count}";
                copy.anchoredPosition = first.anchoredPosition + new Vector2(0f, -RowPitch * _rows.Count);
                _rows.Add(copy);
            }
            for (int i = 0; i < _rows.Count; i++) _rows[i].gameObject.SetActive(i < count);
        }

        private void Fill(Transform row, int ch, IPlayerDataService player, Sprite[] stageBgs, int selected)
        {
            bool open = ch <= player.UnlockedChapter;
            bool cleared = ch < player.UnlockedChapter || ch <= player.ClearedChapter;
            if (Find(row, "ChsRowThumbArt") is Transform t && t.TryGetComponent<Image>(out var art) && stageBgs != null && stageBgs.Length > 0)
                art.sprite = stageBgs[(ch - 1) % stageBgs.Length];
            var no = Text(row, "ChsRowNo", $"CH {ch}");
            var info = player.ChapterInfo(ch);
            var name = Text(row, "ChsRowName", Localize.FromTable($"stage.{ch}.1.name", info.Name ?? string.Empty));
            if (no != null && name != null)
            {
                // 이름은 「CH n」 바로 뒤 — 번호 자리 수가 늘어도 붙어 따라간다
                var nr = no.rectTransform;
                float left = nr.anchoredPosition.x - nr.rect.width * 0.5f + no.GetPreferredValues(no.text).x + NameGap;
                var r = name.rectTransform;
                // 오른쪽은 「도전 가능」 칸 앞까지 — 긴 이름(영어)이 겹치지 않게 줄여 쓴다
                float right = Find(row, "ChsRowState") is RectTransform st ? st.anchoredPosition.x - st.rect.width * 0.5f - NameGap : left + r.rect.width;
                r.sizeDelta = new Vector2(Mathf.Max(40f, right - left), r.sizeDelta.y);
                r.anchoredPosition = new Vector2(left + r.sizeDelta.x * 0.5f, r.anchoredPosition.y);
            }
            // 설명은 두 줄(「…거리\n…기다린다.」) — 줄 칸이 한 줄이라 첫 줄만 보인다
            string desc = Localize.FromTable($"chapter.{ch}.desc", info.Desc ?? string.Empty).Replace("\\n", "\n");
            int cut = desc.IndexOf('\n');
            Text(row, "ChsRowDesc", cut >= 0 ? desc.Substring(0, cut) : desc);
            string best = !open ? string.Format(Localize.FromTable("ui.rb.chs.unlock_hint", "챕터 {0} 을(를) 클리어하면 열립니다"), ch - 1)
                        : cleared ? Localize.FromTable("ui.rb.lobby.cleared", "클리어")
                        : ch == player.CurrentChapter && player.ReachedStage > 0
                            ? string.Format(Localize.FromTable("ui.rb.lobby.best", "최고 {0}/{1}방"), player.ReachedStage, RoomsPerChapter)
                            : Localize.FromTable("ui.rb.chs.no_record", "최고 기록 없음");
            Text(row, "ChsRowBest", best);
            SetOn(row, "ChsRowState", open);
            SetOn(row, "ChsRowLockIcon", !open);
            SetOn(row, "ChsRowLockText", !open);
            SetOn(row, "ChsRowSel", ch == selected);
            if (row.TryGetComponent<Button>(out var b))
            {
                b.onClick.RemoveAllListeners();
                int pick = ch;
                b.onClick.AddListener(() =>
                {
                    if (!open) { GameSound.Cue("ui.tap"); return; }
                    GameSound.Cue("ui.select");
                    _shell.PickChapter(pick);
                });
            }
        }

        private static Transform Find(Transform root, string name)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                var c = root.GetChild(i);
                if (c.name == name) return c;
                var deep = Find(c, name);
                if (deep != null) return deep;
            }
            return null;
        }

        private static void SetOn(Transform row, string node, bool on)
        {
            if (Find(row, node) is Transform t) t.gameObject.SetActive(on);
        }

        private static TextMeshProUGUI Text(Transform row, string node, string value)
        {
            if (Find(row, node) is not Transform t || !t.TryGetComponent<TextMeshProUGUI>(out var tmp)) return null;
            if (tmp.text != value) tmp.text = value;
            return tmp;
        }
    }
}
