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

import java.util.Comparator;
import java.util.Iterator;
import java.util.Map;
import java.util.NoSuchElementException;
import java.util.SortedMap;

/**
 * A sparse array implements something like an unlimited array:
 * literally all {@code long} values can be used as index.
 * The array is initialized with all {@code null} entries.
 * Most {@code null} entries are not backed up by real memory.
 * <p>
 * A sparse array can also be seen as a {@code Map<Long,V>},
 * which does not permit {@code null} keys or {@code null} values.
 * Mapping a key to {@code null} does not throw an exception, but rather
 * removes any former mapping of the key (if any was present).
 * <p>
 * A sparse array is implemented as a path compressed binary tree,
 * omitting all subtrees that do contain {@code null} values, only.
 * <p>
 * Technical notes:<br>
 * We expect a sparse array to never become really completely filled.
 * E.g. we could not really represent the number of entries by a {@code long}.
 * <p>
 * This data type is not thread safe.
 * 
 * @param <V> the type of the array elements, i.e. the type of mapped values
 * 
 * @author Heiner Marxen
 */
public class SparseArray<V>
		implements Iterable<SparseArray.ArrEntry<V>>
		         , Cloneable
//		         , Map<Long, V>
{
	/*
	 * FFS/hm: implement AbstractMap<Long,V>
	 */

	/**
	 * The maximal depth of a node, i.e. the depth of the {@link #root}.
	 */
	private static final byte MAX_DEP = 64;
	
	/**
	 * The root of the internal binary tree.
	 */
	private Node<V> root;
	
	/**
	 * The number of non-{@code null} entries.
	 * Just to implement {@link Map#size()}.
	 */
	private long elemcount;

	/**
	 * Creates a new, empty sparse array.
	 */
	public SparseArray() {
	}

	/**
	 * Creates a new sparse array from an existing one.
	 * The internal tree is fully copied, the referenced values of type V
	 * are not copied, of course.
	 * Passing a {@code null} creates an empty sparse array.
	 * 
	 * @param src the sparse array to be duplicated
	 */
	public SparseArray(SparseArray<V> src) {
		this();
		if (null != src) {
			if (null != src.root) {
				this.elemcount = src.elemcount;
				this.root      = src.root.treeClone();
			}
		}
	}
	
	/**
	 * Creates a new sparse array with just one entry.
	 * 
	 * @param inx   index of the initial element
	 * @param value value of the initial element
	 */
	public SparseArray(long inx, V value) {
		this();
		this.wr(inx, value);
	}
	
	/**
	 * Tells, whether the SparseArray does not contain any non-{@code null}
	 * element.
	 * <p>
	 * This is also part of the {@link Map} interface.
	 * 
	 * @return whether the SparseArray is empty
	 */
	public boolean isEmpty() {
		return root == null;
	}
	
	/**
	 * Removes all entries from the object.
	 * <p>
	 * This method is also part of the map interface.
	 */
	public void clear() {
		this.root      = null;
		this.elemcount = 0;
	}
	
	/**
	 * The internal data of the {@code SparseArray} is a binary tree.
	 * This are the nodes of the tree.
	 * We distinguish inner nodes and leaf nodes, since they store references
	 * of different type.
	 * Here we have the common part, shared by all nodes.
	 * 
	 * @author Heiner Marxen
	 *
	 * @param <V>
	 */
	private abstract static class Node<V>
			implements Cloneable
	{
		/**
		 * Contains the prefix of the Node, directly comparable with the index.
		 * The smallest potential index inside the (sub-)tree.
		 */
		private long pref;
		/**
		 * The length of the effective prefix in bits.
		 */
		private byte plen;
		/**
		 * The depth of the node, including the prefix.
		 * This member is not really needed, since its value is recreated
		 * during all tree parsing.
		 * It is here just for debugging purposes (asserts).
		 */
		private byte ndep;
		
		private Node(long pref, byte plen, byte ndep) {
			this.pref = pref;
			this.plen = plen;
			this.ndep = ndep;
		}
		
		private void pullPa(Node<V> pa) {
			this.plen += pa.plen + 1;
			this.pref |= pa.pref;
			this.ndep  = pa.ndep;
		}
		
		long getPref() {
			return pref;
		}
		
		/**
		 * This is a clone operation that recurses down to the Leaf nodes.
		 * The values of type V are not cloned.
		 * @return a deep copy of the tree, down to the leaf nodes
		 */
		abstract Node<V> treeClone();
	}
	
	/**
	 * This class implements the leaf nodes of the tree encoding the
	 * sparse array.
	 * Leaf nodes contain 2 references to values of the type {@code V},
	 * resolving the least significant bit of the array index.
	 * <p>
	 * Leaf nodes have a "depth" which is exactly 1 greater that their
	 * prefix length ({@code plen}).
	 * 
	 * @author Heiner Marxen
	 *
	 * @param <V> the type of the array elements, i.e. the type of mapped values
	 * @see Inner
	 */
	private static final class Leaf<V> extends Node<V>
	{
		private V sub0;
		private V sub1;
		
		private Leaf(long inx, byte dep) {
			super(inx & ~1L, (byte)(dep-1), dep);
		}
		
		private boolean lfIsEmpty() {
			return (sub0 == null) && (sub1 == null);
		}
		
		private V lfput(long inx, V nval) {
			V oval;
			
			if (0 == (inx & 01)) {
				oval = this.sub0;
				this.sub0 = nval;
			} else {
				oval = this.sub1;
				this.sub1 = nval;
			}
			return oval;
		}
		
		@SuppressWarnings("unchecked")
		@Override
		final Node<V> treeClone() {
			try {
				return (Node<V>) this.clone();
			} catch (CloneNotSupportedException e) {
				throw new InternalError();
			}
		}
		
//		public Object clone() {
//			Leaf<V> r = (Leaf<V>) super.clone();
//			return r;
//		}
	}
	
	/**
	 * This class implements the inner (non-leaf) nodes of the tree,
	 * which encodes the sparse array.
	 * Each inner node contains 2 references to sub-tree nodes, which
	 * may be either inner nodes or leafs.
	 * Both these references are non-{@code null}.
	 * 
	 * @author Heiner Marxen
	 *
	 * @param <V> the type of the array elements, i.e. the type of mapped values
	 * @see Leaf
	 */
	private static final class Inner<V> extends Node<V>
	{
		private Node<V> sub0;
		private Node<V> sub1;
		
		private Inner(long inx, byte plen, byte ndep) {
			super(inx & ~ ArrayHelp.low1s(ndep-plen), plen, ndep);
		}
		
		private Node<V> iget(byte localinx) {
			return (0 == (localinx & 01)) ? sub0 : sub1;
		}
		
		private Node<V> iput(byte localinx, Node<V> nref) {
			Node<V> oref;
			
			if (0 == (localinx & 01)) {
				oref = this.sub0;
				this.sub0 = nref;
			} else {
				oref = this.sub1;
				this.sub1 = nref;
			}
			return oref;
		}
		
		private void iput2(byte localinx, Node<V> nrefinx, Node<V> nrefoth) {
			if (0 == (localinx & 01)) {
				this.sub0 = nrefinx;
				this.sub1 = nrefoth;
			} else {
				this.sub1 = nrefinx;
				this.sub0 = nrefoth;
			}
		}
		
		@SuppressWarnings("unchecked")
		@Override
		final Node<V> treeClone() {
			Inner<V> r;
			
			try {
				r = (Inner<V>) this.clone();
			} catch (CloneNotSupportedException e) {
				throw new InternalError();
			}
			
			if (null != this.sub0) {
				r.sub0 = this.sub0.treeClone();
			}
			if (null != this.sub1) {
				r.sub1 = this.sub1.treeClone();
			}
			return r;
		}
	}

	/**
	 * From the specified subtree of specified depth read the value from
	 * the slot with the specified index.
	 * 
	 * @param t   subtree to search (maybe {@code null})
	 * @param dep depth of "t" (including its prefix)
	 * @param inx index of the array slot to read its value from
	 * @return {@code null}, or the value from the array slot
	 */
	private V rd(Node<V> t, byte dep, long inx) {
		assert(dep >= 1);
		assert(dep <= MAX_DEP);
		
		while (t != null) {
			assert(dep >= 1);
			assert(dep <= MAX_DEP);
			assert(dep >  t.plen);
			assert(dep == t.ndep);
			assert(0 == (((t.pref ^ inx) >>> (dep-1)) >>> 1));
			assert(0 == (t.pref & ArrayHelp.low1s(dep - t.plen)));
			
			// If we have a prefix, check it, and reduce by it
			if (t.plen > 0) {
				dep -= t.plen;			// subtract prefix
				if (0 != ((t.pref ^ inx) >>> dep)) {
					return null;		// prefix does not match
				}
			}
			
			if (t instanceof Leaf) {
				// found a leaf
				assert(dep == 1);
				
				Leaf<V> lf = (Leaf<V>) t;
				return ((inx & 01) == 0) ? lf.sub0 : lf.sub1;
			}
			
			// Reduce the depth for the inner node, leaving the depth
			// of the subtrees
			assert(t instanceof Inner);
			assert(dep > 1);
			
			dep -= 1;
			Inner<V> ii = (Inner<V>) t;
			
			assert(ii.sub0 != null);
			assert(ii.sub1 != null);
			
			t = (((inx >>> dep) & 01) == 0) ? ii.sub0 : ii.sub1;
			// t = ii.iget((byte) (inx >>> dep));
		}
		
		return null;
	}
	
	/**
	 * Read the array element at the specified index.
	 * 
	 * @param inx indexes the (sparse) array
	 * @return {@code null}, or the element at index {@code inx}
	 */
	public V rd(long inx) {
		return rd(root, MAX_DEP, inx);
	}
	
	/**
	 * This is a helper method for {@link #wr(Node, byte, long, Object)}.
	 * We store a new reference where we fetched the last {@code Node}
	 * reference during tree path scanning: at {@code Node pa}
	 * with index {@code painx}.
	 * If {@code pa} is {@code null}, we must have fetched from the
	 * {@link #root} and thus we here store to it.
	 * 
	 * @param pa    the node to which we store a new reference, or {@code null}
	 * @param painx the index of the store
	 * @param nref  the new reference to store
	 */
	private void paput(Inner<V> pa, byte painx, Node<V> nref) {
		if (pa == null) {
			root = nref;
		} else {
			// Store leaf where we found the null in the last step
			pa.iput(painx, nref);
		}
	}
	
	/**
	 * Stores the specified value into the array slot with the specified index,
	 * and returns the former value of that array slot.
	 * The index has already been determined to be part of the specified
	 * subtree of the specified depth.
	 * 
	 * @param t    subtree to search (maybe {@code null})
	 * @param dep  depth of "t" (including its prefix)
	 * @param inx  the index of the array slot to write to
	 * @param nval the value to store at index {@code inx}
	 * @return the former content of the array slot at index {@code inx}
	 */
	private V wr(Node<V> t, byte dep, long inx, V nval) {
		// All structural change (of the tree) happens in here.
		Inner<V> pa    = null;
		byte     painx = 0;
		Inner<V> grandpa    = null;
		byte     grandpainx = 0;
		
		// Loop over the nodes "t" along the search path for the key "inx".
		while (true) {
			if (t == null) {
				// There is not (yet) such a node: we may have to create one.
				if (nval == null) {
					// Put a NIL into nothing: just do nothing
					return null;		// is empty, was empty
				}
				
				// We create a new leaf node, with most of inx as prefix
				Leaf<V> lf = new Leaf<V>(inx, dep);
				//dep = 1;		// dep -= t.plen;
				lf.lfput(inx, nval);
				elemcount++;
				
				// link to father...
				// Store leaf where we found the null in the last step
				paput(pa, painx, lf);
				return null;			// no former content found
			}
			
			// Such a node (on the search path) still is there.
			// If it has a prefix, we have to check the prefix,
			// and eventually we have to split it.
			if (t.plen > 0) {
				byte odep = dep;		// inclusive prefix
				dep -= t.plen;			// depth of the node itself
				long pdiff = t.pref ^ inx;
				if (0 != (pdiff >>> dep)) {
					// Prefix does not match!  We will return a null.
					if (nval == null) {
						// Put a NIL into nothing: just do nothing
						return null;		// is empty, was empty
					}
					// Find the split point, and generate a new split node
					// and a new leaf node
					byte hieq   = (byte) Long.numberOfLeadingZeros(pdiff);
					byte lfdep = (byte) (64 - hieq - 1);
					assert(lfdep >= 1);
					
					/*
					 * This split operation is the most complex change of
					 * this data type.  Lets have some ASCII art:
					 * high                              low   key bits
					 *                            <->          1
					 *     +---------------------+---+
					 * ... | t.pref              | t |...  |
					 *     +---------------------+---+
					 *     |<------------------->|             plen
					 *     |<----------------------------->|   pdep (old dep)
					 *                           |<------->|   dep
					 * ------------->|                         hieq
					 * After:
					 *                   |<----->|             t.plen'
					 *                   +-------+---+
					 *                   |t.pref'| t |...  |
					 *                   +-------+---+
					 *     +---------+---+
					 *     | ii.pref |ii |
					 *     +---------+---+
					 *                   +-------------+---+
					 *                   |   lf.pref   |lf |  with "nval"
					 *                   +-------------+---+
					 *                   |<--------------->|  lfdep
					 *     |<----------->|                    iplen+1
					 */
					final Leaf<V> lf = new Leaf<V>(inx, lfdep);
					lf.lfput(inx, nval);
					elemcount++;
					
					final byte iplen = (byte) (odep - lfdep - 1);
					final Inner<V> ii = new Inner<V>(inx, iplen, odep);
					final byte ixlf = (byte) ((inx >>> lfdep) & 01);
					ii.iput2(ixlf, lf, t);
					
					paput(pa, painx, ii);
					// Reduce the prefix of "t" (now below "ii")
					//t.ndep -= iplen + 1;
					//t.plen -= iplen + 1;
					t.ndep =        lfdep;
					t.plen = (byte)(lfdep - dep);
					
					return null;
				}
			}
			
			// Node "t" is there, and its prefix is handled
			// Either handle a Leaf or follow an Inner node...
			if (t instanceof Leaf) {
				assert(dep == 1);
				
				final Leaf<V> lf = (Leaf<V>) t;
				
				final V oval = lf.lfput(inx, nval);
				elemcount += ((oval!=null) ? -1 : 0) + ((nval!=null) ? +1 : 0);
				
				if (lf.lfIsEmpty()) {
					// This leaf (lf aka t) has to be discarded.
					// Store a null where we took "t" from (in its "pa").
					// But, that causes its other son to now be its only son,
					// and the other son takes over the role of "pa".
					if (pa == null) {
						this.root = null;
					} else {
						final Node<V> otherson = pa.iget((byte)(painx ^ 01));
						otherson.pullPa(pa);
						// Replace "pa" by "otherson" inside grandpa...
						if (grandpa == null) {
							this.root = otherson;
						} else {
							grandpa.iput(grandpainx, otherson);
						}
					}
				}
				
				return oval;
			}
			
			// So, "t" is an Inner node.
			// Follow its "inx" bit to the correct sub-tree...
			assert(t instanceof Inner);
			assert(dep > 1);
			
			final Inner<V> ii = (Inner<V>) t;
			dep -= 1;			// depth of the sons
			
			final byte localinx = (byte) ((inx >>> dep) & 01);
			
			// Remember 2 stages of former search, "pa" and "grandpa".
			// We need both of them when we store a null and eliminate a leaf.
			grandpa    = pa;
			grandpainx = painx;
			pa    = ii;
			painx = localinx;
			
			t = ii.iget(localinx);
		}
	}
	
	/**
	 * Stores the specified value into the array slot with the specified index,
	 * and returns the former value of that array slot.
	 * <p>
	 * Storing a {@code null} value means to remove any former value
	 * from the indexed array slot.
	 * 
	 * @param inx the index of the array slot to write to
	 * @param val the value to store at index {@code inx}
	 * @return the former content of the array slot at index {@code inx}
	 */
	public V wr(long inx, V val) {
		return wr(root, MAX_DEP, inx, val);
	}
	
	//========================================================================
	// Index Searches
	
	/**
	 * Finds and returns the first or last leaf node within the
	 * specified (sub)tree of the indicated depth.
	 * For a forward search we want the first leaf, and for a backwards
	 * search we want the last leaf node.
	 * <p>
	 * Here we do not think about signed or unsigned interpretation of
	 * any index, so in doubt we do it the unsigned way.
	 * 
	 * @param t    non-{@code null} tree to search
	 * @param dep  the "depth" of {@code t}
	 * @param forw if {@code forw==true} we search the first leaf,
	 *             otherwise we search the last leaf
	 * @return the first or last leaf node in the tree
	 */
	private Leaf<V> findFiLaLeafU(Node<V> t, byte dep, boolean forw) {
		assert(t != null);
		
		while (true) {
			dep -= t.plen;
			if (dep <= 1) {
				break;
			}
			assert(t instanceof Inner);
			
			Inner<V> ii = (Inner<V>) t;
			dep -= 1;
			t = (forw ? ii.sub0 : ii.sub1);
		}
		
		assert(dep == 1);
		Leaf<V> lf = (Leaf<V>) t;
		
		return lf;
	}
	
	/**
	 * Find the first or last value inside a leaf node.
	 * For a forward search we want the first value, and for a backwards
	 * search we want the last value.
	 * <p>
	 * Since a leaf node never contains only {@code null}s, the search for
	 * a value inside it can never fail.
	 * The depth of the leaf node is completely irrelevant, here,
	 * and therefore is not passed into the method.
	 * Since all indices implemented in a single leaf share the same sign bit,
	 * this function is independent from the signed or unsigned interpretation
	 * of the index. 
	 * 
	 * @param lf   the leaf node within which we search a value (non-null)
	 * @param forw if {@code forw==true} we search the first value,
	 *             otherwise we search the last value
	 * @param res  if non-{@code null} we fill this object and return it,
	 *             otherwise we create a new object for the result.
	 * @return the entry object with the found index and value
	 */
	private ArrEntry<V> findFiLaInLeaf( Leaf<V> lf, boolean forw, 
			                            ArrEntry<V> res )
	{
		assert(lf != null);
		long    resinx;
		V       resval;
		
		if (forw ? (lf.sub0 != null) : (lf.sub1 == null)) {
			resinx = lf.getPref();
			resval = lf.sub0;
		} else {
			resinx = lf.getPref() | 01;
			resval = lf.sub1;
		}
		
		return ArrEntry.fillOrMake(res, resinx, resval);
	}
	
	/**
	 * Find the first or last value inside a subtree of the specified depth.
	 * For a forward search we want the first value, and for a backwards
	 * search we want the last value.
	 * <p>
	 * All index logic here is <em>unsigned</em>.
	 * <p>
	 * If the search fails, {@code null} is returned.
	 * Otherwise the returned object is either the passed result object,
	 * or a freshly allocated one.
	 * 
	 * @param t    the subtree to search (non-{@code null})
	 * @param dep  the height of the subtree "t" (including its prefix)
	 * @param forw whether we search forward (increasing index)
	 * @param res  if non-{@code null} we fill this object and return it,
	 *             otherwise we create a new object for the result (if any)
	 * @return {@code null}, or the entry object with the found index and value
	 */
	private ArrEntry<V> findFiLaU( Node<V> t, byte dep, boolean forw,
			                       ArrEntry<V> res)
	{
		//assert(t != null);
		Leaf<V> lf = findFiLaLeafU(t, dep, forw);
		
		return findFiLaInLeaf(lf, forw, res);
	}
	
	/**
	 * Find the first or last value inside the sparse array.
	 * For a forward search we want the first value, and for a backwards
	 * search we want the last value.
	 * <p>
	 * All index logic here is <em>unsigned</em>.
	 * <p>
	 * If the search fails, {@code null} is returned.
	 * Otherwise the returned object is either the passed result object,
	 * or a freshly allocated one.
	 * 
	 * @param forw whether we search forward (increasing index)
	 * @param res  if non-{@code null} we fill this object and return it,
	 *             otherwise we create a new object for the result (if any)
	 * @return {@code null}, or the entry object with the found index and value
	 */
	private ArrEntry<V> findFiLaU(boolean forw, ArrEntry<V> res) {
		if (null == root) {
			return null;
		}
		return findFiLaU(root, MAX_DEP, forw, res);
	}
	
	/**
	 * Find the first or last value inside the sparse array.
	 * For a forward search we want the first value, and for a backwards
	 * search we want the last value.
	 * <p>
	 * The index logic here is optionally <em>signed</em> or <em>unsigned</em>.
	 * <p>
	 * If the search fails, {@code null} is returned.
	 * Otherwise the returned object is either the passed result object,
	 * or a freshly allocated one.
	 * 
	 * @param forw   whether we search forward (increasing index)
	 * @param signed whether index logic is done signed (java standard)
	 * @param res  if non-{@code null} we fill this object and return it,
	 *             otherwise we create a new object for the result (if any)
	 * @return {@code null}, or the entry object with the found index and value
	 */
	private ArrEntry<V> findFiLa(boolean forw, boolean signed, ArrEntry<V> res) {
		if (null == root) {
			return null;
		}
		if (signed && (0 == root.plen)) {
			// There are entries with positive and negative index.
			// The top node decides the sign bit, and must be an Inner node,
			// since Leaf nodes always decide the lowest bit, only.
			final Inner<V> ii = (Inner<V>) root;
			final Node<V> subA;
			final Node<V> subB;
			final byte subdep = (byte)(MAX_DEP - 1);
			ArrEntry<V> e;
			
			if (forw) {
				subA = ii.sub1;
				subB = ii.sub0;
			} else {
				subA = ii.sub0;
				subB = ii.sub1;
			}
			e = findFiLaU(subA, subdep, forw, res);
			if (null == e) {
				e = findFiLaU(subB, subdep, forw, res);
			}
			return e;
		}
		return findFiLaU(root, MAX_DEP, forw, res);
	}
	
//	private ArrEntry<V> findFiLaSigned(boolean forw, ArrEntry<V> res) {
//		return findFiLa(forw, true, res);
//	}
	
	
	/**
	 * Within a Leaf node search for the next value (and its index),
	 * starting at the specified index (inclusive), searching in the
	 * specified direction.
	 * The index must be for this leaf (and match its prefix).
	 * <p>
	 * As the start index may clip away the only non-{@code null} value,
	 * this search may fail (i.e. come up with a {@code null} result.
	 *   
	 * @param lf   the leaf node within which we search a value (non-null)
	 * @param inx  the first index of the search space
	 * @param forw whether we search forward (increasing index)
	 * @param res  if non-{@code null} we fill this object and return it,
	 *             otherwise we create a new object for the result (if any)
	 * @return {@code null} if the search failed, or the found index and value
	 */
	private ArrEntry<V> findGeLeInLeaf( Leaf<V> lf, long inx, boolean forw,
			                            ArrEntry<V> res )
	{
		assert(lf != null);
		
		final byte    bit = (byte) (inx & 01);
		
		if (forw) {
			if ( (bit == 0) && (lf.sub0 != null)) {
				return ArrEntry.fillOrMake(res, inx & ~01L, lf.sub0);
			}
			if (lf.sub1 != null) {
				return ArrEntry.fillOrMake(res, inx |  01L, lf.sub1);
			}
		} else {
			if ( (bit == 1) && (lf.sub1 != null)) {
				return ArrEntry.fillOrMake(res, inx |  01L, lf.sub1);
			}
			if (lf.sub0 != null) {
				return ArrEntry.fillOrMake(res, inx & ~01L, lf.sub0);
			}
		}
		return null;
	}
	
	/**
	 * This method is just a recursive prototype implementation of
	 * the next method,
	 * which implements the same operation in an iterative manner.
	 */
	private ArrEntry<V> findGeLeUrec( Node<V> t, byte dep, long inx,
			                          boolean forw, ArrEntry<V> res)
	{
		assert(t != null);
		
		if (t.plen > 0) {
			// When the search index "inx" is at the right side of the tree,
			// such that everything below the prefix is completely to be
			// searched, we (pull the index to the bound and) recur to FiLa.
			if (forw) {
				if (ArrayHelp.leU( inx, t.pref )) {
					return findFiLaU(t, dep, forw, res);
				}
			} else {
				if (ArrayHelp.geU( inx, (t.pref | ArrayHelp.low1s(dep-t.plen)) )) {
					return findFiLaU(t, dep, forw, res);
				}
			}
			
			dep -= t.plen;			// of the node itself
			if (((inx ^ t.pref) >>> dep) != 0) {
				return null;		// prefix does not match
			}
		}
		
		if (t instanceof Leaf) {
			assert(dep == 1);
			
			return findGeLeInLeaf((Leaf<V>)t, inx, forw, res);
			
		} else {
			assert(t instanceof Inner);
			assert(dep > 1);
			
			dep -= 1;				// of the sub trees
			
			final Inner<V> ii  = (Inner<V>) t;
			final byte     bit = (byte) ((inx >>> dep) & 01);
			final Node<V>  subA;
			final Node<V>  subB;
			final byte     bitA;
			
			if (forw) {
				bitA = 0; subA = ii.sub0; subB = ii.sub1;
			} else {
				bitA = 1; subA = ii.sub1; subB = ii.sub0;
			}
			assert(subA != null); assert(subB != null);
			
			if (bit == bitA) {
				// repeat with t = ii.subA, alternate is subB if not null
				ArrEntry<V> r = findGeLeUrec(subA, dep, inx, forw, res);
				if (r != null) {
					return r;
				}
				return findFiLaU(subB, dep, forw, res);
			} else {
				// repeat with t = subB
				return findGeLeUrec(subB, dep, inx, forw, res);
			}
		}
	}
	
	/**
	 * Search a tree (or subtree) for the next index with a non-null value,
	 * where the search is started at a lower bound or an upper bound.
	 * The specified index (bound) must be inside the tree (inclusive its
	 * potential prefix).
	 * <p>
	 * All index logic here is <em>unsigned</em>.
	 * <p>
	 * If the search fails, {@code null} is returned.
	 * Otherwise the returned object is either the passed result object,
	 * or a freshly allocated one.
	 * <p>
	 * This implementation is completely non-recursive, i.e. iterative
	 * version of {@link #findGeLeUrec(Node, byte, long, boolean, ArrEntry)}.
	 * 
	 * @param t    the subtree to search (non-{@code null})
	 * @param dep  the height of the subtree "t" (including its prefix)
	 * @param inx  the index to search (the bound)
	 * @param forw whether we search forward (or backward)
	 * @param res  {@code null} or an object to fill with the result
	 * @return {@code null} or an object with index and value of the found slot
	 */
	private ArrEntry<V> findGeLeU( Node<V> t, byte dep, long inx,
			                       boolean forw, ArrEntry<V> res)
	{
		assert(t != null);
		
		/*
		 * The recursion can be resolved into an iteration easily,
		 * except for one case, where we have to add a call to FiLa
		 * on the second subtree, when the (recursive) search on the first
		 * subtree comes up empty.
		 * Since that second call (to FiLa) cannot fail (on a non-null tree),
		 * it is sufficient to remember just one non-null alternative sub-tree
		 * and its depth.
		 */
		Node<V> altT   = null;
		byte    altDep = 0;
		
		while (true) {
			if (t.plen > 0) {
				// When search index "inx" is at the right side of the tree,
				// such that everything below the prefix is completely to be
				// searched, we (pull the index to the bound and) recur to FiLa.
				if (forw) {
					if (ArrayHelp.leU(inx, t.pref)) {
						return findFiLaU(t, dep, forw, res);
					}
				} else {
					if (ArrayHelp.geU(inx, (t.pref | ArrayHelp.low1s(dep - t.plen)))) {
						return findFiLaU(t, dep, forw, res);
					}
				}

				dep -= t.plen; // of the node itself
				if (((inx ^ t.pref) >>> dep) != 0) {
					// prefix does not match: try the alternative (if any)
					break;
				}
			}
			
			if (t instanceof Leaf) {
				assert (dep == 1);

				final Leaf<V>     lf = (Leaf<V>) t;
				final ArrEntry<V> r  = findGeLeInLeaf(lf, inx, forw, res);
				if (r != null) {
					return r;
				}
				// try the alternative (if any)
				break;

			} else {
				assert (t instanceof Inner);
				assert (dep > 1);

				dep -= 1; // of the sub trees

				final Inner<V> ii  = (Inner<V>) t;
				final byte     bit = (byte) ((inx >>> dep) & 01);
				final Node<V>  subA;
				final Node<V>  subB;
				final byte     bitA;

				if (forw) {
					bitA = 0; subA = ii.sub0; subB = ii.sub1;
				} else {
					bitA = 1; subA = ii.sub1; subB = ii.sub0;
				}
				assert (subA != null); assert (subB != null);

				if (bit == bitA) {
					// repeat with t = ii.subA, alternate is subB if not null
					t = subA;
					if (subB != null) {
						altT   = subB;
						altDep = dep;
					}
				} else {
					// repeat with t = subB
					t = subB;
				}
			}
		}
		
		// Maybe we have an alternative to be searched completely...
		if (altT != null) {
			return findFiLaU(altT, altDep, forw, res);
		}
		
		return null;
	}
	
	private ArrEntry<V> findGeLeU(long inx, boolean forw, ArrEntry<V> res) {
		if (null == root) {
			return null;
		}
		return findGeLeU(root, MAX_DEP, inx, forw, res);
	}
	
	private ArrEntry<V> findGeLe(long inx, boolean forw, boolean signed, ArrEntry<V> res) {
		if (null == root) {
			return null;
		}
		if (signed && (0 == root.plen)) {
			// There are entries with positive and negative index.
			// The top node decides the sign bit, and must be an Inner node.
			final Inner<V> ii = (Inner<V>) root;
			final byte subdep = (byte)(MAX_DEP - 1);
			ArrEntry<V> e = null;
			final Node<V> subA;
			final Node<V> subB;
			final long    inxB;
			final boolean do_A;
			
			// With "do_A" we code whether we will scan through potentially
			// both subtrees.
			// If not, we omit the first and scan the second subtree
			if (forw) {
				do_A = (inx <  0); subA = ii.sub1; subB = ii.sub0; inxB =  0L;
			} else {
				do_A = (inx >= 0); subA = ii.sub0; subB = ii.sub1; inxB = ~0L;
			}
			if (do_A) {
				e = findGeLeU(subA, subdep, inx , forw, res);
			}
			if (null == e) {
				e = findGeLeU(subB, subdep, inxB, forw, res);
			}
			return e;
		}
		return findGeLeU(root, MAX_DEP, inx, forw, res);
	}
	
//	private ArrEntry<V> findGeLeS(long inx, boolean forw, ArrEntry<V> res) {
//		return findGeLe(inx, forw, true, res);
//	}
	
	//========================================================================

	private long countElements(Node<V> t, byte dep) {
		long count = 0;
		
		if (t != null) {
			dep -= t.plen;
			if (t instanceof Inner) {
				assert(dep > 1);
				
				Inner<V> ii = (Inner<V>)t;
				dep -= 1;
				count += countElements(ii.sub0, dep);
				count += countElements(ii.sub1, dep);
			} else {
				assert(t instanceof Leaf);
				assert(dep == 1);
				
				Leaf<V> lf = (Leaf<V>) t;
				count += ((lf.sub0 != null) ? 1 : 0);
				count += ((lf.sub1 != null) ? 1 : 0);
			}
		}
		return count;
	}
	
	public long countElements() {
		return countElements(root, MAX_DEP);
	}

	/**
	 * Creates a new sparse array with the same content as this one.
	 * 
	 * Cloning a sparse array conceptually is a shallow clone, i.e. the
	 * elements values of type V are not copied, just their references
	 * are copied.  That is normal behavior for collection types.
	 * 
	 * But the embedded binary tree, which is not visible from the outside,
	 * has to be replaced by a fresh copy.
	 * 
	 * @return the new clone
	 */
	@SuppressWarnings("unchecked")
	@Override
	public Object clone() {
		SparseArray<V> r;
		try {
			r = (SparseArray<V>) super.clone();
		} catch (CloneNotSupportedException e) {
			throw new InternalError();
		}
		if (null != this.root) {
			r.root = r.root.treeClone();
		}
		return r;
	}
	
	
	
	//========================================================================

	public static class ArrEntry<V>
		implements Cloneable
	{
		private long	inx;
		private V		val;
		
		public long getKey() {
			return inx;
		}
		public V getVal() {
			return val;
		}
		
		public ArrEntry(long inx, V val) {
			this.inx = inx;
			this.val = val;
		}
		
		public ArrEntry() {
			this(0L, null);
		}
		
		public static <V> ArrEntry<V> fillOrMake(ArrEntry<V> ae, long inx, V val) {
			if (ae == null) {
				ae = new ArrEntry<V>(inx, val);
			} else {
				ae.inx = inx;
				ae.val = val;
			}
			return ae;
		}
		
		// NB: clone() is implemented trivially by Object.clone()
		// ? equals
		// ? hashCode: cf. Map.Entry.hashCode
	}
	
	private static long lastSearchInx(boolean forw, boolean signed) {
		if (signed) {
			return forw ? Long.MAX_VALUE : Long.MIN_VALUE;
		} else {
			return forw ? ~0L : 0L;
		}
	}
	
	private static class Iter<V> implements Iterator<ArrEntry<V>>
	{
		private final SparseArray<V> sparr;
		private final boolean forw;
		private final boolean signed;
		
		private static final byte ST_STARTED = 0x01;
		private static final byte ST_HASCURR = 0x02;
		
		private byte state = 0;
		private long curinx;
		
		public Iter(SparseArray<V> sparr, boolean forw, boolean signed) {
			this.sparr  = sparr;
			this.forw   = forw;
			this.signed = signed;
		}
		
		public Iter(SparseArray<V> sparr) {
			this(sparr, true, false);
		}
		
		public boolean hasNext() {
			if ((null == sparr) || sparr.isEmpty()) {
				return false;
			}
			// We have a non-null and non-empty "sparr" ...
			
			if (0 == (state & ST_STARTED)) {
				return true;
			}
			// ... and we have a valid "curinx" ...
			
			// We are going to incr/decr the index: is there such a value left?
			if (curinx == lastSearchInx(forw, signed)) {
				return false;
			}
			final long nextinx = curinx + (forw ? +1 : -1);
			
			return null != sparr.findGeLe(nextinx, forw, signed, null);
		}
		
		public ArrEntry<V> next() {
			if ((null == sparr) || sparr.isEmpty()) {
				throw new NoSuchElementException();
			}
			// We have a non-null and non-empty "sparr" ...
			
			ArrEntry<V> e = null;
			
			if (0 == (state & ST_STARTED)) {
				// We have to determine the first entry (cannot fail)
				e = sparr.findFiLa(forw, signed, null);
			} else {
				// ... and we have a valid "curinx" ...
				// We are going to incr/decr the index:
				// but is there such numerical value left?
				if (curinx == lastSearchInx(forw, signed)) {
					throw new NoSuchElementException();
				}
				final long nextinx = curinx + (forw ? +1 : -1);
				e = sparr.findGeLe(nextinx, forw, signed, null);
				if (null == e) {
					throw new NoSuchElementException();
				}
			}
			
			curinx = e.inx;
			state |= (ST_STARTED | ST_HASCURR);
			return e;
		}
		
		public void remove() {
			if (0 != (state & (ST_HASCURR))) {
				throw new IllegalStateException();
			}
			// ST_HASCURR ==> ST_STARTED ==> curinx is valid and sparr != null
			
			sparr.wr(curinx, null);
			
			// The "remove" operation must not be repeated on this element!
			state &= ~ST_HASCURR;
		}
	}
	
	/**
	 * Creates a new iterator over all the (index,value) pairs currently
	 * in this sparse array.
	 * Indexes are delivered in unsigned increasing order.
	 */
	@Override
	public Iterator<ArrEntry<V>> iterator() {
		return new Iter<V>(this);
	}
	
	private static final class SparrStepper<V> implements Stepper<V>
	{
		private final SparseArray<V> sparr;
		private final boolean forw;
		private final boolean signed;
		
		private static final byte ST_STARTED = 0x01;
		private static final byte ST_HASCURR = 0x02;
		
		private byte state = 0;
		private ArrEntry<V> curE = null;
		
		public SparrStepper(SparseArray<V> sparr, boolean forw, boolean signed) {
			this.sparr  = sparr;
			this.forw   = forw;
			this.signed = signed;
		}
		
		public SparrStepper(SparseArray<V> sparr) {
			this(sparr, true, false);
		}

		public boolean next() {
			if ((null == sparr) || sparr.isEmpty()) {
				return false;
			}
			// We have a non-null and non-empty "sparr" ...
			
			if (0 == (state & ST_STARTED)) {
				// We have to determine the first entry (cannot fail)
				curE = sparr.findFiLa(forw, signed, null);
				state |= (ST_STARTED | ST_HASCURR);
			} else {
				// ... and we have a valid "curE.inx" ...
				// We are going to incr/decr the index:
				// but is there such a numerical value left?
				if (curE.inx == lastSearchInx(forw, signed)) {
					curE.val = null;
					state &= ST_HASCURR;
					return false;
				}
				final long nextinx = curE.inx + (forw ? +1 : -1);
				if (null == sparr.findGeLe(nextinx, forw, signed, curE)) {
					curE.val = null;
					state &= ST_HASCURR;
					return false;
				}
				state |= ST_HASCURR;
			}
			return true;
		}
		
		public long curKey() {
			return (null != curE) ? curE.inx : Long.MIN_VALUE;
		}
		
		public V curValue() {
			if (0 == (state & ST_HASCURR)) {
				throw new NoSuchElementException();
			}
			return curE.val;
		}
		
		public boolean hasCurrent() {
			return 0 != (state & ST_HASCURR);
		}
		
		public void remove() {
			//throw new UnsupportedOperationException();
			if (0 != (state & (ST_HASCURR))) {
				throw new IllegalStateException();
			}
			// ST_HASCURR ==> ST_STARTED ==> curE is valid and sparr != null
			
			sparr.wr(curE.inx, null);
			curE.val = null;
			
			// The "remove" operation must not be repeated on this element!
			state &= ~ST_HASCURR;
		}
	}
	
	public Stepper<V> stepper() {
		return new SparrStepper<V>(this);
	}
	
	//========================================================================
	//	Support for the Map interface
	//	...will be completed later.
	
	/**
	 * {@inheritDoc}
	 * <p>
	 * This implementation is based on a redundant element counter,
	 * and hence is fast.
	 */
	public int size() {
		if (elemcount > Integer.MAX_VALUE) {
			return Integer.MAX_VALUE;
		}
		return (int) elemcount;
	}
	
	/**
	 * {@inheritDoc }
	 * <p>
	 * This implementation just checks the key to be a {@code Long},
	 * and then tells whether {@link #rd(long)} returns non-{@code null}.
	 */
	public boolean containsKey(Object key) {
		if (null == key) {
			//throw new NullPointerException();
			return false;
		}
		if ( ! (key instanceof Long) ) {
			//throw new ClassCastException();
			return false;
		}
		return null != rd( ((Long) key).longValue() );
	}
	
	public V get(Object key) {
		if (null == key) {
			//throw new NullPointerException();
			return null;
		}
		if ( ! (key instanceof Long) ) {
			//throw new ClassCastException();
			return null;
		}
		
		return rd( ((Long) key).longValue() );
	}
	
	public V put(Long key, V value) {
		if (null == key) {
			//throw new NullPointerException();
			return null;
		}
		if ( ! (key instanceof Long) ) {
			//throw new ClassCastException();
			return null;
		}
		
//		if (null == value) {
//			throw new NullPointerException();
//		}
		
		return wr(((Long) key).longValue(), value);
	}
	
	public V remove(Object key) {
		if (null == key) {
			//throw new NullPointerException();
			return null;
		}
		if ( ! (key instanceof Long) ) {
			//throw new ClassCastException();
			return null;
		}
		
		return wr(((Long) key).longValue(), null);
	}
	
	public void putAll(Map<? extends Long, ? extends V> m) {
		if (m == this) {
			// Putting a map into itself cannot really change the map
			return;
		}
		for (Map.Entry<? extends Long, ? extends V> mape : m.entrySet()) {
			put(mape.getKey(), mape.getValue());
		}
	}
	
	// Map: + Map();
	// Map: - Map(Map<? extends Long, ? extends V> m);
	// Map: + int size();
	// Map: + boolean isEmpty();
	// Map: + boolean containsKey(Object key);
	// Map: - boolean containsValue(Object value);
	// Map: + V get(Object key);
	// Map: + V put(Long key, V value);
	// Map: + V remove(Object key);
	// Map: + void putAll(Map<? extends Long, ? extends V> m);
	// Map: + void clear();
	// Map: - Set<Long> keySet();
	// Map: - Collection<V> values();
	// Map: - Set<Map.Entry<Long, V>> entrySet();
	// Map: - boolean equals(Object o);
	// Map: - int hashCode();
	
//	public boolean containsValue(Object value) {
//		return false;
//	}
	
	// AbstractMap implements:
	//  int size()
	//  boolean isEmpty()
	//  boolean containsValue(Object value)
	//  boolean containsKey(Object key)
	//  V get(Object key)
	//  V remove(Object key)
	//  void putAll(Map<? extends K, ? extends V> m)
	//  void clear()
	//  Set<K> keySet()
	//  Collection<V> values()
	//  boolean equals(Object o)
	//  int hashCode()
	//  String toString()
	// but not all of them are really useful for us.
	// Also, we do NOT want to extend AbstractMap.
	
	// SortedMap: + K firstKey();
	
	/**
	 * Determines the first key associated with a non-{@code null} value.
	 * Sorting order is native: signed.
	 * @return the first key in the sparse array
	 * @see SortedMap#firstKey()
	 */
	public long firstInx() {
		if (isEmpty()) {
			throw new NoSuchElementException();
		}
		ArrEntry<V> e = findFiLa(true, true, null);
		return e.inx;
	}

	/**
	 * Determines the last key associated with a non-{@code null} value.
	 * Sorting order is native: signed.
	 * @return the last key in the sparse array
	 * @see SortedMap#lastKey()
	 */
	public long lastInx() {
		if (isEmpty()) {
			throw new NoSuchElementException();
		}
		ArrEntry<V> e = findFiLa(false, true, null);
		return e.inx;
	}

	public Long firstKey() {
		return new Long( firstInx() );
	}
	
	public Long lastKey() {
		return new Long( lastInx() );
	}

	/**
	 * When seen as a map this sparse array uses plain {@code long}s as key.
	 * While the internals interpret them unsigned, the map interface
	 * presents them in a standard signed manner.
	 * 
	 * @return {@code null} for the native comparator for {@code Long}
	 * @see SortedMap#comparator()
	 */
	Comparator<? super Long> comparator() {
		return null;
	}
}
