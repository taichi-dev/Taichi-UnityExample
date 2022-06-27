using UnityEngine;
using Taichi.Unity;
using System.Runtime.InteropServices;
using System;

public class NdArray<T> : IDisposable where T : struct {
    private ComputeBuffer _ComputeBuffer;
    private TaichiMemory _Memory;
    public TaichiMemory Memory {
        get {
            if (_ComputeBuffer != null && !_ComputeBuffer.IsValid()) {
                Debug.LogError("`ComputeBuffer` bound by `NdArray` is no longer valid and the imported memory will be disposed");
                Dispose(true);
            }

            return _Memory;
        }
    }
    public uint[] Shape { get; private set; }
    public ulong Size => Shape2Size(Shape);
    public ComputeBuffer ComputeBuffer => _ComputeBuffer;
    public bool IsInitialized => _Memory.Handle.Inner != IntPtr.Zero;

    private static ulong Shape2Size(uint[] shape) {
        ulong size = (ulong)Marshal.SizeOf(typeof(T));
        foreach (var dim in shape) {
            size *= dim;
        }
        if (size == 0) {
            throw new ArgumentException("NdArray shape cannot be zero");
        }
        return size;
    }

    public NdArray(params uint[] shape) :
        this(new ComputeBuffer((int)Shape2Size(shape) / Marshal.SizeOf(typeof(T)), Marshal.SizeOf(typeof(T))), shape) {}
    public NdArray(ComputeBuffer computeBuffer, params uint[] shape) {
        if (computeBuffer == null) {
            throw new ArgumentNullException("`computeBuffer` cannot be null");
        }
        if ((ulong)computeBuffer.count * (ulong)computeBuffer.stride != Shape2Size(shape)) {
            throw new ArgumentException("NdArray shape mismatches compute buffer size");
        }
        _ComputeBuffer = computeBuffer; // Keep it alive.
        _Memory = new TaichiMemory(TaichiRuntime.Singleton, computeBuffer.GetNativeBufferPtr());
        Shape = shape;
    }

    public Taichi.TiNdArray ToTiNdArray() {
        var shape = new uint[16];
        Array.Copy(Shape, shape, Shape.Length);
        var elemShape = new uint[16];

        return new Taichi.TiNdArray {
            shape = new Taichi.TiNdShape {
                dim_count = (uint)Shape.Length,
                dims = shape,
            },
            elem_shape = new Taichi.TiNdShape {
                dim_count = 0,
                dims = elemShape,
            },
            memory = IsDisposed ? new Taichi.TiMemory { Inner = IntPtr.Zero } : Memory.Handle,
        };
    }
    public Taichi.TiMemorySlice ToTiMemorySlice() {
        return new Taichi.TiMemorySlice {
            memory = _Memory.Handle,
            offset = 0,
            size = Size,
        };
    }

    public T[] ToCpu() {
        var rv = new T[_ComputeBuffer.count];
        _ComputeBuffer.GetData(rv);
        return rv;
    }



    public bool IsDisposed => disposedValue;
    private bool disposedValue;
    protected virtual void Dispose(bool disposing) {
        if (!disposedValue) {
            if (disposing) {
                _Memory.Dispose();
                _ComputeBuffer = null;
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
