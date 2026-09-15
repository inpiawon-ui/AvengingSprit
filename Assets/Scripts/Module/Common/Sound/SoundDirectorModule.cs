using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Module.Events;
using GameFramework.Core.Base;
using GameFramework.Core.Common;
using GameFramework.Core.Module.EventBus;
using GameFramework.Core.Module.Resource;
using UnityEngine;

namespace Game.Module.Common
{
    /// <summary>
    /// 사운드 (2026-09-15, 원작 음원).
    ///
    /// ── 왜 프레임워크의 SoundModule 을 안 쓰나 ─────────────────────
    /// `PlayBGM` 은 파일 전체만 되풀이한다 — 인트로가 있는 곡(12곡 중 9곡)이 매 바퀴
    /// 인트로부터 다시 나온다. `Play` 는 부를 때마다 주소로 로드해(참조 수만 늘고 해제가 없다)
    /// 초당 수십 발 쏘는 몸에서 소리가 늦고 풀이 바닥난다. 프레임워크는 고칠 수 없으니 게임 쪽에 둔다.
    /// </summary>
    [Module(Layer = ModuleLayer.Game)]
    public sealed class SoundDirectorModule : IModule, ITickable
    {
        private SoundDirector _director;
        private readonly List<IDisposable> _tokens = new();

        public bool IsInitialized { get; private set; }

        public void Register()
        {
            _director = new SoundDirector();
            CoreModule.Register<ISoundDirector>(_director);
            IsInitialized = true;
        }

        public void Initialize()
        {
            var bus = CoreModule.Get<IEventBus>();
            _tokens.Add(bus.Subscribe<RoomEnteredEvent>(_director.OnRoomEntered));
            _tokens.Add(bus.Subscribe<StageFinishedEvent>(_director.OnStageFinished));
            _tokens.Add(bus.Subscribe<RunGoldChangedEvent>(_director.OnRunGoldChanged));
            _tokens.Add(bus.Subscribe<BuffChosenEvent>(_director.OnBuffChosen));
            _tokens.Add(bus.Subscribe<ShopPurchasedEvent>(_director.OnShopPurchased));
            _tokens.Add(bus.Subscribe<HostLostEvent>(_director.OnHostLost));
            _director.LoadAsync().Forget();   // fire-and-forget: 표가 오기 전 곡 요청은 모아 뒀다가 튼다
        }

        public void Tick(float deltaTime) => _director?.Tick();

        public void Dispose()
        {
            for (int i = 0; i < _tokens.Count; i++) _tokens[i]?.Dispose();
            _tokens.Clear();
            _director?.Shutdown();
            CoreModule.Unregister<ISoundDirector>();
            _director = null;
            IsInitialized = false;
        }
    }

    internal sealed class SoundDirector : ISoundDirector
    {
        private const string TableAddress = "TableData/SoundTable";
        private const int EffectVoices = 16;
        private const float SameEffectGapSeconds = 0.06f;
        private const int SameEffectMaxVoices = 3;
        private const float MusicFadeSeconds = 0.4f;
        private const double ScheduleLeadSeconds = 0.05;
        // 다음 바퀴를 거는 시점. 너무 가까우면 프레임이 튈 때 놓치고, 멀면 곡을 바꿀 때 예약이 남는다.
        private const double RescheduleAheadSeconds = 1.0;

        /// <summary>검증용 재생 로그. 기본은 꺼 둔다 — 교전 중 매 발 문자열을 만들지 않게.</summary>
        internal static bool s_logPlays;

        private readonly struct EffectRef
        {
            public readonly AudioClip Clip;
            public readonly float Volume;
            public readonly string Label;
            // 드물게 나고 꼭 들려야 하는 소리(스킬·보스 패턴·버튼). 간격 · 같은 소리 개수 제한을 안 받는다.
            public readonly bool Important;

            public EffectRef(AudioClip clip, float volume, string label, bool important)
            {
                Clip = clip;
                Volume = volume;
                Label = label;
                Important = important;
            }
        }

        private SoundTable _table;
        private GameObject _root;
        private AudioSource _musicA, _musicB;
        private readonly AudioSource[] _voices = new AudioSource[EffectVoices];

        private readonly Dictionary<string, AudioClip> _loaded = new();
        private readonly Dictionary<AudioClip, float> _lastPlayed = new();
        private readonly Dictionary<string, EffectRef> _effectCue = new();
        private readonly Dictionary<string, EffectRef> _hostAttack = new();
        private readonly Dictionary<string, EffectRef> _hostHurt = new();
        private readonly Dictionary<string, EffectRef> _skill = new();

        // ── 곡 상태 ──
        private SoundClipEntry _music;
        private AudioClip _musicClip;
        private string _musicKey;
        private string _thenKey;
        private double _boundaryDsp;   // 루프곡: 다음 이음매 시각 · 원샷곡: 끝나는 시각
        private bool _nextIsA;
        private float _fade = 1f;
        private float _fadeTarget = 1f;
        private string _requestedKey;
        private string _requestedThen;
        private string _pendingCue;
        private bool _loading;

        internal int PlayedEffects { get; private set; }
        internal int SkippedEffects { get; private set; }

        public bool IsReady { get; private set; }
        public string CurrentMusicKey => _musicKey;

        // ── 불러오기 ─────────────────────────────────────────────

        public async UniTask LoadAsync()
        {
            EnsureRoot();
            var res = CoreModule.Get<IResourceManager>();
            try
            {
                _table = await res.LoadAsync<SoundTable>(TableAddress);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Sound] 사운드 표를 못 읽었다 — 소리 없이 간다: {e.Message}");
                return;
            }
            if (_table == null) return;

            // 효과음은 미리 다 올린다 — 쏘는 순간 로드를 기다리면 첫 발 소리가 늦는다.
            var clips = _table.Clips;
            for (int i = 0; i < clips.Count; i++)
            {
                var c = clips[i];
                if (c.IsMusic) continue;
                try
                {
                    var clip = await res.LoadAsync<AudioClip>(c.Address);
                    if (clip != null) _loaded[c.Key] = clip;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Sound] 효과음 {c.Address} 를 못 읽었다: {e.Message}");
                }
            }

            BuildEffectIndex();
            IsReady = true;

            if (_pendingCue != null)
            {
                var cue = _pendingCue;
                _pendingCue = null;
                PlayMusic(cue);
            }
        }

        private void EnsureRoot()
        {
            if (_root != null) return;
            _root = new GameObject("[SoundDirector]");
            UnityEngine.Object.DontDestroyOnLoad(_root);
            // ⚠ **귀가 없으면 아무 소리도 안 들린다.** 이 게임 씬(부트·타이틀·로비·인게임)에는
            //   AudioListener 가 하나도 없다 — UI 만 그리는 씬이라 카메라에 붙어 있지 않다.
            //   씬마다 붙이면 전환 순간 둘이 되거나 없어진다. 씬을 넘어 사는 이 뿌리에 하나만 둔다.
            if (UnityEngine.Object.FindFirstObjectByType<AudioListener>() == null)
                _root.AddComponent<AudioListener>();
            _musicA = NewSource("MusicA");
            _musicB = NewSource("MusicB");
            for (int i = 0; i < _voices.Length; i++) _voices[i] = NewSource($"Voice{i}");

            AudioSource NewSource(string name)
            {
                var go = new GameObject(name);
                go.transform.SetParent(_root.transform, false);
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                return s;
            }
        }

        /// <summary>큐 이름을 한 번만 풀어 둔다. 교전 중에는 사전만 찾는다.</summary>
        private void BuildEffectIndex()
        {
            var cues = _table.Cues;
            for (int i = 0; i < cues.Count; i++)
            {
                var cue = cues[i];
                var entry = _table.FindClip(cue.ClipKey);
                if (entry == null || entry.IsMusic) continue;
                if (!_loaded.TryGetValue(entry.Key, out var clip)) continue;

                // ⚠ 간격 제한은 **음원** 기준이다. 스킬이 평타와 같은 음원을 빌리면(흡혈귀 sfx_37)
                //   방금 난 평타에 먹혀 스킬 소리가 안 났다(실측 2026-09-15). 드문 소리는 줄을 서지 않는다.
                bool important = cue.Cue.StartsWith("skill.", StringComparison.Ordinal)
                              || cue.Cue.StartsWith("boss.", StringComparison.Ordinal)
                              || cue.Cue.StartsWith("ui.", StringComparison.Ordinal);
                var fx = new EffectRef(clip, entry.Volume, cue.Cue, important);
                _effectCue[cue.Cue] = fx;

                // host.{hostKey}.attack · host.{hostKey}.hurt · skill.{hostKey}
                var parts = cue.Cue.Split('.');
                if (parts.Length == 3 && parts[0] == "host")
                {
                    if (parts[2] == "attack") _hostAttack[parts[1]] = fx;
                    else if (parts[2] == "hurt") _hostHurt[parts[1]] = fx;
                }
                else if (parts.Length == 2 && parts[0] == "skill")
                {
                    _skill[parts[1]] = fx;
                }
            }
        }

        public void Shutdown()
        {
            if (_root != null) UnityEngine.Object.Destroy(_root);
            _root = null;
            _musicA = _musicB = null;
            for (int i = 0; i < _voices.Length; i++) _voices[i] = null;
            _loaded.Clear();
            _lastPlayed.Clear();
            _effectCue.Clear();
            _hostAttack.Clear();
            _hostHurt.Clear();
            _skill.Clear();
            _table = null;
            IsReady = false;
        }

        // ── 곡 ───────────────────────────────────────────────────

        public void PlayMusic(string cue)
        {
            if (!IsReady) { _pendingCue = cue; return; }
            var entry = _table.FindCue(cue);
            if (entry == null)
            {
                Debug.LogWarning($"[Sound] 곡 큐가 표에 없다: {cue}");
                return;
            }
            RequestMusic(entry.ClipKey, entry.ThenClipKey, cue);
        }

        public void StopMusic()
        {
            _pendingCue = null;
            _requestedKey = null;
            _fadeTarget = 0f;
        }

        private void RequestMusic(string key, string then, string label)
        {
            // 같은 곡 — 다시 틀지 않는다. 방이 바뀔 때마다 처음부터 나오면 안 된다.
            if (key == _musicKey)
            {
                _requestedKey = null;
                _fadeTarget = 1f;
                return;
            }
            if (key == _requestedKey) return;

            _requestedKey = key;
            _requestedThen = then;
            _fadeTarget = 0f;
            if (_musicKey == null) _fade = 0f;   // 틀던 곡이 없으면 기다릴 것이 없다
            if (s_logPlays) Debug.Log($"[Sound] music request {key} ← {label}");
        }

        private async UniTaskVoid BeginRequestedAsync()
        {
            string key = _requestedKey;
            string then = _requestedThen;
            var entry = _table.FindClip(key);
            if (entry == null)
            {
                Debug.LogWarning($"[Sound] 음원이 표에 없다: {key}");
                _requestedKey = null;
                return;
            }

            if (!_loaded.TryGetValue(key, out var clip))
            {
                _loading = true;
                try
                {
                    clip = await CoreModule.Get<IResourceManager>().LoadAsync<AudioClip>(entry.Address);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Sound] 곡 {entry.Address} 를 못 읽었다: {e.Message}");
                }
                finally
                {
                    _loading = false;
                }
                if (_root == null) return;   // 기다리는 사이 모듈이 내려갔다
                if (clip == null)
                {
                    if (_requestedKey == key) _requestedKey = null;
                    return;
                }
                _loaded[key] = clip;
            }

            // 기다리는 사이 다른 곡이 요청됐으면 그쪽을 다음 틱이 시작한다.
            if (_requestedKey != key) return;
            _requestedKey = null;
            StartTrack(entry, clip, then);
        }

        private void StartTrack(SoundClipEntry entry, AudioClip clip, string then)
        {
            StopMusicSources();
            _music = entry;
            _musicClip = clip;
            _musicKey = entry.Key;
            _thenKey = then;
            _fade = 1f;
            _fadeTarget = 1f;
            _musicA.clip = clip;
            _musicB.clip = clip;

            double t0 = AudioSettings.dspTime + ScheduleLeadSeconds;
            _musicA.timeSamples = 0;

            if (entry.HasLoopPoints)
            {
                double introEnd = t0 + SecondsOf(LoopStart + LoopLength);
                _musicA.loop = false;
                _musicB.loop = false;
                _musicA.PlayScheduled(t0);
                _musicA.SetScheduledEndTime(introEnd);
                _musicB.timeSamples = LoopStart;
                _musicB.PlayScheduled(introEnd);
                _musicB.SetScheduledEndTime(introEnd + SecondsOf(LoopLength));
                _boundaryDsp = introEnd + SecondsOf(LoopLength);
                _nextIsA = true;
            }
            else
            {
                _musicA.loop = entry.LoopWhole;
                _musicA.PlayScheduled(t0);
                _boundaryDsp = t0 + (double)clip.samples / clip.frequency;
            }
            ApplyMusicVolume();
            if (s_logPlays)
                Debug.Log($"[Sound] music start {entry.Key} freq={clip.frequency} samples={clip.samples} " +
                          $"loopStart={LoopStart} loopLen={LoopLength} then={then}");
        }

        // 임포트가 표본율을 바꿔도 같은 자리를 가리키게 매니페스트 표본율에서 옮긴다.
        private int LoopStart => ScaleSamples(_music.LoopStartSamples);
        private int LoopLength => ScaleSamples(_music.LoopLengthSamples);

        private int ScaleSamples(int manifestSamples)
        {
            int rate = _music.SampleRate > 0 ? _music.SampleRate : _musicClip.frequency;
            return rate == _musicClip.frequency
                ? manifestSamples
                : (int)Math.Round((double)manifestSamples * _musicClip.frequency / rate);
        }

        private double SecondsOf(int samples) => (double)samples / _musicClip.frequency;

        private void StopMusicSources()
        {
            if (_musicA != null) _musicA.Stop();
            if (_musicB != null) _musicB.Stop();
            _musicKey = null;
            _music = null;
            _musicClip = null;
        }

        private void ApplyMusicVolume()
        {
            if (_musicA == null || _table == null || _music == null) return;
            float v = _table.MusicVolume * _music.Volume * _fade;
            _musicA.volume = v;
            _musicB.volume = v;
        }

        public void Tick()
        {
            if (_root == null || !IsReady) return;

            if (!Mathf.Approximately(_fade, _fadeTarget))
                _fade = Mathf.MoveTowards(_fade, _fadeTarget, Time.unscaledDeltaTime / MusicFadeSeconds);

            if (_fade <= 0f && _fadeTarget <= 0f)
            {
                if (_musicKey != null) StopMusicSources();
                if (_requestedKey != null && !_loading) BeginRequestedAsync().Forget();   // fire-and-forget: 로드 뒤 스스로 시작한다
                return;
            }
            if (_musicKey == null) return;
            ApplyMusicVolume();

            double now = AudioSettings.dspTime;
            if (_music.HasLoopPoints)
            {
                if (now >= _boundaryDsp - RescheduleAheadSeconds) ScheduleNextLoop(now);
            }
            else if (!_music.LoopWhole && now >= _boundaryDsp)
            {
                // 원샷 곡이 끝났다 — 이어 틀 곡이 있으면 바로 잇는다
                string then = _thenKey;
                StopMusicSources();
                if (!string.IsNullOrEmpty(then)) RequestMusic(then, null, "then");
            }
        }

        private void ScheduleNextLoop(double now)
        {
            var next = _nextIsA ? _musicA : _musicB;
            var other = _nextIsA ? _musicB : _musicA;
            double at = _boundaryDsp;

            // 예약을 놓쳤다(에디터 일시정지 · 긴 멈춤). 끊긴 채 두지 말고 지금부터 루프 시작점으로 잇는다.
            if (at < now + ScheduleLeadSeconds)
            {
                at = now + ScheduleLeadSeconds;
                other.Stop();
                if (s_logPlays) Debug.Log($"[Sound] loop missed — restart at loop start ({_musicKey})");
            }

            next.Stop();
            next.timeSamples = LoopStart;
            next.PlayScheduled(at);
            next.SetScheduledEndTime(at + SecondsOf(LoopLength));
            _boundaryDsp = at + SecondsOf(LoopLength);
            _nextIsA = !_nextIsA;
            if (s_logPlays) Debug.Log($"[Sound] loop scheduled {_musicKey} at dsp {at:0.000} from sample {LoopStart}");
        }

        // ── 효과음 ───────────────────────────────────────────────

        public void PlayCue(string cue)
        {
            if (!IsReady || cue == null) return;
            if (_effectCue.TryGetValue(cue, out var fx)) PlayEffect(fx);
        }

        public void PlayHostAttack(string hostKey)
        {
            if (!IsReady || hostKey == null) return;
            if (_hostAttack.TryGetValue(hostKey, out var fx)) PlayEffect(fx);
        }

        public void PlayHostHurt(string hostKey)
        {
            if (!IsReady || hostKey == null) return;
            if (_hostHurt.TryGetValue(hostKey, out var fx)) PlayEffect(fx);
        }

        public void PlaySkill(string hostKey)
        {
            if (!IsReady || hostKey == null) return;
            if (_skill.TryGetValue(hostKey, out var fx)) PlayEffect(fx);
        }

        public void StopAllEffects()
        {
            for (int i = 0; i < _voices.Length; i++)
                if (_voices[i] != null) _voices[i].Stop();
        }

        private void PlayEffect(in EffectRef fx)
        {
            if (fx.Clip == null || _root == null) return;

            // 같은 소리는 간격을 둔다 — 난사(초당 60발)가 소리 한 덩어리로 뭉개지지 않게.
            float now = Time.unscaledTime;
            if (!fx.Important && _lastPlayed.TryGetValue(fx.Clip, out float last) && now - last < SameEffectGapSeconds)
            {
                SkippedEffects++;
                return;
            }

            int same = 0;
            AudioSource free = null;
            for (int i = 0; i < _voices.Length; i++)
            {
                var v = _voices[i];
                if (v.isPlaying) { if (v.clip == fx.Clip) same++; }
                else if (free == null) free = v;
            }
            if ((!fx.Important && same >= SameEffectMaxVoices) || free == null)
            {
                SkippedEffects++;
                return;
            }

            _lastPlayed[fx.Clip] = now;
            free.clip = fx.Clip;
            free.volume = _table.EffectVolume * fx.Volume;
            free.Play();
            PlayedEffects++;
            if (s_logPlays) Debug.Log($"[Sound] sfx {fx.Clip.name} ← {fx.Label}");
        }

        // ── 이벤트 ───────────────────────────────────────────────

        internal void OnRoomEntered(RoomEnteredEvent e)
        {
            if (!IsReady) return;
            if (e.IsBossRoom) { PlayMusic($"chapter.{e.Chapter}.boss"); return; }

            // 원작 스테이지 가운데 구역 — 방 번호 범위로 정했다(계획서 D3)
            var mid = _table.MidClipKey(e.Chapter, e.StageInChapter);
            if (mid != null) { RequestMusic(mid, null, "chapter.mid"); return; }

            // 상점·제단·이벤트 방도 챕터 곡을 그대로 이어 간다 — 방마다 곡이 끊기면 산만하다
            PlayMusic($"chapter.{e.Chapter}.normal");
        }

        internal void OnStageFinished(StageFinishedEvent e) =>
            PlayMusic(e.IsCleared ? "stage.clear" : "stage.fail");

        internal void OnRunGoldChanged(RunGoldChangedEvent e)
        {
            if (e.Delta > 0) PlayCue("run.gold");
        }

        internal void OnBuffChosen(BuffChosenEvent e) => PlayCue("run.card");

        internal void OnShopPurchased(ShopPurchasedEvent e) => PlayCue("run.shop");

        internal void OnHostLost(HostLostEvent e) => PlayHostHurt(e.LostHostKey);
    }
}
