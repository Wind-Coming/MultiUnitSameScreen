/**
		 *  缓冲区
		 **/
using System;
using System.Net;

public class ByteBuf
{
    private byte[] m_data;
    private int m_ReadPos;
    private int m_WritePos;
    /// <summary>
    /// the data is bigEndian store
    /// </summary>
    private bool m_IsLittleEndian = false;
    private static byte[] s_emptyArray = new byte[0];
    #region Constroct
    public ByteBuf()
    {
        _Init();
        m_data = s_emptyArray;
    }
    public ByteBuf(bool isLittleEndian) : this()
    {
        m_IsLittleEndian = isLittleEndian;
    }
    public ByteBuf(int capacity) : this()
    {
        m_data = new byte[capacity];
    }
    public ByteBuf(byte[] content) : this()
    {
        m_data = content;
        m_WritePos = content.Length;
    }
    public ByteBuf(ByteBuf oldbuf) : this()
    {
        this.Clone(oldbuf);
    }
    public ByteBuf(int capacity, bool isLittleEndian) : this()
    {
        m_IsLittleEndian = isLittleEndian;
        m_data = new byte[capacity];
    }

    public ByteBuf(byte[] content, int len) : this()
    {
        m_data = new byte[len];

        Array.Copy(content, m_data, m_data.Length);

        m_WritePos = len;
    }


    #endregion
    #region Private Mothed
    private void _Init()
    {
        m_ReadPos = 0;
        m_WritePos = 0;
    }
    public int RawReadIndex(int offset)
    {
        return m_ReadPos + offset;
    }
    private void _MoveReadPos(int offset)
    {
        var newpos = m_ReadPos + offset;
        if (newpos <= m_WritePos)
        {
            m_ReadPos = newpos;
        }
    }
    public int RawWriteIndex(int offset)
    {
        return m_WritePos + offset;
    }
    private void _MoveWritePos(int offset)
    {
        var newpos = m_WritePos + offset;
        if (newpos <= m_data.Length)
        {
            m_WritePos = newpos;
        }
    }
    #endregion

    public void Clone(ByteBuf oldBuf)
    {
        this.m_ReadPos = oldBuf.m_ReadPos;
        this.m_WritePos = oldBuf.m_WritePos;
        if (this.m_data.Length >= oldBuf.m_data.Length)
        {
            Array.Clear(this.m_data, 0, this.m_data.Length);
        }
        else
        {
            this.m_data = new byte[oldBuf.m_data.Length];
        }
        if (m_data.Length > 0)
        {
            Array.Copy(oldBuf.m_data, m_data, m_data.Length);
        }
    }

    public void Recapacity(int newCapacity)
    {
        byte[] old = m_data;
        m_data = new byte[newCapacity];
        Array.Copy(old, m_data, old.Length);
    }

    public void EnsureCapacity(int newCapacity)
    {
        if (newCapacity > m_data.Length)
        {
            Recapacity(newCapacity);
        }
    }
    public void EnsureCapacityChange(int changevalue)
    {
        int newCapacity = m_WritePos + changevalue;
        EnsureCapacity(newCapacity);
    }
    /// <summary>
    /// the data is use little endian store
    /// </summary>
    public bool IsLittleEndian { get { return m_IsLittleEndian; } }
    /// <summary>
    ///when return true is data endian need convert to cpu endian
    /// </summary>
    private bool needConvertEndian { get { return m_IsLittleEndian != BitConverter.IsLittleEndian; } }
    public void Clear()
    {
        _Init();
    }
    public byte GetByte(int index)
    {
        if (CanRead(1))
        {
            return m_data[RawReadIndex(index)];
        }
        return 0;
    }
    public short GetInt16(int index)
    {
        if (CanRead(2))
        {
            var value = BitConverter.ToInt16(m_data, RawReadIndex(index));
            value = _EnsureReadEndian(value);
            return value;
        }
        return 0;
    }
    public UInt16 GetUInt16(int index)
    {
        if (CanRead(2))
        {
            var value = BitConverter.ToUInt16(m_data, RawReadIndex(index));
            value = _EnsureReadEndian(value);
            return value;
        }
        return 0;
    }
    public int GetInt32(int index)
    {
        if (CanRead(4))
        {
            var value = BitConverter.ToInt32(m_data, RawReadIndex(index));
            value = _EnsureReadEndian(value);
            return value;
        }
        return 0;
    }
    public Int64 GetInt64(int index)
    {
        if (CanRead(8))
        {
            var value = BitConverter.ToInt64(m_data, RawReadIndex(index));
            value = _EnsureReadEndian(value);
            return value;
        }
        return 0;
    }
    public UInt32 GetUInt32(int index)
    {
        if (CanRead(4))
        {
            var value = BitConverter.ToUInt32(m_data, RawReadIndex(index));
            value = _EnsureReadEndian(value);
            return value;
        }
        return 0;
    }
    public UInt64 GetUInt64(int index)
    {
        if (CanRead(8))
        {
            var value = BitConverter.ToUInt64(m_data, RawReadIndex(index));
            value = _EnsureReadEndian(value);
            return value;
        }
        return 0;
    }
    public bool CanRead(int index, int length)
    {
        return m_ReadPos + index + length <= m_WritePos;
    }
    public bool CanRead(int length)
    {
        return CanRead(0, length);
    }
    public byte ReadByte()
    {
        var bt = GetByte(0);
        _MoveReadPos(1);
        return bt;
    }
    public short ReadInt16()
    {
        var v = GetInt16(0);
        _MoveReadPos(2);
        return v;
    }
    public UInt16 ReadUInt16()
    {
        var v = GetUInt16(0);
        _MoveReadPos(2);
        return v;
    }
    public short ReadShort()
    {
        return ReadInt16();
    }
    public int ReadInt32()
    {
        var v = GetInt32(0);
        _MoveReadPos(4);
        return v;
    }
    public UInt32 ReadUInt32()
    {
        var v = GetUInt32(0);
        _MoveReadPos(4);
        return v;
    }
    public Int64 ReadInt64()
    {
        var v = GetInt64(0);
        _MoveReadPos(8);
        return v;
    }
    public UInt64 ReadUInt64()
    {
        var v = GetUInt64(0);
        _MoveReadPos(8);
        return v;
    }
    public int GetBytes(int offset, byte[] destArray, int destIndx, int length)
    {
        if (!CanRead(offset, length))
        {
            return -1;
        }
        if (destIndx + length > destArray.Length)
        {
            return -2;
        }
        Array.Copy(m_data, RawReadIndex(offset), destArray, destIndx, length);
        return length;
    }
    public int ReadToBytes(int srcIndx, byte[] destArray, int destIndx, int length)
    {
        int ret = GetBytes(srcIndx, destArray, destIndx, length);
        _MoveReadPos(length);
        return ret;
    }
    public int PeekSize()
    {
        return m_WritePos - m_ReadPos;
    }
    public int Size { get { return m_WritePos - m_ReadPos; } }
    public int Capacity { get { return m_data.Length; } }
    public int WritePos { get { return m_WritePos; } }
    public int ReadPos { get { return m_ReadPos; } }
    public byte[] RawData { get { return m_data; } }
    public bool CanWrite(int index, int length)
    {
        return m_WritePos + index + length <= m_data.Length;
    }
    public bool CanWrite(int length)
    {
        return CanWrite(0, length);
    }
    public ByteBuf SetByte(int offset, byte value)
    {
        if (CanWrite(offset, 1))
        {
            m_data[RawWriteIndex(offset)] = value;
        }
        return this;
    }
    public void SetBytes(int offset, byte[] src, int srcStartIndex, int len)
    {
        if (CanWrite(offset, len))
        {
            Array.Copy(src, srcStartIndex, m_data, RawWriteIndex(offset), len);
        }
    }
    /// <summary>
    /// Ensure  number to the data shi Data Endian
    /// </summary>
    /// <param name="data"></param>
    private void _EnsureEndian(byte[] data)
    {
        if (needConvertEndian)
        {
            Array.Reverse(data);
        }
    }
    private short _EnsureReadEndian(short value)
    {
        if (needConvertEndian)
        {
            if (IsLittleEndian)
            {
                return IPAddress.HostToNetworkOrder(value);
            }
            else
            {
                return IPAddress.NetworkToHostOrder(value);
            }
        }
        return value;
    }
    private UInt16 _EnsureReadEndian(UInt16 value)
    {
        if (needConvertEndian)
        {
#if false
		var bytes = BitConverter.GetBytes(value);
		Array.Reverse(bytes);
		return BitConverter.ToUInt16(bytes, 0);
#else
            if (IsLittleEndian)
            {
                return (UInt16)IPAddress.HostToNetworkOrder((Int16)value);
            }
            else
            {
                return (UInt16)IPAddress.NetworkToHostOrder((Int16)value);
            }
#endif
        }
        return value;
    }
    private int _EnsureReadEndian(int value)
    {
        if (needConvertEndian)
        {
            if (IsLittleEndian)
            {
                return IPAddress.HostToNetworkOrder(value);
            }
            else
            {
                return IPAddress.NetworkToHostOrder(value);
            }
        }
        return value;
    }
    private UInt32 _EnsureReadEndian(UInt32 value)
    {
        if (needConvertEndian)
        {
#if false
		var bytes = BitConverter.GetBytes(value);
		Array.Reverse(bytes);
		return BitConverter.ToUInt32(bytes, 0);
#else
            if (IsLittleEndian)
            {
                return (UInt32)IPAddress.HostToNetworkOrder((Int32)value);
            }
            else
            {
                return (UInt32)IPAddress.NetworkToHostOrder((Int32)value);
            }
#endif
        }
        return value;
    }
    private Int64 _EnsureReadEndian(Int64 value)
    {
        if (needConvertEndian)
        {
            if (IsLittleEndian)
            {
                return IPAddress.HostToNetworkOrder(value);
            }
            else
            {
                return IPAddress.NetworkToHostOrder(value);
            }
        }
        return value;
    }
    private UInt64 _EnsureReadEndian(UInt64 value)
    {
        if (needConvertEndian)
        {
#if false
		var bytes = BitConverter.GetBytes(value);
		Array.Reverse(bytes);
		return BitConverter.ToUInt64(bytes, 0);
#else
            if (IsLittleEndian)
            {
                return (UInt64)IPAddress.HostToNetworkOrder((Int64)value);
            }
            else
            {
                return (UInt64)IPAddress.NetworkToHostOrder((Int64)value);
            }
#endif
        }
        return value;
    }
    public void SkipBytes(int length)
    {
        if (length > 0)
        {
            _MoveReadPos(length);
        }
    }
    public void WriteByte(byte value)
    {
        EnsureCapacityChange(1);
        SetByte(0, value);
        _MoveWritePos(1);
    }
    public void WriteInt16(short value)
    {
        var bytes = BitConverter.GetBytes(value);
        _EnsureEndian(bytes);
        WriteBytesFrom(bytes);
    }
    public void WriteShort(short value)
    {
        WriteInt16(value);
    }
    public void WriteInt32(int value)
    {
        var bytes = BitConverter.GetBytes(value);
        _EnsureEndian(bytes);
        WriteBytesFrom(bytes);
    }
    public void WriteInt64(Int64 value)
    {
        var bytes = BitConverter.GetBytes(value);
        _EnsureEndian(bytes);
        WriteBytesFrom(bytes);
    }
    public void WriteUInt16(UInt16 value)
    {
        var bytes = BitConverter.GetBytes(value);
        _EnsureEndian(bytes);
        WriteBytesFrom(bytes);
    }
    public void WriteUInt32(UInt32 value)
    {
        var bytes = BitConverter.GetBytes(value);
        _EnsureEndian(bytes);
        WriteBytesFrom(bytes);
    }
    public void WriteUInt64(UInt64 value)
    {
        var bytes = BitConverter.GetBytes(value);
        _EnsureEndian(bytes);
        WriteBytesFrom(bytes);
    }
    public void WriteBytesFrom(ByteBuf src)
    {
        WriteBytesFrom(src, src.Size);
    }
    public void WriteBytesFrom(ByteBuf src, int len)
    {
        WriteBytesFrom(0, src.m_data, src.ReadPos, len);
    }
    public void WriteBytesFrom(byte[] src)
    {
        if (src == null)
        {
            throw new ArgumentNullException("ByteBuf WriteBytesFrom argument");
        }
        WriteBytesFrom(0, src, 0, src.Length);
    }
    public void WriteBytesFrom(int offset, byte[] src, int srcStartIndex, int len)
    {
        if (len > 0)
        {
            EnsureCapacityChange(len);
            Array.Copy(src, srcStartIndex, m_data, RawWriteIndex(offset), len);
            _MoveWritePos(len);
        }
    }

    public int CanUse()
    {
        return m_data.Length - m_WritePos;
    }

    public void Back(int length)
    {
        if (m_ReadPos - length < 0)
        {
            return;
        }

        m_ReadPos -= length;
    }

    public void MoveToHead()
    {
        int length = Size;
        Buffer.BlockCopy(m_data, m_ReadPos, m_data, 0, length);
        Array.Clear(m_data, length, m_ReadPos);
        m_ReadPos = 0;
        m_WritePos = length;
    }
}