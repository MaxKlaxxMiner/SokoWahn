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

import de.sokoban_online.jsoko.board.DirectionConstants;
import de.sokoban_online.jsoko.leveldata.solutions.Solution;
import de.sokoban_online.jsoko.utilities.Utilities;


/**
 * Class for holding the information of one solution.
 * This class is only used in the optimizer.
 */
public class OptimizerSolution implements Cloneable, DirectionConstants {
	
	/** LURD representation of the solution path in lower case format. */
	private String 	 lurd = null;

	/** Number of pushes of this solution. */
	public int 		 pushesCount;

	/** Number of moves of this solution. */
	public int 		 movesCount;

	/** Number of box lines of this solution. */
	public int	 	 boxLines;

	/** Number of box changes of this solution. */
	public int		 boxChanges;

	/** Number of pushing sessions of this solution. */
	public int		 pushingSessions;
	
//	/** Number of player lines of this solution. */
//	public int	 	 playerLines;


	/**
	 * Solution as byte array.
	 * Contains a direction per byte, and does not distinguish between moves and pushes.
	 */
	public byte[] solution = new byte[0]; 


	/**
	 * Creates an object for a solution.
	 */
	public OptimizerSolution() {}

	/**
	 * Creates a new solution object containing the passed lurd solution.  
	 * 
	 * @param sol solution from the main game.
	 */
	public OptimizerSolution(Solution sol) {

		pushesCount     = sol.pushesCount;
		movesCount      = sol.movesCount;
		boxLines        = sol.boxLines;
		boxChanges      = sol.boxChanges;
		pushingSessions = sol.pushingSessions;
		lurd            = sol.lurd.toLowerCase();
			
		solution = new byte[lurd.length()];

		// Convert the format of the solution.
		for(int i=0; i<lurd.length(); i++) {
			switch(lurd.charAt(i)) {
			case 'u':
			case 'U':
				solution[i] = UP;
				break;

			case 'd':
			case 'D':
				solution[i] = DOWN;
				break;

			case 'L':
			case 'l':
				solution[i] = LEFT;
				break;

			case 'R':
			case 'r':
				solution[i] = RIGHT;
				break;
			}
		}
	}

	/* (non-Javadoc)
	 * @see java.lang.Object#toString()
	 */
	public String toString() {
		return      movesCount
			+ "/" + pushesCount
			+ "/" + boxLines
			+ "/" + boxChanges
			+ "/" + pushingSessions;
	}
	
	
	/**
	 * Returns the solution as (lower case) lurd string.
	 * Note: The public "solution" byte array may have changed.
	 * Hence we have to create a new String from the current solution.
	 * 
	 * @return the lurd string of this solution
	 */
	public String getLURD() {	
		
		// Concatenate the solution lurd string.
		StringBuilder s = new StringBuilder();
		for (int moveNo=0; moveNo<movesCount; moveNo++) {
			switch ((solution[moveNo]) & 3) {
			case UP:
				s.append("u");
				break;
			case DOWN:
				s.append("d");
				break;
			case LEFT:
				s.append("l");
				break;
			case RIGHT:
				s.append("r");
				break;
			}
		}
		
		// As a side effect we remember the current lower case "lurd",
		// so the solution as String stays in sync with the "solution"-array 
		// representing the moves of the player.
		lurd = s.toString();
		
		return lurd;
	}


	/* (non-Javadoc)
	 * @see java.lang.Object#equals(java.lang.Object)
	 */
	public boolean equals(Object compareObject) {
		if(this == compareObject)
			return true;

		if(compareObject == null)
			return false;

		if(getClass() != compareObject.getClass())
			return false;

		final OptimizerSolution other = (OptimizerSolution) compareObject;

		// The LURD string must be equal. The solution may have 
		// changed, hence we have to use "getLURD" instead of taking
		// the "lurd"-string directly.
		if(!getLURD().equals(other.getLURD()))
			return false;

		return true;
	}
	
	@Override
	public int hashCode() {
		return lurd.hashCode();
	}

	/**
	 * Creates a new <code>OptimizerSolution</code> with the same content as this one.
	 * 
	 * @return the new clone
	 */
	public OptimizerSolution clone() {
		        
		OptimizerSolution cloneSolution;
			try {
				cloneSolution = (OptimizerSolution) super.clone();
			} catch (CloneNotSupportedException e) {
				throw new InternalError();
			}
			if (solution != null) {
				cloneSolution.solution = solution.clone();
			} 
        
    	// Return with the clone.
		return cloneSolution;
	}
	
	/**
	 * Determine, whether this solution is better than the passed solution,
	 * with respect to moves and then pushes.
	 * 
	 * @param other the other solution we compare against
	 * @return whether this solution is better
	 */
	public boolean isBetterMovesPushesThan(OptimizerSolution other ) {
		if (other == null) {
			return true;			// everybody is better than nobody
		}
		return Utilities.intCompare2Pairs( movesCount , other.movesCount,
		                                   pushesCount, other.pushesCount )
		     < 0 ;
	}
	
	/**
	 * Determine, whether this solution is better than the passed solution,
	 * with respect to pushes and then moves.
	 * 
	 * @param other the other solution we compare against
	 * @return whether this solution is better
	 */
	public boolean isBetterPushesMovesThan(OptimizerSolution other ) {
		if (other == null) {
			return true;			// everybody is better than nobody
		}
		return Utilities.intCompare2Pairs( pushesCount, other.pushesCount,
		                                   movesCount , other.movesCount  )
		     < 0 ;
	}
	
	
	/**
	 * Determine, whether this solution is better than the passed solution,
	 * with respect to:<br>
	 * 1. moves<br> 
	 * 2. pushes<br> 
	 * 3. box lines<br>
	 * 4. box changes<br>
	 * 5. pushing sessions<br>
	 * 
	 * @param other the other solution we compare against
	 * @return whether this solution is better or not
	 */
	public boolean isBetterMovesPushesAllMetricsThan(OptimizerSolution other ) {
		if (other == null) {
			return true;			// everybody is better than nobody
		}
		// Return the compare result.
		return Utilities.intComparePairs(
				movesCount, 	 other.movesCount,
				pushesCount, 	 other.pushesCount,
				boxLines, 		 other.boxLines,
				boxChanges, 	 other.boxChanges,
				pushingSessions, other.pushingSessions) < 0;
	}
	
	/**
	 * Determine, whether this solution is better than the passed solution,
	 * with respect to:<br>
	 * 1. pushes<br> 
	 * 2. moves<br> 
	 * 3. box lines<br>
	 * 4. box changes<br>
	 * 5. pushing sessions<br>
	 * 
	 * @param other the other solution we compare against
	 * @return whether this solution is better or not
	 */
	public boolean isBetterPushesMovesAllMetricsThan(OptimizerSolution other ) {
		if (other == null) {
			return true;			// everybody is better than nobody
		}
		// Return the compare result.
		return Utilities.intComparePairs(
				pushesCount, 	 other.pushesCount,
				movesCount, 	 other.movesCount,
				boxLines, 		 other.boxLines,
				boxChanges, 	 other.boxChanges,
				pushingSessions, other.pushingSessions) < 0;
	}
	
}