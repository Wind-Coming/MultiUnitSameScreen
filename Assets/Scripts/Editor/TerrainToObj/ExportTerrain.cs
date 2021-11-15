using System.IO; 
using System.Text;
using UnityEditor;
using UnityEngine;
using System;

 enum SaveFormat {Triangles, Quads} 

 enum SaveResolution {Full, Half, Quarter, Eighth, Sixteenth} 

 class ExportTerrain : EditorWindow {

    SaveFormat saveFormat = SaveFormat.Triangles;

    SaveResolution saveResolution = SaveResolution.Half; 

     static TerrainData terrain ; 

     static Vector3 terrainPos;


    int tCount ; 

     int counter ; 

     int totalCount ; 

       

     [MenuItem ("Tools/地形/Export To Obj...")] 

     static void Init () { 

         terrain = null;

        Terrain terrainObject = Selection.activeObject as Terrain; 

         if (!terrainObject) { 

             terrainObject = Terrain.activeTerrain; 

         } 

         if (terrainObject) { 

             terrain = terrainObject.terrainData; 

             terrainPos = terrainObject.transform.position; 

         } 

         EditorWindow.GetWindow(typeof(ExportTerrain)).Show(); 

     }

    [MenuItem("Tools/地形/Export Texture")]
    static void Apply()
    {
        Texture2D texture = Selection.activeObject as Texture2D;
        if (texture == null) {
            EditorUtility.DisplayDialog("Select Texture", "You Must Select a Texture first!", "Ok");
            return;
        }

        var bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/exported_texture.png", bytes);

        var bytes2 = texture.EncodeToJPG();
        File.WriteAllBytes(Application.dataPath + "/exported_textureJPG.jpg", bytes2);
    }


    void OnGUI () { 

         if (!terrain) { 

             GUILayout.Label("No terrain found"); 

             if (GUILayout.Button("Cancel")) { 

                 EditorWindow.GetWindow(typeof(ExportTerrain)).Close(); 
             } 

             return; 

         } 

         saveFormat = (SaveFormat)EditorGUILayout.EnumPopup("Export Format", saveFormat); 

         saveResolution = (SaveResolution)EditorGUILayout.EnumPopup("Resolution", saveResolution); 

           

         if (GUILayout.Button("Export")) { 

             Export(); 

         } 

     } 

       

     void Export () { 

         var fileName = EditorUtility.SaveFilePanel("Export .obj file", "", "Terrain", "obj"); 

         var w = terrain.heightmapResolution; 

         var h = terrain.heightmapResolution; 

         var meshScale = terrain.size; 

         int tRes = (int)Mathf.Pow(2, (int)(saveResolution)); 

         meshScale = new  Vector3(meshScale.x/(w-1)*tRes, meshScale.y, meshScale.z/(h-1)*tRes); 

         var uvScale = new Vector2(1.0f/(w-1), 1.0f/(h-1)); 

         var tData = terrain.GetHeights(0, 0, w, h); 

           

         w = (int)((w-1) / tRes + 1); 

         h = (int)((h-1) / tRes + 1); 

         var tVertices = new Vector3[w * h]; 

         var tUV = new Vector2[w * h];

        int[] tPolys;
         if (saveFormat == SaveFormat.Triangles) {

            tPolys = new int[(w-1) * (h-1) * 6]; 

         } 

         else { 

             tPolys = new int[(w-1) * (h-1) * 4]; 

         } 

           

         // Build vertices and UVs 

         for ( int y = 0; y < h; y++) { 

             for (int x = 0; x < w; x++) { 

                 tVertices[y*w + x] = Vector3.Scale(meshScale,new  Vector3(x, tData[x*tRes,y*tRes], y)) + terrainPos; 

                 tUV[y*w + x] = Vector2.Scale( new Vector2(x*tRes, y*tRes), uvScale); 

             } 

         } 

       

         var index = 0; 

         if (saveFormat == SaveFormat.Triangles) { 

             // Build triangle indices: 3 indices into vertex array for each triangle 

             for (int y = 0; y < h-1; y++) { 

                 for (int x = 0; x < w-1; x++) { 

                     // For each grid cell output two triangles 

                     tPolys[index++] = (y     * w) + x; 

                     tPolys[index++] = ((y+1) * w) + x; 

                     tPolys[index++] = (y     * w) + x + 1; 

           

                     tPolys[index++] = ((y+1) * w) + x; 

                     tPolys[index++] = ((y+1) * w) + x + 1; 

                     tPolys[index++] = (y     * w) + x + 1; 

                 } 

             } 
         } 

         else { 

             // Build quad indices: 4 indices into vertex array for each quad 

             for (int y = 0; y < h-1; y++) { 

                 for (int x = 0; x < w-1; x++) { 

                     // For each grid cell output one quad 

                     tPolys[index++] = (y     * w) + x; 

                     tPolys[index++] = ((y+1) * w) + x; 

                     tPolys[index++] = ((y+1) * w) + x + 1; 

                     tPolys[index++] = (y     * w) + x + 1; 

                 } 

             }   

         } 

       

         // Export to .obj 

         try { 

             var sw = new StreamWriter(fileName); 

             sw.WriteLine("# Unity terrain OBJ File"); 

               

             // Write vertices 

             System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US"); 

             counter = tCount = 0; 

             totalCount = (tVertices.Length*2 + (saveFormat == SaveFormat.Triangles? tPolys.Length/3 : tPolys.Length/4)) / 1000; 

             for (int i = 0; i < tVertices.Length; i++) { 

                 UpdateProgress(); 

                 var sb = new  StringBuilder("v ", 20); 
                 // StringBuilder stuff is done this way because it's faster than using the "{0} {1} {2}"etc. format 

                 // Which is important when you're exporting huge terrains. 

                 sb.Append(tVertices[i].x.ToString()).Append(" "). 

                    Append(tVertices[i].y.ToString()).Append(" "). 

                    Append(tVertices[i].z.ToString()); 

                 sw.WriteLine(sb); 

             } 

             // Write UVs 

             for ( int i = 0; i < tUV.Length; i++) { 

                 UpdateProgress();

                StringBuilder sb =  new StringBuilder("vt ", 22); 

                 sb.Append(tUV[i].x.ToString()).Append(" "). 

                    Append(tUV[i].y.ToString()); 

                 sw.WriteLine(sb); 

             } 

             if (saveFormat == SaveFormat.Triangles) { 

                 // Write triangles 

                 for (int i = 0; i < tPolys.Length; i += 3) { 

                     UpdateProgress();

                    StringBuilder sb = new  StringBuilder("f ", 43); 

                     sb.Append(tPolys[i]+1).Append("/").Append(tPolys[i]+1).Append(" "). 

                        Append(tPolys[i+1]+1).Append("/").Append(tPolys[i+1]+1).Append(" "). 

                        Append(tPolys[i+2]+1).Append("/").Append(tPolys[i+2]+1); 

                     sw.WriteLine(sb); 

                 } 

             } 

             else { 

                 // Write quads 

                 for (int i = 0; i < tPolys.Length; i += 4) { 

                     UpdateProgress();

                    StringBuilder sb = new  StringBuilder("f ", 57); 

                     sb.Append(tPolys[i]+1).Append("/").Append(tPolys[i]+1).Append(" "). 

                        Append(tPolys[i+1]+1).Append("/").Append(tPolys[i+1]+1).Append(" "). 

                        Append(tPolys[i+2]+1).Append("/").Append(tPolys[i+2]+1).Append(" "). 

                        Append(tPolys[i+3]+1).Append("/").Append(tPolys[i+3]+1); 

                     sw.WriteLine(sb); 

                 }       

             } 

         } 

         catch (Exception err) { 

             Debug.Log("Error saving file: " + err.Message); 

         } 


           

         terrain = null; 

         EditorUtility.ClearProgressBar(); 

         EditorWindow.GetWindow(typeof( ExportTerrain )).Close(); 

     } 

       

     void UpdateProgress () { 

         if (counter++ == 1000) { 

             counter = 0; 

             EditorUtility.DisplayProgressBar("Saving...", "", Mathf.InverseLerp(0, totalCount, ++tCount)); 

         } 

     } 

 } 
