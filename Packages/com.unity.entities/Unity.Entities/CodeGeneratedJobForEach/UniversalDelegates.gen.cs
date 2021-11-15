using Unity.Entities.UniversalDelegates;
using Unity.Entities.CodeGeneratedJobForEach;
using Unity.Entities;

namespace Unity.Entities.UniversalDelegates
{
    [EntitiesForEachCompatible]
    public delegate void R<T0>(ref T0 t0);
    [EntitiesForEachCompatible]
    public delegate void I<T0>(in T0 t0);
    [EntitiesForEachCompatible]
    public delegate void V<T0>(T0 t0);
    [EntitiesForEachCompatible]
    public delegate void RI<T0, T1>(ref T0 t0, in T1 t1);
    [EntitiesForEachCompatible]
    public delegate void RR<T0, T1>(ref T0 t0, ref T1 t1);
    [EntitiesForEachCompatible]
    public delegate void II<T0, T1>(in T0 t0, in T1 t1);
    [EntitiesForEachCompatible]
    public delegate void VI<T0, T1>(T0 t0, in T1 t1);
    [EntitiesForEachCompatible]
    public delegate void VR<T0, T1>(T0 t0, ref T1 t1);
    [EntitiesForEachCompatible]
    public delegate void VV<T0, T1>(T0 t0, T1 t1);
    [EntitiesForEachCompatible]
    public delegate void RII<T0, T1, T2>(ref T0 t0, in T1 t1, in T2 t2);
    [EntitiesForEachCompatible]
    public delegate void RRI<T0, T1, T2>(ref T0 t0, ref T1 t1, in T2 t2);
    [EntitiesForEachCompatible]
    public delegate void RRR<T0, T1, T2>(ref T0 t0, ref T1 t1, ref T2 t2);
    [EntitiesForEachCompatible]
    public delegate void III<T0, T1, T2>(in T0 t0, in T1 t1, in T2 t2);
    [EntitiesForEachCompatible]
    public delegate void VII<T0, T1, T2>(T0 t0, in T1 t1, in T2 t2);
    [EntitiesForEachCompatible]
    public delegate void VRI<T0, T1, T2>(T0 t0, ref T1 t1, in T2 t2);
    [EntitiesForEachCompatible]
    public delegate void VRR<T0, T1, T2>(T0 t0, ref T1 t1, ref T2 t2);
    [EntitiesForEachCompatible]
    public delegate void VVI<T0, T1, T2>(T0 t0, T1 t1, in T2 t2);
    [EntitiesForEachCompatible]
    public delegate void VVR<T0, T1, T2>(T0 t0, T1 t1, ref T2 t2);
    [EntitiesForEachCompatible]
    public delegate void VVV<T0, T1, T2>(T0 t0, T1 t1, T2 t2);
    [EntitiesForEachCompatible]
    public delegate void RIII<T0, T1, T2, T3>(ref T0 t0, in T1 t1, in T2 t2, in T3 t3);
    [EntitiesForEachCompatible]
    public delegate void RRII<T0, T1, T2, T3>(ref T0 t0, ref T1 t1, in T2 t2, in T3 t3);
    [EntitiesForEachCompatible]
    public delegate void RRRI<T0, T1, T2, T3>(ref T0 t0, ref T1 t1, ref T2 t2, in T3 t3);
    [EntitiesForEachCompatible]
    public delegate void RRRR<T0, T1, T2, T3>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3);
    [EntitiesForEachCompatible]
    public delegate void IIII<T0, T1, T2, T3>(in T0 t0, in T1 t1, in T2 t2, in T3 t3);
    [EntitiesForEachCompatible]
    public delegate void VIII<T0, T1, T2, T3>(T0 t0, in T1 t1, in T2 t2, in T3 t3);
    [EntitiesForEachCompatible]
    public delegate void VRII<T0, T1, T2, T3>(T0 t0, ref T1 t1, in T2 t2, in T3 t3);
    [EntitiesForEachCompatible]
    public delegate void VRRI<T0, T1, T2, T3>(T0 t0, ref T1 t1, ref T2 t2, in T3 t3);
    [EntitiesForEachCompatible]
    public delegate void VRRR<T0, T1, T2, T3>(T0 t0, ref T1 t1, ref T2 t2, ref T3 t3);
    [EntitiesForEachCompatible]
    public delegate void VVII<T0, T1, T2, T3>(T0 t0, T1 t1, in T2 t2, in T3 t3);
    [EntitiesForEachCompatible]
    public delegate void VVRI<T0, T1, T2, T3>(T0 t0, T1 t1, ref T2 t2, in T3 t3);
    [EntitiesForEachCompatible]
    public delegate void VVRR<T0, T1, T2, T3>(T0 t0, T1 t1, ref T2 t2, ref T3 t3);
    [EntitiesForEachCompatible]
    public delegate void VVVI<T0, T1, T2, T3>(T0 t0, T1 t1, T2 t2, in T3 t3);
    [EntitiesForEachCompatible]
    public delegate void VVVR<T0, T1, T2, T3>(T0 t0, T1 t1, T2 t2, ref T3 t3);
    [EntitiesForEachCompatible]
    public delegate void VVVV<T0, T1, T2, T3>(T0 t0, T1 t1, T2 t2, T3 t3);
    [EntitiesForEachCompatible]
    public delegate void RIIII<T0, T1, T2, T3, T4>(ref T0 t0, in T1 t1, in T2 t2, in T3 t3, in T4 t4);
    [EntitiesForEachCompatible]
    public delegate void RRIII<T0, T1, T2, T3, T4>(ref T0 t0, ref T1 t1, in T2 t2, in T3 t3, in T4 t4);
    [EntitiesForEachCompatible]
    public delegate void RRRII<T0, T1, T2, T3, T4>(ref T0 t0, ref T1 t1, ref T2 t2, in T3 t3, in T4 t4);
    [EntitiesForEachCompatible]
    public delegate void RRRRI<T0, T1, T2, T3, T4>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, in T4 t4);
    [EntitiesForEachCompatible]
    public delegate void RRRRR<T0, T1, T2, T3, T4>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4);
    [EntitiesForEachCompatible]
    public delegate void IIIII<T0, T1, T2, T3, T4>(in T0 t0, in T1 t1, in T2 t2, in T3 t3, in T4 t4);
    [EntitiesForEachCompatible]
    public delegate void VIIII<T0, T1, T2, T3, T4>(T0 t0, in T1 t1, in T2 t2, in T3 t3, in T4 t4);
    [EntitiesForEachCompatible]
    public delegate void VRIII<T0, T1, T2, T3, T4>(T0 t0, ref T1 t1, in T2 t2, in T3 t3, in T4 t4);
    [EntitiesForEachCompatible]
    public delegate void VRRII<T0, T1, T2, T3, T4>(T0 t0, ref T1 t1, ref T2 t2, in T3 t3, in T4 t4);
    [EntitiesForEachCompatible]
    public delegate void VRRRI<T0, T1, T2, T3, T4>(T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, in T4 t4);
    [EntitiesForEachCompatible]
    public delegate void VRRRR<T0, T1, T2, T3, T4>(T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4);
    [EntitiesForEachCompatible]
    public delegate void VVIII<T0, T1, T2, T3, T4>(T0 t0, T1 t1, in T2 t2, in T3 t3, in T4 t4);
    [EntitiesForEachCompatible]
    public delegate void VVRII<T0, T1, T2, T3, T4>(T0 t0, T1 t1, ref T2 t2, in T3 t3, in T4 t4);
    [EntitiesForEachCompatible]
    public delegate void VVRRI<T0, T1, T2, T3, T4>(T0 t0, T1 t1, ref T2 t2, ref T3 t3, in T4 t4);
    [EntitiesForEachCompatible]
    public delegate void VVRRR<T0, T1, T2, T3, T4>(T0 t0, T1 t1, ref T2 t2, ref T3 t3, ref T4 t4);
    [EntitiesForEachCompatible]
    public delegate void VVVII<T0, T1, T2, T3, T4>(T0 t0, T1 t1, T2 t2, in T3 t3, in T4 t4);
    [EntitiesForEachCompatible]
    public delegate void VVVRI<T0, T1, T2, T3, T4>(T0 t0, T1 t1, T2 t2, ref T3 t3, in T4 t4);
    [EntitiesForEachCompatible]
    public delegate void VVVRR<T0, T1, T2, T3, T4>(T0 t0, T1 t1, T2 t2, ref T3 t3, ref T4 t4);
    [EntitiesForEachCompatible]
    public delegate void VVVVI<T0, T1, T2, T3, T4>(T0 t0, T1 t1, T2 t2, T3 t3, in T4 t4);
    [EntitiesForEachCompatible]
    public delegate void VVVVR<T0, T1, T2, T3, T4>(T0 t0, T1 t1, T2 t2, T3 t3, ref T4 t4);
    [EntitiesForEachCompatible]
    public delegate void VVVVV<T0, T1, T2, T3, T4>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4);
    [EntitiesForEachCompatible]
    public delegate void RIIIII<T0, T1, T2, T3, T4, T5>(ref T0 t0, in T1 t1, in T2 t2, in T3 t3, in T4 t4, in T5 t5);
    [EntitiesForEachCompatible]
    public delegate void RRIIII<T0, T1, T2, T3, T4, T5>(ref T0 t0, ref T1 t1, in T2 t2, in T3 t3, in T4 t4, in T5 t5);
    [EntitiesForEachCompatible]
    public delegate void RRRIII<T0, T1, T2, T3, T4, T5>(ref T0 t0, ref T1 t1, ref T2 t2, in T3 t3, in T4 t4, in T5 t5);
    [EntitiesForEachCompatible]
    public delegate void RRRRII<T0, T1, T2, T3, T4, T5>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, in T4 t4, in T5 t5);
    [EntitiesForEachCompatible]
    public delegate void RRRRRI<T0, T1, T2, T3, T4, T5>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, in T5 t5);
    [EntitiesForEachCompatible]
    public delegate void RRRRRR<T0, T1, T2, T3, T4, T5>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5);
    [EntitiesForEachCompatible]
    public delegate void IIIIII<T0, T1, T2, T3, T4, T5>(in T0 t0, in T1 t1, in T2 t2, in T3 t3, in T4 t4, in T5 t5);
    [EntitiesForEachCompatible]
    public delegate void VIIIII<T0, T1, T2, T3, T4, T5>(T0 t0, in T1 t1, in T2 t2, in T3 t3, in T4 t4, in T5 t5);
    [EntitiesForEachCompatible]
    public delegate void VRIIII<T0, T1, T2, T3, T4, T5>(T0 t0, ref T1 t1, in T2 t2, in T3 t3, in T4 t4, in T5 t5);
    [EntitiesForEachCompatible]
    public delegate void VRRIII<T0, T1, T2, T3, T4, T5>(T0 t0, ref T1 t1, ref T2 t2, in T3 t3, in T4 t4, in T5 t5);
    [EntitiesForEachCompatible]
    public delegate void VRRRII<T0, T1, T2, T3, T4, T5>(T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, in T4 t4, in T5 t5);
    [EntitiesForEachCompatible]
    public delegate void VRRRRI<T0, T1, T2, T3, T4, T5>(T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, in T5 t5);
    [EntitiesForEachCompatible]
    public delegate void VRRRRR<T0, T1, T2, T3, T4, T5>(T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5);
    [EntitiesForEachCompatible]
    public delegate void VVIIII<T0, T1, T2, T3, T4, T5>(T0 t0, T1 t1, in T2 t2, in T3 t3, in T4 t4, in T5 t5);
    [EntitiesForEachCompatible]
    public delegate void VVRIII<T0, T1, T2, T3, T4, T5>(T0 t0, T1 t1, ref T2 t2, in T3 t3, in T4 t4, in T5 t5);
    [EntitiesForEachCompatible]
    public delegate void VVRRII<T0, T1, T2, T3, T4, T5>(T0 t0, T1 t1, ref T2 t2, ref T3 t3, in T4 t4, in T5 t5);
    [EntitiesForEachCompatible]
    public delegate void VVRRRI<T0, T1, T2, T3, T4, T5>(T0 t0, T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, in T5 t5);
    [EntitiesForEachCompatible]
    public delegate void VVRRRR<T0, T1, T2, T3, T4, T5>(T0 t0, T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5);
    [EntitiesForEachCompatible]
    public delegate void VVVIII<T0, T1, T2, T3, T4, T5>(T0 t0, T1 t1, T2 t2, in T3 t3, in T4 t4, in T5 t5);
    [EntitiesForEachCompatible]
    public delegate void VVVRII<T0, T1, T2, T3, T4, T5>(T0 t0, T1 t1, T2 t2, ref T3 t3, in T4 t4, in T5 t5);
    [EntitiesForEachCompatible]
    public delegate void VVVRRI<T0, T1, T2, T3, T4, T5>(T0 t0, T1 t1, T2 t2, ref T3 t3, ref T4 t4, in T5 t5);
    [EntitiesForEachCompatible]
    public delegate void VVVRRR<T0, T1, T2, T3, T4, T5>(T0 t0, T1 t1, T2 t2, ref T3 t3, ref T4 t4, ref T5 t5);
    [EntitiesForEachCompatible]
    public delegate void VVVVII<T0, T1, T2, T3, T4, T5>(T0 t0, T1 t1, T2 t2, T3 t3, in T4 t4, in T5 t5);
    [EntitiesForEachCompatible]
    public delegate void VVVVRI<T0, T1, T2, T3, T4, T5>(T0 t0, T1 t1, T2 t2, T3 t3, ref T4 t4, in T5 t5);
    [EntitiesForEachCompatible]
    public delegate void VVVVRR<T0, T1, T2, T3, T4, T5>(T0 t0, T1 t1, T2 t2, T3 t3, ref T4 t4, ref T5 t5);
    [EntitiesForEachCompatible]
    public delegate void VVVVVI<T0, T1, T2, T3, T4, T5>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, in T5 t5);
    [EntitiesForEachCompatible]
    public delegate void VVVVVR<T0, T1, T2, T3, T4, T5>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, ref T5 t5);
    [EntitiesForEachCompatible]
    public delegate void VVVVVV<T0, T1, T2, T3, T4, T5>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5);
    [EntitiesForEachCompatible]
    public delegate void RIIIIII<T0, T1, T2, T3, T4, T5, T6>(ref T0 t0, in T1 t1, in T2 t2, in T3 t3, in T4 t4, in T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void RRIIIII<T0, T1, T2, T3, T4, T5, T6>(ref T0 t0, ref T1 t1, in T2 t2, in T3 t3, in T4 t4, in T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void RRRIIII<T0, T1, T2, T3, T4, T5, T6>(ref T0 t0, ref T1 t1, ref T2 t2, in T3 t3, in T4 t4, in T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void RRRRIII<T0, T1, T2, T3, T4, T5, T6>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, in T4 t4, in T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void RRRRRII<T0, T1, T2, T3, T4, T5, T6>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, in T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void RRRRRRI<T0, T1, T2, T3, T4, T5, T6>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void RRRRRRR<T0, T1, T2, T3, T4, T5, T6>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6);
    [EntitiesForEachCompatible]
    public delegate void IIIIIII<T0, T1, T2, T3, T4, T5, T6>(in T0 t0, in T1 t1, in T2 t2, in T3 t3, in T4 t4, in T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VIIIIII<T0, T1, T2, T3, T4, T5, T6>(T0 t0, in T1 t1, in T2 t2, in T3 t3, in T4 t4, in T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VRIIIII<T0, T1, T2, T3, T4, T5, T6>(T0 t0, ref T1 t1, in T2 t2, in T3 t3, in T4 t4, in T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VRRIIII<T0, T1, T2, T3, T4, T5, T6>(T0 t0, ref T1 t1, ref T2 t2, in T3 t3, in T4 t4, in T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VRRRIII<T0, T1, T2, T3, T4, T5, T6>(T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, in T4 t4, in T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VRRRRII<T0, T1, T2, T3, T4, T5, T6>(T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, in T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VRRRRRI<T0, T1, T2, T3, T4, T5, T6>(T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VRRRRRR<T0, T1, T2, T3, T4, T5, T6>(T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VVIIIII<T0, T1, T2, T3, T4, T5, T6>(T0 t0, T1 t1, in T2 t2, in T3 t3, in T4 t4, in T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VVRIIII<T0, T1, T2, T3, T4, T5, T6>(T0 t0, T1 t1, ref T2 t2, in T3 t3, in T4 t4, in T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VVRRIII<T0, T1, T2, T3, T4, T5, T6>(T0 t0, T1 t1, ref T2 t2, ref T3 t3, in T4 t4, in T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VVRRRII<T0, T1, T2, T3, T4, T5, T6>(T0 t0, T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, in T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VVRRRRI<T0, T1, T2, T3, T4, T5, T6>(T0 t0, T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VVRRRRR<T0, T1, T2, T3, T4, T5, T6>(T0 t0, T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VVVIIII<T0, T1, T2, T3, T4, T5, T6>(T0 t0, T1 t1, T2 t2, in T3 t3, in T4 t4, in T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VVVRIII<T0, T1, T2, T3, T4, T5, T6>(T0 t0, T1 t1, T2 t2, ref T3 t3, in T4 t4, in T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VVVRRII<T0, T1, T2, T3, T4, T5, T6>(T0 t0, T1 t1, T2 t2, ref T3 t3, ref T4 t4, in T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VVVRRRI<T0, T1, T2, T3, T4, T5, T6>(T0 t0, T1 t1, T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VVVRRRR<T0, T1, T2, T3, T4, T5, T6>(T0 t0, T1 t1, T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VVVVIII<T0, T1, T2, T3, T4, T5, T6>(T0 t0, T1 t1, T2 t2, T3 t3, in T4 t4, in T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VVVVRII<T0, T1, T2, T3, T4, T5, T6>(T0 t0, T1 t1, T2 t2, T3 t3, ref T4 t4, in T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VVVVRRI<T0, T1, T2, T3, T4, T5, T6>(T0 t0, T1 t1, T2 t2, T3 t3, ref T4 t4, ref T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VVVVRRR<T0, T1, T2, T3, T4, T5, T6>(T0 t0, T1 t1, T2 t2, T3 t3, ref T4 t4, ref T5 t5, ref T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VVVVVII<T0, T1, T2, T3, T4, T5, T6>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, in T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VVVVVRI<T0, T1, T2, T3, T4, T5, T6>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, ref T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VVVVVRR<T0, T1, T2, T3, T4, T5, T6>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, ref T5 t5, ref T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VVVVVVI<T0, T1, T2, T3, T4, T5, T6>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, in T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VVVVVVR<T0, T1, T2, T3, T4, T5, T6>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, ref T6 t6);
    [EntitiesForEachCompatible]
    public delegate void VVVVVVV<T0, T1, T2, T3, T4, T5, T6>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6);
    [EntitiesForEachCompatible]
    public delegate void RIIIIIII<T0, T1, T2, T3, T4, T5, T6, T7>(ref T0 t0, in T1 t1, in T2 t2, in T3 t3, in T4 t4, in T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void RRIIIIII<T0, T1, T2, T3, T4, T5, T6, T7>(ref T0 t0, ref T1 t1, in T2 t2, in T3 t3, in T4 t4, in T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void RRRIIIII<T0, T1, T2, T3, T4, T5, T6, T7>(ref T0 t0, ref T1 t1, ref T2 t2, in T3 t3, in T4 t4, in T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void RRRRIIII<T0, T1, T2, T3, T4, T5, T6, T7>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, in T4 t4, in T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void RRRRRIII<T0, T1, T2, T3, T4, T5, T6, T7>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, in T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void RRRRRRII<T0, T1, T2, T3, T4, T5, T6, T7>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void RRRRRRRI<T0, T1, T2, T3, T4, T5, T6, T7>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void RRRRRRRR<T0, T1, T2, T3, T4, T5, T6, T7>(ref T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7);
    [EntitiesForEachCompatible]
    public delegate void IIIIIIII<T0, T1, T2, T3, T4, T5, T6, T7>(in T0 t0, in T1 t1, in T2 t2, in T3 t3, in T4 t4, in T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VIIIIIII<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, in T1 t1, in T2 t2, in T3 t3, in T4 t4, in T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VRIIIIII<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, ref T1 t1, in T2 t2, in T3 t3, in T4 t4, in T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VRRIIIII<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, ref T1 t1, ref T2 t2, in T3 t3, in T4 t4, in T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VRRRIIII<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, in T4 t4, in T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VRRRRIII<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, in T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VRRRRRII<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VRRRRRRI<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VRRRRRRR<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVIIIIII<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, in T2 t2, in T3 t3, in T4 t4, in T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVRIIIII<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, ref T2 t2, in T3 t3, in T4 t4, in T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVRRIIII<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, ref T2 t2, ref T3 t3, in T4 t4, in T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVRRRIII<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, in T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVRRRRII<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVRRRRRI<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVRRRRRR<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, ref T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVVIIIII<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, T2 t2, in T3 t3, in T4 t4, in T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVVRIIII<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, T2 t2, ref T3 t3, in T4 t4, in T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVVRRIII<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, T2 t2, ref T3 t3, ref T4 t4, in T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVVRRRII<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVVRRRRI<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVVRRRRR<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, T2 t2, ref T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVVVIIII<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, T2 t2, T3 t3, in T4 t4, in T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVVVRIII<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, T2 t2, T3 t3, ref T4 t4, in T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVVVRRII<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, T2 t2, T3 t3, ref T4 t4, ref T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVVVRRRI<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, T2 t2, T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVVVRRRR<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, T2 t2, T3 t3, ref T4 t4, ref T5 t5, ref T6 t6, ref T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVVVVIII<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, in T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVVVVRII<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, ref T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVVVVRRI<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, ref T5 t5, ref T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVVVVRRR<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, ref T5 t5, ref T6 t6, ref T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVVVVVII<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, in T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVVVVVRI<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, ref T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVVVVVRR<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, ref T6 t6, ref T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVVVVVVI<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, in T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVVVVVVR<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, ref T7 t7);
    [EntitiesForEachCompatible]
    public delegate void VVVVVVVV<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7);
}

public static partial class LambdaForEachDescriptionConstructionMethods
{
    public static TDescription ForEach<TDescription, T0>(this TDescription description, [AllowDynamicValue] R<T0> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0>(this TDescription description, [AllowDynamicValue] I<T0> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0>(this TDescription description, [AllowDynamicValue] V<T0> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1>(this TDescription description, [AllowDynamicValue] RI<T0, T1> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1>(this TDescription description, [AllowDynamicValue] RR<T0, T1> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1>(this TDescription description, [AllowDynamicValue] II<T0, T1> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1>(this TDescription description, [AllowDynamicValue] VI<T0, T1> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1>(this TDescription description, [AllowDynamicValue] VR<T0, T1> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1>(this TDescription description, [AllowDynamicValue] VV<T0, T1> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2>(this TDescription description, [AllowDynamicValue] RII<T0, T1, T2> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2>(this TDescription description, [AllowDynamicValue] RRI<T0, T1, T2> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2>(this TDescription description, [AllowDynamicValue] RRR<T0, T1, T2> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2>(this TDescription description, [AllowDynamicValue] III<T0, T1, T2> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2>(this TDescription description, [AllowDynamicValue] VII<T0, T1, T2> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2>(this TDescription description, [AllowDynamicValue] VRI<T0, T1, T2> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2>(this TDescription description, [AllowDynamicValue] VRR<T0, T1, T2> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2>(this TDescription description, [AllowDynamicValue] VVI<T0, T1, T2> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2>(this TDescription description, [AllowDynamicValue] VVR<T0, T1, T2> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2>(this TDescription description, [AllowDynamicValue] VVV<T0, T1, T2> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3>(this TDescription description, [AllowDynamicValue] RIII<T0, T1, T2, T3> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3>(this TDescription description, [AllowDynamicValue] RRII<T0, T1, T2, T3> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3>(this TDescription description, [AllowDynamicValue] RRRI<T0, T1, T2, T3> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3>(this TDescription description, [AllowDynamicValue] RRRR<T0, T1, T2, T3> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3>(this TDescription description, [AllowDynamicValue] IIII<T0, T1, T2, T3> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3>(this TDescription description, [AllowDynamicValue] VIII<T0, T1, T2, T3> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3>(this TDescription description, [AllowDynamicValue] VRII<T0, T1, T2, T3> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3>(this TDescription description, [AllowDynamicValue] VRRI<T0, T1, T2, T3> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3>(this TDescription description, [AllowDynamicValue] VRRR<T0, T1, T2, T3> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3>(this TDescription description, [AllowDynamicValue] VVII<T0, T1, T2, T3> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3>(this TDescription description, [AllowDynamicValue] VVRI<T0, T1, T2, T3> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3>(this TDescription description, [AllowDynamicValue] VVRR<T0, T1, T2, T3> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3>(this TDescription description, [AllowDynamicValue] VVVI<T0, T1, T2, T3> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3>(this TDescription description, [AllowDynamicValue] VVVR<T0, T1, T2, T3> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3>(this TDescription description, [AllowDynamicValue] VVVV<T0, T1, T2, T3> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4>(this TDescription description, [AllowDynamicValue] RIIII<T0, T1, T2, T3, T4> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4>(this TDescription description, [AllowDynamicValue] RRIII<T0, T1, T2, T3, T4> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4>(this TDescription description, [AllowDynamicValue] RRRII<T0, T1, T2, T3, T4> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4>(this TDescription description, [AllowDynamicValue] RRRRI<T0, T1, T2, T3, T4> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4>(this TDescription description, [AllowDynamicValue] RRRRR<T0, T1, T2, T3, T4> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4>(this TDescription description, [AllowDynamicValue] IIIII<T0, T1, T2, T3, T4> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4>(this TDescription description, [AllowDynamicValue] VIIII<T0, T1, T2, T3, T4> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4>(this TDescription description, [AllowDynamicValue] VRIII<T0, T1, T2, T3, T4> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4>(this TDescription description, [AllowDynamicValue] VRRII<T0, T1, T2, T3, T4> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4>(this TDescription description, [AllowDynamicValue] VRRRI<T0, T1, T2, T3, T4> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4>(this TDescription description, [AllowDynamicValue] VRRRR<T0, T1, T2, T3, T4> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4>(this TDescription description, [AllowDynamicValue] VVIII<T0, T1, T2, T3, T4> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4>(this TDescription description, [AllowDynamicValue] VVRII<T0, T1, T2, T3, T4> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4>(this TDescription description, [AllowDynamicValue] VVRRI<T0, T1, T2, T3, T4> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4>(this TDescription description, [AllowDynamicValue] VVRRR<T0, T1, T2, T3, T4> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4>(this TDescription description, [AllowDynamicValue] VVVII<T0, T1, T2, T3, T4> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4>(this TDescription description, [AllowDynamicValue] VVVRI<T0, T1, T2, T3, T4> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4>(this TDescription description, [AllowDynamicValue] VVVRR<T0, T1, T2, T3, T4> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4>(this TDescription description, [AllowDynamicValue] VVVVI<T0, T1, T2, T3, T4> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4>(this TDescription description, [AllowDynamicValue] VVVVR<T0, T1, T2, T3, T4> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4>(this TDescription description, [AllowDynamicValue] VVVVV<T0, T1, T2, T3, T4> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] RIIIII<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] RRIIII<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] RRRIII<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] RRRRII<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] RRRRRI<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] RRRRRR<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] IIIIII<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] VIIIII<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] VRIIII<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] VRRIII<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] VRRRII<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] VRRRRI<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] VRRRRR<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] VVIIII<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] VVRIII<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] VVRRII<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] VVRRRI<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] VVRRRR<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] VVVIII<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] VVVRII<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] VVVRRI<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] VVVRRR<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] VVVVII<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] VVVVRI<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] VVVVRR<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] VVVVVI<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] VVVVVR<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5>(this TDescription description, [AllowDynamicValue] VVVVVV<T0, T1, T2, T3, T4, T5> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] RIIIIII<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] RRIIIII<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] RRRIIII<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] RRRRIII<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] RRRRRII<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] RRRRRRI<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] RRRRRRR<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] IIIIIII<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VIIIIII<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VRIIIII<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VRRIIII<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VRRRIII<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VRRRRII<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VRRRRRI<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VRRRRRR<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VVIIIII<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VVRIIII<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VVRRIII<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VVRRRII<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VVRRRRI<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VVRRRRR<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VVVIIII<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VVVRIII<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VVVRRII<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VVVRRRI<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VVVRRRR<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VVVVIII<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VVVVRII<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VVVVRRI<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VVVVRRR<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VVVVVII<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VVVVVRI<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VVVVVRR<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VVVVVVI<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VVVVVVR<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6>(this TDescription description, [AllowDynamicValue] VVVVVVV<T0, T1, T2, T3, T4, T5, T6> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] RIIIIIII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] RRIIIIII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] RRRIIIII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] RRRRIIII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] RRRRRIII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] RRRRRRII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] RRRRRRRI<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] RRRRRRRR<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] IIIIIIII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VIIIIIII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VRIIIIII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VRRIIIII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VRRRIIII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VRRRRIII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VRRRRRII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VRRRRRRI<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VRRRRRRR<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVIIIIII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVRIIIII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVRRIIII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVRRRIII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVRRRRII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVRRRRRI<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVRRRRRR<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVVIIIII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVVRIIII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVVRRIII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVVRRRII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVVRRRRI<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVVRRRRR<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVVVIIII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVVVRIII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVVVRRII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVVVRRRI<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVVVRRRR<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVVVVIII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVVVVRII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVVVVRRI<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVVVVRRR<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVVVVVII<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVVVVVRI<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVVVVVRR<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVVVVVVI<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVVVVVVR<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7>(this TDescription description, [AllowDynamicValue] VVVVVVVV<T0, T1, T2, T3, T4, T5, T6, T7> codeToRun) where TDescription : struct, ISupportForEachWithUniversalDelegate => ThrowCodeGenException<TDescription>();
}
