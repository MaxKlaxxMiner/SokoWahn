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
package de.sokoban_online.jsoko;

import de.sokoban_online.jsoko.boardpositions.AbsoluteBoardPosition;
import de.sokoban_online.jsoko.boardpositions.BoardPosition;
import de.sokoban_online.jsoko.boardpositions.CorralBoardPosition;
import de.sokoban_online.jsoko.boardpositions.IBoardPosition;
import de.sokoban_online.jsoko.boardpositions.RelativeBoardPosition;


/**
 * Storage for board positions.
 *
 * A hash table is used to store the board positions.
 * Board positions with the same hash value are stored in a linked list
 * in the same slot of the hash table.
 */
public final class PositionStorage {

	// The table holding all stored board positions.
	private Entry table[];

	// The total number of board positions in the hash table.
	private int count;

	/**
	 * Creates an object for storing board positions in a hash table.
	 *
	 * @param initialCapacity	the initial capacity of this hash table.
	 */
	public PositionStorage(int initialCapacity) {
		// Create an array for the board positions to be stored.
		table = new Entry[initialCapacity];
	}

	/**
	 * Returns the stored board position that is equivalent to the passed board position.
	 *
	 * @param boardPositionToBeCompared  board position which is testet of already being in the storage
	 * @return board position that is equivalent to the passed board position and already stored in the storage
	 */
	final public IBoardPosition getBoardPosition(IBoardPosition boardPositionToBeCompared) {

		// Get the hashcode of the board position
		int hash = boardPositionToBeCompared.hashCode();

		// Get the position the board position would have been stored at.
		int index = (hash & 0x7FFFFFFF) % table.length;

		// Loop over all entrys that have the correct hash value until the correct board position is found.
		for (Entry entry = table[index]; entry != null; entry = entry.next) {
			if (entry.boardPosition.hashCode() == hash
					&& ((BoardPosition) entry.boardPosition)
							.equals(boardPositionToBeCompared)) {

				return entry.boardPosition;
			}
		}

		// There isn't an equivalent board position in the hash table, hence return null.
		return null;
	}

	/**
	 * Returns the number of board positions in this hash table.
	 *
	 * @return  the number of board positions in this hash table.
	 */
	final public int getNumberOfStoredBoardPositions() {
		return count;
	}

	/**
	 * Returns whether the hash table contains a board position equivalent to the passed one.
	 *
	 * @param   boardPosition   board position to be checked for already stored
	 * @return  <code>true</code> if an equivalent board position is stored
	 *          <code>false</code> otherwise
	 */
	final public boolean containsBoardPosition(Object boardPosition) {

		// Get the hashcode of the board position
		int hash = boardPosition.hashCode();

		// Get the position the board position would have been stored at.
		int index = (hash & 0x7FFFFFFF) % table.length;

		// Loop over all entrys that have the correct hashvalue until the correct board position is found.
		for (Entry entry = table[index]; entry != null; entry = entry.next) {
			if ((entry.boardPosition.hashCode() == hash)
					&& ((BoardPosition) entry.boardPosition)
							.equals(boardPosition)) {
				return true;
			}
		}
		return false;
	}

	/**
	 * Clears the storage so that it contains no board positions anymore.
	 */
	final public void clear() {
		// This code is similar to hash table.clear()
		Entry tab[] = table;

		for (int index = tab.length; --index >= 0;) {
			tab[index] = null;
		}
		count = 0;
	}

	/**
	 * Stores the passed board position in the hash table.
	 *
	 * @param      boardPosition  the board position to be stored
	 * @return     the equivalent board position that has been replaced by the passed one.
	 */
	final public IBoardPosition storeBoardPosition(IBoardPosition boardPosition) {

		// Get the hashcode of the board position
		int hash = boardPosition.hashCode();

		// Calculate the position the board position is to be stored.
		int index = (hash & 0x7FFFFFFF) % table.length;

		// If there is already an entry for the passed board position replace the old
		// board position with the passed one and return the old board position.
		for (Entry entry = table[index]; entry != null; entry = entry.next) {
			if (entry.boardPosition.hashCode() == hash
					&& ((BoardPosition) entry.boardPosition)
							.equals(boardPosition)) {
				IBoardPosition old = entry.boardPosition;
				entry.boardPosition = boardPosition;

				return old;
			}
		}

		// Create a new entry for the passed board position and store it in the hash table.
		table[index] = new Entry(boardPosition, table[index]);
		count++;

		return null;
	}

	/**
	 * All objects stored in the hash table are instances of this class.
	 *
	 * This object holds the hash value of the stored board position and a reference to
	 * the next board position having the same hash value.
	 * We use that to store board positions with the same hash value
	 * in a linked list.
	 */
	private class Entry {

		// The stored board position.
		IBoardPosition boardPosition;

		// The reference to the next board position that has the same hash value.
		Entry next;

		protected Entry(IBoardPosition boardPosition, Entry next) {
			this.boardPosition = boardPosition;
			this.next = next;
		}

		/**
		 * Returns the stored board positions.
		 *
		 * @return	the board position that is stored in this entry.
		 */
		public Object getBoardPosition() {
			return boardPosition;
		}
	}

	/**
	 * Debug method: prints the number of hash collisions in the hash table.
	 */
	public void printStatisticDebug() {

		int[] statistic = new int[1000];

		// Number of corral board positions.
		int corralBoardPositionCount = 0;

		// Number of absolute board positions.
		int absoluteBoardPositionsCount = 0;

		// Number of relative board positions.
		int relativeBoardPositionsCount = 0;

		for (Entry element : table) {
			int counter = 0;

			for (Entry entry = element; entry != null; entry = entry.next, counter++) {
				if (entry.getBoardPosition() instanceof CorralBoardPosition) {
					corralBoardPositionCount++;
				}
				if (entry.getBoardPosition() instanceof AbsoluteBoardPosition) {
					absoluteBoardPositionsCount++;
				}
				if (entry.getBoardPosition() instanceof RelativeBoardPosition) {
					relativeBoardPositionsCount++;
				}

				if (entry.getBoardPosition().hashCode() == 0) {
					System.out.println("Hashvalue of 0!");
				}

				//				if(((BoardPosition) entry.getBoardPosition()).getBoxNo() == 511)
				//					System.out.println("No box pushed"+" Class = "+entry.getBoardPosition().getClass());
			}
			statistic[counter > 999 ? 999 : counter]++;
		}

		System.out.println("\n\nhash table statistics");
		System.out.println("--------------------\n");
		System.out.println("hash table size: " + table.length);
		System.out.println("Number of stored board positions: " + count);
		System.out.println("Number of free hash slots: 		  " + statistic[0]);
		System.out.println("Number of CorralBoardPositions:   " + corralBoardPositionCount +  "  must always be 0!!!");
		System.out.println("Number of AbsoluteBoardPositions: " + absoluteBoardPositionsCount);
		System.out.println("Number of RelativeBoardPositions: " + relativeBoardPositionsCount);
		System.out.println("\nCollisions");
		System.out.println("----------");
		for (int index = 1; index < statistic.length; index++) {
			if (statistic[index] > 0) {
				System.out.println(index + "fold collision: " + statistic[index] + " times");
			}
		}
	}
}