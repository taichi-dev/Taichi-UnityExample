using System;

namespace Taichi.Unity {
    public class TaichiMemory : IDisposable {
        public readonly TaichiRuntime Runtime;
        private IntPtr _NativeBufferPtr;
        private TiMemory _Handle;
        public TiMemory Handle {
            get {
                if (_Handle.Inner == IntPtr.Zero && _NativeBufferPtr != null) {
                    _Handle = Ffi.ImportNativeMemoryAsyncUNITY(Runtime.Handle, _NativeBufferPtr);
                }
                return _Handle;
            }
        }
        public TaichiMemory(TaichiRuntime runtime, IntPtr nativeBufferPtr) {
            Runtime = runtime;
            _Handle = new TiMemory();
            _NativeBufferPtr = nativeBufferPtr;
        }
        public TaichiMemory(TaichiRuntime runtime, TiMemory handle) {
            Runtime = runtime;
            _Handle = handle;
        }



        public bool IsDisposed => disposedValue;
        private bool disposedValue;
        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                }

                if (!Runtime.IsDisposed && Handle.Inner != null) {
                    Ffi.FreeMemory(Runtime.Handle, Handle);
                }
                disposedValue = true;
            }
        }
        ~TaichiMemory() {
            Dispose(disposing: false);
        }
        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
