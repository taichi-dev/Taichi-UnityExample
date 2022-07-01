using System.Collections;
using System.Collections.Generic;
using Taichi;
using UnityEngine;

public class InputOutput : MonoBehaviour {
    Module_input_output _Module;
    Module_input_output.Data _Data;

    void Start() {
        _Module = new Module_input_output(Application.dataPath + "/TaichiModules/input_output");

        var input1 = new float[256];
        for (int i = 0; i < input1.Length; ++i) {
            input1[i] = i + 1;
        }
        var input2 = new float[256];
        for (int i = 0; i < input2.Length; ++i) {
            input2[i] = i;
        }

        _Data = new Module_input_output.Data();
        _Data.input1.CopyFromArray(input1);
        _Data.input2.CopyFromArray(input2);
    }

    void Update() {
        var input1 = _Data.input1.ToArray();
        var input2 = _Data.input2.ToArray();
        var output1 = _Data.output1.ToArray();
        var output2 = _Data.output2.ToArray();

        _Module.Apply(_Data);
        Runtime.Submit();
    }
}
