using System;
using Microsoft.Xna.Platform.XR;

namespace Microsoft.Xna.Framework.XR
{
    public interface IPlatformHandJointCollectionStrategy
    {
        HandJointCollectionStrategy Strategy { get; }
    }

    public abstract class HandJointCollectionStrategy
    {
        protected readonly XRDeviceStrategy _xrDeviceStrategy;

        protected readonly int _handIndex;

        protected HandJointState[] _states;

        /// <summary>
        /// Indicates whether the hand is currently being tracked by the cameras.
        /// When false, joint data may be stale and should not be used for gesture detection.
        /// </summary>
        protected bool _isActive;

        protected HandJointCollectionStrategy(int handIndex)
        {
            _handIndex = handIndex;
            _isActive = false;

            _states = new HandJointState[Enum.GetValues(typeof(XRHandJoint)).Length];
        }

        public int Length { get { return _states.Length; } }

        /// <summary>
        /// Indicates whether the hand is currently being tracked by the cameras.
        /// When false, joint data may be stale and should not be used for gesture detection.
        /// </summary>
        public bool IsActive { get { return _isActive; } }

        public HandJointState this[XRHandJoint handJoint]
        {
            get { return _states[(int)handJoint]; }
        }

        public HandJointState this[int index]
        {
            get { return _states[index]; }
        }

        public T ToConcrete<T>() where T : HandJointCollectionStrategy
        {
            return (T)this;
        }
    }
}
