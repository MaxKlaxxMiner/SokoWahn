/**
 *  JSoko - A Java implementation of the game of Sokoban
 *  Copyright (c) 2013 by Matthias Meger, Germany
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
package de.sokoban_online.jsoko.optimizer.AllMetricsOptimizer;


import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicIntegerArray;
import java.util.concurrent.atomic.AtomicLong;
import java.util.concurrent.atomic.AtomicReferenceArray;

import de.sokoban_online.jsoko.board.DirectionConstants;
import de.sokoban_online.jsoko.resourceHandling.Settings.SearchDirection;
import de.sokoban_online.jsoko.utilities.Debug;

/**
 * Storage for board positions reached while optimizing a solution in the optimizer.
 * <p>
 * This storage is implementing as hash table of dynamic size. As long as there is
 * more available RAM the hash table will be able to store more data.
 * <br>
 * This hash table only allows adding and modifying of board positions.
 * A board position can never be removed from this storage!
 * A board position is regarded equal to another regarding the following values:
 * box configuration index, player position AND search direction.
 */
public class BoardPositionsStorage implements DirectionConstants {

	/** Number of ints used to store the data for one board position. */
	private final int INTS_PER_BOARD_POSITION;

	/** Offsets the different data of a board position are stored at. */
	static final int PLAYER_POSITION_OFFSET   = 0;
	static final int PRIMARY_METRIC_OFFSET    = 1;
	static final int SECONDARY_METRIC_OFFSET  = 2;

	/** The player position is stored in the lower 15 bits of an int. This bit mask is used to extract the player position from the int. */
	static final int PLAYER_POSITION_BIT_MASK 		 = (1 << 16) - 1;

	/** Bit mask for the bit the search direction is stored in. */
	static final int FLAG_SEARCH_DIRECTION_BACKWARDS = 1 << 16;

	/** Bit mask for extracting the player position and the search direction. */
	static final int PLAYER_POSITION__PLUS__SEARCH_DIRECTION_BIT_MASK = (1<<17) - 1;

	/** Bit mask for the bit the processed status is stored in. */
	static final int PROCESSED_FLAG_BIT_MASK		 = 1 << 30;

	/** Bit mask for the flag which represents the "locked for change" status of a slot in the table. */
	static final int LOCKED = 1 << 29;

	/** Indicating "no board position" - MIN_VALUE is an invalid array index and can therefore never occur as normal index although negative indices are returned in some methods. */
	static public final int NONE = Integer.MIN_VALUE;

	/** Representing an empty slot in the table where a board position can be stored in. */
	static final int EMPTY = 0;

	/** The table holding all stored data. */
	protected final AtomicReferenceArray<BoardPositionsArray> table;

	/** The total number of board positions in this hash table. */
	protected final AtomicInteger count = new AtomicInteger();

	// DEBUG MODUS ONLY: variables used for statistics.
	public static class Statistics {
		public final AtomicInteger maxCollisions = new AtomicInteger();
		public final AtomicLong totalCollisions  = new AtomicLong();
		public final AtomicInteger betterRevisitedCount = new AtomicInteger();
		public final AtomicInteger totalRevisitedCount  = new AtomicInteger();
		public final AtomicLong    time = new AtomicLong();
		public int highestBoardPositionsPerSlotCount   = 0;
		public float averageBoardPositionsPerSlotCount = 0;
		public int emptySlotsCount = 0;
	}
	final Statistics statistics = new Statistics();

	/** Initial number of board positions per slot (the hash table consists of "BoardPositionsArray"s which are stored one per slot). */
	final int positionsPerSlot;

	/** This constant holds the maximum value the secondary metric can be and is used to calculate the order value. */
	final int maxSecondaryMetric_plus_1;

	/**
	 * Creates a storage for storing board positions reached by the optimizer.
	 * <p>
	 * The storage is implemented as a hash table.<br>
	 * The maximum number of board positions to be stored only depends on the available RAM.
	 *
	 * @param boxConfigurationsCount  number of different box configurations that the optimizer has generated
	 * @param positionsPerSlot        number of board positions to be stored per slot (one slot is one array containing all board positions having the same box configuration)
	 * @param maxSecondaryMetric 	  maximum value of the secondary metric
	 */
	public BoardPositionsStorage(int boxConfigurationsCount, int positionsPerSlot, int maxSecondaryMetric) {

		// Throw exception when the parameters have invalid values.
		if (boxConfigurationsCount < 1 || positionsPerSlot <= 0 || maxSecondaryMetric <= 0) {
			throw new IllegalArgumentException();
		}

		// Set the factor used for storing the primary metric.
		this.maxSecondaryMetric_plus_1 = maxSecondaryMetric + 1;

		// Set the size depending on how high the metrics may get.
		INTS_PER_BOARD_POSITION = getStorageSizePerBoardPositionInInts(maxSecondaryMetric);

		// Save initial number of board positions per slot.
		this.positionsPerSlot = positionsPerSlot;

		// Create the main table for the board positions to be stored. At least one slot per boxConfiguration (see getHashIndex()).
		table = new AtomicReferenceArray<BoardPositionsArray>(boxConfigurationsCount);
	}

	/**
	 * Returns the number of bytes this storage uses for storing the data of one board position.
	 * <p>
	 * The size depends one the maximal value of the secondary metric.
	 *
	 * @param maxSecondaryMetric  maximum value of the secondary metric
	 * @return RAM usage for storing one board position in bytes
	 */
	public static int getStorageSizePerBoardPositionInInts(int maxSecondaryMetric) {
		return maxSecondaryMetric < 65536 ? BoardPositionsArraySmallMetrics.INTS_PER_BOARD_POSITION : BoardPositionsArray.INTS_PER_BOARD_POSITION;
	}

	/**
	 * Returns the number of board positions stored in this storage.
	 *
	 * @return the number of board positions stored in this storage
	 */
	public int getNumberOfStoredBoardPositions() {
		return count.get();
	}

	/**
	 * Stores the passed board position in this storage.
	 * <p>
	 * If an equal board position is already stored the new one is only stored if it is "better" regarding the metrics.<br>
	 * Note: A board position can never be removed from this storage.
	 * If the board position is already stored for the other search direction the new board position is stored nevertheless.
	 * However: In this case a negative index is returned to indicate that the board position is also stored for the other
	 * search direction.
	 * This means this method can return negative indices!
	 *
	 * @param primaryMetric  		 primary metric to be stored
	 * @param secondaryMetric        secondary metric to be stored
	 * @param boxConfigurationIndex  index the box configuration is stored at in the box configuration storage
	 * @param playerPosition         player position of the board position
	 * @param searchDirection		 direction of the search that has reached the board position
	 *
	 * @return index of the stored board position in this storage or {@link #NONE} if it hasn't been stored.
	 * The index is negative when the board position has already been stored by the other search direction.
	 */
	public long addIfBetter(int primaryMetric, int secondaryMetric, int boxConfigurationIndex, int playerPosition, SearchDirection searchDirection) {

		long time1 = System.nanoTime();
		int collisionsCount = 0;

		int indexToStore = getHashIndex(boxConfigurationIndex, playerPosition);

		// Get the array containing the board positions containing the passed box configuration.
		// If there isn't one already then create a board position array.
		BoardPositionsArray boardPositions = table.get(indexToStore);
		if (boardPositions == null) {
			boardPositions = BoardPositionsArray.getInstance(INTS_PER_BOARD_POSITION, positionsPerSlot * INTS_PER_BOARD_POSITION);
			if (table.compareAndSet(indexToStore, null, boardPositions) == false) {

				// Use the created array for another slot in order not to create garbage for the garbage collector.
				for(int i=indexToStore+1; i<table.length() && !table.compareAndSet(i, null, boardPositions); i++) {
					;
				}

				// Another thread has already set an array => get the array from the table.
				boardPositions = table.get(indexToStore);
			}
		}

		boolean isBoardPositionAlreadyStoredForOtherSearchDirection = false;

		// Index in the board position array where the data of a board position is located.
		int arrayIndex = 0;

		// Search for a free slot for the new board position. If one has been found lock it for storing the data.
		while (boardPositions.compareAndSet(arrayIndex, EMPTY, LOCKED) == false) {

			// The current slot is not empty.

			// Wait until the board position isn't locked anymore. Once the lock is released all data of the board position
			// have been saved. The player position and search direction can't change anymore.
			// Hence, it doesn't matter if the slot may be locked again after this while loop.
			// If the slot has been locked this might be because the array is replaced by a larger one. Therefore get a fresh reference to the array every time.
			while (boardPositions.isLocked(arrayIndex)) {
				boardPositions = table.get(indexToStore);
			}

			// Check whether the new board position is the same as the already stored one regarding: box configuration index (implicit compare) and player position.
			if (boardPositions.getPlayerPosition(arrayIndex) == playerPosition) {

				// If the board position has been saved for the same search direction we can just adjust the metrics if they are better now.
				if(boardPositions.getSearchDirection(arrayIndex) == searchDirection) {

					// Wait until the slot could be locked.
					int unlockedData = boardPositions.get(arrayIndex)&(~LOCKED);
					while (boardPositions.compareAndSet(arrayIndex, unlockedData, unlockedData | LOCKED) == false) {
						boardPositions = table.get(indexToStore);		 // array may have been replaced by a larger one in the meantime
						unlockedData = boardPositions.get(arrayIndex)&(~LOCKED); // the processed flag may have been set in the meantime -> refresh data
					}

					int oldBoardPositionFirstMetric  = boardPositions.getPrimaryMetric(arrayIndex);
					int oldBoardPositionSecondMetric = boardPositions.getSecondaryMetric(arrayIndex);

					// Return NONE if the new board position isn't stored because it isn't better than the old one.
					if (primaryMetric > oldBoardPositionFirstMetric || primaryMetric == oldBoardPositionFirstMetric && secondaryMetric >= oldBoardPositionSecondMetric) {

						// Release the lock by setting back the unlocked data.
						boardPositions.set(arrayIndex, unlockedData);

						if(Debug.isDebugModeActivated) {
							statistics.totalRevisitedCount.incrementAndGet();
							statistics.time.addAndGet(System.nanoTime() - time1);
						}
						return NONE;
					}

					// Store the new metrics of the board position.
					boardPositions.storeMetrics(arrayIndex, primaryMetric, secondaryMetric);

					// Release the lock by setting back the original value.
					// The optimizer checks whether a board position has already been processed. This board position
					// has been reached better and therefore has to be processed again - now using the better metrics.
					// Hence, also set the processed flag to false.
					boardPositions.set(arrayIndex, unlockedData & (~PROCESSED_FLAG_BIT_MASK));

					// Statistics: count the number of better revisited board positions and measure the time.
					if(Debug.isDebugModeActivated) {
						statistics.betterRevisitedCount.incrementAndGet();
						statistics.time.addAndGet(System.nanoTime() - time1);
					}

					// Return the index of the slot the board position has been stored in. Return a negative index
					// when the board position is already stored for the other search direction, too.
					long index = indexToStore | ((long) arrayIndex << 32);
					return isBoardPositionAlreadyStoredForOtherSearchDirection ?  - index : index;
				}

				// The same board position has been reached from both search directions. It must be saved for both directions.
				isBoardPositionAlreadyStoredForOtherSearchDirection = true;

			}

			// Statistics.
			if(Debug.isDebugModeActivated) {
				statistics.totalCollisions.incrementAndGet();
				if (++collisionsCount > statistics.maxCollisions.get()) {
					statistics.maxCollisions.set(collisionsCount);//just statistics -> no cas
				}
			}

			// There is already another (different) board position stored in the current slot.
			// Hence, jump to the next one.
			arrayIndex += INTS_PER_BOARD_POSITION;

			// If the end of the array has been reached a new larger one must be created to store the board position.
			if (arrayIndex == boardPositions.length()) {

				synchronized (boardPositions) {

					// Ensure another thread hasn't already created a larger array.
					if (boardPositions == table.get(indexToStore)) {

						// Create a larger array and copy the data from the old array to the new one.
						BoardPositionsArray boardPositionsNew = BoardPositionsArray.getInstance(INTS_PER_BOARD_POSITION, boardPositions.length() + 1 * INTS_PER_BOARD_POSITION);
						for (int index = 0; index < boardPositions.length(); ) {

							// Wait until slot can be locked.
							int unlockedData = 0;
							do {
								unlockedData = boardPositions.get(index) & (~LOCKED); // processed flag may have been set in the meantime -> refresh unlockedData
							}while (boardPositions.compareAndSet(index, unlockedData, unlockedData | LOCKED) == false) ;

							// Copy data of board position to the new array and set "index" to the index of the next board position.
							boardPositionsNew.set(index, unlockedData); // Copy the data in "unlocked" status
							for (int indexNextBoxConfiguration = index + INTS_PER_BOARD_POSITION; ++index < indexNextBoxConfiguration; ) {
								boardPositionsNew.set(index, boardPositions.get(index));
							}
						}

						// Set the new larger array in the table. This automatically unlocks all slots for any waiting threads.
						table.set(indexToStore, boardPositionsNew);
						boardPositions = boardPositionsNew;
					}
					else {
						// Get the array the other thread has set.
						boardPositions = table.get(indexToStore);
					}
				}
			}
		}

		// An empty slot for storing the board position data has been found and
		// locked for changing it => store the board position data.
		boardPositions.storeMetrics(arrayIndex, primaryMetric, secondaryMetric);

		// Since this int value is also used for locking the slot, it must be changed as last value. This automatically unlocks this slot.
		boardPositions.set(arrayIndex, searchDirection == SearchDirection.BACKWARD ? playerPosition | FLAG_SEARCH_DIRECTION_BACKWARDS : playerPosition);

		// Statistics.
		if(Debug.isDebugModeActivated) {
			count.incrementAndGet();
			statistics.time.addAndGet(System.nanoTime() - time1);
		}

		// Return the index of the slot the board position has been stored in. Return a negative index
		// when the board position is already stored for the other search direction, too.
		long index = indexToStore | ((long) arrayIndex << 32);
		return isBoardPositionAlreadyStoredForOtherSearchDirection ?  - index : index;
	}

	/**
	 * Returns the index of the passed board position in this storage.
	 *
	 * @param boardPosition  board position to return the index for
	 * @param searchDirection direction of the search for which the board position has been saved
	 * @return index of the board position
	 */
	public long getBoardPositionIndex(BoardPosition boardPosition, SearchDirection searchDirection) {
		return getBoardPositionIndex(boardPosition.boxConfigurationIndex, boardPosition.playerPosition, searchDirection);
	}

	/**
	 * Returns the index of the board position corresponding to the passed values.
	 * <p>
	 * This method doesn't lock the storage while searching the board position. Therefore it's
	 * possible that this method returns {@link #NONE} although another thread may have
	 * added the board position in the meantime.
	 *
	 * @param boxConfigurationIndex  box configuration index of the board position to be returned
	 * @param playerPosition   player position of the board position to be returned
	 * @param searchDirection  direction of the search for which the board position has been saved
	 * @return index of the board position or {@link #NONE}
	 */
	public long getBoardPositionIndex(int boxConfigurationIndex, int playerPosition, SearchDirection searchDirection) {

		int index = getHashIndex(boxConfigurationIndex, playerPosition);

		BoardPositionsArray boardPositions = table.get(index);
		if (boardPositions == null) {
			return NONE;
		}

		int arrayIndex = 0;

		// Search the next not empty slot.
		while (boardPositions.get(arrayIndex) != EMPTY) {

			// Wait until the board position isn't locked anymore. Once the lock is released the boxConfigurationIndex and player position have
			// been stored and can't change anymore. Hence, it doesn't matter if the slot may be locked again after this while loop.
			// We just want to get sure that the box configuration index and the player position have been stored properly.
			// Note: another thread may set a larger array in the meantime. Hence, ensure we always used the correct array.
			while (boardPositions.isLocked(arrayIndex)) {
				boardPositions = table.get(index);
			}

			// Return the index when we have found the correct board position (having the correct box configuration, player position and search direction).
			if (boardPositions.getPlayerPosition(arrayIndex)  == playerPosition &&
				boardPositions.getSearchDirection(arrayIndex) == searchDirection) {
				return index | ((long) arrayIndex << 32);
			}

			arrayIndex += INTS_PER_BOARD_POSITION;

			// If we have reached the end of the array we are finished, except
			// the case where a new larger array has been set in the meantime.
			// This is not thread safe, but better than not checking for a new array.
			if (arrayIndex == boardPositions.length()) {

				boardPositions = table.get(index);
				if (arrayIndex == boardPositions.length()) {
					return NONE;
				}
			}
		}

		// The passed board position hasn't been found.
		return NONE;
	}

	/**
	 * Fills the data of the board position stored at the passed index into the passed <code>BoardPosition</code>.
	 *
	 * @param boardPositionIndex  unique index of the board position
	 * @param boardPosition  <code>BoardPosition</code> to be filled with data
	 */
	public void fillBoardPosition(long boardPositionIndex, StorageBoardPosition boardPosition) {

		int tableIndex = (int) boardPositionIndex; // just cast to int
		int arrayIndex = (int) (boardPositionIndex >>> 32);

		BoardPositionsArray boardPositions = table.get(tableIndex);

		long metrics = boardPositions.getMetrics(arrayIndex);
		boardPosition.primaryMetric   = (int) (metrics >>> 32);
		boardPosition.secondaryMetric = (int) metrics;

		// The optimizer only gets an board position index when it has already
		// been stored - and since the box configuration index and the player
		// position can't change anymore, they can be read unsynchronized.
		boardPosition.playerPosition = boardPositions.getPlayerPosition(arrayIndex);
		boardPosition.boxConfigurationIndex = tableIndex - boardPosition.playerPosition;
		while(boardPosition.boxConfigurationIndex < 0) {
			boardPosition.boxConfigurationIndex += table.length();
		}
	}

	/**
	 * Returns a value that can be used to order the board positions according to their metrics.<br>
	 * The lower this value the "better" the board positions metrics are.
	 *
	 * @param boardPositionIndex  unique index of the board position in this storage
	 * @return the order value
	 */
	public long getOrderValueOfBoardPosition(long boardPositionIndex) {

		int tableIndex = (int) boardPositionIndex; // just cast to int
		int arrayIndex = (int) (boardPositionIndex >>> 32);

		BoardPositionsArray boardPositions = table.get(tableIndex);

		long metrics = boardPositions.getMetrics(arrayIndex);
		int primaryMetric   = (int) (metrics >>> 32);
		int secondaryMetric = (int) metrics;

		// Example for pushes/moves optimizing:
		// returns: pushes * (estimated maximum number of moves) + moves
		// Since we don't know the maximum number of moves the optimizer can only estimate that value.
		// If at some time the secondary metric is higher than this constant "maxSecondaryMetric"
		// the board positions wouldn't be ordered correctly, which slows down the optimizer.
		// Convert to long to ensure high values can be stored.
		return secondaryMetric > maxSecondaryMetric_plus_1 ?
			primaryMetric * (long) maxSecondaryMetric_plus_1 + maxSecondaryMetric_plus_1 - 1  :
			primaryMetric * (long) maxSecondaryMetric_plus_1 + secondaryMetric;
	}

	/**
	 * Returns the search direction of the search that has reached the passed board position.
	 *
	 * @param boardPositionIndex  unique index of the board position in this storage
	 * @return the <code>SearchDirection</code> of the search the board position has been reached from
	 */
	public SearchDirection getSearchDirection(long boardPositionIndex) {

		int tableIndex = (int) boardPositionIndex; // just cast to int
		int arrayIndex = (int) (boardPositionIndex >>> 32);

		return table.get(tableIndex).getSearchDirection(arrayIndex);
	}

	/**
	 * Returns whether the passed board position has already been processed by the optimizer search.
	 *
	 * @param boardPositionIndex  unique index of the board position in this storage
	 * @return <code>true</code> if the board position has already been processed; <code>false<code> otherwise
	 */
	public boolean hasBeenProcessed(long boardPositionIndex) {

		int tableIndex = (int) boardPositionIndex; // just cast to int
		int arrayIndex = (int) (boardPositionIndex >>> 32);

		return table.get(tableIndex).hasBeenProcessed(arrayIndex);
	}


	/**
	 * Sets the "is already processed" status of the passed board position.
	 *
	 * @param boardPositionIndex  unique index of the board position in this storage
	 */
	public void markAsProcessed(long boardPositionIndex) {

		int tableIndex = (int) boardPositionIndex; // just cast to int
		int arrayIndex = (int) (boardPositionIndex >>> 32);

		BoardPositionsArray boardPositions;

		// Set the new processed status - but only when the slot isn't locked at the moment.
		int unlockedData = -1;
		do {
			boardPositions = table.get(tableIndex);		 // a new larger array may have been set by another threads
			unlockedData = boardPositions.get(arrayIndex)&(~LOCKED); // the processed flag may have been set in the meantime -> refresh data
		}while((boardPositions.compareAndSet(arrayIndex, unlockedData, unlockedData | PROCESSED_FLAG_BIT_MASK) == false));

	}

	/**
	 * Returns the index in the main table the passed board position (box configuration + player position)
	 * must be stored in.
	 *
	 * @param boxConfigurationIndex  index the box configuration is stored at in the box configuration storage
	 * @param playerPosition         player position of the board position
	 * @return the table index to store the board position
	 */
	private int getHashIndex(int boxConfigurationIndex, int playerPosition) {

		// The box configuration index in unique and therefore a perfect hash key.
		// However, every box configuration can occur with different player positions.
		// Hence, the player position is also used for calculating the index.
		int hash = (boxConfigurationIndex + playerPosition) % table.length();
		while(hash < 0) {
			hash += table.length();
		}
		return hash;
	}

	/**
	 * Returns statistical data about this storage.
	 * <p>
	 * This method is only used for internal usage and not visible to the user nor necessary for the optimizer to work.
	 *
	 * @return statistic data
	 */
	public Statistics getStatistic() {

		long averageBoardPositionsPerSlotCount = 0;
		for(int index=0; index < table.length(); index++) {
			BoardPositionsArray boardPositions = table.get(index);
			if(boardPositions != null) {
				if(boardPositions.length() > statistics.highestBoardPositionsPerSlotCount) {
					statistics.highestBoardPositionsPerSlotCount = boardPositions.length();
				}
				averageBoardPositionsPerSlotCount += boardPositions.length();
			}
			else {
				statistics.emptySlotsCount++;
			}
		}

		statistics.highestBoardPositionsPerSlotCount /= INTS_PER_BOARD_POSITION;
		statistics.averageBoardPositionsPerSlotCount = averageBoardPositionsPerSlotCount / (float) INTS_PER_BOARD_POSITION / table.length();

		return statistics;
	}

	/**
	 * Compares both board positions and returns a negative number if the first passed one is better, - 0 if the are equally good - a positive number if first
	 * one is worse than the second one
	 *
	 * @param boardPosition1  board position to be compared
	 * @param boardPosition2  board position to be compared
	 * @return a negative integer, zero, or a positive integer as this object is less than, equal to, or greater than the specified object.
	 */
	public int compareBoardPositions(long boardPosition1, long boardPosition2) {
		return (int) (getOrderValueOfBoardPosition(boardPosition1) - getOrderValueOfBoardPosition(boardPosition2));
	}


	/**
	 * Class containing all data of a board position for the optimizer.
	 */
	public static class StorageBoardPosition {
		public int boxConfigurationIndex = 0;
		public int playerPosition 		 = 0;
		public int primaryMetric 		 = 0;
		public int secondaryMetric 		 = 0;

		@Override
		public String toString() {
			return "StorageBoardPosition [boxConfigurationIndex=" + boxConfigurationIndex + ", playerPosition=" + playerPosition + ", primaryMetric="
					+ primaryMetric + ", secondaryMetric=" + secondaryMetric + "]";
		}
	}

	/**
	 * Class containing the primary and secondary metrics.
	 * <p>
	 * This class is used when metrics have to be passed thread safe.
	 */
	public static class Metrics {
		public int primaryMetric   = 0;
		public int secondaryMetric = 0;
	}

	/**
	 * Array for storing board positions.<br>
	 * The hash table stores all board positions having a specific box configuration in one {@link #BoardPositionArray}.
	 */
	@SuppressWarnings("serial")
	private static class BoardPositionsArray extends AtomicIntegerArray {

		/** Number of ints used to store the data for one board position. */
		public static final int INTS_PER_BOARD_POSITION = 3;

		/**
		 * Creates an array of the requested length for storing board positions.
		 *
		 * @param length  length of the array
		 */
		public BoardPositionsArray(int length) {
			super(length);
		}

		/**
		 * Returns an array of the requested length for storing board positions.<br>
		 * The array stores board position data using the passed number of ints.
		 *
		 * @param intsPerBoardPosition  number of ints that may be used for storing the data of one board position
		 * @param length  length of the array
		 * @return {@link #BoardPositionsArray} having the requested properties
		 */
		public static BoardPositionsArray getInstance(int intsPerBoardPosition, int length) {
			return intsPerBoardPosition == 2 ? new BoardPositionsArraySmallMetrics(length) : new BoardPositionsArray(length);
		}

		/**
		 * Returns the search direction of the search that has reached the passed board position.
		 *
		 * @param arrayIndex  index of the board position
		 * @return the <code>SearchDirection</code> the board position has been reached from
		 */
		protected SearchDirection getSearchDirection(int arrayIndex) {
			// The search direction is saved together with the player position in one integer.
			return (get(arrayIndex) & FLAG_SEARCH_DIRECTION_BACKWARDS) != 0 ?  SearchDirection.BACKWARD : SearchDirection.FORWARD;
		}

		/**
		 * Returns whether a board position has already been processed by the optimizer search.
		 *
		 * @param arrayIndex  index of the board position
		 * @return <code>true</code> if the board position has already been processed; <code>false<code> otherwise
		 */
		protected boolean hasBeenProcessed(int arrayIndex) {
			// The flag indicating whether this board position has already been processed by the
			// search is is saved together with the player position in one integer.
			return (get(arrayIndex) & PROCESSED_FLAG_BIT_MASK) != 0;
		}


		/**
		 * Returns the metric of the passed board position.
		 * <p>
		 * The metrics are read consistent / thread safe.
		 *
		 * @param arrayIndex  index of the board position
		 * @return one {@code long} containing the primary metric in the 32 high bits and the secondary metric in the 32 low bits.
		 */
		protected long getMetrics(int arrayIndex) {

			// Reading the metrics must be done synchronized because other threads
			// may just have changed only one of the metrics which would result in inconsistent values.

			// Wait until the slot could be locked.
			int unlockedData = get(arrayIndex)&(~LOCKED);
			while (compareAndSet(arrayIndex, unlockedData, unlockedData | LOCKED) == false) {
				unlockedData = get(arrayIndex)&(~LOCKED); // the processed flag may have been set in the meantime -> refresh data
			}

			long primaryMetric  = getPrimaryMetric(arrayIndex);
			int secondaryMetric = getSecondaryMetric(arrayIndex);

			// Unlock the slot.
			set(arrayIndex, unlockedData);

			return (primaryMetric << 32) | secondaryMetric;
		}

		/**
		 * Returns the primary metric of the passed board position.
		 *
		 * @param arrayIndex  index of the board position
		 * @return the primary metric
		 */
		protected int getPrimaryMetric(int arrayIndex) {
			return get(arrayIndex + PRIMARY_METRIC_OFFSET);
		}

		/**
		 * Returns the secondary metric of the passed board position.
		 *
		 * @param arrayIndex  index of the board position
		 * @return the secondary metric
		 */
		protected int getSecondaryMetric(int arrayIndex) {
			return get(arrayIndex + SECONDARY_METRIC_OFFSET);
		}

		/**
		 * Stores the passed metrics for a board position.
		 *
		 * @param arrayIndex       index of the board position the metrics have to be stored in
		 * @param primaryMetric    the primary metric
		 * @param secondaryMetric  the secondary metric
		 */
		protected void storeMetrics(int arrayIndex, int primaryMetric, int secondaryMetric) {
			set(arrayIndex + PRIMARY_METRIC_OFFSET, primaryMetric);
			set(arrayIndex + SECONDARY_METRIC_OFFSET, secondaryMetric);
		}

		/**
		 * Returns whether the board position represented by the passed index is locked for changing it.
		 *
		 * @param arrayIndex  index of the board position
		 * @return <code>true</code> the board position is locked for changing it, <code>false</code> otherwise
		 */
		protected boolean isLocked(int arrayIndex) {
			return (get(arrayIndex) & LOCKED) == LOCKED;
		}

		/**
		 * Returns the player position of a board position.
		 *
		 * @param arrayIndex  index of the board position
		 * @return the player position
		 */
		protected int getPlayerPosition(int arrayIndex) {
			return get(arrayIndex) & PLAYER_POSITION_BIT_MASK;
		}
	}

	/**
	 * Array for storing board positions.<br>
	 * The hash table stores all board positions having a specific box configuration in one {@link #BoardPositionArray}.<br>
	 * This array can be used when the metrics are smaller than 65536 and therefore can both be stored in one integer.
	 */
	@SuppressWarnings("serial")
	private static class BoardPositionsArraySmallMetrics extends BoardPositionsArray {

		/** Number of ints used to store the data for one board position. */
		public static final int INTS_PER_BOARD_POSITION = 2;

		/** Offset the metrics are stored at. */
		static final int METRIC_OFFSET = 1;

		/**
		 * Creates an array of the requested length for storing board positions.
		 *
		 * @param length  length of the array
		 */
		public BoardPositionsArraySmallMetrics(int length) {
			super(length);
		}

		/**
		 * Returns the metric of the passed board position.
		 * <p>
		 * The metrics are read consistent / thread safe.
		 *
		 * @param boardPositionIndex  index of the board position
		 * @return one {@code long} containing the primary metric in the high bits and the secondary metric in the low bits.
		 */
		@Override
		protected long getMetrics(int boardPositionIndex) {

			int combinedMetric  = get(boardPositionIndex+METRIC_OFFSET);
			int primaryMetric   = combinedMetric >>> 16;
			int secondaryMetric = combinedMetric&65535;

			return ((long) primaryMetric << 32) | secondaryMetric;
		}

		/**
		 * Returns the primary metric of the passed board position.
		 *
		 * @param boardPositionIndex board position to return the primary metric for
		 * @return primary metric
		 */
		@Override
		protected int getPrimaryMetric(int boardPositionIndex) {
			return get(boardPositionIndex+METRIC_OFFSET)>>>16;
		}

		/**
		 * Returns the secondary metric of the passed board position.
		 *
		 * @param boardPositionIndex board position to return the secondary metric for
		 * @return secondary metric
		 */
		@Override
		protected int getSecondaryMetric(int boardPositionIndex) {
			return get(boardPositionIndex+METRIC_OFFSET)&65535;
		}

		/**
		 * Stores the passed metrics in the table.
		 * <p>
		 * This is an extra method in order to offering sub classing this class
		 * and overwriting this method.
		 *
		 * @param index  index of the board position the metrics have to be stored in
		 * @param primaryMetric  the primary metric
		 * @param secondaryMetric  the secondary metric
		 */
		@Override
		protected void storeMetrics(int index, int primaryMetric, int secondaryMetric) {
			set(index+METRIC_OFFSET, (primaryMetric<<16) + secondaryMetric);
		}
	}
}
