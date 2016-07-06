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


import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicIntegerArray;
import java.util.concurrent.atomic.AtomicReference;


/**
 * Unbounded FIFO queue for storing <code>Integer</code>s >= 0.
 * <p>
 * This queue only stores positive integers >= 0!
 */
public class IntegerQueue {

	/** Constant indicating "no value". */
	public static final int NONE = -1;

	/** Constant representing an empty slot in the queue -> nothing is stored. */
	private static final int EMPTY = 0; 


	/** This is the number of ints stored in one memory block. */
	private final int MEMORY_BLOCK_SIZE;

	/** Current memory block to read from. */
	private final AtomicReference<MemoryBlock> memoryBlockToRead;

	/** Current memory block to write to. */
	private AtomicReference<MemoryBlock> memoryBlockToWrite;


	/**
	 * Creates an unbounded queue for storing <code>integer</code>s >= 0.
	 * <p>
	 * This queue can only store values >= 0.<br>
	 * If more than {@code memoryBlockSize} ints are stored,
	 * several memory blocks are chained in a linked list so all ints can be  stored. <br>
	 * Every chaining requires a small memory and performance overhead - on the other hand 
	 * a too large memory block size wastes RAM because not all memory block slots are used.
	 *
	 * @param memoryBlocksSize  number of ints stored in one memory block
	 */
	public IntegerQueue(int memoryBlocksSize) {

		// Set the number of ints stored in one memory block.
		MEMORY_BLOCK_SIZE = memoryBlocksSize;

		// At the start the memory block to write to and the memory block to read from are the same.
		memoryBlockToWrite = new AtomicReference<MemoryBlock>(new MemoryBlock(MEMORY_BLOCK_SIZE));
		memoryBlockToRead  = new AtomicReference<MemoryBlock>(memoryBlockToWrite.get());
	}

	/**
	 * Adds the passed value to this queue.
	 * <p>
	 * If an <code>integer</code> < 0 is passed the method returns 
	 * without storing the value.
	 * 
	 * @param value  the value to be added (must be >= 0!)
	 */
	public void add(int value) {

		// Ignore invalid values.
		if(value < 0) {
			return;
		}
		
		// 0 is used as indicator for an empty slot -> see constant EMPTY.
		// Therefore 0 is saved using the Integer.MIN_VALUE which can't occur as value.
		if(value == 0) {
			value = Integer.MIN_VALUE;
		}

		// Get the memory block to store the value in.
		MemoryBlock memoryBlock = memoryBlockToWrite.get();

		// Until a free slot for storing the value has been found.
		while(true) {

			// Get a new index to store the value in and then store it at that index.
			int indexToWrite = 0;
			while((indexToWrite = memoryBlock.nextIndexToWrite.get()) < MEMORY_BLOCK_SIZE) {
				if(memoryBlock.compareAndSet(indexToWrite, EMPTY, value)) {
					memoryBlock.nextIndexToWrite.incrementAndGet();				
					return;	
				}
			}	

			// The current memory block is full => get the next memory block to write to.
			MemoryBlock newMemoryBlock = memoryBlock.nextMemoryBlock.get();

			if(newMemoryBlock == null) {

				// Yet, there is no next memory block => create a new one.
				newMemoryBlock = new MemoryBlock(MEMORY_BLOCK_SIZE);

				// Set the new memory block (if no other thread has already done).
				if(memoryBlock.nextMemoryBlock.compareAndSet(null, newMemoryBlock) == false) {
					newMemoryBlock = memoryBlock.nextMemoryBlock.get();

					// The new created memory block can't be used and will be 
					// garbage collected. Adding it to a pool and using the pool
					// instead of creating a new memory blocks every time doesn't
					// result in a better performance...
				}
			}

			// In any case "newMemoryBlock" is now the next memory block following
			// after the current memory block. Hence, set it as new memory block to be written to.
			memoryBlockToWrite.compareAndSet(memoryBlock, newMemoryBlock);

			// In the meantime the newMemoryBlock may have been already filled and
			// replaced by a new one. Hence, read the current memory block to write to.
			memoryBlock = memoryBlockToWrite.get();			
		}
	}


	/**
	 * Removes a value from this queue.
	 *  
	 * @return the value from this queue or {@value #NONE} when the queue is empty
	 */
	public int remove() {

		// Until a value to be removed has been found or the queue is empty.
		while(true) {

			// Get the memory block to be read.
			MemoryBlock memoryBlock = memoryBlockToRead.get();

			int indexToRead = 0;			
			while((indexToRead = memoryBlock.nextIndexToRead.get()) < MEMORY_BLOCK_SIZE) {

				// Get the value. If the queue is empty return NONE.
				int value = memoryBlock.get(indexToRead);
				if(value == EMPTY) {		
					return NONE;
				}

				// A value to be removed has been found. Hence, the next call
				// must search for values to be removed at the next index.
				if(memoryBlock.nextIndexToRead.compareAndSet(indexToRead, indexToRead+1)) {

					// 0 represents an empty slot (see constant EMPTY), hence
					// special treatment if value = 0 is stored and removed.
					return value == Integer.MIN_VALUE ? 0 : value;  
				}
			}					

			// The current memory block has completely been read. A new one must be set for reading,
			// if another thread hasn't already set a new memory block to be read.
			if(memoryBlockToRead.get() == memoryBlock) {

				// Get the next memory block to be read.
				MemoryBlock nextMemoryBlock = memoryBlock.nextMemoryBlock.get();

				if(nextMemoryBlock == null) {
					return NONE;		// Queue is empty
				}

				// Set the new memory block for reading.
				memoryBlockToRead.compareAndSet(memoryBlock, nextMemoryBlock);
			}
		}
	}


	/**
	 * Returns whether the queue is empty.
	 * Note: this method is not thread safe. It must only be called
	 * when no thread is currently removing or adding a value.
	 * 
	 * @return <code>true</code> when queue is empty, and<br>
	 *        <code>false</code> otherwise
	 */
	public boolean isEmpty() {			

		// The queue is empty when the next index to read and the next index to write are the same.
		MemoryBlock memoryBlockRead = memoryBlockToRead.get();
		return memoryBlockRead == memoryBlockToWrite.get() &&
				memoryBlockRead.nextIndexToRead.get() == memoryBlockToWrite.get().nextIndexToWrite.get();

	}


	/**
	 * Debug method: this method is used for testing this queue using several threads.
	 * 
	 * @param threadCount  number of threads accessing this queue for testing
	 */
	public void debugTest(final int threadCount) {

		// Number of found failures.
		int failures = 0;
		int queueIsNotEmptyFailures = 0;

		System.out.println("Test started.");

		// 1000 test runs.
		for(int i=0; i<1000; i++) {
			
			final int testNumbersCount = 1000;
			final AtomicIntegerArray hashtable  = new AtomicIntegerArray(testNumbersCount*threadCount);
			final Thread[] threads = new Thread[threadCount]; 

			for(int threadNo=0; threadNo<threadCount; threadNo++) {

				final int id = threadNo;
				threads[id] = new Thread(new Runnable() {

					public void run() {
						for(int i=0; i<testNumbersCount; i++) {
							add(i+testNumbersCount*id);

							if(i%3 == 0) {
								int value = remove();
								if(value != NONE)
									hashtable.incrementAndGet(value);
							}
						}

						while(true) {
							int value = remove();
							if(value == NONE)
								break;
							hashtable.incrementAndGet(value);
						}
					}
				});
			}

			for(Thread t : threads) {
				t.start();
			}

			for(Thread t : threads) {
				try {
					t.join();
				} catch (InterruptedException e) {}
			}

			for(int j=0; j<hashtable.length(); j++) {
				if(hashtable.get(i) != 1) {
					System.out.println("Failure: value "+i+" has been reached "+hashtable.get(i)+" times.");
					failures++;
				}
			}

			if(isEmpty() == false) {
				queueIsNotEmptyFailures++;
			}
		}

		System.out.println("Test finished. "+failures+" failures, queue not empty failures: "+queueIsNotEmptyFailures);
	}
	
	/**
	 * Memory block used in the queue for <code>integer<code>s.
	 * <p>
	 * This essentially is a integer array which additionally holds a reference 
	 * to another <code>MemoryBlock</code> (for chaining several <code>MemoryBlock</code>s)
	 * and two counter for reading and writing to this memory block.
	 */
	@SuppressWarnings("serial")
	static class MemoryBlock extends AtomicIntegerArray {

		/** Next index to store a new value. */
		final AtomicInteger nextIndexToWrite = new AtomicInteger();
		
		/** Next index to read a value from. */
		final AtomicInteger nextIndexToRead  = new AtomicInteger();
				
		/** Reference used for chaining <code>MemoryBlock</code>s. */
		AtomicReference<MemoryBlock> nextMemoryBlock = new AtomicReference<MemoryBlock>();

		
		/**
		 * Creates a new <code>MemoryBlock</code> of the passed size.
		 * 
		 * @param size  the size of this memory block
		 */
		public MemoryBlock(int size) {
			super(size);
		}
	}
}