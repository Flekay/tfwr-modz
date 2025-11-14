using System;
using System.Collections.Generic;
using UnityEngine;

namespace Debug3
{
    /// <summary>
    /// Python color object that can be used with arrow() function and other debug visualizations
    /// Supports wrap() method: colors.red.wrap("text") returns colored text
    /// </summary>
    public class PyColor : IPyObject
    {
        public readonly Color color;
        public readonly string name;
        private readonly PyColorWrapFunction wrapFunction;

        public PyColor(Color color, string name = null)
        {
            this.color = color;
            this.name = name ?? $"#{ColorUtility.ToHtmlStringRGB(color)}";
            this.wrapFunction = new PyColorWrapFunction(this);
        }

        public PyColor(string hexColor)
        {
            // Try parsing with Unity's ColorUtility (supports #RRGGBB format)
            if (ColorUtility.TryParseHtmlString(hexColor, out Color parsedColor))
            {
                this.color = parsedColor;
                this.name = hexColor;
            }
            else
            {
                // Fallback to white if parsing fails
                this.color = Color.white;
                this.name = "#FFFFFF";
            }
            this.wrapFunction = new PyColorWrapFunction(this);
        }

        public string GetTypeName()
        {
            return "Color";
        }

        public override string ToString()
        {
            return name;
        }

        public override bool Equals(object obj)
        {
            if (obj is PyColor other)
            {
                return color.Equals(other.color);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return color.GetHashCode();
        }

        public IPyObject DeepCopy(Dictionary<object, object> memo)
        {
            return this; // Colors are immutable
        }

        public int Size()
        {
            return 1;
        }

        // Helper method for accessing wrap function
        public bool TryGetMember(string name, out IPyObject value)
        {
            if (name == "wrap")
            {
                value = wrapFunction;
                return true;
            }
            value = null;
            return false;
        }

        // Implicit conversion to Unity Color
        public static implicit operator Color(PyColor pyColor)
        {
            return pyColor.color;
        }
    }

    /// <summary>
    /// Wrapper for color.wrap() method
    /// Usage: colors.red.wrap("text")
    /// </summary>
    public class PyColorWrapFunction : PyFunction
    {
        private readonly PyColor parentColor;

        public PyColorWrapFunction(PyColor parentColor) : base(
            "wrap",
            null,
            null,
            true)  // Free function - no unlock required
        {
            this.parentColor = parentColor;
            this.binding = WrapBinding;
        }

        private double WrapBinding(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
        {
            // When called as colors.red.wrap("text"), parameters are: [PyColor, "text"]
            // We need to handle both cases: direct call wrap("text") and method call colors.red.wrap("text")
            PyString text = null;

            if (parameters.Count == 1 && parameters[0] is PyString)
            {
                // Direct call: wrap("text")
                text = (PyString)parameters[0];
            }
            else if (parameters.Count == 2 && parameters[0] is PyColor && parameters[1] is PyString)
            {
                // Method call: colors.red.wrap("text") - first param is self (PyColor), second is the string
                text = (PyString)parameters[1];
            }

            if (text == null)
            {
                exec.States[droneId].ReturnValue = new PyNone();
                return 0.0;
            }

            var coloredText = new PyColoredText(text.str, parentColor.color);
            exec.States[droneId].ReturnValue = coloredText;
            return 0.0;
        }
    }
}
