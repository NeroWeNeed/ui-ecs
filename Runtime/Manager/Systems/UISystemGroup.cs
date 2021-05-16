using Unity.Entities;
using Unity.Transforms;

namespace NeroWeNeed.UIECS.Systems
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public class UISystemGroup : ComponentSystemGroup { }

    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(UISystemGroup))]
    [UpdateAfter(typeof(UILayoutSystem))]
    public class UIBindMaterialPropertyGroup : ComponentSystemGroup { }
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(UISystemGroup))]
    [UpdateBefore(typeof(UILayoutSystem))]
    public class UIPreprocessingSystemGroup : ComponentSystemGroup { }
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class UIInitializationSystemGroup : ComponentSystemGroup { }

}