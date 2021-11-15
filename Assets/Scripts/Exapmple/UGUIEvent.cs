using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UGUIEvent : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    private RectTransform m_RT;

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("-------");
    }

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {
        print("IBeginDragHandler.OnBeginDrag");
        gameObject.GetComponent<Transform>().position = Input.mousePosition;
        print("这是实现的拖拽开始接口");
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        print("IDragHandler.OnDrag");
        //虽然用Input.mousePosition可以得到一个2D坐标，不过我们现在需要的是3D坐标，看下面
        //gameObject.GetComponent<Transform>().position = Input.mousePosition;

        //3D坐标获取方法
        Vector3 pos;
        m_RT = gameObject.GetComponent<RectTransform>();
        //屏幕坐标到世界坐标
        RectTransformUtility.ScreenPointToWorldPointInRectangle(m_RT, eventData.position, eventData.enterEventCamera, out pos);
        m_RT.position = pos;
        print("拖拽中……");
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        print("IEndDragHandler.OnEndDrag");
        gameObject.GetComponent<Transform>().position = Input.mousePosition;
        print("实现的拖拽结束接口");
    }
}
