// Copyright (C)2024 Nick Kastellanos

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.XR;

namespace Microsoft.Xna.Platform.XR
{
    public interface IPlatformXRDevice
    {
        T GetStrategy<T>() where T : XRDeviceStrategy;
    }

    public abstract class XRDeviceStrategy : IDisposable
    {
        protected HandJointCollection[] _handJoints = new HandJointCollection[2];

        public abstract bool IsVRSupported { get; }
        public abstract bool IsARSupported { get; }
        public abstract XRSessionMode SessionMode { get; }
        public abstract XRDeviceState DeviceState { get; }
        public abstract bool IsTrackFloorLevelEnabled { get; }

        protected void Initialize()
        {
            this._handJoints = new HandJointCollection[2];
            this._handJoints[0] = new HandJointCollection(this, 0);
            this._handJoints[1] = new HandJointCollection(this, 1);
        }

        public abstract int BeginSessionAsync(XRSessionMode sessionMode);
        public abstract int BeginFrame();
        public abstract HeadsetState GetHeadsetState();
        public abstract IEnumerable<XREye> GetEyes();
        public abstract RenderTarget2D GetEyeRenderTarget(XREye eye);
        public abstract Matrix CreateProjection(XREye eye, float znear, float zfar);
        public abstract void CommitRenderTarget(XREye eye, RenderTarget2D rt);
        public abstract int EndFrame();
        public abstract HandsState GetHandsState();
        public abstract void EndSessionAsync();
        public abstract void TrackFloorLevelAsync(bool enable);
        public abstract HandJointCollectionStrategy CreateHandJointCollectionStrategy(int handIndex);
        public abstract HandJointCollection GetHandJoints(int handIndex);

        public abstract bool IsSceneSupported { get; }
        public abstract RoomInfo? RoomInfo { get; }
        public abstract void QueryRoomInfoAsync();

        #region IDisposable
        ~XRDeviceStrategy()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
        #endregion
    }
}