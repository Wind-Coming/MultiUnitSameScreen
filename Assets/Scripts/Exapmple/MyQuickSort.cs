using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyQuickSort : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        int[] array = new int[] { 6, 1, 9, 20, 7, 3, 8, 1, -6, 99, 5 };
        QuickSort(array, 0, array.Length-1);

        foreach(var a in array) {
            Debug.Log(a);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void QuickSort(int[] array, int start, int end)
    {
        if(start >= end) {
            return;
        }

        int index = GetIndex(array, start, end);
        QuickSort(array, 0, index - 1);
        QuickSort(array, index + 1, end);
    }

    public int GetIndex(int[] array, int start, int end)
    {
        int low = start;
        int high = end;
        int tmp = array[low];

        while(low < high) {
            while(low < high && tmp <= array[high]) {
                high--;
            }

            array[low] = array[high];

            while(low < high && tmp >= array[low]) {
                low++;
            }

            array[high] = array[low];
        }

        array[high] = tmp;

        return low;
    }
}
