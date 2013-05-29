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
        public static IEnumerable<string> Format(string format, DFile file)
        {
            return format.Inject(file).InjectFunctions(FunctionHandler);
        }

        private static IEnumerable<string> FunctionHandler(string name, object[] arguments)
        {
            var method = GetMethod(name, arguments);
            var methodParameters = method.GetParameters();

            var lastParameter = methodParameters[methodParameters.Length - 1];
            var paramsAttribute = lastParameter.GetCustomAttributes(false)
                .FirstOrDefault(p => p is ParamArrayAttribute) as ParamArrayAttribute;

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

        private static MethodInfo GetMethod(string name, object[] arguments)
        {
            return typeof (ArgumentFormatter)
                .FindMethod(name,
                            BindingFlags.FlattenHierarchy | BindingFlags.Static |
                            BindingFlags.Public,
                            arguments.Select(a => a.GetType()).ToArray())
                .First();
        }

        #region Functions

        #region Range

        public static IEnumerable<string> Range(int numSteps, int every, int length)
        {
            return Range(numSteps, every, 0, length);
        }

        public static IEnumerable<string> Range(int numSteps, int every, int offset, int length)
        {
            return Number(numSteps, every, offset).Select(n => n + "-" + (n + length - 1));
        }

        #endregion

        #region RangeLength

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

        #endregion

        public static IEnumerable<int> Number(int numSteps, int stepSize, params int[] offsets)
        {
            var cycleSteps = (int) Math.Ceiling((double) numSteps/Math.Max(offsets.Length, 1));

            return Utils.EnumerableRange(0, stepSize*cycleSteps, cycleSteps)
                        .SelectMany(step => offsets.Length > 0
                                                ? offsets.Select(offset => step + offset)
                                                : new[] {step})
                        .Take(numSteps);
        }

        #endregion
    }
}
