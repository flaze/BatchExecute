// From: http://mo.notono.us/2008/07/c-stringinject-format-strings-by-key.html

using System;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text.RegularExpressions;
using System.Collections;
using System.Globalization;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows;

[assembly: CLSCompliant(true)]
namespace BatchExecute
{
    public static class StringInjectExtension
    {
        public const string AttributePattern = "{{({0})(?:}}|(?::(.[^}}]*)}}))";
        public const string FunctionSignaturePattern = "{(?<name>.*?)\\((?<arguments>.*?)\\)}";
        public const string FunctionArgumentsPattern = "(?:(?<number>\\d+)|(?:\\\"(?<string>[a-z0-9\\s]*)\\\")|(?<boolean>true|false))\\s?,?\\s?";

        private static readonly Regex FunctionSignatureRegex;
        private static readonly Regex FunctionArgumentsRegex;

        static StringInjectExtension()
        {
            FunctionSignatureRegex = new Regex(FunctionSignaturePattern);
            FunctionArgumentsRegex = new Regex(FunctionArgumentsPattern);
        }

        /// <summary>
        /// Extension method that replaces keys in a string with the values of matching object properties.
        /// <remarks>Uses <see cref="String.Format()"/> internally; custom formats should match those used for that method.</remarks>
        /// </summary>
        /// <param name="formatString">The format string, containing keys like {foo} and {foo:SomeFormat}.</param>
        /// <param name="injectionObject">The object whose properties should be injected in the string</param>
        /// <returns>A version of the formatString string with keys replaced by (formatted) key values.</returns>
        public static string Inject(this string formatString, object injectionObject)
        {
            return formatString.Inject(GetPropertyHash(injectionObject));
        }

        /// <summary>
        /// Extension method that replaces keys in a string with the values of matching dictionary entries.
        /// <remarks>Uses <see cref="String.Format()"/> internally; custom formats should match those used for that method.</remarks>
        /// </summary>
        /// <param name="formatString">The format string, containing keys like {foo} and {foo:SomeFormat}.</param>
        /// <param name="dictionary">An <see cref="IDictionary"/> with keys and values to inject into the string</param>
        /// <returns>A version of the formatString string with dictionary keys replaced by (formatted) key values.</returns>
        public static string Inject(this string formatString, IDictionary dictionary)
        {
            return formatString.Inject(new Hashtable(dictionary));
        }

        /// <summary>
        /// Extension method that replaces keys in a string with the values of matching hashtable entries.
        /// <remarks>Uses <see cref="String.Format()"/> internally; custom formats should match those used for that method.</remarks>
        /// </summary>
        /// <param name="formatString">The format string, containing keys like {foo} and {foo:SomeFormat}.</param>
        /// <param name="attributes">A <see cref="Hashtable"/> with keys and values to inject into the string</param>
        /// <returns>A version of the formatString string with hastable keys replaced by (formatted) key values.</returns>
        public static string Inject(this string formatString, Hashtable attributes)
        {
            var result = formatString;
            if (attributes == null || formatString == null)
                return result;

            return attributes.Keys.Cast<string>()
                .Aggregate(result, (current, attributeKey) =>
                    current.InjectSingleValue(attributeKey, attributes[attributeKey]));
        }

        /// <summary>
        /// Replaces all instances of a 'key' (e.g. {foo} or {foo:SomeFormat}) in a string with an optionally formatted value, and returns the result.
        /// </summary>
        /// <param name="formatString">The string containing the key; unformatted ({foo}), or formatted ({foo:SomeFormat})</param>
        /// <param name="key">The key name (foo)</param>
        /// <param name="replacementValue">The replacement value; if null is replaced with an empty string</param>
        /// <returns>The input string with any instances of the key replaced with the replacement value</returns>
        public static string InjectSingleValue(this string formatString, string key, object replacementValue)
        {
            var result = formatString;
            //regex replacement of key with value, where the generic key format is:
            //Regex foo = new Regex("{(foo)(?:}|(?::(.[^}]*)}))");
            var attributeRegex = new Regex(string.Format(AttributePattern, key));  //for key = foo, matches {foo} and {foo:SomeFormat}

            //loop through matches, since each key may be used more than once (and with a different format string)
            foreach (Match m in attributeRegex.Matches(formatString))
            {
                var replacement = m.ToString();
                if (m.Groups[2].Length > 0) //matched {foo:SomeFormat}
                {
                    //do a double string.Format - first to build the proper format string, and then to format the replacement value
                    string attributeFormatString = string.Format(CultureInfo.InvariantCulture, "{{0:{0}}}", m.Groups[2]);
                    replacement = string.Format(CultureInfo.CurrentCulture, attributeFormatString, replacementValue);
                }
                else //matched {foo}
                {
                    replacement = (replacementValue ?? string.Empty).ToString();
                }
                //perform replacements, one match at a time
                result = result.Replace(m.ToString(), replacement);  //attributeRegex.Replace(result, replacement, 1);
            }
            return result;

        }

        /// <summary>
        /// Extension method that replaces function signatures with the result from functionHandler.
        /// </summary>
        /// <param name="formatString">The format string, containing keys like {somefunction(5, 3)}.</param>
        /// <param name="functionHandler">Function handler to get the result of the function.</param>
        /// <returns>A version of the formatString string with function signatures replaced.</returns>
        public static IEnumerable<string> InjectFunctions(this string formatString,
            Func<string, object[], IEnumerable<string>> functionHandler)
        {
            var functionSignatureMatches = FunctionSignatureRegex.Matches(formatString);

            if (functionSignatureMatches.Count == 0)
                return new[] {formatString};

            // Get the results for each function signature match
            var functions = GetFunctionResults(functionSignatureMatches, functionHandler);

            // Parse into steps and add repeating functions
            var steps = ToStepFunctions(functions);
            RepeatFill(steps);

            // Actually replace the format string with step function results
            return ApplySteps(formatString, steps);
        }

        #region InjectFunctions Helper Methods

        private static List<List<DFunctionResult>> GetFunctionResults(IEnumerable functionMatches,
            Func<string, object[], IEnumerable<string>> functionHandler)
        {
            var functions = new List<List<DFunctionResult>>();

            foreach (Match m in functionMatches)
            {
                if (!m.Success) continue;
                var name = m.Groups["name"].Value;

                // Parse arguments
                var arguments = new List<object>();

                var argumentMatches = FunctionArgumentsRegex
                    .Matches(m.Groups["arguments"].Value)
                    .Cast<Match>().Where(am => am.Success);

                foreach (var argMatch in argumentMatches)
                {
                    if (argMatch.Groups["number"].Success)
                        arguments.Add(int.Parse(argMatch.Groups["number"].Value));

                    else if (argMatch.Groups["string"].Success)
                        arguments.Add(argMatch.Groups["string"].Value);

                    else if (argMatch.Groups["boolean"].Success)
                        arguments.Add(bool.Parse(argMatch.Groups["boolean"].Value.ToLower()));
                }


                // Get the function result via the functionHandler
                var result = functionHandler(name, arguments.ToArray())
                    .Select(r => new DFunctionResult
                    {
                        Index = m.Index,
                        Length = m.Length,
                        Value = r
                    }).ToList();


                functions.Add(result);
            }

            return functions;
        }

        private static List<DFunctionResult[]> ToStepFunctions(IList<List<DFunctionResult>> functions)
        {
            var steps = new List<DFunctionResult[]>();

            for (var f = 0; f < functions.Count; f++)
            {
                for (var s = 0; s < functions[f].Count; s++)
                {
                    if (s >= steps.Count)
                        steps.Add(new DFunctionResult[functions.Count]);
                    steps[s][f] = functions[f][s];
                }
            }

            return steps;
        }

        private static void RepeatFill(IList<DFunctionResult[]> steps)
        {
            var repeatStepStart = -1;
            var functionStepLengths = new int[steps[0].Length];

            for (var s = 0; s < steps.Count; s++)
            {
                for (var f = 0; f < steps[s].Length; f++)
                {
                    if (steps[s][f] == null)
                    {
                        if (repeatStepStart == -1)
                            repeatStepStart = s;

                        var rs = (s + 1).Repeat(functionStepLengths[f]) - 1;

                        steps[s][f] = steps[rs][f];
                    }
                    else
                    {
                        if (repeatStepStart == -1)
                            functionStepLengths[f] += 1;
                    }
                }
            }
        }

        private static IEnumerable<string> ApplySteps(string formatString, IEnumerable<DFunctionResult[]> steps)
        {
            return (
                from step in steps
                let stepResult = new DStep { Offset = 0, Result = (string)formatString.Clone() }
                select step.Aggregate(stepResult, (current, fr) =>
                {
                    current.Result = current.Result.ReplaceAt(
                        fr.Index - current.Offset, fr.Length, fr.Value);
                    current.Offset += fr.Length - fr.Value.Length;

                    return current;
                })
            ).Select(s => s.Result);
        }

        #endregion

        /// <summary>
        /// Creates a HashTable based on current object state.
        /// <remarks>Copied from the MVCToolkit HtmlExtensionUtility class</remarks>
        /// </summary>
        /// <param name="properties">The object from which to get the properties</param>
        /// <returns>A <see cref="Hashtable"/> containing the object instance's property names and their values</returns>
        private static Hashtable GetPropertyHash(object properties)
        {
            if (properties == null) return null;

            var values = new Hashtable();
            var props = TypeDescriptor.GetProperties(properties);

            foreach (PropertyDescriptor prop in props)
            {
                values.Add(prop.Name, prop.GetValue(properties));
            }

            return values;
        }

    }
}
