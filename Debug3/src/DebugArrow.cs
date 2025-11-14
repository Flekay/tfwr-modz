using UnityEngine;

namespace Debug3
{
    public struct DebugArrow
    {
        public int x;
        public int y;
        public bool hasDirection;
        public int direction;  // GridDirection enum value
        public Color color;

        public DebugArrow(int x, int y, bool hasDirection = false, int direction = 0, Color? color = null)
        {
            this.x = x;
            this.y = y;
            this.hasDirection = hasDirection;
            this.direction = direction;
            this.color = color.HasValue ? color.Value : Color.white;
        }
    }
}
