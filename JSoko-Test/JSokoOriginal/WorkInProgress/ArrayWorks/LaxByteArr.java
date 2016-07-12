/**
 *  JSoko - A Java implementation of the game of Sokoban
 *  Copyright (c) 2012 by Matthias Meger, Germany
 * 
 *  This file is part of JSoko.
 *
 *	JSoko is free software; you can redistribute it and/or modify
 *	it under the terms of the GNU General Public License as published by
 *	the Free Software Foundation; either version 2 of the License, or
 *	(at your option) any later version.
 *	
 *	This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
package de.sokoban_online.jsoko.workInProgress.arrayWorks;

/**
 * Here we implement large index byte arrays (lax = large index).
 * The index is typed {@code long} instead of {@code int}.
 * The array content is split into blocks as necessary.
 */
public class LaxByteArr {

	/**
	 * The standard block size.
	 * We create full blocks only where necessary, and make the last block
	 * only as large as necessary.
	 * Hence we have no special reason to use a small block size,
	 * and we use the largest possible power of 2 for this.
	 */
	static public final int	DFT_BLOCKSIZE = (1 << 30);

	/**
	 * The array of blocks implementing the data content.
	 * FFS/hm: final? private?
	 */
	protected byte[][]  arrvec;
	
	/**
	 * The (large) overall array size.
	 * FFS/hm: final? private?
	 */
	protected long      size;
	
	/**
	 * The size of a single array block.
	 * Must be a normal java array dimension (max 2**31 - 1).
	 * Is always greater than 0 (strictly positive).
	 * FFS/hm: final? private?
	 */
	protected int       blocksize;
	
	
	/**
	 * Principle constructor for a large index byte array.
	 * @param size      overall size (dimension) of the array
	 * @param blocksize size of a single normal array block
	 */
	public LaxByteArr( long size, int blocksize ) {
		
		// FFS/hm: document behavior on too small / too large values
		// Check default substitutions
		if (blocksize <= 0) {
			blocksize = DFT_BLOCKSIZE;
		}
		// Document sizes
		this.size      = size;
		this.blocksize = blocksize;
		
		// Compute the needed number of blocks: divide with rounding up
		final long mblocks = (this.size + this.blocksize - 1) / this.blocksize;
		
		// Check convertibility into int index for block vector
		if (mblocks < 0) {
			throw new NegativeArraySizeException("mblocks="+mblocks);
		}
		if (mblocks > Integer.MAX_VALUE) {
			throw new OutOfMemoryError("mblocks="+mblocks);
			// FFS/hm new exception
		}
		final int nblocks = (int) mblocks;
		
		if (nblocks > 0) {
			// We have some nominal content ==> we will allocate something
			// Build top level block vector
			this.arrvec = new byte[nblocks][];
		
			// Fill in the full blocks
			final int fullblocks = nblocks - 1;		// is >= 0
			for( int j=0 ; j<fullblocks ; ++j ) {
				this.arrvec[j] = new byte[this.blocksize];
			}
			
			// Fill in the (potentially incomplete) last block
			if (nblocks > fullblocks) {
				int lastsize = (int) (this.size - (fullblocks * this.blocksize));
				this.arrvec[fullblocks] = new byte[lastsize];
			}
		}
	}
	
	/**
	 * Standard constructor for a large index byte array,
	 * using the default block size.
	 * @param size overall size (dimension) of the array
	 */
	public LaxByteArr( long size ) {
		this(size, 0);
	}
	
	/**
	 * Creates a new {@code LaxByteArr} from an existing java byte array,
	 * either by copying or by referencing the source array.
	 * 
	 * @param srcarr   existing java byte array
	 * @param copysrc  whether the source is to be copied (instead of just
	 *                 referenced)
	 */
	private LaxByteArr( byte[] srcarr, boolean copysrc ) {
		// FFS/hm: (srcarr==null) ?
		this.size      = srcarr.length;
		this.blocksize = srcarr.length;
		this.arrvec    = new byte[1][];
		if (copysrc) {
			// clone srcarr... (java arrays are cloned as copies)
			this.arrvec[0] = srcarr.clone();
		} else {
			// just reference the argument array...
			this.arrvec[0] = srcarr;
		}
	}
	
	/**
	 * Factory method to embed an existing normal java array into
	 * an {@code LaxByteArr}.
	 * @param srcarr normal array to be embedded
	 * @return new object referencing the embedded array
	 */
	public static LaxByteArr makeEmbedded(byte[] srcarr) {
		return new LaxByteArr(srcarr, false);
	}
	
	/**
	 * Tell the overall size of the large index byte array.
	 * Corresponds to the member of same name of array objects.
	 * @return size of the array
	 */
	public long length() {
		return size;
	}
	
	// -----------------------------------------------------------------------
	// Byte access operations (byte aligned)
	
	/**
	 * Reads the element value at the passed large index.
	 * Models the array read access.
	 * 
	 * @param lax large index of the array member
	 * @return value of the array member at the index {@code lax}
	 */
	public byte getAt(long lax) {
		if ((lax < 0) || (lax >= size)) {
			throw new ArrayIndexOutOfBoundsException("lax="+lax+",size="+size);
		}
		if (lax < blocksize) {
			// the simple case: one flat array
			return (arrvec[0][ (int)lax ]);
		}
		long blockno = lax / blocksize;
		long eleminx = lax % blocksize;
		return (arrvec[(int)blockno][(int)eleminx]);
	}
	
	/**
	 * Writes an element value at the passed large index.
	 * 
	 * @param lax large index of the array member
	 * @param val the value to store
	 * @return the just stored value
	 */
	public byte putAt(long lax, byte val) {
		if (lax < blocksize) {
			// the simple case: one flat array
			return (arrvec[0][ (int)lax ] = val);
		}
		long blockno = lax / blocksize;
		long eleminx = lax % blocksize;
		// FFS/hm: index out of range
		return (arrvec[(int)blockno][(int)eleminx] = val);
	}
	
	/**
	 * Modifies the byte at the specified large array index
	 * by "oring in" the passed byte value.
	 * Models the operation {@code (arr[lax] |= val)}.
	 * 
	 * @param lax large index of the array member to modify
	 * @param val the value to "or in"
	 * @return the resulting (stored) byte value
	 */
	public byte orAt(long lax, byte val) {
		if (lax < blocksize) {
			// the simple case: one flat array
			return (arrvec[0][ (int)lax ] |= val);
		}
		long blockno = lax / blocksize;
		long eleminx = lax % blocksize;
		// FFS/hm: index out of range
		return (arrvec[(int)blockno][(int)eleminx] |= val);
	}
	
	private byte putMaskedAt(long lax, byte val, byte mask) {
		byte[] blk;
		long   inx;
		
		if (lax < blocksize) {
			blk = arrvec[0];
			inx = lax;
		} else {
			long blockno = lax / blocksize;
			inx          = lax % blocksize;
			blk = arrvec[(int)blockno];
		}
		int j = (int) inx;
		return (blk[j] = (byte) ((blk[j] & ~mask) | (val & mask)));
	}
	
	/* -----------------------------------------------------------------------
	 * We shall support equivalent methods as System.arraycopy()
	 */
	
	/**
	 * Similar to {@link System#arraycopy(Object, int, Object, int, int)},
	 * but both operands are {@code LaxByteArr}s, and their offsets
	 * as well as the total length to copy are {@code long}.
	 * @param src     the source array
	 * @param srcPos  starting position in the source array
	 * @param dst     the destination array
	 * @param dstPos  starting position in the destination array
	 * @param length  the number of array elements to be copied
	 */
	public static void arraycopy( LaxByteArr src, long srcPos,
			                      LaxByteArr dst, long dstPos,
			                      long length )
	{
		// Check for the simple, "small" case ...
		if (   (src.arrvec.length <= 1)
			&& (dst.arrvec.length <= 1)
			&& (srcPos == (int)srcPos)
			&& (dstPos == (int)dstPos)
			&& (length == (int)length) )
		{
			System.arraycopy( src.arrvec[0], (int)srcPos,
			                  dst.arrvec[0], (int)dstPos, (int)length );
			return;
		}
		// FFS/hm: copy in huge chunks, using System.arraycopy()
		
		// The rest is a reference implementation, defining the semantics
		for( long j=0 ; j<length ; ++j ) {
			dst.putAt(dstPos+j, src.getAt(srcPos+j));
		}
	}
	
	/**
	 * Similar to {@link System#arraycopy(Object, int, Object, int, int)},
	 * but the first (source) operand is a {@code LaxByteArr}, and its
	 * offset is {@code long}.
	 * The overall length is just a normal {@code int}, since the second
	 * operand is a normal java array, limiting the possible copy size.
	 * 
	 * @param src     the source array
	 * @param srcPos  starting position in the source array
	 * @param dst     the destination array
	 * @param dstPos  starting position in the destination array
	 * @param length  the number of array elements to be copied
	 */
	public static void arraycopy( LaxByteArr src,   long srcPos,
	                              byte       dst[], int  dstPos,
	                              int        length )
	{
		// Check for the simple, "small" case ...
		if (   (src.arrvec.length <= 1)
			&& (srcPos == (int)srcPos)  )
		{
			System.arraycopy(src.arrvec[0], (int)srcPos, dst, dstPos, length);
			return;
		}
		// FFS/hm: copy in huge chunks, using System.arraycopy()
		
		// The rest is a reference implementation, defining the semantics
		for( int j=0 ; j<length ; ++j ) {
			dst[ dstPos+j ] = src.getAt(srcPos+j);
		}
	}
	
	/**
	 * Similar to {@link System#arraycopy(Object, int, Object, int, int)},
	 * but the second (destination) operand is a {@code LaxByteArr}, and its
	 * offset is {@code long}.
	 * The overall length is just a normal {@code int}, since the second
	 * operand is a normal java array, limiting the possible copy size.
	 * 
	 * @param src     the source array
	 * @param srcPos  starting position in the source array
	 * @param dst     the destination array
	 * @param dstPos  starting position in the destination array
	 * @param length  the number of array elements to be copied
	 */
	public static void arraycopy( byte       src[], int  srcPos,
	                              LaxByteArr dst,   long dstPos,
	                              int        length )
	{
		// Check for the simple, "small" case ...
		if (   (dst.arrvec.length <= 1)
			&& (dstPos == (int)dstPos)  )
		{
			System.arraycopy(src, srcPos, dst.arrvec[0], (int)dstPos, length);
			return;
		}
		// FFS/hm: copy in huge chunks, using System.arraycopy()
		
		// The rest is a reference implementation, defining the semantics
		for( int j=0 ; j<length ; ++j ) {
			dst.putAt(dstPos+j, src[srcPos+j]);
		}
	}
	
	// -----------------------------------------------------------------------
	// Single Bit access operations (bit aligned)
	
	public boolean getBoolAt( long bitlax ) {
		byte bitoff = (byte)(bitlax & 0x07);
		long lax    = bitlax >>> 3;
		
		byte bytval = getAt(lax);
		return ((bytval >>> bitoff) & 0x01) != 0;
	}
	
	public boolean putBoolAt( long bitlax, boolean bitval ) {
		byte bitoff = (byte)(bitlax & 0x07);
		long lax    = bitlax >>> 3;
		
		byte mask   = (byte)(1 << bitoff);
		orAt(lax, mask);
		return bitval;
	}
	
	// -----------------------------------------------------------------------
	// Operations on "Num": bit sized fragments of a "long"
	
	/**
	 * Fetch a bit sized fragment of a {@code long} from a contiguous series
	 * of bits from this "array".
	 * The result is presented in unsigned interpretation (except the
	 * {@code bitcnt} is 64).
	 * 
	 * @param bitlax index of the first bit (not byte) to fetch
	 * @param totbits number of bits to fetch (at most 64)
	 * @return the indicated bits as unsigned value
	 */
	public long getNumBitsAt( long bitlax, int totbits ) {
		if (totbits > 64 || totbits < 0) {
			throw new java.lang.UnsupportedOperationException("bits="+totbits);
		}
		// asserted: 0 <= totbits <= 64
		long result = 0;
		
		int  resoff = 0;
		while (totbits > resoff) {
			int  toget  = totbits - resoff;
			byte bitoff = (byte)(bitlax & 0x07);	// [0..7]
			long lax    = bitlax >>> 3;

			int bitlen = 8 - bitoff;		// so many in this byte [1..8]
			if (bitlen > toget) {
				bitlen = toget;
			}
			long v    = getAt(lax) & 0xffL;
			v      >>>= bitoff;
			v        &= (1L << bitlen) - 1;

			result |= (v << resoff);
			resoff += bitlen;
			bitlax += bitlen;
		}
		return result;
	}
	
	public long putNumBitsAt( long bitlax, int totbits, long numval ) {
		if (totbits > 64 || totbits < 0) {
			throw new java.lang.UnsupportedOperationException("bits="+totbits);
		}
		// asserted: 0 <= totbits <= 64
		if (totbits == 0) {
			return 0L;
		}
		// asserted: 1 <= totbits <= 64
		
		// Reduce the value to put to the effectively stored bits
		if (totbits < 64) {
			numval &= (1L << totbits) - 1L;
		}
		final long retval = numval;
		
		while (totbits > 0) {
			byte bitoff = (byte)(bitlax & 07L);		// [0..7]
			long lax    = bitlax >>> 3;
		
			byte v      = (byte) numval;
			int  bitlen = 8 - bitoff;			// [1..8]
			if (bitlen > totbits) {
				bitlen = totbits;
			}
			//byte mask = (byte)(0xff >>> (8-bitlen));
			byte mask = (byte)((1 << bitlen) - 1);
			v    <<= bitoff;
			mask <<= bitoff;
			
			putMaskedAt(lax, v, mask);
			
			numval >>>= bitlen;
			bitlax   += bitlen;
			totbits  -= bitlen;
		}

		// We return the original but reduced value
		return retval;
	}
}
