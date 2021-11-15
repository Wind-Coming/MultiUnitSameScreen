using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class DragSceneObj : MonoBehaviour
{
    Camera mainCamera;
    public LineRenderer line;
    private bool m_bDragging = false;
    private float m_fRadius = 0;
    // Start is called before the first frame update

    public Transform m_tEmpty;

    public float m_fSphereDuring = 1;

    public float high = 2;
    public float m_fLineDuring = 1;

    void Start()
    {
        mainCamera = Camera.main;
        m_fRadius = transform.localScale.x * 0.5f;
        m_tEmpty = new GameObject("TestNode").transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(1))
        {
            transform.position = Vector3.forward * 0.5f;
            transform.DOKill();
            transform.rotation = Quaternion.identity;
            m_tEmpty.DOKill();
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (!EventSystem.current.IsPointerOverGameObject())//点击到UI上
            {
                //屏蔽UI 渗透场景下层
                //TODO:Doing...
                RaycastHit hit;
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit) && hit.transform == transform)
                {
                    Debug.Log("begin Drag!");
                    m_bDragging = true;
                    transform.DOKill();
                    m_tEmpty.DOKill();
                }

            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            m_bDragging = false;
            m_tEmpty.position = transform.position;
            Vector3 center = (line.GetPosition(0) + line.GetPosition(line.positionCount - 1)) * 0.5f;
            Vector3 target = (center - transform.position).normalized * Vector3.Distance(center, transform.position) * 6;

            transform.DOJump(target, high, 0, m_fSphereDuring).OnUpdate(
                ()=>
                {
                    transform.Rotate(5, 0, 0);
                }).SetEase(Ease.OutQuad);
            //transform.DOMove(target, 0.3f).SetEase(Ease.Linear);

            m_tEmpty.DOMove(Vector3.zero, m_fLineDuring).SetEase(Ease.OutElastic).OnUpdate(
                () =>
                {
                    //transform.position = m_tEmpty.position;
                    SetLine3(m_tEmpty.position);
                });
        }

        if (m_bDragging)
        {
            RaycastHit hit;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 500, 1 << 2))
            {
                transform.position = hit.point;
                //line.SetPosition(1, hit.point);
                SetLine(hit.point);
            }
        }
    }

    public void SetLine2(Vector3 pos)
    {
        for (int i = 1; i < line.positionCount - 1; i++)
        {
            line.SetPosition(i, pos);
        }
    }


    public void SetLine3(Vector3 pos)
    {
        Vector3 v1 = pos + (line.GetPosition(0) - pos).normalized * 2;
        Vector3 v2 = line.GetPosition(0);
        Vector3 v3 = pos;
        Vector3 v4 = line.GetPosition(line.positionCount - 1);
        Vector3 v5 = pos + (line.GetPosition(line.positionCount - 1) - pos).normalized * 2;

        int halfLinePoints = line.positionCount / 2;
        int halfLinePointsWithoutSE = (line.positionCount - 2) / 2;

        for(int i = 1; i < halfLinePoints; i++) {
            float lerp = i * 1.0f / halfLinePointsWithoutSE;
            line.SetPosition(i, CatmullRom.CatmullRomPoint(v1, v2, v3, v4, lerp));
        }

        for(int i = halfLinePoints; i < line.positionCount - 1; i++) {
            float lerp = (i - halfLinePoints) * 1.0f / halfLinePointsWithoutSE;
            line.SetPosition(i, CatmullRom.CatmullRomPoint(v2, v3, v4, v5, lerp));
        }
    }

    public void SetLine(Vector3 pos)
    {
        Vector3 tanPos = GetTanPosition(line.GetPosition(0), pos);
        Vector3 tanPos2 = GetTanPosition(line.GetPosition(line.positionCount - 1), pos, false);

        List<Vector3> vlist = LerpCirclePosition(pos, tanPos, tanPos2, line.positionCount - 2);

        for (int i = 0; i < vlist.Count; i++)
        {
            line.SetPosition(i + 1, vlist[i]);
        }
    }

    //获取点到圆的切点
    public Vector3 GetTanPosition(Vector3 start, Vector3 centerPosition, bool left = true)
    {
        Vector3 standard = left ? Vector3.right : Vector3.left;
        int xxx = left ? 1 : -1;

        float angle1 = Vector3.Angle(standard, (centerPosition - start).normalized);

        float centerToLineStart = Vector3.Distance(centerPosition, start);
        float radius = m_fRadius;

        float angle2 = Mathf.Asin(radius / centerToLineStart) * Mathf.Rad2Deg;
        float angleFinal = angle2 + angle1;

        float dis = Mathf.Cos(angle2 * Mathf.Deg2Rad) * centerToLineStart;

        Vector3 dir = Quaternion.Euler(0, xxx * angleFinal, 0) * standard;

        return start + dir.normalized * dis;
    }


    private List<Vector3> listv3 = new List<Vector3>();
    public List<Vector3> LerpCirclePosition(Vector3 center, Vector3 left, Vector3 right, int num)
    {
        listv3.Clear();
        Vector2 startDir = new Vector2((left - center).x, (left - center).z);
        Vector2 endDir = new Vector2((right - center).x, (right - center).z);
        for (float i = 0; i < num; i++)
        {
            Vector2 v2 = Vector2.Lerp(startDir, endDir, i / (num - 1)).normalized * (m_fRadius + 0.1f);
            Vector3 p = center + new Vector3(v2.x, 0, v2.y);
            listv3.Add(p);
        }
        return listv3;
    }
}
