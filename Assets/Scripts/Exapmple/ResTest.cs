using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResTest : MonoBehaviour
{
    GameObject go;
    Texture tex;
    Material mat;

    ResLoader m_kLoader = ClassPool<ResLoader>.Get();
    // Start is called before the first frame update
    void Start()
    {
        go = m_kLoader.LoadAsset<UnityEngine.GameObject>("HR_Caesar");
        go.transform.rotation = Quaternion.Euler(0, 180, 0);
        Debug.Log(go.name);

        tex = m_kLoader.LoadAsset<Texture>("pic");
        Debug.Log(tex);

        mat = m_kLoader.LoadAsset<Material>("test");
        Debug.Log(mat);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space)) {
            ClassPool<ResLoader>.Release(m_kLoader);
        }

        if(Input.GetKeyDown(KeyCode.S)) {
            m_kLoader.LoadSceneAsync("test2");
        }

        if (Input.GetKeyDown(KeyCode.U)) {
            //m_kLoader.LoadScene("test2");
        }
    }

    private void OnDestroy()
    {
        ClassPool<ResLoader>.Release(m_kLoader);
    }
}
