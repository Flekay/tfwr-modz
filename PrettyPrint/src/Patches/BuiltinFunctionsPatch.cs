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
                if (!functionList.Any(f => f.functionName == "pprint"))
                {
                    functionList.Add(new PyFunction(
                        "pprint",
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
                throw new ExecuteException("pprint() requires at least one argument", -1, -1);
            }

            StringBuilder output = new StringBuilder();
            foreach (IPyObject param in parameters)
            {
                output.AppendLine(FormatPrettyPrint(param, 0, null, 80));
            }

            Logger.Log(output.ToString().TrimEnd());
            exec.States[droneId].ReturnValue = new PyNone();
            return 0.0;
        }

        private static string FormatPrettyPrint(object obj, int depth, HashSet<object> loopDetector, int width)
        {
            string indent = new string('\t', depth);
            string nextIndent = new string('\t', depth + 1);

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
                HashSet<object> currentLoopDetector = loopDetector;
                if (currentLoopDetector == null)
                {
                    currentLoopDetector = new HashSet<object>(new ReferenceComparer());
                }
                currentLoopDetector.Add(obj);

                var dict = ((PyDict)obj).dict;
                if (dict.Count == 0)
                {
                    return indent + "{}";
                }

                // Dicts are always expanded (multiline)
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(indent + "{");

                var items = dict.ToList();
                for (int i = 0; i < items.Count; i++)
                {
                    var kvp = items[i];
                    string key = FormatKeyInline(kvp.Key);
                    string value = FormatInline(kvp.Value.obj);

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
                HashSet<object> currentLoopDetector = loopDetector;
                if (currentLoopDetector == null)
                {
                    currentLoopDetector = new HashSet<object>(new ReferenceComparer());
                }
                currentLoopDetector.Add(obj);

                var list = (PyList)obj;
                if (list.Count == 0)
                {
                    return indent + "[]";
                }

                StringBuilder sb = new StringBuilder();
                sb.Append(indent + "[");

                // Check if we can fit items on one line
                StringBuilder testLine = new StringBuilder();
                for (int i = 0; i < list.Count; i++)
                {
                    testLine.Append(FormatInline(list[i]));
                    if (i < list.Count - 1)
                    {
                        testLine.Append(", ");
                    }
                }

                int availableWidth = width - indent.Length - 2;
                if (testLine.Length <= availableWidth)
                {
                    sb.Append(testLine.ToString());
                    sb.Append("]");
                    return sb.ToString();
                }
                else
                {
                    sb.AppendLine();
                    for (int i = 0; i < list.Count; i++)
                    {
                        string value = FormatInline(list[i]);
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
            }

            if (obj is PySet)
            {
                var set = (PySet)obj;
                if (set.Count == 0)
                {
                    return indent + "set()";
                }

                StringBuilder sb = new StringBuilder();
                sb.Append(indent + "{");

                var items = set.ToList();
                StringBuilder testLine = new StringBuilder();
                for (int i = 0; i < items.Count; i++)
                {
                    testLine.Append(FormatInline(items[i]));
                    if (i < items.Count - 1)
                    {
                        testLine.Append(", ");
                    }
                }

                int availableWidth = width - indent.Length - 2;
                if (testLine.Length <= availableWidth)
                {
                    sb.Append(testLine.ToString());
                    sb.Append("}");
                    return sb.ToString();
                }
                else
                {
                    sb.AppendLine();
                    for (int i = 0; i < items.Count; i++)
                    {
                        string value = FormatInline(items[i]);
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
            }

            if (obj is PyTuple)
            {
                var tuple = (PyTuple)obj;
                if (tuple.Count == 0)
                {
                    return indent + "()";
                }

                StringBuilder sb = new StringBuilder();
                sb.Append(indent + "(");

                StringBuilder testLine = new StringBuilder();
                for (int i = 0; i < tuple.Count; i++)
                {
                    testLine.Append(FormatInline(tuple[i]));
                    if (i < tuple.Count - 1)
                    {
                        testLine.Append(", ");
                    }
                }

                int availableWidth = width - indent.Length - 2;
                if (testLine.Length <= availableWidth)
                {
                    sb.Append(testLine.ToString());
                    sb.Append(")");
                    return sb.ToString();
                }
                else
                {
                    sb.AppendLine();
                    for (int i = 0; i < tuple.Count; i++)
                    {
                        string value = FormatInline(tuple[i]);
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

        private static string FormatInline(object obj)
        {
            if (obj == null || obj is PyNone)
            {
                return "None";
            }

            if (obj is PyDict)
            {
                var dict = ((PyDict)obj).dict;
                if (dict.Count == 0)
                {
                    return "{}";
                }

                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                var items = dict.ToList();
                for (int i = 0; i < items.Count; i++)
                {
                    var kvp = items[i];
                    string key = FormatKeyInline(kvp.Key);
                    string value = FormatInline(kvp.Value.obj);
                    sb.Append(key + ": " + value);
                    if (i < items.Count - 1)
                    {
                        sb.Append(", ");
                    }
                }
                sb.Append("}");
                return sb.ToString();
            }

            if (obj is PyList)
            {
                var list = (PyList)obj;
                if (list.Count == 0)
                {
                    return "[]";
                }

                StringBuilder sb = new StringBuilder();
                sb.Append("[");
                for (int i = 0; i < list.Count; i++)
                {
                    sb.Append(FormatInline(list[i]));
                    if (i < list.Count - 1)
                    {
                        sb.Append(", ");
                    }
                }
                sb.Append("]");
                return sb.ToString();
            }

            if (obj is PySet)
            {
                var set = (PySet)obj;
                if (set.Count == 0)
                {
                    return "set()";
                }

                StringBuilder sb = new StringBuilder();
                sb.Append("{");
                var items = set.ToList();
                for (int i = 0; i < items.Count; i++)
                {
                    sb.Append(FormatInline(items[i]));
                    if (i < items.Count - 1)
                    {
                        sb.Append(", ");
                    }
                }
                sb.Append("}");
                return sb.ToString();
            }

            if (obj is PyTuple)
            {
                var tuple = (PyTuple)obj;
                if (tuple.Count == 0)
                {
                    return "()";
                }

                StringBuilder sb = new StringBuilder();
                sb.Append("(");
                for (int i = 0; i < tuple.Count; i++)
                {
                    sb.Append(FormatInline(tuple[i]));
                    if (i < tuple.Count - 1)
                    {
                        sb.Append(", ");
                    }
                }
                sb.Append(")");
                return sb.ToString();
            }

            if (obj is PyString)
            {
                return "\"" + ((PyString)obj).str + "\"";
            }

            if (obj is PyNumber)
            {
                return ((PyNumber)obj).num.ToString();
            }

            if (obj is PyBool)
            {
                return ((PyBool)obj).num != 0.0 ? "True" : "False";
            }

            try
            {
                var method = typeof(CodeUtilities).GetMethod("ToNiceString", BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                {
                    return (string)method.Invoke(null, new object[] { obj, 0, null, false });
                }
            }
            catch { }

            return obj.ToString();
        }

        private static string FormatKeyInline(IPyObject key)
        {
            if (key is PyString)
            {
                return "\"" + ((PyString)key).str + "\"";
            }
            if (key is PyNumber)
            {
                return ((PyNumber)key).num.ToString();
            }
            if (key is PyTuple)
            {
                return FormatInline(key);
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
