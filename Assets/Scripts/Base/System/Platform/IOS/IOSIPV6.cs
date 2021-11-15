using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class IOSAddressItem
{
    public AddressFamily af;
    public IPAddress ip;
}

public class IOSIPV6
{
    //[DllImport("__Internal")]
    //private static extern string IOSGetAddressInfo(string host);

    //public static IOSAddressItem[] ResolveIOSAddress(string host)
    //{
    //    var outstr = IOSGetAddressInfo(host);
    //    Debug.Log("IOSGetAddressInfo: " + outstr);
    //    if (outstr.StartsWith("ERROR"))
    //    {
    //        return null;
    //    }

    //    var addressliststr = outstr.Split('|');
    //    var addrlist = new List<IOSAddressItem>();
    //    foreach (string s in addressliststr)
    //    {
    //        if (String.IsNullOrEmpty(s.Trim()))
    //            continue;

    //        IOSAddressItem item = null;

    //        if (s.EndsWith("&ipv6"))
    //        {
    //            item = new IOSAddressItem();
    //            item.af = AddressFamily.InterNetworkV6;
    //            item.ip = IPAddress.Parse(s.Substring(0, s.Length - 5));
    //        }
    //        else if (s.EndsWith("&ipv4"))
    //        {
    //            item = new IOSAddressItem();
    //            item.af = AddressFamily.InterNetwork;
    //            item.ip = IPAddress.Parse(s.Substring(0, s.Length - 5));
    //        }

    //        if (item != null)
    //            addrlist.Add(item);

    //    }
    //    return addrlist.ToArray();
    //}
}
