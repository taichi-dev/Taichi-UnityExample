using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;

namespace Taichi {

static partial class Ffi {
#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
    [DllImport ("__Internal")]
#else
    [DllImport("taichi_unity")]
#endif
private static extern TiRuntime tix_import_native_runtime_unity();

public static TiRuntime ImportNativeRuntimeUNITY() {
    return tix_import_native_runtime_unity();
}
}

static partial class Ffi {
#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
    [DllImport ("__Internal")]
#else
    [DllImport("taichi_unity")]
#endif
private static extern TiMemory tix_import_native_memory_async_unity(TiRuntime runtime, IntPtr native_buffer_ptr);

public static TiMemory ImportNativeMemoryAsyncUNITY(TiRuntime runtime, IntPtr native_buffer_ptr) {
    return tix_import_native_memory_async_unity(runtime, native_buffer_ptr);
}
}


static partial class Ffi {
#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
    [DllImport ("__Internal")]
#else
    [DllImport("taichi_unity")]
#endif
private static extern void tix_launch_kernel_async_unity(
  TiRuntime runtime,
  TiKernel kernel,
  uint arg_count,
  [MarshalAs(UnmanagedType.LPArray)] TiArgument[] args
);
public static void LaunchKernelAsyncUNITY(
  TiRuntime runtime,
  TiKernel kernel,
  uint arg_count,
  TiArgument[] args
) {
  tix_launch_kernel_async_unity(
    runtime,
    kernel,
    arg_count,
    args
  );
}
}

static partial class Ffi {
#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
    [DllImport ("__Internal")]
#else
    [DllImport("taichi_unity")]
#endif
private static extern void tix_launch_compute_graph_async_unity(
  TiRuntime runtime,
  TiComputeGraph compute_graph,
  uint arg_count,
  [MarshalAs(UnmanagedType.LPArray)] TiNamedArgument[] args
);
public static void LaunchComputeGraphAsyncUNITY(
  TiRuntime runtime,
  TiComputeGraph compute_graph,
  uint arg_count,
  TiNamedArgument[] args
) {
  tix_launch_compute_graph_async_unity(
    runtime,
    compute_graph,
    arg_count,
    args
  );
}
}

static partial class Ffi {
#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
    [DllImport ("__Internal")]
#else
    [DllImport("taichi_unity")]
#endif
private static extern void tix_copy_memory_to_native_async_unity(
    TiRuntime runtime,
    IntPtr memory,
    IntPtr native_buffer_ptr,
    ulong native_buffer_offset,
    ulong native_buffer_size
);
public static void CopyMemoryToNativeAsyncUNITY(
    TiRuntime runtime,
    TiMemorySlice memory,
    IntPtr native_buffer_ptr,
    ulong native_buffer_offset,
    ulong native_buffer_size
) {
    IntPtr hglobal_memory = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TiMemoryAllocateInfo)));
    Marshal.StructureToPtr(memory, hglobal_memory, false);
    tix_copy_memory_to_native_async_unity(
      runtime,
      hglobal_memory,
      native_buffer_ptr,
      native_buffer_offset,
      native_buffer_size
    );
    Marshal.FreeHGlobal(hglobal_memory);
}
}

static partial class Ffi {
#if (UNITY_IOS || UNITY_TVOS || UNITY_WEBGL) && !UNITY_EDITOR
    [DllImport ("__Internal")]
#else
    [DllImport("taichi_unity")]
#endif
private static extern IntPtr tix_submit_in_render_thread_unity(TiRuntime runtime);

private static int _LastSubmitFrameNumber = 0;
public static IntPtr SubmitInRenderThreadUNITY(TiRuntime runtime) {
    if (Time.frameCount == _LastSubmitFrameNumber) {
        throw new InvalidOperationException("Cannot submit to Taichi runtime in a frame more than once");
    } else {
        _LastSubmitFrameNumber = Time.frameCount;
        return tix_submit_in_render_thread_unity(runtime);
    }
}
}

} // namespace Taichi
