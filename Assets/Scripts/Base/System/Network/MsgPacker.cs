using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Spenve
{
    public class MessageInfo
    {
        public int msgId;
        public byte[] content;
        public int length;
        public int RetCode;
        public int msgServerType;
        private TimeSpan m_timeWhenNetReceive;

        public void Init(int id, byte[] data, int length, int serverType)
        {
            msgId = id;
            this.content = data;
            this.length = length;
            this.msgServerType = serverType;
            this.RetCode = 0;
        }

        public void SetTimeSpan(TimeSpan s)
        {
            m_timeWhenNetReceive = s;
        }

        public TimeSpan GetTimeSpan()
        {
            return m_timeWhenNetReceive;
        }
    }

    internal abstract class BaseMsgPacker 
    {
        //protected NetCacheAlloctor m_sendAlloctor = new NetCacheAlloctor(4, 2048, 32, 10);
        //protected NetCacheAlloctor m_recvAlloctor = new NetCacheAlloctor(4, 2048, 32, 10);

        internal abstract byte[] EncodeMsg(int msgId, byte[] buffer, int length, out int needSize);
        internal abstract void DecodeMsg(ByteBuf buffer, ref List<MessageInfo> messages);

        internal void FreeSendBuffer(byte[] buffer)
        {
            //m_sendAlloctor.Free(buffer);
        }

        internal void FreeRecvBuffer(byte[] buffer)
        {
            //m_recvAlloctor.Free(buffer);
        }
    }

    internal class BattleMsgPacker : BaseMsgPacker
    {
        private ByteBuf m_sendByteBuff = new ByteBuf(false);             // 消息发送数据缓冲
        private const int SEND_PACKAGE_HEAD_LENGTH = 14;                 // 客户的上行消息(数据除外)的长度
        private const int PACKAGE_SIZE_LENGTH = 4;                       // 包大小长度字节数
        private const int RECV_SKIP_LENGTH = 1;                          // 接收数据解析时跳过的长度
        private int m_sendCount = 0;

        internal override byte[] EncodeMsg(int msgId, byte[] buffer, int length, out int needSize)
        {
            needSize = (short)(length + SEND_PACKAGE_HEAD_LENGTH);
            m_sendByteBuff.Clear();
            m_sendByteBuff.EnsureCapacity(needSize + PACKAGE_SIZE_LENGTH);

            // package length
            m_sendByteBuff.WriteInt32(needSize);

            needSize += PACKAGE_SIZE_LENGTH;
            //time 8 byte
            m_sendByteBuff.WriteUInt64((UInt64)12345678998765);//TimeUtil.NowTotalMilliseconds);
            //pack id 4 byte
            m_sendByteBuff.WriteUInt32((UInt32)(m_sendCount++));
            //msg id 2 byte
            m_sendByteBuff.WriteUInt16((UInt16)msgId);
            //msg data
            m_sendByteBuff.WriteBytesFrom(0, buffer, 0, length);

            byte[] data = new byte[m_sendByteBuff.Size];  //m_sendAlloctor.Alloc(m_sendByteBuff.Size);

            m_sendByteBuff.GetBytes(0, data, 0, m_sendByteBuff.Size);

            return data;
        }

        internal override void DecodeMsg(ByteBuf buffer, ref List<MessageInfo> messages)
        {
            while (true)
            {
                if (buffer.PeekSize() < PACKAGE_SIZE_LENGTH)
                {
                    break;
                }
                int recvPackageSize = buffer.ReadInt32();
               // UnityEngine.Debug.Log("====> RecievePack : " + recvPackageSize);

                if (buffer.PeekSize() < recvPackageSize)
                {
                    buffer.Back(PACKAGE_SIZE_LENGTH);
                    break;
                }

                buffer.SkipBytes(RECV_SKIP_LENGTH);
                int msgId = buffer.ReadInt16();
                int msgLength = recvPackageSize - RECV_SKIP_LENGTH - 2;

                if(msgLength <0)
                {
                    msgLength = 0;
                }

                byte[] data = new byte[msgLength];//m_recvAlloctor.Alloc(msgLength);
                buffer.ReadToBytes(0, data, 0, msgLength);
                
                messages.Add(new MessageInfo() { msgId = msgId, content = data, length = msgLength });
            }
            buffer.MoveToHead();
        }
    }
}
