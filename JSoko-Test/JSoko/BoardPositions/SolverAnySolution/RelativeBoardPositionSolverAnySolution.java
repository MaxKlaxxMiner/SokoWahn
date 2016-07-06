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
package de.sokoban_online.jsoko.boardpositions.solverAnySolution;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.boardpositions.IBoardPosition;
import de.sokoban_online.jsoko.boardpositions.RelativeBoardPosition;

/**
 * Class for storing a board position by just storing the changes relative to a previous
 * board position.
 */
public final class RelativeBoardPositionSolverAnySolution
		extends RelativeBoardPosition implements IBoardPositionSolverAnySolution {

	/**
	 * Relevance of this board position for the search.
	 * The higher the value the higher the relevance.
	 */
	int relevance = 0;

	/** Pushes lower bound of this board position */
	int pushesLowerBound = 0;

	/**
	 * Packing sequence index. This number represents the index in the packing sequence that has already been reached.
	 * Hence, 5 means: the first 5 steps of the packing sequence have already been done.
	 */
	private int indexPackingSequence = -1;

	/**
	 * Flags, indicating which box is located at a square relevant for the packing sequence. Explanation: If the packing
	 * sequence needs a box parked at square 10 and box number 3 is located at square 10, then isBoxAtPackingSequenceSquare[3]
	 * is true.
	 */
	boolean[] isBoxControlledByPackingSequence;

	/**
	 * @param application reference to the main object
	 * @param boxNo		 number of the pushed box
	 * @param direction  direction of the push
	 * @param packingSequenceIndex  index in the packing sequence
	 * @param isBoxAtPackingSequenceSquare boolean array indicating which boxes are under the control of the packing sequence
	 * @param precedingBoardPosition previous board position
	 */
	public RelativeBoardPositionSolverAnySolution(JSoko application,
			int boxNo, int direction, int packingSequenceIndex,
			boolean[] isBoxAtPackingSequenceSquare,
			IBoardPosition precedingBoardPosition) {
		super(board, boxNo, direction, precedingBoardPosition);

		setIndexPackingSequence(packingSequenceIndex);

		this.isBoxControlledByPackingSequence = isBoxAtPackingSequenceSquare;

	}

	/* (non-Javadoc)
	 * @see java.lang.Comparable#compareTo(java.lang.Object)
	 */
	@Override
	public int compareTo(IBoardPositionSolverAnySolution o) {

		// The priority queue returns the lowest value first. But the highest relevance should be best for the search.
		// Hence, subtract the current relevance from the one of the passed board position.

		return o.getRelevance() - getRelevance();
	}

	/**
	 * Returns the relevance of this board position for the search.
	 *
	 * @return the relevance
	 */
	@Override
	public final int getRelevance() {
		return relevance;
	}

	/**
	 * Sets the relevance of this board position for the search.
	 *
	 * @param relevance the relevance to set (the higher the more relevant)
	 */
	@Override
	public final void setRelevance(int relevance) {
		this.relevance = relevance;
	}

	/**
	 * Returns the pushes lower bound of this board position.
	 *
	 * @return the pushes lower bound
	 */
	@Override
	public int getPushesLowerBound() {
		return pushesLowerBound;
	}

	/**
	 * Sets the pushes lower bound of this board position.
	 *
	 * @param pushesLower bound
	 */
	@Override
	public void setPushesLowerBound(int pushesLowerBound) {
		this.pushesLowerBound = pushesLowerBound;
	}

	/**
	 * Returns the index in the packing sequence that has already been reached.
	 *
	 * @return index in the packing sequence
	 */
	@Override
	public int getIndexPackingSequence() {
		return indexPackingSequence;
	}

	/**
	 * Sets the index in the packing sequence that has been reached.
	 *
	 * @param indexPackingSequence the index in the packing sequence
	 */
	@Override
	public void setIndexPackingSequence(int indexPackingSequence) {
		this.indexPackingSequence = indexPackingSequence;
	}

	/**
	 * Returns the array containing the information which box is under the control of the packing sequence.
	 *
	 * @return boolean array of bits indicating which box is controlled by the packing sequence
	 */
	@Override
	public boolean[] getPackingSequenceControlledBoxInformation() {
		return isBoxControlledByPackingSequence;
	}
}
