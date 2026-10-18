// Copyright (C)2024 Nick Kastellanos

using System;

namespace Microsoft.Xna.Framework.XR
{
    /// <summary>
    /// Semantic classification of a detected plane surface.
    /// </summary>
    public enum PlaneSemanticType
    {
        Unknown = 0,
        Ceiling = 1,
        Floor = 2,
        Wall = 3,
        Platform = 4
    }

    /// <summary>
    /// A plane detected by XR plane tracking, with position, orientation, extents, and semantic type.
    /// All spatial values are in meters relative to the XR stage space.
    /// </summary>
    public struct DetectedPlane
    {
        /// <summary>Center position of the plane in meters.</summary>
        public Vector3 Position;

        /// <summary>Orientation of the plane.</summary>
        public Quaternion Orientation;

        /// <summary>Width of the plane in meters (extent along local X axis).</summary>
        public float Width;

        /// <summary>Height of the plane in meters (extent along local Y axis).</summary>
        public float Height;

        /// <summary>Semantic classification: wall, floor, ceiling, etc.</summary>
        public PlaneSemanticType SemanticType;

        /// <summary>Unique identifier for this plane (persists across frames).</summary>
        public ulong PlaneId;
    }
}
