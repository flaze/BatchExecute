using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BatchExecute
{
    public static class Extensions
    {
        public static int Repeat(this int v, int count)
        {
            return (int)(v - ((Math.Ceiling(v / (double)count) - 1) * count));
        }

        public static string ReplaceAt(this string s, int index, int length, string value)
        {
            return s.Substring(0, index) + value + s.Substring(index + length, s.Length - index - length);
        }

        #region Reflection

        public static bool IsParamsParameter(this ICustomAttributeProvider parameter)
        {
            return parameter.GetCustomAttributes(false).Any(a => a is ParamArrayAttribute);
        }

        public static bool MatchesType(this ParameterInfo parameter, Type type)
        {
            if (parameter.ParameterType == type)
                return true;

            if (parameter.IsParamsParameter() && parameter.ParameterType.GetElementType() == type)
                return true;

            return false;
        }

        public static IEnumerable<MethodInfo> FindMethod(this Type type, string name, BindingFlags flags, params Type[] parameterTypes)
        {
            name = name.ToLower();

            return type.GetMethods(flags).Where(m =>
            {
                if (m.Name.ToLower() != name)
                    return false;

                var parameters = m.GetParameters();
                var hasParamsParameter = parameters[parameters.Length - 1].IsParamsParameter();

                if (parameterTypes == null || parameterTypes.Length == 0)
                    return parameters.Length == 0;

                if (parameters.Length != parameterTypes.Length && !hasParamsParameter)
                    return false;

                return !parameterTypes
                            .Where((t, i) => !parameters[Math.Min(parameters.Length - 1, i)].MatchesType(t))
                            .Any();
            });
        }

        #endregion
    }
}
