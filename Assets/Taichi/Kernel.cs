using System;
using Taichi.Generated;
using UnityEngine;

namespace Taichi {
    public class Kernel : IDisposable {
        public readonly AotModule AotModule;
        public readonly TiKernel Handle;

        public Kernel(AotModule aotModule, TiKernel handle) {
            AotModule = aotModule;
            Handle = handle;
        }

        public void LaunchAsync(TiArgument[] args) {
            for (var i = 0; i < args.Length; ++i) {
                if (args[i].type == TiArgumentType.TI_ARGUMENT_TYPE_NDARRAY) {
                    if (args[i].value.ndarray.shape.dims.Length != 16) {
                        throw new ArgumentException("NdArray shape must be 16");
                    }
                    if (args[i].value.ndarray.elem_shape.dims.Length != 16) {
                        throw new ArgumentException("NdArray element shape must be 16");
                    }
                    if (args[i].value.ndarray.memory.Inner == IntPtr.Zero) {
                        Debug.LogWarning("Ignored launch because argument list contains uninitialized `TiMemory`");
                        return;
                    }
                } else {
                    // FIXME: (penguinliong) This is a workaround to avoid
                    // `NullPointerException` when clr tries to marshall these
                    // unused fields.
                    args[i].value.ndarray.shape.dims = new uint[16];
                    args[i].value.ndarray.elem_shape.dims = new uint[16];
                }
            }
            Ffi.TixLaunchKernelAsyncUnity(Runtime.Singleton.Handle, Handle, (uint)args.Length, args);
        }


        public bool IsDisposed => disposedValue;
        private bool disposedValue;
        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                }

                disposedValue = true;
            }
        }
        ~Kernel() {
            Dispose(disposing: false);
        }
        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
