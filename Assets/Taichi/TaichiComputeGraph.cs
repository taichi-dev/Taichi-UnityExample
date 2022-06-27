using System;
using UnityEngine;

namespace Taichi.Unity {
    public class TaichiComputeGraph : IDisposable {
        public readonly TaichiAotModule AotModule;
        public readonly TiComputeGraph Handle;

        public TaichiComputeGraph(TaichiAotModule aotModule, TiComputeGraph handle) {
            AotModule = aotModule;
            Handle = handle;
        }

        public void Launch(TiNamedArgument[] namedArgs) {
            for (var i = 0; i < namedArgs.Length; ++i) {
                if (namedArgs[i].argument.type == TiArgumentType.TI_ARGUMENT_TYPE_NDARRAY) {
                    if (namedArgs[i].argument.value.ndarray.memory.Inner == IntPtr.Zero) {
                        Debug.LogWarning("Ignored launch because argument list contains uninitialized `TiMemory`");
                        return;
                    }
                } else {
                    // FIXME: (penguinliong) This is a workaround to avoid
                    // `NullPointerException` when clr tries to marshall these
                    // unused fields.
                    namedArgs[i].argument.value.ndarray.shape.dims = new uint[16];
                    namedArgs[i].argument.value.ndarray.elem_shape.dims = new uint[16];
                }
            }
            Ffi.LaunchComputeGraphAsyncUNITY(AotModule.Runtime.Handle, Handle, (uint)namedArgs.Length, namedArgs);
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
        ~TaichiComputeGraph() {
            Dispose(disposing: false);
        }
        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
