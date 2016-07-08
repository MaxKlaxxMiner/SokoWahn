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
import java.util.BitSet;

import de.sokoban_online.jsoko.boardpositions.AbsoluteBoardPositionMoves;
import de.sokoban_online.jsoko.boardpositions.IBoardPosition;
import de.sokoban_online.jsoko.deadlockdetection.ClosedDiagonalDeadlock;
import de.sokoban_online.jsoko.deadlockdetection.FreezeDeadlockDetection;
import de.sokoban_online.jsoko.gui.GUI;
import de.sokoban_online.jsoko.gui.Transformation;
import de.sokoban_online.jsoko.resourceHandling.Settings;
import de.sokoban_online.jsoko.resourceHandling.Settings.SearchDirection;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Debug;
import de.sokoban_online.jsoko.utilities.IntStack;
import de.sokoban_online.jsoko.utilities.LruCache;
import de.sokoban_online.jsoko.utilities.Utilities;


/**
 * This class contains the board data of the game and methods for modifying them.
 */
public class Board implements DirectionConstants
{

	/**
	 * This constant represents the value for an infinite distance, that is: unreachable.
	 */
	public static final short UNREACHABLE = Short.MAX_VALUE;

	/** Table of all potential corral forcer situations.
	 * The index is a wall mask from the 3x3 neighborhood around the square,
	 * omitting the central square: (9-1) bits <==> 256 possible index values.
	 */
	final protected boolean[] corralForcerSituations = new boolean[] { false,
			false, false, false, false, true, false, false, false, false,
			false, false, true, true, false, false, false, true, false, false,
			false, true, false, false, true, true, false, false, true, true,
			false, false, false, true, true, true, true, true, true, true,
			false, false, false, false, true, true, false, false, true, true,
			true, true, true, true, true, true, true, true, false, false, true,
			true, false, false, false, true, true, true, true, true, true,
			true, false, false, false, false, true, true, false, false, false,
			true, false, false, false, true, false, false, false, false, false,
			false, false, false, false, false, false, true, true, true, true,
			true, true, true, false, false, false, false, true, true, false,
			false, false, true, false, false, false, true, false, false, false,
			false, false, false, false, false, false, false, false, true, true,
			true, true, true, true, true, true, true, true, true, true, true,
			true, true, false, true, false, false, false, true, false, false,
			true, true, false, false, true, true, false, false, true, true,
			true, true, true, true, true, true, true, true, true, true, true,
			true, true, true, true, true, true, true, true, true, true, true,
			true, true, false, false, true, true, false, false, false, true,
			true, true, true, true, true, true, false, false, false, false,
			true, true, false, false, false, true, false, false, false, true,
			false, false, false, false, false, false, false, false, false,
			false, false, true, true, true, true, true, true, true, false,
			false, false, false, true, true, false, false, false, true, false,
			false, false, true, false, false, false, false, false, false,
			false, false, false, false };

	/** Constant for {@link #playerPosition} indicating "No player in level". */
	private final int NO_PLAYER = -1;

	/** Array holding the number of every goal at its position in this array. */
	protected int[] goalsNumbers;

	/** Array holding the positions of all goals. */
	protected int[] goalsPositions;

	/** Array holding the numbers of the boxes at their positions on the board. */
	private int[] boxNumbers;

	/** Player distance between two squares [startsquare][targetsquare] */
	protected short[][] playerDistances;

	// Arrays holding the information which object is located at which position.
	//
	// Walls are counted, because some methods set a wall to a square
	// and after completion of the calculation they remove the set wall.
	// Problem: These methods need to backup the information whether there
	// already has been a wall before they set a new wall on this square.
	// A square can have more than one object at a time! For example,
	// there may be a box and a wall on position 10. The methods have to
	// pay attention. Setting a wall to one square won't delete a box
	// already located on that square.
	private byte[] wallsArray;
	private boolean[] goalsArray;
	private boolean[] boxesArray;
	protected boolean[] simpleDeadlockSquareForwards;
	protected boolean[] simpleDeadlockSquareBackwards;
	private boolean[] advancedSimpleDeadlockSquareForwards;
	private boolean[] marked;

	// Arrays for the goals of the backwards search. The goals for the backwards search
	// are positioned where the boxes are located at the time the solver has been started.
	protected boolean[] goalSquareBackwardsSearch;
	protected int[] goalPositionsBackwardsSearch;

	/** Positions reachable by the player are set to "true". */
	private boolean[] playersReachableSquaresOnlyWallsAtLevelStart;

	/**
	 * <code>true</code> = a box on this position induces at least one closed area,
	 * (= an area which the player cannot reach, even if just this box
	 * is on this position)
	 */
	protected boolean[] corralForcer;

	/** Object which identifies simple and advanced deadlock squares. */
	protected BadSquares badSquares;

	/** The width of the board. For easier access this variable is public. */
	public int width;

	/** The height of the board. For easier access this variable is public. */
	public int height;

	/**
	 * Array containing the offset values for all directions (up, down, left and right)
	 * (up and down are: -width / +width)
	 * It is filled immediately after any change to {@link Board#width}.
	 */
	public int[] offset;

	/** Board size (height * width). Public for easier access. */
	public int size;

	/**
	 * The position of the first reachable square of the player (inclusively).
	 */
	public int firstRelevantSquare;

	/**
	 * The position of the last reachable square of the player + 1,
	 * i.e. exclusively.
	 */
	public int lastRelevantSquare;

	/** Player position, initialized with an "illegal" value.	*/
	public int playerPosition = NO_PLAYER;

	/**
	 * Number of boxes in a level.
	 * This value always refers to the original number of boxes.
	 * For some calculations boxes are temporarily removed from the board,
	 * but that doesn't change this value.
	 */
	public int boxCount;

	/** Number of goals in a level.	*/
	public int goalsCount;

	/** Object holding all relevant box data. */
	public BoxData boxData;

	/** Object for identifying the reachable squares of the player. */
	public PlayersReachableSquares playersReachableSquares;

	/** Calculation of the path of the player to a specific position. */
	public PlayerPathCalculation playerPath;

	/** Object for identifying the reachable squares of the player
	 *  without regarding boxes as obstacles.
	 */
	public PlayersReachableSquaresOnlyWalls playersReachableSquaresOnlyWalls;

	/**
	 * Identifies the reachable squares for a box (only needed for highlighting the
	 * reachable squares of a box when it is marked to be pushed)
	 */
	public BoxReachableSquares boxReachableSquares;

	/** Identifies the backwards reachable squares of a box. */
	public BoxReachableSquaresBackwards boxReachableSquaresBackwards;

	/**
	 * Identifies the reachable squares for a box if only this one box is on the board.
	 */
	private BoxReachableSquaresOnlyWalls boxReachableSquaresOnlyWalls;

	/** Identifies the reachable squares for a box if only this one box is on the board. */
	private BoxReachableSquaresBackwardsOnlyWalls boxReachableSquaresBackwardsOnlyWalls;

	/** Object for calculating the distances of the player and the boxes. */
	public Distances distances;

	/**
	 * Creates a new board.
	 * <p>
	 * The board has to be set by either calling:
	 * <ul>
	 *   <li> {@link #setBoardFromString(String)}, or
	 *   <li> {@link #newBoard(int, int)}
	 * </ul>
	 * and then
	 * <ol>
	 *  <li> {@link #isValid(StringBuilder)}, and
	 *  <li> {@link #prepareBoard()}
	 * </ol>
	 * to be used properly.
	 */
	public Board() {

	}

	/**
	 * Sets a new board by parsing the passed <code>String</code>.
	 *
	 * @param boardAsString  the board data as <code>String</code>
	 * @throws Exception thrown when the board is too big to be completely loaded
	 */
	public void setBoardFromString(String boardAsString) throws Exception {

		// New board width and height.
		int newBoardWidth  = 0;
		int newBoardHeight = 0;

		// Determine width and height.
		String[] boardRows = boardAsString.split("\n");
		for (String row : boardRows) {
			if (newBoardWidth < row.length()) {
				newBoardWidth = row.length();		// collect maximum
			}
		}
		newBoardHeight = boardRows.length;

		// Flag, indicating whether the level is too big to be completely loaded.
		boolean isLevelTooBig = false;

		// Character in the level at a specific position (e.g. "#", "$", ...)
		int squareCharacter = 0;

		// Check whether the board size exceeds the maximum size. If so, we
		// load only the clipped part of it, and throw an exception at the end.
		if (newBoardWidth > Settings.MAXIMUM_BOARDSIZE) {
			newBoardWidth = Settings.MAXIMUM_BOARDSIZE;
			isLevelTooBig = true;
		}
		if (newBoardHeight > Settings.MAXIMUM_BOARDSIZE) {
			newBoardHeight = Settings.MAXIMUM_BOARDSIZE;
			isLevelTooBig  = true;
		}

		// Prepare the board for the new level (also sets "width" and "height").
		newBoard(newBoardWidth, newBoardHeight);

		// Set the elements of the new board.
		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {

				// Get the relevant board element.
				String row = boardRows[y];
				squareCharacter = (x < row.length()) ? row.charAt(x) : ' ';

				switch (squareCharacter) {
				case ' ':
					break;

				case '.':
					setGoal(x, y);
					break;

				case '$':
					setBox(x, y);
					break;

				case '*':
					setBoxOnGoal(x, y);
					break;

				case '@':
					setPlayerPosition(x, y);
					break;

				case '+':
					setGoal(x, y);
					setPlayerPosition(x, y);
					break;

				case '#':
					setWall(x, y);
					break;

				default:
					// Maybe the user might read this. If not - doesn't matter.
					System.out.println( "Unknown square value in board data: "
							          + squareCharacter );
				}
			}
		}

		// Throw an exception if the board is too big to be completely loaded.
		if (isLevelTooBig) {
			throw new Exception( Texts.getText("message.levelTooBig")
								 + " " + Settings.MAXIMUM_BOARDSIZE
								 + "*" + Settings.MAXIMUM_BOARDSIZE   );
		}
	}

	/**
	 * Based on the already set up dimension data (here {@link #width})
	 * we allocate and fill the array {@link #offset}.
	 */
	private final void makeOffsets() {
		offset = new int[] { -width, +width, -1, +1 };
	}

	/**
	 * For a step from one square to another one, we scan the {@link #offset}
	 * array to find the direction for this step, and return this direction.
	 * If the 2 squares are not related by such a step, we will get
	 * an "array bounds" exception.
	 *
	 * @param srcPosition  where the step starts
	 * @param dstPosition  where the step ends up
	 * @return the direction which causes such a step
	 *         (i.e. <code>srcPosition + offset[direction] == dstPosition</code>)
	 */
	public final byte getMoveDirection(int srcPosition, int dstPosition) {
		int  stepdelta = dstPosition - srcPosition;
		byte direction = 0;
		while (offset[direction] != stepdelta) {
			++direction;
		}
		return direction;
	}

	/**
	 * For a step from one square to another one, we scan the {@link #offset}
	 * array to find the direction for this step, and return this direction.
	 * If the 2 squares are not related by such a step, we will just get
	 * another numerical value, which is too large: 4.
	 * TODO: do we really need this semantics? It results from refactoring existent code.
	 *
	 * @param srcPosition  where the step starts
	 * @param dstPosition  where the step ends up
	 * @return <code>4</code>, or the direction which causes such a step
	 *         (i.e. <code>srcPosition + offset[direction] == dstPosition</code>)
	 * @see #getMoveDirection(int, int)
	 */
	public final byte getMoveDirectionNumber(int srcPosition, int dstPosition) {
		int  stepdelta = dstPosition - srcPosition;
		byte direction = 0;
		for(; direction < DIRS_COUNT ; ++direction) {
			if (offset[direction] == stepdelta) {
				break;
			}
		}
		return direction;
	}


	/**
	 * Initializes the object for a new (empty) board.
	 *
	 * @param width  of the new board
	 * @param height of the new board
	 */
	public void newBoard(int width, int height) {
		this.width  = width;
		this.height = height;

		size = width * height;
		wallsArray = new byte[size];
		goalsArray = new boolean[size];
		boxesArray = new boolean[size];

		simpleDeadlockSquareForwards = new boolean[size];
		simpleDeadlockSquareBackwards = new boolean[size];
		advancedSimpleDeadlockSquareForwards = new boolean[size];
		marked = new boolean[size];

		// We crate an array for the offsets of the 4 directions
		// to be used for indexes into flat arrays with dimension "size".
		makeOffsets();

		// Create objects that identify the reachable squares.
		playersReachableSquares = new PlayersReachableSquares();
		playerPath = new PlayerPathCalculation();
		playersReachableSquaresOnlyWalls = new PlayersReachableSquaresOnlyWalls();
		boxReachableSquares = new BoxReachableSquares();
		boxReachableSquaresBackwards = new BoxReachableSquaresBackwards();
		boxReachableSquaresOnlyWalls = new BoxReachableSquaresOnlyWalls();
		boxReachableSquaresBackwardsOnlyWalls = new BoxReachableSquaresBackwardsOnlyWalls();

		// Now we can create the object, which is used to compute the deadlock positions.
		badSquares = new BadSquares();

		// Initially we have 0 boxes and 0 goals.
		// These values are set to their correct values elsewhere.
		boxCount   = 0;
		goalsCount = 0;

		// The position of the player is not yet determined / set.
		playerPosition = NO_PLAYER;
	}


	/**
	 * Returns whether the current board is valid and calculates the number
	 * of goals and boxes.
	 *
	 * @param message this buffer is filled with the problem detail text
	 *                (e.g. "board has no player")
	 * @return <code>true</code> if the level is valid, and
	 * 		  <code>false</code> if the level is invalid
	 *                           (and "message" is filled)
	 */
	public boolean isValid(StringBuilder message) {

		// Current player position.
		int currentPlayerPosition;

		// Delete the passed message.
		// message.delete(0, message.length());
		message.setLength(0);

		// Create a new array holding the reachable squares of the player.
		// If the level hasn't a player yet all squares are treated
		// as not reachable.
		playersReachableSquaresOnlyWallsAtLevelStart = new boolean[size];

		// Check if the level has a player.
		if (playerPosition == NO_PLAYER) {
			message.append(Texts.getText("levelwithoutplayer"));
			return false;
		}

		IntStack squaresToBeAnalyzed = new IntStack(size);

		/*
		 * Check which squares are reachable for the player.
		 * If the player can reach the border of the level it is invalid.
		 */
		// The current player position is the start position.
		squaresToBeAnalyzed.add(playerPosition);
		playersReachableSquaresOnlyWallsAtLevelStart[playerPosition] = true;

		// Determine all reachable squares of the player.
		while (!squaresToBeAnalyzed.isEmpty()) {
			currentPlayerPosition = squaresToBeAnalyzed.remove();

			// If the player has reached the border of the level the level is invalid.
			if (       currentPlayerPosition < width
					|| currentPlayerPosition > size - width
					|| currentPlayerPosition % width == 0
					|| currentPlayerPosition % width == width - 1) {
				if (message.length() == 0) {
					message.append(Texts.getText("invalidplayerposition"));
				}

				// continue marking all reachable squares
				continue;
			}

			// Put all not already reached squares reachable from
			// the current square to the stack and mark them as reached.
			for (int direction = 0; direction < DIRS_COUNT; direction++) {
				int nextPlayerPosition = currentPlayerPosition + offset[direction];
				if (       isWall(nextPlayerPosition) == false
						&& playersReachableSquaresOnlyWallsAtLevelStart[nextPlayerPosition] == false) {
					squaresToBeAnalyzed.add(nextPlayerPosition);
					playersReachableSquaresOnlyWallsAtLevelStart[nextPlayerPosition] = true;
				}
			}
		}

		// Counting of the relevant boxes and goals
		boxCount   = 0;
		goalsCount = 0;
		for (int position = 0; position < size; position++) {
			if (isBox(position)) {
				if (playersReachableSquaresOnlyWallsAtLevelStart[position]) {
					boxCount++;
				}
			}
			if (isGoal(position)) {
				if (playersReachableSquaresOnlyWallsAtLevelStart[position]) {
					goalsCount++;
				}
			}
		}

		// If we have already collected an error message (-> invalid player position)
		// we are done: return false.
		if (message.length() > 0) {
			return false;
		}

		// There must be as many boxes as goals in a level.
		if (boxCount != goalsCount) {
			message.append(Texts.getText("noboxes_unequally_nogoals"));
			return false;
		}

		// There must be at least one box / goal in the level.
		if (boxCount == 0) {
			message.append(Texts.getText("atleastonebox"));
			return false;
		}

		return true;			// no problems recognized
	}

	private static void loggTime(int location) {
		Utilities.loggTime(location, "prepareBoard");
	}

	/**
	 * A new level has been loaded.
	 * Before this method is called, our caller has already called
	 * {@link #isValid(StringBuilder)}, where the board has been checked
	 * for valid content.
	 * Is a player in the board, there have been calculated the reachable
	 * squares of the board.
	 * <p>
	 * Since {@link #isValid(StringBuilder)} is frequently called from the
	 * editor mode, we have moved all further preparations into this
	 * additional method.
	 */
	public void prepareBoard() {

		loggTime(100);
		// Create a new box data array, and arrays for the goals.
		boxData = new BoxData(this);
		goalsPositions = new int[goalsCount];
		goalSquareBackwardsSearch = new boolean[size];
		goalPositionsBackwardsSearch = new int[goalsCount];
		loggTime(101);

		// Create object which can calculate the box distances.
		distances = new Distances();
		loggTime(102);

		// Create new arrays, to store goal number / box number
		// at the index, where the goal / box is located.
		goalsNumbers = new int[size];
		boxNumbers   = new int[size];

		// Now we index all goals and all boxes, using two counters for them.
		int boxNo  = 0;
		int goalNo = 0;

		// First square the player can reach
		firstRelevantSquare = 0;

		// Scan the complete board...
		loggTime(102); loggTime(-size);
		for (int position = 0; position < size; position++) {

			// Border squares and walls can be ignored
			if (isOuterSquareOrWall(position)) {
				continue;
			}

			if (isBox(position)) {
				boxNumbers[position] = boxNo;
				boxData.setBoxStartPosition(boxNo, position);

				// For lower bound debugging of the backwards search we need
				// correctly set goals of the backwards search.
				goalSquareBackwardsSearch[position] = true;
				goalPositionsBackwardsSearch[boxNo] = position;

				boxNo++;
			} else {
				// For lower bound debugging of the backwards search we need
				// correctly set goals of the backwards search.
				goalSquareBackwardsSearch[position] = false;
			}

			if (isGoal(position)) {
				goalsNumbers[position] = goalNo;
				goalsPositions[goalNo++] = position;
			}

			// Check whether this may be the first player reachable square
			if (firstRelevantSquare == 0 && ! isOuterSquareOrWall(position)) {
				firstRelevantSquare = position;
			}

			// Set up the last position reachable by the player.
			// Since we use it like an array.length and our loops check
			// their variable < board.lastRelevantSquare, we must add 1.
			lastRelevantSquare = position + 1;
		}
		loggTime(105);

		// Calculate distances of the boxes (to all squares)...
		// TODO: we could first determine frozen boxes
		distances.updateBoxDistances(SearchDirection.FORWARD, true );
		distances.updateBoxDistances(SearchDirection.BACKWARD, true);
		loggTime(111);

		// Calculate distances of the player (to all squares)...
		distances.calculatePlayerDistances();
		loggTime(112);

		// Calculate deadlock squares
		badSquares.identifySimpleDeadlockSquaresForwards();
		loggTime(121);
		badSquares.identifySimpleDeadlockSquaresBackwards();
		loggTime(122);
		badSquares.identifyAdvancedSimpleDeadlockSquaresForwards();
		loggTime(123);
	}


	/**
	 * Removes a box from the passed position.
	 *
	 * @param position the position a box is to be removed from.
	 */
	public void removeBox(int position) {
		boxesArray[position] = false;
	}

	/**
	 * Removes the box having the passed number.
	 *
	 * @param boxNo the number of the box that is to be removed.
	 */
	public void removeBoxByNumber(int boxNo) {
		boxesArray[boxData.getBoxPosition(boxNo)] = false;
	}

	/**
	 * Removes a box from the passed position.
	 * The arguments are not checked against the board dimensions.
	 *
	 * @param x	the x-coordinate of the position the box is to be removed from.
	 * @param y the y-coordinate of the position the box is to be removed from.
	 */
	public void removeBox(int x, int y) {
		boxesArray[x + width * y] = false;
	}

	/**
	 * Removes a wall from the passed position.
	 *
	 * @param position the position a wall is to be removed from.
	 */
	public void removeWall(int position) {
		wallsArray[position] -= ((wallsArray[position] > 0) ? 1 : 0);
	}

	/**
	 * Removes a wall from the passed position.
	 * The arguments are not checked against the board dimensions.
	 *
	 * @param x	the x-coordinate of the position the wall is to be removed from.
	 * @param y the y-coordinate of the position the wall is to be removed from.
	 */
	public void removeWall(int x, int y) {
		wallsArray[x + width * y] -= ((wallsArray[x + width * y] > 0) ? 1 : 0);
	}

	/**
	 * Removes a goal from the passed position.
	 *
	 * @param position the position a goal is to be removed from.
	 */
	public void removeGoal(int position) {
		goalsArray[position] = false;
	}

	/**
	 * Removes a goal from the passed position.
	 * The arguments are not checked against the board dimensions.
	 *
	 * @param x	the x-coordinate of the position the goal is to be removed from.
	 * @param y the y-coordinate of the position the goal is to be removed from.
	 */
	public void removeGoal(int x, int y) {
		goalsArray[x + width * y] = false;
	}

	/**
	 * Removes the player from the board.
	 */
	public void removePlayer() {
		playerPosition = NO_PLAYER;
	}

	/**
	 * Sets a box at the passed position.
	 *
	 * @param position the position a box is to be set.
	 */
	public void setBox(int position) {
		boxesArray[position] = true;
	}

	/**
	 * Sets a box at the passed position.
	 * The arguments are not checked against the board dimensions.
	 *
	 * @param x the x-coordinate of the position a box is to be set.
	 * @param y the y-coordinate of the position a box is to be set.
	 */
	public void setBox(int x, int y) {
		boxesArray[x + width * y] = true;
	}

	/**
	 * Sets a box with the passed number at the passed position.
	 *
	 * @param position the position a box is to be set.
	 * @param boxNo	the number of the box to be set
	 */
	public void setBoxWithNo(int boxNo, int position) {
		boxesArray[position] = true;
		boxNumbers[position] = boxNo;
	}

	/**
	 * Sets a box with the passed number at the passed position.
	 * The arguments are not checked against the board dimensions.
	 *
	 * @param x the x-coordinate of the position a box is to be set.
	 * @param y the y-coordinate of the position a box is to be set.
	 * @param boxNo	the number of the box to be set
	 */
	public void setBoxWithNo(int boxNo, int x, int y) {
		boxesArray[x + width * y] = true;
		boxNumbers[x + width * y] = boxNo;
	}

	/**
	 * Sets a box and a goal at the passed position.
	 *
	 * @param position the position the objects are to be set.
	 */
	public void setBoxOnGoal(int position) {
		boxesArray[position] = true;
		goalsArray[position] = true;
	}

	/**
	 * Sets a box and a goal at the passed position.
	 * The arguments are not checked against the board dimensions.
	 *
	 * @param x the x-coordinate of the position the objects are to be set.
	 * @param y the y-coordinate of the position the objects are to be set.
	 */
	public void setBoxOnGoal(int x, int y) {
		boxesArray[x + width * y] = true;
		goalsArray[x + width * y] = true;
	}

	/**
	 * Sets a a goal at the passed position.
	 *
	 * @param position the position the goal is to be set.
	 */
	public void setGoal(int position) {
		goalsArray[position] = true;
	}

	/**
	 * Sets a goal at the passed position.
	 * The arguments are not checked against the board dimensions.
	 *
	 * @param x the x-coordinate of the position a goal is to be set.
	 * @param y the y-coordinate of the position a goal is to be set.
	 */
	public void setGoal(int x, int y) {
		goalsArray[x + width * y] = true;
	}

	/**
	 * Sets a wall at the passed position.
	 *
	 * @param position the position the wall is to be set.
	 */
	public void setWall(int position) {
		wallsArray[position]++;
	}

	/**
	 * Sets a wall at the passed position.
	 * The arguments are not checked against the board dimensions.
	 *
	 * @param x the x-coordinate of the position a wall is to be set.
	 * @param y the y-coordinate of the position a wall is to be set.
	 */
	public void setWall(int x, int y) {
		wallsArray[x + width * y]++;
	}

	/**
	 * Sets the number of a box at the passed position.
	 *
	 * @param boxNo	the box number to be set
	 * @param position the position the box number is to be set.
	 */
	public void setBoxNo(int boxNo, int position) {
		boxNumbers[position] = boxNo;
	}

	/**
	 * Sets the number of a box at the passed position.
	 * The arguments are not checked against the board dimensions.
	 *
	 * @param boxNo	the box number to be set
	 * @param x the x-coordinate of the position the box number is to be set.
	 * @param y the y-coordinate of the position the box number is to be set.
	 */
	public void setBoxNo(int boxNo, int x, int y) {
		boxNumbers[x + width * y] = boxNo;
	}

	/**
	 * Sets the square at the the passed position to be an advanced deadlock
	 * square.
	 *
	 * @param position the position of the square
	 */
	public void setAdvancedSimpleDeadlock(int position) {
		advancedSimpleDeadlockSquareForwards[position] = true;
	}

	/**
	 * Sets the player to the passed position.
	 *
	 * @param position Position the player is to be set at.
	 */
	public void setPlayerPosition(int position) {
		playerPosition = position;
	}

	/**
	 * Sets the player to the passed position.
	 * The arguments are not checked against the board dimensions.
	 *
	 * @param x the x-coordinate of the position the player is to be set at.
	 * @param y the y-coordinate of the position the player is to be set at.
	 */
	public void setPlayerPosition(int x, int y) {
		playerPosition = x + width * y;
	}

	/**
	 * Returns whether the square at the the passed position is a corral
	 * forcer square.
	 *
	 * @param position the position of the square
	 * @return <code>true</code> if there is a corral forcer square at the
	 *                           passed position, or<br>
	 * 		  <code>false</code> if there isn't a corral forcer square at the
	 *                           passed position
	 */
	public boolean isCorralForcerSquare(int position) {
		return corralForcer[position];
	}

	/**
	 * Returns whether the square at the the passed position is a corral
	 * forcer square.
	 * The arguments are not checked against the board dimensions.
	 *
	 * @param x the x-coordinate of the position
	 * @param y the y-coordinate of the position
	 * @return <code>true</code> if there is a corral forcer square at the
	 *                           passed position, or<br>
	 * 		  <code>false</code> if there isn't a corral forcer square at the
	 *                           passed position
	 */
	public boolean isCorralForcerSquare(int x, int y) {
		return corralForcer[x + width * y];
	}

	/**
	 * Returns whether there is a box at the passed position.
	 *
	 * @param position the position of the square
	 * @return <code>true</code> if there is a box at the passed position,
	 * or<br> <code>false</code> if there isn't a box at the passed position
	 */
	public boolean isBox(int position) {
		return boxesArray[position];
	}

	/**
	 * Returns whether there is a box at the passed position.
	 * The arguments are not checked against the board dimensions.
	 *
	 * @param x the x-coordinate of the position
	 * @param y the y-coordinate of the position
	 * @return <code>true</code> if there is a box at the passed position,
	 * or<br> <code>false</code> if there isn't a box at the passed position
	 */
	public boolean isBox(int x, int y) {
		return boxesArray[x + width * y];
	}

	/**
	 * Returns whether there is a wall at the passed position.
	 *
	 * @param position the position of the square
	 * @return <code>true</code> if there is a wall at the passed position,
	 * or<br> <code>false</code> if there isn't a wall at the passed position
	 */
	public boolean isWall(int position) {
		return wallsArray[position] > 0;
	}

	/**
	 * Returns whether there is a wall at the passed position.
	 * The arguments are not checked against the board dimensions.
	 *
	 * @param x the x-coordinate of the position
	 * @param y the y-coordinate of the position
	 * @return <code>true</code> if there is a wall at the passed position,
	 * or<br> <code>false</code> if there isn't a wall at the passed position
	 */
	public boolean isWall(int x, int y) {
		return wallsArray[x + width * y] > 0;
	}

	/**
	 * Returns whether there is a goal at the passed position.
	 *
	 * @param position the position of the square
	 * @return <code>true</code> if there is a goal at the passed position,
	 * or<br> <code>false</code> if there isn't a goal at the passed position
	 */
	public boolean isGoal(int position) {
		return goalsArray[position];
	}

	/**
	 * Returns whether there is a goal at the passed position.
	 * The arguments are not checked against the board dimensions.
	 *
	 * @param x the x-coordinate of the position
	 * @param y the y-coordinate of the position
	 * @return <code>true</code> if there is a goal at the passed position,
	 * or<br> <code>false</code> if there isn't a goal at the passed position
	 */
	public boolean isGoal(int x, int y) {
		return goalsArray[x + width * y];
	}

	/**
	 * Returns whether the square at the passed position is a box or a wall.
	 *
	 * @param position the position of the square
	 * @return <code>true</code> if the square is accessible,
	 * or<br> <code>false</code> if the square isn't accessible
	 */
	public boolean isBoxOrWall(int position) {
		return wallsArray[position] > 0 || boxesArray[position];
	}

	/**
	 * Returns whether the square at the passed position is either a goal
	 * or a wall.
	 *
	 * @param position  the position of the square
	 * @return <code>true</code> if the square is a goal or a wall, or<br>
	 * 		  <code>false</code> otherwise
	 */
	public boolean isGoalOrWall(int position) {
		return goalsArray[position] || wallsArray[position] > 0;
	}

	/**
	 * Returns whether the square at the passed position is a box or a wall.
	 * The arguments are not checked against the board dimensions.
	 *
	 * @param x the x-coordinate of the position
	 * @param y the y-coordinate of the position
	 * @return <code>true</code> if the square is accessible, or<br>
	 * 		  <code>false</code> if the square isn't accessible
	 */
	public boolean isBoxOrWall(int x, int y) {
		return wallsArray[x + width * y] > 0 || boxesArray[x + width * y];
	}

	/**
	 * Returns whether there is a corral forcer at the passed position.
	 * A corral forcer divides the board into areas the player can reach
	 * and can't reach.
	 *
	 * @param position the position of the square
	 * @return <code>true</code> if there is a corral forcer at the position,
	 * or<br> <code>false</code> if there isn't a corral forcer at the position
	 */
	public boolean isCorralForcer(int position) {
		return corralForcer[position] == true;
	}

	/**
	 * Returns whether there is a backward search goal at the passed position.
	 * The backward search goals are the positions of the boxes
	 * at the beginning of a level.
	 *
	 * @param position the position of the square
	 * @return <code>true</code> if there is a backward search goal
	 *                           at the passed position, or<br>
	 * 		  <code>false</code> if there isn't a backward search goal
	 *                           at the passed position
	 */
	public boolean isGoalBackwardsSearch(int position) {
		return goalSquareBackwardsSearch[position];
	}

	/**
	 * Returns whether there is a backward search goal at the passed position.
	 * The backward search goals are the positions of the boxes
	 * at the beginning of a level.
	 * The arguments are not checked against the board dimensions.
	 *
	 * @param x the x-coordinate of the position
	 * @param y the y-coordinate of the position
	 * @return <code>true</code> if there is a backward search goal
	 *                           at the passed position, or<br>
	 * 		  <code>false</code> if there isn't a backward search goal
	 *                           at the passed position
	 */
	public boolean isGoalBackwardsSearch(int x, int y) {
		return goalSquareBackwardsSearch[x + width * y];
	}

	/**
	 * Returns whether there is an empty square at the passed position.
	 * NB: a goal is <em>not</em> considered to be empty.
	 *
	 * @param position the position of the square
	 * @return <code>true</code> if there is an empty square
	 *                           at the passed position, or<br>
	 * 		  <code>false</code> if there isn't an empty square
	 *                           at the passed position
	 */
	public boolean isEmptySquare(int position) {
		return ! (boxesArray[position] || wallsArray[position] > 0 || goalsArray[position]);
	}

	/**
	 * Returns whether there is an empty square at the passed position.
	 * NB: a goal is <em>not</em> considered to be empty.
	 * The arguments are not checked against the board dimensions.
	 *
	 * @param x the x-coordinate of the position
	 * @param y the y-coordinate of the position
	 * @return <code>true</code> if there is an empty square
	 *                           at the passed position, or<br>
	 * 		  <code>false</code> if there isn't an empty square
	 *                           at the passed position
	 */
	public boolean isEmptySquare(int x, int y) {
		return ! (   boxesArray[x + width * y]
		          || wallsArray[x + width * y] > 0
		          || goalsArray[x + width * y]     );
	}

	/**
	 * Returns whether the square at the passed is accessible,
	 * that means: not a box and not a wall.
	 *
	 * @param position the position of the square
	 * @return <code>true</code> if the square is accessible, or<br>
	 * 		  <code>false</code> if the square isn't accessible
	 */
	public boolean isAccessible(int position) {
		return ! (wallsArray[position] > 0 || boxesArray[position]);
	}

	/**
	 * Returns whether the square at the passed is accessible,
	 * i.e. not a box and not a wall.
	 * The arguments are not checked against the board dimensions.
	 *
	 * @param x the x-coordinate of the position
	 * @param y the y-coordinate of the position
	 * @return <code>true</code> if the square is accessible, or<br>
	 * 		  <code>false</code> if the square isn't accessible
	 */
	public boolean isAccessible(int x, int y) {
		return ! (wallsArray[x + width * y] > 0 || boxesArray[x + width * y]);
	}

	/**
	 * Returns whether the square at the passed position is accessible for
	 * a box.  A square is accessible for a box if there is neither a wall
	 * nor a box at this square AND the square is no simple deadlock square.
	 *
	 * @param position the position of the square
	 * @return <code>true</code> if the square is accessible for a box, or<br>
	 * 		  <code>false</code> if the square isn't accessible for a box
	 */
	public boolean isAccessibleBox(int position) {
		return ! (   wallsArray[position] > 0
				  || boxesArray[position]
				  || simpleDeadlockSquareForwards[position]
				  || advancedSimpleDeadlockSquareForwards[position] );
	}

	/**
	 * Returns whether the square at the passed position is accessible for
	 * a box.  A square is accessible for a box if there is neither a wall
	 * nor a box at this square AND the square is no simple deadlock square.
	 * The arguments are not checked against the board dimensions.
	 *
	 * @param x the x-coordinate of the position
	 * @param y the y-coordinate of the position
	 * @return <code>true</code> if the square is accessible for a box, or<br>
	 * 		  <code>false</code> if the square isn't accessible for a box
	 */
	public boolean isAccessibleBox(int x, int y) {
		return ! (   wallsArray[x + width * y] > 0
				  || boxesArray[x + width * y]
				  || simpleDeadlockSquareForwards[x + width * y]
				  || advancedSimpleDeadlockSquareForwards[x + width * y] );
	}

	/**
	 * Returns whether the square at the passed position is a wall
	 * or a simple deadlock square.
	 *
	 * @param position the position of the square
	 * @return <code>true</code> if the square is a wall or a simple
	 *                           deadlock square, or<br>
	 * 		  <code>false</code> if the square is neither a wall nor a simple
	 *                           deadlock square
	 */
	public boolean isWallOrIllegalSquare(int position) {
		return     wallsArray[position] > 0
				|| simpleDeadlockSquareForwards[position]
				|| advancedSimpleDeadlockSquareForwards[position];
	}

	/**
	 * Returns whether the square at the passed position is a wall
	 * or a simple deadlock square.
	 * The arguments are not checked against the board dimensions.
	 *
	 * @param x the x-coordinate of the position
	 * @param y the y-coordinate of the position
	 * @return <code>true</code> if the square is a wall or a simple
	 *                           deadlock square, or<br>
	 * 		  <code>false</code> if the square is neither a wall nor a simple
	 *                           deadlock square
	 */
	public boolean isWallOrIllegalSquare(int x, int y) {
		return     wallsArray[x + width * y] > 0
				|| simpleDeadlockSquareForwards[x + width * y]
				|| advancedSimpleDeadlockSquareForwards[x + width * y];
	}

	/**
	 * Returns whether the passed position is an outer square or a wall.
	 * An outer square is a square which is outside the reachable area
	 * of the player even if there weren't any boxes on the board.
	 *
	 * @param position the position of the square
	 * @return <code>true</code> if the square is an outer square or a wall,
	 * or<br> <code>false</code> if the square isn't an outer square or a wall
	 */
	public boolean isOuterSquareOrWall(int position) {
		return    ! playersReachableSquaresOnlyWallsAtLevelStart[position]
		       || wallsArray[position] > 0;
	}

	/**
	 * Returns whether the square at the passed position is a simple
	 * deadlock square.
	 * <p>
	 * The search direction doesn't matter, because the simple deadlock squares
	 * of the other direction can never be reached from a specific direction.
	 * Therefore both - the forward and the backward simple deadlock squares -
	 * are checked.
	 *
	 * @param position the position of the square
	 * @return <code>true</code> if the square is a simple deadlock square,
	 * or<br> <code>false</code> if the square isn't a simple deadlock square
	 */
	public boolean isSimpleDeadlockSquare(int position) {
		return     simpleDeadlockSquareForwards[position]
				|| advancedSimpleDeadlockSquareForwards[position]
				|| simpleDeadlockSquareBackwards[position];
	}

	/**
	 * Returns whether the square at the passed position is a simple
	 * deadlock square.
	 * <p>
	 * The search direction doesn't matter, because the simple deadlock squares
	 * of the other direction can never be reached from a specific direction.
	 * Therefore both the forward and the backward simple deadlock squares
	 * are checked.
	 * <p>
	 * The arguments are not checked against the board dimensions.
	 *
	 * @param x the x-coordinate of the position
	 * @param y the y-coordinate of the position
	 * @return <code>true</code> if the square is a simple deadlock square,
	 * or<br> <code>false</code> if the square isn't a simple deadlock square
	 */
	public boolean isSimpleDeadlockSquare(int x, int y) {
		return     simpleDeadlockSquareForwards[x + width * y]
				|| advancedSimpleDeadlockSquareForwards[x + width * y]
				|| simpleDeadlockSquareBackwards[x + width * y];
	}

	/**
	 * Method only for debugging:
	 * Returns whether the square at the passed position is
	 * a simple deadlock forward square.
	 *
	 * @param position the position of the square
	 * @return <code>true</code> if the square is a wall or a simple
	 *                           deadlock forward square,
	 * or<br> <code>false</code> if the square isn't a wall nor a simple
	 *                           deadlock forward square
	 */
	public boolean isSimpleDeadlockSquareForwardsDebug(int position) {
		return simpleDeadlockSquareForwards[position];
	}

	/**
	 * Method only for debugging:
	 * Returns whether the square at the passed position is
	 * a simple deadlock forward square.
	 *
	 * @param x the x-coordinate of the position
	 * @param y the y-coordinate of the position
	 * @return <code>true</code> if the square is a wall or a simple
	 *                           deadlock forward square,
	 * or<br> <code>false</code> if the square isn't a wall nor a simple
	 *                           deadlock forward square
	 */
	public boolean isSimpleDeadlockSquareForwardsDebug(int x, int y) {
		return simpleDeadlockSquareForwards[x + width * y];
	}

	/**
	 * Method only for debugging:
	 * Returns whether the square at the passed position is
	 * a simple deadlock backward square.
	 *
	 * @param position the position of the square
	 * @return <code>true</code> if the square is a wall or a simple
	 *                           deadlock backward square,
	 * or<br> <code>false</code> if the square isn't a wall nor a simple
	 *                           deadlock backward square
	 */
	public boolean isSimpleDeadlockSquareBackwardsDebug(int position) {
		return simpleDeadlockSquareBackwards[position];
	}

	/**
	 * Method only for debugging:
	 * Returns whether the square at the passed position is a simple deadlock backward square.
	 *
	 * @param x the x-coordinate of the position
	 * @param y the y-coordinate of the position
	 * @return <code>true</code> if the square is a wall or a simple
	 *                           deadlock backward square,
	 * or<br> <code>false</code> if the square isn't a wall nor a simple
	 *                           deadlock backward square
	 */
	public boolean isSimpleDeadlockSquareBackwardsDebug(int x, int y) {
		return simpleDeadlockSquareBackwards[x + width * y];
	}

	/**
	 * Method only for debugging:
	 * Returns whether the square at the passed position is
	 * an advanced simple deadlock forward square.
	 *
	 * @param position the position of the square
	 * @return <code>true</code> if the square is a wall or an advanced
	 *                           simple deadlock forward square,
	 * or<br> <code>false</code> if the square isn't a wall nor a advanced
	 *                           simple deadlock forward square
	 */
	public boolean isAdvancedSimpleDeadlockSquareForwards(int position) {
		return advancedSimpleDeadlockSquareForwards[position];
	}

	/**
	 * Method only for debugging:
	 * Returns whether the square at the passed position is a advanced simple deadlock forward square.
	 *
	 * @param x the x-coordinate of the position
	 * @param y the y-coordinate of the position
	 * @return <code>true</code> if the square is a wall or an advanced
	 *                           simple deadlock forward square,
	 * or<br> <code>false</code> if the square isn't a wall nor a advanced
	 *                           simple deadlock forward square
	 */
	public boolean isAdvancedSimpleDeadlockSquareForwards(int x, int y) {
		return advancedSimpleDeadlockSquareForwards[x + width * y];
	}

	/**
	 * Returns whether there is a box and a goal at the passed position.
	 *
	 * @param position the position of the square
	 * @return <code>true</code> if there is a box and a goal
	 *                           at the passed position,
	 * or<br> <code>false</code> if there isn't a box and a goal
	 *                           at the passed position
	 */
	public boolean isBoxOnGoal(int position) {
		return boxesArray[position] && goalsArray[position];
	}

	/**
	 * Returns whether there is a box and a goal at the passed position.
	 *
	 * @param x the x-coordinate of the position
	 * @param y the y-coordinate of the position
	 * @return <code>true</code> if there is a box and a goal
	 *                           at the passed position,
	 * or<br> <code>false</code> if there isn't a box and a goal
	 *                           at the passed position
	 */
	public boolean isBoxOnGoal(int x, int y) {
		return boxesArray[x + width * y] && goalsArray[x + width * y];
	}

	/**
	 * Returns whether there is a player in the level.
	 * <p>
	 * Usually there is a player in every level. This method is just used for
	 * determining "special" squares for displaying them correctly.
	 *
	 * @return <code>true</code> if there is a player in the level,
	 * or<br> <code>false</code> if there isn't a player in the level
	 */
	public boolean isPlayerInLevel() {
		return playerPosition != NO_PLAYER;
	}

	//---------- Methods for "marking" (debug only) --------------------------------------

	/**
	 * Method only for debugging:
	 * Returns whether the passed position is marked.
	 * <p>
	 * Marked positions are displayed with a little square.
	 *
	 * @param position the position of the square
	 * @return <code>true</code> if the passed position is marked,
	 * or<br> <code>false</code> if the passed position is not marked
	 */
	public boolean isMarked(int position) {
		return marked[position];
	}

	/**
	 * Method only for debugging:
	 * Returns whether the passed position is marked.
	 * <p>
	 * Marked positions are displayed with a little square graphic.
	 *
	 * @param x the x-coordinate of the position
	 * @param y the y-coordinate of the position
	 * @return <code>true</code> if the passed position is marked,
	 * or<br> <code>false</code> if the passed position is not marked
	 */
	public boolean isMarked(int x, int y) {
		return marked[x + width * y];
	}

	/**
	 * Removes a marking from the passed position.
	 *
	 * @param position the position a marking is to be removed from.
	 */
	public void removeMarking(int position) {
		marked[position] = false;
	}

	/**
	 * Removes a marking from the passed position.
	 *
	 * @param x	the x-coordinate of the position the marking is to be removed from.
	 * @param y the y-coordinate of the position the marking is to be removed from.
	 */
	public void removeMarking(int x, int y) {
		marked[x + width * y] = false;
	}

	/**
	 * Removes all marking from the board.
	 */
	public void removeAllMarking() {
		Arrays.fill(marked, false);			// this is not time critical
	}

	/**
	 * Sets a marking at the passed position.
	 *
	 * @param position the position the marking is to be set.
	 */
	public void setMarking(int position) {
		marked[position] = true;
	}

	/**
	 * Sets a marking at the passed position.
	 *
	 * @param x the x-coordinate of the position a marking is to be set.
	 * @param y the y-coordinate of the position a marking is to be set.
	 */
	public void setMarking(int x, int y) {
		marked[x + width * y] = true;
	}

	/**
	 * Marks the passed position with the passed value.
	 *
	 * @param position  what position is to be marked
	 * @param markValue with what value the position id to be marked
	 */
	public void assignMarking(int position, boolean markValue) {
		marked[position] = markValue;
	}

	/**
	 * Changes the making status of the passed position.
	 *
	 * @param position the position is to be changed
	 */
	public void flipMarking(int position) {
		marked[position] = ! marked[position];
	}

	//------------------------------------------------------------------------------------

	/**
	 * Returns the number of the box located at the passed position.
	 * For positions without a box the return value is undefined.
	 *
	 * @param position the position of the square
	 * @return	the number of the box
	 */
	public int getBoxNo(int position) {
		return boxNumbers[position];
	}

	/**
	 * Returns the number of the goal located at the passed position.
	 * For positions without a goal the return value is undefined.
	 *
	 * @param position the position of the square
	 * @return	the number of the goal
	 */
	public int getGoalNo(int position) {
		return goalsNumbers[position];
	}

	/**
	 * Returns the position of the goal with the passed goal number.
	 *
	 * @param goalNo the number of the goal
	 * @return the position of the goal
	 */
	public int getGoalPosition(int goalNo) {
		return goalsPositions[goalNo];
	}

	/**
	 * Returns the positions of all goals in an array.
	 *
	 * @return the position of all goals
	 */
	public int[] getGoalPositions() {
		return goalsPositions.clone();
	}

	/**
	 * Returns an array containing the positions of all backward goals.
	 *
	 * @return an array containing the positions of all backward goals
	 */
	public int[] getGoalPositionsBackward() {
		return goalPositionsBackwardsSearch;
	}

	/**
	 * Returns the position of the neighbor of the passed position
	 * by moving one move in the passed direction.
	 * <p>
	 * Example: getNeighborPosition(4, DirectionConstants.RIGHT) returns 5.
	 * <p>
	 * This method doesn't checks for the board width or height. The caller
	 * must ensure that the resulting position isn't "out side" of the board.
	 *
	 * @param position the position to start moving from
	 * @param direction the direction to move
	 * @return the reached position by moving in the passed direction
	 */
	public int getPosition(int position, int direction) {
		return position + offset[direction];
	}

	/**
	 * Returns the position of the neighbor of the passed position
	 * by moving one move in the opposite direction than the passed direction.
	 * <p>
	 * Example: getNeighborPosition(4, DirectionConstants.RIGHT) returns the
	 * position to the left that is 3.
	 * <p>
	 * This method doesn't checks for the board width or height. The caller
	 * must ensure that the resulting position isn't "out side" of the board.
	 *
	 * @param position the position to start moving from
	 * @param direction the direction to be used to determine the opposite direction from
	 * @return the reached position by moving in the opposite direction
	 */
	public int getPositionAtOppositeDirection(int position, int direction) {
		return position - offset[direction];
	}

	/**
	 * Moves a box from the fromSquare to the toSquare.
	 * This is the common part of {@link #pushBox(int, int)}
	 * and {@link #pushBoxUndo(int, int)}.
	 *
	 * @param fromSquare   the position the box to be pushed is located
	 * @param toSquare     the position the box is to be pushed to
	 * @param msgSuff      text suffix, just for error messages
	 * @return <code>-1</code>  if box couldn't be pushed as requested, or<br>
	 *         <code>>=0</code>, the number of the box successfully pushed.
	 * @see #pushBox(int, int)
	 * @see #pushBoxUndo(int, int)
	 */
	private int justPushBox(int fromSquare, int toSquare, String msgSuff) {

		if (isBox(fromSquare) == false) {
			if (Debug.isDebugModeActivated) {
				System.out.println("No Box on fromSquare!" + msgSuff);
			}
			return -1;
		}

		if (fromSquare == toSquare) {
			if (Debug.isDebugModeActivated) {
				System.out.println("fromSquare and toSquare are identic!" + msgSuff);
			}
			return -1;
		}

		if (isAccessible(toSquare) == false) {
			if (Debug.isDebugModeActivated) {
				System.out.println("Box isn't pushable. ToSquare isn't empty!" + msgSuff);
			}
			return -1;
		}

		final int boxNo = getBoxNo(fromSquare);
		removeBox(fromSquare);
		setBoxWithNo(boxNo, toSquare);

		// Save new box position
		boxData.setBoxPosition(boxNo, toSquare);
		// NB: "boxNo" cannot be negative, we just used it as array index.

		return boxNo;
	}

	/**
	 * Moves a box from the fromSquare to the toSquare.
	 *
	 * @param fromSquare   the position the box to be pushed is located
	 * @param toSquare     the position the box is to be pushed to
	 * @return <code>-1</code>  if box couldn't be pushed as requested, or<br>
	 *        <code>>=0</code>, the number of the box successfully pushed.
	 * @see #pushBoxUndo(int, int)
	 */
	public int pushBox(int fromSquare, int toSquare) {
		return justPushBox(fromSquare, toSquare, "");
	}

	/**
	 * Pushes a box from the specified source square to the specified
	 * destination square.  This method is called instead of
	 * {@link #pushBox(int, int)} when a move is undone.
	 * That is important because for undoing a move we have also to check,
	 * whether that action resolves a freeze condition.
	 *
	 * @param fromSquare position of square where the box to be pushed is located
	 * @param toSquare   position of square onto which the box is to be pushed
	 * @return <code>-1</code>  if box couldn't be pushed as requested, or<br>
	 *         <code>>=0</code>, the number of the box successfully pushed.
	 * @see #pushBox(int, int)
	 */
	public int pushBoxUndo(int fromSquare, int toSquare) {
		final int boxNo = justPushBox(fromSquare, toSquare, " (undo)");

		if (boxNo >= 0) {		// did it
			// If the box was frozen, it is not any more frozen, now.
			if (boxData.isBoxFrozen(boxNo)) {
				boxData.setBoxUnfrozen(boxNo);
			}
		}

		return boxNo;
	}

	/**
	 * Returns the distance which the player has to walk,
	 * starting from one square in order to reach a second square.
	 *
	 * @param fromSquare player start position
	 * @param toSquare	 player walk destination
	 * @return number of steps the player needs to walk
	 */
	public int getPlayerDistance(int fromSquare, int toSquare) {
		return playerDistances[fromSquare][toSquare];
	}

	/**
	 * Sets a new board position.
	 *
	 * @param position Board position to be set.
	 */
	public void setBoardPosition(IBoardPosition position) {
		setBoardPosition(position.getPositions());
	}

	/**
	 * Sets a new board position.
	 *
	 * @param positions box and player positions to be set.
	 */
	public void setBoardPosition(int[] positions) {

		// remove all boxes from the board
		removeAllBoxes();

		// store the new box positions in our box data object
		boxData.setBoxPositions(positions);

		// Put the new boxes into the board
		for (int boxNo = 0; boxNo < boxCount; boxNo++) {
			setBoxWithNo(boxNo, positions[boxNo]);
		}

		// set up the new player location
		playerPosition = positions[boxCount];
	}

	/**
	 * Removes all boxes from the board.
	 */
	public void removeAllBoxes() {
		for (int boxNo = 0; boxNo < boxCount; boxNo++) {
			removeBox(boxData.getBoxPosition(boxNo));
		}
	}

	/**
	 * The goals of the backward search are the box positions at search start.
	 * Here we set the goals for the backward search from the current box
	 * positions.
	 */
	public void setGoalsBackwardsSearch() {

		int goalNo = 0;

		// Set up the new goals for backwards search, and clear the old ones
		for (int position = firstRelevantSquare; position < lastRelevantSquare; position++) {
			if (isBox(position) && isOuterSquareOrWall(position) == false) {
				goalSquareBackwardsSearch[position] = true;
				goalPositionsBackwardsSearch[goalNo++] = position;
			} else {
				goalSquareBackwardsSearch[position] = false;
			}
		}
	}

	/**
	 * We optimize the board dimensions for the editor.
	 * E.g. if a level is just 5x5 squares large,
	 * we extend the board with empty squares, as long as it still fits
	 * the screen, without changing the size of the graphic elements.
	 *
	 * @param GUI of JSoko (TODO: only needed as long as editor is still just a quick-and-dirty implementation)
	 * @return array containing the modifications of the board,
	 *         indexed by direction
	 */
	public int[] optimizeBoardSizeForEditor(GUI applicationGUI) {

		// Optimal width and height of the game board
		int optimalBoardWidth  = 0;
		int optimalBoardHeight = 0;

		// Loop variables for columns and rows, which have to survive their loops
		int x;
		int y;

		// Width and height of the game board panel
		int boardWidthInPixel  = 0;
		int boardHeightInPixel = 0;

		// The number of empty rows / columns at each side
		int noEmptyRowsAbove;
		int noEmptyRowsBelow;
		int noEmptyColumnsLeft;
		int noEmptyColumnsRight;

		// The object we are going to return: by how many squares we change the board size
		// at the 4 sides.
		int[] modifications = new int[4];

		// Current minimal width and height of the level
		// (including "empty border").
		int minimalBoardWidth  = 0;
		int minimalBoardHeight = 0;

		// Determine width and height of current board in pixels
		boardWidthInPixel  = applicationGUI.mainBoardDisplay.getWidth();
		boardHeightInPixel = applicationGUI.mainBoardDisplay.getHeight();

		// If the first level which we load is one which activates the editor,
		// already, the main panel has not yet been drawn, and does not have
		// a size, yet.  That case is catched/detected in the graphic
		// painting object, which calls this method again, when the graphic
		// size of the panel has been determined.
		if (boardWidthInPixel == 0) {
			return modifications;
		}

		// Determine the number of empty lines at top
		// (the first line is always empty)
		noEmptyRowsAbove = 0;
		jumplabel: for (y = 1; y < height - 1; y++) {
			for (x = 1; x < width - 1; x++) {
				if (!isEmptySquare(x, y) || playerPosition == x + y * width) {
					break jumplabel;
				}
			}
		}
		noEmptyRowsAbove = y;

		// Determine the number of empty lines at bottom
		// (the last line is always empty)
		noEmptyRowsBelow = 0;
		jumplabel: for (y = height - 2; y > 0; y--) {
			for (x = 1; x < width - 1; x++) {
				if (!isEmptySquare(x, y) || playerPosition == x + y * width) {
					break jumplabel;
				}
			}
		}
		noEmptyRowsBelow = height - y - 1;

		// Determine the number of empty columns at left side
		// (the first column is always empty)
		noEmptyColumnsLeft = 0;
		jumplabel: for (x = 1; x < width - 1; x++) {
			for (y = 1; y < height - 1; y++) {
				if (!isEmptySquare(x, y) || playerPosition == x + y * width) {
					break jumplabel;
				}
			}
		}
		noEmptyColumnsLeft = x;

		// Determine the number of empty columns at right side
		// (the last column is always empty)
		noEmptyColumnsRight = 0;
		jumplabel: for (x = width - 2; x > 0; x--) {
			for (y = 1; y < height - 1; y++) {
				if (!isEmptySquare(x, y) || playerPosition == x + y * width) {
					break jumplabel;
				}
			}
		}
		noEmptyColumnsRight = width - x - 1;

		// Determine minimal dimension of the level.
		// At each side we must leave free one square.
		minimalBoardWidth  = width  - noEmptyColumnsLeft - noEmptyColumnsRight + 2;
		minimalBoardHeight = height - noEmptyRowsAbove   - noEmptyRowsBelow    + 2;
		if (Transformation.isWidthAndHeightInterchanged()) {
			int           temp = minimalBoardWidth;
			minimalBoardWidth  = minimalBoardHeight;
			minimalBoardHeight = temp;
		}
		// The absolute minimum for dimensions is 3 squares
		if (minimalBoardWidth < 3) {
			minimalBoardWidth = 3;
		}
		if (minimalBoardHeight < 3) {
			minimalBoardHeight = 3;
		}

		// Compute the maximal possible scaling with respect to the available space
		float widthScalingFactor = (boardWidthInPixel
				- 2	* Settings.X_BORDER_OFFSET - Settings.X_OFFSET_EDITORELEMENTS)
				/ (float) minimalBoardWidth
				/ applicationGUI.mainBoardDisplay.getCurrentSkin().graphicWidth;
		float heightScalingFactor = (boardHeightInPixel - 90)
				/ (float) minimalBoardHeight
				/ applicationGUI.mainBoardDisplay.getCurrentSkin().graphicHeight;

		// Since we shall not use different scaling factors for width and height,
		// we have to choose the one, which makes the result smaller...
		// which is the smaller value: the minimum.
		float scalingFactor = Math.min(widthScalingFactor, heightScalingFactor);

		// We shall not exceed the maximal scaling (as set by the user)
		int maximumScaling = Settings.getInt("maximumScaling", 3);
		if (scalingFactor > maximumScaling) {
			scalingFactor = maximumScaling;
		}

		// Compute the new width of graphic elements in pixels.
		// The graphic height has the same numeric value (we just forced that).
		int graphicSize = (int) (applicationGUI.mainBoardDisplay.getCurrentSkin().graphicWidth * scalingFactor);

		// Determine new optimal board dimensions
		optimalBoardWidth  = (boardWidthInPixel - 2 * Settings.X_BORDER_OFFSET - Settings.X_OFFSET_EDITORELEMENTS) / graphicSize;
		optimalBoardHeight = (boardHeightInPixel - 90) / graphicSize;

		// If width and height are exchanged in the graphic display,
		// we also exchange them here.
		if (Transformation.isWidthAndHeightInterchanged()) {
			int           temp = optimalBoardWidth;
			optimalBoardWidth  = optimalBoardHeight;
			optimalBoardHeight = temp;
		}

		// We have to respect a maximal board dimension from the settings (a constant)
		if (optimalBoardWidth > Settings.MAXIMUM_BOARDSIZE) {
			optimalBoardWidth = Settings.MAXIMUM_BOARDSIZE;
		}
		if (optimalBoardHeight > Settings.MAXIMUM_BOARDSIZE) {
			optimalBoardHeight = Settings.MAXIMUM_BOARDSIZE;
		}

		// Reduce the board to the needed minimum
		// FFS: shouldn't we compare "> 1" instead of "> 0" ?
		if (noEmptyRowsAbove > 0 || noEmptyRowsBelow > 0 || noEmptyColumnsLeft > 0 || noEmptyColumnsRight > 0) {
			downsizeBoard( noEmptyRowsAbove   - 1, noEmptyRowsBelow    - 1,
					       noEmptyColumnsLeft - 1, noEmptyColumnsRight - 1 );
		}

		modifications[UP   ] -= (noEmptyRowsAbove    - 1);
		modifications[DOWN ] -= (noEmptyRowsBelow    - 1);
		modifications[LEFT ] -= (noEmptyColumnsLeft  - 1);
		modifications[RIGHT] -= (noEmptyColumnsRight - 1);

		int noRowsToInsertAbove = 0;
		int noRowsToInsertBelow = 0;
		int noColumnsToInsertLeft = 0;
		int noColumnsToInsertRight = 0;

		// Ermitteln, um wieviele Zeilen / Spalten das Spielfeld erweitert werden muss.
		// Falls nicht symmetrisch erweitert werden kann, so werden die überzähligen Zeilen /
		// Spalten unten bzw. rechts eingefügt.
		noRowsToInsertAbove = (optimalBoardHeight - height) / 2;
		noRowsToInsertBelow = optimalBoardHeight - height - noRowsToInsertAbove;
		noColumnsToInsertLeft = (optimalBoardWidth - width) / 2;
		noColumnsToInsertRight = optimalBoardWidth - width - noColumnsToInsertLeft;

		// Make sure we do not make it smaller.
		noRowsToInsertAbove    = (noRowsToInsertAbove    < 0) ? 0 : noRowsToInsertAbove;
		noRowsToInsertBelow    = (noRowsToInsertBelow    < 0) ? 0 : noRowsToInsertBelow;
		noColumnsToInsertLeft  = (noColumnsToInsertLeft  < 0) ? 0 : noColumnsToInsertLeft;
		noColumnsToInsertRight = (noColumnsToInsertRight < 0) ? 0 : noColumnsToInsertRight;

		// Spielfeld falls möglich erweitern.
		if (noRowsToInsertAbove > 0 || noRowsToInsertBelow > 0 || noColumnsToInsertLeft > 0 || noColumnsToInsertRight > 0) {
			extendBoardSize( noRowsToInsertAbove,   noRowsToInsertBelow,
					         noColumnsToInsertLeft, noColumnsToInsertRight );
		}

		// Errechnen, wieviele Zeilen bzw. Spalten an der jeweiligen Seite hinzugefügt wurden
		modifications[UP   ] += noRowsToInsertAbove;
		modifications[DOWN ] += noRowsToInsertBelow;
		modifications[LEFT ] += noColumnsToInsertLeft;
		modifications[RIGHT] += noColumnsToInsertRight;

		return modifications;
	}

	/**
	 * This method inserts new rows and/or columns at the indicated sides.
	 *
	 * @param noToExtendAbove  count of lines to be inserted above
	 * @param noToExtendBelow  count of lines to be inserted below
	 * @param noToExtendLeft  count of columns to be inserted left
	 * @param noToExtendRight count of columns to be inserted right
	 */
	public void extendBoardSize(int noToExtendAbove, int noToExtendBelow, int noToExtendLeft, int noToExtendRight) {

		// save old size and width
		final int oldBoardSize  = size;
		final int oldBoardWidth = width;

		// change height and width, to new values
		height += noToExtendAbove + noToExtendBelow;
		width  += noToExtendLeft  + noToExtendRight;

		// compute new size
		size = width * height;

		// Die Arrays, die noch nicht benutzt wurden, neu allokieren.
		marked = new boolean[size];

		// recompute steps array
		makeOffsets();

		// Reallocate the arrays which hold player reachability
		playersReachableSquares = new PlayersReachableSquares();
		playersReachableSquaresOnlyWalls = new PlayersReachableSquaresOnlyWalls();

		/*
		 * The arrays already in use have to be copied...
		 */

		// we still need the walls/goals/boxes arrays for copying
		byte[]    oldWallsArray = wallsArray;
		boolean[] oldGoalsArray = goalsArray;
		boolean[] oldBoxesArray = boxesArray;

		// allocate arrays for walls/goals/boxes with new size
		wallsArray = new byte   [size];
		goalsArray = new boolean[size];
		boxesArray = new boolean[size];

		// copy 3 arrays according to the extension
		for (int position = 0; position < oldBoardSize; position++) {
			// Position des Feldes im neuen Spielfeldobjekt ermitteln
			int newPosition = position;

			// Zeilen verrutschen nach unten
			newPosition -= noToExtendAbove * offset[UP];

			// Falls links eine neue Spalte eingefügt werden soll, wird das alte
			// Spielfeld um ein Feld nach rechts verschoben in das neue Spielfeld
			// kopiert.
			newPosition += noToExtendLeft;

			// Für jede neue Spalte im alten Spielfeld ist ein neues
			// Feld im neuen Spielfeld hinzugekommen.
			newPosition += (noToExtendLeft + noToExtendRight) * (position / oldBoardWidth);

			wallsArray[newPosition] = oldWallsArray[position];
			goalsArray[newPosition] = oldGoalsArray[position];
			boxesArray[newPosition] = oldBoxesArray[position];
		}

		// Move the player accordingly
		if (playerPosition != NO_PLAYER) {
			playerPosition =  playerPosition
							- noToExtendAbove * offset[UP]
							+ noToExtendLeft
							+ (noToExtendLeft + noToExtendRight) * (playerPosition / oldBoardWidth);
		}

		// Tell the new size to the transformation object
		Transformation.setNewLevelSize();
	}

	/**
	 * Diese Methode verkleinert das Spielfeld an der angegebenen Positionen um Zeilen bzw. Spalten
	 * TODO: this is editor code -> move to editor
	 * @param noToEraseAbove   Anzahl Zeilen, die oberhalb gelöscht werden sollen
	 * @param noToEraseBelow  Anzahl Zeilen, die unterhalb gelöscht werden sollen
	 * @param noToEraseLeft  Anzahl Spalte, die links gelöscht werden sollen
	 * @param noToEraseRight Anzahl Spalte, die rechts gelöscht werden sollen
	 */
	public void downsizeBoard(int noToEraseAbove, int noToEraseBelow, int noToEraseLeft, int noToEraseRight) {

		// Alte Feldbreite
		final int oldBoardWidth = width;

		// Die Feldbreite und Feldhöhe auf die neuen Werte anpassen
		height -= (noToEraseAbove + noToEraseBelow);
		width  -= (noToEraseLeft  + noToEraseRight);

		// Never make dimensions negative (what could happen for a completely
		// empty board)
		height = (height < 0) ? 0 : height;
		width  = (width  < 0) ? 0 : width;
		// FIXME: in case we clip negative values, the "erase" values cannot be used as below.
		// We rather need to first fix up noToEraseAbove etc

		// compute size
		size = width * height;

		// Die Arrays, die noch nicht benutzt wurden, neu allokieren.
		marked = new boolean[size];

		// recompute steps array
		makeOffsets();

		// Die Objekte neu erzeugen, die die erreichbaren Felder des Spielers errechnen
		playersReachableSquares          = new PlayersReachableSquares();
		playersReachableSquaresOnlyWalls = new PlayersReachableSquaresOnlyWalls();

		/*
		 * Die schon in Benutzung befindlichen Arrays müssen umkopiert werden.
		 */
		// we still need the walls/goals/boxes arrays for copying
		byte   [] oldWallsArray = wallsArray;
		boolean[] oldGoalsArray = goalsArray;
		boolean[] oldBoxesArray = boxesArray;

		// allocate arrays for walls/goals/boxes with new size
		wallsArray = new byte   [size];
		goalsArray = new boolean[size];
		boxesArray = new boolean[size];

		// copy 3 arrays according to the reduction
		for (int newPosition = 0; newPosition < size; newPosition++) {
			// Position des Feldes im alten Spielfeldobjekt ermitteln
			int position = newPosition;

			// Zeilen verrutschen nach oben
			position += noToEraseAbove * oldBoardWidth;

			// Falls links eine neue Spalte gelöscht werden soll, wird das alte
			// Spielfeld um ein Feld nach links verschoben in das neue Spielfeld
			// kopiert.
			position += noToEraseLeft;

			// Für jede gelöschte Spalte ist ein neues
			// Feld mehr im alten Spielfeld vorhanden.
			position += (noToEraseLeft + noToEraseRight) * (newPosition / width);

			wallsArray[newPosition] = oldWallsArray[position];
			goalsArray[newPosition] = oldGoalsArray[position];
			boxesArray[newPosition] = oldBoxesArray[position];
		}

		// Move the player accordingly
		if (playerPosition != NO_PLAYER) {
			playerPosition = (playerPosition % oldBoardWidth) - noToEraseLeft + (playerPosition / oldBoardWidth - noToEraseAbove) * width;
		}

		// Tell the new size to the transformation object
		Transformation.setNewLevelSize();
	}

	/**
	 * Kopiert das aktuelle Spielfeld transformiert in sich selbst. Dies ist notwendig, wenn
	 * der Editormodus verlassen wird. Dann wird das Spielfeld so wie es auf dem Bildschirm
	 * ausgegeben wird in sich selbst kopiert, wobei leere Zeilen und Spalten entfernt werden.
	 *
	 * @param applicationGUI  GUI of JSoko
	 */
	public void adoptBoardFromEditor(GUI applicationGUI) {

		// Nehmen Zeilen- bzw. Spaltennummern auf
		int x, y;

		/*
		 * Zunächst wird das Spielfeld so weit wie möglich verkleinert.
		 * Allerdings sind Spielfelder unter einer Größe von 3 * 3 nicht sinnvoll.
		 */
		// Leere Zeilen oben entfernen
		do {
			for (x = 0; x < width; x++) {
				if (!isEmptySquare(x, 0) || playerPosition == x) {
					break;
				}
			}
			if (x == width) {
				downsizeBoard(1, 0, 0, 0);
			}
		} while (x == width && height > 3);

		// Leere Zeilen unten entfernen
		do {
			for (x = 0; x < width; x++) {
				if (!isEmptySquare(x, height - 1) || playerPosition == x + (height - 1) * width) {
					break;
				}
			}
			if (x == width) {
				downsizeBoard(0, 1, 0, 0);
			}
		} while (x == width && height > 3);

		// Leere Spalten links entfernen
		do {
			for (y = 0; y < height; y++) {
				if (!isEmptySquare(0, y) || playerPosition == y * width) {
					break;
				}
			}
			if (y == height) {
				downsizeBoard(0, 0, 1, 0);
			}
		} while (y == height && width > 3);

		// Leere Spalten rechts entfernen
		do {
			for (y = 0; y < height; y++) {
				if (!isEmptySquare(width - 1, y) || playerPosition == width - 1 + y * width) {
					break;
				}
			}
			if (y == height) {
				downsizeBoard(0, 0, 0, 1);
			}
		} while (y == height && width > 3);

		// we still need the walls/goals/boxes arrays for copying
		byte   [] oldWallsArray = wallsArray;
		boolean[] oldGoalsArray = goalsArray;
		boolean[] oldBoxesArray = boxesArray;

		// allocate arrays for walls/goals/boxes with new size
		wallsArray = new byte[size];
		goalsArray = new boolean[size];
		boxesArray = new boolean[size];

		simpleDeadlockSquareForwards = new boolean[size];
		simpleDeadlockSquareBackwards = new boolean[size];
		advancedSimpleDeadlockSquareForwards = new boolean[size];
		marked = new boolean[size];

		// Die alten Arrays transformiert in die neuen Arrays kopieren
		for (int position = 0; position < size; position++) {
			wallsArray[Transformation.getExternalPosition(position)] = oldWallsArray[position];
			goalsArray[Transformation.getExternalPosition(position)] = oldGoalsArray[position];
			boxesArray[Transformation.getExternalPosition(position)] = oldBoxesArray[position];
		}

		// Spielerposition transformieren
		if (playerPosition != NO_PLAYER) {
			playerPosition = Transformation.getExternalPosition(playerPosition);
		}

		// If the transformation exchanged width and heihgt in the output,
		// we also need to exchange them in the board.
		if (Transformation.isWidthAndHeightInterchanged()) {
			int temp = width;
			width    = height;
			height   = temp;

			// recompute steps array
			makeOffsets();
		}

		// Inform the transformation object about the new board and set "no transformation".
		Transformation.newlevel();

		// compute wall graphics for the new level
		applicationGUI.mainBoardDisplay.setBoardToDisplay(this);
	}

	/*
	 * Clones the current board.
	 */
	@Override
	public Board clone() {

		Board newBoard = new Board();

		// Set the elements of the board in the new board. (quick and dirty programming)
		try {
			newBoard.setBoardFromString(getBoardDataAsString());
		} catch (Exception e) {
			e.printStackTrace();
		}
		newBoard.isValid(new StringBuilder());
		newBoard.prepareBoard();

		return newBoard;
	}

	/**
	 * Returns a <code>String</code> containing the data of the currently shown board.
	 *
	 * @return the <code>String</code> representing this board as string
	 */
	public String getBoardDataAsString() {

		// The board as string.
		StringBuilder boardAsString = new StringBuilder(size+height+2);

		for (int position = 0; position < size; position++) {

			// Check whether this is the beginning of a new line.
			if (position > 0 && position % width == 0) {
				Utilities.removeTrailingSpaces(boardAsString);
				boardAsString.append('\n');
			}

			if (isWall(position)) {
				boardAsString.append('#');
				continue;
			}
			if (isBoxOnGoal(position)) {
				boardAsString.append('*');
				continue;
			}
			if (isBox(position)) {
				boardAsString.append('$');
				continue;
			}
			if (isGoal(position)) {
				if (playerPosition == position) {
					boardAsString.append('+');
				} else {
					boardAsString.append('.');
				}
				continue;
			}
			if (playerPosition == position) {
				boardAsString.append('@');
				continue;
			}

			// If no specific board element is at the relevant position the square is empty.
			boardAsString.append(' ');
		}

		// Remove trailing spaces of the last line.
		Utilities.removeTrailingSpaces(boardAsString);

		boardAsString.append('\n');
		return boardAsString.toString();
	}

	/**
	 * Scans the <code>board</code> and its <code>boxData</code> to count the boxes,
	 * which are on goals.
	 *
	 * @return the number of boxes on goals
	 */
	public int getBoxesOnGoalsCount() {
		int boxOnGoalCounter = 0;
		for (int boxNo = 0; boxNo < boxCount; boxNo++) {
			if (isGoal(boxData.getBoxPosition(boxNo))) {
				boxOnGoalCounter++;
			}
		}

		return boxOnGoalCounter;
	}

	/**
	 * Class for calculating the player path from one position to another.
	 */
	public class PlayerPathCalculation {

		/** Marker for the start position of the player. Used for indicating */
		private final static int START_POSITION_MARKER = Integer.MIN_VALUE;

		/** Indicating that a position hasn't been reached from any direction, yet. */
		private final static int NONE = -1;

		/** All reached positions are stored in this queue. */
		private final int[] queue;

		/** Storage for saving which position has been reached from which previous position. */
		private final int[] reachedFromPosition;


		/**
		 * Creates a new object for calculatin the player path.
		 */
		public PlayerPathCalculation() {
			queue = new int[size];
			reachedFromPosition = new int[size];
		}

		/**
		 * Returns the positions of the shortest path to be gone by the player
		 * to get from the current player position to the passed target position.
		 *
		 * @param targetPosition  target position of the player
		 * @return array of positions to be gone to reach the target position
		 * including the start position and the target position<br>.
		 * If the start position is the target position then the array is empty.<br>
		 * If there is no path to the target position <code>null</code> is returned.
		 */
		public int[] getPathTo(final int targetPosition) {
			return getPath(playerPosition, targetPosition);
		}

		/**
		 * Returns the positions of the shortest path to be gone by the player
		 * to get from the passed start position to the passed target position.
		 *
		 * @param startPosition   current position of the player
		 * @param targetPosition  target position of the player
		 * @return array of positions to be gone to reach the target position
		 * including the start position and the target position<br>.
		 * If the start position is the target position then the array only
		 * contains the target position. Hence, the length of the array is
		 * always "moves to be done + 1".<br>
		 * If there is no path to the target position <code>null</code> is returned.
		 */
		public int[] getPath(final int startPosition, final int targetPosition) {

			// Quick check whether the target position is already been reached.
			if(startPosition == targetPosition) {
				return new int[] { targetPosition };//TODO: FFS/mm: isn't it better to return new int[0]?
			}

			// Initialize the reachedFrom array using invalid positions.
			for(int i=0; i<reachedFromPosition.length; i++) {
				reachedFromPosition[i] = NONE;
			}

			// Initialization of the queue indices.
			int indexToWrite = 0;
			int indexToTake  = 0;

			// Mark the start position as reached by setting an invalid previous position.
			reachedFromPosition[startPosition] = START_POSITION_MARKER;

			int currentPlayerPosition = startPosition;

			// Do a breadth first search to find the shortest path to the target position.
			do {
				// Move the player to all four directions.
				for(int directionOffset : offset) {

					// Calculate the new player position when moving to the direction.
					final int newPlayerPosition = currentPlayerPosition + directionOffset;

					// Check whether the player can move to the new position and has never moved there before.
					if(!isBoxOrWall(newPlayerPosition) && reachedFromPosition[newPlayerPosition] == NONE) {

						// Check whether the target position has been reached.
						if (newPlayerPosition == targetPosition) {

							indexToWrite = queue.length-1;

							// Target position must be included.
							queue[indexToWrite] = newPlayerPosition;

							// Go backwards from the target position to the start position and
							// save the path gone.
							do {
								queue[--indexToWrite] = currentPlayerPosition;
								currentPlayerPosition = reachedFromPosition[currentPlayerPosition];
							}while(currentPlayerPosition != START_POSITION_MARKER);

							// Copy the path into an array having just the right size and return it.
							return Arrays.copyOfRange(queue, indexToWrite, queue.length);
						}

						// Save from which position the new position has been reached.
						reachedFromPosition[newPlayerPosition] = currentPlayerPosition;

						// It must be checked if any neighbor of the new
						// position can be reached, too => queue the new
						// position for further processing.
						queue[indexToWrite++] = newPlayerPosition;
					}
				}

				// Get the next position from the queue.
				currentPlayerPosition = queue[indexToTake++];

			}while(indexToTake <= indexToWrite); // As long as there are positions in the queue

			// Return null since no path to the target position has been found.
			return null;
		}
	}


	/**
	 * This class combines the 3 arrays which form an entry
	 * of a {@link DistCache} as used in {@link Distances}.
	 */
	public class DistCacheElement {
		short[][][] boxDistances;
		boolean[]   corralForcer;
		short[][]   playerDistances;

		/**
		 * Object for storing box distances, corral forcer positions and player distances.
		 *
		 * @param boxDistances
		 * @param corralForcer
		 * @param playerDistances
		 */
		public DistCacheElement(
				short[][][] boxDistances,
				boolean[]   corralForcer,
				short[][]   playerDistances )
		{
			this.boxDistances    = boxDistances;
			this.corralForcer    = corralForcer;
			this.playerDistances = playerDistances;
		}
	}

	/**
	 * A cache for the computation of box distances based on the current
	 * box freeze situation.
	 * Forward and backward search use seperate cache objects.
	 *
	 * @author Heiner Marxen
	 */
	public class DistCache extends LruCache<BitSet, DistCacheElement> {
		/*
		 * We take advantage of:
		 * - BitSet (the key) has a good hashCode() (data based)
		 * - BitSet (the key) has a deep clone()
		 * - all the remembered arrays get used by reference (not by copy),
		 *   since their contents are never changed after computation.
		 */

		/**
		 * Creates a new cache for box distances.
		 *
		 * @param initialCapacity  initial capacity
		 */
		public DistCache(final int initialCapacity) {
			super(initialCapacity);
			this.setMinRAMinMiB(10);
		}
	}

	/**
	 * Computes the box distances from any square to any square,
	 * depending on the player position.
	 */
	public class Distances {

		// [Richtung][vonPosition][nachPosition]
		// Richtung = wo der Spieler relativ zur vonPosition steht. Ein Eintrag im Array steht jeweils für
		// die minimale Kistendistanz einer Kiste, die von der "vonPosition" zur "nachPosition" geschoben
		// wird (wird in "berechneKistenentfernungen" berechnet)
		protected short[][][] boxDistancesForwards;
		protected short[][][] boxDistancesBackwards;

		/**
		 * For performance reasons we want to cache the results of box
		 * distance computations (which change each time a box is frozen).
		 */
		private DistCache distancesForwCache;
		private DistCache distancesBackCache;
		private int[] goalsPositionsBackwardsSearchUsedForCaching = null;

		// Inhalt der Variablen = auf welchem Zielfeld steht eine geblockte Kiste.
		// Steht z.B. auf dem 5. Zielfeld eine geblockte Kiste ist Bit Nr. 5 gesetzt.
		private BitSet currentFreezeSituationForwards;
		private BitSet currentFreezeSituationBackwards;

		/**
		 * Maximally necessary size of a Q to contain one entry for each
		 * possible pair of (boxPosition, playerPosition), for a maximally
		 * large level.
		 */
		public static final int BOX_PL_Q_SIZE = Settings.MAXIMUM_BOARDSIZE * Settings.MAXIMUM_BOARDSIZE * 4;

		/**
		 * This array is part of a queue (Q), used to perform a reachability analysis.
		 * Each entry in the Q consists of 3 components, each of which has its own array.
		 * This one stores positions of boxes.
		 * The other 2 components are {@link #playerPositionsQ} for player positions,
		 * and {@link #distancesQ} for distances.
		 * <p>
		 * Implemented globally for performance reasons.
		 */
		short[] boxPositionsQ    = new short[BOX_PL_Q_SIZE];
		/** Second part of the Q described at {@link #boxPositionsQ} */
		short[] playerPositionsQ = new short[BOX_PL_Q_SIZE];
		/** Third  part of the Q described at {@link #boxPositionsQ} */
		short[] distancesQ       = new short[BOX_PL_Q_SIZE];

		/**
		 * Constructor
		 */
		public Distances() {
			distancesForwCache = new DistCache(5);	// FFS/hm capacity
			distancesBackCache = new DistCache(5);
			currentFreezeSituationForwards  = new BitSet(goalsCount);
			currentFreezeSituationBackwards = new BitSet(goalsCount);
		}

		/**
		 * Returns the push distance of a specific box to a specific goal.
		 * <p>
		 * The distance is calculated under the assumption that:
		 * <ol>
		 *  <li> the box is the only one on the whole board
		 *  <li> the player can reach every side of the box at the moment
		 * </ol>
		 *
		 * @param boxNo number of the relevant box
		 * @param goalNo number of the relevant goal
		 * @return push distance or #
		 */
		public int getBoxDistanceForwardsPlayerPositionIndependentNo(int boxNo, int goalNo) {
			return getBoxDistanceForwardsPlayerPositionIndependent(boxData.getBoxPosition(boxNo), goalsPositions[goalNo]);
		}

		/**
		 * Returns the push distance needed for pushing a box from the passed
		 * start position to the passed target position.
		 * <p>
		 * The distance is calculated under the assumption that:
		 * <ol>
		 *  <li> the box is the only one on the whole board
		 *  <li> the player can reach every side of the box at the moment
		 * </ol>
		 * @param startPosition  position the pushing begins
		 * @param targetPosition position the pushing ends
		 * @return push distance or {@link Board#UNREACHABLE}
		 */
		public int getBoxDistanceForwardsPlayerPositionIndependent(int startPosition, int targetPosition) {

			// If it isn't a corral forcer square then the position of the player doesn't matter because the
			// player can reach ever side of the box. Hence the precalculated distances are equally high for
			// every player position.
			if (isCorralForcerSquare(startPosition) == false) {
				return boxDistancesForwards[UP][startPosition][targetPosition];
			}

			// Determine the minimum push distance assuming the player can reach every side of the box.
			int minimalDistance = UNREACHABLE;
			for (int direction = 0; direction < DIRS_COUNT; direction++) {
				if (minimalDistance > boxDistancesForwards[direction][startPosition][targetPosition]) {
					minimalDistance = boxDistancesForwards[direction][startPosition][targetPosition];
				}
			}
			// Return the determined distance.
			return minimalDistance;

		}

		/**
		 * Gibt die Kistendistanz zu einem bestimmten Zielfeld zurück, wenn nur diese Kiste
		 * auf dem Feld stehen würde und der Spieler an der aktuellen Position steht.
		 * Vorwärts = die Kiste wird geschoben und nicht gezogen.
		 *
		 * @param boxNo		Nummer der Kiste, für die die Distanz berechnet werden soll
		 * @param goalNo	Nummer des Zielfeldes, das die Kiste erreichen soll
		 *
		 * @return the distance of the box, {@link Board#UNREACHABLE} if the box can't reach the goal
		 */
		public int getBoxDistanceForwardsNo(int boxNo, int goalNo) {
			return getBoxDistanceForwardsPosition(boxData.getBoxPosition(boxNo), goalsPositions[goalNo]);
		}

		/**
		 * Gibt die Kistendistanz zurück, die eine Kiste zurücklegen muss, um von der Startposition
		 * auf die Zielposition geschoben zu werden.
		 * Annahmen: Es steht nur diese eine Kiste auf dem Feld und der Spieler steht an der
		 * aktuellen Position.
		 * Vorwärts = die Kiste wird geschoben und nicht gezogen.
		 *
		 * @param fromSquare 	Startposition von wo aus die Distanz berechnet werden soll
		 * @param toSquare	Position des Zielfeldes, das die Kiste erreichen soll
		 * @return return Kistendistanz vom Start- zum Zielfeld, UNENDLICH = Kiste kann Ziel nicht erreichen
		 */
		public int getBoxDistanceForwardsPosition(int fromSquare, int toSquare) {

			// Only for influence calculations all distances are calculated. Hence, it's most likey a bug that a distance to a non goal
			// is to be returned.
			if(Debug.isDebugModeActivated && isGoal(toSquare) == false && Debug.debugShowInfluenceColors == false && Debug.debugShowInfluenceValues == false) {
				System.out.println("box distances have only been calculated to goal positions!");
			}

			// Falls es sich nicht um ein Corralerzwingerfeld handelt ist die Position des Spielers
			// egal und es kann eine beliebige Richtung angenommen werden.
			// (bei einem NichtCorralfeld ist die Distanz immer gleich groß, egal auf welchem Nachbar-
			// feld der Kiste der Spieler steht. Selbst wenn der Spieler auf einer Mauer steht!
			// -> siehe dazu "berechneKistenentfernungen...")
			if (isCorralForcerSquare(fromSquare) == false) {
				return boxDistancesForwards[UP][fromSquare][toSquare];
			}

			// Falls der Spieler "auf" der Kiste steht kann er auf alle Seiten der Kiste gelangen
			// und es muss die kleinste Distanz aller Richtungen zurückgegeben werden.
			// Dieser Fall kann auftreten, da während der Corralanalyse Kisten vom Feld genommen
			// werden. Wird nun z.B. in Freeze der Spieler auf diese "freie" Position gestellt,
			// so steht der Spieler auf einer Kiste, da der Lowerbound alle Kisten berücksichtigt,
			// auch die deaktiven! (Lowerbound bekommt die Kistenpositionen aus dem
			// boxData-Object in board)
			if (playerPosition == fromSquare) {
				int minimalDistance = UNREACHABLE;
				for (int direction = 0; direction < DIRS_COUNT; direction++) {
					if (minimalDistance > boxDistancesForwards[direction][fromSquare][toSquare]) {
						minimalDistance = boxDistancesForwards[direction][fromSquare][toSquare];
					}
				}
				return minimalDistance;
			}

			/*
			 * Die Kiste trennt das Spielfeld in Bereiche, die durch den Spieler erreichbar sind
			 * und die nicht durch den Spieler erreichbar sind. In diesem Fall ist es also
			 * wichtig zu wissen, auf welche Seite der Kiste der Spieler gelangen kann.
			 * Dazu wird folgendes gemacht:
			 * Es wird zunächst die Distanz ermittelt, die der Spieler benötigen würde die
			 * Startposition direkt zu erreichen. Um diese Startposition direkt zu erreichen,
			 * muss der Spieler über eines der 4 Nachbarfelder laufen. Für das Erreichen eines
			 * der Nachbarfelder benötigt er weniger Bewegungen. Es muss also nur ermittelt
			 * werden, welches Nachbarfeld er mit weniger Bewegungen erreichen kann.
			 */
			int playerDistance = getPlayerDistance(playerPosition, fromSquare);

			for (int direction = 0; direction < DIRS_COUNT; direction++) {
				if (getPlayerDistance(playerPosition, fromSquare + offset[direction]) < playerDistance) {
					return boxDistancesForwards[direction][fromSquare][toSquare];
				}
			}

			// Falls die Startposition der Zielposition entspricht ist die Distanz 0.
			// Dies muss extra abgefangen werden, da es sein kann, dass eine Kiste auf einem
			// Zielfeld für den Spieler aufgrund von Blockerkisten unerreichbar wird.
			if (fromSquare == toSquare) {
				return 0;
			}

			// Die Kiste ist nicht durch den Spieler erreichbar. Dies kann sein, wenn eine Blockerkiste
			// wie eine Mauer behandelt wird und dadurch eine andere Kiste vom Spielerbereich abtrennt.
			return UNREACHABLE;
		}

		/**
		 * Gibt die Kistendistanz zu einem bestimmten Zielfeld zurück, wenn nur diese Kiste
		 * auf dem Feld stehen würde und der Spieler an der aktuellen Position steht.
		 * Rückwärts = die Kiste wird gezogen und nicht geschoben.
		 *
		 * @param boxNo		Nummer der Kiste, für die die Distanz berechnet werden soll
		 * @param goalNo	Nummer des Zielfeldes, das die Kiste erreichen soll
		 *
		 * @return the distance of the box, infinite if the box can't reach the goal
		 */
		public int getBoxDistanceBackwardsNo(int boxNo, int goalNo) {

			// Achtung! Die Rückwärtssuche hat eigene Zielfelder (nämlich die Startpositionen der Kisten)
			return getBoxDistanceBackwardPosition(boxData.getBoxPosition(boxNo), goalPositionsBackwardsSearch[goalNo]);
		}

		/**
		 * Gibt die Kistendistanz zurück, die eine Kiste zurücklegen muss, um von der Startposition
		 * auf die Zielposition gezogen zu werden.
		 * Annahmen: Es steht nur diese eine Kiste auf dem Feld und der Spieler steht an der
		 * aktuellen Position.
		 * Vorwärts = die Kiste wird geschoben und nicht gezogen.
		 *
		 * @param fromSquare	Position der Kiste, für die die Distanz berechnet werden soll
		 * @param toSquare	Position des Zielfeldes, das die Kiste erreichen soll
		 *
		 * @return the distance of the box, infinite if the box can't reach the toSquare
		 */
		public int getBoxDistanceBackwardPosition(int fromSquare, int toSquare) {

			// Falls es sich nicht um ein Corralerzwingerfeld handelt ist die Position des Spielers
			// egal und es kann eine beliebige Richtung angenommen werden.
			// (bei einem NichtCorralfeld ist die Distanz immer gleich groß, egal auf welchem Nachbar-
			// feld der Kiste der Spieler steht. Selbst wenn der Spieler auf einer Mauer steht!
			// -> siehe dazu "berechneKistenentfernungen...")
			if (isCorralForcerSquare(fromSquare) == false) {
				return boxDistancesBackwards[UP][fromSquare][toSquare];
			}

			// Falls der Spieler "auf" der Kiste steht kann er auf alle Seiten der Kiste gelangen
			// und es muss die kleinste Distanz aller Richtungen zurückgegeben werden.
			// Dieser Fall kann auftreten, da während der Corralanalyse Kisten vom Feld genommen
			// werden. Wird nun z.B. in Freeze der Spieler auf diese "freie" Position gestellt,
			// so steht der Spieler auf einer Kiste, da der Lowerbound alle Kisten berücksichtigt,
			// auch die deaktiven! (Lowerbound bekommt die Kistenpositionen aus dem
			// boxData-Objekt in board)
			// Bei der Durchsuchung eines Zielfeldraumes werden Kisten vom Feld genommen, die den
			// Zielraum bereits verlassen haben. In diesem Fall werden die Kiste so gezählt, als wenn
			// sie mit der minimalen Distanz ein Zielfeld erreichen können.
			if (playerPosition == fromSquare || isBox(fromSquare) == false) {
				int minimalDistance = UNREACHABLE;
				for (int direction = 0; direction < DIRS_COUNT; direction++) {
					if (minimalDistance > boxDistancesBackwards[direction][fromSquare][toSquare]) {
						minimalDistance = boxDistancesBackwards[direction][fromSquare][toSquare];
					}
				}
				return minimalDistance;
			}

			/*
			 * Die Kiste trennt das Spielfeld in Bereiche, die durch den Spieler erreichbar sind
			 * und die nicht durch den Spieler erreichbar sind. In diesem Fall ist es also
			 * wichtig zu wissen, auf welche Seite der Kiste der Spieler gelangen kann.
			 * Dazu wird folgendes gemacht:
			 * Es wird zunächst die Distanz ermittelt, die der Spieler benötigen würde die
			 * Startposition direkt zu erreichen. Um diese Startposition direkt zu erreichen,
			 * muss der Spieler über eines der 4 Nachbarfelder laufen. Für das Erreichen eines
			 * der Nachbarfelder benötigt er weniger Bewegungen. Es muss also nur ermittelt
			 * werden, welches Nachbarfeld er mit weniger Bewegungen erreichen kann.
			 */
			int playerDistance = getPlayerDistance(playerPosition, fromSquare);

			for (byte direction = 0; direction < DIRS_COUNT; direction++) {
				if (getPlayerDistance(playerPosition, fromSquare + offset[direction]) < playerDistance) {
					return boxDistancesBackwards[direction][fromSquare][toSquare];
				}
			}

			// Falls die Startposition der Zielposition entspricht ist die Distanz 0.
			// Dies muss extra abgefangen werden, da es sein kann, dass eine Kiste auf einem
			// Zielfeld für den Spieler aufgrund von Blockerkisten unerreichbar wird.
			if (fromSquare == toSquare) {
				return 0;
			}

			// Die Kiste ist nicht durch den Spieler erreichbar. Dies kann sein, wenn eine Blockerkiste
			// wie eine Mauer behandelt wird und dadurch eine andere Kiste vom Spielerbereich abtrennt.
			return UNREACHABLE;
		}

		/**
		 * Determines the squares on which a box would induce a closed area,
		 * i.e. a corral with just one box.
		 * Example:<pre>
		 *  ###
		 * XX #
		 *  ###</pre>
		 * The squares marked with <code>X</code> are such corral forcer
		 * squares, since a box on one of them would create a closed area,
		 * which is unreachable for the player, even if there were no other
		 * box on the board.
		 * <p>
		 * Please note: we do not imply that such a corral cannot be resolved!
		 * A box on the first <code>X</code>, e.g. could be pushed upwards
		 * and resolve the corral.
		 * To be more precise: these squares create an area, which the player
		 * cannot reach without at least pushing some box.
		 */
		private void identifyCorralForcerSquares() {

			// Position
			int neighbor;

			// We have to create a new array for each call, since these arrays
			// are going to be buffered in a vector.
			corralForcer = new boolean[size];

			// Scan all squares
			PositionsLoop:
			for (int center = firstRelevantSquare; center < lastRelevantSquare; center++) {

				// skip outer squares and walls
				if (isOuterSquareOrWall(center)) {
					continue;
				}

				int wallMask = 0;
				int startPosition = center + offset[UP] + offset[LEFT];
				int bitNo = 0;
				for (int x = 0; x < 3; x++) {
					for (int y = 0; y < 3; y++) {
						neighbor = startPosition + x + y * offset[DOWN];
						if (neighbor != center) {
							if (isWall(neighbor)) {
								wallMask |= (1 << bitNo);
							}
							bitNo++;
						}
					}
				}

				// When the current neighborhood is a potential corral forcer,
				// we determine, whether it is a real corral forcer.
				if (corralForcerSituations[wallMask]) {

					// We search for two non-wall neighbor squares, such that
					// one is not player reachable from the other.
					// To enumerate neighbors we enumerate ordered pairs
					// of different directions.
					// FFS/hm: we could still reduce the call to "update"
					for (int dir1 = 0; dir1 < (DIRS_COUNT-1); dir1++) {
						neighbor = center + offset[dir1];

						// Skip neighbors blocked by walls
						if (isWall(neighbor)) {
							continue;
						}

						// Determine the reachable squares for the neighbor
						// square, with a wall at the center.
						setWall(center);
						playersReachableSquaresOnlyWalls.update(neighbor);
						removeWall(center);

						// Search for a different neighbor, which is unreachable
						for (int dir2 = dir1 + 1; dir2 < DIRS_COUNT; dir2++) {
							neighbor = center + offset[dir2];

							// Skip neighbors blocked by walls
							if (isWall(neighbor)) {
								continue;
							}

							if (playersReachableSquaresOnlyWalls.isSquareReachable(neighbor) == false) {
								// Placing a wall on the "center" makes this
								// neighbor unreachable from the other one:
								// that splits the formerly connected area.
								corralForcer[center] = true;
								continue PositionsLoop;
							}
						}
					}
				}
			}
		}

		/**
		 * DEBUG: For debug purposes we show all potential corral forcer situations,
		 * as encoded in {@link Board#corralForcerSituations}.
		 * Reachable from the debug menu by "Show corralforcer situations"
		 * with action name "showCorralForcerSituations".
		 */
		public void debugShowCorralForcerSituations() {

			// Backup the board position.
			AbsoluteBoardPositionMoves backup = new AbsoluteBoardPositionMoves(Board.this);

			int currentPosition = 0;
			final int centerPosition = 2 + 2 * offset[DOWN];
			setPlayerPosition(centerPosition);
			int squareNo = 0;
			for (int i = 0; i < 255; i++) {
				if (corralForcerSituations[i] == true) {
					for (int i2 = 0; i2 < size; i2++) {
						removeBox(i2);
					}
					squareNo = 0;
					for (int y = 0; y < 3; y++) {
						for (int x = 0; x < 3; x++) {
							currentPosition = 1 + x + (y + 1) * offset[DOWN];
							if (currentPosition != centerPosition) {
								if ((i & (1 << squareNo)) > 0) {
									setBox(currentPosition);
								}
								squareNo++;
							}
						}
					}
					Debug.debugApplication.redraw(true);
				}
			}

			// Restore board position by removing all boxes.
			for(int position=0; position<size; position++) {
				removeBox(position);
			}
			setBoardPosition(backup);	// set old boxes and player positions
			Debug.debugApplication.redraw(false);
		}

		/**
		 * Calculates the box distances from all squares to the target square, with respect
		 * to all possible player positions.  We do that backwards, i.e. from any square
		 * we pull a box to all reachable squares to determine the distances.
		 *
		 * @param targetPosition the position the distances have to be calculated for (any square to this target position)
		 */
		public void calculateBoxDistancesForwards(int targetPosition) {

			int oppositeDirectionOfPull;
			short next_in = 0;
			short next_out = 0;
			short playerPosition;
			short distance;
			short boxPosition = 0;

			// Alle Außenfelder und Mauern überspringen.
			// Achtung: Auf die Positionen, auf denen eine geblockte Kiste auf einem Zielfeld
			// steht wurde auch eine Mauer gesetzt. Diese Positionen müssen zu sich selbst
			// eine Entfernung von 0 besitzen, damit sie bei der Lowerboundberechnung nicht
			// als Deadlock gelten!
			if (isOuterSquareOrWall(targetPosition)) {
				if (isBoxOnGoal(targetPosition)) {
					for (byte direction = 0; direction < DIRS_COUNT; direction++) {
						boxDistancesForwards[direction][targetPosition][targetPosition] = 0;
					}
				}
				return;
			}

			next_in = 0;
			next_out = 0;

			// Versuchen, eine Kiste vom relevanten Feld in alle möglichen Richtungen zu ziehen
			// und damit Startdaten für die eigentliche Schleife (die "while-Schleife") erzeugen.
			for (byte direction = 0; direction < DIRS_COUNT; direction++) {

				boxPosition    = (short) (targetPosition +     offset[direction]);
				playerPosition = (short) (targetPosition + 2 * offset[direction]);

				// Die Entfernung des Feldes zu sich selbst ist 0.
				boxDistancesForwards[direction][targetPosition][targetPosition] = 0;

				// Falls auf das neue Feld gezogen werden kann, kommt es in den Stack
				if (isWall(boxPosition) == false && isWall(playerPosition) == false) {
					boxPositionsQ   [next_in] = boxPosition;
					playerPositionsQ[next_in] = playerPosition;
					distancesQ      [next_in] = 1;
					next_in++;
				}
			}

			// Ausgehend von den Startpositionen wird nun ermittelt auf welche Felder
			// die Kiste noch gezogen werden kann und welche Entfernungen diese weiteren
			// Felder zu dem Feld "position" haben.
			while (next_out < next_in) {
				boxPosition    = boxPositionsQ   [next_out];
				playerPosition = playerPositionsQ[next_out];
				distance       = distancesQ      [next_out];
				next_out++;

				// Prüfen, ob der Spieler auf alle freien Seiten der Kiste gelangen kann (= Kein Corralerzwingerfeld)
				if (isCorralForcerSquare(boxPosition) == false) {

					// Wenn die neue ermittelte Distanz nicht geringer ist als die bereits vorher
					// ermittelte, dann kann gleich das nächste Feld verarbeitet werden.
					if (boxDistancesForwards[UP][boxPosition][targetPosition] <= distance) {
						continue;
					}

					// Distanzen vom aktuellen Feld zum "Hauptfeld" eintragen
					boxDistancesForwards[UP   ][boxPosition][targetPosition] = distance;
					boxDistancesForwards[DOWN ][boxPosition][targetPosition] = distance;
					boxDistancesForwards[LEFT ][boxPosition][targetPosition] = distance;
					boxDistancesForwards[RIGHT][boxPosition][targetPosition] = distance;

					// Prüfen, auf welche Nachbarfelder die Kiste vom aktuellen Feld
					// gezogen werden kann. Dieses Nachbarfeld kann dann mit
					// Distanz+1 Pulls erreicht werden.
					// Falls das aktuelle Feld und das vorige Feld beide keine Corralerzwingerfelder
					// sind, kann ein Zurückschieben verhindert werden, da in diesem Fall die alte
					// Stellung erneut erreicht würde nur mit einer höheren Distanz.
					// (Falls die Kiste auf dem Ursprungsfeld steht (positionKiste == position)
					// könnte ein Zurückschieben auch vermieden werden, da für die Ursprungsposition
					// der Spieler auf alle möglichen Nachbarpositionen gesetzt wurde und somit
					// schon jede Richtung untersucht wird. Diese zusätzliche Abrage kostet aber
					// mehr Zeit, als die Verhinderung des zusätzlichen Pulls in die Gegenrichtung
					// gewinnen würde ...)
					if (isCorralForcerSquare(2 * boxPosition - playerPosition)) {
						// Das alte Feld war ein Corralerzwinger. Es muss auf jeden Fall ein
						// Zurückschieben geprüft werden. Deshalb Gegenrichtung auf irgend einen
						// ungültigen Wert setzen.  FFS/hm: 1000 ist "unguenstig"
						oppositeDirectionOfPull = 1000;
					} else {
						// Sowohl das alte als auch das aktuelle Feld der Kiste sind NichtCorralerzwinger.
						// Der Spieler kann also bei beiden Feldern auf alle freien Seiten kommen.
						// Ein Zurückschieben ist deshalb unnötig.
						oppositeDirectionOfPull = boxPosition - playerPosition;
					}

					for (byte direction = 0; direction < DIRS_COUNT; direction++) {
						if (offset[direction] != oppositeDirectionOfPull
								&& isWall(boxPosition + offset[direction]) == false
								&& isWall(boxPosition + 2 * offset[direction]) == false) {
							boxPositionsQ   [next_in] = (short) (boxPosition +     offset[direction]);
							playerPositionsQ[next_in] = (short) (boxPosition + 2 * offset[direction]);
							distancesQ      [next_in] = (short) (distance + 1);
							next_in++;
						}
					}
				} else {
					// erreichbare Felder des Spielers ermitteln, wobei nur auf der
					// aktuellen Position eine Kiste im Weg steht.
					// (Die Kiste wird durch eine Mauer simuliert)
					setWall(boxPosition);
					playersReachableSquaresOnlyWalls.update(playerPosition);
					removeWall(boxPosition);

					// Bei Corralerzwingerfeldern reicht es nicht zu prüfen, ob die beiden für ein Ziehen
					// relevanten Felder frei sind (= keine Mauer), sondern es muss auch noch geprüft werden,
					// ob der Spieler tatsächlich auf die jeweilige Seite gelangen kann. Ein Ziehen macht
					// natürlich auch nur dann Sinn, wenn die Situation nicht bereits vorher mit einer
					// kleineren oder genau so hohen Distanz erzeugt wurde.
					for (byte direction = 0; direction < DIRS_COUNT; direction++) {
						if (playersReachableSquaresOnlyWalls.isSquareReachable(boxPosition + offset[direction]) == false
								|| boxDistancesForwards[direction][boxPosition][targetPosition] <= distance) {
							continue;
						}

						// Es wurde ein Weg mit einer geringeren Distanz gefunden.
						boxDistancesForwards[direction][boxPosition][targetPosition] = distance;

						// Prüfen, auf welche Nachbarfelder die Kiste vom aktuellen Feld gezogen werden kann.
						// Das direkte Nachbarfeld wurde ja bereits durch den obigen "if" auf frei (=erreichbar)
						// geprüft. Es muss nun geprüft werden, ob das zweite für ein Ziehen notwendige Feld
						// ebenfalls frei ist. (man könnte hier genau so gut auf board.isWall(...) == false
						// prüfen, das bewirkt das gleiche ...)
						if (playersReachableSquaresOnlyWalls.isSquareReachable(boxPosition + 2 * offset[direction])) {
							boxPositionsQ   [next_in] = (short) (boxPosition +     offset[direction]);
							playerPositionsQ[next_in] = (short) (boxPosition + 2 * offset[direction]);
							distancesQ      [next_in] = (short) (distance + 1);
							next_in++;
						}
					}
				}
			}
		}

		/**
		 * Calculates the box distances from all squares to all squares, with respect
		 * to all possible player positions for pulls (backward pushes).
		 * We do that forwards, i.e. from any square we push a box to all
		 * reachable squares to determine the distances.
		 */
		private void calculateBoxDistancesBackwards() {

			int oppositeDirectionOfPush;
			int next_in = 0;
			int next_out = 0;
			short playerPosition;
			short distance;
			short boxPosition = 0;

			// Array, das die Kistendistanzen aufnimmt. Dieses Array muss bei jedem Aufruf
			// neu angelegt werden, da es später in einem Vector gepuffert wird.
			boxDistancesBackwards = new short[DIRS_COUNT][size][size];

			// Alle Kistendistanzen mit "unendlich" initialisieren
			Utilities.fillArray(boxDistancesBackwards, UNREACHABLE);

			// CorralerzwingerFelder ermitteln
			identifyCorralForcerSquares();

			// Alle Rückwärtszielfelder durchgehen und die Entfernung von jedem anderen Feld zu diesem Feld ermitteln
			for (int goalNo=0; goalNo<goalsCount;goalNo++) {

				int position = goalPositionsBackwardsSearch[goalNo];

				// Alle Außenfelder und Mauern überspringen
				if (isOuterSquareOrWall(position)) {
					continue;
				}

				next_in  = 0;
				next_out = 0;

				// Versuchen, eine Kiste vom relevanten Feld in alle möglichen Richtungen zu verschieben
				// und damit Startdaten für die eigentliche Schleife (die "while-Schleife") erzeugen.
				for (byte dir = 0; dir < DIRS_COUNT; dir++) {

					// Die Entfernung des Feldes zu sich selbst ist 0.
					boxDistancesBackwards[dir][position][position] = 0;

					// Falls auf das neue Feld verschoben werden kann, kommt es in den Stack
					if (       !isWall(position + offset[dir])
							&& !isWall(position - offset[dir]) ) {
						boxPositionsQ   [next_in] = (short) (position + offset[dir]);
						playerPositionsQ[next_in] = (short) (position);
						distancesQ      [next_in] = 1;
						next_in++;
					}
				}

				// Ausgehend von den Startpositionen wird nun ermittelt auf welche Felder
				// die Kiste noch verschoben werden kann und welche Entfernungen diese weiteren
				// Felder zu dem Feld "position" haben.
				while (next_out < next_in) {
					boxPosition    = boxPositionsQ   [next_out];
					playerPosition = playerPositionsQ[next_out];
					distance       = distancesQ      [next_out];
					next_out++;

					// Prüfen, ob der Spieler auf alle freien Seiten der Kiste gelangen kann
					// (= Kein Corralerzwingerfeld)
					if (isCorralForcerSquare(boxPosition) == false) {

						// Wenn die neue ermittelte Distanz nicht geringer ist als die bereits vorher
						// ermittelte, dann kann gleich das nächste Feld verarbeitet werden.
						if (boxDistancesBackwards[UP][boxPosition][position] <= distance) {
							continue;
						}

						// Distanzen vom aktuellen Feld zum "Hauptfeld" eintragen
						boxDistancesBackwards[UP   ][boxPosition][position] = distance;
						boxDistancesBackwards[DOWN ][boxPosition][position] = distance;
						boxDistancesBackwards[LEFT ][boxPosition][position] = distance;
						boxDistancesBackwards[RIGHT][boxPosition][position] = distance;

						// Prüfen, auf welche Nachbarfelder die Kiste vom aktuellen Feld
						// gezogen werden kann. Dieses Nachbarfeld kann dann mit
						// Distanz+1 Pulls erreicht werden.
						// Falls das aktuelle Feld und das vorige Feld beide keine Corralerzwingerfelder
						// sind, kann ein Zurückschieben verhindert werden, da in diesem Fall die alte
						// Stellung erneut erreicht würde nur mit einer höheren Distanz.
						// (Falls die Kiste auf dem Ursprungsfeld steht (positionKiste == position)
						// könnte ein Zurückschieben auch vermieden werden, da für die Ursprungsposition
						// der Spieler auf alle möglichen Nachbarpositionen gesetzt wurde und somit
						// schon jede Richtung untersucht wird. Diese zusätzliche Abrage kostet aber
						// mehr Zeit, als die Verhinderung des zusätzlichen Pushes in die Gegenrichtung
						// gewinnen würde ...)
						if (isCorralForcerSquare(playerPosition)) {
							// Das alte Feld war ein Corralerzwinger. Es muss auf jeden Fall ein
							// Zurückschieben geprüft werden. Deshalb Gegenrichtung auf irgend einen
							// ungültigen Wert setzen.
							oppositeDirectionOfPush = 1000;
						} else {
							// Sowohl das alte als auch das aktuelle Feld der Kiste sind NichtCorralerzwinger.
							// Der Spieler kann also bei beiden Feldern auf alle freien Seiten kommen.
							// Ein Zurückschieben ist deshalb unnötig.
							oppositeDirectionOfPush = playerPosition - boxPosition;
						}

						for (byte direction = 0; direction < DIRS_COUNT; direction++) {
							if (offset[direction] != oppositeDirectionOfPush
									&& isWall(boxPosition + offset[direction]) == false
									&& isWall(boxPosition - offset[direction]) == false) {
								boxPositionsQ   [next_in] = (short) (boxPosition + offset[direction]);
								playerPositionsQ[next_in] = boxPosition;
								distancesQ      [next_in] = (short) (distance + 1);
								next_in++;
							}
						}
					} else {
						// erreichbare Felder des Spielers ermitteln, wobei nur auf der
						// aktuellen Position eine Kiste im Weg steht.
						// (Die Kiste wird durch eine Mauer simuliert)
						setWall(boxPosition);
						playersReachableSquaresOnlyWalls.update(playerPosition);
						removeWall(boxPosition);

						// Bei Corralerzwingerfeldern reicht es nicht zu prüfen, ob die beiden für ein Verschieben
						// relevanten Felder frei sind (= keine Mauer), sondern es muss auch noch geprüft werden,
						// ob der Spieler tatsächlich auf die jeweilige Seite gelangen kann. Ein Verschieben macht
						// natürlich auch nur dann Sinn, wenn die Situation nicht bereits vorher mit einer
						// kleineren oder genau so hohen Distanz erzeugt wurde.
						for (byte direction = 0; direction < DIRS_COUNT; direction++) {
							if (playersReachableSquaresOnlyWalls.isSquareReachable(boxPosition + offset[direction]) == false
									|| boxDistancesBackwards[direction][boxPosition][position] <= distance) {
								continue;
							}

							// Es wurde ein Weg mit einer geringeren Distanz gefunden.
							boxDistancesBackwards[direction][boxPosition][position] = distance;

							// Prüfen, auf welche Nachbarfelder die Kiste vom aktuellen Feld gezogen werden kann.
							// Das direkte Nachbarfeld wurde ja bereits durch den obigen "if" auf frei (=erreichbar)
							// geprüft. Es muss nun geprüft werden, ob das zweite für ein Ziehen notwendige Feld
							// ebenfalls frei ist. (man könnte hier genau so gut auf board.isWall(...) == false
							// prüfen, das bewirkt das gleiche ...)
							if (isWall(boxPosition - offset[direction]) == false) {

								// - offset, damit oben boxDistancesBackwards[direction] stehen kann.
								// würde man + offset[direction] benutzen, müsste man die
								// Gegenrichtung in das Distanzenarray benutzen ...
								boxPositionsQ   [next_in] = (short) (boxPosition - offset[direction]);
								playerPositionsQ[next_in] = boxPosition;
								distancesQ      [next_in] = (short) (distance + 1);
								next_in++;
							}
						}
					}
				}
			}
		}

		/**
		 * Calculates the distance of the player to any square: ({@link Board#playerDistances}
		 * Frozen boxes are recognized as obstacles, provided the caller has converted
		 * the frozen boxes into walls, temporarily. All other boxes are ignored.
		 *
		 * @see #updateBoxDistances(SearchDirection, boolean)
		 */
		public void calculatePlayerDistances() {

			// Here we use (short) to be memory efficient.  Also, such arrays may
			// eventually be saved in a buffer like "distancesBufferForwards".

			// Hold distance and player position from the respective stacks
			short distance;
			short playerPosition;

			// stack indices
			int next_in;
			int next_out;

			// a possible new player position
			short newPosition;

			// This is the array storing the distances between any two squares [fromSquare][toSquare]
			// We need a new instance, each time, since we later buffer it in a vector.
			playerDistances = new short[size][size];

			// Initialize the complete array(s) with "infinite" (a large value)
			Utilities.fillArray(playerDistances, UNREACHABLE);

			// Calculate distances for all squares
			for (int position = firstRelevantSquare; position < lastRelevantSquare; position++) {

				// ignore outer squares and walls ...
				if (isOuterSquareOrWall(position)) {
					continue;
				}

				// Now we calculate distances for the square "position".  For that we
				// fill their distances into the reachable squares, as we detect them.
				playerPositionsQ[0] = (short) position;
				distancesQ      [0] = 0;
				next_in  = 1;
				next_out = 0;
				while (next_out < next_in) {
					// dequeue next item: square and distance
					playerPosition = playerPositionsQ[next_out];
					distance       = distancesQ      [next_out];
					next_out++;

					// In case this square had already been reached with smaller distance
					// (than what we are now going to grant), then we immediately
					// continue with the next square from the queue.
					// Take care: We must not move this check into the loop below,
					// since the stack (at that time) may still contain other squares
					// which may reach our square with a smaller distance.
					if (playerDistances[playerPosition][position] <= distance) {
						continue;
					}
					playerDistances[playerPosition][position] = distance;

					// Now we enter all squares, which are directly reachable from the
					// current one, with their distance (one larger than the current one)
					for (int direction = 0; direction < DIRS_COUNT; direction++) {

						newPosition = (short) (playerPosition + offset[direction]);

						if (isWall(newPosition)) {
							continue;
						}

						// The new square is reachable with (distance+1) from the start square
						playerPositionsQ[next_in] = newPosition;
						distancesQ      [next_in] = (short) (distance + 1);
						next_in++;
					}
				}
			}
		}

		/**
		 * Fill the current freeze situation into the specified object
		 * (set a bit for each frozen box).
		 * Used for forward and backward search.
		 *
		 * @param freezeSet object to be filled
		 */
		private void fillCurrentFreeze(BitSet freezeSet) {
			freezeSet.clear();
			for (int boxNo = 0; boxNo < boxCount; boxNo++) {
				if (boxData.isBoxFrozen(boxNo)) {
					freezeSet.set(goalsNumbers[boxData.getBoxPosition(boxNo)]); // boxes can only freeze on goals (otherwise-> deadlock) but the same box number can be frozen on different positions
				}
			}
		}

		/**
		 * Recalculates box distances, taking into account frozen boxes
		 * (those which cannot be moved anymore, except by "undo").
		 * In this method we mainly handle the caching of the results of
		 * former calculations.  The calculations can become expensive,
		 * and the key for the calculation is more often already done,
		 * than not.  Hence a cache is important for efficiency.
		 *
		 * @param searchDirection the direction of the search (push or pull)
		 * @param onlyDistancesToGoals <code>true</code> calculates ony the distances to the goal positions,
		 * 		  <code>false</code> calculates the distances to all positions
		 *
		 * @see #calculateBoxDistancesForwards(int)
		 */
		public void updateBoxDistances(SearchDirection searchDirection, boolean onlyDistancesToGoals) {

			// Take care: Even if the current freeze state is identical to
			// the last freeze state from the same search direction,
			// we still cannot just "return" (do nothing). The box distances
			// would be correct, but the player distances might still
			// be from/for a former search in the opposite direction!
			// Hence, even for an optimal match we have to access the data
			// we find for our key.

			if (searchDirection == SearchDirection.FORWARD) {

				// Copy current freeze state into a bit vector
				fillCurrentFreeze(currentFreezeSituationForwards);

				// Check whether the current situation is known, already
				DistCacheElement distData = distancesForwCache.getV(currentFreezeSituationForwards);
				if (distData != null) {
					// Fetch the references from the found value
					boxDistancesForwards = distData.boxDistances;
					corralForcer         = distData.corralForcer;
					playerDistances      = distData.playerDistances;

					return;
				}

				/*
				 * Did not find the current freeze state in the buffer.
				 * Hence we have to calculate it.
				 */
				// Temporarily place a wall for all frozen boxes.
				for (int boxNo = 0; boxNo < boxCount; boxNo++) {
					if (boxData.isBoxFrozen(boxNo)) {
						setWall(boxData.getBoxPosition(boxNo));
					}
				}

				/**
				 * Update the distances.
				 */
				// Array, das die Kistendistanzen aufnimmt. Dieses Array muss bei jedem Aufruf
				// neu angelegt werden, da es später in einem Vector gepuffert wird.
				boxDistancesForwards = new short[DIRS_COUNT][size][size];

				// Alle Kistendistanzen mit "unendlich" initialisieren
				Utilities.fillArray(boxDistancesForwards, UNREACHABLE);

				// Spielerdistanzen und Kistendistanzen neu berechnen (Corralerzwinger werden
				// automatisch bei der Berechnung der neuen Kistendistanzen berechnet)
				calculatePlayerDistances();
				identifyCorralForcerSquares();      // for better performance the corral forcer squares are identified

				// Calculate the distance from every position to every other position.
				for (int position = firstRelevantSquare; position < lastRelevantSquare; position++) {
					if(isGoal(position) || onlyDistancesToGoals == false) {
						calculateBoxDistancesForwards(position);
					}
				}

				// Remove the temporary walls
				for (int boxNo = 0; boxNo < boxCount; boxNo++) {
					if (boxData.isBoxFrozen(boxNo)) {
						removeWall(boxData.getBoxPosition(boxNo));
					}
				}

				// NB: We must clone the key we enter to the cache, since that
				// object is reused for multiple computations, while the
				// associated data is always freshly allocated,
				// and directly entered to the cache.
				distancesForwCache.add(
						(BitSet) currentFreezeSituationForwards.clone(),
						new DistCacheElement( boxDistancesForwards,
								              corralForcer,
								              playerDistances      )    );

				return;
			}

			/*
			 * Search direction is "backwards"
			 */

			// Currently, no freeze conditions are recognized for backwards search.
			// Hence, in backwards search we never really have frozen boxes.
			// But the coding for them is already there.
			// TODO: recognize frozen boxes for backwards search
			// IMPORTANT: this means that frotzen boxes from the forward search
			// may still be on the board! Hence, we have to set all boxes "not frozen"
			// for the backward search.
			boxData.setAllBoxesNotFrozen();

			// Fill a BitSet with the current freeze situation...
			fillCurrentFreeze(currentFreezeSituationBackwards);

			// The solver can be started after some pushes have been made. In this situation
			// the new box positions are used as backward goals. Hence, for the caching the
			// backward goal positions are also relevant!
			if(!Arrays.equals(goalPositionsBackwardsSearch, goalsPositionsBackwardsSearchUsedForCaching)) {
				goalsPositionsBackwardsSearchUsedForCaching = Arrays.copyOf(goalPositionsBackwardsSearch, goalPositionsBackwardsSearch.length);
				distancesBackCache.clear();
			}

			// Check whether the current situation is already known.
			DistCacheElement distData = distancesBackCache.getV(currentFreezeSituationBackwards);
			if (distData != null) {
				// Fetch the references from the found value
				boxDistancesBackwards = distData.boxDistances;
				corralForcer          = distData.corralForcer;
				playerDistances       = distData.playerDistances;

				return;
			}

			/*
			 * Die aktuelle Blockersituation war nicht im Puffer vorhanden. Sie muss deshalb neu
			 * berechnet werden.
			 */
			// Auf alle Positionen, auf denen eine geblockte Kiste steht eine Mauer setzen.
			for (int boxNo = 0; boxNo < boxCount; boxNo++) {
				if (boxData.isBoxFrozen(boxNo)) {
					setWall(boxData.getBoxPosition(boxNo));
				}
			}

			// Spielerdistanzen und Kistendistanzen neu berechnen (Corralerzwinger werden
			// automatisch bei der Berechnung der neuen Kistendistanzen berechnet)
			calculatePlayerDistances();
			calculateBoxDistancesBackwards();

			// Die zusätzlichen Mauern wieder entfernen
			for (int boxNo = 0; boxNo < boxCount; boxNo++) {
				if (boxData.isBoxFrozen(boxNo)) {
					removeWall(boxData.getBoxPosition(boxNo));
				}
			}

			// NB: We must clone the key we enter to the cache, since that
			// object is reused for multiple computations, while the
			// associated data is always freshly allocated,
			// and directly entered to the cache.
			distancesBackCache.add(
					(BitSet) currentFreezeSituationBackwards.clone(),
					new DistCacheElement( boxDistancesBackwards,
							              corralForcer,
							              playerDistances       )    );
		}
	}

	/**
	 * This class holds an array, in which the player reachable squares are marked.
	 * For each call of the <code>update</code> method the currently reachable
	 * squares are calculated and marked.
	 */
	public class PlayersReachableSquares {

		// Hierdrin wird der Wert gespeichert, den ein durch den Spieler erreichbares
		// Feld kennzeichnt. Dieser Wert wird bei jedem Durchlauf hochgezählt, so dass
		// immer das gleiche Array zur Ermittlung der erreichbaren Felder genutzt werden
		// kann, ohne dieses Array vor jedem Durchlauf initialisieren zu müssen.
		int indicatorReachableSquare;

		// Array, in dem die bereits erreichten Felder gekennzeichnet werden.
		int[] playersReachableSquaresArray;

		// Stack, in dem die Positionen der noch zu analysierenden Felder gespeichert werden
		// analysieren = ist Feld erreichbar durch den Spieler oder nicht
		IntStack positionsToBeAnalyzed = new IntStack(size);

		/**
		 * Constructor, just allocating array of size {@link #size}.
		 */
		public PlayersReachableSquares() {
			playersReachableSquaresArray = new int[size];
		}

		/**
		 * Constructor for cloning.
		 * Returns a PlayersReachableSquare object containing the passed values.
		 *
		 * @param reachableSquares	the reachable squares array to be set
		 * @param indicatorValue	the indicator for reachable squares to be set
		 */
		public PlayersReachableSquares(int[] reachableSquares, int indicatorValue) {
			playersReachableSquaresArray = reachableSquares.clone();
			indicatorReachableSquare = indicatorValue;
		}

		/**
		 * Returns whether the player can reach the passed position.
		 *
		 * @param position the position to be tested for reachability
		 *
		 * @return <code>true</code> the position is reachable by the player
		 * 			<code>false</code> the position isn't reachable by the player
		 */
		 public boolean isSquareReachable(int position) {
			return playersReachableSquaresArray[position] == indicatorReachableSquare;
		}

		/**
		 * Returns a clone of the current object.
		 *
		 * @return a PlayersReachableSquares object identical to this object
		 */
		 public PlayersReachableSquares getClone() {
			return new PlayersReachableSquares(playersReachableSquaresArray, indicatorReachableSquare);
		}


		/**
		 * Updates the reachable squares of the player assuming the player at the passed position.
		 *
		 * @param xPlayerPosition the x coordinate of the player position
		 * @param yPlayerPosition the y coordinate of the player position
		 */
		public void update(int xPlayerPosition, int yPlayerPosition) {
			update(xPlayerPosition + width * yPlayerPosition);
		}

		/**
		 * Updates the reachable squares of the player.
		 * <p>
		 * The squares then can be tested for reachability calling the method <code>isSquareReachable()</code>
		 */
		public void update() {
			update(playerPosition);
		}

		/**
		 * Updates the reachable squares of the player assuming the player at the passed position.
		 *
		 * @param playerPosition the position of the player
		 */
		public void update(int playerPosition) {

			// Bei jedem Aufruf einen neuen Wert als Indikator für ein erreichbares Feld
			// verwenden, so dass das Array nicht immer vorgelöscht werden muss!
			indicatorReachableSquare++;

			positionsToBeAnalyzed.add(playerPosition);
			playersReachableSquaresArray[playerPosition] = indicatorReachableSquare;

			while (!positionsToBeAnalyzed.isEmpty()) {
				playerPosition = positionsToBeAnalyzed.remove();

				for( int direction = 0; direction<DIRS_COUNT ; ++direction) {
					int newPosition = playerPosition + offset[direction];
					if (isAccessible(newPosition) && playersReachableSquaresArray[newPosition] != indicatorReachableSquare) {
						positionsToBeAnalyzed.add(newPosition);
						playersReachableSquaresArray[newPosition] = indicatorReachableSquare;
					}
				}
			}
		}

		/**
		 * Die derzeit im Array als erreichbar gekennzeichneten Felder behalten ihre Kennzeichnung.
		 * Der Spieler wird auf die übergebenen Koordinaten gesetzt und dann werden zusätzlich
		 * zu den bereits gekennzeichneten Felder alle Felder gekennzeichnet, die nun ebenfalls
		 * erreichbar sind. Diese Methode geht davon aus, dass eine ERWEITERUNG der erreichbaren
		 * Felder vorliegt (also Kisten vom Feld genommen). D.h. sobald ein bereits als erreichbar
		 * gekennzeichnetes Feld erreicht wird, wird an dieser Stelle nicht weitergesucht!
		 *
		 * @param xPlayerPosition the x coordinate of the player position
		 * @param yPlayerPosition the y coordinate of the player position
		 */
		public void enlarge(int xPlayerPosition, int yPlayerPosition) {
			// Der Spieler startet quasi noch einmal von einer anderen Position aus. Der
			// Wert, der ein erreichtes Feld kennzeichnet muss also gleich bleiben.
			// Da er in der Methode automatisch erhöht wird, wird er hier um eins erniedrigt.
			indicatorReachableSquare--;
			update(xPlayerPosition, yPlayerPosition);
		}

		/**
		 * Die derzeit im Array als erreichbar gekennzeichneten Felder behalten ihre Kennzeichnung.
		 * Der Spieler wird auf die übergebenen Koordinaten gesetzt und dann werden zusätzlich
		 * zu den bereits gekennzeichneten Felder alle Felder gekennzeichnet, die nun ebenfalls
		 * erreichbar sind. Diese Methode geht davon aus, dass eine ERWEITERUNG der erreichbaren
		 * Felder vorliegt (also Kisten vom Feld genommen). D.h. sobald ein bereits als erreichbar
		 * gekennzeichnetes Feld erreicht wird, wird an dieser Stelle nicht weitergesucht!
		 *
		 * @param playerPosition the player position
		 */
		public void enlarge(int playerPosition) {
			// Der Spieler startet quasi noch einmal von einer anderen Position aus. Der
			// Wert, der ein erreichtes Feld kennzeichnet muss also gleich bleiben.
			// Da er in der Methode automatisch erhöht wird, wird er hier um eins erniedrigt.
			indicatorReachableSquare--;
			update(playerPosition);
		}

		/**
		 * Ermittelt die vom Spieler erreichbaren Felder und kennzeichnet sie im übergebenen Array.
		 * Normalerweise wird das Array in dieser Klasse genutzt (playersReachableSquares),
		 * aber da dieses globale Array bei jedem Aufruf überschrieben wird, ist es nur für
		 * eine gewisse Zeit aktuell. Sollen die erreichbaren Spielerfelder einer bestimmten
		 * Stellung für einen langen Zeitraum abfragbar sein, so kann diese Methode benutzt
		 * werden, die die erreichbaren Felder in dem übergebenen Array kennzeichnet.
		 * Als Indikatorwert wird einfach 1 angenommen. Somit darf diese Methode nicht
		 * noch einmal für das gleiche Array mit dieser Methodensignatur aufgerufen werden,
		 * da ja schon Felder mit 1 gekennzeichnet sein würden, was zu Fehlern führen würde!
		 *
		 * @param reachableSquares			Array, in dem alle erreichbaren Felder gekennzeichnet werden
		 */
		public void update(byte[] reachableSquares) {
			update(reachableSquares, (byte) 1, playerPosition);
		}

		/**
		 * Ermittelt die vom Spieler erreichbaren Felder und kennzeichnet sie im übergebenen Array.
		 * Normalerweise wird das Array in dieser Klasse genutzt (playersReachableSquaresArray),
		 * aber da dieses globale Array bei jedem Aufruf überschrieben wird, ist es nur für
		 * eine gewisse Zeit aktuell. Sollen die erreichbaren Spielerfelder einer bestimmten
		 * Stellung für einen langen Zeitraum abfragbar sein, so kann diese Methode benutzt
		 * werden, die die erreichbaren Felder in dem übergebenen Array kennzeichnet.
		 *
		 * @param reachableSquares			Array, in dem alle erreichbaren Felder gekennzeichnet werden
		 * @param reachableIndicatorValue	Wert, mit dem erreichbare Felder gekennzeichnet werden
		 */
		public void update(byte[] reachableSquares, byte reachableIndicatorValue) {
			update(reachableSquares, reachableIndicatorValue, playerPosition);
		}

		/**
		 * Ermittelt die vom Spieler erreichbaren Felder und kennzeichnet sie im übergebenen Array.
		 * Normalerweise wird das Array in dieser Klasse genutzt (playersReachableSquares),
		 * aber da dieses globale Array bei jedem Aufruf überschrieben wird, ist es nur für
		 * eine gewisse Zeit aktuell. Sollen die erreichbaren Spielerfelder einer bestimmten
		 * Stellung für einen langen Zeitraum abfragbar sein, so kann diese Methode benutzt
		 * werden, die die erreichbaren Felder in dem übergebenen Array kennzeichnet.
		 *
		 * @param reachableSquares			Array, in dem alle erreichbaren Felder gekennzeichnet werden
		 * @param reachableIndicatorValue	Wert, mit dem erreichbare Felder gekennzeichnet werden
		 * @param playerPosition		 	Spielerposition
		 */
		public void update(byte[] reachableSquares, byte reachableIndicatorValue, int playerPosition) {

			positionsToBeAnalyzed.add(playerPosition);
			reachableSquares[playerPosition] = reachableIndicatorValue;

			while (!positionsToBeAnalyzed.isEmpty()) {
				playerPosition = positionsToBeAnalyzed.remove();

				// Alle von der aktuellen Position erreichbaren Felder in den Stack aufnehmen,
				// falls sie vorher nicht bereits erreicht wurden.
				for( int direction = 0; direction<DIRS_COUNT ; ++direction) {
					int newPosition = playerPosition + offset[direction];
					if (isAccessible(newPosition) && reachableSquares[newPosition] != reachableIndicatorValue) {
						positionsToBeAnalyzed.add(newPosition);
						reachableSquares[newPosition] = reachableIndicatorValue;
					}
				}
			}
		}

		/**
		 * Die derzeit im Array als erreichbar gekennzeichneten Felder werden um die Felder
		 * reduziert, die der Spieler jetzt erreichen kann.
		 *
		 * @param reachableSquares			Array, in dem alle erreichbaren Felder gekennzeichnet sind
		 * @param playerPosition		 	Spielerposition
		 */
		public void reduce(byte[] reachableSquares, int playerPosition) {

			positionsToBeAnalyzed.add(playerPosition);
			reachableSquares[playerPosition] = -1;

			while (!positionsToBeAnalyzed.isEmpty()) {
				playerPosition = positionsToBeAnalyzed.remove();

				// Durch Setzen von -1 wird auf jeden Fall ein neuer Wert der unterschiedlich
				// von reachableIndicatorValue ist gesetzt, wodurch dieses Feld automatisch
				// als nicht mehr erreichbar gesetzt gilt, wenn mit reachableIndicatorValue
				// geprüft wird !!!)

				// Alle von der aktuellen Position erreichbaren Felder in den Stack aufnehmen,
				// falls sie vorher nicht bereits erreicht wurden.
				for (int dir = 0; dir < DIRS_COUNT; dir++) {
					int newPosition = playerPosition + offset[dir];
					if (isAccessible(newPosition) && reachableSquares[newPosition] != -1) {
						positionsToBeAnalyzed.add(newPosition);
						reachableSquares[newPosition] = -1;
					}
				}
			}
		}

		/**
		 * Die derzeit im globalen Array als erreichbar gekennzeichneten Felder werden um die Felder
		 * reduziert, die der Spieler jetzt erreichen kann.
		 *
		 * @param playerPosition		 	Spielerposition
		 */
		public void reduce(int playerPosition) {

			positionsToBeAnalyzed.add(playerPosition);
			playersReachableSquaresArray[playerPosition] = -1;

			while (!positionsToBeAnalyzed.isEmpty()) {
				playerPosition = positionsToBeAnalyzed.remove();

				// Durch Setzen von -1 wird auf jeden Fall ein neuer Wert der unterschiedlich
				// von reachableIndicatorValue ist gesetzt, wodurch dieses Feld automatisch
				// als nicht mehr erreichbar gesetzt gilt, wenn mit reachableIndicatorValue
				// geprüft wird !!!)

				// Alle von der aktuellen Position erreichbaren Felder in den Stack aufnehmen,
				// falls sie vorher nicht bereits erreicht wurden.
				for (int dir = 0; dir < DIRS_COUNT; dir++) {
					int newPosition = playerPosition + offset[dir];
					if (isAccessible(newPosition) && playersReachableSquaresArray[newPosition] != -1) {
						positionsToBeAnalyzed.add(newPosition);
						playersReachableSquaresArray[newPosition] = -1;
					}
				}
			}
		}

		/**
		 * Returns the position reachable of the player that is
		 * the most top left one.
		 * This is a normalization of the player position, used, where the
		 * exact player position is not relevant, but its reachable area is.
		 *
		 * @return the position top left
		 */
		public int getPlayerPositionTopLeft() {

			// Calculate squares reachable by the player.
			update();

			// This is just the square with the smallest index.
			for (int position = firstRelevantSquare; position < lastRelevantSquare; position++) {
				if (isSquareReachable(position)) {
					return position;
				}
			}

			// The player can't reach any square.
			return -1;
		}
	}

	/**
	 * Diese Klasse hält ein Array, in dem die durch den Spieler erreichbaren Felder gekennzeichnet
	 * werden. Bei jedem Aufruf der "aktualisiere"-Methode werden die aktuell erreichbaren Felder
	 * des Spielers gekennzeichnet.
	 * Besonderheit: Der Spieler kann bei der Ermittlung der erreichbaren Felder durch
	 * Kisten hindurchgehen!
	 */
	public class PlayersReachableSquaresOnlyWalls {

		// Hierdrin wird der Wert gespeichert, den ein durch den Spieler erreichbares
		// Feld kennzeichnt. Dieser Wert wird bei jedem Durchlauf hochgezählt, so dass
		// immer das gleiche Array zur Ermittlung der erreichbaren Felder genutzt werden
		// kann, ohne dieses Array vor jedem Durchlauf initialisieren zu müssen.
		int reachableSquareIndicatorOnlyWalls = 1;

		// Array, in dem die bereits erreichten Felder gekennzeichnet werden.
		int[] playersReachableSquaresOnlyWallsArray;

		// Stack, in dem die Positionen der noch zu analysierenden Felder gespeichert werden
		// analysieren = ist Feld erreichbar durch den Spieler oder nicht
		final IntStack positionsToBeAnalyzed;

		/**
		 * Constructor, just allocating arrays of size {@link Board#size}.
		 */
		public PlayersReachableSquaresOnlyWalls() {
			playersReachableSquaresOnlyWallsArray = new int[size];
			positionsToBeAnalyzed = new IntStack(size);
		}

		/**
		 * Constructor for cloning.
		 *
		 * @param reachableSquares	the array of reachable squares to be set
		 * @param reachableIndicatorValue	the value indicating a square to be reachable
		 */
		public PlayersReachableSquaresOnlyWalls(int[] reachableSquares,
				int reachableIndicatorValue) {
			playersReachableSquaresOnlyWallsArray = reachableSquares.clone();
			reachableSquareIndicatorOnlyWalls = reachableIndicatorValue;
			positionsToBeAnalyzed = new IntStack(size); // needn't be cloned
		}

		/**
		 * Returns a (deep) clone of this object.
		 *
		 * @return a (deep) clone of this object
		 */
		public PlayersReachableSquaresOnlyWalls getClone() {
			return new PlayersReachableSquaresOnlyWalls(playersReachableSquaresOnlyWallsArray, reachableSquareIndicatorOnlyWalls);
		}

		/**
		 * Returns whether a specific Square is reachable by the player.
		 *
		 * @param position Position which is checked for being reachable by the player
		 * @return true = Square is reachable; false = Square is not reachable
		 */
		public boolean isSquareReachable(int position) {
			return playersReachableSquaresOnlyWallsArray[position] == reachableSquareIndicatorOnlyWalls;
		}

		/**
		 * Returns if a specific Square is reachable by the player.
		 *
		 * @param x xPosition of square which is checked for being reachable by the player
		 * @param y yPosition of square which is checked for being reachable by the player
		 * @return true = Square is reachable; false = Square is not reachable
		 */
		public boolean isSquareReachable(int x, int y) {
			return playersReachableSquaresOnlyWallsArray[x + width * y] == reachableSquareIndicatorOnlyWalls;
		}

		/**
		 * Identifies the reachable squares of the player regarding only walls as obstacles.
		 */
		public void update() {
			update(playerPosition);
		}

		/**
		 * Identifies the reachable squares of the player regarding only walls as obstacles.
		 * The player is set to the passed position to itentify the reachable squares.
		 *
		 * @param xPlayerPosition the x coordinate of the player position
		 * @param yPlayerPosition the y coordinate of the player position
		 */
		public void update(int xPlayerPosition, int yPlayerPosition) {
			update(xPlayerPosition + width * yPlayerPosition);
		}

		/**
		 * Identifies the reachable squares of the player regarding only walls
		 * as obstacles. The player is considered to be at the passed position
		 * to identify the reachable squares.
		 *
		 * @param playerPosition the position of the player
		 */
		public void update(int playerPosition) {

			// Bei jedem Aufruf einen neuen Wert als Indikator für ein erreichbares Feld
			// verwenden, so dass das Array nicht immer vorgelöscht werden muss!
			reachableSquareIndicatorOnlyWalls++;

			positionsToBeAnalyzed.add(playerPosition);
			playersReachableSquaresOnlyWallsArray[playerPosition] = reachableSquareIndicatorOnlyWalls;

			while (!positionsToBeAnalyzed.isEmpty()) {
				playerPosition = positionsToBeAnalyzed.remove();

				// Alle von der aktuellen Position erreichbaren Felder in den Stack aufnehmen,
				// falls sie vorher nicht bereits erreicht wurden.
				for (int directionOffset : offset) {
					int newPosition = playerPosition + directionOffset;
					if (isWall(newPosition) == false && playersReachableSquaresOnlyWallsArray[newPosition] != reachableSquareIndicatorOnlyWalls) {
						positionsToBeAnalyzed.add(newPosition);
						playersReachableSquaresOnlyWallsArray[newPosition] = reachableSquareIndicatorOnlyWalls;
					}
				}
			}
		}

		/**
		 * Die derzeit im Array als erreichbar gekennzeichneten Felder behalten ihre Kennzeichnung.
		 * Der Spieler wird auf die übergebenen Koordinaten gesetzt und dann werden zusätzlich
		 * zu den bereits gekennzeichneten Felder alle Felder gekennzeichnet, die nun ebenfalls
		 * erreichbar sind. Sobald ein bereits als erreichbar gekennzeichnetes Feld erreicht wird,
		 * wird an dieser Stelle nicht weitergesucht. Der Spieler muss für diese Methode also
		 * in verschiedene, durch Mauern vollständig eingegrenzte Bereiche gesetzt werden und dann
		 * jeweils diese Methode aufgerufen werden.
		 *
		 * @param playerPosition the player position
		 */
		public void enlarge(int playerPosition) {
			// Der Spieler startet quasi noch einmal von einer anderen Position aus. Der
			// Wert, der ein erreichtes Feld kennzeichnet muss also gleich bleiben.
			// Da er in der Methode automatisch erhöht wird, wird er hier um eins erniedrigt.
			reachableSquareIndicatorOnlyWalls--;
			update(playerPosition);
		}
	}

	/**
	 * Diese Klasse hält ein Array, in dem die durch den Spieler erreichbaren Felder gekennzeichnet
	 * werden. Bei jedem Aufruf der "aktualisiere"-Methode werden die aktuell erreichbaren Felder
	 * des Spielers gekennzeichnet. Im Gegensatz zur Klasse "ErreichbareFelderSpieler" werden nicht
	 * nur die erreichbaren Felder ermittelt, sondern auch die Distanz des Spielers zu jedem Feld
	 * ermittelt.
	 */
	public class PlayersReachableSquaresMoves {

		// Array, in dem die bereits erreichten Felder gekennzeichnet werden.
		short[] playersReachableSquaresMoves;

		// Mit diesem Array wird das obige Array bei jedem Aufruf wieder initialisiert.
		// (Falls diese Klasse öfter instanziiert wird sollte dieses Array in die Spielfeldklasse
		// verschoben und static gesetzt werden)
		short[] initializationArray;

		// Stack, in dem die Positionen der noch zu analysierenden Felder gespeichert werden
		// analysieren = ist Feld erreichbar durch den Spieler oder nicht
		int[] positionsToBeAnalyzed = new int[size];

		/**
		 * Konstruktor
		 */
		public PlayersReachableSquaresMoves() {
			playersReachableSquaresMoves = new short[size];
			initializationArray = new short[size];
			Arrays.fill(initializationArray, UNREACHABLE);
		}

		/**
		 * Gibt zurück, ob ein Feld durch den Spieler erreichbar ist.
		 *
		 * @param position  Position, die auf Erreichbarkeit geprüft wird
		 * @return			true = Feld ist erreichbar; false = Feld ist nicht erreichbar
		 */
		public boolean isSquareReachable(int position) {
			return playersReachableSquaresMoves[position] != UNREACHABLE;
		}

		/**
		 * Gibt die Distanz zur übergebenen Position zurück.
		 *
		 * @param position	Position, zu der die Distanz des Spielers zurückgegeben wird.
		 * @return			Distanz des Spielers zur übergebenen Position
		 */
		public short getDistance(int position) {
			return playersReachableSquaresMoves[position];
		}

		/**
		 * Ermittelt die vom Spieler erreichbaren Felder und kennzeichnet sie im entsprechenden Array.
		 */
		public void update() {
			update(playerPosition);
		}

		/**
		 * Ermittelt die Distanz des Spielers zu jedem erreichbaren Feld des Spielers.
		 *
		 * @param playerPosition Position, auf der der Spieler zu Beginn steht.
		 */
		public void update(int playerPosition) {

			// Index auf den höchsten Index der Queue und Index auf den als nächstes zu verarbeitenden
			// Eintrag.
			int highestIndex = 0;
			int currentIndex = 0;
			int newPosition  = 0;

			// Distanz eines Feldes
			short newDistance = 0;

			// Array mit einer "unendlichen" Distanz vorbelegen.
			System.arraycopy(initializationArray, 0, playersReachableSquaresMoves, 0, size);

			// Die aktuelle Spielerposition als Ausgangsfeld nehmen. Es kann mit 0 Moves erreicht werden.
			positionsToBeAnalyzed[0] = playerPosition;
			playersReachableSquaresMoves[playerPosition] = 0;

			while (currentIndex <= highestIndex) {
				playerPosition = positionsToBeAnalyzed[currentIndex++];

				// Distanz der jetzigen Spielerposition zur ursprünglichen Position + 1 =
				// Distanz für die umliegenden Felder
				newDistance = (short) (playersReachableSquaresMoves[playerPosition] + 1);

				// Alle von der aktuellen Position erreichbaren Felder als neue Ausgangsbasis aufnehmen,
				// falls sie vorher nicht bereits erreicht wurden.
				for (int directionOffset : offset) {
					newPosition = playerPosition + directionOffset;
					if (isAccessible(newPosition) && playersReachableSquaresMoves[newPosition] == UNREACHABLE) {
						positionsToBeAnalyzed[++highestIndex] = newPosition;
						playersReachableSquaresMoves[newPosition] = newDistance;
					}
				}
			}
		}
	}

	/**
	 * This class identifies and marks the reachable squares of a box.
	 * The current player position and the positions of the other boxes are considered when
	 * identifying these squares.
	 */
	public class BoxReachableSquares {

		// Indicator for a reachable square.
		private int indicatorReachableSquare = 1;

		// Array, where all reachable squares are marked with the indicator value.
		private int[] boxReachableSquaresArray;

		// Queue holding the positions which still have to be analyzed.
		private final IntStack positionsStack;

		// Array holding the information which square has already been reached from which direction
		private int[][] alreadyReachedSquares;

		/** Frozen boxes deadlock detection. */
		private final FreezeDeadlockDetection freezeDeadlockDetection;

		/** Deadlockdetection for closed diaginal deadlocks. */
		private final ClosedDiagonalDeadlock closedDiagonalDeadlockDetection;

		/**
		 * Constructor
		 */
		public BoxReachableSquares() {
			boxReachableSquaresArray		= new int[size];
			positionsStack          		= new IntStack(4 * size);
			alreadyReachedSquares    		= new int[size][DIRS_COUNT];
	 		freezeDeadlockDetection  		= new FreezeDeadlockDetection(Board.this);
	 		closedDiagonalDeadlockDetection = new ClosedDiagonalDeadlock(Board.this);
		}

		/**
		 * Returns whether the given position has been marked as reachable.
		 *
		 * @param position  Position to be checked to be reachable.
		 * @return	<code>true</code> if position is reachable,
		 *         <code>false</code> if position isn't reachable
		 */
		public boolean isSquareReachable(int position) {
			return boxReachableSquaresArray[position] == indicatorReachableSquare;
		}

		/**
		 * Identifies and marks the reachable squares of a box located at the passed position.
		 * The current player position and the positions of the other boxes are considered when
		 * identifying these squares.
		 *
		 * Simple deadlocks and freeze deadlocks are taken account of, too.
		 *
		 * @param boxPosition the  box position
		 * @param markCurrentPosition specifies whether the current position has also to be marked
		 */
		public void markReachableSquares(int boxPosition, boolean markCurrentPosition) {

			// Backup the current positions
			int playerPositionBackup = playerPosition;
			int boxPositionBackup = boxPosition;

			// Timestamp when this method has to stop reducing the reachable squares
			// because of deadlocks. (200 milli seconds after this method has started)
			long timestampWhenToStop = System.currentTimeMillis() + 200;

			// Increase indicator every time this method is called for avoiding having to erease
			// the array every time.
			indicatorReachableSquare++;

			// Push the current positions to the stack.
			positionsStack.add(playerPosition);
			positionsStack.add(boxPosition);

			// Remove the box from the board, because it is set within the while-loop.
			removeBox(boxPosition);

			// Loop until no more positions can be reached
			while (!positionsStack.isEmpty()) {
				boxPosition    = positionsStack.remove();
				playerPosition = positionsStack.remove();

				// Set the board position that was saved in the stack and
				// update the array holding the reachable squares of the player.
				setBox(boxPosition);
				playersReachableSquares.update();

				// Push the box to every direction possible. If the the box has never been pushed
				// to the new position with this direction before the situation is added to the stack.
				for (int direction = 0; direction < DIRS_COUNT; direction++) {
					int newBoxPosition = boxPosition + offset[direction];
					if (isAccessible(newBoxPosition) && playersReachableSquares.isSquareReachable(boxPosition - offset[direction])
							&& alreadyReachedSquares[newBoxPosition][direction] != indicatorReachableSquare) {

						// Skip simple deadlocks if simple deadlocks are to be detected.
						if (Settings.detectSimpleDeadlocks == true && isSimpleDeadlockSquare(newBoxPosition)) {
							continue;
						}

						// Mark the square as reachable for the box and save the status that it has
						// been reached with the current direction. Then add the new position to the stack.
						alreadyReachedSquares[newBoxPosition][direction] = indicatorReachableSquare;
						boxReachableSquaresArray[newBoxPosition] = indicatorReachableSquare;
						positionsStack.add(boxPosition);
						positionsStack.add(newBoxPosition);
					}
				}
				removeBox(boxPosition);
			}

			// Mark the current position of the box as reachable as requested.
			boxReachableSquaresArray[boxPositionBackup] = (markCurrentPosition ? indicatorReachableSquare : indicatorReachableSquare - 1);

			// Remove as many squares creating a freeze deadlock as the timelimit allows if the freeze test is enabled.
			if (Settings.detectFreezeDeadlocks) {
				for (int position = firstRelevantSquare; position < lastRelevantSquare && System.currentTimeMillis() < timestampWhenToStop; position++) {

					// Skip positions that are already marked as not reachable
					if (boxReachableSquaresArray[position] != indicatorReachableSquare) {
						continue;
					}

					// Set the box to the relevant position. The freeze detection takes the
					// player position into account. At this moment we don't know where the player
					// would be after having pushed the box so we set him at the same position as the box
					// in order to be sure he has access to every area.
					setBox(position);
					setPlayerPosition(position);
					if (freezeDeadlockDetection.isDeadlock(position, false)) {
						boxReachableSquaresArray[position] = 0;
					}
					removeBox(position);
				}
			}

			// Remove as many squares creating a closed diagonal deadlock as the timelimit allows.
			for (int position = firstRelevantSquare; position < lastRelevantSquare && System.currentTimeMillis() < timestampWhenToStop; position++) {

				// Skip positions that are already marked as not reachable
				if (boxReachableSquaresArray[position] != indicatorReachableSquare) {
					continue;
				}

				setBox(position);
				if(closedDiagonalDeadlockDetection.isDeadlock(position) == true) {
					boxReachableSquaresArray[position] = 0;
				}
				removeBox(position);
			}

			// Reset the original board position.
			playerPosition = playerPositionBackup;
			setBox(boxPositionBackup);
		}

		/**
		 * Unmarks all reachable squares.
		 * After this method is called <code>isSquareReachable()</code> will return <code>false</code>
		 * for every position.
		 */
		public void unmarkReachableSquares() {
			indicatorReachableSquare++;
		}
	}

	/**
	 * This class identifies and marks the reachable squares of a box.
	 * The current player position but NOT the other boxes are considered
	 * when identifying these squares.
	 */
	public class BoxReachableSquaresOnlyWalls {

		/** Indicator for a reachable square. */
		private int indicatorReachableSquare = 1;

		/** Array, where all reachable squares are marked with the
		 *  indicator value.
		 */
		private int[] boxReachableSquaresArray;

		/** Queue holding the positions which still have to be analyzed. */
		private int[] positionsQueue;

		/** Array holding the information which square has already been
		 *  reached from which direction
		 */
		private int[][] alreadyReachedSquares;

		/**
		 * Constructor
		 */
		public BoxReachableSquaresOnlyWalls() {
			boxReachableSquaresArray = new int[size];
			positionsQueue           = new int[DIRS_COUNT * size];
			alreadyReachedSquares    = new int[size][DIRS_COUNT];
		}

		/**
		 * Returns whether the given position has been marked as reachable.
		 *
		 * @param position  position to be checked to be reachable.
		 * @return	<code>true</code> position is reachable
		 *          <code>false</code> position isn't reachable
		 */
		public boolean isSquareReachable(int position) {
			return boxReachableSquaresArray[position] == indicatorReachableSquare;
		}

		/**
		 * Identifies and marks the reachable squares of a box located at the passed position.
		 * The current player position is considered when identifying these squares BUT NOT the other boxes.
		 *
		 * Simple deadlocks are taken account of, too.
		 *
		 * @param boxPosition the  box position
		 * @param markCurrentPosition specifies whether the current position has also to be marked
		 */
		public void markReachableSquares(int boxPosition, boolean markCurrentPosition) {

			// Backup the current positions
			int playerPositionBackup = playerPosition;
			int boxPositionBackup    = boxPosition;

			int topOfQueue = 0;
			int newBoxPosition = 0;

			// Increase indicator every time this method is called for avoiding having to erease
			// the array every time.
			indicatorReachableSquare++;

			// Add the current positions to the queue.
			positionsQueue[topOfQueue++] = playerPosition;
			positionsQueue[topOfQueue++] = boxPosition;

			// Loop until no more positions can be reached
			while (topOfQueue > 1) {

				boxPosition    = positionsQueue[--topOfQueue];
				playerPosition = positionsQueue[--topOfQueue];

				// Set the board position that was saved in the queue and update the array holding the reachable squares of the player.
				// A wall is set because boxes are ignored as obstacles.
				setWall(boxPosition);
				playersReachableSquaresOnlyWalls.update();

				// Push the box to every direction possible. If the the box has never been pushed
				// to the new position with this direction before the situation is added to the queue.
				for (int direction = 0; direction < DIRS_COUNT; direction++) {
					newBoxPosition = boxPosition + offset[direction];
					if (isWall(newBoxPosition) == false && playersReachableSquaresOnlyWalls.isSquareReachable(boxPosition - offset[direction])
							&& alreadyReachedSquares[newBoxPosition][direction] != indicatorReachableSquare) {

						// Skip simple deadlocks.
						if (isSimpleDeadlockSquare(newBoxPosition)) {
							continue;
						}

						// Mark the square as reachable for the box and save the status that it has
						// been reached with the current direction. Then add the new position to the queue.
						alreadyReachedSquares[newBoxPosition][direction] = indicatorReachableSquare;
						boxReachableSquaresArray[newBoxPosition] = indicatorReachableSquare;
						positionsQueue[topOfQueue++] = boxPosition;
						positionsQueue[topOfQueue++] = newBoxPosition;
					}
				}
				removeWall(boxPosition);
			}

			// Mark the current position of the box as reachable as requested.
			boxReachableSquaresArray[boxPositionBackup] = indicatorReachableSquare - (markCurrentPosition ? 0 : 1);

			// Reset the original board position.
			playerPosition = playerPositionBackup;
		}

		/**
		 * Unmarks all reachable squares.
		 * After this method is called <code>isSquareReachable()</code> will return <code>false</code>
		 * for every position.
		 */
		public void unmarkReachableSquares() {
			indicatorReachableSquare++;
		}
	}

	/**
	 * This class identifies and marks the reachable squares of a box when pulling a box.
	 * The current player position and the positions of the other boxes are considered when
	 * identifying these squares.
	 */
	public class BoxReachableSquaresBackwards {

		// Indicator for a reachable square.
		private int indicatorReachableSquare = 1;

		// Array, where all reachable squares are marked with the indicator value.
		private int[] boxReachableSquaresArray;

		// Stack holding the positions which still have to be analyzed.
		private final IntStack positionsStack;

		// Array holding the information which square has already been reached from which direction.
		private int[][] alreadyReachedSquares;

		/**
		 * Constructor
		 */
		public BoxReachableSquaresBackwards() {
			boxReachableSquaresArray = new int[size];
			positionsStack           = new IntStack(3 * size);
			alreadyReachedSquares    = new int[size][DIRS_COUNT];
		}

		/**
		 * Returns whether the given position has been marked as reachable.
		 *
		 * @param position  Position to be checked to be reachable.
		 * @return	<code>true</code> position is reachable
		 *         <code>false</code> position isn't reachable
		 */
		public boolean isSquareReachable(int position) {
			return boxReachableSquaresArray[position] == indicatorReachableSquare;
		}

		/**
		 * Identifies and marks the backwards reachable squares of a box
		 * located at the passed position.
		 * The current player position and the positions of the other boxes
		 * are considered when identifying these squares.
		 *
		 * @param boxPosition  the box position
		 * @param markCurrentPosition specifies whether the current position has also to be marked
		 */
		public void markReachableSquares(int boxPosition, boolean markCurrentPosition) {

			// Backup the current positions
			int playerPositionBackup = playerPosition;
			int boxPositionBackup = boxPosition;

			// Increase indicator every time this method is called for avoiding having to erease
			// the array every time.
			indicatorReachableSquare++;

			// Push the current positions to the stack.
			positionsStack.add(playerPosition);
			positionsStack.add(boxPosition);

			// Remove the box from the board, because it is set within the while-loop.
			removeBox(boxPosition);

			// Loop until no more positions can be reached
			while (!positionsStack.isEmpty()) {
				boxPosition    = positionsStack.remove();
				playerPosition = positionsStack.remove();

				// Set the board position that was saved in the queue and
				// update the array holding the reachable squares of the player.
				setBox(boxPosition);
				playersReachableSquares.update();

				// Pull the box to every direction possible. If the the box has never been pulled
				// to the new position from this direction before the situation is added to the queue.
				for (int direction = 0; direction < DIRS_COUNT; direction++) {
					int newBoxPosition = boxPosition + offset[direction];

					// Skip simple deadlocks.
					if (isSimpleDeadlockSquare(newBoxPosition)) {
						continue;
					}

					if (isAccessible(newBoxPosition)
							&& playersReachableSquares.isSquareReachable(newBoxPosition + offset[direction])
							&& alreadyReachedSquares[newBoxPosition][direction] != indicatorReachableSquare) {

						// Mark the square as reachable for the box and save the status that it has
						// been reached with the current direction. Then add the new position to the queue.
						alreadyReachedSquares[newBoxPosition][direction] = indicatorReachableSquare;
						boxReachableSquaresArray[newBoxPosition] = indicatorReachableSquare;
						positionsStack.add(newBoxPosition + offset[direction]);
						positionsStack.add(newBoxPosition);
					}
				}
				removeBox(boxPosition);
			}

			// Mark the current position of the box as reachable as requested.
			boxReachableSquaresArray[boxPositionBackup] = (markCurrentPosition ? indicatorReachableSquare : indicatorReachableSquare - 1);

			// Reset the original board position.
			playerPosition = playerPositionBackup;
			setBox(boxPositionBackup);
		}

		/**
		 * Unmarks all reachable squares.
		 * After this method is called <code>isSquareReachable()</code> will return <code>false</code>
		 * for every position.
		 */
		public void unmarkReachableSquares() {
			indicatorReachableSquare++;
		}
	}

	/**
	 * This class identifies and marks the reachable squares of a box when pulling a box.
	 * The current player position BUT NOT other boxes are considered when identifying these squares.
	 */
	public class BoxReachableSquaresBackwardsOnlyWalls {

		// Indicator for a reachable square.
		private int indicatorReachableSquare = 1;

		// Array, where all reachable squares are marked with the indicator value.
		private int[] boxReachableSquaresArray;

		// Queue holding the positions which still have to be analyzed.
		private int[] positionsQueue;

		// Array holding the information which square has already been reached from which direction.
		private int[][] alreadyReachedSquares;

		/**
		 * Constructor
		 */
		public BoxReachableSquaresBackwardsOnlyWalls() {
			boxReachableSquaresArray = new int[size];
			positionsQueue           = new int[DIRS_COUNT * size];
			alreadyReachedSquares    = new int[size][DIRS_COUNT];
		}

		/**
		 * Returns whether the given position has been marked as reachable.
		 *
		 * @param position  Position to be checked to be reachable.
		 * @return	<code>true</code> position is reachable
		 *          <code>false</code> position isn't reachable
		 */
		public boolean isSquareReachable(int position) {
			return boxReachableSquaresArray[position] == indicatorReachableSquare;
		}

		/**
		 * Identifies and marks the backwards reachable squares of a box located at the passed position.
		 * The current player position BUT NOT other boxes are considered when identifying these squares.
		 *
		 * @param boxPosition  the box position
		 * @param markCurrentPosition specifies whether the current position has also to be marked
		 */
		public void markReachableSquares(int boxPosition, boolean markCurrentPosition) {

			// Backup the current positions
			int playerPositionBackup = playerPosition;
			int boxPositionBackup = boxPosition;

			int topOfQueue = 0;
			int newBoxPosition = 0;

			// Increase indicator every time this method is called
			// for avoiding having to erase the array every time.
			indicatorReachableSquare++;

			// Push the current positions to the queue.
			positionsQueue[topOfQueue++] = playerPosition;
			positionsQueue[topOfQueue++] = boxPosition;

			// Loop until no more positions can be reached
			while (topOfQueue > 1) {

				boxPosition    = positionsQueue[--topOfQueue];
				playerPosition = positionsQueue[--topOfQueue];

				// Set the board position that was saved in the queue and
				// update the array holding the reachable squares of the player.
				// A wall is set instead of a box because boxes are ignored in this class.
				setWall(boxPosition);
				playersReachableSquaresOnlyWalls.update();

				// Pull the box to every direction possible. If the the box has never been pulled
				// to the new position from this direction before the situation is added to the queue.
				for (int direction = 0; direction < DIRS_COUNT; direction++) {
					newBoxPosition = boxPosition + offset[direction];

					// Skip simple deadlocks.
					if (isSimpleDeadlockSquare(newBoxPosition)) {
						continue;
					}

					if (isWall(newBoxPosition) == false
							&& playersReachableSquaresOnlyWalls.isSquareReachable(newBoxPosition + offset[direction])
							&& alreadyReachedSquares[newBoxPosition][direction] != indicatorReachableSquare) {

						// Mark the square as reachable for the box and save the status that it has
						// been reached with the current direction. Then add the new position to the queue.
						alreadyReachedSquares[newBoxPosition][direction] = indicatorReachableSquare;
						boxReachableSquaresArray[newBoxPosition] = indicatorReachableSquare;
						positionsQueue[topOfQueue++] = newBoxPosition + offset[direction];
						positionsQueue[topOfQueue++] = newBoxPosition;
					}
				}
				removeWall(boxPosition);
			}

			// Mark the current position of the box as reachable as requested.
			boxReachableSquaresArray[boxPositionBackup] = (markCurrentPosition ? indicatorReachableSquare : indicatorReachableSquare - 1);

			// Reset the original board position.
			playerPosition = playerPositionBackup;
		}

		/**
		 * Unmarks all reachable squares.
		 * After this method is called <code>isSquareReachable()</code>
		 * will return <code>false</code> for every position.
		 */
		public void unmarkReachableSquares() {
			indicatorReachableSquare++;
		}
	}

	protected class BadSquares {

		/**
		 * Diese Methode ermittelt alle SimpleDeadlockfelder bezüglich des Vorwärts-
		 * schiebens von Kisten. Dazu wird für jedes Feld geprüft, ob von ihm aus ein
		 * Zielfeld erreichbar ist. SimpleDeadlockfelder sind Felder, die ein Level
		 * unlösbar machen, wenn eine Kiste auf ihnen steht - unabhängig davon,
		 * auf welche Seite der Kiste der Spieler dabei gelangen kann.
		 */
		protected void identifySimpleDeadlockSquaresForwards() {

			// Zähler für die Zielfelder
			int goalNo;

			// Alle Felder überprüfen: Kann von dem Feld aus eine Kiste auf irgend ein
			// Zielfeld geschoben werden ? Falls nein => SimpleDeadlockfeld.
			for (int position = firstRelevantSquare; position < lastRelevantSquare; position++) {

				// Alle Außenfelder und Mauern können überspringen werden
				if (isOuterSquareOrWall(position)) {
					continue;
				}

				// Bei Corralerzwingerfeldern spielt die Spielerposition eine Rolle.
				if (isCorralForcerSquare(position)) {
					// Prüfen, ob von diesem Feld irgend ein Zielfeld erreichbar ist.
					// (Da es kein Corralerzwingerfeld ist, ist die Spielerposition egal.
					// Es wird OBEN angenommen.)
					endOfLoop:
					for (goalNo = 0; goalNo < goalsCount; goalNo++) {
						for (int direction = 0; direction < DIRS_COUNT; direction++) {
							if (distances.boxDistancesForwards[direction][position][goalsPositions[goalNo]] != UNREACHABLE) {
								break endOfLoop;
							}
						}
					}
				} else {
					// Prüfen, ob von diesem Feld irgend ein Zielfeld erreichbar ist.
					// (Da es kein Corralerzwingerfeld ist, ist die Spielerposition egal.
					// Es wird OBEN angenommen.)
					for (goalNo = 0; goalNo < goalsCount; goalNo++) {
						if (distances.boxDistancesForwards[UP][position][goalsPositions[goalNo]] != UNREACHABLE) {
							break;
						}
					}
				}

				// Falls kein Zielfeld erreichbar ist, handelt es sich um ein SimpleDeadlockfeld.
				simpleDeadlockSquareForwards[position] = (goalNo == goalsCount);
			}


			/**
			 * Dead corridor detection.
			 * Some squares are deadlock squares because pushing a box to them ends
			 * in a one way street. Example:<pre>
			 *     ####
			 * #####  #
			 * #DDDD  #    D = dead end squares which are deadlock squares
			 * #####$ #
			 *     #+ #
			 *     ####</pre>
			 */

			// Backup the player position.
//			int playerPositionBackup = playerPosition;
//
//			 Check every square for detecting dead corridors.
//			for (int position = firstRelevantSquare; position < lastRelevantSquare; position++) {
//				// Jump over squares that are outside the player accessible area and squares that don't split the level into several parts (corral forcer).
//				if(isOuterSquareOrWall(position) || isCorralForcer(position) == false)
//					continue;
//
//				// Determine the number of sides from which the player can push a box from that position to any goal.
//				for(int direction=0; direction<DIRS; direction++) {
//
//					// Caculate the player position next to the box for every direction.
//					playerPosition = position + offset[direction];
//
//					// Jump over walls.
//					if(isWall(playerPosition))
//						continue;
//
//
//					for(goalNo=0; goalNo<goalsCount; goalNo++) {
//
//					}
//				}
//			}
		}

		/**
		 * Diese Methode ermittelt alle SimpleDeadlockfelder bezüglich des Rückwärts-
		 * ziehens von Kisten. Dazu wird für jedes Zielfeld geprüft, ob von ihm aus ein
		 * Kistenfeld erreichbar ist. SimpleDeadlockfelder sind Felder, die ein Level
		 * unlösbar machen, wenn eine Kiste auf ihnen steht - unabhängig davon,
		 * auf welche Seite der Kiste der Spieler dabei gelangen kann.
		 * Achtung: Es muss sozusagen das Spielfeld invertiert werden: Alle Zielfelder
		 * erhalten eine Kiste und alle Felder mit einer Kiste werden zu Zielfeldern.
		 * Deswegen muss geprüft werden, von welchem Zielfeld aus eine Kiste ein aktuelles
		 * Kistenfeld erreichen könnte (durch Ziehen!)
		 */
		protected void identifySimpleDeadlockSquaresBackwards() {

			int boxNo = -1;

			// Alle Felder überprüfen: Kann von dem Feld aus eine Kiste auf irgend ein
			// aktuelles Kistenfeld gezogen werden ? Falls nein => SimpleDeadlockfeld.
			for (int position = firstRelevantSquare; position < lastRelevantSquare; position++) {

				// Alle Außenfelder und Mauern können überspringen werden
				if (isOuterSquareOrWall(position)) {
					continue;
				}

				// Bei Corralerzwingerfeldern spielt die Spielerposition eine Rolle.
				if (isCorralForcerSquare(position)) {
					// Prüfen, ob von diesem Feld irgend ein Kistenfeld erreichbar ist.
					// (Da es kein Corralerzwingerfeld ist, ist die Spielerposition egal.
					// Es wird OBEN angenommen.)
					endOfLoop:
					for (boxNo = 0; boxNo < boxCount; boxNo++) {
						for (int direction = 0; direction < DIRS_COUNT; direction++) {
							if (distances.boxDistancesBackwards[direction][position][boxData.getBoxPosition(boxNo)] != UNREACHABLE) {
								break endOfLoop;
							}
						}
					}
				} else {
					// Prüfen, ob von diesem Feld irgend ein Kistenfeld erreichbar ist.
					// (Da es kein Corralerzwingerfeld ist, ist die Spielerposition egal.
					// Es wird OBEN angenommen.)
					for (boxNo = 0; boxNo < boxCount; boxNo++) {
						if (distances.boxDistancesBackwards[UP][position][boxData.getBoxPosition(boxNo)] != UNREACHABLE) {
							break;
						}
					}
				}

				// Falls kein Zielfeld erreichbar ist, handelt es sich um ein SimpleDeadlockfeld.
				simpleDeadlockSquareBackwards[position] = (boxNo == boxCount);
			}
		}

		/**
		 * Detect <em>advanced simple deadlocks</em>.
		 * Advanced simple deadlocks are squares, which generate a bipartite
		 * deadlock, when a box is located on them -
		 * independently from the location of the other boxes.
		 * Example:<pre>
		 *   ######
		 *   #.$ @#
		 *   ##A$.#
		 *    #   #
		 *    #####</pre>
		 *
		 * The square marked "A" is an advanced simple deadlock square.
		 * <p>
		 * Our advanced simple deadlocks always occur with boxes which can
		 * reach just a single goal square.  We do not check any other cases
		 * for performance reasons (no real lower bound calculation).
		 * <p>
		 * The result of this computation is stuffed into the Board.
		 * @see Board#setAdvancedSimpleDeadlock(int)
		 */
		protected void identifyAdvancedSimpleDeadlockSquaresForwards() {

			// In order to have any effect at all, we need at least 2 boxes
			// For speed up see e.g. Microban III, 101.
			if (boxCount < 2) {
				return;
			}

			// Loop over all boxes.
			for (int boxNo = 0; boxNo < boxCount; boxNo++) {

				// Number of goals this box can reach.
				int reachableGoalsCount = 0;

				// Number of the reachable goal.
				int nrReachableGoal = 0;

				// Calculate the number of reachable goals of the box.
				for (int goalNo = 0; goalNo < goalsCount && reachableGoalsCount < 2; goalNo++) {
					if (distances.getBoxDistanceForwardsPlayerPositionIndependentNo(boxNo, goalNo) != UNREACHABLE) {
						nrReachableGoal = goalNo;
						reachableGoalsCount++;
					}
				}

				// If the box can reach more than just one goal
				// immediately continue with the next box.
				if (reachableGoalsCount > 1) {
					continue;
				}

				// Box position.
				final int boxPosition;

				// Whether "markReachableSquares" done for "boxPosition"
				boolean boxPositionMarked;

				// Get the position of the box which can only reach one goal.
				boxPosition       = boxData.getBoxPosition(boxNo);
				boxPositionMarked = false;

				// Check every square of the board whether a box from
				// this square can also only reach this goal.
				loopLabel:
				for (int position = firstRelevantSquare; position < lastRelevantSquare; position++) {

					// Jump over all known deadlock squares
					if (isAccessibleBox(position) == false) {
						continue;
					}

					// Check whether a box on this position can also only
					// reach this goal. If yes, two boxes share only one goal.
					for (int goalNo = 0; goalNo < goalsCount; goalNo++) {
						if (distances.getBoxDistanceForwardsPlayerPositionIndependent(position, goalsPositions[goalNo]) != UNREACHABLE) {
							if (goalNo != nrReachableGoal) {
								continue loopLabel;
							}
						}
					}

					// It's only an advanced simple deadlock
					// if the current box can't reach the position, too.
					if ( ! boxPositionMarked) {
						boxReachableSquaresOnlyWalls.markReachableSquares(boxPosition, true);
						boxPositionMarked = true;
					}
					if(boxReachableSquaresOnlyWalls.isSquareReachable(position) == false) {
						// A box on this square can only reach a goal
						// which is already reserved for another box
						// => deadlock square.
						setAdvancedSimpleDeadlock(position);
					}
				}
			}
		}

		// Alle NichtZielfelder mit drei oder sogar vier umliegenden Mauern als
		// weiteres Mauerfeld kennzeichnen.
		//			boolean isSearchToBeContinued = true;
		//
		//			while(isSearchToBeContinued == true){
		//			isSearchToBeContinued = false;
		//
		//			for(int position = firstRelevantSquare; position < lastRelevantSquare; position++) {
		//
		//			// Falls das Feld frei ist (= keine Kiste, keine Mauer und kein Zielfeld!!!)
		//			if (isEmptySquare(position)) {
		//
		//			// Anzahl der Mauern rundherum(oben, unten, links, rechts) zählen
		//			byte anzahlNachbarmauern = 0;
		//			for(byte direction = 0; direction < DIRS; direction++)
		//			if(isWall(position+offset[direction]))
		//			anzahlNachbarmauern++;
		//
		//			// Wenn das Feld mehr als 2 Nachbarmauern hat, dann ist das Feld auch eine Mauer
		//			if (anzahlNachbarmauern > 2) {
		//			setWall(position);
		//			isSearchToBeContinued = true;
		//			}
		//			}
		//			}
		//			}
	}
}