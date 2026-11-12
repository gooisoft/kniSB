// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

// Copyright (C)2023 Nick Kastellanos

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Platform.Graphics.OpenGL;
using Microsoft.Xna.Platform.Graphics.Utilities;


namespace Microsoft.Xna.Platform.Graphics
{
    public abstract class ConcreteShader : ShaderStrategy
    {
        // The shader handle.
        private int _shaderHandle = -1;

        internal int ShaderHandle { get { return _shaderHandle; } }

#if OCULUS
        // The legacy MGFX GLSL this shader came from (GLSL ES 1.00 on the Reach profile), kept for the ES 3.00
        // variants multiview needs: a multiview program's vertex AND fragment shaders must both be ES 3.00.
        private string _legacyGlsl;
        // KNIFX shaders (KNI's built-in effects, e.g. SpriteBatch's) carry several GLSL versions, ES 3.00 among them.
        private byte[] _knifxBytecode;
        private bool _compiledAs300es;
        private bool _variantFailed;
        private int _multiviewShaderHandle = -1;
        private int _es300ShaderHandle = -1;

        /// <summary>This shader's GLSL ES 3.00 source: converted from legacy MGFX, or the KNIFX ES 3.00 variant. Null if none.</summary>
        private string GetEs300Source(ShaderType shaderType)
        {
            if (_legacyGlsl != null)
                return ConvertGLSLToGLSL300es(shaderType, _legacyGlsl);
            if (_knifxBytecode != null)
            {
                try
                {
                    int offset = FindShaderByteCode(_knifxBytecode, major: 3, minor: 0, es: true);
                    int length = BitConverter.ToInt32(_knifxBytecode, offset); offset += 4;
                    return System.Text.Encoding.ASCII.GetString(_knifxBytecode, offset, length).TrimEnd('\0');
                }
                catch (Exception e)
                {
                    Console.WriteLine("[KNISB] multiview: no GLSL ES 3.00 in a KNIFX shader: " + e.Message);
                }
            }
            return null;
        }

        /// <summary>The multiview variant of this vertex shader (compiled on first use); -1 if it cannot have one.</summary>
        internal int GetMultiviewShaderHandle(OGL GL)
        {
            if (_multiviewShaderHandle != -1 || _variantFailed)
                return _multiviewShaderHandle;
            string es300 = GetEs300Source(ShaderType.VertexShader);
            if (es300 == null)
            {
                _variantFailed = true;
                return -1;
            }
            string source = Multiview.ToMultiviewSource(es300);
            if (source == null)
            {
                Console.WriteLine("[KNISB] multiview: vertex shader without a posFixup line, no multiview variant");
                _variantFailed = true;
                return -1;
            }
            _multiviewShaderHandle = CompileVariant(GL, ShaderType.VertexShader, source);
            _variantFailed = _multiviewShaderHandle == -1;
            return _multiviewShaderHandle;
        }

        /// <summary>This fragment shader as GLSL ES 3.00 (itself if already compiled so; else compiled on first use).</summary>
        internal int GetEs300FragmentShaderHandle(OGL GL)
        {
            if (_compiledAs300es)
                return _shaderHandle;
            if (_es300ShaderHandle != -1 || _variantFailed)
                return _es300ShaderHandle;
            string es300 = GetEs300Source(ShaderType.FragmentShader);
            if (es300 == null)
            {
                _variantFailed = true;
                return -1;
            }
            _es300ShaderHandle = CompileVariant(GL, ShaderType.FragmentShader, es300);
            _variantFailed = _es300ShaderHandle == -1;
            return _es300ShaderHandle;
        }

        private static int CompileVariant(OGL GL, ShaderType shaderType, string source)
        {
            int handle = GL.CreateShader(shaderType);
            GL.ShaderSource(handle, System.Text.Encoding.ASCII.GetBytes(source));
            GL.CompileShader(handle);
            int compiled = 0;
            GL.GetShader(handle, ShaderParameter.CompileStatus, out compiled);
            if (compiled != (int)Bool.True)
            {
                Console.WriteLine("[KNISB] multiview: {0} variant failed to compile: {1}", shaderType, GL.GetShaderInfoLog(handle));
                GL.DeleteShader(handle);
                return -1;
            }
            return handle;
        }
#endif


        internal ConcreteShader(GraphicsContextStrategy contextStrategy, ShaderVersion shaderVersion, byte[] shaderBytecode, SamplerInfo[] samplers, int[] cBuffers, VertexAttribute[] attributes)
            : base(contextStrategy, shaderVersion, shaderBytecode, samplers, cBuffers, attributes)
        {
            GraphicsProfile graphicsProfile = this.GraphicsDeviceStrategy.GraphicsProfile;
            ShaderVersion maxVersion = MaxShaderVersions[graphicsProfile];
            if (shaderVersion != default
            &&  shaderVersion > maxVersion)
            {
                throw new NotSupportedException(
                    $"Shader model {shaderVersion} is not supported by the current graphics profile '{graphicsProfile}'.");
            }

        }

        internal void CreateShader(GraphicsContextStrategy contextStrategy, ShaderType shaderType, ShaderVersion shaderVersion, byte[] shaderBytecode)
        {
            bool isSharedContext = contextStrategy.ToConcrete<ConcreteGraphicsContextGL>().BindSharedContext();
            try
            {
                var GL = contextStrategy.ToConcrete<ConcreteGraphicsContextGL>().GL;

                _shaderHandle = GL.CreateShader(shaderType);
                GL.CheckGLError();

                if (shaderVersion == default) // Handle legacy MGFX
                {
#if OCULUS
                    _legacyGlsl = System.Text.Encoding.ASCII.GetString(shaderBytecode);
#endif
                    if (this.GraphicsDevice.Adapter.Backend == GraphicsBackend.GLES
                    &&  this.GraphicsDevice.GraphicsProfile >= GraphicsProfile.HiDef)
                    {
                        string glslCode = System.Text.Encoding.ASCII.GetString(shaderBytecode);
                        // GLES 3.00 is required for gl_FragData
                        string glsl300esCode = ConvertGLSLToGLSL300es(shaderType, glslCode);
#if OCULUS
                        _compiledAs300es = true;
#endif
                        shaderBytecode = System.Text.Encoding.ASCII.GetBytes(glsl300esCode);

                        GL.ShaderSource(_shaderHandle, shaderBytecode);
                        GL.CheckGLError();
                    }
                    else
                    {
                        GL.ShaderSource(_shaderHandle, shaderBytecode);
                        GL.CheckGLError();
                    }
                }
                else // Handle KNIFX
                {
#if OCULUS
                    _knifxBytecode = shaderBytecode;
                    _compiledAs300es = (this.GraphicsDevice.Adapter.Backend == GraphicsBackend.GLES
                                        || this.GraphicsDevice.Adapter.Backend == GraphicsBackend.WebGL)
                                       && this.GraphicsDevice.GraphicsProfile >= GraphicsProfile.HiDef;
#endif
                    int bytecodeOffset;

                    if (this.GraphicsDevice.Adapter.Backend == GraphicsBackend.GLES
                    ||  this.GraphicsDevice.Adapter.Backend == GraphicsBackend.WebGL)
                    {
                        if (this.GraphicsDevice.GraphicsProfile >= GraphicsProfile.HiDef)
                        {
                            bytecodeOffset = FindShaderByteCode(shaderBytecode, major: 3, minor: 0, es: true);
                        }
                        else
                        {
                            bytecodeOffset = FindShaderByteCode(shaderBytecode, major: 1, minor: 0, es: false);
                        }
                    }
                    else //if (this.GraphicsDevice.Adapter.Backend == GraphicsBackend.OpenGL)
                    {
                        bytecodeOffset = FindShaderByteCode(shaderBytecode, major: 1, minor: 1, es: false);
                    }

                    int bytecodeLength = BitConverter.ToInt32(shaderBytecode, bytecodeOffset); bytecodeOffset += 4;
                    GL.ShaderSource(_shaderHandle, shaderBytecode, bytecodeOffset, bytecodeLength);
                    GL.CheckGLError();
                }

                GL.CompileShader(_shaderHandle);
                GL.CheckGLError();
                int compiled = 0;
                GL.GetShader(_shaderHandle, ShaderParameter.CompileStatus, out compiled);
                GL.CheckGLError();
                if (compiled != (int)Bool.True)
                {
                    string log = GL.GetShaderInfoLog(_shaderHandle);
                    Debug.WriteLine(log);

                    if (!GraphicsDevice.IsDisposed)
                    {
                        if (GL.IsShader(_shaderHandle))
                        {
                            GL.DeleteShader(_shaderHandle);
                            GL.CheckGLError();
                        }
                    }
                    _shaderHandle = -1;

                    throw new InvalidOperationException("Shader Compilation Failed."
                        + Environment.NewLine + log);
                }
            }
            finally
            {
                contextStrategy.ToConcrete<ConcreteGraphicsContextGL>().UnbindSharedContext();
            }
        }

        private int FindShaderByteCode(byte[] shaderBytecode, int major, int minor, bool es)
        {
            int pos = 0;
            
            short reserved0 = BitConverter.ToInt16(shaderBytecode, pos); pos += 2;
            if (reserved0 != 0)
                throw new Exception("Invalid shader bytecode");

            short count = BitConverter.ToInt16(shaderBytecode, pos); pos += 2;

            for (int i = 0; i < count; i++)
            {
                int major0 = shaderBytecode[pos]; pos += 1;
                int minor0 = shaderBytecode[pos]; pos += 1;
                bool es0 = BitConverter.ToBoolean(shaderBytecode, pos); pos += 1;
                int bytecodeOffset0 = BitConverter.ToInt32(shaderBytecode, pos); pos += 4;

                if (major == major0
                &&  minor == minor0
                &&  es == es0)
                {
                    return bytecodeOffset0;
                }
            }

            throw new InvalidOperationException("GLSL bytecode not found.");
        }

        static Regex rgxOES = new Regex(
                @"^#extension GL_OES_standard_derivatives : enable\n", RegexOptions.Multiline);
        static Regex rgxPrecision = new Regex(
                @"precision mediump (float|int);", RegexOptions.Multiline);
        static Regex rgxAttribute = new Regex(
                @"^attribute(?=\s)", RegexOptions.Multiline);
        static Regex rgxVarying = new Regex(
                @"^varying(?=\s)", RegexOptions.Multiline);
        static Regex rgxFragColor = new Regex(
                @"^#define (\w+) gl_FragColor", RegexOptions.Multiline);
        static Regex rgxFragData = new Regex(
                @"^#define (\w+) gl_FragData\[(\d+)\]", RegexOptions.Multiline);
        static Regex rgxTexture = new Regex(
                @"texture(2D|3D|Cube)(?=\()", RegexOptions.Multiline);

        private string ConvertGLSLToGLSL300es(ShaderType shaderType, string glslCode)
        {
            switch (shaderType)
            {
                case ShaderType.VertexShader:
                    {
                        glslCode = rgxVarying.Replace(glslCode, "out");
                    }
                    break;

                case ShaderType.FragmentShader:
                    {
                        glslCode = rgxOES.Replace(glslCode, "");
                        glslCode = rgxVarying.Replace(glslCode, "in");
                        glslCode = rgxFragColor.Replace(glslCode, "out vec4 $1;");
                        glslCode = rgxFragData .Replace(glslCode, "layout(location=$2) out vec4 $1;");
                    }
                    break;
            }

            glslCode = rgxPrecision.Replace(glslCode, "precision highp $1;");
            glslCode = rgxAttribute.Replace(glslCode, "in");
            glslCode = rgxTexture.Replace(glslCode, "texture");

            glslCode = "#version 300 es\n"
                     + glslCode;

            return glslCode;
        }

        protected override void PlatformGraphicsContextLost()
        {
            var GL = _contextStrategy.ToConcrete<ConcreteGraphicsContext>().GL;

            if (_shaderHandle != -1)
            {
                if (GL.IsShader(_shaderHandle))
                {
                    GL.DeleteShader(_shaderHandle);
                    GL.CheckGLError();
                }
                _shaderHandle = -1;
            }
#if OCULUS
            if (_multiviewShaderHandle != -1)
            {
                if (GL.IsShader(_multiviewShaderHandle))
                    GL.DeleteShader(_multiviewShaderHandle);
                _multiviewShaderHandle = -1;
            }
            if (_es300ShaderHandle != -1)
            {
                if (GL.IsShader(_es300ShaderHandle))
                    GL.DeleteShader(_es300ShaderHandle);
                _es300ShaderHandle = -1;
            }
#endif

            base.PlatformGraphicsContextLost();
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }

            if (_shaderHandle != -1)
            {
                _contextStrategy.ToConcrete<ConcreteGraphicsContextGL>().BindDisposeContext();
                try
                {
                    var GL = _contextStrategy.ToConcrete<ConcreteGraphicsContext>().GL;

                    if (GL.IsShader(_shaderHandle))
                    {
                        GL.DeleteShader(_shaderHandle);
                        GL.CheckGLError();
                    }
                }
                finally
                {
                    _contextStrategy.ToConcrete<ConcreteGraphicsContextGL>().UnbindDisposeContext();
                }
            }

            _shaderHandle = -1;

            base.Dispose(disposing);
        }
    }
}
