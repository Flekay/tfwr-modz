using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace PrettyPrint.Patches
{
    [HarmonyPatch(typeof(BuiltinFunctions))]
    public static class BuiltinFunctionsPatch
    {
        private static bool hasAddedFunction = false;

        [HarmonyPatch("Functions", MethodType.Getter)]
        [HarmonyPrefix]
        public static void Functions_Prefix()
        {
            if (hasAddedFunction) return;

            var functionListField = typeof(BuiltinFunctions).GetField("functionList",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (functionListField?.GetValue(null) is List<PyFunction> functionList)
            {
                if (!functionList.Any(f => f.functionName == "pretty_print"))
                {
                    functionList.Add(new PyFunction(
                        "pretty_print",
                        new Func<List<IPyObject>, Simulation, Execution, int, double>(PrettyPrint),
                        null,
                        false // Will be unlocked via Farm.startUnlocks instead
                    ));

                    hasAddedFunction = true;
                }
            }
        }

        private static double PrettyPrint(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
        {
            if (parameters.Count == 0)
            {
                throw new ExecuteException("pretty_print() requires at least one argument", -1, -1);
            }

            StringBuilder output = new StringBuilder();
            foreach (IPyObject param in parameters)
            {
                output.AppendLine(FormatPrettyPrint(param, 0, null));
            }

            Logger.Log(output.ToString().TrimEnd());
            exec.States[droneId].ReturnValue = new PyNone();
            return 0.0;
        }

        private static string FormatPrettyPrint(object obj, int depth, HashSet<object> loopDetector)
        {
            const int indentSize = 2;
            string indent = new string(' ', depth * indentSize);
            string nextIndent = new string(' ', (depth + 1) * indentSize);

            if (depth > 100)
            {
                return indent + "...";
            }

            if (obj == null || obj is PyNone)
            {
                return indent + "None";
            }

            if (loopDetector != null && loopDetector.Contains(obj))
            {
                return indent + "...";
            }

            if (obj is PyDict)
            {
                if (loopDetector == null)
                {
                    loopDetector = new HashSet<object>(new ReferenceComparer());
                }
                loopDetector.Add(obj);

                var dict = ((PyDict)obj).dict;
                if (dict.Count == 0)
                {
                    return indent + "{}";
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine(indent + "{");

                var items = dict.ToList();
                for (int i = 0; i < items.Count; i++)
                {
                    var kvp = items[i];
                    string key = FormatKey(kvp.Key);
                    string value = FormatPrettyPrint(kvp.Value.obj, depth + 1, loopDetector).TrimStart();

                    sb.Append(nextIndent + key + ": " + value);
                    if (i < items.Count - 1)
                    {
                        sb.AppendLine(",");
                    }
                    else
                    {
                        sb.AppendLine();
                    }
                }

                sb.Append(indent + "}");
                return sb.ToString();
            }

            if (obj is PyList)
            {
                if (loopDetector == null)
                {
                    loopDetector = new HashSet<object>(new ReferenceComparer());
                }
                loopDetector.Add(obj);

                var list = (PyList)obj;
                if (list.Count == 0)
                {
                    return indent + "[]";
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine(indent + "[");

                for (int i = 0; i < list.Count; i++)
                {
                    string value = FormatPrettyPrint(list[i], depth + 1, loopDetector).TrimStart();
                    sb.Append(nextIndent + value);
                    if (i < list.Count - 1)
                    {
                        sb.AppendLine(",");
                    }
                    else
                    {
                        sb.AppendLine();
                    }
                }

                sb.Append(indent + "]");
                return sb.ToString();
            }

            if (obj is PySet)
            {
                var set = (PySet)obj;
                if (set.Count == 0)
                {
                    return indent + "set()";
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine(indent + "{");

                var items = set.ToList();
                for (int i = 0; i < items.Count; i++)
                {
                    string value = FormatPrettyPrint(items[i], depth + 1, loopDetector).TrimStart();
                    sb.Append(nextIndent + value);
                    if (i < items.Count - 1)
                    {
                        sb.AppendLine(",");
                    }
                    else
                    {
                        sb.AppendLine();
                    }
                }

                sb.Append(indent + "}");
                return sb.ToString();
            }

            if (obj is PyTuple)
            {
                var tuple = (PyTuple)obj;
                if (tuple.Count == 0)
                {
                    return indent + "()";
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine(indent + "(");

                for (int i = 0; i < tuple.Count; i++)
                {
                    string value = FormatPrettyPrint(tuple[i], depth + 1, loopDetector).TrimStart();
                    sb.Append(nextIndent + value);
                    if (i < tuple.Count - 1)
                    {
                        sb.AppendLine(",");
                    }
                    else
                    {
                        sb.AppendLine();
                    }
                }

                sb.Append(indent + ")");
                return sb.ToString();
            }

            // For primitives and other types, use simple string representation
            if (obj is PyString)
            {
                return indent + "\"" + ((PyString)obj).str + "\"";
            }

            if (obj is PyNumber)
            {
                return indent + ((PyNumber)obj).num.ToString();
            }

            if (obj is PyBool)
            {
                return indent + (((PyBool)obj).num != 0.0 ? "True" : "False");
            }

            // Fallback to CodeUtilities.ToNiceString for unknown types
            try
            {
                var method = typeof(CodeUtilities).GetMethod("ToNiceString", BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                {
                    return indent + (string)method.Invoke(null, new object[] { obj, 0, null, false });
                }
            }
            catch { }

            return indent + obj.ToString();
        }

        private static string FormatKey(IPyObject key)
        {
            if (key is PyString)
            {
                return "\"" + ((PyString)key).str + "\"";
            }
            if (key is PyNumber)
            {
                return ((PyNumber)key).num.ToString();
            }
            return key.ToString();
        }

        // Helper class for reference comparison
        private class ReferenceComparer : IEqualityComparer<object>
        {
            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
