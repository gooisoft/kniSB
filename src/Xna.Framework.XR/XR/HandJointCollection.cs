using System;
using Microsoft.Xna.Platform.XR;

namespace Microsoft.Xna.Framework.XR
{
    public sealed class HandJointCollection
       : IPlatformHandJointCollectionStrategy
    {
        private HandJointCollectionStrategy _strategy;

        HandJointCollectionStrategy IPlatformHandJointCollectionStrategy.Strategy { get { return _strategy; } }

        internal HandJointCollection(XRDeviceStrategy xrDeviceStrategy, int handIndex)
        {
            _strategy = xrDeviceStrategy.CreateHandJointCollectionStrategy(handIndex);
        }

        public int Length { get { return _strategy.Length; } }

        /// <summary>
        /// Indicates whether the hand is currently being tracked by the cameras.
        /// When false, joint data may be stale and should not be used for gesture detection.
        /// </summary>
        public bool IsActive { get { return _strategy.IsActive; } }

        public HandJointState this[XRHandJoint handJoint]
        {
            get { return _strategy[handJoint]; }
        }

        public HandJointState this[int index]
        {
            get { return _strategy[index]; }
        }
    }
}
