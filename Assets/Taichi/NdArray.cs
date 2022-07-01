using System.Runtime.InteropServices;
using System;
using Taichi.Generated;

namespace Taichi {
    public class NdArray<T> : IDisposable where T : struct {
        private int[] _Shape;
        private Memory _Memory;



        public int[] Shape => _Shape;
        public int Count => Shape2Count(Shape);



        private static int Shape2Count(int[] shape) {
            int size = 1;
            foreach (var dim in shape) {
                size *= dim;
            }
            if (size == 0) {
                throw new ArgumentException("NdArray shape cannot be zero");
            }
            return size;
        }
        private static int Shape2Size(int[] shape) {
            return Marshal.SizeOf(typeof(T)) * Shape2Count(shape);
        }



        public NdArray(int[] shape, bool hostRead, bool hostWrite, TiMemoryUsageFlagBits usage) {
            _Shape = shape;
            _Memory = new Memory(Shape2Size(shape), hostRead, hostWrite, usage);
        }
        public NdArray(int[] shape, bool hostRead, bool hostWrite) :
            this(shape, hostRead, hostWrite, TiMemoryUsageFlagBits.TI_MEMORY_USAGE_STORAGE_BIT) { }
        public NdArray(params int[] shape) :
            this(shape, true, true) { }



        public void CopyFromArray(T[] data) {
            if (data.Length * Marshal.SizeOf<T>() != Shape2Size(Shape)) {
                throw new ArgumentException(nameof(data));
            }
            _Memory.Write(data);
        }

        public static NdArray<T> FromArray(T[] data, int[] shape) {
            var rv = new NdArray<T>(shape);
            rv.CopyFromArray(data);
            return rv;
        }

        public void CopyToArray(T[] data) {
            if (data.Length == 0) {
                return;
            }
            if (data.Length * Marshal.SizeOf<T>() != Shape2Size(Shape)) {
                throw new ArgumentException(nameof(data));
            }
            _Memory.Read(data);
        }
        public T[] ToArray() {
            var rv = new T[Shape2Count(Shape)];
            CopyToArray(rv);
            return rv;
        }



        public void CopyToNativeBufferAsync(IntPtr native_buffer_ptr) {
            if (native_buffer_ptr == IntPtr.Zero) {
                throw new ArgumentNullException("`native_buffer_ptr` cannot be null");
            }
            _Memory.CopyToNativeBufferAsync(native_buffer_ptr);
        }



        public TiNdArray ToTiNdArray() {
            var shape = new uint[16];
            for (int i = 0; i < _Shape.Length; ++i) {
                shape[i] = (uint)_Shape[i];
            }
            var elemShape = new uint[16];

            return new TiNdArray {
                shape = new TiNdShape {
                    dim_count = (uint)Shape.Length,
                    dims = shape,
                },
                elem_shape = new TiNdShape {
                    dim_count = 0,
                    dims = elemShape,
                },
                memory = _Memory.Handle,
            };
        }
        public TiMemorySlice ToTiMemorySlice() {
            return _Memory.ToTiMemorySlice(0, Shape2Size(Shape));
        }



        public bool IsDisposed => disposedValue;
        private bool disposedValue;
        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    _Memory.Dispose();
                }
                disposedValue = true;
            }
        }
        ~NdArray() {
            Dispose(disposing: false);
        }
        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

}
