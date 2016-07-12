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
package de.sokoban_online.jsoko.board;

import java.util.Arrays;

/**
 * Class that manages the data of all boxes on the board.
 * The methods of this class do not really compute much, they mainly record
 * the facts that are stated by method calls.
 */
final public class BoxData implements Cloneable {

	/** Reference to the board of the game. */
	private Board board;

	/** local copy of count of boxes in the level */
	private int boxCount;

	/** positions of the boxes */
	private int[] boxPositions;

	/** A "frozen" box is a box, which cannot be pushed any more
	 *  (except perhaps by undo).
	 */
	private boolean[] isBoxFrozen;

	/** Indicates whether the box is part of a corral */
	private boolean[] isBoxInCorral;

	/** Indicates whether the box is "inactive",
	 *  i.e. is not located on the board, anymore.
	 */
	private boolean[] isBoxInactive;

	/**
	 * Creates a new object for handling of the box data.
	 *
	 *@param board  the object holding the board of the game
	 */
	public BoxData(Board board) {
		this.board = board;

		boxCount = board.boxCount;
		boxPositions = new int[boxCount];

		isBoxFrozen   = new boolean[boxCount];
		isBoxInCorral = new boolean[boxCount];
		isBoxInactive = new boolean[boxCount];
	}

	/**
	 * Constructor for cloning.
	 *
	 *@param  boxData BoxData object to be cloned
	 */
	private BoxData(BoxData boxData) {
		boxPositions  = boxData.getBoxPositionsClone();
		isBoxInactive = boxData.isBoxInactive.clone();
		isBoxInCorral = boxData.isBoxInCorral.clone();
		isBoxFrozen   = boxData.isBoxFrozen.clone();
		board		  = boxData.board;
		boxCount	  = boxData.boxCount;
	}

	/**
	 * Creation of a box data clone.
	 *
	 *@return  the cloned box data object
	 */
	@Override
	final public Object clone() {
		return new BoxData(this);
	}

	/**
	 * Define the initial location (position) of a box.
	 *
	 *@param boxNo       number of the box, the location of which is to be set
	 *@param boxPosition position of the box
	 */
	final public void setBoxStartPosition(int boxNo, int boxPosition) {
		boxPositions[boxNo] = boxPosition;
	}

	/**
	 * Change the location (position) of a box.
	 *
	 *@param boxNo		 number of the box, the location of which is to be set
	 *@param boxPosition new position of the box
	 */
	final public void setBoxPosition(int boxNo, int boxPosition) {
		boxPositions[boxNo] = boxPosition;
	}

	/**
	 * Set / change the position of all boxes at once.
	 * The passed array often contains an additional player position
	 * (at the end of the array).  We don't care much, the array is
	 * just one larger than necessary.
	 *
	 *@param  newBoxPositions the new box positions to be set
	 */
	final public void setBoxPositions(int[] newBoxPositions) {

		// We must use a copy of the the array!
		// The caller does not donate this object, he will continue
		// to use and modify that array, while we expect to own this array.
		boxPositions = newBoxPositions.clone();
	}

	/**
	 * Activate a box.
	 *
	 *@param  boxNo number of the box that shall be activated
	 */
	final public void setBoxActive(int boxNo) {
		isBoxInactive[boxNo] = false;
	}

	/**
	 * Deactivate a box.
	 * Implies {@link #removeBoxFromCorral(int)}.
	 *
	 *@param  boxNo number of the box that shall be set inactive
	 */
	final public void setBoxInactive(int boxNo) {
		isBoxInactive[boxNo] = true;

		// An inactive box cannot be part of a corral
		isBoxInCorral[boxNo] = false;
	}

	/**
	 * Tell whether a box is active.
	 *
	 *@param  boxNo number of the box we want to investigate
	 *@return state of the box: <code>true</code> = is active
	 */
	final public boolean isBoxActive(int boxNo) {
		return isBoxInactive[boxNo] == false;
	}

	/**
	 * Tell whether a box is inactive.
	 *
	 *@param  boxNo number of the box we want to investigate
	 *@return state of the box: <code>true</code> = is inactive
	 */
	final public boolean isBoxInactive(int boxNo) {
		return isBoxInactive[boxNo];
	}

	/**
	 * Mark a specified box to be frozen.
	 * <p>
	 * Such a box can never again be moved (pushed), regardless all other
	 * possible changes on the board... except for an "undo" action.
	 * <p>
	 * Only boxes on goals are to be marked as frozen, as otherwise
	 * it would constitute a deadlock condition.
	 *
	 *@param  boxNo number of the box to be marked "frozen"
	 */
	final public void setBoxFrozen(int boxNo) {
		isBoxFrozen[boxNo] = true;
	}

	/**
	 * Tell whether a box is frozen.
	 * A box can be frozen on a goal, only, since otherwise it would be
	 * a deadlock condition.
	 *
	 *@param  boxNo number of the box we want to investigate
	 *@return       whether the box is frozen
	 */
	final public boolean isBoxFrozen(int boxNo) {
		return isBoxFrozen[boxNo];
	}

	/**
	 * Mark a specified box to not be frozen, anymore
	 * This method is used for "undo" (on a frozen box).
	 *
	 *@param  boxNo number of the box to be unmarked
	 */
	final public void setBoxUnfrozen(int boxNo) {
		isBoxFrozen[boxNo] = false;
	}

	/**
	 * Mark a specified box to be part of a corral.
	 *
	 *@param  boxNo number of the box which is part of a corral
	 */
	final public void setBoxInCorral(int boxNo) {
		isBoxInCorral[boxNo] = true;
	}

	/**
	 * Mark a specified box to not be part of any corral.
	 *
	 *@param  boxNo number of the box which is not part of any corral
	 */
	final public void removeBoxFromCorral(int boxNo) {
		isBoxInCorral[boxNo] = false;
	}

	/**
	 * Tell whether a box is part of a corral.
	 *
	 *@param  boxNo number of the box we want to investigate
	 *@return <code>true</code> = box is part of a corral
	 */
	final public boolean isBoxInCorral(int boxNo) {
		return isBoxInCorral[boxNo];
	}

	/**
	 * Tell the location of a box by its number.
	 *
	 *@param  boxNo number of the box we want to investigate
	 *@return       position (location) of the box
	 */
	final public int getBoxPosition(int boxNo) {
		return boxPositions[boxNo];
	}

	/**
	 * Create a clone of our content.
	 *
	 *@return a new clone of our box position array
	 */
	final public int[] getBoxPositionsClone() {
		return boxPositions.clone();
	}

	/**
	 * Tells whether all our boxes are on some goal.
	 *
	 *@return <code>true</code> if all boxes are on a goal, and
	 *       <code>false</code> if at least one box is on a non-goal
	 */
	final public boolean isEveryBoxOnAGoal() {

		// Check whether all "active" boxes are on a goal.
		for (int boxNo = 0; boxNo < boxCount; boxNo++) {
			if (!isBoxInactive(boxNo) && !board.isBoxOnGoal(boxPositions[boxNo])) {
				return false;
			}
		}
		return true;
	}

	/**
	 * Returns the number of boxes on a goal.
	 *
	 * @return number of boxes on a goal
	 */
	public int getBoxesOnGoalsCount() {
		int boxesOnGoalsCount = 0;
		for(int boxNo=0; boxNo<boxCount; boxNo++) {
			if(isBoxActive(boxNo) && board.isBoxOnGoal(boxPositions[boxNo])) {
				boxesOnGoalsCount++;
			}
		}
		return boxesOnGoalsCount;
	}

	/**
	 * Returns whether all active boxes are located on a backward goal.
	 *
	 * @return <code>true</code> if all active boxes are on backward goals, and
	 * <code>false</code> if at least one active box not on a backward goal.
	 */
	final public boolean isEveryBoxOnABackwardGoal() {

		int[] backwardGoalPositions = board.getGoalPositionsBackward();

		for (int boxNo = 0; boxNo < boxCount; boxNo++) {
			if (isBoxInactive(boxNo)) {
				continue;
			}
			if (board.isBox(backwardGoalPositions[boxNo]) == false) {
				return false;
			}
		}
		return true;
	}

	/**
	 * Tell whether all corral boxes are located on goals.
	 *
	 *@return <code>true</code> if all corral boxes are on goals, and
	 *		 <code>false</code> if at least one corral box is on a non-goal
	 */
	final public boolean isEveryCorralBoxOnAGoal() {

		// check all boxes
		for (int boxNo = 0; boxNo < boxCount; boxNo++) {
			// ignore deactivated and non-corral boxes
			if (isBoxInactive(boxNo) || isBoxInCorral(boxNo) == false) {
				continue;
			}
			if (board.isBoxOnGoal(boxPositions[boxNo]) == false) {
				return false;
			}
		}
		return true;
	}

	/**
	 * Mark all boxes as not frozen.
	 */
	final public void setAllBoxesNotFrozen() {
		Arrays.fill(isBoxFrozen, false);
	}
}