using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.InGame
{
    /// <summary>
    /// 스킬 연출 퀄업 — 파워 · 마법 12종 (2026-10-07). PD 가 시안 13종을 한 번에 통과시켰다.
    /// 시안 `Projects/AVSR/_exchange/in/mock_skill_{몸 키}_v1.png` (1 발동 → 2 최대 → 3 끝).
    /// 부품은 시안을 앵커로 발주한 `in/skill_parts_{몸 키}.png` → `fx_{부품}_{n}`(칸 256, `Tools/cut_skill_parts.py`).
    ///
    /// PD 기준(반려 이력): 화면 전체 물들이기 금지 · 원작 자세 그대로 · 궤적 안내 없이 「그냥 날아가면」 ·
    /// 여운은 짧게 · 너무 크지 않게 · 별 · 표창 모양 금지. 시전 때 화면 섬광은 끈다(`QualityFxHosts`).
    ///
    ///   미사일 다중 유도  — 모여 꽂힌 자리마다 도트 폭발(피격과 같은 그림, `PwExplode`)
    ///   구루 수호 결계    — 발밑 황금 원 → 몸을 감싼 결계 막(0.7초) → 결계 동안 얇은 테두리
    ///   닌자(사슬) 결박   — 적마다 사슬이 뻗고 감기는 섬광 · 조임 → 묶인 동안 몸에 고리
    ///   로봇 포탑 전개    — 포탑 자리에 흙먼지 · 떨림, 포탑 탄은 도트 폭발
    ///   슬러거 전탄 반사  — 몸 둘레 반사 원(지속), 되받아칠 때 둥근 타격 섬광
    ///   샐러맨더 독 뿜기  — 입 앞에 모이는 빛 → 둘레로 퍼지는 독 안개 → 중독된 적 머리 위 독 방울
    ///   드라군 화염 지대  — 불바다가 확 피어나고 낮은 불꽃으로 남는다
    ///   청룡 뇌전 폭주    — 몸에 감기는 전기, 적에서 적으로 튕기는 청백 번개 · 맞은 자리 잔광
    ///   화이트 위저드     — 지팡이 앞에 모여 수축하는 빛, 광탄이 맞은 자리에 빛 조각
    ///   영매 골렘 소환    — 바닥에 그려지는 룬 원, 골렘이 솟을 때 돌 파편 · 보라 빛
    ///   설녀 얼음 감옥    — 모이는 서리 → 얼음 결정 속(지속) → 깨져 흩어짐
    ///   흡혈귀 혈연       — 발밑 붉은 원, 적에게서 피 구슬이 빨려 들어오고 몸에 붉은 빛
    ///   사신의 시간       — 낫 한 번(보라 호) · 발밑 보라 기운(지속), 즉사한 적 위에 해골 표식
    /// 부품이 없으면 그 자리는 예전 그림으로 돌아간다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        /// <summary>부품 칸 256 = 화면 256 px(시안 크기 그대로 그려 왔다).</summary>
        private const float QxCell = 256f;
        /// <summary>몸 중심에서 발밑까지 — 바닥 원은 발밑에 깐다.</summary>
        private const float QxFootDrop = 34f;
        private const float QxHeadLift = 64f;

        private readonly Dictionary<(string, int, int), Sprite[]> _qxRange = new();
        private Impact _gdDomeFx, _dsCoilFx, _vmCircleFx, _rpAuraFx;

        private bool Qx(string name) => FxFrames(name) != null;

        private static Vector2 QxFeet(Vector2 at) => at + Vector2.down * QxFootDrop;

        /// <summary><paramref name="first"/>~<paramref name="last"/> 장만(1부터). 지속 표시가 나타남 · 사라짐 장을 빼고 돌 때.</summary>
        private Sprite[] QxFrames(string name, int first, int last)
        {
            var key = (name, first, last);
            if (_qxRange.TryGetValue(key, out var cached)) return cached;
            var all = FxFrames(name);
            Sprite[] part = null;
            if (all != null && first >= 1 && last <= all.Length && first <= last)
            {
                part = new Sprite[last - first + 1];
                for (int i = 0; i < part.Length; i++) part[i] = all[first - 1 + i];
            }
            _qxRange[key] = part;
            return part;
        }

        /// <summary>한 번 넘기고 끝나는 부품.</summary>
        private Impact QxOnce(string name, Vector2 at, float frameSeconds, float scale = 1f)
        {
            var im = PlayFx(name, at, QxCell * scale, loop: false);
            im?.SetFrameSeconds(frameSeconds);
            return im;
        }

        /// <summary>도는 부품. <paramref name="last"/> 가 0 이면 전부. <paramref name="life"/> 가 있으면 그 뒤 흐려지며 꺼진다.</summary>
        private Impact QxLoop(string name, Vector2 at, float frameSeconds, float scale = 1f,
                              int first = 1, int last = 0, float life = 0f)
        {
            var frames = last > 0 ? QxFrames(name, first, last) : FxFrames(name);
            if (frames == null) return null;
            float size = QxCell * scale;
            var im = FreeImpact(size);
            if (im == null) return null;
            im.Play(at, frames, size, loop: true);
            im.SetFrameSeconds(frameSeconds);
            if (life > 0f) im.SetLife(life, Mathf.Min(0.25f, life * 0.5f));
            return im;
        }

        /// <summary>몸을 따라다니는 부품 — 꺼졌으면 비운다.</summary>
        private void QxFollow(ref Impact fx, Vector2 at)
        {
            if (fx == null) return;
            if (!fx.IsActive) { fx = null; return; }
            fx.MoveTo(at);
        }

        private void TickQx()
        {
            if (_host == null) return;
            QxFollow(ref _gdDomeFx, _host.Position);
            QxFollow(ref _dsCoilFx, _host.Position);
            QxFollow(ref _vmCircleFx, QxFeet(_host.Position));
            QxFollow(ref _rpAuraFx, QxFeet(_host.Position));
        }

        private void ClearQx()
        {
            _gdDomeFx?.Stop(); _gdDomeFx = null;
            _dsCoilFx?.Stop(); _dsCoilFx = null;
            _vmCircleFx?.Stop(); _vmCircleFx = null;
            _rpAuraFx?.Stop(); _rpAuraFx = null;
        }

        // ── 구루 · 수호 결계 ───────────────────────────────────
        private const float GuardScale = 0.8f;

        /// <summary>결계를 연다 — 결계 동안 도는 테두리를 돌려준다(`_wardFx`). 부품이 없으면 null.</summary>
        private Impact QxGuardOpen(Unit me)
        {
            if (!Qx("gdring") || !Qx("gddome") || !Qx("gdrim")) return null;
            // 시안보다 커서(몸의 약 세 배) 0.8 배로(인게임 확인 2026-10-07)
            QxOnce("gdring", QxFeet(me.Position), 0.06f, GuardScale);
            _gdDomeFx?.Stop();
            _gdDomeFx = QxLoop("gddome", me.Position, 0.16f, GuardScale, life: 0.7f);
            return QxLoop("gdrim", me.Position, 0.2f, GuardScale);
        }

        // ── 닌자(사슬) · 사슬 결박 ─────────────────────────────
        private bool QxChainCast(Unit me, Unit e)
        {
            if (!Qx("cbchain") || !Qx("cbwrap")) return false;
            var beam = FreeImpact(QxCell);
            beam?.PlayBeam(me.Position, e.Position, FxFrames("cbchain"), QxCell);
            beam?.SetFrameSeconds(0.28f);
            QxOnce("cbsnap", e.Position, 0.05f, 0.7f);
            QxOnce("cbwrap", e.Position, 0.08f, 0.9f);
            return true;
        }

        /// <summary>묶인 적의 표시 — 사슬 고리(몸에) · 없으면 예전 발밑 사슬.</summary>
        private string RootFxName => Qx("cbhold") ? "cbhold" : "chain";
        private float RootFxSizeQ => Qx("cbhold") ? QxCell * 0.8f : RootFxSize;
        private float RootFxLiftQ => Qx("cbhold") ? 0f : RootFxLift;

        // ── 독 표시 ─────────────────────────────────────────────
        private string PoisonFxName => Qx("vndrip") ? "vndrip" : "venom";
        private float PoisonFxSizeQ => Qx("vndrip") ? QxCell * 0.7f : PoisonFxSize;
        private Vector2 PoisonFxAt(Unit u) => Qx("vndrip") ? u.Position + Vector2.up * QxHeadLift : u.Position;

        // ── 샐러맨더 · 독 뿜기 ─────────────────────────────────
        private void QxVenomCast(Unit me)
        {
            if (!Qx("vncloud")) return;
            QxOnce("vngather", me.MuzzlePosition, 0.06f, 0.6f);
            QxOnce("vncloud", QxFeet(me.Position), 0.1f, 1.4f);
        }

        // ── 로봇 · 포탑 전개 ───────────────────────────────────
        private void QxTurretDrop(Vector2 at)
        {
            _pfx?.Puff(QxFeet(at), ParticleElement.Dust, 0.6f);
            Shake(2.5f);
        }

        // ── 슬러거 · 전탄 반사 ─────────────────────────────────
        private Impact QxReflectAura(Unit me)
            => Qx("rfring") ? QxLoop("rfring", me.Position, 0.14f, 1f, 2, 3) : null;

        private bool QxReflectSwing(Vector2 at)
        {
            if (!Qx("rfswing")) return false;
            QxOnce("rfswing", at, 0.04f, 0.7f);
            return true;
        }

        // ── 드라군 · 화염 지대 ─────────────────────────────────
        private string FireFieldArt => Qx("ffburn") ? "ffburn" : "firefield";

        private void QxFireBloom(Vector2 at, float radius)
        {
            if (!Qx("ffbloom")) return;
            // 상자를 장판 지름에 맞춘다 — 칸 안 그림 크기(160 px)로 키웠더니 화면 위쪽을 다 덮었다(인게임 확인 2026-10-07)
            QxOnce("ffbloom", at, 0.06f, radius * 2f / QxCell);
        }

        // ── 청룡 · 뇌전 폭주 ───────────────────────────────────
        private bool DsReady => _host != null && _host.Key == "dragon_blue" && Qx("dsbolt");

        private void QxSurgeOpen(Unit me, float seconds)
        {
            if (!Qx("dscoil")) return;
            _dsCoilFx?.Stop();
            _dsCoilFx = QxLoop("dscoil", me.Position, 0.1f, 1f, life: seconds);
        }

        private bool QxSurgeBolt(Vector2 from, Vector2 to)
        {
            if (!DsReady) return false;
            var im = FreeImpact(QxCell);
            im?.PlayBeam(from, to, FxFrames("dsbolt"), QxCell * 0.9f);
            im?.SetFrameSeconds(0.06f);
            return true;
        }

        private bool QxSurgeSpark(Vector2 at)
        {
            if (!DsReady || !Qx("dsspark")) return false;
            QxOnce("dsspark", at, 0.06f, 0.8f);
            return true;
        }

        // ── 화이트 위저드 · 광휘 확산 ──────────────────────────
        private bool QxWizardGather(Unit me)
        {
            if (!Qx("wfgather")) return false;
            QxOnce("wfgather", me.MuzzlePosition, 0.05f, 0.7f);
            return true;
        }

        /// <summary>광탄(lightorb)이 맞은 자리 — 빛 조각. 다뤘으면 true(도트 폭발 대신).</summary>
        private bool QxOrbPop(string kind, Vector2 at)
        {
            if (kind != "lightorb" || !Qx("wfpop")) return false;
            QxOnce("wfpop", at, 0.05f, 0.6f);
            return true;
        }

        // ── 영매 · 골렘 소환 ───────────────────────────────────
        private void QxGolemRune(Vector2 at)
        {
            if (!Qx("mgrune")) return;
            QxOnce("mgrune", QxFeet(at), 0.07f);
        }

        private bool QxGolemRise(Vector2 at)
        {
            if (!Qx("mgrise")) return false;
            QxOnce("mgrise", QxFeet(at), 0.07f);
            return true;
        }

        // ── 설녀 · 얼음 감옥 ───────────────────────────────────
        private Impact QxIceOpen(Unit me)
        {
            if (!Qx("isprison")) return null;
            QxOnce("isgather", me.Position, 0.06f, 0.8f);
            var shell = QxLoop("isprison", me.Position, 0.22f);
            // 납품 결정이 꽤 불투명하다 — 시안처럼 안의 설녀가 비치게 조금 옅게
            shell?.SetTint(new Color(1f, 1f, 1f, 0.72f));
            return shell;
        }

        private void QxIceBreak(Vector2 at)
        {
            if (Qx("isbreak")) QxOnce("isbreak", at, 0.06f);
        }

        // ── 흡혈귀 · 혈연 ───────────────────────────────────────
        private Sprite[] BatFrames => Qx("vmorb") ? FxFrames("vmorb") : FxFrames("bat");
        // 납품 피 구슬이 칸의 약 16 px 로 작다 — 시안(약 24 px)에 맞게 키운다
        private float BatFxSizeQ => Qx("vmorb") ? QxCell * 1.5f : BatFxSize;

        private void QxFeastOpen(Unit me, float seconds)
        {
            if (!Qx("vmcircle")) return;
            _vmCircleFx?.Stop();
            _vmCircleFx = QxLoop("vmcircle", QxFeet(me.Position), 0.1f, life: seconds + 0.2f);
        }

        private bool QxFeastGlow(Vector2 at)
        {
            if (!Qx("vmglow")) return false;
            QxOnce("vmglow", at, 0.1f, 0.9f);
            return true;
        }

        // ── 사신 · 사신의 시간 ─────────────────────────────────
        private bool QxReaperOpen(Unit me, float seconds)
        {
            if (!Qx("rpslash") || !Qx("rpaura")) return false;
            QxOnce("rpslash", me.Position, 0.06f);
            _rpAuraFx?.Stop();
            _rpAuraFx = QxLoop("rpaura", QxFeet(me.Position), 0.1f, life: seconds);
            return true;
        }

        private bool QxReaperKill(Unit victim)
        {
            if (!Qx("rpskull")) return false;
            QxOnce("rpslash", victim.Position, 0.05f, 0.6f);
            var mark = QxLoop("rpskull", victim.Position + Vector2.up * QxHeadLift, 0.2f, 0.6f, life: 0.6f);
            return mark != null;
        }
    }
}
