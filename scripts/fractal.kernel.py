import taichi as ti
import numpy as np

ti.init(arch=ti.vulkan)

n = 320
canvas = ti.ndarray(dtype=ti.f32, shape=(n * 2, n))


@ti.func
def complex_sqr(z):
    return ti.Vector([z[0]**2 - z[1]**2, z[1] * z[0] * 2])


@ti.kernel
def fractal(t: ti.f32, canvas: ti.types.ndarray()):
    for i, j in canvas:  # Parallelized over all pixels
        c = ti.Vector([-0.8, ti.cos(t) * 0.2])
        z = ti.Vector([i / n - 1, j / n - 0.5]) * 2
        iterations = 0
        while z.norm() < 20 and iterations < 50:
            z = complex_sqr(z) + c
            iterations += 1
        canvas[i, j] = 1 - iterations * 0.02

mod = ti.aot.Module(ti.vulkan)
mod.add_kernel(fractal,
                template_args={
                    'canvas': canvas,
                })
mod.save("Assets/TaichiModules/fractal", "")

gui = ti.GUI("Julia Set", res=(n * 2, n))

i = 0
while True:
    i += 1
    # It will crash here because the aot compilation uses a different vulkan
    # version than that for realtime running. But the aot compilation result
    # should have been dumped in the output directory at this point.
    fractal(0.03 * i, canvas)
    canvas2 = np.repeat(canvas.to_numpy().reshape(n * 2,n,1), 3, 2)

    gui.set_image(canvas2)
    gui.show()
