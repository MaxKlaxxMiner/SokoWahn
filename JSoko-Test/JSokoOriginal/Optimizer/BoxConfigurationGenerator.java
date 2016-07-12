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
package de.sokoban_online.jsoko.optimizer;

import java.util.concurrent.CancellationException;

import de.sokoban_online.jsoko.optimizer.dataStructures.BoxConfigurationStorageHashSet;



/**
 * Class for generating box configurations for the optimizer in an own thread.<br>
 * This class is passed a box configuration from the optimizer and generates
 * permutations of this box configuration which are stored in a storage.
 * This class is instantiated for every box configuration of the solution
 * to be optimized.
 */
class BoxConfigurationGenerator implements Runnable {

	/** Used to check for deadlock box configurations. */
	private final DeadlockIdentification deadlockDetection;

	/** A box configuration. */
	private final BoxConfiguration boxConfiguration; 

	/**
	 * Number of vicinity squares used for generating deviation box configurations.
	 * {10, 20} for instance means: one box may be repositioned to any of the 20 
	 * nearest neighbor squares and another box may be repositioned to any of the 
	 * 10 nearest neighbor squares.<br>
	 * The numbers must be stored in ascending order in order for the duplicates check to work! 
	 */
	private final int[] numberOfVicinitySquaresABoxMayEnter;

	/** The positions of all boxes in the box configuration. */
	private final int[] boxPositions;

	/** Positions of boxes that have been repositioned. */
	private final int[] repositionedBoxPositions;

	/** Which positions can a box reach from a specific position. */
	private final int[][] reachablePositionsFrom;

	/** Box configuration storage of this class. */
	private final BoxConfigurationStorageHashSet boxConfigurationStorage;

	/** The thread this task is running in. */
	private Thread currentThread;


	/**
	 * Creates an object for generating permutation box configurations
	 * from the passed box configuration in an own thread.
	 * <p>
	 * The passed variables are usually the same for all generation tasks,
	 * except of the passed box configuration and pushes depth.
	 * However, this way it's possible to pass different settings
	 * for every Runnable.
	 * 
	 * @param numberOfVicinitySquaresABoxMayEnter  number of vicinity squares a box may be repositioned to
	 * @param reachablePositionsFrom  which positions are reachable for a box from which position
	 * @param boxConfiguration   the box configuration which is taken as basis for generating permutations
	 * @param storage  			 the storage all generated box configurations of all generation threads are to be added to
	 * @param deadlockDetection  the optimizer which controls the generation
	 */
	BoxConfigurationGenerator(
			final int[]   numberOfVicinitySquaresABoxMayEnter,
			final int[][] reachablePositionsFrom,
			final BoxConfiguration boxConfiguration,
			final BoxConfigurationStorageHashSet storage,
			final DeadlockIdentification deadlockDetection) {

		this.numberOfVicinitySquaresABoxMayEnter = numberOfVicinitySquaresABoxMayEnter;	
		this.reachablePositionsFrom = reachablePositionsFrom; 
		this.deadlockDetection 		= deadlockDetection;
		this.boxConfiguration  		= (BoxConfiguration) boxConfiguration.clone(); // Clone to be sure the original is never changed

		boxConfigurationStorage = storage;

		// Get the positions of all boxes.
		boxPositions = new int[boxConfiguration.getBoxCount()];
		boxConfiguration.fillBoxPositions(boxPositions);

		// Create an array for storing the positions of the boxes that have been repositioned.
		repositionedBoxPositions = new int[numberOfVicinitySquaresABoxMayEnter.length];

	}

	/**
	 * Generates all box configurations that are in the vicinity of the current
	 * box configuration regarding the passed limit, which limits
	 * the number of boxes that may leave their current positions.
	 * <p>
	 * A {@code CancellationException} exception is thrown when not all box configurations 
	 * could successfully be generated (due to thread being interrupted, full storage, ...).
	 */
	final public void run() {	

		// Get the current thread and save a reference to it.
		currentThread = Thread.currentThread();

		// Set minimum priority for the generation to avoid performance 
		// penalties for other programs running on the CPU.
		currentThread.setPriority(Thread.MIN_PRIORITY);

		// Hence, generate deviations of the current box configuration.
		generatePermutationsOfBoxConfiguration(0, -1, -1, Integer.MAX_VALUE);

	}

	/**
	 * Generate all box configurations that are near to the current board.
	 * "near" is specified by the passed number of boxes that may be
	 * repositioned and the passed maximum number of times
	 * the boxes may be repositioned.
	 * <p>
	 * The result of our operation is a set of box configurations,
	 * which is added to {@link #boxConfigurationStorage}.
	 * <p>
	 * The current box configuration to be modified is our instance variable
	 * {@link #boxConfiguration}. The list of mutable box positions,
	 * {@link #boxPositions} must also be updated accordingly.
	 * <p>
	 * NB: this method is recursive.
	 * <p>
	 * Here we are at the heart of the notion <em>vicinity</em>.
	 * 
	 * @param repositionedBoxesCount  number of boxes that have already been repositioned
	 * @param previousDepthBoxNo	 number of the last repositioned box
	 * @param previousDepthBoxOriginalPosition  old position of the last repositioned box
	 * @param previousDepthVicinityLimit number of squares the boxes have been repositioned to in the previous recursion depth   
	 */
	private void generatePermutationsOfBoxConfiguration(final int repositionedBoxesCount, final int previousDepthBoxNo, final int previousDepthBoxOriginalPosition, final int previousDepthVicinityLimit) {

		// Vicinity squares limit for this recursion depth.
		int vicinityLimit = numberOfVicinitySquaresABoxMayEnter[repositionedBoxesCount];

		// Loop over all box positions.
		for (int boxNo=0; boxNo < boxPositions.length; boxNo++) {

			if(currentThread.isInterrupted()) {
				throw new CancellationException("Thread has been interrupted.");
			}
			
			int boxPosition = boxPositions[boxNo];

			// Remove the box from the boxConfiguration.
			boxConfiguration.removeBox(boxPosition);

			// Get the positions the box may be set to.
			int[] boxVicinityPositions = reachablePositionsFrom[boxPosition];

			// Set the maximum number of new positions for the box. If there are less accessible squares than the limit given by the user,
			// then set the number of accessible squares as maximum.
			int maximumNewBoxPositions = Math.min(boxVicinityPositions.length, vicinityLimit);

			// For all accessible positions of the box until the maximum is reached.
			for (int positionCounter=0; positionCounter<maximumNewBoxPositions; positionCounter++) {

				// Get the position of the "squareCounter"-th accessible square.
				// The array is sorted by distance.
				// Hence, the nearest positions are taken first.
				int newBoxPosition = boxVicinityPositions[positionCounter];

				// Avoid generating duplicates. See a description for this at the end of this file.
				if(boxNo < previousDepthBoxNo && positionCounter < previousDepthVicinityLimit && newBoxPosition != previousDepthBoxOriginalPosition) {
					continue;
				}

				// If there already is a box at the new position no box can be added.
				if (boxConfiguration.isBoxAtPosition(newBoxPosition) == false) {

					boxConfiguration.addBox(newBoxPosition);

					// Save the positions of all repositioned boxes for checking for deadlocks.
					repositionedBoxPositions[repositionedBoxesCount] = newBoxPosition;
					
					// If no more boxes can be repositioned then add the new boxConfiguration to the storage.
					if (repositionedBoxesCount == numberOfVicinitySquaresABoxMayEnter.length-1) {
						if(isDeadlock(boxConfiguration, repositionedBoxPositions) == false) {
							if(boxConfigurationStorage.add(boxConfiguration) == BoxConfigurationStorageHashSet.TABLE_IS_FULL) {
								// restoring of boxConfiguration isn't necessary -> we operate on a clone (no "addBox / remove box")
								throw new CancellationException("Storage is full"); 
							}
						}
					} else {
						// The box has a new position. For the deeper recursion depths this is the new original position of the box.
						boxPositions[boxNo] = newBoxPosition;
						
						// Reposition more boxes.
						generatePermutationsOfBoxConfiguration(repositionedBoxesCount+1, boxNo, boxPosition, vicinityLimit);
					}

					boxConfiguration.removeBox(newBoxPosition);
				}
			}

			// Set the box back to the original position.
			boxConfiguration.addBox(boxPosition);
			boxPositions[boxNo] = boxPosition;
		}		
	}
	
	/**
	 * Checks whether any box in the passed positions array contributes to a deadlock on the board.
	 * 
	 * @param boxConfiguration  the box configuration containing all box positions
	 * @param boxPositionsToCheckForDeadlock  positions to check for contributing to a deadlock
	 * @return
	 */
	private boolean isDeadlock(BoxConfiguration boxConfiguration, int[] boxPositionsToCheckForDeadlock) {

		// Note: a box in one recursion depth may be repositioned in the next depth.
		// Hence, there needn't to be a box at the position at the moment => check for box first! 
		for(int position : boxPositionsToCheckForDeadlock) {
			if (boxConfiguration.isBoxAtPosition(position) && deadlockDetection.isDeadlock(boxConfiguration, position)) {	
				return true;
			}
		}
		
		return false;
	}


	
	/**
	  Description for avoiding duplicates while generating box configurations:
	    
	  Some duplicate box configurations can be pruned already before they are generated.
	  An example with vicinity settings 10/20 and four boxes A, B, C, and D:

	  01. A:10 B:20
	  02. A:10 C:20
	  03. A:10 D:20

	  04. B:10 A:20 only A:11-20 is necessary (see 01)
	  05. B:10 C:20
	  06. B:10 D:20

	  07. C:10 A:20 only A:11-20 is necessary (see 02)
	  08. C:10 B:20 only B:11-20 is necessary (see 05)
	  09. C:10 D:20

	  10. D:10 A:20 only A:11-20 is necessary (see 03)
	  11. D:10 B:20 only B:11-20 is necessary (see 06)
	  12. D:10 C:20 only C:11-20 is necessary (see 09)

	  There is one special case where this duplicate pruning cannot be applied.
	  An example with vicinity settings 5/5 and two boxes A and B on a 6-square board:

	  A---B-

	  01. A:5 B:5
		  A moves first. Note that no box configurations are generated with 
		  A at B's original square because that square is occupied at the
		  time A moves around on the board.

	  02. B:5 A:5
		  B moves first. If this leg was pruned entirely, the following 
		  box configuration would not be generated:
		  ----AB
		  This type of box configuration occurs when B moves to a square which
		  A cannot reach within its limit, and A moves to B's original square.
	 */
}
