namespace GameFramework.Game.Common
{
    /// <summary>
    /// 열거형 정의
    /// </summary>
    public static class Enums
    {
        #region Action

        /// <summary>
        /// 유저 행위
        /// </summary>
        public enum ActionType
        {
            UserLevel,              //: 유저 레벨
        }

        #endregion

        #region Quest

        /// <summary>
        /// 퀘스트
        /// </summary>
        public enum QuestType
        {
            Daily,      //: 일일
            Weekly,     //: 주간
            Monthly,    //: 월간
            FixedTerm,  //: 기간
        }

        #endregion

        #region Reward

        /// <summary>
        /// 
        /// </summary>
        public enum RewardType
        {
            Silver,
            Gem,
        }

        /// <summary>
        /// 
        /// </summary>
        public enum RewardStateType
        {
            None,   //: 받을 수 없음
            Gain,   //: 받을 수 있음
            Done,   //: 획득한 상태
        }

        #endregion
    }
}