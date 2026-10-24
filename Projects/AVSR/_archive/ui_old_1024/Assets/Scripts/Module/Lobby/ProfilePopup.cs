using System;
using System.Collections.Generic;
using Game.Character;
using Game.Module.Common;
using Game.Module.Common.Store;
using Game.Module.Common.UI;
using Game.Module.Events;
using Game.User;
using GameFramework.Core.Base;
using GameFramework.Core.Module.EventBus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.Lobby
{
    /// <summary>
    /// 프로필 꾸미기 — 로비 왼쪽 위 프로필을 누르면 뜬다(기획서 B-9, 시안 `mock_store_profile`). 능력치 없음 · 꾸미기 전용.
    /// 테두리 · 대표 얼굴(유령 + 가진 호스트) · 칭호. 노드는 `ProfileBuilder` 가 세운다.
    /// </summary>
    public sealed class ProfilePopup : MonoBehaviour
    {
        [SerializeField] private LobbyShopPanel.KeyedSprite[] _frames = Array.Empty<LobbyShopPanel.KeyedSprite>();
        [SerializeField] private LobbyShopPanel.KeyedSprite[] _faces = Array.Empty<LobbyShopPanel.KeyedSprite>();
        [SerializeField] private Sprite _tab, _tabSelected, _soon;

        private const int Cells = 24;
        private static readonly string[] Types = { "frame", "face", "title" };

        private UIBinder _ui;
        private IDisposable _token;
        private string _type = "frame";
        private string _selected;
        private readonly List<string> _keys = new();

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            _ui = new UIBinder(transform);
            Localize.ApplyFonts(transform);
            _ui.OnClick("ProfileTabFrame", () => ShowTab("frame"));
            _ui.OnClick("ProfileTabFace", () => ShowTab("face"));
            _ui.OnClick("ProfileTabTitle", () => ShowTab("title"));
            _ui.OnClick("ProfileClose", Close);
            _ui.OnClick("ProfileEquip", Equip);
            for (int i = 0; i < Cells; i++)
            {
                int c = i;
                _ui.OnClick($"ProfileCell{i}", () => Select(c));
            }
        }

        private void OnEnable()
        {
            if (CoreModule.TryGet<IEventBus>(out var bus)) _token = bus.Subscribe<SeasonChangedEvent>(_ => Refresh());
        }

        private void OnDisable()
        {
            _token?.Dispose();
            _token = null;
        }

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            ShowTab("frame");
        }

        public void Close() => gameObject.SetActive(false);

        private void ShowTab(string type)
        {
            _type = type;
            _selected = CoreModule.TryGet<ISeasonService>(out var s) ? s.Equipped(type) : null;
            SetTab("ProfileTabFrame", type == "frame");
            SetTab("ProfileTabFace", type == "face");
            SetTab("ProfileTabTitle", type == "title");
            var scroll = _ui.Get<ScrollRect>("ProfileGrid");
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
            Refresh();
        }

        private void SetTab(string node, bool on)
        {
            var img = _ui.Get<Image>(node);
            if (img != null) img.sprite = on ? _tabSelected : _tab;
            var t = _ui.Find(node);
            var label = t != null ? t.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            if (label != null) label.color = on ? new Color(1f, 0.85f, 0.32f) : Color.white;  // 고른 탭은 금테 · 어두운 속 — 시안처럼 금색 글자
        }

        private void Refresh()
        {
            if (!CoreModule.TryGet<ISeasonService>(out var s) || !s.IsReady) return;
            CoreModule.TryGet<IPlayerDataService>(out var p);
            _ui.SetText("ProfileTitleText", Localize.FromTable("ui.profile.title", "프로필 꾸미기"));
            _ui.SetText("ProfileTabFrameText", Localize.FromTable("ui.profile.tab.frame", "테두리"));
            _ui.SetText("ProfileTabFaceText", Localize.FromTable("ui.profile.tab.face", "대표 얼굴"));
            _ui.SetText("ProfileTabTitleText", Localize.FromTable("ui.profile.tab.title", "칭호"));
            _ui.SetText("ProfileNote", Localize.FromTable("ui.profile.note", "능력치 변화 없음 · 꾸미기 전용"));
            _ui.SetText("ProfileEquipText", Localize.FromTable("ui.profile.equip", "장착"));

            // 미리보기 — 고른 것(장착 전이면 고른 것으로 미리)
            string face = _type == "face" && _selected != null ? _selected : s.Equipped("face");
            string frame = _type == "frame" && _selected != null ? _selected : s.Equipped("frame");
            string title = _type == "title" && _selected != null ? _selected : s.Equipped("title");
            Pic("ProfilePreviewFace", Keyed(_faces, face));
            ApplyLobbyFace(s);
            Pic("ProfilePreviewFrame", FrameArt(s, frame));
            var host = p?.GetHost(face);
            _ui.SetText("ProfilePreviewName", host != null ? host.DisplayName : Localize.Get("host.ghost.name"));
            _ui.SetText("ProfilePreviewTitle", Localize.Get($"profile.{title}.name"));

            // 칸
            _keys.Clear();
            if (_type == "face")
            {
                _keys.Add(HostEntry.GhostKey);
                var hosts = p?.PlayableHosts;
                if (hosts != null)
                    for (int i = 0; i < hosts.Count; i++)
                        if (hosts[i] != null && !hosts[i].IsGhost && !PlayerDataService.IsHiddenHost(hosts[i].HostKey)
                            && p.IsHostOwned(hosts[i])) _keys.Add(hosts[i].HostKey);   // 사신(히든)은 얼굴 목록에도 안 보인다
            }
            else
            {
                foreach (var it in s.ProfileItems) if (it.Type == _type) _keys.Add(it.Key);
                _keys.Add("__soon");   // 「곧 공개」 칸(시안)
            }
            for (int i = 0; i < Cells; i++)
            {
                var cell = _ui.Find($"ProfileCell{i}");
                if (cell == null) continue;
                bool on = i < _keys.Count;
                cell.gameObject.SetActive(on);
                if (!on) continue;
                FillCell(s, p, cell, _keys[i]);
            }
            var content = _ui.Find("ProfileGridContent") as RectTransform;
            if (content != null)
                content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.CeilToInt(_keys.Count / 2f) * 232f + 8f);

            var equip = _ui.Get<Button>("ProfileEquip");
            if (equip != null) equip.interactable = CanEquip(s, p, _selected) && _selected != s.Equipped(_type);
        }

        private void FillCell(ISeasonService s, IPlayerDataService p, Transform cell, string key)
        {
            bool soon = key == "__soon";
            bool owned = !soon && (_type == "face" || s.Owns(key));
            bool equipped = !soon && s.Equipped(_type) == key;
            Act(cell, "CellSelected", key == _selected);
            Act(cell, "CellCheck", equipped);
            Act(cell, "CellLock", !soon && !owned);
            string name = soon ? Localize.FromTable("ui.profile.soon", "곧 공개")
                : _type == "face" ? (p?.GetHost(key)?.DisplayName ?? key)
                : Localize.Get($"profile.{key}.name");
            Txt(cell, "CellName", name);
            var artNode = _ui.Find(cell, "CellArt");
            var art = artNode != null ? artNode.GetComponent<Image>() : null;
            if (art != null)
            {
                art.sprite = soon ? _soon : _type == "face" ? Keyed(_faces, key) : _type == "frame" ? FrameArt(s, key) : null;
                art.enabled = art.sprite != null;
                art.preserveAspect = true;
                art.color = owned || soon ? Color.white : new Color(0.45f, 0.48f, 0.58f);
            }
            Act(cell, "CellTitleBand", _type == "title" && !soon);
            Txt(cell, "CellTitleText", _type == "title" && !soon ? name : string.Empty);
            string status = soon ? string.Empty
                : equipped ? Localize.FromTable("ui.profile.equipped", "장착 중")
                : owned ? Localize.FromTable("ui.profile.owned", "보유")
                : string.Format(Localize.FromTable("ui.profile.source", "얻는 곳: {0}"), Source(s, key));
            Act(cell, "CellStatus", status.Length > 0);
            Txt(cell, "CellStatusText", status);
        }

        private static string Source(ISeasonService s, string key)
        {
            foreach (var it in s.ProfileItems)
            {
                if (it.Key != key) continue;
                if (it.Source.StartsWith("chapter_clear:"))
                    return string.Format(Localize.FromTable("ui.profile.src_chapter", "챕터 {0} 클리어"), it.Source.Substring(14));
                if (it.Source.StartsWith("pass_")) return Localize.FromTable("ui.profile.src_pass", "시즌 패스");
            }
            return "-";
        }

        private Sprite FrameArt(ISeasonService s, string key)
        {
            foreach (var it in s.ProfileItems)
                if (it.Key == key) return Keyed(_frames, it.Art);
            return null;
        }

        private static bool CanEquip(ISeasonService s, IPlayerDataService p, string key)
        {
            if (string.IsNullOrEmpty(key) || key == "__soon") return false;
            return s.Owns(key) || p != null && (key == HostEntry.GhostKey || p.GetHost(key) != null && p.IsHostOwned(p.GetHost(key)));
        }

        private void Select(int i)
        {
            if (i >= _keys.Count || _keys[i] == "__soon") return;
            _selected = _keys[i];
            GameSound.Cue("ui.tap");
            Refresh();
        }

        private void Equip()
        {
            if (!CoreModule.TryGet<ISeasonService>(out var s)) return;
            if (s.TryEquip(_type, _selected)) GameSound.Cue("ui.select");
        }

        /// <summary>로비 왼쪽 위 프로필 얼굴을 장착한 얼굴로 — 팝업과 같은 그림 목록을 쓴다.</summary>
        private void ApplyLobbyFace(ISeasonService s)
        {
            var lobby = transform.parent;
            var node = lobby != null ? _ui.Find(lobby, "ProfileGhostFace") : null;
            var img = node != null ? node.GetComponent<Image>() : null;
            var sprite = Keyed(_faces, s.Equipped("face"));
            if (img != null && sprite != null) img.sprite = sprite;
        }

        // ── 도우미 ───────────────────────────────────────────

        private void Pic(string node, Sprite sprite)
        {
            var img = _ui.Get<Image>(node);
            if (img == null) return;
            img.sprite = sprite;
            img.enabled = sprite != null;
            img.preserveAspect = true;
        }

        private static Sprite Keyed(LobbyShopPanel.KeyedSprite[] list, string key)
        {
            if (list == null || string.IsNullOrEmpty(key)) return null;
            for (int i = 0; i < list.Length; i++) if (list[i].Key == key) return list[i].Sprite;
            return null;
        }

        private void Txt(Transform root, string name, string value)
        {
            var t = _ui.Find(root, name);
            var tmp = t != null ? t.GetComponent<TextMeshProUGUI>() : null;
            if (tmp != null) tmp.text = value;
        }

        private void Act(Transform root, string name, bool on)
        {
            var t = _ui.Find(root, name);
            if (t != null && t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
        }
    }
}
