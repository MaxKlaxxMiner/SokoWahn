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

import java.security.InvalidParameterException;
import java.util.Arrays;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicIntegerArray;

import de.sokoban_online.jsoko.optimizer.BoxConfiguration;


/**
 * Stores box configurations.
 * <p>
 * Box configurations contain the box positions as a bit array,
 * and are usually stored in byte arrays.
 * The optimizer needs the box configurations stored in an array.
 * More precisely: it needs to name the box configurations by an int index.
 * For a lower RAM usage the "visitedData" structure in the optimizer
 * is only created for really created box configurations.
 * Thus the hash table is forced to number the stored box configurations
 * because the optimizer must know which box configuration got which number.
 * [the hash table contains more elements than box configuration have
 * been created].
 * <p>
 * This storage is of "accum" type, i.e. once a box configuration is entered,
 * it is not deleted, again, until the complete storage is cleared.
 */
public class BoxConfigurationStorageHashSet {
	
	/** Constant indicating that the added box configuration is already stored in this set. */
	public static final int DUPLICATE = -1;
	
	/** Constant indicating that the box configuration coundn't be added due to this set being full. */
	public static final int TABLE_IS_FULL = -2;
			
	/** Impossible box configuration number to indicate empty hash table slots. */
	private final static int EMPTY = 0;
	
	/** Bit mask for the flag which represents the "locked" status of a slot in the table. */
	private static final int LOCKED = 1<<31;
	
	/** Calculate the absolute maximum capacity for this set. */ 
	public static final int MAX_CAPACITY = LOCKED - 1;

	/**
	 * Hash table slot array for storing the box configurations.
	 * Each slot contains the logical index into the data array
	 * {@link #boxConfigurations}.
	 */
	private final AtomicIntegerArray table;
	
	/** 
	 * Number of stored box configurations. <br>
	 * Note: the counter starts with 1 (which means index 0 is wasted for a dummy
	 * box configuration) in order to always return indices higher than 0 when 
	 * adding box configurations using {@link #add(BoxConfiguration)}.
	 * This makes it easier to use the indices in other classes like
	 * {@linkplain de.sokoban_online.jsoko.optimizer.AllMetricsOptimizer.BoardPositionsStorage.StorageBoardPosition StorageBoxPosition} 
	 * where the arrays needn't to be initialized with other values than 0.
	 * This also means we can use 0 as indicator value for EMPTY.
	 */
	private final AtomicInteger storedBoxConfigurationsCount = new AtomicInteger(1);
	
	private final BoxConfigurationStorage storage;
	
	/** The maximum number of box configurations that can be stored in this hash table. */
	private int capacity; 

	/**
	 * How many bytes each box configuration (hash table entry) consists of.
	 * Factor to compute physical index from logical index.
	 */
	private final int packedBoardByteSize;
	
	
	/**
	 * Creates a hash table for storing at most capacity box configurations
	 * having a size of boxPositionsCount positions.
	 * <p>
	 * The maximum capacity is {@value #MAX_CAPACITY}.<br>
	 * However, depending on the passed parameter {@code boxPositionsCount} the passed capacity
	 * may be set to a lower value even if it doesn't exceed {@value #MAX_CAPACITY}.
	 * 
	 * @param capacity  the maximum number of box configurations to be stored
	 * @param boxPositionsCount  number of valid box positions in one box configuration 
	 */
	public BoxConfigurationStorageHashSet(int capacity, int boxPositionsCount) {

		if(capacity >= MAX_CAPACITY || capacity <= 0) {
			throw new InvalidParameterException("invalid capacity");
		}
		
		// Save the length of a box configuration (one bit for every box accessible square).
		this.packedBoardByteSize = (boxPositionsCount - 1) / 8 + 1;

		// Create the hash table. It's 1,25 times bigger than the maximal number
		// of box configurations to store to minimize collisions.
		table = new AtomicIntegerArray((int) (1.25*capacity));

		// Create a huge array to store all box configuration data in.
		// We need one additional slot because "storedBoxConfigurationsCount" starts with 1 and this 
		// number is used in "BoxConfigurationStorage" as index! (-> boxConf number 0 is stored at index 1)
		storage = new BoxConfigurationStorage(capacity+1);
				
		// If the capacity is reached the hash table can't store any more box configurations.
		this.capacity = capacity;
		
	}

	
	
	
	/**
	 * Adds A COPY of the passed box configuration to this hash table.
	 * If the box configuration is already stored in the table it isn't stored again.
	 * <p>
	 * This method is thread safe.<br>
	 * The "index" is the index the box configuration is stored in the data array of this table.
	 * The first box configuration is stored at index 0,  the second is stored at index 1, ...<b>
	 * 
	 * @param boxConfiguration box configuration to be added
	 * 
	 * @return index the box configuration has been stored in, or<br>
	 *        {@link #DUPLICATE} (value: {@value #DUPLICATE}) if it is already stored in this set, or<br>
	 *        {@link #TABLE_IS_FULL} {@value #TABLE_IS_FULL} if it couldn't be stored because this set is full
	 *        
	 */
	public int add(BoxConfiguration boxConfiguration) {

		// Calculate hash value of the box configuration.
		int hashValue = boxConfiguration.hashCode();

		// Calculate the index of the box configuration in the hash table.
		int index = (hashValue & 0x7FFFFFFF) % table.length();
		
		// Number of the box configuration in the "boxConfigurations" array.
		int boxConfigurationNumber = -1;

		// Search for a free slot for the new box configuration. If one has been found lock it for storing the box configuration.
		while(table.compareAndSet(index, EMPTY, LOCKED) == false) {

			// The slot might be locked. Wait until it isn't locked anymore, 
			// which means all data have been stored.
			while( (boxConfigurationNumber = table.get(index)) == LOCKED);
			
			// Special case: usually after the lock is released a box configuration has been stored.
			// However, when the table is full the lock is released by setting back "EMPTY" (see coding above).
			// Hence, this index may be empty again and is a candidate to store in again.
			if(boxConfigurationNumber != EMPTY) {
			
				// If the box configuration is already stored it needn't be stored again.
				if(storage.isEqual(boxConfigurationNumber, boxConfiguration)) {
					return DUPLICATE;
				}
				
				// Check the next index in the loop.
				if(++index == table.length()) {
					index = 0;
				}
			}
		}

		// The box configuration must be stored. Get the number of the new box configuration.
		// Return "TABLE_IS_FULL" if maximum capacity of the hash table has already been reached.
		int boxConfNumber = 0;
		do { 
			boxConfNumber = storedBoxConfigurationsCount.get();
			if(boxConfNumber >= capacity) {
				// Remove the lock to ensure other threads aren't waiting for the lock to be released any longer.
				table.set(index, EMPTY);
				return TABLE_IS_FULL;
			}			
		}while(storedBoxConfigurationsCount.compareAndSet(boxConfNumber, boxConfNumber+1) == false);		
		
		// Store the box configuration. 
		storage.storeBoxConfiguration(boxConfNumber, boxConfiguration);
		
		// Store the index / number of the box configuration and thereby unlock the slot.
		table.set(index, boxConfNumber);

		return boxConfNumber;
	}

	
	/**
	 * Returns the index of the passed box configuration in this storage.
	 * <p>
	 * The "index" is a logical index. The box configurations are numbered. 
	 * The first box configuration is stored using index = 1, the second
	 * using index = 2, ...
	 * 
	 * @param boxConfiguration  the box configuration to be searched in this storage
	 * @return {@code -1} or the unique number of the passed box configuration in this storage
	 */
	public int getBoxConfigurationIndex(BoxConfiguration boxConfiguration) {

		// Calculate hash value of the box configuration.
		int hashValue = boxConfiguration.hashCode();
				
		// Calculate the index of the box configuration in the hash table.
		int index = (hashValue & 0x7FFFFFFF) % table.length();
	
		// Logical index of the box configuration in our flat data array "boxConfigurations".
		int boxConfigurationIndex = 1;

		// Search until an empty index is reached.
		while((boxConfigurationIndex = table.get(index)) != EMPTY) {

			// Wait until the board position isn't locked anymore, which means all its data have been saved.
			if(boxConfigurationIndex == LOCKED) {
				while((boxConfigurationIndex = table.get(index)) == LOCKED);
			}

			// If it has been found, return the number of the box configuration.
			if(storage.isEqual(boxConfigurationIndex, boxConfiguration)) {
				return boxConfigurationIndex;
			}

			// Check the next index in the loop.
			if(++index == table.length()) {
				index = 0;
			}
		}
				
		// The passed box configuration is not stored.
		return -1;
	}
	
	/**
	 * Returns whether there is a box at the passed position in the passed box configuration
	 * (represented by the passed index).
	 * 
	 * @param boxConfigurationNumber index of box configuration
	 * @param boxPosition	position to be checked for a box
	 * @return whether there is a box
	 */
	public boolean isBoxAtPosition(int boxConfigurationNumber, int boxPosition) {
		return storage.isBoxAtPosition(boxConfigurationNumber, boxPosition);
	}
	
	/**
	 * Copies a box configuration from this storage into the passed new box configuration.
	 * 
	 * @param newBoxConfiguration	  the box configuration which should become the clone
	 * @param boxConfigurationNumber  number of the box configuration to be copied
	 */
	public void copyBoxConfiguration(BoxConfiguration newBoxConfiguration, int boxConfigurationNumber) {
		storage.copyBoxConfiguration(newBoxConfiguration, boxConfigurationNumber);
	}
	
	/**
	 * Returns the number of stored box configurations.
	 * 
	 * @return the number of stored box configurations
	 */
	public int getSize() {
		return storedBoxConfigurationsCount.get();
	}
	
	/**
	 * The box configuration data array is resized to the optimal size.
	 * After this operation all further attempts to do an {@link #add(BoxConfiguration)}
	 * will fail.  This storage is now frozen.<br>
	 * This method is not thread safe. It musn't be called when any thread
	 * is currently calling method {@link #add(BoxConfiguration)}.
	 */
	public void optimizeSize() {	
		
		// Adjust the data array to the right size if it isn't fully filled.
		if(storedBoxConfigurationsCount.get() < capacity) {
		
			// Announce the reduced capacity.
			capacity = storedBoxConfigurationsCount.get();
			
			storage.trim(capacity);

		}
		
	}
	
	/**
	 * Returns the capacity of this storage.
	 * 
	 * @return the capacity of this storage
	 */
	public int getCapacity() {
		return capacity;
	}
	
	/**
	 * Removes all stored box configurations from this set.
	 */
	public void clear() {
		storedBoxConfigurationsCount.set(1); // set 1 (see comment for variable by declaration)
		for(int i=table.length(); --i != -1; ) {
			table.set(i, EMPTY);
		}
	}
	
	/**
	 * Class for storing board positions.
	 * <p>
	 * The board positions are stored according to their passed number.
	 * Hence this storage might be seen as a HashMap with the box configuration number as key. 
	 */
	private class BoxConfigurationStorage {

		/** 
		 * An array bigger than the maximal array size is split into several arrays having at most this size each. 
		 * The value isn't set to the real maximal array size in Java since the "trim" method needs to make copies 
		 * of the array and to create another huge array may result in an out-of-memory-error.
		 * Hence, this low max size tries to help the operation system to find space in the RAM to create a huge array.
		 */
		private final static int MAX_BYTES_PER_ARRAY = 1<<27;
		
		/** Calculated maximal number of box configurations to be stored in one array - this value is always a power of 2. */
		private final int BIT_MASK_BOX_CONFIGURATION_INDEX_IN_ARRAY;
		
		/** 
		 * If more box configurations have to be stored than can be stored in one array the data is split into several arrays.
		 * This value is the number of times the box configuration number must be bit-shifted right to get the index of the array
		 * the box configuration data is stored in. 
		 */
		private final int BIT_SHIFT_VALUE_DATA_ARRAY_INDEX;
		
		/**
		 * Flat array of box configurations.
		 * Each box configuration occupies {@link #packedBoardByteSize} many bytes.
		 * The board position having number 1 is stored at index: 1 * packedBoardByteSize
		 * The board position having number 2 is stored at index: 2 * packedBoardByteSize
		 * ...
		 *  
		 * This can be done using one huge byte array. However, since arrays have a maximum
		 * size in Java we might not store all board positions in one array. Therefore this
		 * class splits the data into several arrays if necessary.
		 */
		final private byte[][] boxConfigurations;
		
		/**
		 * Creates a storing for box configurations. 
		 * 
		 * @param capacity  maximum number of box configurations this storage can store
		 */
		private BoxConfigurationStorage(int capacity) {
								
			// MAX_BYTES_PER_ARRAY/packedBoardByteSize is the maximum number of board positions that can be stored in one array (assuming a size of 1<<27).
			// For a better performance in the other methods which adds/reads board positions we want to have this maximum number to be a power of 2.
			// 2**x  *  packedBoardByteSize <= MAX_BYTES_PER_ARRAY      <- we search the highest possible x
			int powerOf2Value = (int) (Math.log(MAX_BYTES_PER_ARRAY/packedBoardByteSize) / Math.log(2));
			
			// This is the bit-mask to extract the box configuration index.
			BIT_MASK_BOX_CONFIGURATION_INDEX_IN_ARRAY = (1<<powerOf2Value) - 1;
			
			// Number of bit-shifts to the right to be done to get the index of the array the boxConf is stored in (the index is stored in the "higher" bits).
			BIT_SHIFT_VALUE_DATA_ARRAY_INDEX = powerOf2Value;		   
						
			// Create the arrays the box configurations are stored in.
			int maxBoxConfigurationsPerArray = BIT_MASK_BOX_CONFIGURATION_INDEX_IN_ARRAY + 1; // index 0 to index "BIT_MASK_BOX_CONFIGURATION_INDEX_IN_ARRAY"
			int neededArraysCount = (capacity-1) / maxBoxConfigurationsPerArray + 1;			
			boxConfigurations = new byte[neededArraysCount][];
			for(int i=0, boxConfCount = capacity; i<neededArraysCount; i++) {
				boxConfigurations[i] = boxConfCount < maxBoxConfigurationsPerArray ? new byte[boxConfCount * packedBoardByteSize] :
													 					             new byte[maxBoxConfigurationsPerArray * packedBoardByteSize]; 
				boxConfCount -= maxBoxConfigurationsPerArray;
			}
		}
		
		/**
		 * Stores the specified box configuration in this storage and associates it with the specified number.<br>
	     * If there was already a box configuration associated with the specified number the stored box configuration is replaced with the new one.<br>
	     * The number must be in the range 0 - "capacity of this storage".
	     *
	     * @param boxConfigurationNumber  number with which the specified box configuration is to be associated
	     * @param boxConfiguration  {@code BoxConfiguration} to be stored
		 */
		private void storeBoxConfiguration(int boxConfigurationNumber, BoxConfiguration boxConfiguration) {
			
			int dataArrayIndex = boxConfigurationNumber >>> BIT_SHIFT_VALUE_DATA_ARRAY_INDEX;
			int boxConfigurationIndex = boxConfigurationNumber & BIT_MASK_BOX_CONFIGURATION_INDEX_IN_ARRAY;
			
			System.arraycopy(boxConfiguration.data, 0, boxConfigurations[dataArrayIndex], boxConfigurationIndex * packedBoardByteSize, packedBoardByteSize);

		}
		
		/**
		 * Returns whether the two box configurations are equal.
		 * 
		 * @param boxConfigurationNumber  number with which the specified box configuration is associated
		 * @param boxConfiguration  the candidate box configuration to compare with
		 * @return <code>true</code> if both box configurations are equal, and<br>
		 *        <code>false</code> otherwise
		 */
		private boolean isEqual(int boxConfigurationNumber, BoxConfiguration boxConfiguration) {
			
			int dataArrayIndex = boxConfigurationNumber >>> BIT_SHIFT_VALUE_DATA_ARRAY_INDEX;
			int boxConfigurationIndex = boxConfigurationNumber & BIT_MASK_BOX_CONFIGURATION_INDEX_IN_ARRAY;
			
			byte[] dataArray = boxConfigurations[dataArrayIndex];
			
			int indexOfBoxConfiguration = boxConfigurationIndex * packedBoardByteSize;

			// Compare byte by byte of both box configurations.
			for(int byteNo=0; byteNo < boxConfiguration.data.length; byteNo++) {
				if (dataArray[indexOfBoxConfiguration++] != boxConfiguration.data[byteNo]) {
					return false;
				}
			}
			
			return true;
		}
		
		/**
		 * Returns whether there is a box at the passed position in the passed box configuration
		 * (represented by its number).
		 * 
		 * @param boxConfigurationNumber  number with which the specified box configuration is associated
		 * @param boxPosition	position to be checked for a box
		 * @return <code>true</code> when there is a box at the position, <code>false</code> otherwise
		 */
		private boolean isBoxAtPosition(int boxConfigurationNumber, int boxPosition) {
			
			int dataArrayIndex = boxConfigurationNumber >>> BIT_SHIFT_VALUE_DATA_ARRAY_INDEX;
			int boxConfigurationIndex = boxConfigurationNumber & BIT_MASK_BOX_CONFIGURATION_INDEX_IN_ARRAY;
			
			int byteNo=(boxPosition>>3), bitPosition=(boxPosition&7);
			
			return (boxConfigurations[dataArrayIndex][boxConfigurationIndex*packedBoardByteSize+byteNo]&(1<<bitPosition)) != 0;			
		}
		
		/**
		 * Copies a box configuration from this storage into the passed new box configuration.
		 * 
		 * @param newBoxConfiguration	  the box configuration which should become the clone
		 * @param boxConfigurationNumber  number of the box configuration to be copied
		 */
		private void copyBoxConfiguration(BoxConfiguration newBoxConfiguration, int boxConfigurationNumber) {
			
			int dataArrayIndex = boxConfigurationNumber >>> BIT_SHIFT_VALUE_DATA_ARRAY_INDEX;
			int boxConfigurationIndex = boxConfigurationNumber & BIT_MASK_BOX_CONFIGURATION_INDEX_IN_ARRAY;
			
			System.arraycopy(boxConfigurations[dataArrayIndex], boxConfigurationIndex*packedBoardByteSize, newBoxConfiguration.data, 0, packedBoardByteSize);
		}
		
		/**
		 * The box configuration data array is resized to the optimal size.
		 *
		 * @param newCapacity  new capacity of the storage
		 */
		private void trim(int newCapacity) {	
							
			int dataArrayIndex = newCapacity >>> BIT_SHIFT_VALUE_DATA_ARRAY_INDEX;
			int boxConfigurationIndex = newCapacity & BIT_MASK_BOX_CONFIGURATION_INDEX_IN_ARRAY;		
	
			// "boxConfigurationIndex" is the highest index of a box configuration. There are the indicies 0 - boxConfigurationIndex,
			// therefore the new size must be: ( boxConfigurationIndex + 1 ) * packedBoardByteSize.
			int newSize = boxConfigurationIndex * packedBoardByteSize + packedBoardByteSize;
					
			// Resize the array holding the last box configuration.			
			boxConfigurations[dataArrayIndex] = Arrays.copyOf(boxConfigurations[dataArrayIndex], newSize);
			
			// Delete all further arrays because they aren't needed anymore.
			for(int i=dataArrayIndex+1; i<boxConfigurations.length; i++) {
				boxConfigurations[i] = null;
			}
	
			// Let the garbage collector free the memory used by the old "boxConfigurations" array.
			System.gc();
			
		}
	}
	
}