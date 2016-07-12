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

import static de.sokoban_online.jsoko.optimizer.Optimizer.NONE;
import static de.sokoban_online.jsoko.optimizer.dataStructures.BoxConfigurationStorageHashSet.DUPLICATE;
import static de.sokoban_online.jsoko.optimizer.dataStructures.BoxConfigurationStorageHashSet.TABLE_IS_FULL;

import java.io.File;
import java.util.ArrayList;
import java.util.HashSet;
import java.util.concurrent.ConcurrentLinkedQueue;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicBoolean;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicLong;

import de.sokoban_online.jsoko.board.Board;
import de.sokoban_online.jsoko.board.DirectionConstants;
import de.sokoban_online.jsoko.optimizer.dataStructures.BoxConfigurationStorageHashSet;
import de.sokoban_online.jsoko.optimizer.dataStructures.IntegerQueue;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Debug;
import de.sokoban_online.jsoko.utilities.Utilities;

/**
 * This class calculates (forward) deadlock box configurations, which are used in the optimizer to reduce the search space. All involved box configurations are
 * of small cardinality: 2, 3, ... boxes, up to some small limit. Otherwise the amount of data would grow too large.
 * 
 * @see File "./development/Tasks/OptimizerDeadlocks.txt"
 */
public class DeadlockIdentification implements DirectionConstants {

	/**
	 * A board of size e.g. 10*10 has 100 squares. Hence, to store whether there is a box/player at a specific position 200 bits are needed. However, due to the
	 * fact that the boxes and the player can't access every square (walls, deadlocks) the number of accessible squares is smaller than 100. Hence, the total
	 * number of accessible squares for the player and the total number of accessible squares for the boxes is determined, and only the accessible squares are
	 * considered.
	 * 
	 * For instance: the player can only access 70 of the 100 squares. Then the bit array would only contain 70 bits. To convert an external position (regarding
	 * all 100 squares) to an internal position (regarding only 70 squares as relevant) these arrays are created.
	 */
	private final int[] boxInternalToExternalPosition;
	private final int[] boxExternalToInternalPosition;
	private final int[] playerExternalToInternalPosition;

	/** Counts the accessible box squares on the board */
	private final int boxPositionsCount;

	/**
	 * This array stores which player position is reachable in the 4 directions, or NONE if none is reachable. It is indexed by direction, first.
	 */
	private final int[][] playerSquareNeighbor;

	/**
	 * Which internal player position corresponds to which internal box position. playerPositionToBoxPosition[10] = 5; -> the 10th player accessible square is
	 * the 5th box accessible square on the board.
	 */
	private final int[] playerPositionToBoxPosition;

	/**
	 * The internal board containing the level elements.
	 */
	private final OptimizerBoard board;

	/** Number of boxes on the board. */
	private final int boxCount;

	/**
	 * Contains all identified deadlocked box configurations: [boxPositionOfOneOfTheBoxes][AllDeadlockedBoxConfigurations] Example: deadlockIdentification[2]
	 * contains all deadlock box configurations that have a box at position 2.
	 */
	private final ArrayList<ConcurrentLinkedQueue<BoxConfiguration>> deadlockBoxConfigurations;

	/** The start position of the player in a level. */
	private final int playerStartPosition;

	/** The main optimizer object which has started the search. */
	private final Optimizer optimizer;

	/** Number of threads to be used to identify deadlock box configurations. */
	private final int THREADS_TO_USE;

	/** Main currentThread this deadlock detection is executed in. */
	private Thread deadlockDetectionThread;

	/**
	 * Creates an object for detecting deadlocks.
	 * <p>
	 * This deadlock detection:
	 * <ul>
	 * <li>identifies deadlocks that are player position independent</li>
	 * <li>is done in a separate currentThread, started from the constructor of the optimizer
	 * <li>is done for a limited, fixed number of boxes on the board</li>
	 * <li>is used during the generation of box configuration in the optimizer (to limit search space)
	 * </ul>
	 * The results (a set of deadlocked box configurations) are stored in this object, and are queried by {@link #isDeadlock}.
	 * <p>
	 * This deadlock identification is only used while the optimizer generates box configurations (phase 1). In phase 2 where the vicinity search is performed
	 * these deadlocks aren't needed anymore.
	 * 
	 * @param optimizer
	 *            the optimizer this deadlock detection is used in
	 * @param boardObject
	 *            the main board of the game
	 * @param threadsCount
	 *            number of threads this method may use
	 */
	public DeadlockIdentification(Optimizer optimizer, Board boardObject, int threadsCount) {

		this.optimizer = optimizer;
		boxCount = boardObject.boxCount;
		this.boxPositionsCount = optimizer.boxPositionsCount;
		this.THREADS_TO_USE = threadsCount;

		boxInternalToExternalPosition = optimizer.boxInternalToExternalPosition;
		boxExternalToInternalPosition = optimizer.boxExternalToInternalPosition;
		playerExternalToInternalPosition = optimizer.playerExternalToInternalPosition;
		playerPositionToBoxPosition = optimizer.playerPositionToBoxPosition;
		playerSquareNeighbor = optimizer.playerSquareNeighbor;

		// Reference to the optimizer board. A clone to be safe of changes.
		board = optimizer.board.getClone();

		// Calculate the player start position in the level.
		playerStartPosition = playerExternalToInternalPosition[boardObject.playerPosition];

		// Create a byte array for storing all found deadlocked box configurations.
		// For quicker access the found deadlocks are divided according to the
		// box positions. That means:
		// deadlockIdentification[10] contains all deadlocked box configurations
		// where one of the boxes is located at position 10.
		// The queue is used by several threads, hence it's a concurrent queue.
		deadlockBoxConfigurations = new ArrayList<ConcurrentLinkedQueue<BoxConfiguration>>(boxPositionsCount);

		// Create queue of box configurations for every box position.
		for (int counter = 0; counter < boxPositionsCount; counter++) {
			deadlockBoxConfigurations.add(new ConcurrentLinkedQueue<BoxConfiguration>());
		}
	}

	/**
	 * Identifies and saves all deadlock box configurations consisting of up to x boxes. "x" is calculated depending on the level size.
	 * <p>
	 * This method identifies and saves all deadlock box configurations where at most "x" boxes are involved with the restriction, that it must be a deadlock
	 * box configurations no matter where the player is located.
	 */
	public void identifyDeadlocksInExtraThread() {

		// Create an own thread for the deadlock detection.
		deadlockDetectionThread = new Thread(new Runnable() {

			@Override
			public void run() {

				final long begtime = System.currentTimeMillis();

				// If the level contains only one box no further deadlock detection has to be done.
				// In this case the deadlockBoxConfiguration array stays empty.
				if (boxCount < 2) {
					return;
				}

				final AtomicBoolean hasDeadlockDetectionFinished = new AtomicBoolean();

				// Identify all deadlocks in an own thread in order to use the current thread for waiting.
				final ExecutorService executor = Executors.newFixedThreadPool(1);
				executor.execute(new Runnable() {
					public void run() {
						identifyDeadlockBoxConfigurations();
						hasDeadlockDetectionFinished.set(true);
					}
				});

				// The deadlock detection runs at most 3 second to ensure the resources of this computer aren't used too much.
				// If the deadlock detection won't finish within 3 seconds we just continue with not all deadlock box configurations having been identified.
				Utilities.shutdownAndAwaitTermination(executor, 3, TimeUnit.SECONDS);

				// Wait until the deadlock detection has finished.
				while (hasDeadlockDetectionFinished.get() == false) {
					Thread.yield();
				}

				// Try to free the RAM immediately because we have allocated a lot.
				System.gc();

				// Add the information that the deadlock detection has finished.
				if (Debug.isDebugModeActivated) {
					long millis = System.currentTimeMillis() - begtime;
					printLogText("time=" + millis + " ms");
					debugShowStatistics();
				}

				printLogText(Texts.getText("optimizer.deadlockDetectionFinished") + "\n");
			}
		});
		deadlockDetectionThread.setPriority(Thread.MIN_PRIORITY);
		deadlockDetectionThread.start();
	}

	/**
	 * Stops the thread currently trying to identify deadlocks.
	 */
	public void stopDeadlockIdentificationThread() {
		if (deadlockDetectionThread != null) {
			deadlockDetectionThread.interrupt();
		}

		// No need to wait for the thread having finished.
		// Just no more deadlocks are added to "deadlockBoxConfigurations".
	}

	/**
	 * Identifies and saves all deadlocked box configurations that contain exactly the indicated amount of boxes (have the indicated cardinality).
	 * 
	 * @param boxesToBeUsedInDeadlockCount
	 *            number of boxes that create the deadlocks to identify
	 */
	private void identifyDeadlockBoxConfigurations() {

		// Create generator for generating all "no deadlock" box configurations.
		NoDeadlockBoxConfigurationsGenerator noDeadlockBoxConfigurationsGenerator = new NoDeadlockBoxConfigurationsGenerator();

		// Create deadlock detector for detecting all deadlocks.
		DeadlockDetector deadlockDetector = new DeadlockDetector();

		Thread currentThread = Thread.currentThread();

		// Identify all deadlocks containing 2, 3, ..., maxBoxesUsedInDeadlocks boxes.
		for (int boxesToBeSetCount = 2; boxesToBeSetCount <= boxCount && currentThread.isInterrupted() == false; boxesToBeSetCount++) {

			optimizer.optimizerGUI.addLogText(Texts.getText("optimizer.identifyingDeadlocks", boxesToBeSetCount));

			// Create all possible box configurations consisting of "boxesUsedInDeadlockCount" boxes
			// that are not a deadlock and store them in "allNoDeadlockBoxConfigurations".
			BoxConfigurationStorageHashSet allNoDeadlockBoxConfigurations = noDeadlockBoxConfigurationsGenerator.generate(boxesToBeSetCount);

			// Create all possible box configurations consisting of "boxesUsedInDeadlockCount" boxes.
			// Those which aren't stored in "allNoDeadlockBoxConfigurations" must be a deadlock and are stored in "deadlockIdentification".
			if (!currentThread.isInterrupted()) {
				deadlockDetector.detect(boxesToBeSetCount, allNoDeadlockBoxConfigurations);
			}
		}
	}

	/**
	 * Creates all possible box configurations consisting of {@link #boxesToBeUsedCount} boxes that are not a deadlock and stores them in
	 * "allNoDeadlockBoxConfigurations".
	 */
	private class NoDeadlockBoxConfigurationsGenerator {

		/** Storage where all found box configurations not being a deadlock are stored in. */
		private final BoxConfigurationStorageHashSet boxConfigurationStorage;

		/** Open queue for the search for no deadlock box configurations. */
		private final IntegerQueue queue;

		/** The positions of all goals on the board converted to internal box positions. */
		private final int[] boxPositionsOfGoals;

		/**
		 * Atomic counter used by all generating threads to calculate the next box position to set a box at.
		 */
		private final AtomicInteger firstBoxPosition = new AtomicInteger();

		/** Number of boxes to be used for generating box configurations. */
		private int boxesToBeUsedCount;

		/**
		 * Creates a new generator for generating all possible box configurations consisting of a specific number of boxes that are not a deadlock.
		 */
		NoDeadlockBoxConfigurationsGenerator() {

			// Create the open queue with initial size of 100000.
			queue = new IntegerQueue(100000);

			// Estimate the RAM usage of the storage in bytes and reserve a 5th
			// of the available RAM for the storage.
			final int sizePerBoxConfigurationData = (boxPositionsCount - 1) / 8 + 1;
			final float sizePerBoxConfigurationTable = 1.25f * 4; // 1,25 * integer size
			int capacity = (int) (Utilities.getMaxUsableRAMInBytes() / 5 / (sizePerBoxConfigurationData + sizePerBoxConfigurationTable));
			capacity = Math.min(capacity, BoxConfigurationStorageHashSet.MAX_CAPACITY);

			// Create the storage for the found no deadlock box configurations.
			boxConfigurationStorage = new BoxConfigurationStorageHashSet(capacity, boxPositionsCount);

			// Store the positions of all goals as box positions.
			boxPositionsOfGoals = new int[boxCount];
			for (int boxPosition = 0, goalCount = 0; boxPosition < boxPositionsCount; boxPosition++) {
				if (board.isGoal(boxInternalToExternalPosition[boxPosition])) {
					boxPositionsOfGoals[goalCount++] = boxPosition;
				}
			}
		}

		/**
		 * Generates all possible box configurations consisting of {@link #boxesToBeUsedCount} boxes that are not a deadlock. These box configurations are
		 * stored in a hash set which is returned.
		 * 
		 * @param boxesToBeUsedCount
		 *            number of boxes to be used for generating box configurations
		 * @return a set of all no deadlock box configurations having boxesToBeUsedCount boxes
		 */
		public BoxConfigurationStorageHashSet generate(int boxesToBeUsedCount) {

			this.boxesToBeUsedCount = boxesToBeUsedCount;

			// If the storage is already filled from a previous run then clear it.
			if (boxConfigurationStorage.getSize() > 0) {
				boxConfigurationStorage.clear();
			}

			// The first box has to be set at position 0.
			firstBoxPosition.set(0);

			final ExecutorService executor = Executors.newFixedThreadPool(THREADS_TO_USE);

			// Generate all possible box configurations consisting of
			// "boxesToBeUsedCount" boxes on goals and save them in the open queue
			// and the box configuration storage.
			for (int i = 0; i < THREADS_TO_USE; i++) {
				executor.execute(new StartBoxConfigurationsGenerator());
			}

			// The generated box configurations are used as start
			// box configurations. From them all possible pulls are done.
			// This way all possible "no deadlock" box configurations
			// are generated and saved in the storage.
			for (int i = 0; i < THREADS_TO_USE; i++) {
				executor.execute(new BoxConfigurationGenerator());
			}

			// Wait until all box configurations have been generated.
			Utilities.shutdownAndAwaitTermination(executor, 1, TimeUnit.DAYS);

			return boxConfigurationStorage;
		}

		/**
		 * Used to generate all box configurations consisting of a specific number of boxes which are all located on a goal.
		 */
		private class StartBoxConfigurationsGenerator implements Runnable {

			/** BoxConfiguration for storing the information where on the board there are boxes. */
			private final BoxConfiguration boxConfiguration = new BoxConfiguration(boxPositionsCount);

			/** Direct reference to the current Thread. */
			private Thread currentThread;

			/**
			 * Starts the generation of box configurations.
			 */
			public void run() {

				currentThread = Thread.currentThread();

				// Recursively set "boxesToBeUsedCount" boxes at every position.
				// The first box is set at the positions: 0 to (lastPosition - boxesToBeUsedCount)
				// The second box is set at the positions: 1 to (lastPosition - boxesToBeUsedCount + 1)
				// ...
				int endIndex = boxPositionsOfGoals.length - boxesToBeUsedCount;

				// Set the first box at every position between the start and
				// the end position and then set further boxes "behind" that
				// position until boxesToBeUsedCount boxes have been set.
				for (int index = firstBoxPosition.getAndIncrement(); index <= endIndex && !currentThread.isInterrupted(); index = firstBoxPosition
						.getAndIncrement()) {

					int newBoxPosition = boxPositionsOfGoals[index];

					boxConfiguration.addBox(newBoxPosition);

					setBox(index + 1, endIndex + 1, boxesToBeUsedCount - 1);

					boxConfiguration.removeBox(newBoxPosition);
				}
			}

			/**
			 * Sets a box at every position between startPosition and endPosition (including both positions). If boxesToSet is higher than 1 further boxes are
			 * set behind those positions.
			 * 
			 * @param startIndex
			 *            first position to set a box
			 * @param endIndex
			 *            last position to set a box
			 * @param boxesToSetCount
			 *            number of boxes still to be set
			 */
			void setBox(final int startIndex, final int endIndex, final int boxesToSetCount) {

				// For every position between the start and the end position.
				for (int index = startIndex; index <= endIndex && !currentThread.isInterrupted(); index++) {

					int newBoxPosition = boxPositionsOfGoals[index];

					boxConfiguration.addBox(newBoxPosition);

					if (boxesToSetCount == 1) {
						// This has been the last box to be set => store the
						// box configuration (in fact a copy is stored!).
						int boxConfigurationIndex = boxConfigurationStorage.add(boxConfiguration);

						if (boxConfigurationIndex == TABLE_IS_FULL) {
							deadlockDetectionThread.interrupt(); // Interrupt the whole deadlock detection
							currentThread.interrupt(); // Interrupt the current currentThread
							return;
						}

						// If it's a new box configuration stored it in the queue.
						// Note: this algorithm can't create duplicate ones.
						// However, one of the threads may already have finished
						// the generation of start box configurations and already
						// pulling boxes to create new box configurations (see
						// method generate). Hence, there might be duplicates.
						// This check is just there to document this.
						if (boxConfigurationIndex != DUPLICATE) {
							queue.add(boxConfigurationIndex);
						}
					} else {
						// Set a further box.
						setBox(index + 1, endIndex + 1, boxesToSetCount - 1);
					}

					boxConfiguration.removeBox(newBoxPosition);
				}
			}
		}

		/**
		 * Generates all no deadlock box configurations. The box configurations to start the search with have been added to the queue. These box configurations
		 * are used to pull the boxes to generate all box configurations reachable from the end box configurations (where all boxes are on a goal).
		 */
		private class BoxConfigurationGenerator implements Runnable {

			@Override
			public void run() {

				BoxConfiguration boxConfiguration = new BoxConfiguration(boxPositionsCount);

				// The positions of all boxes in a box configuration.
				int[] boxPositions = new int[boxesToBeUsedCount];

				// Current currentThread this deadlock detection is performed in.
				Thread currentThread = Thread.currentThread();

				// Loop over all box configurations that are known not to be deadlock
				// ones and generate new "no deadlock" box configurations from them
				// by pulling the boxes.
				while (!currentThread.isInterrupted()) {

					int indexOfBoxConfiguration = queue.remove();

					// Stop the loop when the queue is empty.
					if (indexOfBoxConfiguration == IntegerQueue.NONE) {
						break;
					}

					// Copy the box configuration to "boxConfiguration".
					boxConfigurationStorage.copyBoxConfiguration(boxConfiguration, indexOfBoxConfiguration);

					// Get the box positions from the box configuration.
					boxConfiguration.fillBoxPositions(boxPositions);

					// Loop over all box positions.
					for (int boxPosition : boxPositions) {

						// Get the internal player position that corresponds to the box position.
						int playerPosition = playerExternalToInternalPosition[boxInternalToExternalPosition[boxPosition]];

						// Set the player next to the box and move the player to every direction.
						for (int direction = 0; direction < 4; direction++) {

							int currentPlayerPosition, newPlayerPosition, newBoxPosition, boxPositionOfNewPlayerPosition;

							// Check if it is possible to pull the box:
							// 1. The position to set the player mustn't be a wall (-> != NONE)
							// 2. The new player position mustn't be a wall.
							// 3. The new box position must be no deadlock and not blocked by another box
							// 4. The new player position mustn't be occupied by a box
							if ((currentPlayerPosition = playerSquareNeighbor[direction][playerPosition]) == NONE || // a wall at that position?
									(newPlayerPosition = playerSquareNeighbor[direction][currentPlayerPosition]) == NONE || // a wall at that position?
									(newBoxPosition = playerPositionToBoxPosition[currentPlayerPosition]) == NONE || // is a deadlock position?
									boxConfiguration.isBoxAtPosition(newBoxPosition) || // is a box at that position?
									(boxPositionOfNewPlayerPosition = playerPositionToBoxPosition[newPlayerPosition]) != NONE && // is a blocking box at the
									boxConfiguration.isBoxAtPosition(boxPositionOfNewPlayerPosition)) { // new player position?
								continue;
							}

							boolean isPlayerStuck = true;

							/*
							 * ##### __$@# Example for player being stuck ___$# The box has just been pulled right. The player can't move anymore. The player is
							 * stuck. This situation is therefore a deadlock position because the boxes can't be pulled further to finally reach the start box
							 * configuration of the level - except this IS the start box configuration. The start box configuration is identified by checking if
							 * the new player position is on his start position in the level and all boxes being on a start position of a box in the level. In
							 * fact during the forward search stuck situation (except the start board position) can't occur - however, the generator also
							 * generates these box configurations, hence they have to be identified as deadlocks, so they are discarded during the generation.
							 */

							// Check whether the player can move.
							for (int direction2 = 0; direction2 < 4; direction2++) {
								// If the player can move to any neighbor (note: the current player position
								// will be blocked by the pulled box!), then the player isn't stuck.
								int neighborPosition = playerSquareNeighbor[direction2][newPlayerPosition];
								if (neighborPosition != NONE && neighborPosition != currentPlayerPosition) {
									int neighborBoxPosition = playerPositionToBoxPosition[neighborPosition];
									if (neighborBoxPosition == NONE || !boxConfiguration.isBoxAtPosition(neighborBoxPosition)) {
										isPlayerStuck = false;
										break;
									}
								}
							}

							// If the player is stuck, check whether it's
							// (a subset of) the start board position of the level.
							if (isPlayerStuck == true) {
								if (newPlayerPosition == playerStartPosition) {
									isPlayerStuck = false;
									for (int boxPos : boxPositions) {
										if (boxPos == boxPosition) {
											boxPos = newBoxPosition;
										}
										if (!board.isBox(boxInternalToExternalPosition[boxPos])) {
											isPlayerStuck = true;
											break;
										}
									}
								}
							}
							if (isPlayerStuck == false) {

								// Do the pull.
								boxConfiguration.moveBox(boxPosition, newBoxPosition);

								// Add the box configuration to the storage.
								int boxConfigurationIndex = boxConfigurationStorage.add(boxConfiguration);

								if (boxConfigurationIndex == TABLE_IS_FULL) {
									deadlockDetectionThread.interrupt(); // Interrupt the whole deadlock detection
									currentThread.interrupt(); // Interrupt the current currentThread
									return;
								}

								// If the box configuration isn't a duplicate, then add it to the open queue.
								if (boxConfigurationIndex != DUPLICATE) {
									queue.add(boxConfigurationIndex);
								}

								// Undo the pull to reuse the box configuration.
								boxConfiguration.moveBox(newBoxPosition, boxPosition);
							}
						}
					}
				}
			} // end of method "run"
		}
	}

	/**
	 * <code>Runnable</code> that generates all possible box configurations and checks whether they are deadlocks.
	 */
	private class DeadlockDetector {

		/** Number of boxes to be used for generating box configurations. */
		private int boxesToBeSetCount;

		/** Storage all box configurations that aren't a deadlock are stored in. */
		private BoxConfigurationStorageHashSet noDeadlockBoxConfigurations;

		/** Atomic counter used by all generating threads to calculate the next box position. */
		private final AtomicInteger firstBoxPosition = new AtomicInteger();

		/**
		 * Used for identifying deadlock box configurations.
		 */
		DeadlockDetector() {
		}

		/**
		 * Searches and stores all deadlocks consisting of {@link #boxesToBeSetCount} boxes.
		 * 
		 * @param boxesToBeSetCount
		 *            number of boxes to be set for generating box configurations
		 * @param noDeadlockBoxConfigurations
		 *            storage of all no deadlock box configurations
		 */
		public void detect(int boxesToBeSetCount, BoxConfigurationStorageHashSet noDeadlockBoxConfigurations) {

			this.boxesToBeSetCount = boxesToBeSetCount;
			this.noDeadlockBoxConfigurations = noDeadlockBoxConfigurations;

			// Initialize the first box position with 0 again (the detector is
			// reused for several runs and the value has to be reset).
			firstBoxPosition.set(0);

			/**
			 * Create and execute multiple threads that generate the box configurations.
			 */
			final ExecutorService executor = Executors.newFixedThreadPool(THREADS_TO_USE);

			for (int i = 0; i < THREADS_TO_USE; i++) {
				executor.execute(new GenerateBoxConfigurations());
			}

			// Wait until all box configurations have been generated.
			Utilities.shutdownAndAwaitTermination(executor, 1, TimeUnit.DAYS);
		}

		/**
		 * Generator for generating all box configurations consisting of a specific number of boxes. If any of them is known to be a deadlock then it is stored
		 * in {@link DeadlockIdentification#deadlockBoxConfigurations deadlockIdentification}.
		 */
		private class GenerateBoxConfigurations implements Runnable {

			/** BoxConfiguration for storing the positions of all boxes on the board. */
			private final BoxConfiguration boxConfiguration = new BoxConfiguration(boxPositionsCount);

			/** Direct reference to the current currentThread. */
			private Thread currentThread;

			/**
			 * Starts the generation of box configurations.
			 */
			public void run() {

				currentThread = Thread.currentThread();

				// The last position to set the first box at. Since
				// boxesToBeSetCount-1 further boxes have to be set "behind" this
				// first box the end position must be set accordingly.
				final int endPosition = boxPositionsCount - boxesToBeSetCount;

				/*
				 * Recursively set "boxesToBeUsedCount" boxes at every position. The first box is set at the positions: 0 to (lastPosition - boxesToBeUsedCount)
				 * The second box is set at the positions: 1 to (lastPosition - boxesToBeUsedCount + 1) ...
				 * 
				 * The box configurations are generated in the following way: Assuming 5 box accessible positions, and 3 boxes to be used, box1 is represented
				 * by 1, box2 is represented by 2, ... 123__ 12_3_ 12__3 1_23_ 1_2_3 1__23 _123_ _12_3 _1_23 __123
				 */
				// Set the first box at every position between the start and
				// the end position and then set further boxes "behind" that
				// position until boxesToBeUsedCount boxes have been set.
				for (int boxPosition = firstBoxPosition.getAndIncrement(); boxPosition <= endPosition && !currentThread.isInterrupted(); boxPosition = firstBoxPosition
						.getAndIncrement()) {

					boxConfiguration.addBox(boxPosition);

					setBox(boxPosition + 1, endPosition + 1, boxesToBeSetCount - 1);

					boxConfiguration.removeBox(boxPosition);
				}
			}

			/**
			 * Sets a box at every position between startPosition and endPosition (including both positions). If boxesToSet is higher than 1, further boxes are
			 * set behind those positions.
			 * 
			 * @param startPosition
			 *            first position to set a box
			 * @param endPosition
			 *            last position to set a box
			 * @param boxesToSetCount
			 *            number of boxes still to be set
			 */
			void setBox(final int startPosition, final int endPosition, final int boxesToSetCount) {

				// For every position between the start and the end position.
				for (int boxPosition = startPosition; boxPosition <= endPosition && !currentThread.isInterrupted(); boxPosition++) {

					boxConfiguration.addBox(boxPosition);

					// Check whether this is a deadlock box configuration.
					// (the deadlock detection has already identified all deadlocks having
					// fewer boxes than this deadlock detection run has!).
					if (!isDeadlock(boxConfiguration, boxPosition)) {

						// If boxes to be set is 1 then this has been the last box to be set.
						if (boxesToSetCount == 1) {

							// Check whether it's a deadlock box configuration.
							if (noDeadlockBoxConfigurations.getBoxConfigurationIndex(boxConfiguration) == NONE) {

								// Add the clone of the box configuration to every deadlock list where
								// there is a box in the box configuration.
								BoxConfiguration boxConfigurationClone = (BoxConfiguration) boxConfiguration.clone();
								for (int boxPos = 0, boxCount = 0; boxCount < boxesToBeSetCount; boxPos++) {
									if (boxConfigurationClone.isBoxAtPosition(boxPos)) {
										deadlockBoxConfigurations.get(boxPos).add(boxConfigurationClone);
										boxCount++;
									}
								}
							}
						} else {
							// Set a further box.
							setBox(boxPosition + 1, endPosition + 1, boxesToSetCount - 1);
						}
					}

					boxConfiguration.removeBox(boxPosition);
				}
			}
		}
	}

	/**
	 * Returns true when the passed box configuration is a deadlock.
	 * <p>
	 * This method only checks for deadlocks with a box at position "involvedBoxPosition".
	 * 
	 * @param boxConfiguration
	 *            box configuration to be checked for being a deadlock
	 * @param involvedBoxPosition
	 *            box position to be checked for being involved in a deadlock
	 * 
	 * @return <code>true</code> if box configuration is a deadlock, and<br>
	 *         <code>false</code> otherwise
	 */
	boolean isDeadlock(BoxConfiguration boxConfiguration, int involvedBoxPosition) {

		// deadlockStatistic.increaseDeadlockCheckCounter(); // High contention when using multiple threads! => bad performance, only use in debug mode!

		// Check against all found deadlocks.
		for (BoxConfiguration deadlockBoxConfiguration : deadlockBoxConfigurations.get(involvedBoxPosition)) {
			if (boxConfiguration.hasSubset(deadlockBoxConfiguration)) {
				// deadlockStatistic.checkSucceeded(deadlockBoxConfiguration); // statistics. High contention when using multiple threads! => bad performance,
				// only use in debug mode!
				return true;
			}
		}

		return false;
	}

	/**
	 * The deadlock detection may have run for several seconds to identify deadlocks. The deadlocks for a level don't change and are therefore reused. However,
	 * if only a specific range of pushes of a solution is optimized a new board is created from the specified solution range. In order to avoid an additional
	 * long deadlock identifying run this method reuses as many deadlocks as possible from the already identified deadlocks.<br>
	 * 
	 * @param otherDeadlockIdentification
	 *            the {@code DeadlockIdentification} for the original level
	 */
	public void inheritDeadlockBoxConfigurations(DeadlockIdentification otherDeadlockIdentification) {

		// Note:
		// "board" operates with "external" board positions. Both "deadlock identification" instances
		// operate with their own internal box positions depending on the underlying board. Hence, the box
		// positions must be converted between the objects.

		// There is a list of deadlock box configurations for every box position. Hence, we have to process all of these lists.
		for (int boxPosition = 0; boxPosition < otherDeadlockIdentification.deadlockBoxConfigurations.size(); boxPosition++) {

			// On the new board there now might be a wall at the position.
			int externalBoxPosition = otherDeadlockIdentification.boxInternalToExternalPosition[boxPosition];
			if (board.isWallOrDeadlock(externalBoxPosition)) {
				continue;
			}

			// Create a new box configurations by converting the internal box positions. Since there might be more walls on the
			// board now this has to be checked and invalid box positions are discarded.
			for (BoxConfiguration deadlockBoxConfiguration : otherDeadlockIdentification.deadlockBoxConfigurations.get(boxPosition)) {
				BoxConfiguration boxConfiguration = new BoxConfiguration(boxPositionsCount);
				for (int boxPos : deadlockBoxConfiguration.getBoxPositions()) {
					int otherExternalBoxPosition = otherDeadlockIdentification.boxInternalToExternalPosition[boxPos];
					if (!board.isWallOrDeadlock(otherExternalBoxPosition)) {
						boxConfiguration.addBox(boxExternalToInternalPosition[otherExternalBoxPosition]);
					}
				}

				// Box configurations containing only one box are useless, since this is identified by the simple deadlock square test.
				if (boxConfiguration.getBoxCount() > 1) {
					deadlockBoxConfigurations.get(boxExternalToInternalPosition[externalBoxPosition]).add(boxConfiguration);
				}
			}
		}

		// // DEBUG ONLY: Display all inherited deadlock box configurations for checking correctness of the coding.
		// HashSet<BoxConfiguration> deadlockBoxConfHashSet = new HashSet<BoxConfiguration>();
		// for(int boxPosition=0; boxPosition<boxPositionsCount; boxPosition++) {
		// for(BoxConfiguration boxConfiguration : deadlockBoxConfigurations.get(boxPosition)) {
		// if(deadlockBoxConfHashSet.add(boxConfiguration)) {
		// optimizer.debugDisplayBoxConfiguration(boxConfiguration, 0, true, true);
		// }
		// }
		// }
	}

	/**
	 * We are going to measure the success of the box deadlock data.<br>
	 * We want to know:<br>
	 * <li>how often we did ask, and <li>how often was the answer useful.
	 */
	private static class DeadLockQueryStats {

		/** Deadlock questions asked. */
		private final AtomicLong deadlockChecksPerformedCount = new AtomicLong();

		/**
		 * Distribution of {@code deadlockChecksAnsweredWithTrueCount} by cardinality of found box deadlock
		 */
		private AtomicLong positiveDeadlockChecksBy[] = new AtomicLong[10];

		public DeadLockQueryStats() {
			for (int i = 0; i < positiveDeadlockChecksBy.length; i++) {
				positiveDeadlockChecksBy[i] = new AtomicLong();
			}
		}

		/**
		 * Add the values of that other object into this object.
		 * 
		 * @param other
		 *            to be summed in to me
		 */
		synchronized void sumFrom(DeadLockQueryStats other) {
			deadlockChecksPerformedCount.addAndGet(other.deadlockChecksPerformedCount.get());
			for (int i = 0; i < positiveDeadlockChecksBy.length; i++) {
				positiveDeadlockChecksBy[i].addAndGet(other.positiveDeadlockChecksBy[i].get());
			}
		}

		/**
		 * Set all counters back to zero.
		 */
		void clear() {
			deadlockChecksPerformedCount.set(0);
			for (int i = 0; i < positiveDeadlockChecksBy.length; i++) {
				positiveDeadlockChecksBy[i].set(0);
			}
		}

		@Override
		public boolean equals(Object o) {
			if (o != null) {
				if (o instanceof DeadLockQueryStats) {
					DeadLockQueryStats oth = (DeadLockQueryStats) o;
					if (deadlockChecksPerformedCount != oth.deadlockChecksPerformedCount)
						return false;
					for (int i = 0; i < positiveDeadlockChecksBy.length; i++) {
						if (positiveDeadlockChecksBy[i] != oth.positiveDeadlockChecksBy[i])
							return false;
					}
					return true;
				}
			}
			return false;
		}
	}

	/**
	 * The overall total statistics
	 */
	private static DeadLockQueryStats dlqTotStats = new DeadLockQueryStats();

	/**
	 * The currently active statistics. Summed into the grand total, and cleared, when printed (after generation of a vicinity cloud).
	 */
	private DeadLockQueryStats deadlockStatistic = new DeadLockQueryStats();

	private void debugShowDeadlockStatistic(String hdrtxt, DeadLockQueryStats stats, long total) {
		if (!Debug.isDebugModeActivated) {
			return;
		}
		if (stats.deadlockChecksPerformedCount.get() != 0) {
			String totPercStr;
			if ((total == 0) || (total == stats.deadlockChecksPerformedCount.get())) {
				totPercStr = "";
			} else {
				totPercStr = " (" + Utilities.percStr(stats.deadlockChecksPerformedCount.get(), total) + " of all)";
			}

			long totalPositiveDeadlockChecks = 0;
			for (AtomicLong positiveDeadlockChecksCount : stats.positiveDeadlockChecksBy) {
				totalPositiveDeadlockChecks += positiveDeadlockChecksCount.get();
			}

			String succPercStr = " (" + Utilities.percStr(totalPositiveDeadlockChecks, stats.deadlockChecksPerformedCount.get()) + " of asked)";

			printLogTextMonoStyle("Box Deadlock Questions (" + hdrtxt + "):");
			printLogTextMonoStyle("  questions asked: " + Utilities.fillL(9, "" + stats.deadlockChecksPerformedCount) + totPercStr);
			printLogTextMonoStyle("  with success:    " + Utilities.fillL(9, "" + totalPositiveDeadlockChecks) + succPercStr);
			if (totalPositiveDeadlockChecks != 0) {
				for (int i = 0; i < stats.positiveDeadlockChecksBy.length; i++) {
					long positiveChecks = stats.positiveDeadlockChecksBy[i].get();
					if (positiveChecks != 0) {
						printLogTextMonoStyle("     by boxCard " + i + ": " + Utilities.fillL(9, "" + positiveChecks)
								+ Utilities.percStrEmb(positiveChecks, totalPositiveDeadlockChecks));
					}
				}
			}
		}
	}

	/**
	 * Prints the text to the optimizer log.
	 * 
	 * @param txt
	 *            text to print
	 */
	private void printLogText(String txt) {
		optimizer.optimizerGUI.addLogText(txt);
	}

	/**
	 * Prints the text to the optimizer log using a monospace font.
	 * 
	 * @param txt
	 *            text to print
	 */
	private void printLogTextMonoStyle(String txt) {
		optimizer.optimizerGUI.addLogTextDebug(txt);
	}

	/**
	 * Debug method: without {@link Debug#isDebugModeActivated} nothing visible happens. In debug mode we print the current statistics to the optimizer
	 * logging.
	 */
	void debugShowStatistics() {

		if (Debug.isDebugModeActivated == false) {
			return;
		}

		// Collect all deadlock box configurations and discard duplicates.
		HashSet<BoxConfiguration> deadlockBoxConfHashSet = new HashSet<BoxConfiguration>();
		for (int boxPosition = 0; boxPosition < boxPositionsCount; boxPosition++) {
			for (BoxConfiguration boxConfiguration : deadlockBoxConfigurations.get(boxPosition)) {
				deadlockBoxConfHashSet.add(boxConfiguration);
			}
		}

		// Count the deadlock box configurations having x boxes.
		int[] deadlockCount = new int[boxCount + 1];
		for (BoxConfiguration boxConfiguration : deadlockBoxConfHashSet) {
			deadlockCount[boxConfiguration.getBoxCount()]++;
		}

		printLogTextMonoStyle("\nFound deadlock box configurations:");

		// Display statistic.
		for (int boxCounter = 2; boxCounter < deadlockCount.length; boxCounter++) {

			if (deadlockCount[boxCounter] == 0) {
				continue;
			}

			// Display the number of found deadlock box configurations.
			printLogTextMonoStyle("boxCnt=" + boxCounter + ": " + String.format(" %4d box configurations", deadlockCount[boxCounter]));

			// Display all box configurations containing x boxes for debug.
			// System.out.println("Deadlock box configurations having "+boxCounter+" boxes:");
			// for(BoxConfiguration boxConfiguration : deadlockBoxConfHashSet) {
			// if(boxConfiguration.getBoxCount() == boxCounter) {
			// optimizer.new Debug().debugDisplayBoxConfiguration(boxConfiguration, 0, false, false);
			// }
			// }
		}

		dlqTotStats.sumFrom(deadlockStatistic);
		if (!dlqTotStats.equals(deadlockStatistic)) {
			debugShowDeadlockStatistic("total", dlqTotStats, 0);
		}

		debugShowDeadlockStatistic("this time", deadlockStatistic, dlqTotStats.deadlockChecksPerformedCount.get());
		deadlockStatistic.clear();
	}
}
