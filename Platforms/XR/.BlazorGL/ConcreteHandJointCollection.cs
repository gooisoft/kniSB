using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.XR;
using nkast.Wasm.XR;

namespace Microsoft.Xna.Platform.XR
{
    internal sealed class ConcreteHandJointCollection : HandJointCollectionStrategy
    {
        private XRJointSpace[] _jointSpaces;
        private System.Numerics.Matrix4x4[] _jointTransforms;
        private float[] _jointRadii;

        internal ConcreteHandJointCollection(int handIndex) : base(handIndex)
        {
            int webXRJointCount = 25;
            _jointSpaces = new XRJointSpace[webXRJointCount];
            _jointTransforms = new System.Numerics.Matrix4x4[webXRJointCount];
            _jointRadii = new float[webXRJointCount];
        }

        internal void SetHandJoints(XRFrame currentXRFrame, XRHand xrHand, XRReferenceSpace referenceSpace)
        {
            for (int i = 0; i < _jointSpaces.Length; i++)
            {
                if (_jointSpaces[i] == null)
                    _jointSpaces[i] = xrHand[(nkast.Wasm.XR.XRHandJoint)i];
            }

            bool filledPoses = currentXRFrame.FillPoses(_jointSpaces, referenceSpace, _jointTransforms);
            currentXRFrame.FillJointRadii(_jointSpaces, _jointRadii);

            // Set IsActive based on whether we got valid pose data
            _isActive = filledPoses;

            for (int i = 0; i < _jointSpaces.Length; i++)
            {
                if (i == (int)Framework.XR.XRHandJoint.Palm)
                {
                    HandJointState emulatedPalmJointState = EmulatePalmJointState();
                    _states[i] = emulatedPalmJointState;
                    continue;
                }

                int webXRHandJointIndex = i - 1;
                Matrix transform = (Matrix)_jointTransforms[webXRHandJointIndex];
                _states[i].Pose = new Pose3(Quaternion.CreateFromRotationMatrix(transform), transform.Translation);
                _states[i].Radius = _jointRadii[webXRHandJointIndex];
            }
        }

        private HandJointState EmulatePalmJointState()
        {
            int middleFingerMetacarpalIndex = (int)nkast.Wasm.XR.XRHandJoint.Middle_Finger_Metacarpal;
            Matrix middleFingerMetacarpalTransform = (Matrix)_jointTransforms[middleFingerMetacarpalIndex];
            float middleFingerMetacarpalRadius = _jointRadii[middleFingerMetacarpalIndex];

            int middleFingerProximalIndex = (int)nkast.Wasm.XR.XRHandJoint.Middle_Finger_Phalanx_Proximal;
            Matrix middleFingerProximalTransform = (Matrix)_jointTransforms[middleFingerProximalIndex];
            float middleFingerProximalRadius = _jointRadii[middleFingerProximalIndex];

            Vector3 palmPosition = Vector3.Lerp(middleFingerMetacarpalTransform.Translation,
                middleFingerProximalTransform.Translation, 0.5f);
            Quaternion palmOrientation = Quaternion.CreateFromRotationMatrix(middleFingerMetacarpalTransform);
            float palmRadius = MathHelper.Lerp(middleFingerMetacarpalRadius, middleFingerProximalRadius, 0.5f);

            HandJointState palmJointState = new HandJointState();
            palmJointState.Pose = new Pose3(palmOrientation, palmPosition);
            palmJointState.Radius = palmRadius;
            return palmJointState;
        }
    }
}
