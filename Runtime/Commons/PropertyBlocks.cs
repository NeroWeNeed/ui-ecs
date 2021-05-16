using System;
using NeroWeNeed.UIECS;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;



[assembly:RegisterGenericComponentType(typeof(UIPropertyBlockTag<DisplayConfigBlock>))]

namespace NeroWeNeed.UIECS
{
    [PropertyBlock(null,true)]
    public struct DisplayConfigBlock : IPropertyBlock {
        [UIBool("visible", "display")]
        [DefaultValue(true,0)]
        [DefaultValue(true, 1)]
        public UIBool8 flags;
        public bool Visible { get => flags.Get(0); set => flags.Set(0, value); }
        public bool Display { get => flags.Get(1); set => flags.Set(1, value); }
    }

    

}