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
package de.sokoban_online.jsoko.leveldata;

import java.util.ArrayList;

import de.sokoban_online.jsoko.board.DirectionConstants;



/**
 * This class stores the performed moves / pushes, in order to
 * allow an "undo" operation.
 * Here, a "movement" contains some moving of the player and/or
 * some moving of a box.  Has no box been moved, we put a -1
 * into the movement vector as a dummy for the box.
 */
public final class History implements DirectionConstants {

	// vector, holding movements for the undo/redo operations
	private ArrayList<HistoryElement> movesHistory = new ArrayList<HistoryElement>();

	// All performed movements are stored in a vector, to be able
	// to take them back by "undo".
	// The variable "currentMoventNo" indicates the current position within
	// this vector.  We need that for undo undone movements (aka "redo").
	private int currentMovementNo = -1;


	/**
	 * Creates a new object for holding all history data.
	 */
	public History() { }

	/**
	 * Inserts a move of the player into the history.
	 *
	 * @param direction	into which the player moved
	 */
	final public void addPlayerMove(int direction) {
		addMovement(direction, -1);
	}

	/**
	 * Inserts a movement into the history, at (behind) the "current" point.
	 *
	 * @param direction   into which the player moved
	 * @param pushedBoxNo <code>-1</code>, or the box number pushed by this movement
	 */
	final public void addMovement(int direction, int pushedBoxNo) {

		HistoryElement currentMovement = new HistoryElement(direction, pushedBoxNo);

		// If the new move equals the next move from the old history, the user "redoes"
		// a formerly "undone" move.  In that case we just advance the "current" pointer.
		if (currentMovementNo + 1 < movesHistory.size()) {
			HistoryElement nextMovement = movesHistory.get(currentMovementNo + 1);
			if (nextMovement.equals(currentMovement)) {

				currentMovementNo++;

				return;
			}
		}

		// The user has done a new movement.  Therefore the rest of the history is not
		// relevant anymore, we just create a new branch.  Hence we delete the old rest,
		// and append the new movement entry.
		while (currentMovementNo + 1 < movesHistory.size()) {
			//movementHistory.remove(currentMovementNo + 1);
			// We remove the last entry first, to avoid unnecessary copy-down operations
			movesHistory.remove(movesHistory.size() - 1);
		}

		// Enter new move
		currentMovementNo++;
		if (currentMovementNo >= movesHistory.size()) {
			movesHistory.add(currentMovement);
		} else {
			movesHistory.add(currentMovementNo, currentMovement);
		}
	}

	/**
	 * Returns the next movement from the history,
	 * and advances the "current" pointer to it.
	 *
	 * @return <code>null</code>, or the <code>HistoryElement</code> containing the data
	 *         for the next movement of the history
	 */
	final public HistoryElement getSuccessorMovement() {
		return hasSuccessorMovement() ? movesHistory.get( ++currentMovementNo ) : null;
	}

	/**
	 * Returns the previously executed movement from the history,
	 * and moves back the "current" pointer before it.
	 *
	 * @return <code>null</code>, or the <code>HistoryElement</code> containing the data
	 *         for the previously executed movement
	 */
	final public HistoryElement getPrecedingMovement() {
		return hasPrecedingMovement() ? movesHistory.get( currentMovementNo-- ) : null;
	}

	/**
	 * Returns the movement with the given number.
	 *
	 * @param movementNo number of the movement to be returned
	 * @return  <code>null</code>, or the <code>HistoryElement</code> of the movement
	 *          with the given number
	 */
	final public HistoryElement getMovement(int movementNo) {

		if (movementNo < 0 || movementNo >= movesHistory.size()) {
			return null;
		}
		return movesHistory.get(movementNo);
	}

	/**
	 * Returns the index of the last executed movement.
	 *
	 * @return index of the last executed movement
	 */
	final public int getCurrentMovementNo() {
		return currentMovementNo;
	}

	/**
	 * Returns a shallow copy of the movement history.
	 *
	 * @return a shallow copy of the movement history
	 */
	final public ArrayList<HistoryElement> getMovementHistoryClone() {
		return new ArrayList<HistoryElement>(movesHistory);
	}

	/**
	 * Returns the total count of the stored movements.
	 *
	 * @return total count of movements
	 */
	final public int getMovementsCount() {
		return movesHistory.size();
	}

	/**
	 * Returns the (total) number of pushes contained in the history.
	 *
	 * @return number of pushes in the history
	 */
	final public int getPushesCount() {

		int pushesCount = 0;

		for (HistoryElement historyElement : movesHistory) {
			if (historyElement.isPush()) {
				pushesCount++;
			}
		}
		return pushesCount;
	}

	/**
	 * Returns a segment of the LURD string for this history.
	 *
	 * @param movementNoFrom  first movement to be contained in the LURD string
	 * @param movementNoTo    last  movement to be contained in the LURD string
	 * @return LURD string for selected segment
	 * @see #getLURDFromHistory()
	 * @see #getLURDFromHistoryTotal()
	 */
	final public String getLURDFromHistory(int movementNoFrom, int movementNoTo) {

		// LURD characters for Up, Down, Left and Right (English initials)
		final char[] playerLURD = { 'u', 'd', 'l', 'r' };
		final char[]    boxLURD = { 'U', 'D', 'L', 'R' };

		// Here we assembly the String we will return
		StringBuilder lurdString = new StringBuilder();

		HistoryElement currentMovement = null;

		for (int index = movementNoFrom; index <= movementNoTo; index++) {
			currentMovement = movesHistory.get(index);
			if (currentMovement.isPush()) {
				lurdString.append(   boxLURD[currentMovement.direction]);
			} else {
				lurdString.append(playerLURD[currentMovement.direction]);
			}
		}

		return lurdString.toString();
	}

	/**
	 * Returns the LURD string of all movements up to the current movement.
	 *
	 * @return LURD string till the current movement (inclusive)
	 * @see #getLURDFromHistory(int, int)
	 */
	final public String getLURDFromHistory() {
		return getLURDFromHistory(0, currentMovementNo);
	}

	/**
	 * Returns the complete LURD string of all movements, independent from the
	 * current point inside of the history.
	 * <p>
	 * Example: the moves have been uulldd then the user has undone one move.
	 * {@link #getLURDFromHistory()} would return: uulld <br>
	 * However this method returns all stored history movements.
	 *
	 * @return LURD string till the end (inclusive)
	 * @see #getLURDFromHistory(int, int)
	 */
	final public String getLURDFromHistoryTotal() {
		return getLURDFromHistory(0, getMovementsCount() - 1);
	}

	/**
	 * Returns the complete LURD string of all movements, independent from the
	 * current point inside of the history.<br>
	 * Additionally to the method {@link #getLURDFromHistoryTotal()} this method
	 * marks the current point in the history by adding a "*" after the current movement.
	 * <p>
	 * Example: the moves have been uulldd, then the user has undone one move.
	 * This method will then return: uulld*d
	 *
	 * @return LURD string till the end (inclusive) with a "*" after the current movement
	 * @see #getLURDFromHistoryTotal()
	 */
	public String getHistoryAsSaveGame() {

		StringBuilder historyLURD = new StringBuilder(getLURDFromHistoryTotal());

		if (historyLURD.length() > 0) {
			// If the current position in the history is not the end of the history, mark the current position with a "*".
			if (getCurrentMovementNo() != getMovementsCount()-1) {
				historyLURD.insert(getCurrentMovementNo() + 1, "*");
			}
		}

		return historyLURD.toString();
	}


	/**
	 * Sets the history to the first movement.
	 */
	final public void setHistoryToStart() {
		currentMovementNo = -1;
	}

	/**
	 * Sets the "current" pointer to the specified index.
	 * This changes to reference point for further "undo" and "redo" operations.
	 * This is the way to quickly jump to an arbitrary point in the history.
	 *
	 * @param movementNo the movement number to be set
	 */
	final public void setMovementNo(int movementNo) {
		currentMovementNo = movementNo;
	}

	/**
	 * Sets back the "current" pointer to the previous movement.
	 * This operation is unchecked / unconditional.
	 */
	final public void goToPrecedingMovement() {
		currentMovementNo--;
	}

	/**
	 * Sets forward the "current" pointer to the next movement.
	 * This operation is unchecked / unconditional.
	 */
	final public void goToNextMovement() {
		currentMovementNo++;
	}

	/**
	 * Returns whether there is a preceding movement.
	 *
	 * @return <code>true</code>, if there is a  preceding movement,<br>
	 *   	  <code>false</code>, if there is no preceding movement
	 */
	final public boolean hasPrecedingMovement() {
		return currentMovementNo >= 0; // at index 0 the first movement is stored which can also be undone!
	}

	/**
	 * Returns whether there is a successor movement.
	 *
	 * @return <code>true</code> if there is a  successor movement,<br>
	 * 		  <code>false</code> if there is no successor movement
	 */
	final public boolean hasSuccessorMovement() {
		return (currentMovementNo + 1) < movesHistory.size();
	}

	/**
	 * Returns the length of the previous combined movement in moves. A combined movement
	 * ends when the current box is pushed for the last time before another box is pushed.
	 *
	 * @return length of the last combined movement
	 */
	final public int getPreviousCombinedMovementLength() {

		int movesCount = 0;
		int movesToLastPushedBox = -1;
		int pushedBoxNo = -1;

		// Calculate the number of moves that have been done to push the same box.
		for( int moveNo = currentMovementNo; moveNo >= 0 ; --moveNo ) {

			HistoryElement histElem = getMovement(moveNo);

			movesCount++;

			if(histElem.isPush()) {
				if(pushedBoxNo == -1) {
					pushedBoxNo = histElem.pushedBoxNo;
				}
				else {
					if(pushedBoxNo != histElem.pushedBoxNo) {
						return movesToLastPushedBox;
					}
				}
				movesToLastPushedBox = movesCount;
			}
		}

		return movesCount;
	}

	/**
	 * Returns the length of the next combined movement in moves. A combined movement
	 * ends when the current box is pushed for the last time before another box is pushed.
	 *
	 * @return length of the next combined movement
	 */
	final public int getNextCombinedMovementLength() {

		int movesCount = 0;

		int movesToLastPushedBox = -1;
		int pushedBoxNo = -1;

		// Calculate the number of moves that have been done to push the same box.
		for(int moveNo = currentMovementNo+1; moveNo < movesHistory.size() ;moveNo++) {

			HistoryElement histElem = getMovement(moveNo);

			movesCount++;

			if(histElem.isPush()) {
				if(pushedBoxNo == -1) {
					pushedBoxNo = histElem.pushedBoxNo;
				}
				else {
					if(pushedBoxNo != histElem.pushedBoxNo) {
						return movesToLastPushedBox;
					}
				}
				movesToLastPushedBox = movesCount;
			}
		}

		return movesCount;
	}

	/**
	 * Returns the movement number of the passed "history browser"-movement.
	 * <p>
	 * A history browser starts when a box is pushed and ends when no box
	 * or another box is pushed.
	 *
	 * @param historyBrowserMovementNo the number of the history browser
	 *                      movement whose movement number is to be returned
	 * @return the movement number
	 */
	final public int getMovementNoOfHistoryBrowserMovement(int historyBrowserMovementNo) {

		int combinedMovementsCount = 0;  // Number of combined movement that contain at least one push.
		int lastPushedBoxNumber = -1;
		int movementNo = 0;

		if(historyBrowserMovementNo == 0) {
			return 0;
		}

		for (movementNo = 0; movementNo < movesHistory.size(); movementNo++) {

			HistoryElement historyElement = movesHistory.get(movementNo);

			// If it's a push but not of the same box as before then increase the number of found "history browser"-movements
			// and break if the target movement has been reached.
			if (historyElement.isPush() && historyElement.pushedBoxNo != lastPushedBoxNumber) {
				if (++combinedMovementsCount == historyBrowserMovementNo) {
					// All same box pushes also belong to the current history browser movement.
					// Hence, break when a none same box push comes next.
					for (; movementNo < movesHistory.size() - 1; movementNo++) {
						lastPushedBoxNumber = historyElement.pushedBoxNo;
						historyElement = movesHistory.get(movementNo + 1);
						if (historyElement.pushedBoxNo != lastPushedBoxNumber) {
							break;
						}
					}
					break;
				}
			}
			lastPushedBoxNumber = historyElement.pushedBoxNo;
		}

		// Return the determined movement number.
		return movementNo;
	}

	/**
	 * Returns the number of moves and pushes corresponding to the passed
	 * history movement number.
	 * <p>
	 * A history browser starts when a box is pushed and ends when no box
	 * is pushed or another box is pushed.
	 *
	 * @param historyBrowserMovementNo the number of the history browser
	 *                      movement whose movement number is to be returned
	 * @return the number of moves and pushes
	 */
	final public int[] getMovesPushesFromHistoryBrowserMovementNo(int historyBrowserMovementNo) {

		// Number of combined movement that contain at least one push.
		int combinedPushMovementCount = 0;

		// Current movement number in the loop.
		int movementNo = 0;

		// Number of pushes and moves.
		int pushesCount = 0, movesCount = 0;

		// Last pushed box number.
		int lastPushedBoxNumber = -1;

		HistoryElement historyElement;

		// 0 always means 0 moves and 0 pushes.
		if(historyBrowserMovementNo == 0) {
			return new int[] {0,0};
		}

		// Loop through all movements.
		for (movementNo = 0; movementNo < movesHistory.size(); movementNo++) {

			// Get the next history element.
			historyElement = movesHistory.get(movementNo);

			if(historyElement.isPush()) {
				pushesCount++;
			}
			movesCount++;

			// If it's a push but not of the same box as before then increase
			// the number of found "history browser"-movements
			// and break if the target movement has been reached.
			if (historyElement.isPush() && historyElement.pushedBoxNo != lastPushedBoxNumber) {
				if (++combinedPushMovementCount == historyBrowserMovementNo) {

					// All same box pushes also belong to the current history browser movement.
					// Hence, break when a none same box push comes next.
					for (; movementNo < movesHistory.size() - 1; movementNo++) {
						lastPushedBoxNumber = historyElement.pushedBoxNo;
						historyElement = movesHistory.get(movementNo + 1);
						if (historyElement.pushedBoxNo != lastPushedBoxNumber) {
							break;
						}
						movesCount++;
						pushesCount++;
					}
					break;
				}
			}
			lastPushedBoxNumber = historyElement.pushedBoxNo;
		}

		// Return the determined movement number.
		return new int[] {movesCount, pushesCount};
	}


	/**
	 * Returns the number of the history browser movement the current movement
	 * belongs to.
	 * <p>
	 * A history browser starts when a box is pushed and ends when no box
	 * or another box is pushed.
	 *
	 * @return the movement number
	 */
	final public int getHistoryBrowserMovementNoFromMovementNo() {

		// Number of combined movement that contain at least one push.
		int historyBrowserMovementCount = 0;

		// Current movement number in the loop.
		int movementNo = 0;

		// Last pushed box number.
		int lastPushedBoxNumber = -1;

		HistoryElement historyElement;

		// Loop through all movements.
		for (movementNo = 0; movementNo < movesHistory.size(); movementNo++) {

			// Break when the passed movement number has been reached.
			// Must be placed here because currentMovementNo may be -1 (=no moves made)!
			if (movementNo == currentMovementNo + 1) {
				break;
			}

			// Get the next history element.
			historyElement = movesHistory.get(movementNo);

			// If it's a push but not of the same box as before then increase the number of found "history browser"-movements
			// and break if the target movement has been reached.
			if (historyElement.isPush() && historyElement.pushedBoxNo != lastPushedBoxNumber) {
				historyBrowserMovementCount++;
			}

			lastPushedBoxNumber = historyElement.pushedBoxNo;
		}

		// Return the determined movement number.
		return historyBrowserMovementCount;
	}

	/**
	 * Returns the number of "history browser"-movements in the history.
	 * <p>
	 * A history browser starts when a box is pushed and ends when a no box
	 * or another box is pushed.
	 *
	 * @return the number of combined movements in the history
	 */
	final public int getHistoryBrowserMovementsCount() {

		// Number of combined movement that contain at least one push.
		int historyBrowserMovementsCount = 0;

		// Last pushed box number.
		int lastPushedBoxNumber = -1;

		for(HistoryElement historyElement : movesHistory) {
			// If it's a push but not of the same box as before then increase the number of found "history browser"-movements.
			if (historyElement.isPush() && historyElement.pushedBoxNo != lastPushedBoxNumber) {
				historyBrowserMovementsCount++;
			}

			lastPushedBoxNumber = historyElement.pushedBoxNo;
		}

		return historyBrowserMovementsCount;
	}
}