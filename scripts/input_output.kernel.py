import taichi as ti
import numpy as np

ti.init(arch=ti.vulkan)

n = 16
input1 = ti.ndarray(dtype=ti.f32, shape=(n, n))
input2 = ti.ndarray(dtype=ti.f32, shape=(n, n))
output1 = ti.ndarray(dtype=ti.f32, shape=(n, n))
output2 = ti.ndarray(dtype=ti.f32, shape=(n, n))

input1.from_numpy(np.arange(0, 256).reshape(-1, 16) + 1)
input2.from_numpy(np.arange(0, 256).reshape(-1, 16))


@ti.func
def complex_sqr(z):
    return ti.Vector([z[0]**2 - z[1]**2, z[1] * z[0] * 2])


@ti.kernel
def input_output(input1: ti.types.ndarray(), input2: ti.types.ndarray(), output1: ti.types.ndarray(), output2: ti.types.ndarray()):
    for i, j in output1:
        #output1[i, j] = input1[i, j] + input2[i, j]
        output1[i, j] = 123
    for i, j in output2:
        #output2[i, j] = input1[i, j] - input2[i, j]
        output2[i, j] = 123

mod = ti.aot.Module(ti.vulkan)
mod.add_kernel(input_output,
                template_args={
                    'input1': input1,
                    'input2': input2,
                    'output1': output1,
                    'output2': output2,
                })
mod.save("Assets/TaichiModules/input_output", "")
