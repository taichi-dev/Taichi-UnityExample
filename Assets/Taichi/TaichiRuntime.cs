using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Taichi.Unity {
    public class TaichiRuntime : IDisposable {
        public readonly TiRuntime Handle;

        public TaichiRuntime(TiRuntime handle) {
            Handle = handle;
        }



        private static TaichiRuntime _Singleton;
        public static TaichiRuntime Singleton {
            get {
                if (_Singleton == null) {
                    _Singleton = new TaichiRuntime(Ffi.ImportNativeRuntimeUNITY());
                }
                return _Singleton;
            }
        }


        public TaichiMemory ImportMemoryAsync(IntPtr nativeBufferPtr) {
            return new TaichiMemory(this, Ffi.ImportNativeMemoryAsyncUNITY(Handle, nativeBufferPtr));
        }
        public TaichiMemory AllocateMemory(TiMemoryAllocateInfo memoryAllocateInfo) {
            return new TaichiMemory(this, Ffi.AllocateMemory(Handle, memoryAllocateInfo));
        }

        public TaichiAotModule LoadAotModule(string module_path) {
            return new TaichiAotModule(this, Ffi.LoadAotModule(Handle, module_path));
        }

        public void CopyMemoryToNativeAsync(TiMemorySlice memory, IntPtr native_buffer_ptr, ulong native_buffer_offset, ulong native_buffer_size) {
            Ffi.CopyMemoryToNativeAsyncUNITY(Handle, memory, native_buffer_ptr, native_buffer_offset, native_buffer_size);
        }

        public void Submit() {
            GL.IssuePluginEvent(Ffi.SubmitInRenderThreadUNITY(Handle), 0);
        }
        public void Submit(CommandBuffer commandBuffer) {
            commandBuffer.IssuePluginEvent(Ffi.SubmitInRenderThreadUNITY(Handle), 0);
        }



        public bool IsDisposed => disposedValue;
        private bool disposedValue;
        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                }

                Ffi.DestroyRuntime(Handle);
                disposedValue = true;
            }
        }
        ~TaichiRuntime() {
            Dispose(disposing: false);
        }
        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
