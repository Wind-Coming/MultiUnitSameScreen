using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBrowser
{
    public class EndBuild : BasicPipeline
    {
        public EndBuild()
        {
            name = "preBuild";
            tip = @"后处理";

            canDisable = false;
            configable = true;
            showConfig = true;

        }

        public override void Refresh()
        {
            
        }


        public override int Process(Dictionary<string, object> objectInPipeline)
        {

            return 0;
        }

        

    }
}
