## Run the demo
This demo may not run directly on your device. So we have listed some of the problems you may encounter here.

- **If you get the follow popup when trying to run:**
<div align = center>
<img src = "img/dll_crash.PNG" width="70%">
</div>

This means that you need to regenerate the `taichi_c_api.dll` and `taichi_unity.dll` files and put them in the `Assets/Plugins/X86_64`.

Among them, the generation of taichi_c_api.dll is completed during the source code compilation process of taichi. if you can't find `c_api` folder under `.\taichi\_skbuild\win-amd64-3.X\cmake-install`. You should set the H_C_API option to ON in the CmakeCache.txt file under the `.\taichi\_skbuild\win-amd64-3.X\cmake-build`. 
<div align = center>
<img src = "img/capi_compile.PNG" width="70%">
</div>

After recompiling, you can find the dll file in the c_api folder mentioned earlier.
<div align = center>
<img src = "img/capi_cll.PNG" width="70%">
</div>

You can get `taichi_unity.dll` from [taichi-dev/taichi-unity2](https://github.com/taichi-dev/taichi-unity2) in a similar way.

In this way, our demo will no longer crash :).

- **If you get the follow Exception when trying to run:**

<div align = center>
<img src = "img/exception.PNG" width="70%">
</div>

This means that we need to regenerate the compute graph.

For fractal demo, we only need to run the `fractal.cgraph.py` and `fractal.kernel.py` in the `.\scripts` path, and close it after successful operation.

For implicit_fem demo, we have to make sure that the `--aot` parameter in the corresponding .py file is set to **True**. Then the next steps are the same as the fractal demo.