
//using System.Runtime.InteropServices;
//[StructLayoutAttribute(LayoutKind.Sequential , CharSet = CharSet.Ansi ,Pack = 1 )]
//public struct Sdkcallback{
//    public int result;
//}


public delegate void Sdkcallback(bool tag , params object[] args);


public interface ISDKInterface
{
  void Init(string method ,Sdkcallback callback, params object[] args);
  void Login(string method , Sdkcallback callback , params object[] args);
  void Logout(string method , Sdkcallback callback , params object[] args);
  void Exit(string method , Sdkcallback callback , params object[] args);
  
  //Pay?
}