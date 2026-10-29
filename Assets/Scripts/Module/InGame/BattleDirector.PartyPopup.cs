using System.Collections.Generic;
using GameFramework.Core.Base;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 빙의 파티 창 세 개 — **PD 확정 시안 그대로**(PD 2026-10-29 「시안을 컨펌했는데 너무 대충 적용하면 어떻게 믿냐」).
    ///   · 빙의 선택창 `_exchange/in/mock_partyui_F_choice.png` — 큰 얼굴 · 이름 · 태그 칩 · 보조 자동 스킬 / 줄마다 얼굴 칸 · 비용 · 남은 유령 HP · 시너지 바뀜 · 추천
    ///   · 메인 사망 `F_death.png` — 판 없이 피 튄 제목 · 원형 시계 · 얇은 줄 A · B · C(얼굴 · 유령 비용 · 남는 HP · 사슬 · 추천 육각)
    ///   · 시너지 설명 `F_syndetail.png` — 누른 칸 옆 말풍선(문장 · 이름 · 설명 · 단계 줄 · 무리 몸 칸 · 규칙)
    /// 좌표는 전부 시안 720 × 1280 판의 px 상자(왼 · 위 · 오른 · 아래)다 — `Mx` 하나로만 놓는다.
    /// 글자는 HUD 굵은 글꼴을 본뜬다(기본 TMP 글꼴은 픽셀 글꼴이라 한글이 띄엄띄엄 넓게 나왔다).
    /// </summary>
    public sealed partial class BattleDirector
    {
        // 시안 색
        private static readonly Color PopGold = new(1f, 0.79f, 0.29f);
        private static readonly Color PopCyan = new(0.37f, 0.85f, 1f);
        private static readonly Color PopRed = new(1f, 0.33f, 0.40f);
        private static readonly Color PopGray = new(0.62f, 0.66f, 0.76f);
        private static readonly Color PopDim = new(0.42f, 0.45f, 0.52f);

        private TMP_Text _popLike;
        private InGameMainUI _popUi;

        // ─────────────────────────────────────────
        // 판 · 놓기 도구
        // ─────────────────────────────────────────

        /// <summary>시안 px 상자.</summary>
        private readonly struct MBox
        {
            public readonly float X0, Y0, X1, Y1;
            public MBox(float x0, float y0, float x1, float y1) { X0 = x0; Y0 = y0; X1 = x1; Y1 = y1; }
            public float W => X1 - X0;
            public float H => Y1 - Y0;
            public MBox Shift(float dy) => new(X0, Y0 + dy, X1, Y1 + dy);
        }

        /// <summary>창 바닥(화면 전체 · 딤) + 시안 판(720 × 1280, 가운데). 판을 돌려준다.</summary>
        private RectTransform PopupBoard(float dim, bool tapClose)
        {
            ClosePartyPopup();
            var canvas = _unitLayer != null ? _unitLayer.GetComponentInParent<Canvas>() : null;
            if (canvas == null) return null;
            var root = canvas.rootCanvas.transform;
            if (_popLike == null)
                foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
                    if (t.name == "RbNoBodyText") { _popLike = t; break; }
            if (_popUi == null) TryGetComponent(out _popUi);

            var go = new GameObject("PartyChoicePopup", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(root, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, dim);
            if (tapClose) go.AddComponent<Button>().onClick.AddListener(ClosePartyPopup);

            var board = NewRect("Board", rt, new Vector2(720f, 1280f));
            _partyPopup = go;
            _partyHold = true;
            _partyTimer = -1f;
            return board;
        }

        /// <summary>시안 상자 b 를 부모(시안 상자 pb 자리)의 자식으로 놓는다.</summary>
        private static RectTransform Mx(string name, RectTransform parent, MBox pb, MBox b)
        {
            var rt = NewRect(name, parent, new Vector2(b.W, b.H));
            rt.anchoredPosition = new Vector2((b.X0 + b.X1) * 0.5f - (pb.X0 + pb.X1) * 0.5f,
                                              (pb.Y0 + pb.Y1) * 0.5f - (b.Y0 + b.Y1) * 0.5f);
            return rt;
        }

        private static readonly MBox BoardBox = new(0f, 0f, 720f, 1280f);

        private Image MImg(string name, RectTransform parent, MBox pb, MBox b, string sprite, bool sliced = false)
        {
            var img = Mx(name, parent, pb, b).gameObject.AddComponent<Image>();
            var sp = sprite != null ? GetSprite(sprite) : null;
            img.sprite = sp;
            img.type = sliced && sp != null && sp.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.color = sp != null || sprite == null ? Color.white : new Color(0.1f, 0.12f, 0.2f, 0.9f);
            img.raycastTarget = false;
            return img;
        }

        private TextMeshProUGUI MTxt(RectTransform parent, MBox pb, MBox b, string s, float size, Color c,
                                     TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, bool wrap = false)
        {
            var rt = Mx("Text", parent, pb, b);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (_popLike != null) { t.font = _popLike.font; t.fontSharedMaterial = _popLike.fontSharedMaterial; }
            t.text = s;
            t.fontSize = size;
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = Mathf.Min(size, 9f);
            t.color = c;
            t.alignment = align;
            t.enableWordWrapping = wrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private Sprite PopFace(string key) => _popUi != null && key != null ? _popUi.FaceOf(key) : null;

        /// <summary>정사각 칸 안 얼굴(칸 테 안쪽으로 들인다).</summary>
        private void MFace(RectTransform parent, MBox pb, MBox box, string frame, string key, float inset, Color tint)
        {
            if (frame != null) MImg("Frame", parent, pb, box, frame);
            var face = PopFace(key);
            if (face == null) return;
            var img = MImg("Face", parent, pb, new MBox(box.X0 + inset, box.Y0 + inset, box.X1 - inset, box.Y1 - inset), null);
            img.sprite = face;
            img.preserveAspect = true;
            img.color = tint;
        }

        private void FinishPopup()
        {
            if (_partyPopup != null && CoreModule.TryGet<Game.Module.Common.ILanguageService>(out var lang) && lang.IsReady)
                lang.ApplyFonts(_partyPopup.transform);
        }

        // ─────────────────────────────────────────
        // 무리 시너지 바뀜 — 이 호스트를 넣으면(빼면) 무엇이 켜지고 꺼지나
        // ─────────────────────────────────────────

        private readonly struct SynDelta
        {
            public readonly string Tag;
            public readonly int Count;
            public readonly bool Gain;
            public SynDelta(string tag, int count, bool gain) { Tag = tag; Count = count; Gain = gain; }
        }

        private readonly HashSet<string> _probeKeys = new();
        private readonly Dictionary<string, int> _probeBefore = new();
        private readonly Dictionary<string, int> _probeAfter = new();

        private static void CountOrgs(HashSet<string> keys, Dictionary<string, int> into)
        {
            into.Clear();
            foreach (var k in keys)
                if (OrgOf(k) is string o) into[o] = into.TryGetValue(o, out int n) ? n + 1 : 1;
        }

        /// <summary>파티에서 out 을 빼고 add 를 넣었을 때 켜지는(+) · 꺼지는(−) 무리.</summary>
        private List<SynDelta> SynChange(string outKey, string addKey)
        {
            var list = new List<SynDelta>();
            _probeKeys.Clear();
            foreach (var k in Unit.PartyKeys) _probeKeys.Add(k);
            CountOrgs(_probeKeys, _probeBefore);
            if (outKey != null) _probeKeys.Remove(outKey);
            if (addKey != null) _probeKeys.Add(addKey);
            CountOrgs(_probeKeys, _probeAfter);
            foreach (var kv in _probeAfter)
            {
                _probeBefore.TryGetValue(kv.Key, out int before);
                if (kv.Value >= 2 && kv.Value > before) list.Add(new SynDelta(kv.Key, kv.Value, true));
            }
            foreach (var kv in _probeBefore)
            {
                _probeAfter.TryGetValue(kv.Key, out int after);
                if (kv.Value >= 2 && after < 2) list.Add(new SynDelta(kv.Key, after, false));
            }
            return list;
        }

        /// <summary>「◆ + 무리 2/3 • ◆ − 무리」 — 시안 줄 오른쪽 아래.</summary>
        private void MDeltas(RectTransform row, MBox rb, List<SynDelta> deltas, float x0, float y0)
        {
            float x = x0;
            for (int i = 0; i < deltas.Count && i < 2; i++)
            {
                var d = deltas[i];
                if (i > 0)
                {
                    MTxt(row, rb, new MBox(x, y0 + 8, x + 14, y0 + 36), "•", 18, Color.white, TextAlignmentOptions.Center);
                    x += 16;
                }
                var icon = MImg("SynIcon", row, rb, new MBox(x, y0, x + 40, y0 + 44), SynIcon(d.Tag));
                icon.preserveAspect = true;
                if (!d.Gain) icon.color = new Color(0.62f, 0.5f, 0.42f);
                string name = SynShortName(d.Tag);
                string s = d.Gain ? $"<color=#FFC94A>+</color> {name}\n<size=90%>{d.Count}/3</size>" : $"− {name}";
                MTxt(row, rb, new MBox(x + 44, y0, x + 118, y0 + 44), s, 18, d.Gain ? PopGold : Color.white,
                     d.Gain ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft);
                x += 122;
            }
        }

        // ─────────────────────────────────────────
        // 빙의 선택창 — F_choice
        // ─────────────────────────────────────────

        private sealed class ChoiceRow
        {
            public string Frame;       // 얼굴 칸 그림(메인 · 보조 · 빈 칸) — 놔두기는 null
            public string FaceKey;
            public bool Ghost;
            public string Title;
            public int Cost;
            public int Left = -1;      // 남은 횟수(없으면 −1)
            public List<SynDelta> Deltas;
            public bool Enabled;
            public bool Best;
            public UnityAction Act;
        }

        private void OpenPartyChoice(Unit target)
        {
            _partyPick = target;
            var entry = _player != null ? _player.GetHost(target.Key) : null;
            string name = entry != null ? entry.DisplayName : target.DisplayName;
            int mainLeft = PartyRule.MainSwapMax - _mainSwaps;
            int subLeft = PartyRule.SubSwapMax - _subSwaps;
            var rows = new List<ChoiceRow>
            {
                new()
                {
                    Frame = "partyhud_main_frame", FaceKey = _host.Key, Title = Tx("ui.party.p2.main_swap"),
                    Cost = PartyRule.CostMainSwap, Left = mainLeft, Deltas = SynChange(_host.Key, target.Key),
                    Enabled = mainLeft > 0 && _ghostHp > PartyRule.CostMainSwap, Act = () => PartySwapMain(target),
                },
            };
            for (int i = 0; i < PartyRule.SubSlots; i++)
            {
                int slot = i;
                if (i < _subs.Count)
                {
                    string old = _subs[i].U.Key;
                    rows.Add(new ChoiceRow
                    {
                        Frame = "partyhud_sub_frame", FaceKey = old, Title = Tf("ui.party.p2.sub_swap", i + 1),
                        Cost = PartyRule.CostSubSwap, Left = subLeft, Deltas = SynChange(old, target.Key),
                        Enabled = subLeft > 0 && _ghostHp > PartyRule.CostSubSwap,
                        Act = () => PartySetSub(target, slot, PartyRule.CostSubSwap),
                    });
                }
                else
                {
                    rows.Add(new ChoiceRow
                    {
                        Frame = "partyhud_empty_frame", Title = Tf("ui.party.p2.sub_add", i + 1),
                        Cost = PartyRule.CostSub, Deltas = SynChange(null, target.Key),
                        Enabled = _ghostHp > PartyRule.CostSub, Act = () => PartySetSub(target, slot, PartyRule.CostSub),
                    });
                    break;   // 빈 칸은 앞에서부터 하나만 보인다
                }
            }
            rows.Add(new ChoiceRow { Ghost = true, Title = Tx("ui.party.p2.leave"), Enabled = true, Deltas = new List<SynDelta>() });

            // 추천 — 켜지는 무리가 가장 많은 줄, 같으면 싼 줄(시안: 빈 보조 칸 「추천」)
            ChoiceRow best = null;
            int bestGain = -1;
            foreach (var r in rows)
            {
                if (!r.Enabled || r.Ghost) continue;
                int gain = 0;
                foreach (var d in r.Deltas) gain += d.Gain ? 1 : -1;
                if (gain > bestGain || (gain == bestGain && best != null && r.Cost < best.Cost)) { best = r; bestGain = gain; }
            }
            if (best != null) best.Best = true;

            BuildChoice(target, name, rows);
        }

        private void BuildChoice(Unit target, string name, List<ChoiceRow> rows)
        {
            var board = PopupBoard(0.55f, false);
            if (board == null) return;
            const float rowTop = 690f, pitch = 101f, rowH = 94f;
            float bottom = rowTop + pitch * rows.Count + 2f;
            var panelBox = new MBox(34, 553, 688, bottom);
            var panel = MImg("Panel", board, BoardBox, panelBox, "party_popup_panel", true).rectTransform;

            // 머리 — 큰 얼굴(칸 밖으로 넘는 부분은 자른다) · 이름 · 태그 칩 · 세로선 · 보조 자동 스킬
            var faceArea = Mx("FaceArea", panel, panelBox, new MBox(46, 560, 274, 688));
            faceArea.gameObject.AddComponent<RectMask2D>();
            var faceBox = new MBox(46, 560, 274, 688);
            if (PopFace(target.Key) is Sprite big)
            {
                var img = MImg("BigFace", faceArea, faceBox, new MBox(70, 536, 250, 716), null);
                img.sprite = big;
                img.preserveAspect = true;
            }
            MTxt(panel, panelBox, new MBox(284, 578, 470, 636), name, 40, Color.white, TextAlignmentOptions.MidlineLeft);
            float tx = 284;
            var kind = AffinityRule.KindOf(target.Key);
            if (AttrName(kind) is string attr) { Chip(panel, panelBox, ref tx, "party_tag_gold", SynShortName(attr), PopGold); }
            if (OrgOf(target.Key) is string org) { Chip(panel, panelBox, ref tx, "party_tag_red", SynShortName(org), new Color(1f, 0.62f, 0.55f)); }
            MImg("Divider", panel, panelBox, new MBox(474, 590, 480, 684), "party_divider_v", true);
            MTxt(panel, panelBox, new MBox(490, 592, 684, 618), Tx("ui.party.p2.sub_skill"), 19, PopCyan);
            var skillBox = new MBox(490, 624, 550, 684);
            MImg("SkillBox", panel, panelBox, skillBox, "party_box_cyan");
            if (ActiveSkillIcon(target.Key) is Sprite sk)
            {
                var si = MImg("SkillIcon", panel, panelBox, new MBox(496, 630, 544, 678), null);
                si.sprite = sk;
                si.preserveAspect = true;
            }
            MTxt(panel, panelBox, new MBox(558, 628, 684, 680), SubSkillName(target.Key), 30, Color.white);

            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                float y = rowTop + pitch * i;
                var rb = new MBox(50, y, 672, y + rowH);
                var bg = MImg($"Option{i + 1}", panel, panelBox, rb,
                              !r.Enabled ? "party_row_off" : r.Best ? "party_row_gold" : "party_row", true);
                if (bg.sprite == null && r.Best) { bg.sprite = GetSprite("party_row_best"); bg.type = Image.Type.Sliced; bg.color = Color.white; }
                bg.raycastTarget = true;
                var row = bg.rectTransform;
                var btn = row.gameObject.AddComponent<Button>();
                btn.interactable = r.Enabled;
                var act = r.Act;
                btn.onClick.AddListener(() => { ClosePartyPopup(); act?.Invoke(); });
                var off = r.Enabled ? Color.white : PopDim;

                // 얼굴 칸
                var fb = new MBox(64, y + 13, 132, y + 81);
                if (r.Ghost)
                {
                    var gi = MImg("Ghost", row, rb, new MBox(70, y + 18, 126, y + 76), "party_ghost_mini");
                    gi.preserveAspect = true;
                    if (gi.sprite == null) gi.sprite = GhostHudSprite();
                }
                else if (r.FaceKey == null)
                {
                    MImg("Frame", row, rb, fb, r.Frame);
                    MTxt(row, rb, new MBox(fb.X0, y + 50, fb.X1, y + 78), $"−{PartyRule.CostSub}", 16, PopGray, TextAlignmentOptions.Center);
                }
                else MFace(row, rb, fb, r.Frame, r.FaceKey, 7f, r.Enabled ? Color.white : PopDim);

                // 제목 · 비용 · 남은 횟수
                string title = r.Ghost ? r.Title
                    : $"{r.Title}  <color=#FF5566><size=125%>−{r.Cost}</size></color>"
                      + (r.Left >= 0 ? $" <size=72%><color=#E0E4F0>{Tf("ui.party.p2.left_times", r.Left)}</color></size>" : string.Empty);
                MTxt(row, rb, new MBox(160, y + 6, r.Best ? 566 : 664, y + 44), title, 25, off);
                if (r.Best)
                {
                    MImg("BestBadge", row, rb, new MBox(578, y + 16, 664, y + 52), "party_badge_best", true);
                    MTxt(row, rb, new MBox(578, y + 16, 664, y + 52), Tx("ui.party.p2.best"), 20, new Color(0.25f, 0.13f, 0.02f),
                         TextAlignmentOptions.Center);
                }

                // 남은 유령 HP 칸
                var pill = new MBox(156, y + 48, 360, y + 88);
                MImg("HpPill", row, rb, pill, "party_hp_pill", true);
                int leftHp = r.Ghost ? _ghostHp : _ghostHp - r.Cost;
                string col = leftHp >= GhostHpMax / 2 ? "#5FD8FF" : "#FF5566";
                string hp = r.Ghost
                    ? Tf("ui.party.p2.hp_keep", $"<size=150%><color={col}>{leftHp}</color></size>")
                    : $"{Tx("ui.party.p2.hp_left")}  <size=150%><color={col}>{leftHp}</color></size>";
                MTxt(row, rb, new MBox(pill.X0 + 10, pill.Y0, pill.X1 - 6, pill.Y1), hp, 17, off);

                if (r.Deltas.Count > 0) MDeltas(row, rb, r.Deltas, 410, y + 44);
            }
            FinishPopup();
        }

        private void Chip(RectTransform panel, MBox pb, ref float x, string sprite, string text, Color c)
        {
            // 칩 폭은 글자 폭 + 양옆 14 — 영어 「Weapon」 이 84 칸을 넘었다(2026-10-29 5언어 검수). 시안 한국어 칩은 84 그대로
            var img = MImg("Tag", panel, pb, new MBox(x, 640, x + 84, 674), sprite, true);
            var t = MTxt(panel, pb, new MBox(x, 640, x + 84, 674), text, 20, c, TextAlignmentOptions.Center);
            float w = Mathf.Max(84f, t.GetPreferredValues(text).x + 28f);
            img.rectTransform.sizeDelta = new Vector2(w, 34f);
            t.rectTransform.sizeDelta = new Vector2(w, 34f);
            img.rectTransform.anchoredPosition += new Vector2((w - 84f) * 0.5f, 0f);
            t.rectTransform.anchoredPosition += new Vector2((w - 84f) * 0.5f, 0f);
            x += w + 8f;
        }

        private string SubSkillName(string key)
        {
            var e = _player != null ? _player.GetHost(key) : null;
            return SubSafeSkills.Contains(key) && e != null && !string.IsNullOrEmpty(e.ActiveSkillKey)
                ? Game.Module.Common.Localize.Get($"askill.{e.ActiveSkillKey}.name")
                : Tx("ui.party.sub_strike");
        }

        private Sprite _ghostHud;

        private Sprite GhostHudSprite()
        {
            if (_ghostHud != null || _popUi == null) return _ghostHud;
            foreach (var img in _popUi.GetComponentsInChildren<Image>(true))
                if (img.name == "GhostHudIcon") { _ghostHud = img.sprite; break; }
            return _ghostHud;
        }

        // ─────────────────────────────────────────
        // 메인 사망 — F_death
        // ─────────────────────────────────────────

        private sealed class DeathRow
        {
            public string FaceKey;
            public string Letter;
            public string Title;
            public int Cost;
            public bool Enabled;
            public bool Best;
            public UnityAction Act;
        }

        /// <summary>LoseHost 끝에서 부른다. 유령 HP 가 남았을 때만 고른다.</summary>
        private void OpenPartyDeath(string lostKey, Vector2 pos)
        {
            if (_ghostHp <= 0) return;
            var near = NearestHostEnemy(pos);
            var rows = new List<DeathRow>
            {
                new()
                {
                    FaceKey = near != null ? near.Key : null, Title = Tx("ui.party.p2.death_new"), Cost = PartyRule.CostDeathNew,
                    Enabled = _ghostHp > PartyRule.CostDeathNew, Act = () => _partyPossessCost = PartyRule.CostDeathNew,
                },
            };
            for (int i = 0; i < _subs.Count; i++)
            {
                int slot = i;
                rows.Add(new DeathRow
                {
                    FaceKey = _subs[i].U.Key, Title = Tf("ui.party.p2.death_promote", i + 1, HostName(_subs[i].U.Key)),
                    Cost = PartyRule.CostDeathPromote, Enabled = _ghostHp > PartyRule.CostDeathPromote,
                    Act = () => PartyPromote(slot, pos),
                });
            }
            rows.Add(new DeathRow
            {
                FaceKey = lostKey, Title = Tf("ui.party.p2.death_revive", HostName(lostKey)), Cost = PartyRule.CostDeathRevive,
                Enabled = _ghostHp > PartyRule.CostDeathRevive, Act = () => PartyRevive(lostKey, pos),
            });
            bool any = false;
            DeathRow best = null;
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].Letter = ((char)('A' + i)).ToString();
                any |= rows[i].Enabled;
                if (rows[i].Enabled && (best == null || rows[i].Cost < best.Cost)) best = rows[i];
            }
            if (!any) return;   // 고를 길이 없다 — 유령으로 남고 다음 틱 규칙대로
            if (best != null) best.Best = true;   // 시안: 가장 싼 길(보조를 메인으로)에 「추천」
            BuildDeath(rows, PartyRule.DeathChoiceSeconds);
        }

        private Unit NearestHostEnemy(Vector2 from)
        {
            Unit best = null;
            float bd = float.MaxValue;
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e == null || !e.IsAlive || !e.IsHostBody) continue;
                float d = (e.Position - from).sqrMagnitude;
                if (d < bd) { bd = d; best = e; }
            }
            return best;
        }

        private void BuildDeath(List<DeathRow> rows, float seconds)
        {
            // 시안은 판도 딤도 거의 없다 — 쓰러진 자리 · 유령이 보이게 옅게만
            var board = PopupBoard(0.30f, false);
            if (board == null) return;
            var splat = MImg("Splat", board, BoardBox, new MBox(212, 214, 518, 322), "party_death_splat");
            splat.preserveAspect = true;
            var title = MTxt(board, BoardBox, new MBox(222, 222, 508, 282), Tx("ui.party.p2.death_title"), 52, PopRed,
                             TextAlignmentOptions.Center);
            title.outlineWidth = 0.18f;
            title.outlineColor = new Color32(255, 230, 230, 255);
            MTxt(board, BoardBox, new MBox(222, 284, 508, 318), Tf("ui.party.p2.death_sub", Mathf.RoundToInt(seconds)), 26,
                 Color.white, TextAlignmentOptions.Center);

            var ringBox = new MBox(298, 320, 422, 444);
            MImg("TimerRing", board, BoardBox, ringBox, "party_timer_ring");
            _partyTimerFill = MImg("TimerFill", board, BoardBox, new MBox(306, 328, 414, 436), "party_timer_ring");
            _partyTimerFill.type = Image.Type.Filled;
            _partyTimerFill.fillMethod = Image.FillMethod.Radial360;
            _partyTimerFill.fillOrigin = (int)Image.Origin360.Top;
            _partyTimerFill.color = new Color(0.37f, 0.85f, 1f, 0.9f);
            _partyTimerText = MTxt(board, BoardBox, ringBox, "", 34, Color.white, TextAlignmentOptions.Center);
            _partyTimerTotal = seconds;

            const float top = 514f, pitch = 88f, h = 82f;
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                float y = top + pitch * i;
                var rb = new MBox(42, y, 680, y + h);
                var bg = MImg($"Option{i + 1}", board, BoardBox, rb, !r.Enabled ? "party_row_locked" : r.Best ? "party_row_gold" : "party_row", true);
                if (bg.sprite == null && !r.Enabled) { bg.sprite = GetSprite("party_row_off"); bg.type = Image.Type.Sliced; bg.color = Color.white; }
                bg.raycastTarget = true;
                var row = bg.rectTransform;
                var btn = row.gameObject.AddComponent<Button>();
                btn.interactable = r.Enabled;
                var act = r.Act;
                btn.onClick.AddListener(() => { ClosePartyPopup(); act?.Invoke(); });

                if (r.Best)
                {
                    MImg("BestHex", row, rb, new MBox(8, y - 4, 62, y + 86), "party_best_hex");
                    MTxt(row, rb, new MBox(8, y + 46, 62, y + 76), Tx("ui.party.p2.best"), 16, PopGold, TextAlignmentOptions.Center);
                }
                var fb = new MBox(58, y + 8, 136, y + 74);
                string frame = !r.Enabled ? "party_box_gray" : r.Best ? "syn_member_gold" : "party_box_cyan";
                if (r.FaceKey != null) MFace(row, rb, fb, frame, r.FaceKey, 6f, r.Enabled ? Color.white : PopDim);
                else
                {
                    MImg("Frame", row, rb, fb, frame);
                    var gi = MImg("Ghost", row, rb, new MBox(74, y + 18, 120, y + 64), "party_ghost_mini");
                    gi.preserveAspect = true;
                }
                Color letter = !r.Enabled ? PopDim : r.Best ? PopGold : PopCyan;
                string lc = ColorUtility.ToHtmlStringRGB(letter);
                MTxt(row, rb, new MBox(148, y + 16, 412, y + 66), $"<color=#{lc}>{r.Letter}.</color> {r.Title}", 25,
                     r.Enabled ? Color.white : PopGray);

                var gm = MImg("GhostMini", row, rb, new MBox(414, y + 25, 444, y + 57), "party_ghost_mini");
                gm.preserveAspect = true;
                MTxt(row, rb, new MBox(446, y + 16, 520, y + 66), $"−{r.Cost}", 32, PopRed);
                int left = _ghostHp - r.Cost;
                if (!r.Enabled)
                {
                    // 시안: 사슬이 오른쪽 위 모서리에서 내려와 아래 테를 따라 왼쪽으로 · 자물쇠는 사슬 왼쪽 끝(아래 테 위)
                    MImg("Chain", row, rb, new MBox(478, y - 8, 690, y + 88), "party_chain");
                    var lk = MImg("Lock", row, rb, new MBox(500, y + 52, 530, y + 86), "party_lock");
                    if (lk.sprite == null) lk.enabled = false;   // 받기 전에는 줄 그림 자물쇠를 쓴다
                    MTxt(row, rb, new MBox(536, y + 6, 656, y + 36), Tx("ui.party.p2.hp_short"), 20, PopRed, TextAlignmentOptions.MidlineRight);
                    MTxt(row, rb, new MBox(536, y + 38, 656, y + 72),
                         $"<size=60%>{Tx("ui.party.p2.hp_after")}</size> 0", 30, PopGray, TextAlignmentOptions.MidlineRight);
                }
                else
                {
                    MTxt(row, rb, new MBox(522, y + 16, 662, y + 66),
                         $"<color=#5FD8FF>▶</color> <size=55%>{Tx("ui.party.p2.hp_after")}</size> <color=#5FD8FF>{left}</color>", 32,
                         Color.white, TextAlignmentOptions.MidlineRight);
                }
            }
            _partyTimer = seconds;
            _partyTimerText.text = Tf("ui.party.p2.seconds", seconds);
            FinishPopup();
        }

        // ─────────────────────────────────────────
        // 시너지 설명 — F_syndetail (누른 칸 옆 말풍선)
        // ─────────────────────────────────────────

        /// <summary>왼쪽 칸을 눌렀다 — tag 가 null 이면 인연 칸.</summary>
        public void PartyOpenSynergyDetail(string tag, RectTransform cell)
        {
            if (!PartyOn || _partyHold) return;
            var board = PopupBoard(0.01f, true);
            if (board == null) return;
            // 누른 칸 높이에 꼬리를 맞춘다(시안: 꼬리 = 칸 가운데)
            float cellY = 218f;
            if (cell != null)
            {
                var local = board.InverseTransformPoint(cell.TransformPoint(cell.rect.center));
                cellY = 640f - local.y;
            }
            bool inyeon = tag == null;
            var members = new List<string>();
            if (inyeon)
            {
                string mk = _inyeonKey ?? (_host != null ? _host.Key : null);
                if (mk == null) { ClosePartyPopup(); return; }
                members.Add(mk);
                foreach (var p in InyeonPartners(mk)) members.Add(p);
            }
            else if (_player != null)
            {
                foreach (var h in _player.AllHosts)
                    if (h != null && !h.IsGhost && !Game.User.PlayerDataService.IsHiddenHost(h.HostKey) && OrgOf(h.HostKey) == tag)
                        members.Add(h.HostKey);
                // 파티에 있는 몸을 앞으로(메인 → 보조 1 → 2)
                members.Sort((a, b) => PartyRank(a).CompareTo(PartyRank(b)));
            }

            int memberRows = Mathf.Max(1, (members.Count + 4) / 5);
            float tierH = inyeon ? 0f : 94f;
            float top = Mathf.Clamp(cellY - 58f, 160f, 900f);
            float bottom = top + 88f + tierH + 104f * memberRows + 52f;
            var pb = new MBox(120, top, 548, bottom);
            var pop = MImg("Popover", board, BoardBox, pb, "syn_pop_panel", true).rectTransform;
            pop.GetComponent<Image>().raycastTarget = true;   // 판 안을 눌러도 닫힌다(바닥 버튼)
            // 꼬리는 판 그림 왼쪽 위에 붙어 있다(9분할 위 테 안 — 판 위에서 56 아래) · 판 위를 칸 가운데 − 58 에 둬서 칸을 가리킨다

            // 머리 — 문장(인연이면 메인 얼굴) · 이름 · 설명
            var iconBox = new MBox(134, top + 18, 196, top + 80);
            if (inyeon) MFace(pop, pb, iconBox, "syn_cell_on", members[0], 9f, Color.white);
            else
            {
                MImg("Glow", pop, pb, new MBox(126, top + 10, 204, top + 88), "syn_icon_glow");
                MImg("Icon", pop, pb, iconBox, SynIcon(tag)).preserveAspect = true;
            }
            string title = inyeon ? Tf("ui.party.inyeon.title", InyeonName(members[0])) : SynShortName(tag);
            string desc = inyeon ? InyeonDesc(members[0]) : Tx($"ui.party.tag.{TagId(tag)}.desc");
            MTxt(pop, pb, new MBox(206, top + 18, 536, top + 54), title, 30, Color.white);
            MTxt(pop, pb, new MBox(206, top + 54, 536, top + 84), desc, 18, PopGray, TextAlignmentOptions.MidlineLeft, true);

            float y = top + 92f;
            if (!inyeon)
            {
                _probeKeys.Clear();
                foreach (var k in Unit.PartyKeys) _probeKeys.Add(k);
                CountOrgs(_probeKeys, _probeBefore);
                _probeBefore.TryGetValue(tag, out int have);
                for (int t = 0; t < 2; t++)
                {
                    int need = t + 2;
                    bool on = have >= need;
                    bool next = !on && (t == 0 || have >= 2);
                    var tb = new MBox(134, y, 536, y + 40);
                    MImg("Tier", pop, pb, tb, on ? "syn_tier_on" : "syn_tier_off", true);
                    string eff = Tf($"ui.party.p2.eff.{TagId(tag)}", SynTierPct(tag, need >= 3));
                    MTxt(pop, pb, new MBox(148, y, next ? 430 : 526, y + 40), Tf("ui.party.p2.tier", need, eff), 24, on ? PopGold : PopGray);
                    if (next)
                    {
                        MImg("More", pop, pb, new MBox(442, y + 6, 526, y + 34), "syn_more_chip", true);
                        MTxt(pop, pb, new MBox(442, y + 6, 526, y + 34), Tf("ui.party.p2.more", need - have), 17, Color.white,
                             TextAlignmentOptions.Center);
                    }
                    y += 47f;
                }
            }

            // 무리 몸 칸 — 메인 금 · 보조 보라 · 아직 없음 회색(얼굴 어둡게)
            for (int i = 0; i < members.Count; i++)
            {
                string k = members[i];
                float x = 134f + (i % 5) * 81f;
                float my = y + 8f + (i / 5) * 104f;
                var mb = new MBox(x, my, x + 72, my + 72);
                int rank = PartyRank(k);
                string frame = rank == 0 ? "syn_member_gold" : rank <= 2 ? "syn_member_purple" : "syn_member_gray";
                MFace(pop, pb, mb, frame, k, 6f, rank <= 2 ? Color.white : new Color(0.45f, 0.47f, 0.55f));
                if (rank <= 2)
                {
                    var bb = new MBox(x + 52, my - 6, x + 76, my + 18);
                    MImg("Badge", pop, pb, bb, "syn_member_badge");
                    MTxt(pop, pb, bb, rank == 0 ? "M" : rank.ToString(), 15, PopGold, TextAlignmentOptions.Center);
                }
                MTxt(pop, pb, new MBox(x - 6, my + 74, x + 78, my + 98), HostName(k), 16, rank <= 2 ? Color.white : PopGray,
                     TextAlignmentOptions.Center);
            }
            float fy = bottom - 44f;
            MImg("Line", pop, pb, new MBox(134, fy, 536, fy + 4), "party_divider_h", true);
            MTxt(pop, pb, new MBox(134, fy + 8, 536, fy + 36), Tx(inyeon ? "ui.party.p2.inyeon_rule" : "ui.party.p2.syn_rule"), 15,
                 PopGray, TextAlignmentOptions.Center);
            FinishPopup();
        }

        /// <summary>파티 안 순서 — 메인 0 · 보조 1 · 2 · 그 밖 9.</summary>
        private int PartyRank(string key)
        {
            if (_host != null && _host.Key == key) return 0;
            for (int i = 0; i < _subs.Count; i++) if (_subs[i].U != null && _subs[i].U.Key == key) return i + 1;
            return 9;
        }

        /// <summary>무리 시너지 세기(%) — 2명 = 약 · 3명 = 강. 실제로 거는 값(`CountSynergy`)과 같은 표.</summary>
        private static int SynTierPct(string tag, bool strong) => tag switch
        {
            "갱단" => strong ? 15 : 10,
            "코만도 부대" => strong ? 30 : 15,
            "무도가" => strong ? 16 : 8,
            "괴물" => strong ? 4 : 2,
            "마도" => strong ? 30 : 15,
            _ => 0,
        };
    }
}
