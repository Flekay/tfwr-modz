using System.Collections.Generic;
using UnityEngine;

namespace Debug3
{
    /// <summary>
    /// Represents colored text for use with quick_print()
    /// Created by colors.red.wrap("text")
    /// </summary>
    public class PyColoredText : IPyObject
    {
        public readonly string text;
        public readonly Color color;

        public PyColoredText(string text, Color color)
        {
            this.text = text;
            this.color = color;
        }

        public string GetTypeName()
        {
            return "ColoredText";
        }

        public override string ToString()
        {
            return text;
        }

        // Returns text with Unity rich text color tags
        public string ToColoredString()
        {
            string hexColor = ColorUtility.ToHtmlStringRGB(color);
            return $"<color=#{hexColor}>{text}</color>";
        }

        public override bool Equals(object obj)
        {
            if (obj is PyColoredText other)
            {
                return text == other.text && color.Equals(other.color);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return text.GetHashCode() ^ color.GetHashCode();
        }

        public IPyObject DeepCopy(Dictionary<object, object> memo)
        {
            return this; // Immutable
        }

        public int Size()
        {
            return 1;
        }
    }
}
