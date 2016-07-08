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
package de.sokoban_online.jsoko.leveldata.solutions;

import java.util.Comparator;
import java.util.Date;

import de.sokoban_online.jsoko.resourceHandling.Settings;
import de.sokoban_online.jsoko.utilities.Utilities;



/**
 * Holds the information of one solution.
 * Contains mainly the data of a solution as it is found in the DB.
 * <p>
 * A solution is unique with respect to its lurd representation.
 */
public class Solution {

	/** Solution information (public for easier access). */

	/**
	 * Flag specifying whether this solution has been proven to be valid.
	 * For better performance new solutions are just stored without doing a verification.
	 * First when the solution is read it is verified.
	 */
	public boolean isSolutionVerified = false;

	/**
	 * Database ID of this solution, or -1 if it is not (yet) found in the DB.
	 */
	public int 		 databaseID = -1;

	/** Solution name. */
	public String 	 name = "";

	/** Flag indicating whether this solution is an own solution. */
	public boolean   isOwnSolution = false;

	/** LURD representation of the solution path. */
	public String 	 lurd = "";

	/** Main metric: number of pushes of this solution. */
	public int 		 pushesCount;

	/** Main metric: number of moves of this solution. */
	public int 		 movesCount;

	/** Secondary metric: number of box lines of this solution. */
	public int	 	 boxLines;

	/** Secondary metric: number of box changes of this solution. */
	public int		 boxChanges;

	/** Secondary metric: number of pushing sessions of this solution. */
	public int		 pushingSessions;

//	/** Secondary metric: number of player lines of this solution. */
//	public int	 	 playerLines;

	/** Comment attached to this solution. */
	public String	 comment = "";

	/** Order information to display the solution in a specific order. */
	public int 		 orderValue = 0;

	/** Data for highlighting a solution in the GUI. */
	public String 	 highLightData = "";

	/**
	 * Last time this solution values have been changed.
	 * This is a point in time (either a Date or a Timestamp).
	 * Its string representation depends on the current locale,
	 * which may change during runtime.
	 */
	private Date lastChanged = null;

	/**
	 * A sequential number determined by the constructor, and used as an
	 * extension to {@link #lastChanged} date, as a tie break for equal dates.
	 */
	private final long seqNumber;

	/**
	 * A global source for new unique values for {@link #seqNumber}.
	 * @see #nextSeqNumber()
	 */
	private static long lastSeqNumber = 0;


	/**
	 * Comparator for comparing two solutions regarding first moves.
	 * We use this for solution sorting in {@link SolutionsGUI}.
	 */
	static final Comparator<Solution> MOVES_COMPARATOR =
		new Comparator<Solution>() {
		@Override
		public int compare(Solution solution1, Solution solution2) {
			int compareResult = solution1.movesCompare(solution2);
			if (compareResult != 0) {
				return compareResult;
			}

			return compareDateRest(solution1, solution2);
		}
	};

	/**
	 * Comparator for comparing two solutions regarding first pushes.
	 * We use this for solution sorting in {@link SolutionsGUI}.
	 */
	static final Comparator<Solution> PUSHES_COMPARATOR =
		new Comparator<Solution>() {
		@Override
		public int compare(Solution solution1, Solution solution2) {
			int compareResult = solution1.pushesCompare(solution2);
			if (compareResult != 0) {
				return compareResult;
			}

			return compareDateRest(solution1, solution2);
		}
	};

	/**
	 * Comparator for comparing two solutions regarding their order value.
	 * The order value is an artificial value for ordering the solutions
	 * according to an order the user has set.
	 */
	static final Comparator<Solution> ORDER_VALUE_COMPARATOR =
		new Comparator<Solution>() {
		@Override
		public int compare(Solution solution1, Solution solution2) {
			int compareResult = Utilities.intCompare1Pair(
					solution1.orderValue, solution2.orderValue );
			if (compareResult != 0) {
				return compareResult;
			}

			// A comparator may only return 0 if the objects are really equal.
			// For example a TreeSet only uses the comparator value to check
			// for duplicates.
			return (solution1.lurd.compareTo(solution2.lurd));
		}
	};

	/**
	 * Determine the next unique value for {@link #seqNumber}.
	 * This must be synchronized to be reliably used in the constructor.
	 *
	 * @return the next sequence number
	 */
	private static synchronized long nextSeqNumber() {
		return ++lastSeqNumber;
	}

	/**
	 * Creates a new solution object containing the passed lurd solution.
	 *
	 * @param lurd	the lurd representation of the solution path
	 */
	public Solution(String lurd) {
		this.seqNumber = nextSeqNumber();
		this.lurd      = lurd;
	}


	/**
	 * Creates a new solution object containing the passed lurd solution.
	 *
	 * @param lurd	the lurd representation of the solution path
	 * @param solutionID ID on the database of this solution
	 */
	public Solution(String lurd, int solutionID) {
		this(lurd);
		this.databaseID = solutionID;
	}


	/**
	 * Compares two dates.  Null values compare equal.
	 * @param x first  date to compare (or null)
	 * @param y second date to compare (or null)
	 * @return 0, or comparison result from x and y
	 */
	private static int tryCompareDate(Date x, Date y) {
		if ((x != null) && (y != null)) {
			// NB: the method "equals" may be much cheaper than "compareTo"
			if (!x.equals(y)) {
				return x.compareTo(y);
			}
			return 0;		// equal
		}
		return 0;			// incomparable
	}

	/**
	 * Compares the date of two solutions (both not null).
	 * @param x first  solution to compare
	 * @param y second solution to compare
	 * @return  0, or comparison result from the dates of x and y
	 * @see #tryCompareDate(Date, Date)
	 */
	private static int tryCompareSolDate(Solution x, Solution y) {
		int cmp = tryCompareDate(x.lastChanged, y.lastChanged);
		if (cmp != 0) {
			return cmp;
		}

		// Equal dates: try to tie-break by the sequential construction number
		if (x.seqNumber != y.seqNumber) {
			return (x.seqNumber < y.seqNumber) ? -1 : +1;
		}

		return 0;		// should not happen
	}

	/**
	 * The typical rest of a solution "compare" function: checks for equal
	 * LURDs, tries to compare dates,
	 * @param x first  solution to compare
	 * @param y second solution to compare
	 * @return
	 */
	private static int compareDateRest(Solution x, Solution y) {
		// A comparator may only return 0 if the objects are really equal.
		// For example a TreeSet only uses the comparator value to check for
		// duplicates.
		// Note: identical solutions mustn't occur in JSoko. A specific lurd
		// must always correspond to one specific solution object!
		if(x.lurd.equals(y.lurd)) {
			return 0;
		}

		// The oldest solution wins.
		int cmp = tryCompareSolDate(x, y);
		if (cmp != 0) {
			return cmp;
		}

		// Solution2 is the already stored one and solution1 the new solution.
		// If they have identical metrics and time stamp then the old one wins.
		// This avoids that the marking of the currently best solutions jumps
		// to the new solution although the metrics are identical.
		// FFS/hm: this breaks the contract of Comparator!
		//return 1;
		return 0;
	}

	/**
	 * Returns whether this solution is a better moves solution,
	 * equal moves solution or worse moves solution than the passed one.
	 * The comparison looks at {@link #movesCount} and {@link #pushesCount},
	 * and according to the global setting, also at the minor metrics
	 * in their standard order.
	 *
	 * @param compareSolution solution to be compared with this solution, or null
	 * @return	<code>-1</code> if this solution is a better moves solution,<br>
	 *         <code>0</code> if this solution is an equally good moves solution
	 *         <code>1</code> if this solution is a better moves solution
	 */
	public int movesCompare(Solution compareSolution) {

		// Each non-null (we are non-null) is better than null (nothing).
		if(compareSolution == null) {
			return -1;
		}

		// Check for better moves, then better pushes on equal moves.
		// We end up with dictionary order comparison of
		// (a) 2 initial values
		// (b) optionally 3 further values
		// We are in fact better for a negative comparison result,
		// since that means, that we have the smaller values.
		int wecompare = Utilities.intCompare2Pairs(
				this.movesCount , compareSolution.movesCount,
				this.pushesCount, compareSolution.pushesCount );
		if (wecompare != 0) {
			// It is decided, already...
			return wecompare;
		}

		// If the other values aren't to be checked the current solution
		// just isn't better than the passed one.
		if (!Settings.checkAllMinorMetrics) {
			return 0;
		}

		// The first 2 were equal, so check the remaining 3 ...
		return Utilities.intCompare3Pairs(
					this.boxLines       , compareSolution.boxLines,
					this.boxChanges     , compareSolution.boxChanges,
					this.pushingSessions, compareSolution.pushingSessions);
	}


	/**
	 * Returns whether this solution is a better pushes solution,
	 * equal pushes solution or worse pushes solution than the passed one.
	 *
	 * @param compareSolution solution to be compared with this solution, or null
	 * @return	<code>-1</code> if this solution is a better pushes solution,<br>
	 *         <code>0</code> if this solution is an equally good pushes solution
	 *         <code>1</code> if this solution is a better pushes solution
	 */
	public int pushesCompare(Solution compareSolution) {

		// Each non-null (we are non-null) is better than null (nothing).
		if(compareSolution == null) {
			return -1;
		}

		// Check for better pushes, then better moves on equal pushes.
		// We end up with dictionary order comparison of
		// (a) 2 initial values
		// (b) optionally 3 further values
		// We are in fact better for a negative comparison result,
		// since that means, that we have the smaller values.
		int wecompare = Utilities.intCompare2Pairs(
				this.pushesCount, compareSolution.pushesCount,
				this.movesCount , compareSolution.movesCount  );
		if (wecompare != 0) {
			// It is decided, already...
			return wecompare;
		}

		// If the other values aren't to be checked the current solution
		// just isn't better than the passed one.
		if (!Settings.checkAllMinorMetrics) {
			return 0;
		}

		// The first 2 were equal, so check the remaining 3 ...
		return Utilities.intCompare3Pairs(
					this.boxLines       , compareSolution.boxLines,
					this.boxChanges     , compareSolution.boxChanges,
					this.pushingSessions, compareSolution.pushingSessions);
	}

	/**
	 * Returns whether this solution is a better moves solution than
	 * the passed solution.
	 *
	 * @param compareSolution solution to be compared with this solution, or null
	 * @return	<code>true</code> if this solution is a better moves solution,<br>
	 *         <code>false</code> if this solution is not a better moves solution
	 */
	public final boolean isBetterMovesSolutionThan(Solution compareSolution) {
		return MOVES_COMPARATOR.compare(this, compareSolution) < 0;
	}

	/**
	 * Returns whether the this solution is a better pushes solution than
	 * the passed solution.
	 *
	 * @param compareSolution solution to be compared with this solution, or null
	 * @return	<code>true</code> if this solution is a better pushes solution,<br>
	 * 		   <code>false</code> if this solution is not a better pushes solution
	 */
	public final boolean isBetterPushesSolutionThan(Solution compareSolution) {
		return PUSHES_COMPARATOR.compare(this, compareSolution) < 0;
	}


	/**
	 * Tell whether we currently have a valid {@code lastChanged} data.
	 *
	 * @return whether the {@code lastChanged} data is valid
	 */
	public boolean isValidLastChanged() {
		if (lastChanged != null) {
			if (lastChanged.getTime() > 0) {
				return true;
			}
		}
		return false;
	}

	/**
	 * If currently there is not yet any "lastChanged" data set in the object,
	 * we now set it to "now".
	 *
	 * @return whether we changed the {@code Solution} object.
	 */
	public boolean chkSetLastChangedToNow() {
		if (!isValidLastChanged()) {
			lastChanged = new Date();		// now
			return true;
		}
		return false;			// nothing happened
	}

	/**
	 * Sets the {@code lastChanged} data to the specified point in time.
	 *
	 * @param changedAt the time to be remembered as {@code lastChanged} data
	 */
	public void setLastChanged(Date changedAt) {
		lastChanged = changedAt;
		// FFS/hm: clone it?
	}

	/**
	 * Retrieves the {@code lastChanged} data as a {@code Date} object.
	 *
	 * @return the {@code lastChanged} data as a {@code Date} object
	 */
	public Date getLastChanged() {
		Date result = null;

		if (isValidLastChanged()) {
			result = lastChanged;
			// FFS/hm: clone it?
		}
		if (result == null) {
			result = new Date();			// now
		}
		return result;
	}

	/* (non-Javadoc)
	 * @see java.lang.Object#hashCode()
	 */
	@Override
	public int hashCode() {
		final int PRIME = 31;

		// The lurd representation is the unique identifier of a solution that never changes.
		return PRIME + lurd.hashCode();
	}


	/* (non-Javadoc)
	 * @see java.lang.Object#equals(java.lang.Object)
	 */
	@Override
	public boolean equals(Object compareObject) {
		if(this == compareObject) {
			return true;
		}

		if(compareObject == null) {
			return false;
		}

		if(getClass() != compareObject.getClass()) {
			return false;
		}

		final Solution other = (Solution) compareObject;

		// The LURD string must be equal.
		if(!lurd.equals(other.lurd)) {
			return false;
		}

		// Attention: solutions must be unique!
		// Hence, it's enough to check whether the lurd representation is equal.
		return true;
	}

	/* (non-Javadoc)
	 * @see java.lang.Object#toString()
	 */
	@Override
	public String toString() {
		return      movesCount
			+ "/" + pushesCount
			+ "/" + boxLines
			+ "/" + boxChanges
			+ "/" + pushingSessions;
	}
}
