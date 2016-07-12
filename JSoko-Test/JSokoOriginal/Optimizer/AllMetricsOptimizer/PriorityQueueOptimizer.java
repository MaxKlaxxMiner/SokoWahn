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
public abstract class PriorityQueueOptimizer {

		/** Constant for "no board position" of value -1. */
		public static final long NONE = LongQueue.NONE;


		/**
		 * Returns a priority queue for storing board positions.
		 * <p>
		 * The board positions are ordered according to their "order value" which
		 * is read from the passed "boardPositionStorage".
		 *
		 * @param maxValueChange  the maximum value change of the order value between two calls of the {@link #add(long)} method
		 * @param minValueChange  the minimum value change of the order value between two calls of the {@link #add(long)} method
		 * @param boardPositionStorage  the storage that holds all reached board positions
		 * @param threadsCount  number of threads that use this queue. It's important that all of these threads repeatedly
		 * 						call the {@link #removeFirst(int)} method because that methods waits for all threads having reached
		 * 					    that methods at specific times
		 * @return the priority queue
		 */
		public static PriorityQueueOptimizer getInstance(final int minValueChange, final int maxValueChange, final BoardPositionsStorage boardPositionStorage, final int threadsCount) {

			/** The prio queue for small metrics seems not to be faster than the one for large metrics. Hence, it's not used at the moment. */
			//			return maxValueChange > 10000 ? new PriorityQueueOptimizerLargeMetrics(minValueChange, maxValueChange, boardPositionStorage, threadsCount) :
			//									    	new PriorityQueueOptimizerSmallMetricChanges(minValueChange, maxValueChange, boardPositionStorage, threadsCount);

			return new PriorityQueueOptimizerLargeMetrics(minValueChange, maxValueChange, boardPositionStorage, threadsCount);
		}

		/**
		 * Adds the passed board position index to this queue.
		 * <p>
		 * The board position is inserted according to its order value.
		 *
		 * @param boardPositionIndex  board position index to be stored
		 */
		public abstract void add(long boardPositionIndex);


		/**
		 * Returns the index of the board position having the lowest order value.
		 *
		 * @param currentThreadID  ID of the thread that calls this method
		 * @return index of the board position having the lowest order value or {@value #NONE} if this queue is empty.
		 */
		public abstract long removeFirst(int currentThreadID);
}
