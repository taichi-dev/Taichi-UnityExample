using System;

namespace Taichi.Unity {
    public class TaichiAotModule : IDisposable {
        public readonly TaichiRuntime Runtime;
        public readonly TiAotModule Handle;

        public TaichiAotModule(TaichiRuntime runtime, TiAotModule handle) {
            Runtime = runtime;
            Handle = handle;
        }

        public TaichiKernel GetKernel(string name) {
            return new TaichiKernel(this, Ffi.GetAotModuleKernel(Handle, name));
        }
        public TaichiComputeGraph GetComputeGraph(string name) {
            return new TaichiComputeGraph(this, Ffi.GetAotModuleComputeGraph(Handle, name));
        }



        public bool IsDisposed => disposedValue;
        private bool disposedValue;
        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                }

                if (!Runtime.IsDisposed && Handle.Inner != null) {
                    Ffi.DestroyAotModule(Handle);
                }
                disposedValue = true;
            }
        }
        ~TaichiAotModule() {
            Dispose(disposing: false);
        }
        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
