
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace NeroWeNeed.UIECS {
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void Layout(UIConfigHandle configHandle, UIConfigHandle* children, int totalChildren, float2* positions,UIExtraDataHandle extraData,long extraDataLength);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void GenerateMeshData(UIConfigHandle configHandle, in float4 layout, UIVertexData* vertexData, UIExtraDataHandle extraData, long extraDataLength);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void Constrain(UIConfigHandle configHandle, UIConfigHandle* children, int childIndex, int totalChildren, out float4 childConstraints, UIExtraDataHandle extraData, long extraDataLength);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void Size(UIConfigHandle configHandle, UIConfigHandle* children, int totalChildren, out float2 size, UIExtraDataHandle extraData, long extraDataLength);
    public interface IUINode { }
    

    public interface IUIElementLayout {
        public unsafe void Layout(UIConfigHandle configHandle, UIConfigHandle* children, int totalChildren, float2* positions, UIExtraDataHandle extraData, long extraDataLength);
    }
    
    public interface IUIElementConstrain {
        public unsafe void Constrain(UIConfigHandle configHandle, UIConfigHandle* children, int childIndex, int totalChildren, out float4 childConstraints, UIExtraDataHandle extraData, long extraDataLength);
    }
    
    public interface IUIElementSize {
        public unsafe void Size(UIConfigHandle configHandle, UIConfigHandle* children, int totalChildren, out float2 size, UIExtraDataHandle extraData, long extraDataLength);
    }
    
    public interface IUIElementGenerateMeshData {
        public unsafe void GenerateMeshData(UIConfigHandle configHandle, in float4 layout, UIVertexData* vertexData, UIExtraDataHandle extraData, long extraDataLength);
    }
    public interface IUIElement : IUIElementConstrain, IUIElementGenerateMeshData, IUIElementLayout, IUIElementSize, IUINode { }
    public interface IUITerminalElement : IUIElementGenerateMeshData, IUIElementSize, IUINode { }
    
}



