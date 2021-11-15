using UnityEngine;
public class KooSdkManager:ISDKInterface {
    public void Init(string method, Sdkcallback callback, params object[] args)
    {
        //fortest
        if(PlayerPrefs.HasKey("koo_uid"))
        {
            object[] acinfo = new object[] { 0 ,PlayerPrefs.GetString("koo_uid"), PlayerPrefs.GetString("koo_psw") };
           callback(true , acinfo);
        }
        else
        {
            callback(false , null);
        }
    }

    public void Login(string method, Sdkcallback callback, params object[] args)
    {
       if(args.Length < 3 )
       {
           Debug.LogError("Login Error : ------------------");
           return;
       }

      //send to server 
      int tag =  (int)args[0];
      if(tag == 0)
      {
          Debug.Log("Login : send msg to server ");
          //for test : server Call : callback
          callback(true, args);
      }
      else if(tag == 1)
      {
          Debug.Log("Regist : send msg to server , regist account .");
          //for test : server Call : callback
          //success:
          PlayerPrefs.SetString("koo_uid" , (string)args[1]);
          PlayerPrefs.SetString("koo_psw" , (string)args[2]) ;
          callback(true, args);
      }
       

    }

    public void Logout(string method, Sdkcallback callback, params object[] args)
    {
        if(PlayerPrefs.HasKey("koo_uid"))
        {
            PlayerPrefs.DeleteKey("koo_uid");
            PlayerPrefs.DeleteKey("koo_psw");
        }

        callback(true,null);
    }

    public void Exit(string method, Sdkcallback callback, params object[] args)
    {
     
    }
}
