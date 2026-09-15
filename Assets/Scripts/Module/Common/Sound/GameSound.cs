using GameFramework.Core.Base;

namespace Game.Module.Common
{
    /// <summary>
    /// 소리 정적 창구. 부르는 곳마다 `CoreModule.TryGet` 을 적지 않게 한다.
    /// 모듈이 없으면(에디터 도구 · 테스트) 조용히 넘어간다.
    /// </summary>
    public static class GameSound
    {
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
