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

import java.util.Arrays;



/**
 * Priority queue for storing the optimizer board positions ordered by their metrics (moves, pushes, box lines, ...)
 */
public class BoardPositionPriorityQueueInts {

	/**
	 * Priority queue represented as a binary heap: the two
	 * children of queue[i] are queue[2*i+1] and queue[2*(i+1)].
	 * Board positions are ordered by the first two metrics stored in the board positions.
	 * The board position having the lowest metrics is in queue[0].
	 */
	private int[] queue;

	/**
	 * The number of elements in the priority queue.
	 */
	private int count = 0;
	
	/**
	 * Storage where the real board position data is saved. This priority queue just stores references to this data.
	 */
	private BoardPositionsStorage boardPositionsStorage;


	/**
	 * Creates a <code>PriorityQueue</code> with the specified initial capacity
	 * for storing the indices of board positions from the optimizer.
	 *
	 * @param initialCapacity the initial capacity for this priority queue
	 * @param boardPositionStorage storage where the real board position data is saved
	 */
	public BoardPositionPriorityQueueInts(int initialCapacity, BoardPositionsStorage boardPositionStorage) {
		this.queue = new int[initialCapacity];
		this.boardPositionsStorage = boardPositionStorage;
	}



	/**
	 * Adds the specified board position into this priority queue.
	 *
	 * @param boardPosition board position to be added
	 */
	public synchronized void add(int boardPosition) {
		
		// Double the size of the queue if it is full.
		if (count >= queue.length) {
			queue = Arrays.copyOf(queue, queue.length * 2);	
		}		
		
		// Add the new value at the correct position. 
		siftUp(count, boardPosition);
		
		// One more board position has been stored => increase the counter.
		count++;
	}

	/**
	 * Returns the number of stored <code>OptimizerBoardPositions</code>.
	 * 
	 * @return the number of stored <code>OptimizerBoardPositions</code>
	 */
	public synchronized int size() {
		return count;
	}


	/**
	 * Removes and returns the board position having the lowest metrics (moves, pushes, ...).
	 * 
	 * @return  board position having the lowest metrics 
	 */
	public synchronized int removeFirst() {

		if (count == 0)
			return -1;

		// The head of the queue must be returned.
		int result = queue[0];
		
		// The head of the queue has (logically) been removed => adjust the size.
		--count;
		
		// Remove the last board position.
		int  x = queue[count];
		queue[count] = -1;
		
		// If there is at least one board position left in the queue then 
		// shift the board positions so the queue has a new head.
		if (count != 0)
			siftDown(0, x);

		return result;
	}


	 /**
	  * Inserts the passed board position at the passed position.<br>
	  * The board position is promoted up the tree until it is greater 
	  * than or equal to its parent.
	  *
	  * @param position the position to insert into
	  * @param boardPosition the <code>OptimizerBoardPosition</code> to insert
	  */
	private void siftUp(int position, int boardPosition) {
		
		// Promote the board position. However, if the root is reached stop.
		while (position > 0) {
			
			// Calculate the index of the parent.
			int parentIndex = (position - 1) >>> 1;
			
			// Get the parent.
			int parent = queue[parentIndex];
			
			// If the parent has better metrics ("is lower") than the board position
			// to be inserted then we have found the position to insert to.
			if (boardPositionsStorage.compareBoardPositions(boardPosition, parent) >= 0)
				break;
			
			// Move the parent one position down in the queue.
			queue[position] = parent;
			
			// Continue using the parent index.
			position = parentIndex;
		}
		
		// Save the new board position at the calculated position.
		queue[position] = boardPosition;
	}


	 /**
	  *Inserts the passed board position at the passed position.
	  * <p>
	  * This method moves the passed board position down until 
	  * it is less than or equal to its children (or is a leaf).
	  *
	  * @param position the position to start the search for the correct position to insert into
	  * @param boardPosition the board position to be inserted
	  */
	 private void siftDown(int position, int boardPosition) {
		 
		 // Calculate the half of the size as the maximum position the loop must go to.
		 int half = count >>> 1;        
		 
		 while (position < half) {
			 int childIndex = (position << 1) + 1; // assuming the left child is the "better" board position
			 int child = queue[childIndex];
			 int rightChildIndex = childIndex + 1;
			 if (rightChildIndex < count && boardPositionsStorage.compareBoardPositions(child, queue[rightChildIndex]) > 0)
				 child = queue[childIndex = rightChildIndex];
			 if (boardPositionsStorage.compareBoardPositions(boardPosition, child) <= 0)
				 break;
			 queue[position] = child;
			 position = childIndex;
		 }
		 
		 queue[position] = boardPosition;
	 }

}