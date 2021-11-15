using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Build;
using Unity.Build.Common;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEditor.Networking.PlayerConnection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Hash128 = Unity.Entities.Hash128;
using Object = UnityEngine.Object;

namespace Unity.Scenes.Editor
{
    //@TODO: LiveLinkConnection is starting to be a relatively complex statemachine. Lets have unit tests for it in isolation...

    // A connection to a Player with a specific build configuration.
    // Each destination world in each player/editor, has it's own LiveLinkConnection so we can generate different data for different worlds.
    // For example server world vs client world.
    class LiveLinkConnection
    {
        static int                                 GlobalDirtyID = 0;

        static readonly MethodInfo                 _GetDirtyIDMethod = typeof(Scene).GetProperty("dirtyID", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetMethod;

        HashSet<Hash128>                           _LoadedScenes = new HashSet<Hash128>();
        HashSet<Hash128>                           _SentLoadScenes = new HashSet<Hash128>();
        NativeList<Hash128>                        _RemovedScenes;
        Dictionary<Hash128, LiveLinkDiffGenerator> _SceneGUIDToLiveLink = new Dictionary<Hash128, LiveLinkDiffGenerator>();
        int                                        _PreviousGlobalDirtyID;
        Dictionary<Hash128, Scene>                 _GUIDToEditScene = new Dictionary<Hash128, Scene>();

        BuildConfiguration                         _BuildConfiguration;
        UnityEngine.Hash128                        _BuildConfigurationArtifactHash;

        internal bool                              _IsEnabled = true;
        internal readonly Hash128                  _BuildConfigurationGUID;

        static readonly List<LiveLinkConnection>   k_AllConnections = new List<LiveLinkConnection>();

        public LiveLinkConnection(Hash128 buildConfigurationGuid)
        {
            _BuildConfigurationGUID = buildConfigurationGuid;
            if (buildConfigurationGuid != default)
            {
                _BuildConfiguration = BuildConfiguration.LoadAsset(buildConfigurationGuid);
                if (_BuildConfiguration == null)
                    Debug.LogError($"Unable to load build configuration asset from guid {buildConfigurationGuid}.");
            }

            Undo.postprocessModifications += PostprocessModifications;
            Undo.undoRedoPerformed += GlobalDirtyLiveLink;

            _RemovedScenes = new NativeList<Hash128>(Allocator.Persistent);
            k_AllConnections.Add(this);
        }

        public void Dispose()
        {
            k_AllConnections.Remove(this);
            Undo.postprocessModifications -= PostprocessModifications;
            Undo.undoRedoPerformed -= GlobalDirtyLiveLink;

            foreach (var livelink in _SceneGUIDToLiveLink.Values)
                livelink.Dispose();
            _SceneGUIDToLiveLink.Clear();
            _SceneGUIDToLiveLink = null;
            _RemovedScenes.Dispose();
        }

        public NativeArray<Hash128> GetInitialScenes(int playerId, Allocator allocator)
        {
            var sceneList = _BuildConfiguration.GetComponent<SceneList>();
            var nonEmbeddedStartupScenes = new List<string>();
            foreach (var path in sceneList.GetScenePathsToLoad())
            {
                if (SceneImporterData.CanLiveLinkScene(path))
                    nonEmbeddedStartupScenes.Add(path);
            }

            if (nonEmbeddedStartupScenes.Count > 0)
            {
                var sceneIds = new NativeArray<Hash128>(nonEmbeddedStartupScenes.Count, allocator);
                for (int i = 0; i < nonEmbeddedStartupScenes.Count; i++)
                    sceneIds[i] = new Hash128(AssetDatabase.AssetPathToGUID(nonEmbeddedStartupScenes[i]));
                return sceneIds;
            }
            return new NativeArray<Hash128>(0, allocator);
        }

        bool HasAssetDependencies()
        {
            foreach (var kvp in _SceneGUIDToLiveLink)
            {
                if (kvp.Value.HasAssetDependencies)
                    return true;
            }

            return false;
        }

        class GameObjectPrefabLiveLinkSceneTracker : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
                string[] movedFromAssetPaths)
            {
                foreach (var asset in importedAssets)
                {
                    if (asset.EndsWith(".prefab", true, System.Globalization.CultureInfo.InvariantCulture))
                    {
                        GlobalDirtyLiveLink();
                        return;
                    }
                }

                var connections = k_AllConnections;
                if (connections.Count == 0)
                    return;
                {
                    bool hasDependencies = false;
                    foreach (var c in connections)
                        hasDependencies |= c.HasAssetDependencies();

                    if (!hasDependencies)
                        return;
                }

                foreach (var asset in importedAssets)
                {
                    var guid = new GUID(AssetDatabase.AssetPathToGUID(asset));
                    foreach (var connection in connections)
                    {
                        foreach (var diff in connection._SceneGUIDToLiveLink)
                            diff.Value.MarkAssetChanged(guid);
                    }
                }
            }
        }

        public static void GlobalDirtyLiveLink()
        {
            GlobalDirtyID++;
            EditorUpdateUtility.EditModeQueuePlayerLoopUpdate();
        }

        internal static bool IsHotControlActive()
        {
            return GUIUtility.hotControl != 0;
        }

        UndoPropertyModification[] PostprocessModifications(UndoPropertyModification[] modifications)
        {
            foreach (var mod in modifications)
            {
                var target = GetGameObjectFromAny(mod.currentValue.target);
                if (target)
                {
                    var liveLink = GetLiveLink(target.scene);
                    if (liveLink != null)
                    {
                        liveLink.AddChanged(target);
                        EditorUpdateUtility.EditModeQueuePlayerLoopUpdate();
                    }
                }
            }

            if (HasAssetDependencies())
            {
                foreach (var mod in modifications)
                {
                    if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mod.currentValue.target, out var guidString, out long _))
                        continue;
                    var guid = new GUID(guidString);
                    foreach (var kvp in _SceneGUIDToLiveLink)
                        kvp.Value.MarkAssetChanged(guid);
                }
            }

            return modifications;
        }

        int GetSceneDirtyID(Scene scene)
        {
            if (scene.IsValid())
            {
                return (int)_GetDirtyIDMethod.Invoke(scene, null);
            }
            else
                return -1;
        }

        static GameObject GetGameObjectFromAny(Object target)
        {
            Component component = target as Component;
            if (component != null)
                return component.gameObject;
            return target as GameObject;
        }

        LiveLinkDiffGenerator GetLiveLink(Hash128 sceneGUID)
        {
            _SceneGUIDToLiveLink.TryGetValue(sceneGUID, out var liveLink);
            return liveLink;
        }

        LiveLinkDiffGenerator GetLiveLink(Scene scene)
        {
            //@TODO: Cache _SceneToLiveLink ???
            var guid = new GUID(AssetDatabase.AssetPathToGUID(scene.path));
            return GetLiveLink(guid);
        }

        public void ApplyLiveLinkSceneMsg(LiveLinkSceneMsg msg)
        {
            SetLoadedScenes(msg.LoadedScenes);
            QueueRemovedScenes(msg.RemovedScenes);
        }

        void SetLoadedScenes(NativeArray<Hash128> loadedScenes)
        {
            _LoadedScenes.Clear();
            foreach (var scene in loadedScenes)
            {
                if (scene != default)
                    _LoadedScenes.Add(scene);
            }
        }

        void QueueRemovedScenes(NativeArray<Hash128> removedScenes)
        {
            _RemovedScenes.AddRange(removedScenes);
        }

        public bool HasScene(Hash128 sceneGuid)
        {
            return _LoadedScenes.Contains(sceneGuid);
        }

        public bool HasLoadedScenes()
        {
            return _LoadedScenes.Count > 0;
        }

        void RequestCleanConversion()
        {
            foreach (var liveLink in _SceneGUIDToLiveLink.Values)
                liveLink.RequestCleanConversion();
        }

        public void Update(List<LiveLinkChangeSet> changeSets, NativeList<Hash128> loadScenes, NativeList<Hash128> unloadScenes, LiveLinkMode mode)
        {
            if (_LoadedScenes.Count == 0 && _SceneGUIDToLiveLink.Count == 0 && _RemovedScenes.Length == 0)
                return;

            // If build configuration changed, we need to trigger a full conversion
            if (_BuildConfigurationGUID != default)
            {
                // TODO: Allocs, needs better API
                var buildConfigurationDependencyHash = AssetDatabase.GetAssetDependencyHash(AssetDatabase.GUIDToAssetPath(_BuildConfigurationGUID.ToString()));
                if (_BuildConfigurationArtifactHash != buildConfigurationDependencyHash)
                {
                    _BuildConfigurationArtifactHash = buildConfigurationDependencyHash;
                    RequestCleanConversion();
                }
            }

            if (_PreviousGlobalDirtyID != GlobalDirtyID)
            {
                RequestCleanConversion();
                _PreviousGlobalDirtyID = GlobalDirtyID;
            }

            // By default all scenes need to have m_GameObjectSceneCullingMask, otherwise they won't show up in game view
            _GUIDToEditScene.Clear();
            for (int i = 0; i != EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                var sceneGUID = new GUID(AssetDatabase.AssetPathToGUID(scene.path));

                if (_LoadedScenes.Contains(sceneGUID))
                {
                    if (scene.isLoaded && sceneGUID != default(GUID))
                        _GUIDToEditScene.Add(sceneGUID, scene);
                }
            }

            foreach (var scene in _SceneGUIDToLiveLink)
            {
                if (!_GUIDToEditScene.ContainsKey(scene.Key))
                    unloadScenes.Add(scene.Key);
            }

            // Process scenes that are no longer loaded
            foreach (var scene in unloadScenes)
            {
                var liveLink = _SceneGUIDToLiveLink[scene];
                liveLink.Dispose();
                _SceneGUIDToLiveLink.Remove(scene);
                _SentLoadScenes.Remove(scene);
            }
            foreach (var scene in _RemovedScenes)
            {
                if (_SceneGUIDToLiveLink.TryGetValue(scene, out var liveLink))
                {
                    liveLink.Dispose();
                    _SceneGUIDToLiveLink.Remove(scene);
                }

                unloadScenes.Add(scene);
                _SentLoadScenes.Remove(scene);
            }
            _RemovedScenes.Clear();

            _SentLoadScenes.RemoveWhere(scene => !_LoadedScenes.Contains(scene));

            // Process all scenes that the player needs
            foreach (var sceneGuid in _LoadedScenes)
            {
                var isLoaded = _GUIDToEditScene.TryGetValue(sceneGuid, out var scene);

                // We are editing with live link. Ensure it is active & up to date
                if (isLoaded)
                {
                    var liveLink = GetLiveLink(sceneGuid);
                    if (liveLink == null || liveLink.DidRequestUpdate() || liveLink.LiveLinkDirtyID != GetSceneDirtyID(scene))
                    {
                        AddLiveLinkChangeSet(sceneGuid, changeSets, mode);
                    }
                }
                else
                {
                    if (_SentLoadScenes.Add(sceneGuid))
                        loadScenes.Add(sceneGuid);
                }
            }
        }

        void AddLiveLinkChangeSet(Hash128 sceneGUID, List<LiveLinkChangeSet> changeSets, LiveLinkMode mode)
        {
            var liveLink = GetLiveLink(sceneGUID);
            var editScene = _GUIDToEditScene[sceneGUID];

            // The current behaviour is that we do incremental conversion until we release the hot control
            // This is to avoid any unexpected stalls
            // Optimally the undo system would tell us if only properties have changed, but currently we don't have such an event stream.
            var sceneDirtyID = GetSceneDirtyID(editScene);
            var updateLiveLink = true;
            if (IsHotControlActive())
            {
                if (liveLink != null)
                {
                    sceneDirtyID = liveLink.LiveLinkDirtyID;
                }
                else
                {
                    updateLiveLink = false;
                    EditorUpdateUtility.EditModeQueuePlayerLoopUpdate();
                }
            }
            else
            {
                if (liveLink != null && liveLink.LiveLinkDirtyID != sceneDirtyID)
                    liveLink.RequestCleanConversion();
            }

            if (updateLiveLink)
            {
                //@TODO: need one place that LiveLinkDiffGenerators are managed. UpdateLiveLink does a Dispose()
                // but this must be paired with membership in _SceneGUIDToLiveLink. not good to have multiple places
                // doing ownership management.
                //
                // also: when implementing an improvement to this, be sure to deal with exceptions, which can occur
                // during conversion.

                if (liveLink != null)
                    _SceneGUIDToLiveLink.Remove(sceneGUID);

                try
                {
                    changeSets.Add(LiveLinkDiffGenerator.UpdateLiveLink(editScene, sceneGUID, ref liveLink, sceneDirtyID, mode, _BuildConfiguration));
                }
                finally
                {
                    if (liveLink != null)
                        _SceneGUIDToLiveLink.Add(sceneGUID, liveLink);
                }
            }
        }
    }
}
