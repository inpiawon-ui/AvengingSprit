using System.Collections.Generic;
using Game.Character;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 드릴 두더지 — 3챕터의 새 잡몹(무기 · 견제). 기획 `AVSR_Content_EnemyGimmick.md` 3-2 「잠복 미사일」 · 4절(라인업 2차 4번).
    /// PD 2026-10-07 「궁수의 전설처럼 땅속에 숨었다가 1~2초 있다가 갑자기 생겨나서 미사일 쏘는 패턴」.
    ///
    /// **평타가 없다.** 한 바퀴 :
    ///   0 땅속 이동 — 몸은 안 보이고 흙더미만 다닌다. **맞지 않는다.** 내 둘레 2.6 ~ 4.6 m 의 한 점으로 파고든다
    ///   1 들썩 — 솟을 자리에 흙이 들썩인다(0.45초 예고). 아직 숨어 있다
    ///   2 솟아 겨눔 — 몸이 나온다(0.5초). 이때부터 맞는다
    ///   3 발사 — 등 미사일 셋을 부채꼴로 쏘고 **1초 그 자리에 선다** — 근접이 붙어 치는 틈(PD 「근거리도 감안해서」)
    ///   4 다시 파고든다 → 0
    /// 솟는 자리를 근접이 닿을 거리(2.6 m~)에도 두므로, 붙어 싸우는 몸은 들썩이는 흙을 보고 그리로 달려가면 된다.
    /// 원거리 몸은 솟은 1.5초 안에 딴다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const string TrashMoleKey = "mole";
        private static HostEntry s_mole;

        private static HostEntry Mole => s_mole ??= HostEntry.CreateTrash(
            TrashMoleKey, "드릴 두더지", AttackKind.Single,
            hp: 28, atk: 8, moveMps: 1.6f, engageMps: 2.4f,
            rangeMeters: 6.0f, interval: 2.2f, telegraph: 0.6f);

        private const float MoleDigMps = 3.2f;          // 땅속에서 다니는 빠르기
        private const float MoleDigMaxSeconds = 1.6f;   // 이만큼 파고들면 그 자리에서 솟는다
        private const float MoleNearMeters = 2.6f;      // 솟는 자리 — 내게서 이만큼 ~
        private const float MoleFarMeters = 4.6f;       //               ~ 이만큼(근접이 닿을 거리도 섞인다)
        private const float MoleBulgeSeconds = 0.45f;
        private const float MoleAimSeconds = 0.5f;
        private const float MoleStandSeconds = 1.0f;
        private const float MoleFanDegrees = 16f;       // 미사일 셋 사이 각도
        private const float MoleMoundMeters = 1.2f;

        private readonly Dictionary<Unit, Image> _moleMounds = new();

        /// <summary>두더지 한 프레임. 늘 true — 다른 패턴으로 내려가지 않는다(`TickPatterns3` 가 부른다).</summary>
        private bool TickMole(Unit e, Unit me, float distance, float dt)
        {
            e.PatternTimer -= dt;
            switch (e.PatternPhase)
            {
                case 0:   // 땅속 이동
                {
                    if (!e.IsHidden) { e.SetHidden(true); e.PatternTimer = MoleDigMaxSeconds; e.PatternTo = MoleSurfaceSpot(e, me); }
                    ShowMound(e, true);
                    e.SetMoving(true);
                    e.SetState(EnemyState.Approach);
                    var to = e.PatternTo - e.Position;
                    float step = Meters(MoleDigMps) * dt;
                    bool arrived = to.magnitude <= step;
                    if (!arrived) e.Position = SlideMove(e, e.Position, to.normalized * step);
                    if (!arrived && e.PatternTimer > 0f) return true;
                    e.PatternPhase = 1;
                    e.PatternTimer = MoleBulgeSeconds;
                    PlayFx("bulge", e.Position, Meters(1.6f), loop: false);   // 솟을 자리가 들썩인다
                    return true;
                }

                case 1:   // 들썩 — 아직 숨어 있다
                    e.SetMoving(false);
                    if (e.PatternTimer > 0f) return true;
                    ShowMound(e, false);
                    e.SetHidden(false);
                    FaceAt(e, me);
                    e.SetTelegraph(true);
                    e.PlayAttack();
                    e.PatternPhase = 2;
                    e.PatternTimer = MoleAimSeconds;
                    return true;

                case 2:   // 솟아 겨눔
                    e.SetMoving(false);
                    e.SetState(EnemyState.Attack);
                    FaceAt(e, me);
                    if (e.PatternTimer > 0f) return true;
                    e.SetTelegraph(false);
                    if (_host != null && me != null)
                        for (int k = -1; k <= 1; k++) FireShot(e, me, fromPlayer: false, k * MoleFanDegrees, lastShot: k == 1);
                    e.PatternPhase = 3;
                    e.PatternTimer = MoleStandSeconds;
                    return true;

                case 3:   // 선 채로 숨 고르기 — 근접이 치는 틈
                    e.SetMoving(false);
                    e.SetState(EnemyState.Cooldown);
                    if (e.PatternTimer > 0f) return true;
                    PlayFx("burrow", e.Position, Meters(1.4f), loop: false);
                    e.PatternPhase = 0;   // 다음 프레임에 숨는다
                    return true;
            }
            e.PatternPhase = 0;
            return true;
        }

        private static void FaceAt(Unit e, Unit me)
        {
            if (me == null) return;
            var d = me.Position - e.Position;
            if (d.sqrMagnitude > 0.0001f) e.SetFacing(d.normalized);
        }

        /// <summary>솟을 자리 — 내 둘레 2.6 ~ 4.6 m 의 아무 점. 방 안으로 자른다.</summary>
        private Vector2 MoleSurfaceSpot(Unit e, Unit me)
        {
            if (me == null) return e.Position;
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float r = Meters(Random.Range(MoleNearMeters, MoleFarMeters));
            var p = me.Position + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
            float m = Meters(0.8f);
            p.x = Mathf.Clamp(p.x, m, _roomSize.x - m);
            p.y = Mathf.Clamp(p.y, -_roomSize.y + m, -m);
            return p;
        }

        /// <summary>땅속 흙더미 — 몸 그림이 꺼진 동안 그 자리에 깐다(그림 `unit_mole_s_under`).</summary>
        private void ShowMound(Unit e, bool on)
        {
            if (!_moleMounds.TryGetValue(e, out var im) || im == null)
            {
                if (!on || _unitLayer == null) return;
                var sprite = UnitGet(TrashMoleKey, "s_under");
                if (sprite == null) return;
                var go = new GameObject("MoleMound", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(_unitLayer, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);   // 방 좌표 그대로
                rt.pivot = new Vector2(0.5f, 0.2f);
                float s = Meters(MoleMoundMeters);
                rt.sizeDelta = new Vector2(s, s);
                im = go.GetComponent<Image>();
                im.sprite = sprite;
                im.raycastTarget = false;
                _moleMounds[e] = im;
            }
            if (im.gameObject.activeSelf != on) im.gameObject.SetActive(on);
            if (on) ((RectTransform)im.transform).anchoredPosition = new Vector2(e.Position.x, e.Position.y - FootDrop(e));
        }

        /// <summary>죽거나 방을 나간 두더지의 흙더미를 거둔다 — `TickWarns` 가 부른다.</summary>
        private void TickMoleMounds()
        {
            if (_moleMounds.Count == 0) return;
            foreach (var pair in _moleMounds)
            {
                var u = pair.Key;
                bool keep = u != null && u.IsAlive && u.IsHidden && _enemies.Contains(u);
                if (!keep && pair.Value != null && pair.Value.gameObject.activeSelf) pair.Value.gameObject.SetActive(false);
            }
        }

        private void ClearMoleMounds()
        {
            foreach (var pair in _moleMounds)
                if (pair.Value != null) Object.Destroy(pair.Value.gameObject);
            _moleMounds.Clear();
        }
    }
}
