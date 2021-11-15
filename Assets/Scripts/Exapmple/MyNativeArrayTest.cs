using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public class MyNativeArrayTest : MonoBehaviour
{
    NativeArray<int> array;
    NativeArray<int> array2;
    int[] toarray;
    // Start is called before the first frame update
    void Start()
    {
        array = new NativeArray<int>(1024, Allocator.Persistent);
        array2 = new NativeArray<int>(1024, Allocator.Persistent);
        for(int i = 0; i < 1024; i++)
        {
            array[i] = i;
        }

        toarray = new int[1024];
    }

    // Update is called once per frame
    void Update()
    {
        // NativeArray<int> a = new NativeArray<int>(1024, Allocator.TempJob);
        // a.Dispose();
        GlobalFunc.BeginSample();
        array.CopyTo(toarray);
        GlobalFunc.EndSample();

        Test();
    }

    private unsafe void Test()
    {
        GlobalFunc.BeginSample();
        Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemCpy(array2.GetUnsafePtr(), array.GetUnsafePtr(), 1024 * sizeof(int));
        GlobalFunc.EndSample();
    }
}
