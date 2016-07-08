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

import de.sokoban_online.jsoko.boardpositions.IBoardPosition;
import de.sokoban_online.jsoko.solver.AnySolution.SolverAnySolution;

/**
 * Interface for board positions used in the {@link SolverAnySolution}.
 */
public abstract interface IBoardPositionSolverAnySolution
		extends IBoardPosition, Comparable<IBoardPositionSolverAnySolution> {

	 /**
     * Returns the relevance of this board position for the search.
     *
     * @return the relevance
     */
    int getRelevance();

	/**
	 * Sets the relevance of this board position for the search.
	 *
	 * @param relevance the relevance to set (the higher the more relevant)
	 */
    void setRelevance(int relevance);

    /**
     * Returns the pushes lower bound of this board position.
     *
     * @return the pushes lower bound
     */
    int getPushesLowerBound();


	/**
	 * Sets the pushes lower bound of this board position.
	 *
	 * @param pushesLowerBound
	 */
	void setPushesLowerBound(int pushesLowerbound);

    /**
     * Returns the index in the packing sequence that has already been reached.
     *
     * @return index in the packing sequence
     */
    int getIndexPackingSequence();

	/**
	 * Sets the index in the packing sequence that has been reached.
	 *
	 * @param indexPackingSequence the index in the packing sequence
	 */
	void setIndexPackingSequence(int indexPackingSequence);

    /**
     * Returns the array containing the information which box is under the control
     * of the packing sequence.
     *
     * @return boolean array of bits indicating which box is controlled by the packing sequence
     */
	 boolean[] getPackingSequenceControlledBoxInformation();
}
