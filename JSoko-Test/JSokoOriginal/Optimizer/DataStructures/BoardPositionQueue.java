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
package de.sokoban_online.jsoko.optimizer.dataStructures;



/**
 * Queue for storing integers.
 * This queue is used in the optimizer for storing board positions.
 */
public final class BoardPositionQueue {

	/** Current block to read from */
	private MemoryBlock memoryBlockToRead;
	
	/** Current block to write to */
	private MemoryBlock memoryBlockToWrite;
	
	/** Index of the next item to read from {@link #memoryBlockToRead}. */
	private int nextIndexToRead  = 0;
	
	/** Index of the next item to write to {@link #memoryBlockToWrite}. */
	private int nextIndexToWrite = 0;
			
	/** Flag, indicating whether a memory block is to be deleted after it has been completely read. */
	private boolean isDeletingMemoryBlocksActivated = false;
	
	/** Size per memory block (in int's) */
	final int MEMORY_BLOCK_SIZE;
   

	/**
	 * Creates a new queue for storing board positions.
	 * 
	 * @param isDeletingMemoryBlocksActivated  indicates whether a completely read memory block can be deleted or not
	 * @param memoryBlocksSize  size per memory block
	 */
	public BoardPositionQueue(boolean isDeletingMemoryBlocksActivated, int memoryBlocksSize) {
		MEMORY_BLOCK_SIZE = memoryBlocksSize;
		memoryBlockToRead = memoryBlockToWrite = new MemoryBlock(memoryBlocksSize);
		
		this.isDeletingMemoryBlocksActivated = isDeletingMemoryBlocksActivated;
	}

	/**
	 * Adds data to the queue.
	 * 
	 * @param boardPositionIndex        the board position to be added
	 * @param boardPositionPredecessor  the predecessor to be added
	 */
	public void add(int boardPositionIndex, int boardPositionPredecessor) {
		
		// The moves queue also contains an arbitrary number of span markers.
		// Therefore after every added board position there must be done
		// a check whether the memory block is already full.
		add(boardPositionIndex      );
		add(boardPositionPredecessor);
	}

	/**
	 * Adds the passed board position to the queue.
	 * 
	 * @param boardPosition the value to be added
	 */
	public void add(int boardPosition) {
			
		// Save the board position in the queue.
		memoryBlockToWrite.memory[nextIndexToWrite++] = boardPosition;
					
		// Check if the memory block is full. If yes, immediately create a new memory block.
		// The method "add" always needs a next memory block to exist.
		if(nextIndexToWrite == MEMORY_BLOCK_SIZE) {

			// If there isn't any free next memory block then create a new one.
			// Otherwise advance to the next memory block.
			if(memoryBlockToWrite.nextMemoryBlock == null) {
				memoryBlockToWrite = new MemoryBlock(memoryBlockToWrite, MEMORY_BLOCK_SIZE);
			} else {
				memoryBlockToWrite = memoryBlockToWrite.nextMemoryBlock;
			}
			
			// Start filling the new or old memory block at its start.
			nextIndexToWrite = 0;
		}	
	}
	
	/**
	 * Retrieves and logically removes a board position of this queue.
	 * The caller must ensure that the queue is not empty.
	 *  
	 * @return the board position from the queue
	 */
	public int removeBoardPosition() {
		
		// Get the next board position out of this queue.
		final int boardPosition = memoryBlockToRead.memory[nextIndexToRead++];
					
		// Check if the whole memory block has been read. 
		if(nextIndexToRead == MEMORY_BLOCK_SIZE) {

			if( isDeletingMemoryBlocksActivated ) {
				
				// Backup the memory block to be read next.
				MemoryBlock temp = memoryBlockToRead.nextMemoryBlock;
				
				// The current memory block has completely been read. Hence, it can be recycled.
				// It's inserted between the memory block to be written to and the next memory block to be written to.		
				if(memoryBlockToWrite.nextMemoryBlock != null) {
					memoryBlockToWrite.nextMemoryBlock.previousMemoryBlock = memoryBlockToRead;
				}
				
				memoryBlockToRead.nextMemoryBlock	  = memoryBlockToWrite.nextMemoryBlock;
				memoryBlockToRead.previousMemoryBlock = memoryBlockToWrite;
				
				memoryBlockToWrite.nextMemoryBlock = memoryBlockToRead;
				
				// Set the next memory block to be read.
				memoryBlockToRead = temp;	
			}
			else {
				// We shall not recycle the just used up block for reading.
				// Just advance to the next block in the list.
				memoryBlockToRead = memoryBlockToRead.nextMemoryBlock;
			}

			// Start reading the newly selected block at its first item.
			nextIndexToRead = 0;
		}

		// Return the board position.
		return boardPosition;
	}
		
	/**
	 * Logically removes all board positions from the queue until a board position 
	 * different to the passed one occurs in the queue.
	 * 
	 * @param boardPosition the board position to be over jumped
	 */
	public void jumpOverBoardPosition(int boardPosition) {
		// "Remove" all values from the queue which are equal to the passed one
		while(memoryBlockToRead.memory[nextIndexToRead] == boardPosition) {
			removeBoardPosition();
		}
	}
	
	/**
	 * Retrieves and removes the last board position of this queue.
	 * 
	 * @return the previous board position
	 */
	public int removeLastBoardPosition() {
						
		if(nextIndexToWrite == 0) {
			
			// The previous memory block becomes the new current memory block.
			memoryBlockToWrite = memoryBlockToWrite.previousMemoryBlock;
			nextIndexToWrite = MEMORY_BLOCK_SIZE;
		}
		
		return memoryBlockToWrite.memory[--nextIndexToWrite];
	}
	

	/**
	 * Jumps backwards in the queue by the passed number of board positions.
	 * 
	 * @param jumpCount number of board positions to be skipped backwards
	 */
	public void jumpXBoardPositionsBackwards(int jumpCount) {
		
		while(jumpCount > nextIndexToWrite) {
							
			// Assign the previous memory block of the queue.
			memoryBlockToWrite = memoryBlockToWrite.previousMemoryBlock;
			
			jumpCount -= nextIndexToWrite;
			nextIndexToWrite = MEMORY_BLOCK_SIZE;					
		}
		nextIndexToWrite -= jumpCount;
	}

	/**
	 * Returns whether the queue is empty.
	 * 
	 * @return <code>true</code> when queue is empty, and<br>
	 *        <code>false</code> otherwise
	 */
	public boolean isEmpty() {
		return memoryBlockToRead == memoryBlockToWrite
		    && nextIndexToRead   == nextIndexToWrite;
	}
	
	/**
	 * For storing board positions the memory is allocated step by step.
	 * Every time new memory is needed an instance of this class is created
	 * and used to store board positions.
	 * <p>
	 * Memory blocks live in double linked lists without an explicit anchor object.
	 */
	private static class MemoryBlock {
		
		/** The memory block data content */
		public int[] memory;
		
		/** List linkage to the previous memory block. */
		public MemoryBlock previousMemoryBlock = null;
		
		/** List linkage to the next memory block. */
		public MemoryBlock nextMemoryBlock     = null;
		
		/**
		 * Creates a new memory block of the indicated size, and appends it
		 * to the doubly linked list indicated by its currently last block.
		 * 
		 * @param previousMemoryBlock previous last memory block to which the new
		 *                            block is appended to
		 * @param size size of the new block in int's
		 */
		public MemoryBlock(MemoryBlock previousMemoryBlock, int size) {
			this.previousMemoryBlock = previousMemoryBlock;
			previousMemoryBlock.nextMemoryBlock = this;
			memory = new int[size]; 
		}
		
		/**
		 * Creates a new memory block of the indicated size.
		 * @param size size of the new block in int's
		 */
		public MemoryBlock(int size) {
			memory = new int[size];
		}
	}
}