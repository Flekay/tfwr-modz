namespace Directions
{
    /// <summary>
    /// Enum representing different types of direction commands
    /// </summary>
    public enum DirectionType
    {
        // Absolute directions (grid-based)
        North,
        East,
        South,
        West,
        
        // Relative rotation
        Left,    // Rotate 90° counter-clockwise
        Right,   // Rotate 90° clockwise
        
        // Relative movement
        Forward,  // Move in current facing direction
        Backward, // Move opposite to current facing direction
        
        // Vertical movement
        Up,      // Move visually up by 0.3f
        Down     // Move visually down by 0.3f
    }
}
