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
package de.sokoban_online.jsoko.deadlockdetection;

import java.util.Arrays;

import de.sokoban_online.jsoko.board.Board;
import de.sokoban_online.jsoko.pushesLowerBoundCalculation.LowerBoundCalculation;
import de.sokoban_online.jsoko.resourceHandling.Settings.SearchDirection;
import de.sokoban_online.jsoko.utilities.IntStack;

/**
 * Class for calculating bipartite matchings for boxes and goals.
 * <p>
 * A Sokoban level can only be solved when every box can be pushed to a goal where
 * one goal can only be occupied by one box. <br>
 * This class provides methods to calculating matchings that assigns every box to
 * its own goal.
 */
public class BipartiteMatchings {

	/**
	 * To avoid circular assignments the auction algorithms needs the prices to be increased by at least this value.
	 * The number of boxes * EPSILON must be lower than the lowest bid of any box to ensure this EPSILON value doesn't
	 * interfere with the actual bids of the boxes.
	 */
	private static final int EPSILON = 1;

	/**
	 * This value is used every time an infinite bid or price is to be set. Divide by 2 to ensure there are no
	 * integer overflows when adding values.
	 */
	private static final int INFINITE = Integer.MAX_VALUE / 2;

	/** Constant indicating that a box doesn't bid for a goal. */
	private static final int NO_BID = Integer.MIN_VALUE;

	private final int NOT_MATCHED = -1; // indicator value that a box hasn't yet been matched to a goal

	/** A distance higher the the maximum distance of any box on the board to any goal. */
	private final int MAXIMUM_BOX_DISTANCE;

	/** For easy access we use a direct reference to the board object */
	private final Board board;

	/** Arrays for storing the assignments: boxes <-> goals */
	private final int[] goalsMatchedToBoxes;
	private final int[] boxesMatchedToGoals;

	/**
	 * This class uses the auction algorithm. These arrays hold the prices and the bids.
	 * Every box bids for a goal. The highest bid "wins" the goal.
	 */
	private final int[] prices;
	private final int[][] bids;

	/** LiFo queue for storing the numbers of the boxes that have to be matched with a goal. */
	private final IntStack boxesToBeMatched;


	/**
	 * Creates an Object for calculating the pushes lower bound of a board position.
	 *
	 * @param board  the board the lower bound is to be calculated for
	 */
	public BipartiteMatchings(Board board) {

		this.board	= board;
		goalsMatchedToBoxes = new int[board.boxCount];
		boxesMatchedToGoals = new int[board.goalsCount];
		prices			   	= new int[board.boxCount];
		bids		   		= new int[board.boxCount][board.goalsCount];
		boxesToBeMatched 	= new IntStack(board.boxCount);

		// Must be higher than the maximum distance of a box on the board to any goal.
		MAXIMUM_BOX_DISTANCE = board.size * 4 + 1;
	}

	/**
	 * Returns whether the current board contains a bipartite deadlock.<br>
	 * This method must only be called after the "freeze" status of every box on the board has been updated
	 * since this status is used in this method!
	 * <p>
	 * A bipartite deadlock is present if not every box can reach its own goal.
	 *
	 * Example:<pre>
	 * #######
	 * # $.$ #
	 * #@ .  #
	 * #######</pre>
	 *
	 * The two boxes share one goal. Hence, the situation is a deadlock because one goal can't be reached by any boxes.
	 *
	 * @param searchDirection	direction of the search (forwards or backwards)
	 * @return <code>true</code> if the board is a bipartite deadlock, or <code>false</code> otherwise
	 */
	public boolean isDeadlock(SearchDirection searchDirection) {
		return isDeadlock(searchDirection, null);
	}

	/**
	 * Returns whether the current board contains a bipartite deadlock.<br>
	 * This method must only be called after the "freeze" status of every box on the board has been updated
	 * since this status is used in this method!
	 * <p>
	 * A bipartite deadlock is present if not every box can reach its own goal.
	 *
	 * Example:<pre>
	 * #######
	 * # $.$ #
	 * #@ .  #
	 * #######</pre>
	 *
	 * The two boxes share one goal. Hence, the situation is a deadlock because one goal can't be reached by any boxes.
	 *
	 * @param searchDirection	direction of the search (forwards or backwards)
	 * @param goalsToBeExcluded boolean array indicating which goals are to be excluded by the bipartite matching. {@code true} means "exclude goal"
	 * @return <code>true</code> if the board is a bipartite deadlock, or <code>false</code> otherwise
	 */
	public boolean isDeadlock(SearchDirection searchDirection, boolean[] goalsToBeExcluded) {

		// The box distances to the goals may have changed because of frozen boxes.
		// Hence we update them (may be a recalculation).
		board.distances.updateBoxDistances(searchDirection, true);

		// Calculate the bids of every box for the auction algorithm.
		calculateBidsForDeadlockDetection(searchDirection, goalsToBeExcluded);

		// Return whether a perfect matching has been found or not.
		// IF no perfect matching has been found the board is as deadlock situation.
		return !searchMinimumPerfectBipartiteMatching(searchDirection);

	}

	/**
	 * Computes a minimum number of pushes needed to push every box to a goal on the current board.
	 * The calculated number of pushes in never higher than the real minimum number but usually lower.<br>
	 * This method must only be called after the "freeze" status of every box on the board has been updated
	 * since this status is used in this method.
	 * <p>
	 * Note: the detection changes the board while trying to find a deadlock. However, after the method
	 * is finished the board is set back to the original board.
	 *
	 * @param searchDirection  direction of the search (forwards or backwards)
	 * @return lower bound of the current board
	 */
	public int calculatePushesLowerBound(SearchDirection searchDirection){

		// The box distances to the goals may have changed because of frozen boxes.
		// Hence we update them (may be a recalculation).
		board.distances.updateBoxDistances(searchDirection, true);

		// The matching is searched by an auction algorithm, where the boxes "buy" the goals.
		// Each box buys the goal, which is most favorable for it.
		// We have to calculate the bids of every box in a way that the closer goals get a
		// higher bid and the goals far away a lower bid.
		calculateBids(searchDirection);

		boolean machtingFound = searchMinimumPerfectBipartiteMatching(searchDirection);
		if(!machtingFound) {
			return LowerBoundCalculation.DEADLOCK;
		}

		// Calculate the sum of all distances of the boxes to their assigned goals.
		// See bids calculation method for details about how to extract the distances.
		int lowerBound = 0;
		for(int boxNo = 0; boxNo < board.boxCount; boxNo++) {
			if(board.boxData.isBoxActive(boxNo) && board.boxData.getBoxPosition(boxNo) != 0) {
				lowerBound += MAXIMUM_BOX_DISTANCE - (bids[boxNo][goalsMatchedToBoxes[boxNo]] / board.boxCount);
			}
		}

		return lowerBound;
	}

	/**
	 * Calculates the bids of every box for every goal. <br>
	 * The auction algorithm takes the distances of a box to the goals as "bids". The lower the distance
     * the higher the bid.
	 *
	 * @param searchDirection  direction of the search (forwards or backwards)
	 */
	private void calculateBids(SearchDirection searchDirection) {

		// The auction algorithm takes the distances of a box to the goals as "bids". The lower the distance
		// the higher the bid. To avoid circular matchings the algorithm must add an EPSILON values to the
		// prices of the goals. These additions mustn't interfere with the normal bid values. Since epsilon
		// is 1 and can only be added boxCount times we multiply every bid with the number of boxes. This way
		// epsilon doesn't interfere with the actual bids.
		for(int boxNo = 0; boxNo < board.boxCount; boxNo++) {
			for(int goalNo = 0; goalNo < board.goalsCount; goalNo++) {
				int boxDistance =  searchDirection == SearchDirection.FORWARD ?
						board.distances.getBoxDistanceForwardsNo(boxNo, goalNo) :
						board.distances.getBoxDistanceBackwardsNo(boxNo, goalNo);
				bids[boxNo][goalNo] =  boxDistance == Board.UNREACHABLE ?  NO_BID  :  (MAXIMUM_BOX_DISTANCE - boxDistance) * board.boxCount;
			}
		}
	}

	/**
	 * Calculates the bids of every box for every goal. <br>
	 * For the deadlock detection the real distances are irrelevant. Hence, the bid is either "minimum bid" or NO_BID,
	 * depending on whether a box can reach a specific goal or not.
	 * These bids are used in the auction algorithm to pair every box with a goal.
	 *
	 * @param searchDirection	direction of the search (forwards or backwards)
	 * @param goalsToBeExcluded boolean array indicating which goals are to be excluded by the bipartite matching. {@code true} means "exclude goal"
	 */
	private void calculateBidsForDeadlockDetection(SearchDirection searchDirection, boolean[] goalsToBeExcluded) {

		// The auction algorithm adds EPSILON at most boxCount times, hence the minimum
		// bid has to be higher in order EPSILON not to influence with the bids.
		final int MINIMUM_BID = board.boxCount * EPSILON + 1;

		for(int boxNo = 0; boxNo < board.boxCount; boxNo++) {
			for(int goalNo = 0; goalNo < board.goalsCount; goalNo++) {
				int boxDistance =  searchDirection == SearchDirection.FORWARD ?
						board.distances.getBoxDistanceForwardsNo(boxNo, goalNo) :
						board.distances.getBoxDistanceBackwardsNo(boxNo, goalNo);
				bids[boxNo][goalNo] =  boxDistance == Board.UNREACHABLE ?  NO_BID  :  MINIMUM_BID ;
			}
		}


		// If a goal has to be ignored treat it as unreachable.
		// The array is null in most cases, hence it's quicker to check
		// this here instead of in the above for loops.
		if (goalsToBeExcluded != null) {
			for(int goalNo = 0; goalNo < goalsToBeExcluded.length; goalNo++) {
				if(goalsToBeExcluded[goalNo] == true) {
					for(int boxNo = 0; boxNo < board.boxCount; boxNo++) {
						bids[boxNo][goalNo] = NO_BID;
					}
				}
			}
		}
	}


	/**
	 * An auction algorithm is performed, where the boxes "buy" the goals. Each box tries to buy the goal,
	 * which is most favorable for it (-> highest bid).<br>
	 * In the end we get a perfect minimal bipartite matching.<br>
	 * If no perfect matching can be found the board is a deadlock and can't be solved anymore.<br>
	 * The found matchings are stored in {@link #goalsMatchedToBoxes} and {@link #boxesMatchedToGoals}.
	 *
	 * @param searchDirection direction of the search (forwards or backwards)
	 * @return {@code true} when a perfect minimum bipartite matching has been found, <code>false</code> otherwise
	 */
	private boolean searchMinimumPerfectBipartiteMatching(SearchDirection searchDirection) {

		// Initialize arrays.
		Arrays.fill(goalsMatchedToBoxes, NOT_MATCHED);
		Arrays.fill(boxesMatchedToGoals, NOT_MATCHED);
		Arrays.fill(prices, 0);


		boxesToBeMatched.clear();

		// Add all boxes to the stack for searching goals for them.
		for (int boxNo = 0; boxNo < board.boxCount; boxNo++) {

			// Some boxes are logically removed from the board. These boxes don't have to be assigned to any goal.
			if(!board.boxData.isBoxInactive(boxNo) && board.boxData.getBoxPosition(boxNo) != 0) {

				// If the box is "frozen" it can just reach the goal it is currently located on.
				// The price for this goal must be set so high that no other box can "steal" it from this box.
				if(board.boxData.isBoxFrozen(boxNo) && searchDirection == SearchDirection.FORWARD) {    // Only forward search can used "frozen" status!
					int boxPosition = board.boxData.getBoxPosition(boxNo);
					int goalNo = board.getGoalNo(boxPosition);
					goalsMatchedToBoxes[boxNo]  = goalNo;
					boxesMatchedToGoals[goalNo] = boxNo;
					prices[goalNo] = INFINITE;
				} else {
					// Add to stack for calculating a matching for it.
					boxesToBeMatched.add(boxNo);
				}
			}
		}

		// Inside the stack there are those boxes (their numbers), which currently are not paired with a goal.
		// As long as there is such a box, we have to continue the search for a perfect matching.
		while(!boxesToBeMatched.isEmpty()) {

			// Fetch next box number from the stack-
			int boxNo = boxesToBeMatched.remove();

			int bestGoalNo           = -1;
			int highestBenefit       = -1;
			int secondHighestBenefit = -1;

			// Search the goal with the highest "benefit".
			for(int goalNo = 0; goalNo < board.goalsCount; goalNo++) {

				if(bids[boxNo][goalNo] != NO_BID) {
					int benefit = bids[boxNo][goalNo] - prices[goalNo];
					if(benefit > highestBenefit) {
						secondHighestBenefit = highestBenefit;
						highestBenefit       = benefit;
						bestGoalNo           = goalNo;
					} else if(benefit > secondHighestBenefit) {
						secondHighestBenefit = benefit;
					}
				}
			}

			// If the box couldn't be assigned to any goal no perfect matching can be found.
			if(bestGoalNo == -1) {
				return false;
			}

			// If the "best" goal for the box is already assigned to another box then delete that assignment and
			// add that box to the queue, because a new assignment has to be searched for that box.
			int currentlyMatchedBox = boxesMatchedToGoals[bestGoalNo];
			if(currentlyMatchedBox != NOT_MATCHED) {
				goalsMatchedToBoxes[currentlyMatchedBox] = NOT_MATCHED;
				boxesToBeMatched.add(currentlyMatchedBox);
			}

			// Match the box with the goal.
			goalsMatchedToBoxes[boxNo] = bestGoalNo;
			boxesMatchedToGoals[bestGoalNo] = boxNo;

			// Another box has to pay at least the difference between highest and
			// the second highest benefit for "stealing" the goal from the current box.
			// If there is no alternative goal for the box (-> secondHighestBenefit is < 0),
			// then the price is set to "INFINITE" to ensure no other box can get this goal.
			// Epsilon is added to avoid circular assignments.
			if(secondHighestBenefit < 0) {
				prices[bestGoalNo] = INFINITE;
			}
			else {
				prices[bestGoalNo] += highestBenefit - secondHighestBenefit + EPSILON;
			}
		}

		/* We have found a minimum weight perfect bipartite matching. */
		return true;
	}
}
