using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

using UnityEditor;


namespace AssetBundleBrowser
{
    public class EncryptResources : BasicPipeline
    {
        public EncryptResources()
        {
            name = "Encrypt";
            tip = @"加密资源";
        }

        private string key = "Goodgoodstudy,daydayup!";

        public override int Process(Dictionary<string, object> objectInPipeline)
        {


          AssetBundleManifest mainfest = objectInPipeline["manifest"] as AssetBundleManifest;

          string[] allBundle = mainfest.GetAllAssetBundles();

          for (int a = 0; a < allBundle.Length; a++)
          {
              if (!string.IsNullOrEmpty(allBundle[a])){  // "file://" +
                  string path = System.Environment.CurrentDirectory.Replace("\\", "/") + "/" + Utils.OutsideAbFolder + "/" + Utils.GetPlatformFolder() + "/" + allBundle[a];
                if(!File.Exists(path)){
                    continue;
                }

                 FileStream fs = new FileStream(path , FileMode.Open, FileAccess.Read);

                 byte[] bytes =  new byte[fs.Length];
                 fs.Read(bytes, 0 , (int)fs.Length);
                 Encrypt(bytes, bytes.Length , key);

                 fs.Flush();
                 fs.Close();
                 File.Delete(path);

                FileStream ws = new FileStream(path,FileMode.Create , FileAccess.Write);
                ws.Write(bytes ,0 , bytes.Length);
                bytes = null;
                ws.Flush();
                ws.Close();
            }
          }

          return 0;
        }


        //private void Encrypt(byte[] buffer , int len, byte[] key )
        //{
        //    int l = len;
        //    int a = 0;

        //    for (int i = 0; i < l; i++)
        //    {
        //        if( a  >= key.Length )
        //            a = 0;

        //        buffer[i] ^= key[a];
        //        a++;
        //    }
        //}

        private void Encrypt(byte[] buffer , int len  , string key )
        {

            int l = len;
            int a = 0;

            for (int i = 0; i < l; i++)
            {
                if (a >= key.Length)
                    a = 0;

                buffer[i] ^= (byte)key[a];
                a++;
            }
        }
    }
}
