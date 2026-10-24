using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// 레벨업 카드 검증 녹화(PD 2026-10-21 「카드별 녹화 진행해줘」) — 카드 한 장 = 영상 한 개.
    ///
    /// 실제 판(1챕터)에 들어가 적을 더 세우고, **레벨업 창에 그 카드를 띄워 고른 뒤** 최대 레벨까지 올려 싸우는 장면을 찍는다.
    /// 수치만 바뀌는 카드(공격 · 속사 · 가속 …)는 화면만으로는 안 읽히므로 고르기 전 · 후 `RunBuffs` 값을 로그(`[CARD]`)에 남긴다.
    /// 쓰기: `CardCheckRecorder.Batch("c014|commando_mg|;c035|thug|")` — 줄 = 카드|몸|조건(쉼표), 줄끼리 `;`
    /// 조건: solo(한 명 · 맷집 큼) · hit(무적 끔 · 맞으며 버팀) · low(체력 25%) · half(체력 50%) · move(좌우로 걷기)
    ///       active(쿨 될 때마다 액티브) · ult(고른 뒤 필살기) · ultfirst(필살기를 먼저 써 둠) · req=c009(앞 카드 먼저 최대로)
    /// </summary>
    public static class CardCheckRecorder
    {
        private const string QueueKey = "AVSR.CardRecQueue";
        private const string LastMark = "#LAST";
        private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

        private const float RecordSeconds = 9f;
        private const float OfferAt = 0.6f;     // 녹화 시작 뒤 레벨업 창을 띄우는 초
        private const float PickAt = 2.0f;      // 카드를 고르는 초 — 창에 무엇이 떴는지 읽힐 만큼
        private const int EnemyHpMul = 4;
        private const int SoloHpMul = 60;
        private const int MaxEnemies = 14;

        private static readonly Vector2[] Offsets =
        {
            new(-3f, 2.5f), new(3f, 3f), new(-2f, 4.5f), new(2.5f, 4f), new(0f, 3.5f),
            new(1.2f, 6.5f), new(-3.5f, 5.5f), new(3.5f, 1.2f),
        };

        public static void Batch(string spec)
        {
            SessionState.SetString(QueueKey, spec ?? string.Empty);
            if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
        }

        public static void CancelBatch() => SessionState.EraseString(QueueKey);

        [InitializeOnLoadMethod]
        private static void HookBatch()
        {
            EditorApplication.playModeStateChanged -= OnBatchMode;
            EditorApplication.playModeStateChanged += OnBatchMode;
            EditorApplication.update -= Watchdog;
            EditorApplication.update += Watchdog;
        }

        private static double s_idleSince = -1;

        /// <summary>
        /// 플레이를 끈 뒤 `delayCall` 로 다시 켜는 것이 가끔 안 불린다(2026-10-21 — 34장 중 감전 뒤에서 멈춤).
        /// 줄이 남았는데 플레이가 5초 넘게 꺼져 있으면 여기서 켠다.
        /// </summary>
        private static void Watchdog()
        {
            var q = SessionState.GetString(QueueKey, string.Empty);
            if (string.IsNullOrEmpty(q) || q == LastMark || EditorApplication.isPlaying
                || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            { s_idleSince = -1; return; }
            double now = EditorApplication.timeSinceStartup;
            if (s_idleSince < 0) { s_idleSince = now; return; }
            if (now - s_idleSince < 5) return;
            s_idleSince = -1;
            Debug.Log("[CARD] 다음 줄을 다시 켠다");
            EditorApplication.isPlaying = true;
        }

        private static void OnBatchMode(PlayModeStateChange state)
        {
            var q = SessionState.GetString(QueueKey, string.Empty);
            if (string.IsNullOrEmpty(q)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                int cut = q.IndexOf(';');
                string head = cut < 0 ? q : q.Substring(0, cut);
                string rest = cut < 0 ? string.Empty : q.Substring(cut + 1);
                SessionState.SetString(QueueKey, string.IsNullOrEmpty(rest) ? LastMark : rest);
                var f = head.Split('|');
                if (f.Length < 2 || f[0] == LastMark) { SessionState.EraseString(QueueKey); return; }
                Debug.Log("[CARD] batch " + f[0] + " (남은 줄 " + (string.IsNullOrEmpty(rest) ? 0 : rest.Split(';').Length) + ")");
                Run(f[0], f[1], f.Length > 2 ? f[2] : string.Empty);
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                if (q == LastMark) { SessionState.EraseString(QueueKey); Debug.Log("[CARD] batch done"); return; }
                EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
            }
        }

        public static void Run(string cardKey, string hostKey, string setup)
        {
            var flags = new HashSet<string>((setup ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            string req = null;
            foreach (var s in flags) if (s.StartsWith("req=")) req = s.Substring(4);
            bool Has(string s) => flags.Contains(s);

            int phase = 0, wait = 0;
            double t0 = EditorApplication.timeSinceStartup, recAt = 0;
            float battleAt = 0f;
            bool offered = false, clicked = false, picked = false, ulted = false;
            int casts = 0;
            string before = null;
            Vector2 moveStart = default;
            float moveClock = 0f;
            var bt = typeof(Game.Module.InGame.BattleDirector);
            EditorApplication.CallbackFunction cb = null;
            cb = () =>
            {
                try
                {
                    double now = EditorApplication.timeSinceStartup;
                    if (now - t0 > 200)
                    {
                        EditorApplication.update -= cb;
                        Debug.Log("[CARD] timeout " + cardKey + " phase " + phase);
                        if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
                        return;
                    }
                    if (!EditorApplication.isPlaying) return;
                    if (wait > 0) { wait--; return; }

                    if (phase == 0)
                    {
                        if (GameObject.Find("[LoadingView]") != null) { wait = 20; return; }
                        if (Click("SkipButton") || Click("TouchArea") || Click("StartButton")) { wait = 40; return; }
                        // 새 로비(RbLobbyUI)에서 바로 판을 연다 — 옛 로비 · 시나리오 창은 2026-10-24 Assets 밖으로 옮겼다
                        var lobby = UnityEngine.Object.FindAnyObjectByType<Game.Module.Lobby.RbLobbyUI>();
                        if (lobby == null || !lobby.IsReady) { wait = 15; return; }
                        if (!GameFramework.Core.Base.CoreModule.TryGet<Game.User.IPlayerDataService>(out var p) || !p.IsReady) { wait = 30; return; }
                        p.SetStamina(p.StaminaMax);   // 판마다 행동력이 빠진다 — 연달아 돌리면 바닥난다(시험 세이브는 끝나면 되돌린다)
                        lobby.StartRunDirect(1, hostKey);
                        phase = 1;
                        wait = 60;
                        return;
                    }

                    var bd = UnityEngine.Object.FindAnyObjectByType<Game.Module.InGame.BattleDirector>();
                    if (bd == null) { wait = 10; return; }
                    var host = (Game.Module.InGame.Unit)bt.GetField("_host", F).GetValue(bd);
                    if (!Has("hit")) bt.GetField("_invuln", F).SetValue(bd, 999f);
                    bt.GetField("_exp", F).SetValue(bd, -1000000);
                    bt.GetField("_stageLevelGiven", F)?.SetValue(bd, true);
                    var buffs = bt.GetField("_buffs", F).GetValue(bd);
                    var table = (Game.Character.BuffTable)bt.GetField("_buffTable", F).GetValue(bd);
                    if (table == null) { wait = 10; return; }   // 판이 막 열려 표를 아직 안 읽었다
                    var card = table.Get(cardKey);

                    if (phase == 1)
                    {
                        if (Click("SkipButton")) { wait = 20; return; }
                        if (battleAt == 0f) battleAt = Time.unscaledTime;
                        if (Time.unscaledTime - battleAt < 6f) return;
                        var enemies = (IList)bt.GetField("_enemies", F).GetValue(bd);
                        int alive = 0;
                        Game.Module.InGame.Unit melee = null, gun = null;
                        foreach (var o in enemies)
                        {
                            var u = o as Game.Module.InGame.Unit;
                            if (u == null || !u.IsAlive || u.IsBoss) continue;
                            alive++;
                            if (u.Profile == null) continue;
                            if (u.Profile.Kind == Game.Character.AttackKind.Melee) { if (melee == null || !u.IsHostBody) melee = u; }
                            else if (gun == null || !u.IsHostBody) gun = u;
                        }
                        if ((alive == 0 || host == null) && Time.unscaledTime - battleAt < 20f) return;
                        if (host == null || card == null)
                        {
                            Debug.LogError($"[CARD] {cardKey} 시작 실패 host={host != null} card={card != null}");
                            EditorApplication.update -= cb;
                            EditorApplication.isPlaying = false;
                            return;
                        }
                        float m = Meters(bd);
                        host.Position += new Vector2(0f, 3f * m);
                        var src = melee ?? gun;
                        var spawn = bt.GetMethod("SpawnRoomUnit", F);
                        for (int i = 0; i < Offsets.Length && src != null && !Has("solo"); i++)
                        {
                            var u = spawn.Invoke(bd, new object[] { src.Profile, false, false, host.Position + Offsets[i] * m, "CardTest" + i })
                                as Game.Module.InGame.Unit;
                            if (u != null) SetHp(u, u.HpMax * EnemyHpMul);
                        }
                        var all = new List<Game.Module.InGame.Unit>();
                        foreach (var o in enemies)
                        {
                            var u = o as Game.Module.InGame.Unit;
                            if (u != null && u.IsAlive && !u.IsBoss) all.Add(u);
                        }
                        var hp = host.Position;
                        all.Sort((x, y) => (x.Position - hp).sqrMagnitude.CompareTo((y.Position - hp).sqrMagnitude));
                        var kill = bt.GetMethod("KillEnemy", F);
                        int keep = Has("solo") ? 1 : MaxEnemies;
                        for (int i = keep; i < all.Count; i++) kill.Invoke(bd, new object[] { all[i] });
                        if (Has("solo") && all.Count > 0)
                        {
                            all[0].Position = hp + new Vector2(0f, 2.6f * m);
                            SetHp(all[0], all[0].HpMax * SoloHpMul);
                            // 보스 사냥 — 1챕터 첫 방엔 보스가 없어 이 한 명을 보스로 친다(피해 배율만 보는 녹화)
                            if (Has("boss")) typeof(Game.Module.InGame.Unit).GetProperty("IsBoss", F)?.SetValue(all[0], true);
                        }

                        // 몸 체력 — 맞으며 버텨야 하는 카드는 그릇을 크게 잡는다(죽으면 판이 끝난다)
                        if (Has("hit")) { bt.GetField("_invuln", F).SetValue(bd, 0f); SetHp(host, 3000); }
                        // 위기 방벽 문턱(30%) 아래 — 카드를 고르는 순간 방벽이 서는지 본다
                        if (Has("hit") && Has("low")) SetHpNow(host, Mathf.RoundToInt(host.HpMax * 0.27f));
                        else if (Has("low")) SetHpNow(host, Mathf.RoundToInt(host.HpMax * 0.25f));
                        else if (Has("half")) SetHpNow(host, Mathf.RoundToInt(host.HpMax * 0.5f));

                        // 앞 카드(테크트리) — 먼저 최대로
                        if (!string.IsNullOrEmpty(req))
                        {
                            var pre = table.Get(req);
                            if (pre != null) for (int i = 0; i < pre.MaxLevel; i++) Apply(buffs, pre);
                        }
                        if (Has("ultfirst"))
                        {
                            bt.GetField("_ultReady", F).SetValue(bd, true);
                            bt.GetMethod("TryUltimate", F).Invoke(bd, null);
                        }
                        before = Snapshot(buffs) + Extra(bd);
                        moveStart = host.Position;
                        SkillFxRecorder.RecordClip("card_" + cardKey, RecordSeconds);
                        recAt = now;
                        phase = 2;
                        return;
                    }

                    double t = now - recAt;
                    if (!offered && t > OfferAt)
                    {
                        offered = true;
                        // 레벨업 창에 이 카드를 가운데로 띄운다 — 양옆은 아무 카드나
                        var offer = (List<Game.Character.BuffEntry>)bt.GetField("_offer", F).GetValue(bd);
                        offer.Clear();
                        foreach (var e in table.Entries)
                            if (e != card && e.Implemented && offer.Count < 1) offer.Add(e);
                        offer.Add(card);
                        for (int i = table.Entries.Count - 1; i >= 0 && offer.Count < 3; i--)
                            if (table.Entries[i] != card && table.Entries[i].Implemented && !offer.Contains(table.Entries[i])) offer.Add(table.Entries[i]);
                        bt.GetField("_awaitingBuff", F).SetValue(bd, true);
                        var keys = new string[offer.Count];
                        for (int i = 0; i < offer.Count; i++) keys[i] = offer[i].BuffKey;
                        GameFramework.Core.Base.CoreModule.Get<GameFramework.Core.Module.EventBus.IEventBus>()
                            .Publish(new Game.Module.Events.BuffOfferEvent { OfferedKeys = keys, Level = 2 });
                    }
                    // 화면의 가운데 카드를 누른다 — 고르는 연출(카드가 몸으로 흡수)을 거쳐 ChooseBuff 가 불린다
                    if (offered && !clicked && t > PickAt)
                    {
                        clicked = true;
                        if (!Click("BuffCard1")) bt.GetMethod("ChooseBuff", F).Invoke(bd, new object[] { card.BuffKey });
                    }
                    if (clicked && !picked && (LevelOf(buffs, cardKey) > 0 || t > PickAt + 4f))
                    {
                        picked = true;
                        // 고른 것은 1레벨 — 효과가 눈에 보이게 최대 레벨까지 마저 올린다
                        for (int i = 0; i < card.MaxLevel && LevelOf(buffs, cardKey) < card.MaxLevel; i++) Apply(buffs, card);
                        string after = Snapshot(buffs) + Extra(bd);
                        Debug.Log($"[CARD] {cardKey} {card.DisplayName} host={hostKey} body={BodyOk(bd, card)} lv={LevelOf(buffs, cardKey)}"
                                  + $"\n  전: {before}\n  후: {Diff(before, after)}");
                        if (Has("boss"))
                        {
                            foreach (var o in (IList)bt.GetField("_enemies", F).GetValue(bd))
                                if (o is Game.Module.InGame.Unit u && u.IsAlive)
                                    Debug.Log($"[CARD] boss 확인 {u.name} IsBoss={u.IsBoss} 카드 피해 배율="
                                              + bt.GetMethod("CardDamageMul", F).Invoke(bd, new object[] { u }));
                        }
                        if (Has("ultuse"))
                            Debug.Log("[CARD] 고른 뒤 필살기 준비=" + bt.GetField("_ultReady", F).GetValue(bd));
                        moveStart = host != null ? host.Position : default;
                        moveClock = 0f;
                    }
                    if (picked && host != null)
                    {
                        if (Has("move"))
                        {
                            moveClock += Time.deltaTime;
                            bd.MoveInput = (int)(moveClock / 1.2f) % 2 == 0 ? Vector2.right : Vector2.left;
                        }
                        if (Has("active") && bd.SkillCooldownRatio >= 1f && t > PickAt + 0.6f)
                        {
                            bt.GetMethod("TryActiveSkill", F).Invoke(bd, null);
                            casts++;
                        }
                        if ((Has("ult") || Has("ultuse")) && !ulted && t > PickAt + 1.2f)
                        {
                            ulted = true;
                            // ultuse — 준비를 억지로 켜지 않는다(필살기 초기화 카드가 켰는지 보는 녹화)
                            if (Has("ult")) bt.GetField("_ultReady", F).SetValue(bd, true);
                            bt.GetMethod("TryUltimate", F).Invoke(bd, null);
                        }
                    }
                    if (t > RecordSeconds + 2.0)
                    {
                        EditorApplication.update -= cb;
                        bd.MoveInput = Vector2.zero;
                        Debug.Log($"[CARD] done {cardKey} casts={casts} hostHp={(host != null ? host.Hp + "/" + host.HpMax : "-")}");
                        EditorApplication.isPlaying = false;
                    }
                }
                catch (Exception e)
                {
                    EditorApplication.update -= cb;
                    Debug.LogError("[CARD] " + e);
                    if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
                }
            };
            EditorApplication.update += cb;
        }

        // ── 도우미 ──────────────────────────────────────────────

        private static float Meters(Game.Module.InGame.BattleDirector bd)
            => (float)typeof(Game.Module.InGame.BattleDirector).GetMethod("Meters", F).Invoke(bd, new object[] { 1f });

        private static void Apply(object buffs, Game.Character.BuffEntry e)
            => buffs.GetType().GetMethod("Apply", new[] { typeof(Game.Character.BuffEntry) }).Invoke(buffs, new object[] { e });

        private static int LevelOf(object buffs, string key)
            => (int)buffs.GetType().GetMethod("LevelOf", new[] { typeof(string) }).Invoke(buffs, new object[] { key });

        private static bool BodyOk(Game.Module.InGame.BattleDirector bd, Game.Character.BuffEntry card)
        {
            var host = (Game.Module.InGame.Unit)typeof(Game.Module.InGame.BattleDirector).GetField("_host", F).GetValue(bd);
            return ((Game.Character.ICardRule)bd).BodyMatches(card.Body, host != null ? host.Profile : null);
        }

        /// <summary>RunBuffs 의 공개 수치 · 참거짓 전부 — 고르기 전 · 후를 맞대어 무엇이 바뀌었는지 본다.</summary>
        private static string Snapshot(object buffs)
        {
            var sb = new StringBuilder();
            foreach (var p in buffs.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (p.GetIndexParameters().Length > 0) continue;
                var ty = p.PropertyType;
                if (ty != typeof(int) && ty != typeof(float) && ty != typeof(bool)) continue;
                object v;
                try { v = p.GetValue(buffs); } catch { continue; }
                sb.Append(p.Name).Append('=').Append(v is float fv ? fv.ToString("0.###") : v).Append(' ');
            }
            return sb.ToString();
        }

        private static string Extra(Game.Module.InGame.BattleDirector bd)
        {
            var bt = typeof(Game.Module.InGame.BattleDirector);
            var cd = bt.GetProperty("PlayerSkillCooldown", F);
            return cd != null ? "SkillCooldownSec=" + ((float)cd.GetValue(bd)).ToString("0.##") + " " : string.Empty;
        }

        private static string Diff(string before, string after)
        {
            var a = new Dictionary<string, string>();
            foreach (var kv in before.Split(' ')) { int i = kv.IndexOf('='); if (i > 0) a[kv.Substring(0, i)] = kv.Substring(i + 1); }
            var sb = new StringBuilder();
            foreach (var kv in after.Split(' '))
            {
                int i = kv.IndexOf('=');
                if (i <= 0) continue;
                string k = kv.Substring(0, i), v = kv.Substring(i + 1);
                if (!a.TryGetValue(k, out var old) || old != v) sb.Append(k).Append(' ').Append(old).Append("→").Append(v).Append("  ");
            }
            return sb.Length > 0 ? sb.ToString() : "(바뀐 수치 없음)";
        }

        private static void SetHp(Game.Module.InGame.Unit u, int hp)
        {
            var t = typeof(Game.Module.InGame.Unit);
            t.GetProperty("HpMax", F)?.SetValue(u, hp);
            t.GetProperty("Hp", F)?.SetValue(u, hp);
        }

        private static void SetHpNow(Game.Module.InGame.Unit u, int hp)
            => typeof(Game.Module.InGame.Unit).GetProperty("Hp", F)?.SetValue(u, hp);

        private static bool Click(string name)
        {
            var go = GameObject.Find(name);
            if (go == null) return false;
            var b = go.GetComponent<UnityEngine.UI.Button>();
            if (b == null || !b.isActiveAndEnabled) return false;
            b.onClick.Invoke();
            return true;
        }
    }
}
