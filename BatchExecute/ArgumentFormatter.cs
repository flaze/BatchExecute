using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Policy;

namespace BatchExecute
{
    public static class ArgumentFormatter
    {
        public static string[] Format(string format, DFile file)
        {
            return format.Inject(file).InjectFunctions(FunctionHandler);
        }

        private static IEnumerable<string> FunctionHandler(string name, object[] arguments)
        {
            var method = typeof (ArgumentFormatter)
                    .FindMethod(name,
                                BindingFlags.FlattenHierarchy | BindingFlags.Static |
                                BindingFlags.Public,
                                arguments.Select(a => a.GetType()).ToArray())
                    .First();

            var methodParameters = method.GetParameters();

            var lastParameter = methodParameters[methodParameters.Length - 1];
            var customAttributes = lastParameter.GetCustomAttributes(false);
            var paramsAttribute = customAttributes.FirstOrDefault(p => p is ParamArrayAttribute) as ParamArrayAttribute;

            if (paramsAttribute != null)
            {
                var arrayElementType = lastParameter.ParameterType.GetElementType();

                var paramsObjectArray = arguments.Skip(methodParameters.Length - 1).ToArray();
                var paramsArray = Array.CreateInstance(arrayElementType, paramsObjectArray.Length);
                Array.Copy(paramsObjectArray, paramsArray, paramsObjectArray.Length);

                arguments = arguments.Take(methodParameters.Length - 1)
                                     .Concat(new object[] { paramsArray })
                                     .ToArray();
            }

            return ((IEnumerable)method.Invoke(null, arguments)).Cast<object>().Select(v => v.ToString());
        }

        public static IEnumerable<string> Range(int numSteps, int every, int length)
        {
            return Range(numSteps, every, 0, length);
        }

        public static IEnumerable<string> Range(int numSteps, int every, int offset, int length)
        {
            return Number(numSteps, every, offset).Select(n => n + "-" + (n + length - 1));
        }

        public static IEnumerable<string> RangeLength(int numSteps, int every, int offset, params int[] lengths)
        {
            return RangeLength(numSteps, every, offset, false, lengths);
        }

        public static IEnumerable<string> RangeLength(int numSteps, int every, int offset, bool collapse, params int[] lengths)
        {
            return Number(numSteps, every, offset)
                .Select((s, i) =>
                {
                    var e = s + lengths[(i + 1).Repeat(lengths.Length) - 1] - 1;
                    if (collapse && s == e)
                        return s.ToString();
                    return s + "-" + e;
                });
        }

        public static IEnumerable<int> Number(int numSteps, int stepSize, params int[] offsets)
        {
            var cycleSteps = (int) Math.Ceiling((double) numSteps/Math.Max(offsets.Length, 1));

            return EnumRange(0, stepSize * cycleSteps, cycleSteps)
                             .SelectMany(step => offsets.Length > 0
                                 ? offsets.Select(offset => step + offset)
                                 : new[] {step})
                             .Take(numSteps);
        }


        private static IEnumerable<MethodInfo> FindMethod(this Type type, string name, BindingFlags flags, params Type[] parameterTypes)
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
                            .Where((t, i) => !ParameterMatchesType(parameters[Math.Min(parameters.Length - 1, i)], t))
                            .Any();
            });
        }

        private static bool ParameterMatchesType(ParameterInfo parameter, Type type)
        {
            if (parameter.ParameterType == type)
                return true;

            if (parameter.IsParamsParameter() && parameter.ParameterType.GetElementType() == type)
                return true;

            return false;
        }

        private static bool IsParamsParameter(this ParameterInfo parameter)
        {
            return parameter.GetCustomAttributes(false).Any(a => a is ParamArrayAttribute);
        }

        private static IEnumerable<int> EnumRange(int min, int max, int steps)
        {
            return Enumerable.Range(0, steps)
                             .Select(i => (int)(min + (max - min)*((double) i/(steps))));
        }
    }
}
