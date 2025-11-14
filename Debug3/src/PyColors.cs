using System.Collections.Generic;
using UnityEngine;

namespace Debug3
{
    /// <summary>
    /// Python colors namespace object that provides predefined colors and custom() method
    /// Usage: colors.red, colors.blue, colors.custom("#ff00ff")
    /// </summary>
    public class PyColors : IPyObject
    {
        private readonly Dictionary<string, IPyObject> members;

        public PyColors()
        {
            // Initialize predefined colors
            var red = new PyColor(Color.red, "red");
            var green = new PyColor(Color.green, "green");
            var blue = new PyColor(Color.blue, "blue");
            var yellow = new PyColor(Color.yellow, "yellow");
            var cyan = new PyColor(Color.cyan, "cyan");
            var magenta = new PyColor(Color.magenta, "magenta");
            var white = new PyColor(Color.white, "white");
            var black = new PyColor(Color.black, "black");
            var orange = new PyColor(new Color(1f, 0.5f, 0f), "orange");
            var purple = new PyColor(new Color(0.5f, 0f, 0.5f), "purple");
            var pink = new PyColor(new Color(1f, 0.75f, 0.8f), "pink");
            var lime = new PyColor(new Color(0.75f, 1f, 0f), "lime");
            var teal = new PyColor(new Color(0f, 0.5f, 0.5f), "teal");
            var navy = new PyColor(new Color(0f, 0f, 0.5f), "navy");
            var maroon = new PyColor(new Color(0.5f, 0f, 0f), "maroon");
            var olive = new PyColor(new Color(0.5f, 0.5f, 0f), "olive");
            var silver = new PyColor(new Color(0.75f, 0.75f, 0.75f), "silver");
            var gray = new PyColor(new Color(0.5f, 0.5f, 0.5f), "gray");

            // Build members dictionary
            members = new Dictionary<string, IPyObject>
            {
                { "red", red },
                { "green", green },
                { "blue", blue },
                { "yellow", yellow },
                { "cyan", cyan },
                { "magenta", magenta },
                { "white", white },
                { "black", black },
                { "orange", orange },
                { "purple", purple },
                { "pink", pink },
                { "lime", lime },
                { "teal", teal },
                { "navy", navy },
                { "maroon", maroon },
                { "olive", olive },
                { "silver", silver },
                { "gray", gray },
                { "custom", new PyColorsCustomFunction() }
            };
        }

        public string GetTypeName()
        {
            return "colors";
        }

        public override string ToString()
        {
            return "<colors namespace>";
        }

        public IPyObject DeepCopy(Dictionary<object, object> memo)
        {
            return this; // Colors namespace is a singleton
        }

        public int Size()
        {
            return 1;
        }

        // Helper method for patches to access members
        public bool TryGetMember(string name, out IPyObject value)
        {
            return members.TryGetValue(name, out value);
        }
    }

    /// <summary>
    /// Wrapper for colors.custom() function
    /// </summary>
    public class PyColorsCustomFunction : PyFunction
    {
        public PyColorsCustomFunction() : base(
            "custom",
            CustomBinding,
            null,
            false)
        {
        }

        private static double CustomBinding(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
        {
            if (parameters.Count != 1 || !(parameters[0] is PyString hexString))
            {
                exec.States[droneId].ReturnValue = new PyNone();
                return 0.0;
            }

            var color = new PyColor(hexString.str);
            exec.States[droneId].ReturnValue = color;
            return 0.0;
        }
    }
}
