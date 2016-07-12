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
import java.util.Comparator;


/**
 * Priority queue for storing the optimizer board positions ordered by their metrics (moves, pushes, box lines, ...)
 */
public class BoardPositionPriorityQueue {

	/**
	 * Priority queue represented as a binary heap: the two
	 * children of queue[i] are queue[2*i+1] and queue[2*(i+1)].
	 * Board positions are ordered by the first two metrics stored in the board positions.
	 * The board position having the lowest metrics is in queue[0].
	 */
	private BoardPosition[] queue;
	
	/** The comparator used to compare the elements. */
	private final Comparator<BoardPosition> comparator;

	/** The number of elements in the priority queue. */
	private int count = 0;

	
	/**
	 * Creates a priority queue with the specified initial capacity
	 * for storing "OptimizerBoardPositions".
	 *
	 * @param  initialCapacity the initial capacity for this priority queue
	 * @param  comparator the comparator that will be used to order this
     *         priority queue.
	 */
	public BoardPositionPriorityQueue(int initialCapacity, Comparator<BoardPosition> comparator) {
		
        if (initialCapacity < 1 || comparator == null) {
            throw new IllegalArgumentException();
        }
		
		this.queue = new BoardPosition[initialCapacity];
		this.comparator = comparator;
	}



	/**
	 * Adds the specified board position into this priority queue.
	 *
	 * @param boardPosition board position to be added
	 */
	public void add(BoardPosition boardPosition) {

		// Ensure that a real board position has been passed.
        if (boardPosition == null)
            throw new NullPointerException();
		
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
	public int size() {
		return count;
	}


	/**
	 * Removes and returns the board position having the lowest metrics (moves, pushes, ...).
	 * 
	 * @return  board position having the lowest metrics 
	 */
	public BoardPosition removeFirst() {

		if (count == 0)
			return null;

		// The head of the queue must be returned.
		BoardPosition result = queue[0];
		
		// The head of the queue has (logically) been removed => adjust the size.
		--count;
		
		// Remove the last board position.
		BoardPosition x = queue[count];
		queue[count] = null;
		
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
	private void siftUp(int position, BoardPosition boardPosition) {
		
		// Promote the board position. However, if the root is reached stop.
		while (position > 0) {
			
			// Calculate the index of the parent.
			int parentIndex = (position - 1) >>> 1;
			
			// Get the parent.
			BoardPosition parent = queue[parentIndex];
			
			// If the parent has better metrics ("is lower") than the board position
			// to be inserted then we have found the position to insert to.
			if (comparator.compare(boardPosition, parent) >= 0)
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
	 private void siftDown(int position, BoardPosition boardPosition) {
		 
		 // Calculate the half of the size as the maximum position the loop must go to.
		 int half = count >>> 1;        
		 
		 while (position < half) {
			 int childIndex = (position << 1) + 1; // assuming the left child is the "better" board position
			 BoardPosition child = queue[childIndex];
			 int rightChildIndex = childIndex + 1;
			 if (rightChildIndex < count && comparator.compare(child, queue[rightChildIndex]) > 0)
				 child = queue[childIndex = rightChildIndex];
			 if (comparator.compare(boardPosition, child) <= 0)
				 break;
			 queue[position] = child;
			 position = childIndex;
		 }
		 
		 queue[position] = boardPosition;
	 }
}