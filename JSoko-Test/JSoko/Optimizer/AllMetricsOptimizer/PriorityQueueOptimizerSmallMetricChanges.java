/**
 *  JSoko - A Java implementation of the game of Sokoban
 *  Copyright (c) 2014 by Matthias Meger, Germany
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
import java.util.concurrent.atomic.AtomicLong;
import java.util.concurrent.atomic.AtomicLongArray;

import de.sokoban_online.jsoko.optimizer.dataStructures.LongQueue;


/**
 * A priority queue used in the optimizer.
 *
 * The elements of the priority queue are ordered according to their
 * "order value" which is read from the "boardPositionStorage".
 *
 * <p>The <em>head</em> of this queue is the <em>least</em> board position
 * with respect to the specified ordering value.
 * If multiple elements are tied for least value, the head is one
 * of those elements -- ties are broken arbitrarily.
 */
public class PriorityQueueOptimizerSmallMetricChanges extends PriorityQueueOptimizer {

	/**
	 * Storage where the data of a board position is stored. This queue only
	 * stores the index to the this data.
	 */
	private final BoardPositionsStorage boardPositionStorage;

	/** Each bucket contains board positions having a specific order value. */
	private final LongQueue[] buckets;

	/** The lowest order value that is stored in this queue: top of the queue. */
	private final AtomicLong minimumOrderValue = new AtomicLong(0);

	/** Number of threads that use this queue. */
	private final int threadsCount;

	/** The order values currently processed by the different threads. */
	private final AtomicLongArray currentlyProcessedOrderValues;

	/** The minimum value change of the order value between two calls of the {@link #add(long)} method. */
	private final int minValueChange;

	/** Counter for checking if there are no more board positions to remove. */
	private final AtomicInteger boardPositionsCount = new AtomicInteger();


	/**
	 * Creates a priority queue for storing board positions.
	 * <p>
	 * The board positions are ordered according to their "order value" which
	 * is read from the passed "boardPositionStorage".
	 *
	 * @param minValueChange  the minimum value change of the order value between two calls of the {@link #add(long)} method
	 * @param maxValueChange  the maximum value change of the order value between two calls of the {@link #add(long)} method
	 * @param boardPositionStorage  the storage that holds all reached board positions
	 * @param threadsCount  number of threads that use this queue. It's important that all of these threads repeatedly
	 * 						call the {@link #removeFirst()} method because that methods waits for all threads having reached
	 * 					    that methods at specific times
	 */
	PriorityQueueOptimizerSmallMetricChanges(final int minValueChange, final int maxValueChange, final BoardPositionsStorage boardPositionStorage, final int threadsCount) {

		this.boardPositionStorage = boardPositionStorage;
		this.minValueChange = minValueChange;
		this.threadsCount   = threadsCount;
		currentlyProcessedOrderValues = new AtomicLongArray(threadsCount);
		boardPositionsCount.set(threadsCount);

		// Create the queues for the buckets for storing board positions.
		// +minValueChange because other threads can advance ahead "minValueChange" buckets to remove board positions.
		buckets = new LongQueue[maxValueChange+minValueChange+1];
		for(int i=0; i<buckets.length; i++) {
			buckets[i] = new LongQueue(200);
		}

	}


	/**
	 * This queue stores every order value in a specific bucket (bucket-based priority queue):
	 * buckets[0]  contains the board position having a order value of 0
	 * buckets[1]  contains the board position having a order value of 1
	 * buckets[2]  contains the board position having a order value of 3
	 * buckets[3]  contains the board position having a order value of 4
	 *
	 * All threads remove board positions from bucket 0 (minimumOrderValue = 0).
	 * When all board positions have been removed from bucket 0 (it is empty) minimumOrderValue is increased to 1.
	 * Then all threads remove board positions from bucket[1].
	 * Since bucket[0] is then used to store board positions having an order value of 5.
	 *
	 * currentlyProcessedOrderValues contains the order values each thread is currently processing.
	 */


	/**
	 * Adds the passed board position index to this queue.
	 * <p>
	 * The board position is inserted according to its order value.
	 *
	 * @param boardPositionIndex  board position index to be stored
	 */
	@Override
	public void add(long boardPositionIndex) {

		// All board positions are ordered by their "orderValue" in this queue.
		long orderValue = boardPositionStorage.getOrderValueOfBoardPosition(boardPositionIndex);

		// Add the board position into the bucket corresponding to the calculated order value.
		int bucketIndex = (int) (orderValue % buckets.length);
		buckets[bucketIndex].add(boardPositionIndex);

		boardPositionsCount.incrementAndGet();
	}

	/**
	 * Returns the index of the board position having the lowest order value.
	 *
	 * @return index of the board position having the lowest order value or {@value #NONE} if this queue is empty.
	 */
	@Override
	public long removeFirst(final int currentThreadID) {

		long currentOrderValue = minimumOrderValue.get();
		int bucketIndex = (int) (currentOrderValue % buckets.length);

		// This thread will remove a board position -> decrease the counter.
		// Note: if the counter becomes 0 this means: ALL threads are in this method
		// but there is no more board position to remove.
		boardPositionsCount.decrementAndGet();

		// Return the board position having the lowest order value.
		while(boardPositionsCount.get() > 0) {

			// A higher order value (may) have been reached -> set the new value.
			currentlyProcessedOrderValues.set(currentThreadID, currentOrderValue);

			long boardPositionIndex = 0;
			while((boardPositionIndex = buckets[bucketIndex].remove()) != NONE) {
				if(boardPositionStorage.hasBeenProcessed(boardPositionIndex) == false) {
					return boardPositionIndex;
				}
				boardPositionsCount.decrementAndGet();
			}

			// Calculate the lowest order value a thread is currently processing.
			long lowestRemovedOrderValue = currentOrderValue;
			for(int threadID=0; threadID < threadsCount; threadID++) {
				if(threadID != currentThreadID) {
					lowestRemovedOrderValue = Math.min(lowestRemovedOrderValue, currentlyProcessedOrderValues.get(threadID));
				}
			}

			// An empty bucket has been found. Check whether any of the threads can still generate board positions that
			// may be added to this bucket. If not, then we can also increase "minimumOrderValue".
			if(currentOrderValue < lowestRemovedOrderValue + minValueChange) {

				// The new reached order value is the new minimum value for this priority queue.
				if(minimumOrderValue.compareAndSet(currentOrderValue, currentOrderValue+1)) {
					// The current bucket is empty. We can jump to the next one.
					currentOrderValue++;
					if(++bucketIndex == buckets.length) {
						bucketIndex = 0;
					}
				} else {
					currentOrderValue = minimumOrderValue.get();
					bucketIndex = (int) (currentOrderValue % buckets.length);
				}
			}
			else {
				if(Thread.currentThread().isInterrupted()) {
					break;
				}
			}
		}

		return NONE;
	}
}