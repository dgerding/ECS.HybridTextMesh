using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace E7.ECS.SpriteFont
{
    [UpdateInGroup(typeof(SimulationGroup))]
    internal class CharacterPrefabLookupPreparationSystem : JobComponentSystem
    {
        List<NativeHashMap<char, Entity>> forDispose;
        BeginInitializationEntityCommandBufferSystem ecbs;
        EntityQuery noLookupFontAssetQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            forDispose = new List<NativeHashMap<char, Entity>>(4);
            ecbs = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();

            RequireForUpdate(noLookupFontAssetQuery);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            foreach (var fd in forDispose)
            {
                fd.Dispose();
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var ecb = ecbs.CreateCommandBuffer();
            Entities.WithAll<FontAsset>().ForEach(
                    (Entity e, in SpriteFontAssetHolder holder, in DynamicBuffer<CharacterPrefabBuffer> buffer) =>
                    {
                        var nativeHashMap = new NativeHashMap<char, Entity>(buffer.Length, Allocator.Persistent);
                        var nativeHashMapWithScale =
                            new NativeHashMap<char, Entity>(buffer.Length, Allocator.Persistent);
                        forDispose.Add(nativeHashMap);
                        forDispose.Add(nativeHashMapWithScale);

                        for (int i = 0; i < buffer.Length; i++)
                        {
                            nativeHashMap.Add(buffer[i].character.ToString()[0], buffer[i].prefab);
                            nativeHashMapWithScale.Add(buffer[i].character.ToString()[0], buffer[i].prefabWithScale);
                        }

                        EntityManager.AddSharedComponentData(e,
                            new CharacterPrefabLookup
                            {
                                characterToPrefabEntity = nativeHashMap,
                                characterToPrefabEntityWithScale = nativeHashMapWithScale
                            });
                    })
                .WithStoreEntityQueryInField(ref noLookupFontAssetQuery)
                .WithStructuralChanges()
                .Run();
            ecb.RemoveComponent<CharacterPrefabBuffer>(noLookupFontAssetQuery);
            return default;
        }
    }
}