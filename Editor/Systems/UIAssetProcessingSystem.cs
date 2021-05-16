using System;
using System.Collections.Generic;
using System.Linq;
using NeroWeNeed.UIECS.Authoring;
using TMPro;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using UnityEngine;

namespace NeroWeNeed.UIECS.Systems
{
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.GameObjectConversion)]
    [UpdateBefore(typeof(UIObjectConversionSystem))]
    public class UIFontAssetProcessingSystem : GameObjectConversionSystem
    {
        private List<Unity.Entities.Hash128> hashBuffer = new List<Unity.Entities.Hash128>();
        protected unsafe override void OnUpdate()
        {
            using var context = new BlobAssetComputationContext<Settings, UIFontInfo>(BlobAssetStore, 32, Unity.Collections.Allocator.Temp);
            
            Entities.ForEach((Entity entity, UIObject uiObject) =>
            {
                if (uiObject.view != null)
                {
                    var asset = uiObject.view;
                    var primary = GetPrimaryEntity(uiObject);
                    var buffer = DstEntityManager.AddBuffer<UIBlobAssetInfo>(primary);
                    if (asset.referencedAssets.Count > 0)
                    {
                        hashBuffer.Clear();
                        var referencedAssetBlobs = new NativeList<UIBlobAssetInfo>(8, Allocator.Temp);
                        //Register fonts
                        var fonts = asset.referencedAssets.OfType<TMP_FontAsset>().ToArray();

                        if (fonts.Length > 0)
                        {
                            var fontTypeHash = TypeHash.CalculateStableTypeHash(typeof(TMP_FontAsset));
                            var upper = (uint)(fontTypeHash >> 32);
                            var lower = (uint)(fontTypeHash & uint.MaxValue);
                            foreach (var font in fonts)
                            {

                                DeclareAssetDependency(uiObject.gameObject, font);
                                var hash = new Unity.Entities.Hash128((uint)(font?.GetHashCode() ?? 0), upper, lower, 0);
                                context.AssociateBlobAssetWithUnityObject(hash, uiObject.gameObject);
                                if (context.NeedToComputeBlobAsset(hash))
                                {
                                    var blob = font.CreateBlob(128, Unity.Collections.Allocator.Persistent);
                                    context.AddComputedBlobAsset(hash, blob);
                                    referencedAssetBlobs.Add(new UIBlobAssetInfo(hash, UnsafeUntypedBlobAssetReference.Create(blob)));
                                }
                                else
                                {
                                    context.GetBlobAsset(hash, out var blob);
                                    referencedAssetBlobs.Add(new UIBlobAssetInfo(hash, UnsafeUntypedBlobAssetReference.Create(blob)));
                                }
                                hashBuffer.Add(hash);
                            }
                        }
                        if (referencedAssetBlobs.Length > 0)
                        {
                            buffer.Capacity = referencedAssetBlobs.Length;
                            buffer.Length = referencedAssetBlobs.Length;
                            UnsafeUtility.MemCpy(buffer.GetUnsafePtr(), referencedAssetBlobs.GetUnsafePtr(), UnsafeUtility.SizeOf<UIBlobAssetInfo>() * referencedAssetBlobs.Length);
                            EntityManager.AddComponentObject(entity, BlobData.Create(hashBuffer));
                        }
                        referencedAssetBlobs.Dispose();
                    }
                }
            });
        }
        private struct Settings
        {
            public Unity.Entities.Hash128 hash;
        }
    }
    internal class BlobData : ScriptableObject
    {
        public static BlobData Create(IEnumerable<Unity.Entities.Hash128> info)
        {
            var obj = CreateInstance<BlobData>();
            obj.hashes = info.ToArray();
            return obj;
        }
        [SerializeField]
        private Unity.Entities.Hash128[] hashes;
        public bool IsValid { get => hashes != null; }
        public int IndexOf(Unity.Entities.Hash128 hash) => hashes == null ? -1 : Array.IndexOf(hashes, hash);
    }
}