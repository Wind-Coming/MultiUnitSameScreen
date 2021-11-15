using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Build;
using Unity.Entities.Conversion;

namespace Unity.Entities.Tests.Conversion
{
    class GameObjectConversionSettingsTests
    {
        [Test]
        public void Fork_WithZeroNamespaceID_Throws()
        {
            var settings = new GameObjectConversionSettings();

            Assert.That(() => settings.Fork(0), Throws.Exception
                .With.TypeOf<ArgumentException>()
                .With.Message.Contains("is reserved"));
        }

        [Test]
        public void Fork_CopiesOnlyForkedFields()
        {
            using (var world = new World("test world"))
            {
                var settings = new GameObjectConversionSettings
                {
                    DestinationWorld          = world,
                    SceneGUID                 = new Hash128(1, 2, 3, 4),
                    DebugConversionName       = "test name",
                    ConversionFlags           = GameObjectConversionUtility.ConversionFlags.AddEntityGUID,
#if UNITY_EDITOR
                    BuildConfiguration        = BuildConfiguration.CreateInstance(),
                    //AssetImportContext        = new AssetImportContext(), // << private
#endif
                    ExtraSystems              = new[] { typeof(int) },
                    Systems                   = new List<Type> { typeof(int) },
                    NamespaceID               = 123,
                    ConversionWorldCreated    = _ => {},
                    ConversionWorldPreDispose = _ => {},
                };

                var forked = settings.Fork(234);

                // forked
                Assert.That(forked.DestinationWorld,          Is.EqualTo(settings.DestinationWorld));
                Assert.That(forked.SceneGUID,                 Is.EqualTo(settings.SceneGUID));
                Assert.That(forked.DebugConversionName,       Is.EqualTo(settings.DebugConversionName + $":{234:x2}"));
                Assert.That(forked.ConversionFlags,           Is.EqualTo(settings.ConversionFlags));
#if UNITY_EDITOR
                Assert.That(forked.BuildConfiguration,        Is.EqualTo(settings.BuildConfiguration));
#endif

                // non-forked
                Assert.That(forked.ExtraSystems,              Is.Empty);
                Assert.That(forked.Systems,                   Is.Null);
                Assert.That(forked.NamespaceID,               Is.EqualTo(234));
                Assert.That(forked.ConversionWorldCreated,    Is.Null);
                Assert.That(forked.ConversionWorldPreDispose, Is.Null);
            }
        }

        [Test]
        public void WithExtraSystems_WithRedundantCall_Throws()
        {
            var settings = new GameObjectConversionSettings();
            settings.WithExtraSystem<int>();

            Assert.That(() => settings.WithExtraSystem<float>(), Throws.Exception
                .With.TypeOf<InvalidOperationException>()
                .With.Message.Contains("already initialized"));
        }

        [UpdateInGroup(typeof(GameObjectBeforeConversionGroup))]
        class TestConversionSystem : GameObjectConversionSystem
        {
            protected override void OnUpdate()
            {
            }
        }

        [Test]
        public void Systems_OnlySystemsFromListAndGameObjectConversionSystemAreAdded()
        {
            using (var world = new World("test world"))
            {
                var settings = new GameObjectConversionSettings
                {
                    DestinationWorld          = world,
                    SceneGUID                 = new Hash128(1, 2, 3, 4),
                    DebugConversionName       = "test name",
                    ConversionFlags           = GameObjectConversionUtility.ConversionFlags.AddEntityGUID,
#if UNITY_EDITOR
                    BuildConfiguration        = BuildConfiguration.CreateInstance(),
#endif
                    Systems                   = new List<Type> {typeof(TestConversionSystem)},
                    NamespaceID               = 123,
                    ConversionWorldCreated    = _ => {},
                    ConversionWorldPreDispose = _ => {},
                };
                using (var conversionWorld = settings.CreateConversionWorld())
                {
                    foreach (var system in conversionWorld.Systems)
                    {
                        if (system is ComponentSystemGroup || system == null)
                            continue;
                        Assert.That(system is TestConversionSystem || system is GameObjectConversionMappingSystem, $"System is of unexpected type {system.GetType()}");
                    }
                }
            }
        }
    }
}
