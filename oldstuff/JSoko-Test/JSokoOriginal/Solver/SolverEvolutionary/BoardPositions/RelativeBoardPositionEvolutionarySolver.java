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
package de.sokoban_online.jsoko.solver.solverEvolutionary.boardPositions;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.boardpositions.IBoardPosition;
import de.sokoban_online.jsoko.boardpositions.RelativeBoardPosition;


/**
 * Class for storing a board position by just storing the changes relative to a previous board position.
 */
public final class RelativeBoardPositionEvolutionarySolver extends RelativeBoardPosition implements IBoardPositionEvolutionarySolver {

	/** Relevance of this board position for the search. The higher the value the higher the relevance. */
	int relevance = 0;

	/** Pushes lower bound of this board position */
	int pushesLowerBound = 0;


	/**
	 * @param application reference to the main object
	 * @param boxNo		 number of the pushed box
	 * @param direction  direction of the push
	 * @param precedingBoardPosition previous board position
	 */
	public RelativeBoardPositionEvolutionarySolver(JSoko application, int boxNo, int direction, IBoardPosition precedingBoardPosition) {
		super(board, boxNo, direction, precedingBoardPosition);
	}

	/* (non-Javadoc)
	 * @see java.lang.Comparable#compareTo(java.lang.Object)
	 */
	@Override
	public int compareTo(IBoardPositionEvolutionarySolver o) {

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
	 * @param pushesLowerBound
	 */
	@Override
	public void setPushesLowerBound(int pushesLowerBound) {
		this.pushesLowerBound = pushesLowerBound;
	}
}

