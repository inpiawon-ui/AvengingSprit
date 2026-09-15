namespace Game.Module.Common
{
    public interface ISoundDirector
    {
        /// <summary>표와 효과음을 다 올렸는가. 그 전에 온 곡 요청은 모아 뒀다가 튼다.</summary>
        bool IsReady { get; }

        /// <summary>지금 틀고 있는 음원 키. 없으면 null.</summary>
        string CurrentMusicKey { get; }

        /// <summary>큐의 곡을 튼다. 같은 곡이면 다시 틀지 않고, 다르면 페이드아웃 뒤 바꾼다.</summary>
        void PlayMusic(string cue);

        /// <summary>곡을 페이드아웃해 멈춘다.</summary>
        void StopMusic();

        /// <summary>큐의 효과음을 한 번 낸다.</summary>
        void PlayCue(string cue);

        /// <summary>그 호스트의 평타 소리. 없는 몸은 조용하다.</summary>
        void PlayHostAttack(string hostKey);

        /// <summary>그 호스트가 맞는 소리.</summary>
        void PlayHostHurt(string hostKey);

        /// <summary>그 호스트의 액티브 스킬 소리. 없는 몸은 조용하다.</summary>
        void PlaySkill(string hostKey);

        /// <summary>나고 있는 효과음을 모두 끊는다 — 보스 대폭발 직전.</summary>
        void StopAllEffects();
    }
}
