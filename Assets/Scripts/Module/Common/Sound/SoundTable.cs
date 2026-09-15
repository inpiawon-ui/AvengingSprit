using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Module.Common
{
    /// <summary>음원 한 개 — 주소 · 루프 지점 · 볼륨.</summary>
    [Serializable]
    public sealed class SoundClipEntry
    {
        [SerializeField] private string _key;
        [SerializeField] private string _address;
        [SerializeField] private bool _isMusic;
        [Tooltip("곡 전체를 되풀이한다. 루프 지점이 없는 곡을 화면에 오래 깔 때 쓴다(로비).")]
        [SerializeField] private bool _loopWhole;
        [Tooltip("매니페스트의 표본율. 임포트가 표본율을 바꾸면 루프 지점을 이 비율로 옮긴다.")]
        [SerializeField] private int _sampleRate = 48000;
        [Tooltip("루프가 시작되는 샘플. 매니페스트에서 옮겨 온다 — 손으로 적지 않는다.")]
        [SerializeField] private int _loopStartSamples;
        [Tooltip("루프 한 바퀴 길이(샘플). 0 이면 루프 지점이 없다.")]
        [SerializeField] private int _loopLengthSamples;
        [SerializeField] private float _volume = 1f;

        public SoundClipEntry() { }

        public SoundClipEntry(string key, string address, bool isMusic, bool loopWhole,
                              int sampleRate, int loopStartSamples, int loopLengthSamples, float volume)
        {
            _key = key;
            _address = address;
            _isMusic = isMusic;
            _loopWhole = loopWhole;
            _sampleRate = sampleRate;
            _loopStartSamples = loopStartSamples;
            _loopLengthSamples = loopLengthSamples;
            _volume = volume;
        }

        public string Key => _key;
        public string Address => _address;
        public bool IsMusic => _isMusic;
        public bool LoopWhole => _loopWhole;
        public int SampleRate => _sampleRate;
        public int LoopStartSamples => _loopStartSamples;
        public int LoopLengthSamples => _loopLengthSamples;
        public float Volume => _volume;
        public bool HasLoopPoints => _loopLengthSamples > 0;
    }

    /// <summary>게임의 한 순간(큐) → 음원. 원샷 곡은 끝난 뒤 이어 틀 음원을 가질 수 있다.</summary>
    [Serializable]
    public sealed class SoundCueEntry
    {
        [SerializeField] private string _cue;
        [SerializeField] private string _clipKey;
        [Tooltip("곡이 끝나면 이어서 틀 음원 키. 루프 지점이 없는 곡에만 쓴다.")]
        [SerializeField] private string _thenClipKey;

        public SoundCueEntry() { }

        public SoundCueEntry(string cue, string clipKey, string thenClipKey = null)
        {
            _cue = cue;
            _clipKey = clipKey;
            _thenClipKey = thenClipKey;
        }

        public string Cue => _cue;
        public string ClipKey => _clipKey;
        public string ThenClipKey => _thenClipKey;
    }

    /// <summary>
    /// 챕터 「중간 구역」 곡. 원작 스테이지 가운데 구역에 해당하는 방 범위다 —
    /// 우리 방에는 그런 구역이 없어 방 번호로 정했다(계획서 D3).
    /// </summary>
    [Serializable]
    public sealed class ChapterMidMusicEntry
    {
        [SerializeField] private int _chapter;
        [SerializeField] private int _fromRoom;
        [SerializeField] private int _toRoom;
        [SerializeField] private string _clipKey;

        public ChapterMidMusicEntry() { }

        public ChapterMidMusicEntry(int chapter, int fromRoom, int toRoom, string clipKey)
        {
            _chapter = chapter;
            _fromRoom = fromRoom;
            _toRoom = toRoom;
            _clipKey = clipKey;
        }

        public int Chapter => _chapter;
        public int FromRoom => _fromRoom;
        public int ToRoom => _toRoom;
        public string ClipKey => _clipKey;
    }

    /// <summary>
    /// 음원 · 큐 · 챕터 중간 곡. `Tools/Game/사운드/사운드 표 만들기` 가 만든다 —
    /// 루프 지점은 매니페스트에서 읽으므로 인스펙터에서 손으로 고치지 않는다.
    /// </summary>
    [CreateAssetMenu(fileName = "SoundTable", menuName = "AVSR/Sound Table")]
    public sealed class SoundTable : ScriptableObject
    {
        [Range(0f, 1f)] [SerializeField] private float _musicVolume = 0.7f;
        [Range(0f, 1f)] [SerializeField] private float _effectVolume = 1f;
        [SerializeField] private SoundClipEntry[] _clips = Array.Empty<SoundClipEntry>();
        [SerializeField] private SoundCueEntry[] _cues = Array.Empty<SoundCueEntry>();
        [SerializeField] private ChapterMidMusicEntry[] _chapterMid = Array.Empty<ChapterMidMusicEntry>();

        private Dictionary<string, SoundClipEntry> _clipIndex;
        private Dictionary<string, SoundCueEntry> _cueIndex;

        public float MusicVolume => _musicVolume;
        public float EffectVolume => _effectVolume;
        public IReadOnlyList<SoundClipEntry> Clips => _clips;
        public IReadOnlyList<SoundCueEntry> Cues => _cues;

        public SoundClipEntry FindClip(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_clipIndex == null)
            {
                _clipIndex = new Dictionary<string, SoundClipEntry>(_clips.Length);
                for (int i = 0; i < _clips.Length; i++)
                    if (!string.IsNullOrEmpty(_clips[i].Key)) _clipIndex[_clips[i].Key] = _clips[i];
            }
            return _clipIndex.TryGetValue(key, out var e) ? e : null;
        }

        public SoundCueEntry FindCue(string cue)
        {
            if (string.IsNullOrEmpty(cue)) return null;
            if (_cueIndex == null)
            {
                _cueIndex = new Dictionary<string, SoundCueEntry>(_cues.Length);
                for (int i = 0; i < _cues.Length; i++)
                    if (!string.IsNullOrEmpty(_cues[i].Cue)) _cueIndex[_cues[i].Cue] = _cues[i];
            }
            return _cueIndex.TryGetValue(cue, out var e) ? e : null;
        }

        /// <summary>그 챕터 그 방이 중간 구역이면 음원 키, 아니면 null.</summary>
        public string MidClipKey(int chapter, int room)
        {
            for (int i = 0; i < _chapterMid.Length; i++)
            {
                var m = _chapterMid[i];
                if (m.Chapter == chapter && room >= m.FromRoom && room <= m.ToRoom) return m.ClipKey;
            }
            return null;
        }

#if UNITY_EDITOR
        public void Fill(float musicVolume, float effectVolume, SoundClipEntry[] clips,
                         SoundCueEntry[] cues, ChapterMidMusicEntry[] chapterMid)
        {
            _musicVolume = musicVolume;
            _effectVolume = effectVolume;
            _clips = clips;
            _cues = cues;
            _chapterMid = chapterMid;
            _clipIndex = null;
            _cueIndex = null;
        }
#endif
    }
}
