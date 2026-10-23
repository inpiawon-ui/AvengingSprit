using GameFramework.Core.Base;

namespace Game.Module.Common
{
    /// <summary>
    /// 소리 정적 창구. 부르는 곳마다 `CoreModule.TryGet` 을 적지 않게 한다.
    /// 모듈이 없으면(에디터 도구 · 테스트) 조용히 넘어간다.
    /// </summary>
    public static class GameSound
    {
        // 설정(RE:BORN 설정 창) — 기기 설정이라 세이브가 아니라 PlayerPrefs. 읽을 때마다 PlayerPrefs 를 열지 않게 처음 한 번 읽어 둔다
        private const string MusicKey = "settings.music", SfxKey = "settings.sfx", VibrateKey = "settings.vibrate";
        private static int s_music = -1, s_sfx = -1, s_vibrate = -1;

        public static bool MusicOn
        {
            get { if (s_music < 0) s_music = UnityEngine.PlayerPrefs.GetInt(MusicKey, 1); return s_music == 1; }
            set { s_music = value ? 1 : 0; UnityEngine.PlayerPrefs.SetInt(MusicKey, s_music); UnityEngine.PlayerPrefs.Save(); }
        }

        public static bool SfxOn
        {
            get { if (s_sfx < 0) s_sfx = UnityEngine.PlayerPrefs.GetInt(SfxKey, 1); return s_sfx == 1; }
            set { s_sfx = value ? 1 : 0; UnityEngine.PlayerPrefs.SetInt(SfxKey, s_sfx); UnityEngine.PlayerPrefs.Save(); }
        }

        /// <summary>진동 — 지금 게임에 진동을 쓰는 곳이 없다. 설정만 남겨 둔다(쓰는 곳이 생기면 이 값을 본다).</summary>
        public static bool VibrateOn
        {
            get { if (s_vibrate < 0) s_vibrate = UnityEngine.PlayerPrefs.GetInt(VibrateKey, 1); return s_vibrate == 1; }
            set { s_vibrate = value ? 1 : 0; UnityEngine.PlayerPrefs.SetInt(VibrateKey, s_vibrate); UnityEngine.PlayerPrefs.Save(); }
        }

        public static void Music(string cue)
        {
            if (CoreModule.TryGet<ISoundDirector>(out var d)) d.PlayMusic(cue);
        }

        public static void StopMusic()
        {
            if (CoreModule.TryGet<ISoundDirector>(out var d)) d.StopMusic();
        }

        public static void Cue(string cue)
        {
            if (CoreModule.TryGet<ISoundDirector>(out var d)) d.PlayCue(cue);
        }

        public static void HostAttack(string hostKey)
        {
            if (CoreModule.TryGet<ISoundDirector>(out var d)) d.PlayHostAttack(hostKey);
        }

        public static void HostHurt(string hostKey)
        {
            if (CoreModule.TryGet<ISoundDirector>(out var d)) d.PlayHostHurt(hostKey);
        }

        public static void Skill(string hostKey)
        {
            if (CoreModule.TryGet<ISoundDirector>(out var d)) d.PlaySkill(hostKey);
        }

        public static void StopEffects()
        {
            if (CoreModule.TryGet<ISoundDirector>(out var d)) d.StopAllEffects();
        }
    }
}
