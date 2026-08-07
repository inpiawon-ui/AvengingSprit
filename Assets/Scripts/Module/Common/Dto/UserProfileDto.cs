using System;

namespace Game.Module.Common.Dto
{
    // JsonUtility 직렬화 규칙: [Serializable] + public 필드만 사용 (프로퍼티 불가)
    [Serializable]
    public struct UserProfileDto
    {
        public string userId;
        public string nickname;
        public int    level;
    }
}
