// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2023 Nick Kastellanos

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Platform.Graphics.OpenGL;


namespace Microsoft.Xna.Platform.Graphics
{
    internal class ConcreteTexture : GraphicsResourceStrategy, ITextureStrategy
    {
        private readonly GraphicsContextStrategy _contextStrategy;
        private readonly SurfaceFormat _format;        
        private readonly int _levelCount;

        internal ConcreteTexture(GraphicsContextStrategy contextStrategy, SurfaceFormat format, int levelCount)
            : base(contextStrategy)
        {
            this._contextStrategy = contextStrategy;
            this._format = format;
            this._levelCount = levelCount;
        }


        #region ITextureStrategy
        public SurfaceFormat Format { get { return _format; } }
        public int LevelCount { get { return _levelCount; } }
        #endregion ITextureStrategy



        internal int _glTexture = -1;
        internal TextureTarget _glTarget;
        internal TextureUnit _glTextureUnit = TextureUnit.Texture0;
        internal PixelInternalFormat _glInternalFormat;
        internal PixelFormat _glFormat;
        internal PixelType _glType;
        internal bool _glIsCompressedTexture;
        internal SamplerState _glLastSamplerState;

        internal static void ToGLSurfaceFormat(SurfaceFormat format,
            GraphicsContextStrategy contextStrategy,
            out PixelInternalFormat glInternalFormat,
            out PixelFormat glFormat,
            out PixelType glType,
            out bool glIsCompressedTexture)
        {
            var GL = contextStrategy.ToConcrete<ConcreteGraphicsContextGL>().GL;

            bool supportsSRgb = contextStrategy.Capabilities.SupportsSRgb;
            bool supportsS3tc = contextStrategy.Capabilities.SupportsS3tc;
            bool supportsPvrtc = contextStrategy.Capabilities.SupportsPvrtc;
            bool supportsEtc1 = contextStrategy.Capabilities.SupportsEtc1;
            bool supportsEtc2 = contextStrategy.Capabilities.SupportsEtc2;
            bool supportsAtitc = contextStrategy.Capabilities.SupportsAtitc;
            bool supportsFloat = contextStrategy.Capabilities.SupportsFloatTextures;
            bool supportsHalfFloat = contextStrategy.Capabilities.SupportsHalfFloatTextures;
            bool supportsNormalized = contextStrategy.Capabilities.SupportsNormalized;
            bool isGLES2 = GL.BoundApi == OGL.RenderApi.ES && ((ConcreteGraphicsContextGL)contextStrategy)._glVersion.Major == 2;

            switch (format)
            {
                case SurfaceFormat.Color:
                    glInternalFormat      = PixelInternalFormat.Rgba;
                    glFormat              = PixelFormat.Rgba;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = false;
                    break;
                case SurfaceFormat.ColorSRgb:
                    if (!supportsSRgb)
                        goto case SurfaceFormat.Color;
                    glInternalFormat      = PixelInternalFormat.Srgb;
                    glFormat              = PixelFormat.Rgba;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = false;
                    break;
                case SurfaceFormat.ColorSRgba:
                    if (!supportsSRgb)
                        goto case SurfaceFormat.Color;
                    glInternalFormat      = PixelInternalFormat.Srgba;
                    glFormat              = PixelFormat.Rgba;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = false;
                    break;

                case SurfaceFormat.Bgr565:
                    glInternalFormat      = PixelInternalFormat.Rgb;
                    glFormat              = PixelFormat.Rgb;
                    glType                = PixelType.UnsignedShort565;
                    glIsCompressedTexture = false;
                    break;
                case SurfaceFormat.Bgra4444:
#if IOS || TVOS || ANDROID
                    glInternalFormat      = PixelInternalFormat.Rgba;
#else
                    glInternalFormat      = PixelInternalFormat.Rgba4;
#endif
                    glFormat              = PixelFormat.Rgba;
                    glType                = PixelType.UnsignedShort4444;
                    glIsCompressedTexture = false;
                    break;
                case SurfaceFormat.Bgra5551:
                    glInternalFormat      = PixelInternalFormat.Rgba;
                    glFormat              = PixelFormat.Rgba;
                    glType                = PixelType.UnsignedShort5551;
                    glIsCompressedTexture = false;
                    break;
                case SurfaceFormat.Alpha8:
                    glInternalFormat      = PixelInternalFormat.Luminance;
                    glFormat              = PixelFormat.Luminance;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = false;
                    break;
                case SurfaceFormat.Dxt1:
                    if (!supportsS3tc) goto case default;
                    glInternalFormat      = PixelInternalFormat.CompressedRgbS3tcDxt1Ext;
                    glFormat              = (PixelFormat)glInternalFormat;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = true;
                    break;
                case SurfaceFormat.Dxt1SRgb:
                    if (!supportsSRgb)
                        goto case SurfaceFormat.Dxt1;
                    glInternalFormat      = PixelInternalFormat.CompressedSrgbS3tcDxt1Ext;
                    glFormat              = (PixelFormat)glInternalFormat;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = true;
                    break;
                case SurfaceFormat.Dxt1a:
                    if (!supportsS3tc) goto case default;
                    glInternalFormat      = PixelInternalFormat.CompressedRgbaS3tcDxt1Ext;
                    glFormat              = (PixelFormat)glInternalFormat;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = true;
                    break;
                case SurfaceFormat.Dxt3:
                    if (!supportsS3tc) goto case default;
                    glInternalFormat      = PixelInternalFormat.CompressedRgbaS3tcDxt3Ext;
                    glFormat              = (PixelFormat)glInternalFormat;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = true;
                    break;
                case SurfaceFormat.Dxt3SRgb:
                    if (!supportsSRgb)
                        goto case SurfaceFormat.Dxt3;
                    glInternalFormat      = PixelInternalFormat.CompressedSrgbAlphaS3tcDxt3Ext;
                    glFormat              = (PixelFormat)glInternalFormat;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = true;
                    break;
                case SurfaceFormat.Dxt5:
                    if (!supportsS3tc) goto case default;
                    glInternalFormat      = PixelInternalFormat.CompressedRgbaS3tcDxt5Ext;
                    glFormat              = (PixelFormat)glInternalFormat;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = true;
                    break;
                case SurfaceFormat.Dxt5SRgb:
                    if (!supportsSRgb)
                        goto case SurfaceFormat.Dxt5;
                    glInternalFormat      = PixelInternalFormat.CompressedSrgbAlphaS3tcDxt5Ext;
                    glFormat              = (PixelFormat)glInternalFormat;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = true;
                    break;
#if !IOS && !TVOS && !ANDROID
                case SurfaceFormat.Rgba1010102:
                    glInternalFormat      = PixelInternalFormat.Rgb10A2ui;
                    glFormat              = PixelFormat.Rgba;
                    glType                = PixelType.UnsignedInt1010102;
                    glIsCompressedTexture = false;
                    break;
#endif

                // float formats
                case SurfaceFormat.HalfSingle:
                    if (!supportsHalfFloat) goto case default;
                    glInternalFormat      = PixelInternalFormat.R16f;
                    glFormat              = PixelFormat.Red;
                    glType                = isGLES2 ? PixelType.HalfFloatOES : PixelType.HalfFloat;
                    glIsCompressedTexture = false;
                    break;
                case SurfaceFormat.Single:
                    if (!supportsFloat) goto case default;
                    glInternalFormat      = PixelInternalFormat.R32f;
                    glFormat              = PixelFormat.Red;
                    glType                = PixelType.Float;
                    glIsCompressedTexture = false;
                    break;
                case SurfaceFormat.HalfVector2:
                    if (!supportsHalfFloat) goto case default;
                    glInternalFormat      = PixelInternalFormat.Rg16f;
                    glFormat              = PixelFormat.Rg;
                    glType                = PixelType.HalfFloat;
                    glIsCompressedTexture = false;
                    break;
                case SurfaceFormat.Vector2:
                    if (!supportsFloat) goto case default;
                    glInternalFormat      = PixelInternalFormat.Rg32f;
                    glFormat              = PixelFormat.Rg;
                    glType                = PixelType.Float;
                    glIsCompressedTexture = false;
                    break;
                // HdrBlendable implemented as HalfVector4 (see http://blogs.msdn.com/b/shawnhar/archive/2010/07/09/surfaceformat-hdrblendable.aspx)
                case SurfaceFormat.HdrBlendable:
                case SurfaceFormat.HalfVector4:
                    if (!supportsHalfFloat) goto case default;
                    glInternalFormat      = PixelInternalFormat.Rgba16f;
                    glFormat              = PixelFormat.Rgba;
                    glType                = PixelType.HalfFloat;
                    glIsCompressedTexture = false;
                    break;
                case SurfaceFormat.Vector4:
                    if (!supportsFloat) goto case default;
                    glInternalFormat      = PixelInternalFormat.Rgba32f;
                    glFormat              = PixelFormat.Rgba;
                    glType                = PixelType.Float;
                    glIsCompressedTexture = false;
                    break;

                case SurfaceFormat.NormalizedByte2:
                    glInternalFormat      = PixelInternalFormat.Rg8i;
                    glFormat              = PixelFormat.Rg;
                    glType                = PixelType.Byte;
                    glIsCompressedTexture = false;
                    break;

                case SurfaceFormat.NormalizedByte4:
                    glInternalFormat      = PixelInternalFormat.Rgba8i;
                    glFormat              = PixelFormat.Rgba;
                    glType                = PixelType.Byte;
                    glIsCompressedTexture = false;
                    break;

                case SurfaceFormat.Rg32:
                    if (!supportsNormalized) goto case default;
                    glInternalFormat      = PixelInternalFormat.Rg16ui;
                    glFormat              = PixelFormat.Rg;
                    glType                = PixelType.UnsignedShort;
                    glIsCompressedTexture = false;
                    break;

                case SurfaceFormat.Rgba64:
                    if (!supportsNormalized) goto case default;
                    glInternalFormat      = PixelInternalFormat.Rgba16;
                    glFormat              = PixelFormat.Rgba;
                    glType                = PixelType.UnsignedShort;
                    glIsCompressedTexture = false;
                    break;
                case SurfaceFormat.RgbaAtcExplicitAlpha:
                    if (!supportsAtitc) goto case default;
                    glInternalFormat      = PixelInternalFormat.AtcRgbaExplicitAlphaAmd;
                    glFormat              = (PixelFormat)glInternalFormat;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = true;
                    break;
                case SurfaceFormat.RgbaAtcInterpolatedAlpha:
                    if (!supportsAtitc) goto case default;
                    glInternalFormat      = PixelInternalFormat.AtcRgbaInterpolatedAlphaAmd;
                    glFormat              = (PixelFormat)glInternalFormat;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = true;
                    break;
                case SurfaceFormat.RgbEtc1:
                    if (!supportsEtc1) goto case default;
                    glInternalFormat      = PixelInternalFormat.Etc1; // GL_ETC1_RGB8_OES
                    glFormat              = (PixelFormat)glInternalFormat;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = true;
                    break;
                case SurfaceFormat.Rgb8Etc2:
                    if (!supportsEtc2) goto case default;
                    glInternalFormat      = PixelInternalFormat.Etc2Rgb8; // GL_COMPRESSED_RGB8_ETC2
                    glFormat              = (PixelFormat)glInternalFormat;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = true;
                    break;
                case SurfaceFormat.Srgb8Etc2:
                    if (!supportsEtc2) goto case default;
                    glInternalFormat      = PixelInternalFormat.Etc2Srgb8; // GL_COMPRESSED_SRGB8_ETC2
                    glFormat              = (PixelFormat)glInternalFormat;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = true;
                    break;
                case SurfaceFormat.Rgb8A1Etc2:
                    if (!supportsEtc2) goto case default;
                    glInternalFormat      = PixelInternalFormat.Etc2Rgb8A1; // GL_COMPRESSED_RGB8_PUNCHTHROUGH_ALPHA1_ETC2
                    glFormat              = (PixelFormat)glInternalFormat;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = true;
                    break;
                case SurfaceFormat.Srgb8A1Etc2:
                    if (!supportsEtc2) goto case default;
                    glInternalFormat      = PixelInternalFormat.Etc2Srgb8A1; // GL_COMPRESSED_SRGB8_PUNCHTHROUGH_ALPHA1_ETC2
                    glFormat              = (PixelFormat)glInternalFormat;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = true;
                    break;
                case SurfaceFormat.Rgba8Etc2:
                    if (!supportsEtc2) goto case default;
                    glInternalFormat      = PixelInternalFormat.Etc2Rgba8Eac; // GL_COMPRESSED_RGBA8_ETC2_EAC
                    glFormat              = (PixelFormat)glInternalFormat;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = true;
                    break;
                case SurfaceFormat.SRgb8A8Etc2:
                    if (!supportsEtc2) goto case default;
                    glInternalFormat      = PixelInternalFormat.Etc2SRgb8A8Eac; // GL_COMPRESSED_SRGB8_ALPHA8_ETC2_EAC
                    glFormat              = (PixelFormat)glInternalFormat;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = true;
                    break;
                case SurfaceFormat.RgbPvrtc2Bpp:
                    if (!supportsPvrtc) goto case default;
                    glInternalFormat      = PixelInternalFormat.CompressedRgbPvrtc2Bppv1Img;
                    glFormat              = (PixelFormat)glInternalFormat;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = true;
                    break;
                case SurfaceFormat.RgbPvrtc4Bpp:
                    if (!supportsPvrtc) goto case default;
                    glInternalFormat      = PixelInternalFormat.CompressedRgbPvrtc4Bppv1Img;
                    glFormat              = (PixelFormat)glInternalFormat;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = true;
                    break;
                case SurfaceFormat.RgbaPvrtc2Bpp:
                    if (!supportsPvrtc) goto case default;
                    glInternalFormat      = PixelInternalFormat.CompressedRgbaPvrtc2Bppv1Img;
                    glFormat              = (PixelFormat)glInternalFormat;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = true;
                    break;
                case SurfaceFormat.RgbaPvrtc4Bpp:
                    if (!supportsPvrtc) goto case default;
                    glInternalFormat      = PixelInternalFormat.CompressedRgbaPvrtc4Bppv1Img;
                    glFormat              = (PixelFormat)glInternalFormat;
                    glType                = PixelType.UnsignedByte;
                    glIsCompressedTexture = true;
                    break;

                default:
                    throw new NotSupportedException(string.Format("The requested SurfaceFormat `{0}` is not supported.", format));
            }
        }

        internal static void PlatformCreateRenderTarget(IRenderTargetStrategyGL renderTargetGL, GraphicsContextStrategy contextStrategy, int width, int height, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat, int multiSampleCount)
        {
            bool isSharedContext = contextStrategy.ToConcrete<ConcreteGraphicsContextGL>().BindSharedContext();
            try
            {
                var GL = contextStrategy.ToConcrete<ConcreteGraphicsContextGL>().GL;

#if OCULUS
                // Enabled = a new on-tile target; already registered = one being re-created after a lost context.
                if (multiSampleCount > 0
                &&  (OnTileMultisample.Enabled || OnTileMultisample.SamplesOf(renderTargetGL) > 0)
                &&  PlatformCreateRenderTargetOnTile(renderTargetGL, contextStrategy, GL, width, height, preferredDepthFormat, multiSampleCount))
                    return;
                OnTileMultisample.Unregister(renderTargetGL);
#endif

                if (multiSampleCount > 0)
                {
                    RenderbufferStorage colorInternalFormat = default;
                    switch (preferredFormat)
                    {
                        case SurfaceFormat.Color:
                            colorInternalFormat = RenderbufferStorage.RGBA8;
                            break;
                        case SurfaceFormat.Bgr565:
                            colorInternalFormat = RenderbufferStorage.RGB565;
                            break;
                        case SurfaceFormat.Bgra4444:
                            colorInternalFormat = RenderbufferStorage.RGBA4;
                            break;
                        case SurfaceFormat.Bgra5551:
                            colorInternalFormat = RenderbufferStorage.RGB5_A1;
                            break;
                        case SurfaceFormat.Single:
                            colorInternalFormat = RenderbufferStorage.R32F;
                            break;
                        case SurfaceFormat.HalfSingle:
                            colorInternalFormat = RenderbufferStorage.R16F;
                            break;
                        case SurfaceFormat.Vector2:
                            colorInternalFormat = RenderbufferStorage.RG32F;
                            break;
                        case SurfaceFormat.HalfVector2:
                            colorInternalFormat = RenderbufferStorage.RG16F;
                            break;
                        case SurfaceFormat.Vector4:
                            colorInternalFormat = RenderbufferStorage.RGBA32F;
                            break;
                        case SurfaceFormat.HalfVector4:
                            colorInternalFormat = RenderbufferStorage.RGBA16F;
                            break;

                        default:
                            break;
                    }

                    renderTargetGL.GLColorBuffer = GL.GenRenderbuffer();
                    GL.CheckGLError();
                    GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderTargetGL.GLColorBuffer);
                    GL.CheckGLError();

                    System.Diagnostics.Debug.Assert(GL.RenderbufferStorageMultisample != null);
                    GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, multiSampleCount, colorInternalFormat, width, height);
                    GL.CheckGLError();
                }

                if (preferredDepthFormat != DepthFormat.None)
                {
                    RenderbufferStorage depthInternalFormat = RenderbufferStorage.DepthComponent16;
                    RenderbufferStorage stencilInternalFormat = (RenderbufferStorage)0;
                    switch (preferredDepthFormat)
                    {
                        case DepthFormat.None:
                            break;

                        case DepthFormat.Depth16:
                            depthInternalFormat = RenderbufferStorage.DepthComponent16;
                            break;

#if GLES
                        case DepthFormat.Depth24:
                            if (contextStrategy.Capabilities.SupportsDepth24)
                                depthInternalFormat = RenderbufferStorage.DepthComponent24oes;
                            else if (contextStrategy.Capabilities.SupportsDepthNonLinear)
                                depthInternalFormat = RenderbufferStorage.DepthComponent16NonlinearNv;
                            else
                                depthInternalFormat = RenderbufferStorage.DepthComponent16;
                            break;

                        case DepthFormat.Depth24Stencil8:
                            if (contextStrategy.Capabilities.SupportsPackedDepthStencil)
                                depthInternalFormat = RenderbufferStorage.Depth24Stencil8oes;
                            else
                            {
                                if (contextStrategy.Capabilities.SupportsDepth24)
                                    depthInternalFormat = RenderbufferStorage.DepthComponent24oes;
                                else if (contextStrategy.Capabilities.SupportsDepthNonLinear)
                                    depthInternalFormat = RenderbufferStorage.DepthComponent16NonlinearNv;
                                else
                                    depthInternalFormat = RenderbufferStorage.DepthComponent16;
                                stencilInternalFormat = RenderbufferStorage.StencilIndex8;
                            }
                            break;

#else
                        case DepthFormat.Depth24:
                            depthInternalFormat = RenderbufferStorage.DepthComponent24;
                            break;

                        case DepthFormat.Depth24Stencil8:
                            depthInternalFormat = RenderbufferStorage.Depth24Stencil8;
                            break;
#endif

                        default:
                            throw new InvalidOperationException("preferredDepthFormat");
                    }

                    if (depthInternalFormat != 0)
                    {
                        renderTargetGL.GLDepthBuffer = GL.GenRenderbuffer();
                        GL.CheckGLError();
                        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderTargetGL.GLDepthBuffer);
                        GL.CheckGLError();
                        if (multiSampleCount > 0)
                        {
                            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, multiSampleCount, depthInternalFormat, width, height);
                            GL.CheckGLError();
                        }
                        else
                        {
                            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, depthInternalFormat, width, height);
                            GL.CheckGLError();
                        }
                        if (preferredDepthFormat == DepthFormat.Depth24Stencil8)
                        {
                            renderTargetGL.GLStencilBuffer = renderTargetGL.GLDepthBuffer;
                            if (stencilInternalFormat != 0)
                            {
                                renderTargetGL.GLStencilBuffer = GL.GenRenderbuffer();
                                GL.CheckGLError();
                                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderTargetGL.GLStencilBuffer);
                                GL.CheckGLError();
                                if (multiSampleCount > 0)
                                {
                                    System.Diagnostics.Debug.Assert(GL.RenderbufferStorageMultisample != null);
                                    GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, multiSampleCount, stencilInternalFormat, width, height);
                                    GL.CheckGLError();
                                }
                                else
                                {
                                    GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, stencilInternalFormat, width, height);
                                    GL.CheckGLError();
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                contextStrategy.ToConcrete<ConcreteGraphicsContextGL>().UnbindSharedContext();
            }
            return;
        }

#if OCULUS
        /// <summary>
        /// A multisampled render target that is resolved on the GPU tile (GL_EXT_multisampled_render_to_texture): no
        /// multisampled color buffer exists in memory; the color attachment is the render target's own texture, attached
        /// with a sample count (see PlatformApplyRenderTargets), and the depth buffer is an on-tile multisampled
        /// renderbuffer that is discarded at resolve. False = not possible here; the caller uses the ordinary path.
        /// </summary>
        private static bool PlatformCreateRenderTargetOnTile(IRenderTargetStrategyGL renderTargetGL, GraphicsContextStrategy contextStrategy, OGL GL,
            int width, int height, DepthFormat preferredDepthFormat, int multiSampleCount)
        {
            if (GL.FramebufferTexture2DMultiSampleOnTile == null || GL.RenderbufferStorageMultisampleOnTile == null)
                return false;

            RenderbufferStorage depthInternalFormat;
            switch (preferredDepthFormat)
            {
                case DepthFormat.None:
                    depthInternalFormat = 0;
                    break;
                case DepthFormat.Depth16:
                    depthInternalFormat = RenderbufferStorage.DepthComponent16;
                    break;
                case DepthFormat.Depth24:
                    if (!contextStrategy.Capabilities.SupportsDepth24)
                        return false;
                    depthInternalFormat = RenderbufferStorage.DepthComponent24oes;
                    break;
                case DepthFormat.Depth24Stencil8:
                    if (!contextStrategy.Capabilities.SupportsPackedDepthStencil)
                        return false;
                    depthInternalFormat = RenderbufferStorage.Depth24Stencil8oes;
                    break;
                default:
                    return false;
            }

            if (depthInternalFormat != 0)
            {
                renderTargetGL.GLDepthBuffer = GL.GenRenderbuffer();
                GL.CheckGLError();
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderTargetGL.GLDepthBuffer);
                GL.CheckGLError();
                GL.RenderbufferStorageMultisampleOnTile(RenderbufferTarget.Renderbuffer, multiSampleCount, depthInternalFormat, width, height);
                GL.CheckGLError();
                if (preferredDepthFormat == DepthFormat.Depth24Stencil8)
                    renderTargetGL.GLStencilBuffer = renderTargetGL.GLDepthBuffer;
            }

            OnTileMultisample.Register(renderTargetGL, multiSampleCount, width, height);
            Console.WriteLine("[KNISB] On-tile multisampled render target {0}x{1}, {2} samples, depth {3}", width, height, multiSampleCount, preferredDepthFormat);
            return true;
        }
#endif

        internal static void PlatformDeleteRenderTarget(IRenderTargetStrategyGL renderTargetGL, GraphicsContextStrategy contextStrategy)
        {
            contextStrategy.ToConcrete<ConcreteGraphicsContextGL>().BindDisposeContext();
            try
            {
                var GL = contextStrategy.ToConcrete<ConcreteGraphicsContextGL>().GL;

                if (renderTargetGL.GLColorBuffer != 0)
                {
                    GL.DeleteRenderbuffer(renderTargetGL.GLColorBuffer);
                    GL.CheckGLError();
                }
                if (renderTargetGL.GLStencilBuffer != 0 && renderTargetGL.GLStencilBuffer != renderTargetGL.GLDepthBuffer)
                {
                    GL.DeleteRenderbuffer(renderTargetGL.GLStencilBuffer);
                    GL.CheckGLError();
                }
                if (renderTargetGL.GLDepthBuffer != 0)
                {
                    GL.DeleteRenderbuffer(renderTargetGL.GLDepthBuffer);
                    GL.CheckGLError();
                }

                contextStrategy.ToConcrete<ConcreteGraphicsContext>().PlatformUnbindRenderTarget(renderTargetGL);
            }
            finally
            {
                contextStrategy.ToConcrete<ConcreteGraphicsContextGL>().UnbindDisposeContext();
            }
        }

        protected override void PlatformGraphicsContextLost()
        {
            if (_glTexture > 0)
            {
                if (!GraphicsDevice.IsDisposed)
                {
                    var GL = _contextStrategy.ToConcrete<ConcreteGraphicsContext>().GL;

                    GL.DeleteTexture(_glTexture);
                    GL.CheckGLError();
                }
            }
            _glTexture = -1;
            _glLastSamplerState = null; 

            base.PlatformGraphicsContextLost();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {

            }
            
            if (_glTexture > 0)
            {
                _contextStrategy.ToConcrete<ConcreteGraphicsContextGL>().BindDisposeContext();
                try
                {
                    var GL = _contextStrategy.ToConcrete<ConcreteGraphicsContext>().GL;

                    GL.DeleteTexture(_glTexture);
                    GL.CheckGLError();
                }
                finally
                {
                    _contextStrategy.ToConcrete<ConcreteGraphicsContextGL>().UnbindDisposeContext();
                }
            }
            _glTexture = -1;

            _glLastSamplerState = null;

            base.Dispose(disposing);
        }

    }

#if OCULUS
    /// <summary>
    /// One render target drawn at a smaller size than the code drawing into it lays out for (the Quest's 2D panel: laid
    /// out at 4096 x 2304, stored smaller). While it is bound the viewport and scissor are in LOGICAL units (so
    /// SpriteBatch, clip rectangles and hit tests see the logical size) and are scaled to the texture when sent to GL.
    /// Whoever samples the texture must use its real size.
    /// </summary>
    public static class ScaledRenderTarget
    {
        public static Texture Target { get; private set; }
        public static int LogicalWidth { get; private set; }
        public static int LogicalHeight { get; private set; }
        internal static float ScaleX = 1f, ScaleY = 1f;

        public static void Set(RenderTarget2D target, int logicalWidth, int logicalHeight)
        {
            Target = target;
            LogicalWidth = logicalWidth;
            LogicalHeight = logicalHeight;
            ScaleX = target != null ? target.Width / (float)logicalWidth : 1f;
            ScaleY = target != null ? target.Height / (float)logicalHeight : 1f;
        }

        internal static bool Is(object renderTarget)
        {
            return renderTarget != null && ReferenceEquals(renderTarget, Target) && (ScaleX != 1f || ScaleY != 1f);
        }
    }

    /// <summary>
    /// Multiview (GL_OVR_multiview2): the eye target is the swapchain's 2-layer image and each draw reaches both eyes.
    /// The app draws once, as the left eye; every vertex shader's multiview variant ends with
    /// gl_Position = mv_eye[gl_ViewID_OVR] * gl_Position (before posFixup), where view 0 is the identity and view 1 is
    /// inverse(eyeView_left x proj_left) x (eyeView_right x proj_right): any transform chain ending in the left eye's
    /// view-projection becomes the right eye's.
    /// </summary>
    public static class Multiview
    {
        /// <summary>Set by the XR device when its eye target is a multiview target.</summary>
        public static bool Enabled;
        /// <summary>The bound framebuffer has two views: programs must be the multiview variants.</summary>
        internal static bool TargetBound;
        /// <summary>View 0 then view 1, each as four rows of the XNA matrix (read as columns by the shader = transpose).</summary>
        internal static readonly Vector4[] EyeRows = new Vector4[8];
        internal static int EyeVersion = 1;

        // Temporary counters (running totals) for the bench log.
        public static long Binds, Draws, OneViewBinds, Acquires;
        public static int LastImageIndex = -1, LastTexture;
        public static string Counters()
        {
            return string.Format("binds={0} oneViewBinds={1} draws={2} acquires={3} lastIndex={4} lastTexture={5} enabled={6}",
                Binds, OneViewBinds, Draws, Acquires, LastImageIndex, LastTexture, Enabled);
        }

        static Multiview()
        {
            SetRightFromLeft(Matrix.Identity);
        }

        /// <summary>The right eye's clip position from the left eye's: inverse(VP_left) x VP_right.</summary>
        public static void SetRightFromLeft(Matrix rightFromLeft)
        {
            Matrix identity = Matrix.Identity;
            EyeRows[0] = new Vector4(identity.M11, identity.M12, identity.M13, identity.M14);
            EyeRows[1] = new Vector4(identity.M21, identity.M22, identity.M23, identity.M24);
            EyeRows[2] = new Vector4(identity.M31, identity.M32, identity.M33, identity.M34);
            EyeRows[3] = new Vector4(identity.M41, identity.M42, identity.M43, identity.M44);
            EyeRows[4] = new Vector4(rightFromLeft.M11, rightFromLeft.M12, rightFromLeft.M13, rightFromLeft.M14);
            EyeRows[5] = new Vector4(rightFromLeft.M21, rightFromLeft.M22, rightFromLeft.M23, rightFromLeft.M24);
            EyeRows[6] = new Vector4(rightFromLeft.M31, rightFromLeft.M32, rightFromLeft.M33, rightFromLeft.M34);
            EyeRows[7] = new Vector4(rightFromLeft.M41, rightFromLeft.M42, rightFromLeft.M43, rightFromLeft.M44);
            EyeVersion++;
        }

        /// <summary>The multiview variant of a vertex shader's GLSL ES 3.00 source; null if it has no posFixup line.</summary>
        internal static string ToMultiviewSource(string glsl300es)
        {
            const string fixup = "gl_Position.y = gl_Position.y * posFixup.y;";
            int at = glsl300es.IndexOf(fixup, StringComparison.Ordinal);
            int versionAt = glsl300es.IndexOf("#version 300 es", StringComparison.Ordinal);
            if (at < 0 || versionAt < 0)
                return null;
            int afterVersion = glsl300es.IndexOf('\n', versionAt);
            if (afterVersion < 0 || afterVersion > at)
                return null;
            afterVersion++;
            string eye = "gl_Position = mat4(mv_eye[int(gl_ViewID_OVR)*4], mv_eye[int(gl_ViewID_OVR)*4+1], "
                       + "mv_eye[int(gl_ViewID_OVR)*4+2], mv_eye[int(gl_ViewID_OVR)*4+3]) * gl_Position;\n\t";
            return glsl300es.Substring(0, afterVersion)
                 + "#extension GL_OVR_multiview2 : require\n"
                 + "layout(num_views = 2) in;\n"
                 + "uniform vec4 mv_eye[8];\n"
                 + glsl300es.Substring(afterVersion, at - afterVersion) + eye + glsl300es.Substring(at);
        }
    }

    /// <summary>
    /// Render targets multisampled on the GPU tile (GL_EXT_multisampled_render_to_texture) rather than through a
    /// multisampled renderbuffer and a blit resolve. Opt-in per render target: set <see cref="Enabled"/> while
    /// constructing it (the XR device does this for its eye targets), then clear it.
    /// </summary>
    internal static class OnTileMultisample
    {
        /// <summary>While true, multisampled render targets being created are made on-tile where possible.</summary>
        internal static bool Enabled;

        private sealed class Info
        {
            internal int Samples;
            internal int Width;
            internal int Height;
            /// <summary>A GL texture to render into instead of the target's own (0 = its own).</summary>
            internal int ColorOverride;
            /// <summary>2 = ColorOverride is a 2-layer array texture drawn with multiview (both eyes at once).</summary>
            internal int OverrideViews = 1;
            /// <summary>One framebuffer per override texture: the same depth buffer, that texture as color.</summary>
            internal readonly Dictionary<int, int> OverrideFramebuffers = new Dictionary<int, int>();
            /// <summary>The 2-layer depth/stencil array texture used with multiview framebuffers.</summary>
            internal int DepthArrayTexture;
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<IRenderTargetStrategyGL, Info> _infos =
            new System.Runtime.CompilerServices.ConditionalWeakTable<IRenderTargetStrategyGL, Info>();

        internal static void Register(IRenderTargetStrategyGL renderTargetGL, int samples, int width, int height)
        {
            _infos.AddOrUpdate(renderTargetGL, new Info { Samples = samples, Width = width, Height = height });
        }

        internal static void Unregister(IRenderTargetStrategyGL renderTargetGL)
        {
            _infos.Remove(renderTargetGL);
        }

        /// <summary>True if the GL has the OVR multiview functions this needs.</summary>
        internal static bool MultiviewSupported(OGL GL)
        {
            return GL.FramebufferTextureMultisampleMultiviewOVR != null;
        }

        /// <summary>The on-tile sample count of this render target; 0 = an ordinary render target.</summary>
        internal static int SamplesOf(IRenderTargetStrategyGL renderTargetGL)
        {
            Info info;
            return renderTargetGL != null && _infos.TryGetValue(renderTargetGL, out info) ? info.Samples : 0;
        }

        /// <summary>
        /// Points an on-tile render target at another GL texture of the same size (the XR swapchain image being
        /// rendered this frame), so drawing goes straight into it with no copy afterwards. 0 restores its own texture.
        /// Takes effect the next time the render target is set. False = not an on-tile render target.
        /// </summary>
        internal static bool SetColorOverride(RenderTarget2D renderTarget, int glTexture, int views = 1)
        {
            IRenderTargetStrategyGL renderTargetGL = ((IPlatformTexture)renderTarget).GetTextureStrategy<ITextureStrategy>() as IRenderTargetStrategyGL;
            Info info;
            if (renderTargetGL == null || !_infos.TryGetValue(renderTargetGL, out info))
                return false;
            info.ColorOverride = glTexture;
            info.OverrideViews = views;
            return true;
        }

        /// <summary>
        /// The framebuffer to bind instead of the target's own while a color override is set (made on first use), or 0.
        /// </summary>
        internal static int OverrideFramebufferOf(IRenderTargetStrategyGL renderTargetGL, OGL GL)
        {
            bool multiview;
            return OverrideFramebufferOf(renderTargetGL, GL, out multiview);
        }

        internal static int OverrideFramebufferOf(IRenderTargetStrategyGL renderTargetGL, OGL GL, out bool multiview)
        {
            multiview = false;
            Info info;
            if (renderTargetGL == null || !_infos.TryGetValue(renderTargetGL, out info) || info.ColorOverride == 0)
                return 0;
            multiview = info.OverrideViews == 2;

            int framebuffer;
            if (info.OverrideFramebuffers.TryGetValue(info.ColorOverride, out framebuffer))
                return framebuffer;

            framebuffer = GL.GenFramebuffer();
            GL.CheckGLError();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
            GL.CheckGLError();
            if (multiview)
            {
                // Both eyes: the swapchain's 2-layer array texture as color, a 2-layer depth/stencil array texture,
                // each multisampled on the tile.
                if (info.DepthArrayTexture == 0)
                {
                    int[] depthTex = new int[1];
                    Android.Opengl.GLES30.GlGenTextures(1, depthTex, 0);
                    Android.Opengl.GLES30.GlBindTexture(0x8C1A /*GL_TEXTURE_2D_ARRAY*/, depthTex[0]);
                    Android.Opengl.GLES30.GlTexStorage3D(0x8C1A, 1, 0x88F0 /*GL_DEPTH24_STENCIL8*/, info.Width, info.Height, 2);
                    Android.Opengl.GLES30.GlBindTexture(0x8C1A, 0);
                    info.DepthArrayTexture = depthTex[0];
                }
                GL.FramebufferTextureMultisampleMultiviewOVR(FramebufferTarget.Framebuffer, (FramebufferAttachment)0x821A /*GL_DEPTH_STENCIL_ATTACHMENT*/,
                    info.DepthArrayTexture, 0, info.Samples, 0, 2);
                GL.CheckGLError();
                GL.FramebufferTextureMultisampleMultiviewOVR(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                    info.ColorOverride, 0, info.Samples, 0, 2);
                GL.CheckGLError();
            }
            else
            {
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, renderTargetGL.GLDepthBuffer);
            GL.CheckGLError();
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.StencilAttachment, RenderbufferTarget.Renderbuffer, renderTargetGL.GLStencilBuffer);
            GL.CheckGLError();
            GL.FramebufferTexture2DMultiSampleOnTile(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, info.ColorOverride, 0, info.Samples);
            GL.CheckGLError();
            }
            FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            Console.WriteLine("[KNISB] On-tile override framebuffer {0} for texture {1} ({2} view(s)): {3}", framebuffer, info.ColorOverride, info.OverrideViews, status);
            if (status != FramebufferErrorCode.FramebufferComplete)
            {
                GL.DeleteFramebuffer(framebuffer);
                info.ColorOverride = 0;
                return 0;
            }
            info.OverrideFramebuffers.Add(info.ColorOverride, framebuffer);
            return framebuffer;
        }
    }
#endif
}
