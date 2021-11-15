using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

public static class CommonUtil
{
    public static bool Overlap(this RectInt rc1, RectInt rc2)
    {
        if (rc1.x + rc1.width > rc2.x &&
           rc2.x + rc2.width > rc1.x &&
           rc1.y + rc1.height > rc2.y &&
           rc2.y + rc2.height > rc1.y
          )
            return true;
        else
            return false;
    }

    public static RectInt ToRectInt(this Rect tar)
    {
        return new RectInt((int)tar.x, (int)tar.y, (int)tar.width, (int)tar.height);
    }

    public static List<T> GetIntersectionSet<T>(List<T> la, List<T> lb)
    {
        List<T> newList = new List<T>();
        for (int i_obj = 0, n_obj = la.Count; i_obj < n_obj; i_obj++)
        {
            var obj = la[i_obj];
            if (lb.Contains(obj))
                newList.Add(obj);
        }
        return newList;
    }

    public static List<T> GetDifferenceSet<T>(List<T> la, List<T> lb)
    {
        List<T> newList = new List<T>();
        for (int i_obj = 0, n_obj = la.Count; i_obj < n_obj; i_obj++)
        {
            var obj = la[i_obj];
            if (!lb.Contains(obj))
                newList.Add(obj);
        }
        return newList;
    }

    public static List<T> Clone<T>(this List<T> list)
    {
        List<T> newList = new List<T>();
        for (int iList = 0, nList = list.Count; iList < nList; iList++)
        {
            newList.Add(list[iList]);
        }
        return newList;
    }

    public static void SaveTemp(string fileNamePrefix, string text)
    {
        if (!Directory.Exists(Application.streamingAssetsPath + "/SaveData/"))
            Directory.CreateDirectory(Application.streamingAssetsPath + "/SaveData/");
        File.WriteAllText(Application.streamingAssetsPath + "/SaveData/" + fileNamePrefix + "_" + DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss") + ".txt", text);
    }

    /// <summary>去除Unity实例化物体后添加的" (Instance)"字段</summary>
    public static string RemovePostfix_Instance(string str)
    {
        string backstr = " (Instance)";
        while (str.EndsWith(backstr))
            str = str.Substring(0, str.Length - backstr.Length);
        return str;
    }

    /// <summary>
    /// 靠近目标点
    /// </summary>
    /// <param name="curPos">当前位置</param>
    /// <param name="tarPos">目标点位置</param>
    /// <param name="moveDis">最大移动距离</param>
    /// <returns>剩余的移动能力, 若到达, 则大于等于0</returns>
    public static float MoveToPoint(Vector3 curPos, Vector3 tarPos, float moveDis, ref Vector3 nextPos)
    {
        Vector3 dir = tarPos - curPos;
        dir = dir.normalized;
        float distance = Vector3.Distance(tarPos, curPos);
        if (distance > moveDis)
        {
            nextPos = curPos + dir.GetScaledVector(moveDis);
            return 0;
        }
        else
        {
            nextPos = tarPos;
            return moveDis - distance;
        }
    }
    /// <summary>
    /// 靠近目标点
    /// </summary>
    /// <param name="curPos">当前位置</param>
    /// <param name="tarPos">目标点位置</param>
    /// <param name="moveDis">最大移动距离</param>
    /// <returns>剩余的移动能力, 若到达, 则大于等于0</returns>
    public static float MoveToPoint(Vector2 curPos, Vector2 tarPos, float moveDis, ref Vector2 nextPos)
    {
        Vector2 dir = tarPos - curPos;
        dir = dir.normalized;
        float distance = Vector2.Distance(tarPos, curPos);
        if (distance > moveDis)
        {
            nextPos = curPos + new Vector2(dir.x * moveDis, dir.y * moveDis);
            return 0;
        }
        else
        {
            nextPos = tarPos;
            return moveDis - distance;
        }
    }

    #region 本地图片加载 与Texture2D的创建与缩放
    public static Texture2D ScaleTextureBilinear(Texture2D originalTexture, float scaleFactor)
    {
        Texture2D newTexture = new Texture2D(Mathf.CeilToInt(originalTexture.width * scaleFactor), Mathf.CeilToInt(originalTexture.height * scaleFactor));
        float scale = 1.0f / scaleFactor;
        int maxX = originalTexture.width - 1;
        int maxY = originalTexture.height - 1;
        for (int y = 0; y < newTexture.height; y++)
        {
            for (int x = 0; x < newTexture.width; x++)
            {
                // Bilinear Interpolation
                float targetX = x * scale;
                float targetY = y * scale;
                int x1 = Mathf.Min(maxX, Mathf.FloorToInt(targetX));
                int y1 = Mathf.Min(maxY, Mathf.FloorToInt(targetY));
                int x2 = Mathf.Min(maxX, x1 + 1);
                int y2 = Mathf.Min(maxY, y1 + 1);

                float u = targetX - x1;
                float v = targetY - y1;
                float w1 = (1 - u) * (1 - v);
                float w2 = u * (1 - v);
                float w3 = (1 - u) * v;
                float w4 = u * v;
                UnityEngine.Color color1 = originalTexture.GetPixel(x1, y1);
                UnityEngine.Color color2 = originalTexture.GetPixel(x2, y1);
                UnityEngine.Color color3 = originalTexture.GetPixel(x1, y2);
                UnityEngine.Color color4 = originalTexture.GetPixel(x2, y2);
                UnityEngine.Color color = new UnityEngine.Color(Mathf.Clamp01(color1.r * w1 + color2.r * w2 + color3.r * w3 + color4.r * w4),
                    Mathf.Clamp01(color1.g * w1 + color2.g * w2 + color3.g * w3 + color4.g * w4),
                    Mathf.Clamp01(color1.b * w1 + color2.b * w2 + color3.b * w3 + color4.b * w4),
                    Mathf.Clamp01(color1.a * w1 + color2.a * w2 + color3.a * w3 + color4.a * w4)
                    );
                newTexture.SetPixel(x, y, color);
            }
        }
        return newTexture;
    }

    /// <summary>
    ///Exp: Texture2D bigtexture = new Texture2D(100, 100);
    ///bigtexture.LoadImage(bytes);
    /// </summary>
    public static byte[] LoadImage(string path)
    {
        FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
        fileStream.Seek(0, SeekOrigin.Begin);
        //创建文件长度缓冲区
        byte[] bytes = new byte[fileStream.Length];
        //读取文件
        fileStream.Read(bytes, 0, (int)fileStream.Length);
        //释放文件读取流
        fileStream.Close();
        fileStream.Dispose();
        fileStream = null;
        return bytes;
    }

    #endregion


    //public static T GetMonoByMouse<T>() where T : MonoBehaviour
    //{
    //    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //    RaycastHit rh;
    //    if (Physics.Raycast(ray, out rh, 99999f))
    //    {
    //        return rh.collider.GetComponentInParent<T>();
    //    }
    //    return null;
    //}
    //public static T GetMonoByMouse<T>(out RaycastHit rh) where T : MonoBehaviour
    //{
    //    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

    //    if (Physics.Raycast(ray, out rh, 99999f))
    //    {
    //        return rh.collider.GetComponentInParent<T>();
    //    }
    //    return null;
    //}

    public static bool IsPic(string fileName)
    {
        string postFix = CommonUtil.GetFilePostfix(fileName);
        return postFix == "png"
            || postFix == "PNG"
            || postFix == "jpg"
            || postFix == "JPG"
            || postFix == "jpeg"
            || postFix == "JPEG";
    }

    /// <summary>添加进key-value(list)型字典, 并确保列表非空与不重复添加</summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <param name="dic"></param>
    /// <param name="key"></param>
    /// <param name="tar"></param>
    /// <returns></returns>
    public static Dictionary<T1, List<T2>> AddToList<T1, T2>(this Dictionary<T1, List<T2>> dic, T1 key, T2 tar)
    {
        if (!dic.ContainsKey(key))
            dic.Add(key, new List<T2>());
        List<T2> list = dic[key];
        if (!list.Contains(tar))
            list.Add(tar);
        return dic;
    }

    /// <summary>按Key插入列表, doCheck是否查重</summary>
    public static Dictionary<T1, List<T2>> AddToList<T1, T2>(this Dictionary<T1, List<T2>> dic, T1 key, T2 tar, bool doCheck)
    {
        if (!dic.ContainsKey(key))
            dic.Add(key, new List<T2>());
        List<T2> list = dic[key];
        if(!doCheck || !list.Contains(tar))
            list.Add(tar);
        return dic;
    }

    public static List<T> RemoveFromList<T>(this List<T> list, Predicate<T> pred, Action<T> operation = null)
    {
        List<T> deleteList = new List<T>();
        for (int i = 0, length = list.Count; i < length; i++)
        {
            if (pred(list[i]))
                deleteList.Add(list[i]);
        }
        for (int i = 0, length = deleteList.Count; i < length; i++)
        {
            list.Remove(deleteList[i]);
            if (operation != null)
                operation(deleteList[i]);
        }
        return list;
    }

    public static Dictionary<T1, T2> RemoveFromDic<T1, T2>(this Dictionary<T1, T2> dic, Predicate<T2> pred, Action<T2> operation = null)
    {
        Dictionary<T1, T2> deleteDic = new Dictionary<T1, T2>();
        foreach (var key in dic.Keys)
        {
            deleteDic.AddRep(key, dic[key]);
        }
        foreach (var key in deleteDic.Keys)
        {
            dic.Remove(key);
            if(operation != null)
                operation(deleteDic[key]);
        }

        return dic;
    }

    /// <summary>替换/添加, 如果字典中已有则替换值</summary>
    public static Dictionary<T1, T2> AddRep<T1, T2>(this Dictionary<T1, T2> dic, T1 key, T2 value)
    {
        if (dic.ContainsKey(key))
            dic[key] = value;
        else
            dic.Add(key, value);
        return dic;
    }

    /// <summary>获取法向量</summary>
    public static Vector3 GetNormalVector(Vector3 va, Vector3 vb, Vector3 vc)
    {
        //平面方程Ax+BY+CZ+d=0 行列式计算
        float A = va.y * vb.z + vb.y * vc.z + vc.y * va.z - va.y * vc.z - vb.y * va.z - vc.y * vb.z;
        float B = -(va.x * vb.z + vb.x * vc.z + vc.x * va.z - vc.x * vb.z - vb.x * va.z - va.x * vc.z);
        float C = va.x * vb.y + vb.x * vc.y + vc.x * va.y - va.x * vc.y - vb.x * va.y - vc.x * vb.y;
        //float D = -(va.x * vb.y * vc.z + vb.x * vc.y * va.z + vc.x * va.y * vb.z - va.x * vc.y * vb.z - vb.x * va.y * vc.z - vc.x * vb.y * va.z);
        float E = Mathf.Sqrt(A * A + B * B + C * C);
        Vector3 res = new Vector3(A / E, B / E, C / E);
        return (res);
    }

    public static T FindItem<T>(this IEnumerable<T> enu, Predicate<T> judgeFunc)
    {
        foreach (var item in enu)
        {
            if (judgeFunc(item))
                return item;
        }
        return default(T);
    }
    public static List<T> FindAllItem<T>(this IEnumerable<T> enu, Predicate<T> judgeFunc)
    {
        List<T> list = new List<T>();
        foreach (var item in enu)
        {
            if (judgeFunc(item))
                list.Add(item);
        }
        return list;
    }


    /// <summary>过滤并返回一个新数组</summary>
    public static T[] filter<T>(this T[] arr, Func<T, bool> filterFunc)
    {
        List<T> list = new List<T>();
        for (int i = 0, length = arr.Length; i < length; i++)
        {
            if (filterFunc(arr[i]))
                list.Add(arr[i]);
        }
        return list.ToArray();
    }
    /// <summary>过滤并返回一个新数组</summary>
    public static List<T> filter<T>(this List<T> arr, Func<T, bool> filterFunc)
    {
        List<T> list = new List<T>();
        for (int i = 0, length = arr.Count; i < length; i++)
        {
            if (filterFunc(arr[i]))
                list.Add(arr[i]);
        }
        return list;
    }

    //public static void openFileDialog(Action<string> onFileOpen)
    //{
    //    //Debug.Log("openDialog");
    //    OpenFileName ofn = new OpenFileName();

    //    ofn.structSize = System.Runtime.InteropServices.Marshal.SizeOf(ofn);

    //    //ofn.filter = "All Files\0*.*\0\0";
    //    //ofn.filter = "图片文件(*.jpg*.png)\0*.jpg;*.png";  
    //    ofn.filter = "地块文件(*.assetbundle*.txt)\0*.assetbundle;*.txt";  

    //    ofn.file = new string(new char[256]);

    //    ofn.maxFile = ofn.file.Length;

    //    ofn.fileTitle = new string(new char[64]);

    //    ofn.maxFileTitle = ofn.fileTitle.Length;

    //    ofn.initialDir = UnityEngine.Application.dataPath;//默认路径  

    //    ofn.title = "Open Project";

    //    ofn.defExt = "JPG";//显示文件的类型  
    //    //注意 一下项目不一定要全选 但是0x00000008项不要缺少  
    //    ofn.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;//OFN_EXPLORER|OFN_FILEMUSTEXIST|OFN_PATHMUSTEXIST| OFN_ALLOWMULTISELECT|OFN_NOCHANGEDIR  

    //    if (WindowDll.GetOpenFileName(ofn))
    //    {
    //        //Debug.Log("Selected file with full path: {0}" + ofn.file);

    //        //this.pathOver(ofn.file);
    //        if (onFileOpen != null)
    //            onFileOpen(ofn.file);
    //    }
    //}

    public static Vector3 GetScaledVector(this Vector3 tar, float multi)
    {
        return new Vector3(tar.x * multi, tar.y * multi, tar.z * multi);
    }
    public static string ForeachToString<T>(this ICollection<T> list, string sep = ", ", Func<T, string> toString = null)
    {
        string res = "";
        if (list is List<T>)
        {
            for (int i = 0, length = list.Count; i < length; i++)
            {
                List<T> _list = list as List<T>;

                if (toString == null)
                    res += _list[i] == null ? null : _list[i].ToString();
                else
                    res += _list[i] == null ? null : toString(_list[i]);
                if (i != length - 1)
                    res += sep;
            }
        }
        else
        {
            foreach (var item in list)
            {
                if (item != null)
                    res += toString == null ? item.ToString() : toString(item);
                else
                    res += null;
                res += sep;
            }
        }
        return res;
    }

    public static Bounds? GetBounds(GameObject go)
    {
        Bounds? bounds = null;
        //bounds.
        Bounds ab;

        CommonUtil.forAllChildren(go, tar =>
        {
            if (tar.GetComponent<Renderer>() != null)
            {
                Bounds b = tar.GetComponent<Renderer>().bounds;
                if (bounds == null)
                    bounds = b;
                else
                {
                    ab = bounds.Value;//对bounds.Value的改变不能作用到bounds上 需要中间变量
                    ab.Encapsulate(b);
                    bounds = ab;
                }
            }
        });
        return bounds;
    }

    //public static string GetUnderArea(Vector3 pos)
    //{
    //    string areaName = null;
    //    RaycastHit rh = new RaycastHit();
    //    bool isHit = SongeUtil.GetPointOnGround(pos, ref rh);
    //    if (isHit)
    //    {
    //        Transform tr = rh.transform;
    //        AreaScript areaSc = tr.GetComponentInParent<AreaScript>();
    //        if (areaSc != null)
    //            areaName = areaSc.AreaName;
    //    }
    //    return areaName;
    //}

    public static Vector3 GetMoveInput(bool allowArrow = true)
    {
        float kz = 0;
        if (allowArrow)
        {
            kz += Input.GetKey(KeyCode.UpArrow) ? 1f : 0f * 1f;
            kz += Input.GetKey(KeyCode.DownArrow) ? -1f : 0f * 1f;
        }
        kz += Input.GetKey(KeyCode.W) ? 1f : 0f * 1f;
        kz += Input.GetKey(KeyCode.S) ? -1f : 0f * 1f;
        float kx = 0;
        if (allowArrow)
        {
            kx -= Input.GetKey(KeyCode.LeftArrow) ? -1f : 0f * 1f;
            kx -= Input.GetKey(KeyCode.RightArrow) ? 1f : 0f * 1f;
        }
        kx -= Input.GetKey(KeyCode.A) ? -1f : 0f * 1f;
        kx -= Input.GetKey(KeyCode.D) ? 1f : 0f * 1f;
        return new Vector3(kx, 0, kz);
    }

    /// <summary>获取朝目标方向前进后退左右平移后, 坐标的改变</summary>
    /// <param name="rotH">水平面旋转方向</param>
    /// <param name="dirX">左右移动距离</param>
    public static Vector3 MoveTowards(float rotH, float dirX, float dirZ)
    {
        float dx = dirX * Mathf.Sin(rotH) + dirZ * Mathf.Cos(rotH);
        float dz = dirX * Mathf.Cos(rotH) - dirZ * Mathf.Sin(rotH);
        return new Vector3(dx, 0, dz);
    }
    public static Vector3 MoveTowards(float rotH, Vector3 dir)
    {
        float dx = dir.x * Mathf.Sin(rotH) + dir.z * Mathf.Cos(rotH);
        float dz = dir.x * Mathf.Cos(rotH) - dir.z * Mathf.Sin(rotH);
        return new Vector3(dx, 0, dz);
    }

    public static string GetStandardPath(string path)
    {
        int loopNum = 20;
        path = path.Replace(@"\", @"/");
        while (path.IndexOf(@"//") != -1)
        {
            path = path.Replace(@"//", @"/");
            loopNum--;
            if (loopNum < 0)
            {
                //Debug.Log("路径清理失败: " + path);
                return path;
            }
        }
        return path;
    }

    /// <summary>获取文件名后缀</summary>
    public static string GetFilePostfix(string fileName)
    {
        if (fileName == null)
            return null;
        string res;
        if (fileName.IndexOf(".") == -1)
            res = "";
        else
        {
            string[] ss = fileName.Split(new char[1] { '.' });
            res = ss[ss.Length - 1];
        }
        return res;
    }

    public static string GetFolderPath(string path, bool fullPath = true)
    {
        path = GetStandardPath(path);
        if (fullPath)//获取全路径
        {
            if (path.LastIndexOf(@"/") == path.Length - 1)
                return GetFolderPath(path.Substring(0, path.Length - 1));
            else
                return path.Substring(0, path.LastIndexOf(@"/") + 1);
        }
        else//获取父级文件夹名
        {
            string[] strArr = path.Split('/');

            if (path.LastIndexOf(@"/") == path.Length - 1)
                return strArr[strArr.Length - 2];
            else
                return strArr[strArr.Length - 1];
        }
    }

    public static string GetParentFolderPath(string path, bool fullPath = true)
    {
        path = GetStandardPath(path);
        if (fullPath)//获取全路径
        {
            if (path.LastIndexOf(@"/") == path.Length - 1)
                return GetFolderPath(path.Substring(0, path.Length - 1));
            else
                return path.Substring(0, path.LastIndexOf(@"/") + 1);
        }
        else//获取父级文件夹名
        {
            string[] strArr = path.Split('/');
            return strArr[strArr.Length - 2];
        }
    }

    public static string GetFileName(string path, bool needPostfix = false)
    {
        path = GetStandardPath(path);
        string fileFolderPath = path.Substring(0, path.LastIndexOf(@"/") + 1);

        string fileName = path.Substring(path.LastIndexOf("/") + 1, path.Length - fileFolderPath.Length);
        if (needPostfix)
            return fileName;
        else
            return fileName.Substring(0, fileName.LastIndexOf("."));
    }

    public static void ChangeShader(GameObject tar)
    {
        forAllChildren(tar, go =>
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
            {
                for (int i = 0, len = r.sharedMaterials.Length; i < len; i++)
                {
                    Material mat = r.sharedMaterials[i];
                    if (mat != null && mat.shader != null)
                    {
                        if (mat.shader.name != "Nature/SpeedTree" && mat.shader.name != "Nature/SpeedTree Billboard")
                        {
                            if (mat.shader.name.IndexOf("Transparent") != -1)
                                mat.shader = Shader.Find("MyShader/Transparent/DoubleSided_SelfIllum");
                            else
                                mat.shader = Shader.Find("MyShader/Opaque/SelfIllum");
                        }
                    }
                }
            }
        }, false);
    }

    /// <summary>返回目标向量在X-Z平面旋转(增加)angle(弧度)后的单位向量</summary>
    public static Vector3 RotateVecotrAtXZ(Vector3 tar, float angle)
    {
        float ang = Mathf.Atan2(tar.z, tar.x);
        ang = ang + angle;
        float newX = 1f * Mathf.Cos(ang);
        float newZ = 1f * Mathf.Sin(ang);
        return new Vector3(newX, tar.y, newZ); ;
    }

    #region 扩展方法
    public enum EnumVector { x, y, z }
    public static void SetLocalPos(this GameObject tar, float value, EnumVector dir)
    {
        SetLocalPos(tar.transform, value, dir);
    }
    public static void SetLocalPos(this Transform tar, float value, EnumVector dir)
    {
        float newValueX = dir == EnumVector.x ? value : tar.localPosition.x;
        float newValueY = dir == EnumVector.y ? value : tar.localPosition.y;
        float newValueZ = dir == EnumVector.z ? value : tar.localPosition.z;
        tar.localPosition = new Vector3(newValueX, newValueY, newValueZ);
    }

    public static void SetVec(ref Vector3 tar, float value, EnumVector dir)
    {
        float newValueX = dir == EnumVector.x ? value : tar.x;
        float newValueY = dir == EnumVector.y ? value : tar.y;
        float newValueZ = dir == EnumVector.z ? value : tar.z;
        tar.Set(newValueX, newValueY, newValueZ);
    }

    #endregion

    public static Vector3 Approach(Vector3 fromPos, Vector3 toPos, float delta, out bool isArrive)
    {
        Vector3 dir = toPos - fromPos;
        isArrive = dir.magnitude <= delta;
        Vector3 deltaPos = Vector3.ClampMagnitude(dir, delta);
        return isArrive ? toPos : fromPos + deltaPos;
    }

    //public static void addClickEventListener(MonoBehaviour listener, string methodName, GameObject tarObj)
    //{
    //    if (tarObj.GetComponent<UIButton>() == null)
    //        return;
    //    EventDelegate ev = new EventDelegate(listener, methodName);
    //    ev.parameters[0] = new EventDelegate.Parameter();
    //    ev.parameters[0].obj = tarObj;
    //    EventDelegate.Add(tarObj.GetComponent<UIButton>().onClick, ev);
    //}

    ///// <summary>
    ///// 侦听子集的UIButton.onClick事件
    ///// </summary>
    ///// <param name="listener">事件接受者</param>
    ///// <param name="target">事件发送者</param>
    ///// <param name="callback">回调函数</param>
    //public static void addChildClickEventListener(MonoBehaviour listener, Transform target, string callback)
    //{
    //    foreach (Transform childTran in target)
    //    {
    //        addClickEventListener(listener, callback, childTran.gameObject);

    //        addChildClickEventListener(listener, childTran, callback);
    //    }
    //}

    //示例 eularAngle = new Vector3( -GetRotatV, GetRotateY, 0)
    /// <summary>返回Vector3(0, 0, 1)指向目标向量, Y轴需要旋转角度(360)</summary>
    public static float GetRotateY(Vector3 dir)
    {
        return Mathf.Atan2(dir.x, dir.z) / Mathf.PI * 180;
    }
    //示例 eularAngle = new Vector3( -GetRotatV, GetRotateY, 0)
    /// <summary>获取仰角</summary>
    public static float GetRotatV(Vector3 dir)
    {
        float len2 = dir.x * dir.x + dir.z * dir.z;
        float len = Mathf.Sqrt(len2);
        float atan = Mathf.Atan2(dir.y, len);
        return atan / Mathf.PI * 180;
    }

    /// <summary>获取鼠标在水平面的投影点</summary>
    public static Vector3 GetMousePointOnHorizontal(Camera tarCamera = null)
    {
        if (tarCamera == null)
            tarCamera = Camera.main;
        Ray ray = tarCamera.ScreenPointToRay(Input.mousePosition);
        float ratio = -ray.origin.y / ray.direction.y;
        var ret = ray.origin + ray.direction * ratio;
        return ret;
    }

    /// <summary>
    /// 遍历Transform子集并执行operate
    /// 用例 1: SongeUtil.forAllChildren(gameObject,tar => {tar.transform.position = Vector3.zero;});
    /// </summary>
    public static void forAllChildren(GameObject target, Action<GameObject> operate, bool includeTarget = true)
    {
        //用例 2
        //System.Action<GameObject> setShow = null;
        //setShow += (GameObject tar) =>{};
        //SongeUtil.forAllChildren(wall, setShow);
        if (target == null)
            return;

        if (includeTarget)
            operate(target);
        for (int i = 0, length = target.transform.childCount; i < length; i++)
        {
            Transform childTran = target.transform.GetChild(i);
            operate(childTran.gameObject);
            forAllChildren(childTran.gameObject, operate, false);
        }
    }

    public static void followMouse(Transform targetTrans, Camera uiCamera = null)
    {
        Vector3 pos = Input.mousePosition;
        //if (uiCamera == null)
        //    uiCamera = UICamera.list[0].gameObject.GetComponent<Camera>();

        if (uiCamera != null)
        {
            pos.x = Mathf.Clamp01(pos.x / Screen.width);
            pos.y = Mathf.Clamp01(pos.y / Screen.height);
            targetTrans.position = uiCamera.ViewportToWorldPoint(pos);

            // For pixel-perfect results
#if UNITY_4_3 || UNITY_4_5 || UNITY_4_6
            if (uiCamera.isOrthoGraphic)
#else
            if (uiCamera.orthographic)
#endif
            {
                Vector3 lp = targetTrans.localPosition;
                lp.x = Mathf.Round(lp.x);
                lp.y = Mathf.Round(lp.y);
                targetTrans.localPosition = lp;
            }
        }
        else
        {
            // Simple calculation that assumes that the camera is of fixed size
            pos.x -= Screen.width * 0.5f;
            pos.y -= Screen.height * 0.5f;
            pos.x = Mathf.Round(pos.x);
            pos.y = Mathf.Round(pos.y);
            targetTrans.localPosition = pos;
        }
    }

    /// <summary>仅当该键按下时, 返回true</summary>
    public static bool IsMouseButtonOnly(int button)
    {
        Dictionary<int, bool> dic = new Dictionary<int, bool>();
        dic.Add(0, Input.GetMouseButton(0));
        dic.Add(1, Input.GetMouseButton(1));
        dic.Add(2, Input.GetMouseButton(2));
        return (dic[0] == (button == 0)) && (dic[1] == (button == 1)) && (dic[2] == (button == 2));
    }

}
namespace songeP
{
    public class FileUtil
    {
        /// <summary>打开文件选取对话框</summary>
        //public static void OpenFileDialog(Action<string> onFileOpen)
        //{
        //    //Debug.Log("openDialog");
        //    OpenFileName ofn = new OpenFileName();

        //    ofn.structSize = System.Runtime.InteropServices.Marshal.SizeOf(ofn);

        //    ofn.filter = "All Files\0*.*\0\0";

        //    ofn.file = new string(new char[256]);

        //    ofn.maxFile = ofn.file.Length;

        //    ofn.fileTitle = new string(new char[64]);

        //    ofn.maxFileTitle = ofn.fileTitle.Length;

        //    ofn.initialDir = UnityEngine.Application.dataPath;//默认路径  

        //    ofn.title = "Open Project";

        //    ofn.defExt = "JPG";//显示文件的类型  
        //    //注意 一下项目不一定要全选 但是0x00000008项不要缺少  
        //    ofn.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;//OFN_EXPLORER|OFN_FILEMUSTEXIST|OFN_PATHMUSTEXIST| OFN_ALLOWMULTISELECT|OFN_NOCHANGEDIR  

        //    if (WindowDll.GetOpenFileName(ofn))
        //    {
        //        //Debug.Log("Selected file with full path: {0}" + ofn.file);

        //        //this.pathOver(ofn.file);
        //        if (onFileOpen != null)
        //            onFileOpen(ofn.file);
        //    }
        //}

        /// <summary>获取文件名后缀</summary>
        public static string GetFilePostfix(string fileName)
        {
            string res;
            if (fileName.IndexOf(".") == -1)
                res = "";
            else
            {
                string[] ss = fileName.Split(new char[1] { '.' });
                res = ss[ss.Length - 1];
            }
            return res;
        }

        public static string GetFolderPath(string path)
        {
            path = path.Replace(@"\", @"/");
            if (path.LastIndexOf(@"/") == path.Length - 1)
                return GetFolderPath(path.Substring(0, path.Length - 1));
            else
                return path.Substring(0, path.LastIndexOf(@"/") + 1);
        }

        public static string GetFileName(string path, bool needPostfix = false)
        {
            path = path.Replace(@"\", @"/");
            string fileFolderPath = path.Substring(0, path.LastIndexOf(@"/") + 1);

            string fileName = path.Substring(path.LastIndexOf("/") + 1, path.Length - fileFolderPath.Length);
            if (needPostfix)
                return fileName;
            else
                return fileName.Substring(0, fileName.LastIndexOf("."));
        }
    }
}

#region PerspectiveTransform 变换矩阵
public class PerspectiveTransform
{
    float a11;
    float a12;
    float a13;
    float a21;
    float a22;
    float a23;
    float a31;
    float a32;
    float a33;
    public PerspectiveTransform(float inA11, float inA21,
                                       float inA31, float inA12,
                                       float inA22, float inA32,
                                       float inA13, float inA23,
                                       float inA33)
    {
        a11 = inA11;
        a12 = inA12;
        a13 = inA13;

        a21 = inA21;
        a22 = inA22;
        a23 = inA23;

        a31 = inA31;
        a32 = inA32;
        a33 = inA33;
    }

    public static PerspectiveTransform quadrilateralToQuadrilateral(float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3, float x0p, float y0p, float x1p, float y1p, float x2p, float y2p, float x3p, float y3p)
    {
        PerspectiveTransform qToS = quadrilateralToSquare(x0, y0, x1, y1, x2, y2, x3, y3);
        PerspectiveTransform sToQ = squareToQuadrilateral(x0p, y0p, x1p, y1p, x2p, y2p, x3p, y3p);
        return sToQ.times(qToS);
    }

    static PerspectiveTransform squareToQuadrilateral(float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3)
    {
        float dx3 = x0 - x1 + x2 - x3;
        float dy3 = y0 - y1 + y2 - y3;
        if (dx3 == 0.0f && dy3 == 0.0f)
        {
            return new PerspectiveTransform(x1 - x0, x2 - x1, x0, y1 - y0, y2 - y1, y0, 0.0f, 0.0f, 1.0f);
        }
        else
        {
            float dx1 = x1 - x2;
            float dx2 = x3 - x2;
            float dy1 = y1 - y2;
            float dy2 = y3 - y2;
            float denominator = dx1 * dy2 - dx2 * dy1;
            float a13 = (dx3 * dy2 - dx2 * dy3) / denominator;
            float a23 = (dx1 * dy3 - dx3 * dy1) / denominator;
            return new PerspectiveTransform(x1 - x0 + a13 * x1, x3 - x0 + a23 * x3, x0, y1 - y0 + a13 * y1, y3 - y0 + a23 * y3, y0, a13, a23, 1.0f);
        }
    }

    static PerspectiveTransform quadrilateralToSquare(float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3)
    {
        return squareToQuadrilateral(x0, y0, x1, y1, x2, y2, x3, y3).buildAdjoint();
    }

    PerspectiveTransform buildAdjoint()
    {
        return new PerspectiveTransform(a22 * a33 - a23 * a32, a23 * a31 - a21 * a33, a21 * a32
                               - a22 * a31, a13 * a32 - a12 * a33, a11 * a33 - a13 * a31, a12 * a31 - a11 * a32, a12 * a23 - a13 * a22,
                               a13 * a21 - a11 * a23, a11 * a22 - a12 * a21);
    }

    PerspectiveTransform times(PerspectiveTransform other)
    {
        return new PerspectiveTransform(a11 * other.a11 + a21 * other.a12 + a31 * other.a13,
                               a11 * other.a21 + a21 * other.a22 + a31 * other.a23, a11 * other.a31 + a21 * other.a32 + a31
                               * other.a33, a12 * other.a11 + a22 * other.a12 + a32 * other.a13, a12 * other.a21 + a22
                               * other.a22 + a32 * other.a23, a12 * other.a31 + a22 * other.a32 + a32 * other.a33, a13
                               * other.a11 + a23 * other.a12 + a33 * other.a13, a13 * other.a21 + a23 * other.a22 + a33
                               * other.a23, a13 * other.a31 + a23 * other.a32 + a33 * other.a33);
    }

    public void transformPoints(List<float> points)
    {
        int max = points.Count;
        for (int i = 0; i < max; i += 2)
        {
            float x = points[i];
            float y = points[i + 1];
            float denominator = a13 * x + a23 * y + a33;
            points[i] = (a11 * x + a21 * y + a31) / denominator;
            points[i + 1] = (a12 * x + a22 * y + a32) / denominator;
        }
    }


}

#endregion