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
package de.sokoban_online.jsoko.optimizer.AllMetricsOptimizer;

import java.util.concurrent.BrokenBarrierException;
import java.util.concurrent.CyclicBarrier;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;
import java.util.concurrent.atomic.AtomicInteger;

import de.sokoban_online.jsoko.optimizer.dataStructures.LongQueue;
import de.sokoban_online.jsoko.utilities.Debug;




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
public class PriorityQueueOptimizerLargeMetrics extends PriorityQueueOptimizer {

	/**
	 * Storage where the data of a board position is stored. This queue only
	 * stores the index to the this data.
	 */
	protected final BoardPositionsStorage boardPositionStorage;

	/** Low level buckets: each bucket contains board positions having a specific order value. */
	private final LongQueue[] lowLevelBuckets;

	/** High level buckets: each bucket contains board positions having an order value in a specific range. */
	private final LongQueue[] highLevelBuckets;

	/** Number of high level buckets. */
	private final int highLevelBucketsCount;

	/** Number of low level buckets. */
	private final int lowLevelBucketsCount;

	/** Number of different order values per high level bucket. */
	private final int valuesPerHighLevelBucket;

	/** Highest order value that can be stored in a low level bucket. */
	private volatile long highestLowLevelValue  = 0;

	/** Lowest order value that can be stored in a low level bucket. */
	private volatile long lowestLowLevelValue   = 0;

	/** Highest order value that can be stored in a high level bucket. */
	private volatile long highestHighLevelValue = 0;

	private volatile LongQueue bucketToBeDistributed;

	/** Barrier used for waiting for all threads having reached this barrier and then distributes a high level bucket. */
	private final CyclicBarrier highLevelBucketDistributionBarrier;

	/** The index of the first not empty low bucket.*/
	private final AtomicInteger firstNotEmptyLowLevelBucketIndex = new AtomicInteger();

	/** The minimum value change of the order value between two calls of the {@link #add(long)} method. */
	private final int minValueChange;


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
	PriorityQueueOptimizerLargeMetrics(final int minValueChange, final int maxValueChange, final BoardPositionsStorage boardPositionStorage, final int threadsCount) {

		this.boardPositionStorage = boardPositionStorage;
		this.minValueChange = minValueChange;

		// Create barrier all threads are synchronized on.
		highLevelBucketDistributionBarrier = new CyclicBarrier(threadsCount, getBarrierRunnable());

		// Calculate the number of low- and high level buckets.
		highLevelBucketsCount    = (int) Math.ceil(Math.sqrt(maxValueChange+1));
		valuesPerHighLevelBucket = (int) Math.ceil( (double) maxValueChange/highLevelBucketsCount);
		lowLevelBucketsCount     = valuesPerHighLevelBucket;

		// Create the queues for the high level buckets for storing board positions.
		highLevelBuckets = new LongQueue[highLevelBucketsCount];
		for(int i=0; i<highLevelBucketsCount; i++) {
			highLevelBuckets[i] = new LongQueue(200);
		}

		// Create the queues for the low level buckets for storing board positions.
		lowLevelBuckets = new LongQueue[lowLevelBucketsCount];
		for(int i=0; i<lowLevelBucketsCount; i++) {
			lowLevelBuckets[i]  = new LongQueue(100);
		}

		// At start the board positions of the first high level bucket are distributed to the low level buckets.
		// Hence, values between 0 and valuesPerHighLevelBucket - 1 must be stored in the low level buckets.
		highestLowLevelValue = valuesPerHighLevelBucket - 1;
		lowestLowLevelValue  = 0;

		// We have "highLevelBucketsCount" high level buckets and which each can store "valuesPerHighLevelBucket"
		// values and low level buckets which also can store "valuesPerHighLevelBucket" values.
		// Therefore, in total we can store:
		// (highLevelBucketsCount + 1) * valuesPerHighLevelBucket values.
		// Since the lowest value that can be stored is 0 the highest value is one less than the total number of values that can be stored.
		highestHighLevelValue = (highLevelBucketsCount + 1 ) * valuesPerHighLevelBucket - 1;
	}

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

		// Add the board position to a low level bucket if the order value is low enough.
		if(orderValue <= highestLowLevelValue ) {

			if(Debug.isDebugModeActivated && orderValue < lowestLowLevelValue) {
				System.out.println("order: "+orderValue+ " but lowest possible is: "+lowestLowLevelValue);
			}

			// Each bucket holds board positions having a specific order value.
			// The higher the order value the higher the bucket.
			int lowLevelBucketIndex = (int) (orderValue - lowestLowLevelValue);
			lowLevelBuckets[lowLevelBucketIndex].add(boardPositionIndex);
		}
		else {
			// The optimizer can only guess the max value change that can occur.
			// If the change has been higher than "maxValueChange" the orderValue is too high to store.
			// However, the optimizer can handle the case when the board positions aren't always ordered correctly in this
			// priority queue. Hence all board positions having a too high order value are stored in the highest bucket.
			if(orderValue > highestHighLevelValue) {

				if(Debug.isDebugModeActivated) {
					System.out.println("order value  "+orderValue+" is too high -> set to highest possible: "+highestHighLevelValue);
				}

				orderValue = highestHighLevelValue;
			}

			// Each high level bucket contains "valuesPerHighLevelBucket" different order values.
			// Hence, we get the correct high level bucket index by dividing by "valuesPerHighLevelBucket".
			// If the value is too high, we wrap around. The "wrap around" can be done because its ensured
			// that the order value isn't higher than "highestHighLevelValue".
			int highLevelBucketIndex = (int) ((orderValue/valuesPerHighLevelBucket)%highLevelBucketsCount);
			highLevelBuckets[highLevelBucketIndex].add(boardPositionIndex);
		}

	}

	/**
	 * Returns the index of the board position having the lowest order value.
	 *
	 * @param currentThreadID unused in this method
	 * @return index of the board position having the lowest order value or {@value #NONE} if this queue is empty.
	 */
	@Override
	public long removeFirst(int currentThreadID) {

		// Return the board position having the lowest order value.
		for(int index=firstNotEmptyLowLevelBucketIndex.get(); index < lowLevelBuckets.length; index++) {

			long boardPositionIndex = 0;
			while((boardPositionIndex = lowLevelBuckets[index].remove()) != NONE) {

				if(Debug.isDebugModeActivated) {
					long orderValue = boardPositionStorage.getOrderValueOfBoardPosition(boardPositionIndex);
					if(orderValue < lowestLowLevelValue) {
						System.out.println("Failure in remove method: orderValue: "+orderValue+", but lowest: "+lowestLowLevelValue);
					}
				}

				// If the board position has already been processed by the optimizer search we can discard it.
				// This happens when a board position is reached again by the optimizer but this time with better metrics.
				// Then the board position is stored twice in this storage and the first one is processed and sets the flag
				// for both board positions to "processed" in the storage.
				if(boardPositionStorage.hasBeenProcessed(boardPositionIndex) == false) {
					return boardPositionIndex;
				}
			}

			// The bucket is empty. All further calls to this method can start at the next bucket immediately.
			// We have to check that no thread can add any further board positions to the current bucket!
			// Even if there is still a "slow running" thread that has taken a board position out of bucket 0,
			// then the first index the successor board positions can be added to is: 0 + minValueChange.
			if(minValueChange >= index) {
				firstNotEmptyLowLevelBucketIndex.compareAndSet(index, index + 1); // ensure only one thread increases the index
			}
		}

		// All low-level buckets are empty => distribute the board positions of the next not empty high-level bucket to the low level buckets.
		// However, there still might be threads which add new board positions to this queue. We therefore have to wait until all
		// threads have reached this barrier before the next high level bucket can be distributed.
		// The last thread that arrives determines the next high level bucket to expand by automatically
		// calling the barrier runnable (see method: getBarrierRunnable). The bucket index is set in "bucketToBeDistributed".
		try {
			// Wait until all threads have arrived. 10 seconds should never be reached.
			highLevelBucketDistributionBarrier.await(10l, TimeUnit.SECONDS);
		} catch (TimeoutException e) { return NONE; }		 							       // When any exception occurs this
		  catch (InterruptedException e) { Thread.currentThread().interrupt(); return NONE; }  // search direction can't continue.
		  catch (BrokenBarrierException e) { return NONE; }	 								   // Hence, return NONE to indicate that the search is over.

		// If no next not-empty bucket has been found this queue is empty.
		// => return NONE to indicate that the queue is empty.
		if(bucketToBeDistributed == null) {
			return NONE;
		}

		// All board positions of the high level bucket to be distributed are removed and added to the low level bucket corresponding to its order value.
		for(long boardPositionIndex = bucketToBeDistributed.remove(); boardPositionIndex != NONE; boardPositionIndex = bucketToBeDistributed.remove()) {

			// The board positions have been added to this queue according to their orderValue. However, when a board position is
			// reached by the optimizer search with a better orderValue -  which may happen - its orderValue decreases.
			// In this case the "board position" is added to the queue again at the correct position according to its orderValue.
			// The one having the lower order value may have already been processed by the search. In this case its marked as "processed".
			// Since this queue only holds references to board positions, both board positions are therefore marked as processed in the storage!
			// The second one can be discarded, since there is no need to process a board position more than once. It's a simple duplicate board
			// position due to this queue not using a "decrease-key" operation but inserting board positions multiple times if they are reached better again.
			if(boardPositionStorage.hasBeenProcessed(boardPositionIndex)) {
				continue;
			}

			long orderValue = boardPositionStorage.getOrderValueOfBoardPosition(boardPositionIndex);

			// The orderValue may be higher than the bucket range of the high level bucket really is. (see method "add" for details).
			// In that case board position is added to the last bucket.
			if(orderValue > highestLowLevelValue) {
				lowLevelBuckets[lowLevelBuckets.length-1].add(boardPositionIndex);
			}
			else {
				if(orderValue < lowestLowLevelValue) {
					if(Debug.isDebugModeActivated) {
						System.out.println("Failure: order value is: "+orderValue+", but lowest possible is: "+lowestLowLevelValue);
					}
					orderValue = lowestLowLevelValue;
				}

				// Add the board position to the low level bucket according to its order value.
				int lowLevelBucketIndex = (int) (orderValue - lowestLowLevelValue);
				lowLevelBuckets[lowLevelBucketIndex].add(boardPositionIndex);
			}
		}

		// The low level buckets have been filled again. Every thread that reaches this coding line must set the index back to 0 so
		// the next "removeFirst" call searches for board positions at index 0 again. Otherwise the other threads may already increase this value
		// and then later a thread that is still in the above for-loop adds a board position to the bucket having index 0.
		firstNotEmptyLowLevelBucketIndex.set(0);

		// The low level buckets have been filled again. Now return the first board position.
		return removeFirst(currentThreadID);
	}

	/**
	 * Returns the <code>Runnable</code> that is to be executed when all threads have arrived at the barrier
	 * and a new high level bucket is to be distributed to the low level buckets.
	 * <p>
	 * The <code>Runnable</code> is automatically executed when the last thread arrived at the barrier.
	 *
	 * @return the <code>Runnable</code> to be executed when the last thread has arrived at the barrier
	 */
	private Runnable getBarrierRunnable() {

		return new Runnable() {

			/** Index of the high level bucket to expand next. */
			private int highLevelBucketToExpandIndex = 0;

			@Override
			public void run() {

				// If no next high level bucket is found which isn't empty,
				// then null must be returned.
				bucketToBeDistributed = null;

				// Search for the next not-empty high level bucket.
				for(int emptyHighLevelBucketsCount = 0; emptyHighLevelBucketsCount != highLevelBuckets.length; emptyHighLevelBucketsCount++) {

					// Jump to the next high level bucket because the current is empty.
					if(++highLevelBucketToExpandIndex == highLevelBuckets.length) {
						highLevelBucketToExpandIndex = 0;
					}

					LongQueue highLevelBucket = highLevelBuckets[highLevelBucketToExpandIndex];

					// For every new high level bucket the values have to be adjusted.
					lowestLowLevelValue   += valuesPerHighLevelBucket;
					highestLowLevelValue  += valuesPerHighLevelBucket;
					highestHighLevelValue += valuesPerHighLevelBucket;

					if(!highLevelBucket.isEmpty()) {
						bucketToBeDistributed = highLevelBucket;
						break;
					}
				}
			}
		};
	}

}