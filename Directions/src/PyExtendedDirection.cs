using System;
using System.Collections.Generic;

namespace Directions
{
    /// <summary>
    /// Custom Python object representing extended direction commands
    /// Wraps both absolute grid directions and relative movement/rotation commands
    /// </summary>
    public class PyExtendedDirection : IPyObject
    {
        public readonly DirectionType directionType;
        public readonly GridDirection? gridDirection; // Only set for North/East/South/West

        public PyExtendedDirection(DirectionType type, GridDirection? grid = null)
        {
            directionType = type;
            gridDirection = grid;
        }

        public string GetTypeName()
        {
            return "Direction";
        }

        public override string ToString()
        {
            return directionType.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is PyExtendedDirection other)
            {
                return directionType == other.directionType;
            }
            // Also allow comparison with PyGridDirection for backward compatibility
            if (obj is PyGridDirection gridDir && gridDirection.HasValue)
            {
                return gridDirection.Value == (GridDirection)gridDir;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return directionType.GetHashCode();
        }

        // Implement IPyObject.DeepCopy - directions are immutable, so return this
        public IPyObject DeepCopy(Dictionary<object, object> memo)
        {
            return this;
        }

        // Implement IPyObject.Size - directions are single values
        public int Size()
        {
            return 1;
        }

        // Allow implicit conversion to GridDirection for absolute directions
        public static implicit operator GridDirection?(PyExtendedDirection dir)
        {
            return dir.gridDirection;
        }
    }
}
