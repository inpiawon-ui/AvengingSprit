using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Module.InGame
{
    /// <summary>
    /// 화염 분사구 불줄기 — **전용 연속 동작 그림** 한 장을 돌린다 (2026-10-07 PD 「불 이미지 대충 올려 놓은 느낌 — 퀄이 똥」).
    ///
    /// 그림 `fx_flamejet_01~14`(코덱스 — 2차 화면 시안이 통과한 원뿔 불) :
    ///   01~03 붙는다 — 켜지는 순간 한 번
    ///   04~11 뿜는다 — 켜져 있는 동안 되풀이
    ///   12~14 꺼진다 — 꺼진 뒤 한 번, 끝에 검붉은 연기
    /// 그림은 입이 왼쪽 가운데이고 오른쪽으로 뿜는다 — 분사구 입에 붙여 뿜는 쪽으로 돌린다. 길이는 불이 닿는 3 m 그대로.
    ///
    /// 예전 불 그림 석 장(`fx_breath_fire` — 위로 타오르는 모닥불)은 끈다. 파티클은 불티 · 연기만 조금(공용 파티클).
    /// 그림이 아직 없으면 예전 석 장이 그대로 보인다.
    /// </summary>
    public sealed partial class BattleDirector
    {
        private const float FlameFrameSeconds = 0.07f;
        private const int FlameIgniteFrames = 3, FlameLoopFrames = 8, FlameDieFrames = 3;
        private const float FlameEmberSeconds = 0.12f;

        private sealed class FlameJet
        {
            public Image Img;
            public bool On;
            public float Since;   // 켜진 뒤 · 꺼진 뒤 흐른 시간
            public float Ember;
        }

        private readonly Dictionary<Obstacle, FlameJet> _flameJets = new();
        private Sprite[] _flameJetFrames;
        private static readonly string[] FlameJetNames = BuildFlameJetNames();   // 매 프레임 이름을 조립하지 않게

        private static string[] BuildFlameJetNames()
        {
            var n = new string[FlameIgniteFrames + FlameLoopFrames + FlameDieFrames];
            for (int i = 0; i < n.Length; i++) n[i] = "fx_flamejet_" + (i + 1).ToString("00");
            return n;
        }

        private Sprite[] FlameJetFrames()
        {
            if (_flameJetFrames != null) return _flameJetFrames;
            var f = new Sprite[FlameIgniteFrames + FlameLoopFrames + FlameDieFrames];
            for (int i = 0; i < f.Length; i++)
            {
                f[i] = GetSprite(FlameJetNames[i]);
                if (f[i] == null) return null;   // 하나라도 없으면 예전 그림으로(다음에 다시 찾는다)
            }
            return _flameJetFrames = f;
        }

        /// <summary>한 프레임 — `TickFlame` 이 부른다.</summary>
        private void TickFlameFx(Obstacle o, bool on, float dt)
        {
            var frames = FlameJetFrames();
            if (frames == null || _unitLayer == null) return;

            // 예전 불 그림 석 장은 끈다
            if (o.Puffs != null)
                for (int k = 0; k < o.Puffs.Length; k++)
                    if (o.Puffs[k] != null && o.Puffs[k].enabled) o.Puffs[k].enabled = false;

            var dir = FlameDir(o);
            var nozzle = o.ShotBounds.center + dir * (o.ShotBounds.width * 0.5f);
            float len = Meters(FlameLengthMeters);

            if (!_flameJets.TryGetValue(o, out var jet))
            {
                var go = new GameObject("FlameJet", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(_unitLayer, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);   // 방 좌표 그대로
                rt.pivot = new Vector2(0f, 0.5f);                   // 입이 왼쪽 가운데
                var s0 = frames[0];
                float h = len * (s0.rect.height / s0.rect.width);
                rt.sizeDelta = new Vector2(len, h);
                rt.anchoredPosition = nozzle;
                rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                go.SetActive(false);
                jet = new FlameJet { Img = img };
                _flameJets[o] = jet;
            }

            if (on != jet.On)
            {
                jet.On = on;
                jet.Since = 0f;
                if (on && _pfx != null && IsOnScreenAt(nozzle)) _pfx.Hit(nozzle, ParticleElement.Fire, 0.7f);   // 확 붙는다
            }
            else jet.Since += dt;

            int idx;
            if (on)
            {
                int f = Mathf.FloorToInt(jet.Since / FlameFrameSeconds);
                idx = f < FlameIgniteFrames ? f : FlameIgniteFrames + (f - FlameIgniteFrames) % FlameLoopFrames;
            }
            else
            {
                int f = Mathf.FloorToInt(jet.Since / FlameFrameSeconds);
                idx = f < FlameDieFrames ? FlameIgniteFrames + FlameLoopFrames + f : -1;   // 다 꺼지면 숨긴다
            }

            bool show = idx >= 0;
            if (jet.Img.gameObject.activeSelf != show) jet.Img.gameObject.SetActive(show);
            if (!show) return;
            if (jet.Img.sprite != frames[idx]) jet.Img.sprite = frames[idx];
            jet.Img.transform.SetAsLastSibling();

            // 불티 — 켜진 동안 불줄기 끝 쪽에서 조금씩
            if (!on || _pfx == null || !IsOnScreenAt(nozzle)) return;
            jet.Ember -= dt;
            if (jet.Ember > 0f) return;
            jet.Ember = FlameEmberSeconds;
            _pfx.Embers(nozzle + dir * (len * Random.Range(0.55f, 0.95f)), ParticleElement.Fire, 1, Meters(0.3f));
        }

        /// <summary>방을 나갈 때 — `ClearWarns` 가 부른다. 불줄기 그림은 방 물건과 같이 지운다.</summary>
        private void ClearFlameFx()
        {
            foreach (var pair in _flameJets)
                if (pair.Value.Img != null) Object.Destroy(pair.Value.Img.gameObject);
            _flameJets.Clear();
        }
    }
}
