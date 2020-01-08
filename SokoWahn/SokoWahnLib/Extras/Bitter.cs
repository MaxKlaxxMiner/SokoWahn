using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

/* * * * * * * * * * * * *
 *  Quelle: ngMax.Mcart  *
 * * * * * * * * * * * * */

namespace SokoWahnLib
{
  /// <summary>
  /// wie Bitter64, jedoch werden Zeiger statt normale UInt64-Arrays benutzt
  /// </summary>
  public unsafe struct Bitter : IDisposable
  {
    /// <summary>
    /// merkt sich die Anzahl der gespeicherten Bits
    /// </summary>
    readonly ulong bitCount;
    /// <summary>
    /// merkt sich die Bits in ein Byte-Array
    /// </summary>
    readonly ulong* data;

    /// <summary>
    /// Konstruktor
    /// </summary>
    /// <param name="bitCount">Anzahl der Bits, welche reserviert werden sollen</param>
    public Bitter(ulong bitCount)
    {
      this.bitCount = bitCount;
      ulong limit = (bitCount + 63) / 64;
      data = (ulong*)Marshal.AllocHGlobal((IntPtr)(limit * sizeof(ulong)));
      for (ulong i = 0; i < limit; i++) data[i] = 0;
    }

    /// <summary>
    /// gibt die Länge des Arrays in Bits zurück
    /// </summary>
    public ulong Length { get { return bitCount; } }

    /// <summary>
    /// setzt ein Bit an einer bestimmten Position
    /// </summary>
    /// <param name="bitPos">Position, wo das Bit gesetzt werden soll</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBit(ulong bitPos)
    {
      Debug.Assert(bitPos < bitCount);
      uint valPos = (uint)(bitPos >> 6);
      int bit = (int)((uint)bitPos & 0x3f);
      data[valPos] |= 1UL << bit;
    }

    /// <summary>
    /// setzt ein 8-Bit wert an einer bestimmten Bit-Position
    /// </summary>
    /// <param name="bitPos">Position, wohin der 8-Bit Wert geschrieben werden soll</param>
    /// <param name="value">Wert, welcher geschrieben werden soll</param>
    public void SetByte(ulong bitPos, byte value)
    {
      Debug.Assert(bitPos + 8 <= bitCount);
      // todo: optimize
      if ((value & 1) != 0) SetBit(bitPos); else ClearBit(bitPos);
      if ((value & 2) != 0) SetBit(bitPos + 1); else ClearBit(bitPos + 1);
      if ((value & 4) != 0) SetBit(bitPos + 2); else ClearBit(bitPos + 2);
      if ((value & 8) != 0) SetBit(bitPos + 3); else ClearBit(bitPos + 3);
      if ((value & 16) != 0) SetBit(bitPos + 4); else ClearBit(bitPos + 4);
      if ((value & 32) != 0) SetBit(bitPos + 5); else ClearBit(bitPos + 5);
      if ((value & 64) != 0) SetBit(bitPos + 6); else ClearBit(bitPos + 6);
      if ((value & 128) != 0) SetBit(bitPos + 7); else ClearBit(bitPos + 7);
    }

    /// <summary>
    /// setzt einen 16-Bit Wert an einer bestimmten Bit-Position
    /// </summary>
    /// <param name="bitPos">Position, wohin der 16-Bit Wert geschrieben werden soll</param>
    /// <param name="value">Wert, welcher geschrieben werden soll</param>
    public void SetUShort(ulong bitPos, ushort value)
    {
      Debug.Assert(bitPos + 16 <= bitCount);
      SetByte(bitPos, (byte)value);
      SetByte(bitPos + 8, (byte)(value >> 8));
    }

    /// <summary>
    /// setzt einen 24-Bit Wert an einer bestimmten Bit-Position
    /// </summary>
    /// <param name="bitPos">Position, wohin der 24-Bit Wert geschrieben werden soll</param>
    /// <param name="value">Wert, welcher geschrieben werden soll</param>
    public void SetUInt24(ulong bitPos, uint value)
    {
      Debug.Assert(bitPos + 24 <= bitCount);
      Debug.Assert(value < 16777216);
      SetByte(bitPos, (byte)value);
      SetByte(bitPos + 8, (byte)(value >> 8));
      SetByte(bitPos + 16, (byte)(value >> 16));
    }

    /// <summary>
    /// setzt einen 32-Bit Wert an einer bestimmten Bit-Position
    /// </summary>
    /// <param name="bitPos">Position, wohin der 32-Bit Wert geschrieben werden soll</param>
    /// <param name="value">Wert, welcher geschrieben werden soll</param>
    public void SetUInt(ulong bitPos, uint value)
    {
      Debug.Assert(bitPos + 32 <= bitCount);
      SetByte(bitPos, (byte)value);
      SetByte(bitPos + 8, (byte)(value >> 8));
      SetByte(bitPos + 16, (byte)(value >> 16));
      SetByte(bitPos + 24, (byte)(value >> 24));
    }

    /// <summary>
    /// setzt einen 64-Bit Wert an einer bestimmten Bit-Position
    /// </summary>
    /// <param name="bitPos">Position, wohin der 64-Bit Wert geschrieben werden soll</param>
    /// <param name="value">Wert, welcher geschrieben werden soll</param>
    public void SetULong(ulong bitPos, ulong value)
    {
      Debug.Assert(bitPos + 64 <= bitCount);
      SetByte(bitPos, (byte)value);
      SetByte(bitPos + 8, (byte)(value >> 8));
      SetByte(bitPos + 16, (byte)(value >> 16));
      SetByte(bitPos + 24, (byte)(value >> 24));
      SetByte(bitPos + 32, (byte)(value >> 32));
      SetByte(bitPos + 40, (byte)(value >> 40));
      SetByte(bitPos + 48, (byte)(value >> 48));
      SetByte(bitPos + 56, (byte)(value >> 56));
    }

    /// <summary>
    /// setzt mehrere Bits ab einer bestimmten Position
    /// </summary>
    /// <param name="bitPos">Startposiition, wo die Bits gesetzt werden sollen</param>
    /// <param name="count">Anzahl der zu setzenden Bits</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBits(ulong bitPos, ulong count)
    {
      ulong endPos = bitPos + count;
      Debug.Assert(bitPos <= bitCount && endPos <= bitCount);
      uint valPos = (uint)(bitPos >> 6);
      for (; bitPos < endPos; bitPos++)
      {
        int bit = (int)((uint)bitPos & 0x3f);
        if (bit == 0) break;
        data[valPos] |= 1UL << bit;
      }
      valPos = (uint)(bitPos >> 6);
      bitPos += 63;
      for (; bitPos < endPos; bitPos += 64, valPos++)
      {
        data[valPos] = 0xffffffffffffffff;
      }
      bitPos -= 63;
      for (; bitPos < endPos; bitPos++)
      {
        int bit = (int)((uint)bitPos & 0x3f);
        data[valPos] |= 1UL << bit;
      }
    }

    /// <summary>
    /// merkt sich die negative Bit-Maske für schnelle Abfragen
    /// </summary>
    static readonly ulong[] Mask =
    {
      ~(1UL << 0), ~(1UL << 1), ~(1UL << 2), ~(1UL << 3), ~(1UL << 4), ~(1UL << 5), ~(1UL << 6), ~(1UL << 7),
      ~(1UL << 8), ~(1UL << 9), ~(1UL << 10), ~(1UL << 11), ~(1UL << 12), ~(1UL << 13), ~(1UL << 14), ~(1UL << 15),
      ~(1UL << 16), ~(1UL << 17), ~(1UL << 18), ~(1UL << 19), ~(1UL << 20), ~(1UL << 21), ~(1UL << 22), ~(1UL << 23),
      ~(1UL << 24), ~(1UL << 25), ~(1UL << 26), ~(1UL << 27), ~(1UL << 28), ~(1UL << 29), ~(1UL << 30), ~(1UL << 31),
      ~(1UL << 32), ~(1UL << 33), ~(1UL << 34), ~(1UL << 35), ~(1UL << 36), ~(1UL << 37), ~(1UL << 38), ~(1UL << 39),
      ~(1UL << 40), ~(1UL << 41), ~(1UL << 42), ~(1UL << 43), ~(1UL << 44), ~(1UL << 45), ~(1UL << 46), ~(1UL << 47),
      ~(1UL << 48), ~(1UL << 49), ~(1UL << 50), ~(1UL << 51), ~(1UL << 52), ~(1UL << 53), ~(1UL << 54), ~(1UL << 55),
      ~(1UL << 56), ~(1UL << 57), ~(1UL << 58), ~(1UL << 59), ~(1UL << 60), ~(1UL << 61), ~(1UL << 62), ~(1UL << 63)
    };

    /// <summary>
    /// löscht ein Bit an einer bestimmten Position
    /// </summary>
    /// <param name="bitPos">Position, wo das Bit gelöscht werden soll</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearBit(ulong bitPos)
    {
      Debug.Assert(bitPos < bitCount);
      uint valPos = (uint)(bitPos >> 6);
      data[valPos] &= Mask[(uint)bitPos & 0x3f];
    }

    /// <summary>
    /// löscht mehrere Bits ab einer bestimmten Position
    /// </summary>
    /// <param name="bitPos">Startposition, wo die Bits gelöscht werden sollen</param>
    /// <param name="count">Anzahl der zu löschenden Bits</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearBits(ulong bitPos, ulong count)
    {
      ulong endPos = bitPos + count;
      Debug.Assert(bitPos <= bitCount && endPos <= bitCount);
      uint valPos = (uint)(bitPos >> 6);
      for (; bitPos < endPos; bitPos++)
      {
        int bit = (int)((uint)bitPos & 0x3f);
        if (bit == 0) break;
        data[valPos] &= Mask[bit];
      }
      valPos = (uint)(bitPos >> 6);
      bitPos += 63;
      for (; bitPos < endPos; bitPos += 64, valPos++)
      {
        data[valPos] = 0x0000000000000000;
      }
      bitPos -= 63;
      for (; bitPos < endPos; bitPos++)
      {
        int bit = (int)((uint)bitPos & 0x3f);
        data[valPos] &= Mask[bit];
      }
    }

    /// <summary>
    /// gibt das Bit an einer bestimmten Position zurück
    /// </summary>
    /// <param name="bitPos">Position, wo das Bit abgefragt werden soll</param>
    /// <returns>das jeweilige Bit</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetBit(ulong bitPos)
    {
      Debug.Assert(bitPos < bitCount);
      uint valPos = (uint)(bitPos >> 6);
      int bit = (int)((uint)bitPos & 0x3f);
      return (data[valPos] & 1UL << bit) != 0;
    }

    /// <summary>
    /// gibt einen 8-Bit Wert von einer bestimmten Bit-Position zurück
    /// </summary>
    /// <param name="bitPos">Position, wo der 8-Bit Wert abgefragt werden soll</param>
    /// <returns>ausgelesener 8-Bit Wert</returns>
    public byte GetByte(ulong bitPos)
    {
      Debug.Assert(bitPos + 8 <= bitCount);
      // todo: optimize
      byte value = 0;
      value |= GetBit(bitPos) ? (byte)1 : (byte)0;
      value |= GetBit(bitPos + 1) ? (byte)2 : (byte)0;
      value |= GetBit(bitPos + 2) ? (byte)4 : (byte)0;
      value |= GetBit(bitPos + 3) ? (byte)8 : (byte)0;
      value |= GetBit(bitPos + 4) ? (byte)16 : (byte)0;
      value |= GetBit(bitPos + 5) ? (byte)32 : (byte)0;
      value |= GetBit(bitPos + 6) ? (byte)64 : (byte)0;
      value |= GetBit(bitPos + 7) ? (byte)128 : (byte)0;
      return value;
    }

    /// <summary>
    /// gibt einen 16-Bit Wert von einer bestimmten Bit-Position zurück
    /// </summary>
    /// <param name="bitPos">Position, wo der 16-Bit Wert abgefragt werden soll</param>
    /// <returns>ausgelesener 16-Bit Wert</returns>
    public ushort GetUShort(ulong bitPos)
    {
      Debug.Assert(bitPos + 16 <= bitCount);
      ushort value = 0;
      value |= GetByte(bitPos);
      value |= (ushort)(GetByte(bitPos + 8) << 8);
      return value;
    }

    /// <summary>
    /// gibt einen 24-Bit Wert von einer bestimmten Bit-Position zurück
    /// </summary>
    /// <param name="bitPos">Position, wo der 24-Bit Wert abgefragt werden soll</param>
    /// <returns>ausgelesener 24-Bit Wert</returns>
    public uint GetUInt24(ulong bitPos)
    {
      Debug.Assert(bitPos + 24 <= bitCount);
      uint value = 0;
      value |= GetByte(bitPos);
      value |= (uint)GetByte(bitPos + 8) << 8;
      value |= (uint)GetByte(bitPos + 16) << 16;
      return value;
    }

    /// <summary>
    /// gibt einen 32-Bit Wert von einer bestimmten Bit-Position zurück
    /// </summary>
    /// <param name="bitPos">Position, wo der 32-Bit Wert abgefragt werden soll</param>
    /// <returns>ausgelesener 32-Bit Wert</returns>
    public uint GetUInt(ulong bitPos)
    {
      Debug.Assert(bitPos + 32 <= bitCount);
      uint value = 0;
      value |= GetByte(bitPos);
      value |= (uint)GetByte(bitPos + 8) << 8;
      value |= (uint)GetByte(bitPos + 16) << 16;
      value |= (uint)GetByte(bitPos + 24) << 24;
      return value;
    }

    /// <summary>
    /// gibt einen 64-Bit Wert von einer bestimmten Bit-Position zurück
    /// </summary>
    /// <param name="bitPos">Position, wo der 64-Bit Wert abgefragt werden soll</param>
    /// <returns>ausgelesener 64-Bit Wert</returns>
    public ulong GetULong(ulong bitPos)
    {
      Debug.Assert(bitPos + 64 <= bitCount);
      ulong value = 0;
      value |= GetByte(bitPos);
      value |= (uint)GetByte(bitPos + 8) << 8;
      value |= (uint)GetByte(bitPos + 16) << 16;
      value |= (uint)GetByte(bitPos + 24) << 24;
      value |= (ulong)GetByte(bitPos + 32) << 32;
      value |= (ulong)GetByte(bitPos + 40) << 40;
      value |= (ulong)GetByte(bitPos + 48) << 48;
      value |= (ulong)GetByte(bitPos + 56) << 56;
      return value;
    }

    /// <summary>
    /// zählt die Anzahl der freien Bits ab einer bestimmten Position
    /// </summary>
    /// <param name="bitPos">Startposition der Bits, wo die Zählung beginnen soll</param>
    /// <returns>Anzahl der nicht markierten Bits</returns>
    public ulong CountFreeBits(ulong bitPos)
    {
      Debug.Assert(bitPos <= bitCount);
      ulong pos = bitPos;
      uint valPos = (uint)(pos >> 6);
      ulong dataVal = data[valPos];
      for (; pos < bitCount; pos++)
      {
        int bit = (int)((uint)pos & 0x3f);
        if (bit == 0) break;
        if ((dataVal & 1UL << bit) != 0) return pos - bitPos;
      }
      valPos = (uint)(pos >> 6);
      for (; pos < bitCount; pos += 64, valPos++)
      {
        dataVal = data[valPos];
        if (dataVal != 0x0000000000000000) break;
      }
      for (; pos < bitCount; pos++)
      {
        int bit = (int)((uint)pos & 0x3f);
        if ((dataVal & 1UL << bit) != 0) return pos - bitPos;
      }
      return bitCount - bitPos;
    }

    /// <summary>
    /// zählt die Anzahl der gesetzten Bits ab einer bestimmten Position
    /// </summary>
    /// <param name="bitPos">Startposition der Bits, wo die Zählung beginnen soll</param>
    /// <returns>Anzahl der markierten Bits</returns>
    public ulong CountMarkedBits(ulong bitPos)
    {
      Debug.Assert(bitPos <= bitCount);
      ulong pos = bitPos;
      uint valPos = (uint)(pos >> 6);
      ulong dataVal = data[valPos];
      for (; pos < bitCount; pos++)
      {
        int bit = (int)((uint)pos & 0x3f);
        if (bit == 0) break;
        if ((dataVal & 1UL << bit) == 0) return pos - bitPos;
      }
      valPos = (uint)(pos >> 6);
      for (; pos < bitCount; pos += 64, valPos++)
      {
        dataVal = data[valPos];
        if (dataVal != 0xffffffffffffffff) break;
      }
      for (; pos < bitCount; pos++)
      {
        int bit = (int)((uint)pos & 0x3f);
        if ((dataVal & 1UL << bit) == 0) return pos - bitPos;
      }
      return bitCount - bitPos;
    }

    /// <summary>
    /// zählt die Anzahl der Bits, welche vor einer bestimmte Position frei sind (Rückwärts-Suche)
    /// </summary>
    /// <param name="bitPos">Startposition der Bits (exklusive = erst das vorhergehende Bit wird gezählt werden)</param>
    /// <returns>Anzahl der nicht markierten Bits</returns>
    public ulong RevCountFreeBits(ulong bitPos)
    {
      Debug.Assert(bitPos <= bitCount);
      ulong pos = bitPos - 1;
      uint valPos = (uint)(pos >> 6);
      ulong dataVal = 0xffffffffffffffff;
      if (pos < bitCount)
      {
        dataVal = data[valPos];
        for (; pos < bitCount; pos--)
        {
          int bit = (int)((uint)pos & 0x3f);
          if (bit == 0x3f) break;
          if ((dataVal & 1UL << bit) != 0) return bitPos - pos - 1;
        }
      }
      valPos = (uint)(pos >> 6);
      for (; pos < bitCount; pos -= 64, valPos--)
      {
        dataVal = data[valPos];
        if (dataVal != 0x0000000000000000) break;
      }
      for (; pos < bitCount; pos--)
      {
        int bit = (int)((uint)pos & 0x3f);
        if ((dataVal & 1UL << bit) != 0) return bitPos - pos - 1;
      }
      return bitPos;
    }

    /// <summary>
    /// zählt die Anzahl der Bits, welche vor einer bestimmten Position gesetzt sind (Rückwärts-Suche)
    /// </summary>
    /// <param name="bitPos">Startposition der Bits (exklusive = erst das vorhergehende Bit wird gezählt werden)</param>
    /// <returns>Anzahl der markierten Bits</returns>
    public ulong RevCountMarkedBits(ulong bitPos)
    {
      Debug.Assert(bitPos <= bitCount);
      ulong pos = bitPos - 1;
      uint valPos = (uint)(pos >> 6);
      ulong dataVal = 0x0000000000000000;
      if (pos < bitCount)
      {
        dataVal = data[valPos];
        for (; pos < bitCount; pos--)
        {
          int bit = (int)((uint)pos & 0x3f);
          if (bit == 0x3f) break;
          if ((dataVal & 1UL << bit) == 0) return bitPos - pos - 1;
        }
      }
      valPos = (uint)(pos >> 6);
      for (; pos < bitCount; pos -= 64, valPos--)
      {
        dataVal = data[valPos];
        if (dataVal != 0xffffffffffffffff) break;
      }
      for (; pos < bitCount; pos--)
      {
        int bit = (int)((uint)pos & 0x3f);
        if ((dataVal & 1UL << bit) == 0) return bitPos - pos - 1;
      }
      return bitPos;
    }

    /// <summary>
    /// verschiebt eine Reihe von mehrere Bits von einer Position zu einer anderen
    /// </summary>
    /// <param name="destBitPos">Ziel-Position, wohin die Bits geschoben werden sollen</param>
    /// <param name="srcBitPos">Quell-Position, welche Bits verschoben werden sollen</param>
    /// <param name="count">Anzahl der zu verschiebenden Bits</param>
    public void MoveBits(ulong destBitPos, ulong srcBitPos, ulong count)
    {
      if (bitCount == 0 || destBitPos == srcBitPos) return;
      Debug.Assert(destBitPos < bitCount);
      Debug.Assert(srcBitPos < bitCount);
      Debug.Assert(count <= bitCount);
      Debug.Assert(destBitPos + count <= bitCount);
      Debug.Assert(srcBitPos + count <= bitCount);
      if (destBitPos < srcBitPos)
      {
        for (ulong c = 0; c < count; c++)
        {
          if (GetBit(srcBitPos + c)) SetBit(destBitPos + c); else ClearBit(destBitPos + c);
        }
      }
      else
      {
        for (ulong c = count - 1; c < count; c--)
        {
          if (GetBit(srcBitPos + c)) SetBit(destBitPos + c); else ClearBit(destBitPos + c);
        }
      }
    }

    /// <summary>
    /// gibt den nicht gemanagten Speicher wieder frei
    /// </summary>
    public void Dispose()
    {
      Marshal.FreeHGlobal((IntPtr)data);
    }
  }
}
