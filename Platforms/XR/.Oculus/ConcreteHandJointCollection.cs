using System;
using System.Diagnostics;
using Microsoft.Xna.Framework.XR;
using nkast.LibOXR;
using Silk.NET.OpenXR;

namespace Microsoft.Xna.Platform.XR
{
    internal sealed class ConcreteHandJointCollection : HandJointCollectionStrategy
    {
        private unsafe delegate Result xrCreateHandTrackerEXT(Session xrSession, HandTrackerCreateInfoEXT* handTrackerCreateInfo,
            HandTrackerEXT* handTracker);
        private unsafe delegate Result xrLocateHandJointsEXT(HandTrackerEXT handTracker, HandJointsLocateInfoEXT* handJointsLocateInfo,
            HandJointLocationsEXT* handJointLocations);

        private static xrCreateHandTrackerEXT _pfnCreateHandTrackerEXT;
        private static xrLocateHandJointsEXT _pfnLocateHandJointsEXT;

        private HandTrackerEXT _handTracker;
        private HandJointLocationEXT[] _jointLocations;
        private HandJointVelocityEXT[] _jointVelocities;

        private bool _initialized;

        internal unsafe ConcreteHandJointCollection(int handIndex) : base(handIndex)
        {
            int jointCount = 26;
            _jointLocations = new HandJointLocationEXT[jointCount];
            _jointVelocities = new HandJointVelocityEXT[jointCount];
        }

        internal unsafe void Initialize(OxrInstance oxrInstance, Session session)
        {
            if (_pfnCreateHandTrackerEXT == null)
            {
                Result xrResult = oxrInstance.GetInstanceProcAddr<xrCreateHandTrackerEXT>("xrCreateHandTrackerEXT",
                    out _pfnCreateHandTrackerEXT);
                Debug.Assert(xrResult == Result.Success, "xrCreateHandTrackerEXT");
            }

            if (_pfnLocateHandJointsEXT == null)
            {
                Result xrResult = oxrInstance.GetInstanceProcAddr<xrLocateHandJointsEXT>("xrLocateHandJointsEXT",
                    out _pfnLocateHandJointsEXT);
                Debug.Assert(xrResult == Result.Success, "xrLocateHandJointsEXT");
            }

            HandTrackerCreateInfoEXT handTrackerCreateInfo = new HandTrackerCreateInfoEXT(StructureType.HandTrackerCreateInfoExt);
            handTrackerCreateInfo.Hand = _handIndex == 0 ? HandEXT.LeftExt : HandEXT.RightExt;
            handTrackerCreateInfo.HandJointSet = HandJointSetEXT.DefaultExt;
            _handTracker = new HandTrackerEXT();
            fixed (HandTrackerEXT* pHandTracker = &_handTracker)
            {
                Result xrResult = _pfnCreateHandTrackerEXT(session, &handTrackerCreateInfo, pHandTracker);
                Debug.Assert(xrResult == Result.Success, "pfnCreateHandTrackerEXT");
            }

            _initialized = true;
        }

        internal unsafe void SetHandJoints(OxrInstance oxrInstance, Session session, Space space, long time)
        {
            if (!_initialized)
                Initialize(oxrInstance, session);

            HandJointLocationsEXT handJointLocations = new HandJointLocationsEXT(StructureType.HandJointLocationsExt);
            handJointLocations.JointCount = (uint)_jointLocations.Length;

            HandJointVelocitiesEXT handJointVelocities = new HandJointVelocitiesEXT(StructureType.HandJointVelocitiesExt);
            handJointVelocities.JointCount = (uint)_jointVelocities.Length;

            fixed (HandJointLocationEXT* pJointLocations = _jointLocations)
            {
                fixed (HandJointVelocityEXT* pJointVelocities = _jointVelocities)
                {
                    handJointLocations.JointLocations = pJointLocations;
                    handJointLocations.Next = &handJointVelocities;
                    handJointVelocities.JointVelocities = pJointVelocities;

                    HandJointsLocateInfoEXT handJointsLocateInfo = new HandJointsLocateInfoEXT(StructureType.HandJointsLocateInfoExt);
                    handJointsLocateInfo.BaseSpace = space;
                    handJointsLocateInfo.Time = time;
                    Result xrResult = _pfnLocateHandJointsEXT(_handTracker, &handJointsLocateInfo, &handJointLocations);

                    bool wasActive = _isActive;
                    _isActive = (handJointLocations.IsActive == 1);
                    System.Diagnostics.Trace.WriteLine($"[KNISB] Hand {_handIndex} IsActive={_isActive} (raw={handJointLocations.IsActive})");
                    if (wasActive != _isActive)
                    {
                        System.Diagnostics.Trace.WriteLine($"[KNISB] Hand {_handIndex} IsActive CHANGED: {wasActive} -> {_isActive}");
                    }
                    if (_isActive)
                    {
                        for (int i = 0; i < _jointLocations.Length; i++)
                        {
                            _states[i].Pose = _jointLocations[i].Pose.ToPose3();
                            _states[i].Radius = _jointLocations[i].Radius;
                        }
                    }
                }
            }
        }
    }
}
