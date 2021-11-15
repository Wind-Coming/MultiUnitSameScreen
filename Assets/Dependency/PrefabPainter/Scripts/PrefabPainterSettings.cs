
#if UNITY_EDITOR


using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;


namespace nTools.PrefabPainter
{
    public enum OrientationMode
    {
        None,
        AlongSurfaceNormal,
        AlongBrushStroke,
        X,
        Y,
        Z,
    }

    public enum TransformMode
    {
        Relative,
        Absolute,
    }

    public enum Placement
    {
        World,
        HitObject,
        CustomObject,
    }

    public enum ScaleMode
    {
        Uniform,
        PerAxis,
    }
    
    [System.Serializable]
    public class BrushPreset
    {        
        public List<GameObject> prefabs = new List<GameObject>();

        public string name;

        public float brushSize;
        public float eraseBrushSize;
        public float brushSpacing;

        public Vector3 positionOffset;

        public TransformMode    orientationTransformMode;
        public OrientationMode  orientationMode;
        public bool             flipOrientation;
        public Vector3          rotation;
        public Vector3          randomizeOrientation;

        public TransformMode    scaleTransformMode;
        public ScaleMode        scaleMode;
        public float            scaleUniformMin;
        public float            scaleUniformMax;
        public Vector3          scalePerAxisMin;
        public Vector3          scalePerAxisMax;

		public bool 			selected = false;
        public Vector2Int       lodRange;


        public GameObject prefab {
            get {
                return prefabs.Count > 0 ? prefabs[0] : null;
            }
        }


		public Texture prefabPreview {
			get {                
				if (prefab != null)
				{                    
					Texture previewTexture = GetAssetPreview(prefab);
					if (previewTexture != null)
						return previewTexture;

					previewTexture = AssetPreview.GetMiniThumbnail(prefab);
					if (previewTexture != null)
						return previewTexture;

					previewTexture = (Texture2D)AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(prefab));
					if (previewTexture != null)
						return previewTexture;

     				return AssetPreview.GetMiniTypeThumbnail(typeof(GameObject));
				}
				return null;
			}
		}


    private Bounds GetBounds(GameObject obj)
    {
        Vector3 Min = new Vector3(99999, 99999, 99999);
        Vector3 Max = new Vector3(-99999, -99999, -99999);
        Renderer[] renders = obj.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renders.Length; i++)
        {
            if (renders[i].bounds.min.x < Min.x)
                Min.x = renders[i].bounds.min.x;
            if (renders[i].bounds.min.y < Min.y)
                Min.y = renders[i].bounds.min.y;
            if (renders[i].bounds.min.z < Min.z)
                Min.z = renders[i].bounds.min.z;

            if (renders[i].bounds.max.x > Max.x)
                Max.x = renders[i].bounds.max.x;
            if (renders[i].bounds.max.y > Max.y)
                Max.y = renders[i].bounds.max.y;
            if (renders[i].bounds.max.z > Max.z)
                Max.z = renders[i].bounds.max.z;
        }

        Vector3 center = (Min + Max) / 2;
        Vector3 size = new Vector3(Max.x - Min.x, Max.y - Min.y, Max.z - Min.z);
        return new Bounds(center, size);
    }

	Dictionary<UnityEngine.Object, Texture> o2t = new Dictionary<UnityEngine.Object, Texture>();
    private Texture GetAssetPreview(UnityEngine.Object obj)
    {
		if(o2t.ContainsKey(obj))
		{
			if( o2t[obj] != null)
			{
				return o2t[obj];
			}
			else
			{
				o2t.Remove(obj);
			}
		}
        GameObject clone = GameObject.Instantiate(obj) as GameObject;
        Transform cloneTransform = clone.transform;
        cloneTransform.position = new Vector3(-1000, -1000, -1000);
        //cloneTransform.localRotation = new Quaternion(0, 0, 0, 1);

        LodObj lo = clone.GetComponent<LodObj>();
        if(lo != null)
            lo.SetMaxLod();

        Transform[] all = clone.GetComponentsInChildren<Transform>();
        foreach (Transform trans in all)
        {
            trans.gameObject.layer = 21;
        }

        Bounds bounds = GetBounds(clone);
        Vector3 Min = bounds.min;
        Vector3 Max = bounds.max;
        GameObject cameraObj = new GameObject("render camera");
        cameraObj.transform.position = new Vector3(cloneTransform.position.x, (Max.y + Min.y) / 2f, Max.z + 5);

        Vector3 center = new Vector3(cloneTransform.position.x, (Max.y + Min.y) / 2f, cloneTransform.position.z);

        cameraObj.transform.LookAt(center);

        Camera renderCamera = cameraObj.AddComponent<Camera>();
        renderCamera.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        renderCamera.clearFlags = CameraClearFlags.Color;
        renderCamera.cameraType = CameraType.Preview;
        renderCamera.cullingMask = 1 << 21;
        int angle = (int)(Mathf.Atan2((Max.y - Min.y) / 2, (Max.z - Min.z)) * 180 / 3.1415f * 2);
        renderCamera.fieldOfView = 60;
        renderCamera.depth = 10;

        RenderTexture texture = new RenderTexture(64, 64, 0, RenderTextureFormat.Default);
        renderCamera.targetTexture = texture;

        renderCamera.RenderDontRestore();

        RenderTexture tex = new RenderTexture(64, 64, 0, RenderTextureFormat.Default);
        Graphics.Blit(texture, tex);

        UnityEngine.Object.DestroyImmediate(clone);
        UnityEngine.Object.DestroyImmediate(cameraObj);
		o2t.Add(obj, tex);
        return tex;
    }

        public BrushPreset() { Reset(); }
        public BrushPreset(GameObject newPrefab, PrefabPainterSettings settings) { Reset(); AssignPrefab(newPrefab, settings); name = newPrefab.name; }

        public BrushPreset(BrushPreset other)
        {
            Reset();

            prefabs = new List<GameObject>(other.prefabs);

            name = other.name;

            brushSize = other.brushSize;
            eraseBrushSize = other.eraseBrushSize;
            brushSpacing = other.brushSpacing;

            positionOffset = other.positionOffset;

            orientationTransformMode = other.orientationTransformMode;
            orientationMode = other.orientationMode;
            flipOrientation = other.flipOrientation;
            rotation = other.rotation;
            randomizeOrientation = other.randomizeOrientation;

            scaleTransformMode = other.scaleTransformMode;
            scaleMode = other.scaleMode;
            scaleUniformMin = other.scaleUniformMin;
            scaleUniformMax = other.scaleUniformMax;
            scalePerAxisMin = other.scalePerAxisMin;
            scalePerAxisMax = other.scalePerAxisMax;
        }


        public void Reset()
        {
            brushSize = 1.0f;
            eraseBrushSize = 1.0f;
            brushSpacing = 0.5f;

            positionOffset = new Vector3(0 ,0 ,0);

            orientationTransformMode = TransformMode.Relative;
            orientationMode = OrientationMode.AlongSurfaceNormal;
            flipOrientation = false;
            rotation = new Vector3(0, 0, 0);
            randomizeOrientation = new Vector3(0, 0, 0);


            scaleTransformMode = TransformMode.Relative;
            scaleMode = ScaleMode.Uniform;
            scaleUniformMin = 1.0f;
            scaleUniformMax = 1.0f;
            scalePerAxisMin = new Vector3(1, 1, 1);
            scalePerAxisMax = new Vector3(1, 1, 1);
        }

        public void AssignPrefab(GameObject newPrefab, PrefabPainterSettings settings)
        {
            LodObj lo = newPrefab.GetComponent<LodObj>();
            if(lo != null && lo.LodGameObjs.Length > 0)
            {
                int maxLod = lo.LodGameObjs[lo.LodGameObjs.Length - 1].lodRange.y;
                int minLod = lo.LodGameObjs[0].lodRange.x;

                if(maxLod < settings.LodBrushSize.Count)
                {
                    Vector4 v = settings.LodBrushSize[maxLod];
                    brushSize = v.x;
                    brushSpacing = v.y;
                    scaleUniformMin = v.z;
                    scaleUniformMax = v.w;
                }

                lodRange = new Vector2Int(minLod, maxLod);
            }

            prefabs.Clear();
            prefabs.Add(newPrefab);
        }


    }





    //
    // class PrefabPainterSettings
    //
    public class PrefabPainterSettings : ScriptableObject
    {
        public bool paintRandom = false;
        public bool paintOnSelected = false;
        public int  paintLayers = ~0;

        public bool overwritePrefabLayer = false;
        public int  prefabPlaceLayer = 0;

        public bool groupPrefabs = true;

        public float brushSizeMax = 20.0f;
        public float brushSpacingMax = 5.0f;


        public bool openLodInEditor = true;
        public bool showAll = true;
        public List<BrushPreset> presets = new List<BrushPreset>();

        [NonSerialized]
        public List<BrushPreset> lodPresets = new List<BrushPreset>();


        public bool brushSettingsFoldout = true;
        public bool positionSettingsFoldout = true;
        public bool orientationSettingsFoldout = true;
        public bool scaleSettingsFoldout = true;
        public bool commonSettingsFoldout = true;

        public List<Vector4> LodBrushSize = new List<Vector4>();
        void OnEnable()
        {
        }




		public bool HasMultipleSelectedPresets()
		{
			int selectedCount = 0;
			for (int i = 0; i < presets.Count; i++)
			{
				if (presets[i].selected)
					selectedCount++;

				if (selectedCount > 1)
					return true;
			}
			return false;
		}

		public bool HasSelectedPresets()
		{
			for (int i = 0; i < presets.Count; i++)
			{
				if (presets[i].selected)
					return true;				
			}
			return false;
		}

		public BrushPreset GetFirstSelectedPreset()
		{
            if (paintRandom)
            {
                return presets[UnityEngine.Random.Range(0, presets.Count)];
            }
            else
            {
                for (int i = 0; i < presets.Count; i++)
                {
                    if (presets[i].selected)
                        return presets[i];
                }
            }
			return null;
		}

		public bool IsPresetSelected(int presetIndex)
		{
			if (presetIndex >= 0 && presetIndex < presets.Count)
			{
				return presets[presetIndex].selected;
			}
			return false;
		}


		public void SelectPreset(List<BrushPreset> presetList, int presetIndex)
		{
			if (presetIndex >= 0 && presetIndex < presetList.Count)
			{
				presets.ForEach ((preset) => preset.selected = false);
				presetList[presetIndex].selected = true;
			}
		}

		public void SelectPresetAdd(List<BrushPreset> presetList, int presetIndex)
		{
			if (presetIndex >= 0 && presetIndex < presetList.Count)
			{
				presetList[presetIndex].selected = true;
			}
		}

		public void SelectPresetRange(List<BrushPreset> presetList, int toPresetIndex)
		{
			if (toPresetIndex < 0 && toPresetIndex >= presetList.Count)
				return;

			int rangeMin = toPresetIndex;
			int rangeMax = toPresetIndex;

			for (int i = 0; i < presetList.Count; i++)
			{
				if (presetList[i].selected)
				{
					rangeMin = Mathf.Min(rangeMin, i);
					rangeMax = Mathf.Max(rangeMax, i);
				}
			}
			for (int i = rangeMin; i <= rangeMax; i++) {
				presetList[i].selected = true;
			}
		}

		public void DeselectAllPresets()
		{
			presets.ForEach ((preset) => preset.selected = false);
		}


		public void DuplicateSelectedPresets()
		{
			if (!HasSelectedPresets ())
				return;

			Undo.RegisterCompleteObjectUndo(this, "Duplicate Preset(s)");

			for (int presetIndex = 0; presetIndex < presets.Count; presetIndex++)
			{
				if (presets[presetIndex].selected)
				{
					BrushPreset duplicate = new BrushPreset (presets [presetIndex]);

					presets [presetIndex].selected = false;
					duplicate.selected = true;
					
					presets.Insert(presetIndex, duplicate);

					presetIndex++; // move over new inserted duplicate
				}
			}
		}

		public void DeleteSelectedPresets()
		{
			if (!HasSelectedPresets ())
				return;

			Undo.RegisterCompleteObjectUndo (this, "Delete Preset(s)");

			presets.RemoveAll ((preset) => preset.selected);
		}

		public void ResetSelectedPresets()
		{
			if (!HasSelectedPresets ())
				return;

			Undo.RegisterCompleteObjectUndo (this, "Reset Preset(s)");

			presets.ForEach ((preset) => preset.Reset());
		}
    }

}

#endif
