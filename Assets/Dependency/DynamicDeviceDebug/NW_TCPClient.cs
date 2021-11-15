using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using System.Net.Sockets;

/// <summary>
/// TCP 连接器，后台线程启动，用于传送图像或者日志到后台
/// @author duwei
/// </summary>
public class NW_TCPClient
{
#region Member Data 
    public enum ConnectType
    {
        CT_NONE,
        CT_CONNECTING,
        CT_CONNECTED,
    }

    protected ConnectType m_eCurConnectType = ConnectType.CT_NONE;
    public ConnectType ConnectState { get { return m_eCurConnectType; } }

    private Action                              m_kConnectCallback;
    private Action<byte[]>                      m_kReceiveCallback;

    private Socket 								m_kSocket = null;
    private string 								m_kIP 	  = "";
    private int 								m_iPort   = 0;

    private static int  						RECEIVE_BUFFER_SIZE = 65536;
    private byte[] 								m_cReadData = new byte[RECEIVE_BUFFER_SIZE];


    protected bool m_bReceivedState = false;
    protected byte[] m_kReceiveBuffer = null;
#endregion
    #region Member Public Func 

    public bool pro_ReceiveState { get { return m_bReceivedState; } }
    public byte[] pro_ReceiveBuffer { get { return m_kReceiveBuffer; } }

    public void Init()
    {
        ResetReceiveState();
        m_kConnectCallback  = null;
        //m_kReceiveCallback  = null;
        m_eCurConnectType   = ConnectType.CT_NONE;
        m_kSocket = null;
		m_kIP 	  = "";
		m_iPort   = 0;
    }

    public void ResetReceiveState()
    {
        m_bReceivedState = false;
        m_kReceiveBuffer = null;
    }

    public virtual void CloseSocket()
    {
        m_eCurConnectType = ConnectType.CT_NONE;
        if (m_kSocket != null)
            m_kSocket.Close();

        m_cReadData.Initialize();
    }

    public virtual void ConnectServer(string kIP,int iPort, Action kConnectedCallback, Action<byte[]> kReceiveCallback)
    {
        m_kIP   = kIP.ToLower();
        m_iPort = iPort;
        m_kConnectCallback = kConnectedCallback;
        m_kReceiveCallback = kReceiveCallback;

        IPAddress[] ipHost  = Dns.GetHostAddresses (m_kIP);
		TcpClient client    = new TcpClient (ipHost[0].AddressFamily);
		m_kSocket           = client.Client;

        m_kSocket.DontFragment = true;
        m_kSocket.SendBufferSize 	= 65536;//20Mb
		m_kSocket.ReceiveBufferSize = RECEIVE_BUFFER_SIZE;

        m_kSocket.BeginConnect(ipHost, m_iPort, new AsyncCallback(_connected), null);
        m_eCurConnectType = ConnectType.CT_CONNECTING;
    }
    
    public void Send(byte[] bytes, Int32 length)
    {
        ResetReceiveState();
        if (m_eCurConnectType != ConnectType.CT_CONNECTED)
        {
            _postMessageCallback(null);
            return;
        }

        int iSendSize = 0;
        try
        {
            iSendSize = m_kSocket.Send(bytes, length, SocketFlags.None);
        }
        catch (Exception e)
        {
            m_eCurConnectType = ConnectType.CT_NONE;
            _postMessageCallback(null);
        }

        if (iSendSize <= 0)
        {
            m_eCurConnectType = ConnectType.CT_NONE;
            _postMessageCallback(null);
        }
    }

    //public virtual void Update()
    //{
    //    if (m_kSocket == null)
    //        return;
    //    if (m_eCurConnectType != ConnectType.CT_CONNECTED)
    //        return;
    //    _startReceiveMsg();
    //}
#endregion
#region Member Private Func 
    protected virtual void _connected(IAsyncResult iar)
    {
        m_kSocket.EndConnect(iar);
        m_eCurConnectType   = ConnectType.CT_CONNECTED;
        if (m_kConnectCallback != null)
            m_kConnectCallback.Invoke();

        _startReceiveMsg();
    }

    protected virtual void _startReceiveMsg()
    {
        try {
            m_kSocket.BeginReceive(m_cReadData, 0, m_cReadData.Length, SocketFlags.None, new AsyncCallback(_endReceive), m_kSocket);
        }
        catch (Exception e) {
            CloseSocket();
        }
    }

    protected virtual void _postMessageCallback(byte[] kBuffer)
    {
        m_bReceivedState = true;
        m_kReceiveBuffer = kBuffer;
        if(m_kReceiveCallback != null) {
            m_kReceiveCallback(kBuffer);
        }
    }

    protected void _endReceive(IAsyncResult iar)
    {
        try
        {
            Socket remote = (Socket)iar.AsyncState;
            int recv = remote.EndReceive(iar);
            if (recv > 0)
            {
                byte[] kBuffer = new byte[recv];
                Buffer.BlockCopy(m_cReadData, 0, kBuffer, 0, recv);

                //Debug.Log("[NW_TCPConnector::_endReceive] Receive buffer size:"+ recv);
                _postMessageCallback(kBuffer);
            }
            else
            {
                _postMessageCallback(null);
            }

            m_kSocket.BeginReceive(m_cReadData, 0, m_cReadData.Length, SocketFlags.None, new AsyncCallback(_endReceive), m_kSocket);
        }
        catch (Exception e)
        {
            CloseSocket();
            _postMessageCallback(null);
        }
    }
    #endregion
}
