using System;
using System.Collections.Generic;

namespace GameFramework.Game.Common
{
    public class EnumUtil
    {
        /// <summary>
        /// Get : Enum Values
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <returns></returns>
        public static List<TEnum> GetEnumValues<TEnum>() where TEnum : struct, IConvertible
        {
            List<TEnum> result = new List<TEnum>();

            var values = Enum.GetNames(typeof(TEnum));
            for (int i = 0; i < values.Length; i++)
            {
                result.Add(ToEnumValue<TEnum>(values[i]));
            }

            return result;
        }

        /// <summary>
        /// String to EnumValue
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="item"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static TEnum ToEnumValue<TEnum>(string item, bool ignoreCase = default) where TEnum : struct
        {
            TEnum result = default;
            return Enum.TryParse(item, ignoreCase, out result) ? result : default;
        }

        /// <summary>
        /// String to EnumValue
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="item"></param>
        /// <param name="ignoreCase"></param>
        /// <returns></returns>
        public static bool IsEnumValue<TEnum>(string item, bool ignoreCase = default) where TEnum : struct
        {
            TEnum tenumResult = default;
            return Enum.TryParse(item, ignoreCase, out tenumResult);
        }
    }
}
