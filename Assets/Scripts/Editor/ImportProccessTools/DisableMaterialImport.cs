using UnityEngine;
using System.Collections;
using UnityEditor;
 
class DisableMaterialImport : AssetPostprocessor {
	//void OnPreprocessModel ()
	//{
	//	ModelImporter modelImporter = assetImporter as ModelImporter;
	//	modelImporter.importMaterials = false;
 //       modelImporter.importTangents = ModelImporterTangents.None;
 //       //modelImporter.isReadable = false;
 //       modelImporter.importBlendShapes = false;
	//}
}


//void OnPostprocessModel(GameObject model){
//        if (!assetPath.Contains ("@")) {
//            Renderer [] renderers = model.transform.GetComponentsInChildren<Renderer> ();
//            for (int i =0; i< renderers.Length; i++){
//                if(renderers[i].sharedMaterial.name!= model.name){
//                    Debug.LogError("材质名和模型名不匹配！");
//                    //FileUtil.DeleteFileOrDirectory(Application.dataPath+assetPath.Replace("Assets",""));
//                    AssetDatabase.Refresh();
//                    break;
//                }
//            }
//        }
 
//    }