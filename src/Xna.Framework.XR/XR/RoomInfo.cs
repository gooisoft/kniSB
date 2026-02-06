// Copyright (C)2024 Nick Kastellanos

using System;

namespace Microsoft.Xna.Framework.XR
{
    /// <summary>
    /// Physical room dimensions from XR scene scanning, all in meters.
    /// </summary>
    public struct RoomInfo
    {
        /// <summary>Width of the room in meters (X axis).</summary>
        public float Width;

        /// <summary>Depth of the room in meters (Z axis).</summary>
        public float Depth;

        /// <summary>Height of the room in meters (Y axis).</summary>
        public float Height;

        /// <summary>Number of walls detected.</summary>
        public int WallCount;

        /// <summary>Whether this data is valid (a room was found and queried).</summary>
        public bool IsValid;
    }
}
