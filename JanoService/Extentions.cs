using JanoService.Service;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace JanoService
{
    public static class Extentions
    {
        public static T ToEnum<T>(this string value, T defaultValue) where T : struct
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            T result;
            return Enum.TryParse(value, true, out result) 
                ? result
                : defaultValue;
        }
    }
}
