using System;
using Unity.Collections;
using Unity.Entities;

namespace NeroWeNeed.UIECS.Editor
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class UIModelPropertyInitializerAttribute : Attribute
    {
        public Type value;
        public UIModelPropertyInitializerAttribute(Type value)
        {
            this.value = value;
        }
    }
    public interface IUIModelPropertyInitializer
    {
        public void Initialize(Type componentType, EntityManager entityManager, int index, UIViewAsset viewAsset, NativeArray<Entity> entities);
    }
    [UIModelPropertyInitializer(typeof(UIValueNodeBufferBinding<,>))]
    [UIModelPropertyInitializer(typeof(UIValueNodeComponentBinding<,>))]
    public class UIValueNodeBindingInitializer : IUIModelPropertyInitializer
    {

        public void Initialize(Type componentType, EntityManager entityManager, int index, UIViewAsset viewAsset, NativeArray<Entity> entities)
        {
            int parentIndex = viewAsset[index].parentIndex;
            var targetComponent = componentType.GenericTypeArguments[0];
            while (parentIndex >= 0)
            {
                var parentEntity = entities[parentIndex];
                if (entityManager.HasComponent(parentEntity, targetComponent))
                {
                    typeof(UIValueNodeBindingInitializer).GetMethod(nameof(SetNodeBinding), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).MakeGenericMethod(componentType).Invoke(null, new object[] { entityManager, parentEntity, entities[index] });
                    break;
                }
                parentIndex = viewAsset[parentIndex].parentIndex;
            }
        }
        private static void SetNodeBinding<TComponent>(EntityManager entityManager, Entity containerEntity, Entity entity) where TComponent : struct, IComponentData, IUIValueBinding
        {
            entityManager.SetComponentData(entity, new TComponent { Value = containerEntity });
        }
    }
}