using System.Linq;
using Taichi;

class Module_implicit_fem {


    public class Data {

        public Data(int vertexCount, int faceCount, int edgeCount, int cellCount) {
        }
    };

    public bool Initialize(Data data) {
        // Note these are `&` (logical and) and is not `&&` (fusing logical
        // and). So all the initialization states are checked and memory
        // imports are triggered.
        return true;
    }

    public void Apply(Data data) {
    }
}
