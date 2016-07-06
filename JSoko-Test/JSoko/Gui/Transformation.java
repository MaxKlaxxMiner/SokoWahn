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
package de.sokoban_online.jsoko.gui;

import java.util.ArrayList;
import java.util.List;

import javax.swing.event.ChangeEvent;
import javax.swing.event.ChangeListener;
import javax.swing.event.EventListenerList;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.board.DirectionConstants;


/**
 * The display of the board can be transformed (rotated and/or mirrored).
 * Internally the board isn't transformed, however.
 * This class converts from external to internal representation and vice versa.
 * This class contains static methods, only.
 */
public final class Transformation implements DirectionConstants {

	/*
	 * The first 4 values (0..3) are clockwise rotation in units of 90 degrees,
	 * and can be used arithmetically (modulo 4).
	 */
	/** Constant for a rotation of 0 degree */
	final public static int ROTATION_BY_0_DEGREES = 0;

	/** Constant for a rotation of 90 degree (clockwise) */
	final public static int ROTATION_BY_90_DEGREES = 1;

	/** Constant for a rotation of 180 degree (clockwise) */
	final public static int ROTATION_BY_180_DEGREES = 2;

	/** Constant for a rotation of 270 degree (clockwise) */
	final public static int ROTATION_BY_270_DEGREES = 3;

	/** Constant for a horizontal flip */
	final public static int FLIP_HORIZONTALLY = 4;

	/** Constant for a vertical flip */
	final public static int FLIP_VERTICALLY = 5;

	/** Constant for no transformation (no flip and no rotation). */
	final public static int NO_TRANSFORMATION = 6;

	/** Current rotation of the external view on the board (0..3). */
	private static int rotationValue;

	/** Whether the board is displayed with a horizontal flip. */
	private static boolean flippedHorizontally;

	// Arrays needed for converting the positions from external to internal and vice versa.
	private static int[] positionInternalToExternal;
	private static int[] positionExternalToInternal;

	/** Reference to the main object. */
	private static JSoko application;

    /** A list of event listeners for this component. */
    private static EventListenerList listenerList = new EventListenerList();

	// For easier access we store here the size, width and height of the board.
	static int boardSize;
	static int boardWidth;
	static int boardHeight;

	/**
	 * LURD characters indexed by direction, lower case and upper case.
	 */
	static private final char[] lurdCharacters = { 'u', 'd', 'l', 'r',
	                                               'U', 'D', 'L', 'R' };

	/**
	 * This array maps internal directions to external directions.
	 * Since we may display the board transformed, it may happen, that an "UP" on the
	 * screen is e.g. a "LEFT" on the internal board ...
	 * <br>1. index: rotation value (0°, 90°, 180°, 270°)
	 * <br>2. index: horizontal flip (no, yes)
	 * <br>3. index: internal direction (UP, DOWN, LEFT, RIGHT)
	 */
	final private static int[][][] directionConvertionArrayInternalToExternal = new int[][][] {
			{ { UP, DOWN, LEFT, RIGHT }, //  no  rotation, no mirroring
			  { UP, DOWN, RIGHT, LEFT }, //  no  rotation, horizontal mirroring (= vertical exchange)
			},
			{ { RIGHT, LEFT, UP, DOWN }, //  90° rotation, no mirroring
			  { LEFT, RIGHT, UP, DOWN }, //  90° rotation, horizontal mirroring (= vertical exchange)
			},
			{ { DOWN, UP, RIGHT, LEFT }, // 180° rotation, no mirroring
			  { DOWN, UP, LEFT, RIGHT }, // 180° rotation, horizontal mirroring (= vertical exchange)
			},
			{ { LEFT, RIGHT, DOWN, UP }, // 270° rotation, no mirroring
			  { RIGHT, LEFT, DOWN, UP }, // 270° rotation, horizontal mirroring (= vertical exchange)
			}
		};

	/**
	 * This array maps external directions to internal directions.
	 * Since we may display the board transformed, it may happen, that an "UP" on the
	 * screen is e.g. a "LEFT" on the internal board ...
	 * <br>1. index: rotation value (0°, 90°, 180°, 270°)
	 * <br>2. index: horizontal flip (no, yes)
	 * <br>3. index: external direction (UP, DOWN, LEFT, RIGHT)
	 */
	final private static int[][][] directionConvertionArrayExternalToInternal = new int[][][] {
			{ { UP, DOWN, LEFT, RIGHT }, //  no  rotation, no mirroring
			  { UP, DOWN, RIGHT, LEFT }, //  no  rotation, vertical mirroring (= horizontal exchange)
			},
			{ { LEFT, RIGHT, DOWN, UP }, //  90° Rotation, no mirroring
			  { LEFT, RIGHT, UP, DOWN }, //  90° Rotation, vertical mirroring (= horizontal exchange)
			},
			{ { DOWN, UP, RIGHT, LEFT }, // 180° Rotation, no mirroring
			  { DOWN, UP, LEFT, RIGHT }, // 180° Rotation, vertical mirroring (= horizontal exchange)
			},
			{ { RIGHT, LEFT, UP, DOWN }, // 270° Rotation, no mirroring
			  { RIGHT, LEFT, DOWN, UP }, // 270° Rotation, vertical mirroring (= horizontal exchange)
			} };

	/**
	 * Constructor
	 */
	private Transformation() {
	}

	// inhibit cloning
	@Override
	public Object clone() throws CloneNotSupportedException {
		throw new CloneNotSupportedException();
	}

	/**
	 * Sets the reference to the main object.
	 *
	 * @param application reference to the main Object which holds all references
	 */
	final public static void setApplication(JSoko application) {
		Transformation.application = application;
	}

	/**
	 * This method is called when a new level is set for playing.
	 * It ensures that the display of the level is transformed correctly.
	 * <p>
	 * Here we parse the string constructed by
	 * {@link #getTransformationAsString()}.
	 */
	final public static void newlevel() {

		boardSize   = application.board.size;
		boardWidth  = application.board.width;
		boardHeight = application.board.height;

		// Create arrays to store the mapping between internal and external positions.
		positionInternalToExternal = new int[boardSize];
		positionExternalToInternal = new int[boardSize];

		// Fetch the string which indicates, how the level is to be transformed
		// for its display on the screen.
		String levelTransformation = application.currentLevel.getTransformationString();

		// For a new level the transformation is off by default, except a transformation
		// has been explicitly requested for this level.
		// But in editor mode we never load a new level with an existing view,
		// but rather build a new level from scratch.
		// Hence, with activated editor mode we set transformations off.
		if (application.isEditorModeActivated() || levelTransformation.equals("")) {
			rotationValue = 0;
			flippedHorizontally = false;
			return;
		} else {
			if (levelTransformation.indexOf(" 0") != -1) {
				rotationValue = ROTATION_BY_0_DEGREES;
			}
			if (levelTransformation.indexOf("90") != -1) {
				rotationValue = ROTATION_BY_90_DEGREES;
			}
			if (levelTransformation.indexOf("180") != -1) {
				rotationValue = ROTATION_BY_180_DEGREES;
			}
			if (levelTransformation.indexOf("270") != -1) {
				rotationValue = ROTATION_BY_270_DEGREES;
			}
			if (levelTransformation.indexOf("horizontal") != -1) {
				flippedHorizontally = true;
			} else {
				flippedHorizontally = false;
			}
		}

		// FFS/hm: mapping arrays not filled?

		// Inform the action listeners.
		fireChangeEvent(new ChangeEvent(Transformation.class));
	}

	/**
	 * This method is called, when the dimensions of the level have changed.
	 * That may happen, when the editor extends the level.
	 */
	final public static void setNewLevelSize() {

		boardSize   = application.board.size;
		boardWidth  = application.board.width;
		boardHeight = application.board.height;

		// Create the arrays in which we store the mapping between internal and external
		// positions.  The dimensions may have changed, so we just recreate them.
		positionInternalToExternal = new int[boardSize];
		positionExternalToInternal = new int[boardSize];

		// New dimensions imply that the internal mapping arrays are not valid any more.
		// Hence we call "transform()" with an illegal argument, to force a recomputation
		// without changing the transformation itself.
		transform(-1);
	}

	/**
	 * Sets the correct transformation value if the board is to be displayed
	 * transformed.
	 * <p>
	 * This method must NOT be called when a level is to be transformed.
	 * Call {@link MainBoardDisplay#transformBoard(int)} instead!
	 *
	 * @param transformationValue the transformation to be done
	 */
	final public static void transform(final int transformationValue) {

		// Set new rotation value.
		// When the board is mirrored, we change the direction of the rotation.
		if (transformationValue == ROTATION_BY_90_DEGREES || transformationValue == ROTATION_BY_270_DEGREES) {
			if (flippedHorizontally) {
				rotationValue = (rotationValue + transformationValue + ROTATION_BY_180_DEGREES) % 4;
			} else {
				rotationValue = (rotationValue + transformationValue) % 4;
			}
		} else if (transformationValue == ROTATION_BY_180_DEGREES) {
			rotationValue = (rotationValue + transformationValue) % 4;
		}

		if (transformationValue == FLIP_HORIZONTALLY) {
			flippedHorizontally = ! flippedHorizontally;
		}

		if (transformationValue == FLIP_VERTICALLY) {
			rotationValue       = (rotationValue + ROTATION_BY_180_DEGREES) % 4;
			flippedHorizontally = ! flippedHorizontally;
		}

		// NO_TRANSFORMATION means: reset all transformation to default
		if (transformationValue == NO_TRANSFORMATION) {
			rotationValue       = ROTATION_BY_0_DEGREES;
			flippedHorizontally = false;
		}

		// To avoid recomputation for each each mapping request, we store the position
		// mapping between internal and external positions in 2 arrays.
		for (int position = 0; position < boardSize; position++) {

			// compute the corresponding external position
			int outputPosition = getInternToExtern(position);

			// Store this pair of values for both mappings
			positionExternalToInternal[outputPosition] = position;
			positionInternalToExternal[position] = outputPosition;
		}

		// Prüfen, ob die Grafikgrößen des Spielfeldes an die neue Transformation angepasst
		// werden müssen. Im Editor können ja Leerzeilen - Spalten vorhanden sein.
		if (application.isEditorModeActivated() && (transformationValue == ROTATION_BY_90_DEGREES
						|| transformationValue == ROTATION_BY_270_DEGREES || transformationValue == NO_TRANSFORMATION)) {
			application.board.optimizeBoardSizeForEditor(application.applicationGUI);

			// Die Levelgültigkeit checken. Dies ist nur notwendig, da dabei auch die durch
			// den Spieler erreichbaren Felder ermittelt werden.
			application.isLevelValid();

			// Neu Ermitteln, welche Grafik an welcher Position ausgegeben
			// werden muss. Anschließend wird das modifizierte Level ausgegeben.
			application.applicationGUI.mainBoardDisplay.setBoardToDisplay(application.board);
		}

		// Inform the action listeners.
		if (!application.isEditorModeActivated()) {
			fireChangeEvent(new ChangeEvent(Transformation.class));
		}
	}

	/**
	 * Returns the current transformation as String.
	 * This String is saved into the file of the level as information.
	 * Note: this string is NOT influenced by any language settings.
	 *
	 * @return the transformation as <code>String</code>
	 * @see #newlevel()
	 */
	static public String getTransformationAsString() {

		// If the level isn't displayed transformed return an empty String.
		if (getRotationValue() == ROTATION_BY_0_DEGREES && isLevelFlippedHorizontally() == false) {
			return "";
		}

		// Build the transformation string and return it.
		String transformationString = "View: Rotated "
			                        + getRotationAsString()
			                        + " degrees clockwise";
		if (isLevelFlippedHorizontally()) {
			transformationString += ", flipped horizontally.";
		} else {
			transformationString += ".";
		}

		return transformationString;
	}

	/**
	 * This method maps an internal board position to an external screen
	 * position. Due to rotations and flips the displayed board may differ
	 * from the internal board.
	 * <p>
	 * This method has to compute the mapping, since we use it to fill the
	 * map arrays.
	 *
	 * @param position  internal position in the board
	 * @return external screen position corresponding to the passed internal
	 *                  position
	 */
	final private static int getInternToExtern(int position) {

		// Split the position into X and Y components
		int x = position % boardWidth;
		int y = position / boardWidth;

		// The translated components, building the result value
		int x_external = x;
		int y_external = y;

		// First, consider the rotation
		switch (rotationValue) {

		case ROTATION_BY_90_DEGREES:
			x_external = boardHeight - 1 - y;
			y_external = x;
			break;

		case ROTATION_BY_180_DEGREES:
			x_external = boardWidth  - 1 - x;
			y_external = boardHeight - 1 - y;
			break;

		case ROTATION_BY_270_DEGREES:
			x_external = y;
			y_external = boardWidth  - 1 - x;
			break;
		}

		// Then, consider horizontal flip
		if (flippedHorizontally) {
			x_external = getOutputLevelWidth() - 1 - x_external;
		}

		return x_external + (getOutputLevelWidth() * y_external);
	}

	/**
	 * Returns the degree of rotation as String.
	 *
	 * @return	Degree the level is rotated
	 */
	final public static String getRotationAsString() {

		switch (getRotationValue()) {
		case ROTATION_BY_90_DEGREES:
			return "90";

		case ROTATION_BY_180_DEGREES:
			return "180";

		case ROTATION_BY_270_DEGREES:
			return "270";

		default:
			return "0";
		}
	}

	/**
	 * Due to rotations the width an height of the displayed boards may be exchanged
	 * with respect to the internal board.
	 * This method tells the current external width to be displayed.
	 *
	 * @return width of the external displayed board
	 */
	final public static int getOutputLevelWidth() {
		return rotationValue == ROTATION_BY_90_DEGREES || rotationValue == ROTATION_BY_270_DEGREES ? boardHeight : boardWidth;
	}

	/**
	 * Due to rotations the width an height of the displayed boards may be exchanged
	 * with respect to the internal board.
	 * This method tells the current external height to be displayed.
	 *
	 * @return height of the external displayed board
	 */
	final public static int getOutputLevelHeight() {
		return rotationValue == ROTATION_BY_90_DEGREES || rotationValue == ROTATION_BY_270_DEGREES ? boardWidth : boardHeight;
	}

	/**
	 * Tells whether width and height of the externally displayed board are exchanged
	 * with respect to the internal width and height.
	 *
	 * @return <code>true</code> if the width and the height are interchanged, and
	 * 		  <code>false</code> if the width and the height are not interchanged
	 */
	final public static boolean isWidthAndHeightInterchanged() {
		return rotationValue == ROTATION_BY_90_DEGREES || rotationValue == ROTATION_BY_270_DEGREES;
	}

	/**
	 * Returns the internal position corresponding to the specified external position.
	 *
	 * @param externalPosition  position on screen
	 * @return internal position corresponding to the external position
	 */
	final public static int getInternalPosition(int externalPosition) {
		return positionExternalToInternal[externalPosition];
	}

	/**
	 * Returns the external position corresponding to the specified internal position.
	 *
	 * @param interalPosition position in board
	 * @return screen position corresponding to board position
	 */
	final public static int getExternalPosition(int interalPosition) {
		return positionInternalToExternal[interalPosition];
	}

	/**
	 * Returns the internal direction in the board corresponding to the specified
	 * external direction on the screen.
	 *
	 * @param  externalDirection direction on screen
	 * @return internal direction corresponding to screen direction
	 */
	final public static int getInternalDirection(int externalDirection) {
		return (directionConvertionArrayExternalToInternal[rotationValue][flippedHorizontally == true ? 1 : 0][externalDirection]);
	}

	/**
	 * Returns the external direction on the screen corresponding to the specified
	 * internal direction in the board.
	 *
	 * @param  internalDirection direction in the board
	 * @return external direction corresponding to the board direction
	 */
	final public static int getExternalDirection(int internalDirection) {
		return (directionConvertionArrayInternalToExternal[rotationValue][flippedHorizontally == true ? 1 : 0][internalDirection]);
	}

	/**
	 * Returns the rotation value.
	 * <p>
	 * This value indicates how much the display of the board is rotated.
	 *
	 * @return the rotation value
	 */
	final public static int getRotationValue() {
		return rotationValue;
	}

	/**
	 * Returns whether the board is displayed horizontally flipped.
	 *
	 * @return <code>true</code> the board is displayed horizontally flipped
	 *  		<code>false</code> the board is not displayed horizontally flipped
	 */
	final public static boolean isLevelFlippedHorizontally() {
		return flippedHorizontally == true;
	}

	/**
	 * Returns a transformed version of the passed lurd string by transforming it to
	 * the the currently set transformation.
	 *
	 *
	 * @param lurdString  the string to be transformed
	 * @return the transformed string
	 */
	public static String getTransformedLURDInternalToExternal(String lurdString) {

		// Ensure that a String has been passed.
		if(lurdString == null) {
			return null;
		}

		// Current direction of a move.
		int currentDirection = 0;

		// Direction when the transformation is considered.
		int transformedDirection = 0;

		// The character representing the current move.
		char currentMoveCharacter;

		// The new lurd string which is a transformed version of the passes lurd string.
		StringBuilder transformedLURD = new StringBuilder();

		// Transform every move.
		for(int move=0; move < lurdString.length(); move++) {

			currentMoveCharacter = lurdString.charAt(move);

			// Determine the direction of the move.
			switch (currentMoveCharacter) {
			case 'u':
			case 'U':
				currentDirection = UP;
				break;

			case 'd':
			case 'D':
				currentDirection = DOWN;
				break;

			case 'l':
			case 'L':
				currentDirection = LEFT;
				break;

			case 'r':
			case 'R':
				currentDirection = RIGHT;
				break;
			}

			// Get the direction of the move considering the current transformation.
			transformedDirection = getExternalDirection(currentDirection);

			// Save the new direction to the new lurd string.
			int offset = Character.isLowerCase(currentMoveCharacter) ? 0 : 4;
			transformedLURD.append(lurdCharacters[transformedDirection+offset]);
		}

		return transformedLURD.toString();
	}

	/**
	 * Returns a transformed version of the passed lurd string by transforming it from
	 * the current transformation shown in the GUI to the internal representation.
	 *
	 * @param lurdString  the string to be transformed
	 * @return the transformed string
	 */
	public static String getTransformedLURDExternalToInternal(String lurdString) {

		// Ensure that a String has been passed.
		if(lurdString == null) {
			return null;
		}

		// Current direction of a move.
		int currentDirection = 0;

		// Direction when the transformation is considered.
		int transformedDirection = 0;

		// The character representing the current move.
		char currentMoveCharacter;

		// The new lurd string which is a transformed version of the passes lurd string.
		StringBuilder transformedLURD = new StringBuilder();

		// Transform every move.
		for(int move=0; move < lurdString.length(); move++) {

			currentMoveCharacter = lurdString.charAt(move);

			// Jump over white spaces.
			if(Character.isWhitespace(currentMoveCharacter)) {
				continue;
			}

			// Determine the direction of the move.
			switch (currentMoveCharacter) {
			case 'u':
			case 'U':
				currentDirection = UP;
				break;

			case 'd':
			case 'D':
				currentDirection = DOWN;
				break;

			case 'l':
			case 'L':
				currentDirection = LEFT;
				break;

			case 'r':
			case 'R':
				currentDirection = RIGHT;
				break;

			default:
				// The transformation stops at the first not valid character.
				return transformedLURD.toString();
			}

			// Get the direction of the move considering the current transformation.
			transformedDirection = getInternalDirection(currentDirection);

			// Save the new direction to the new lurd string.
			int offset = Character.isLowerCase(currentMoveCharacter) ? 0 : 4;
			transformedLURD.append(lurdCharacters[transformedDirection+offset]);
		}

		return transformedLURD.toString();
	}

	/**
	 * Returns a transformed version of the passed board considering the current transformation.
	 *
	 * @param boardData the board to be transformed
	 * @return the transformed board as array of strings
	 */
	public static ArrayList<String> getTransformedBoardData(List<String> boardData) {

		// Board element at a specific position.
		char boardElement;

		// Original position and transformed position.
		int originalPosition = 0;
		int transformedPosition = 0;

		// The new height and width of the transformed level.
		final int transformedLevelHeight = getOutputLevelHeight();
		final int transformedLevelWidth  = getOutputLevelWidth();

		// One row of the transformed board.  We will recycle it for each new row.
		StringBuilder boardRow = new StringBuilder(transformedLevelWidth);

		// The transformed board data stored in an ArrayList.
		ArrayList<String> transformedBoardData = new ArrayList<String>(transformedLevelHeight);

		// Transform the board row by row.
		for(int y=0; y < transformedLevelHeight; y++) {

			// Start over with an empty row buffer
			boardRow.setLength(0);

			// Loop over all columns of the new transformed board.
			for(int x=0; x < transformedLevelWidth; x++) {

				transformedPosition = y * transformedLevelWidth + x;
				originalPosition = getInternalPosition(transformedPosition);

				// Get the character of the original board.
				// FFS/hm: short lines? (currently does not occur)
				boardElement = boardData.get(originalPosition / boardWidth)
				                     .charAt(originalPosition % boardWidth);

				// Add the board element to the row of the transformed board.
				boardRow.append(boardElement);
			}

			// Add the row to the data of the transformed board.
			transformedBoardData.add(boardRow.toString());
		}

		// Return the transformed board as array of strings.
		return transformedBoardData;
	}

	/**
	 * Adds an <code>ChangeListener</code>.
	 * @param l the <code>ChangeListener</code> to be added
	 */
	public static void addChangeEventListener(ChangeListener l) {
		listenerList.add(ChangeListener.class, l);
	}

	/**
	 * Removes an <code>ChangeListener</code>.
	 *
	 * @param l the listener to be removed
	 */
	public static void removeChangeEventListener(ChangeListener l) {
		listenerList.remove(ChangeListener.class, l);
	}

	/**
	 * Notifies all listeners that have registered interest for
	 * notification on this event type.
	 *
	 * @param event  the <code>SolutionEventListener</code> object
	 */
	protected static void fireChangeEvent(ChangeEvent event) {
		// Guaranteed to return a non-null array
		Object[] listeners = listenerList.getListenerList();
		// Process the listeners last to first, notifying
		// those that are interested in this event
		for (int i = listeners.length-2; i>=0; i-=2) {
			if (listeners[i]==ChangeListener.class) {
				((ChangeListener)listeners[i+1]).stateChanged(event);
			}
		}
	}
}