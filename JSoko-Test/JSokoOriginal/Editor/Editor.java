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
package de.sokoban_online.jsoko.editor;

import java.awt.AWTException;
import java.awt.Rectangle;
import java.awt.Robot;
import java.awt.event.InputEvent;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;
import java.awt.event.MouseWheelListener;

import javax.swing.JPanel;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.board.Board;
import de.sokoban_online.jsoko.board.DirectionConstants;
import de.sokoban_online.jsoko.gui.MainBoardDisplay;
import de.sokoban_online.jsoko.gui.Skin;
import de.sokoban_online.jsoko.gui.Transformation;
import de.sokoban_online.jsoko.resourceHandling.Settings;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Debug;


/**
 * Editor class.
 */
@SuppressWarnings("serial")
public final class Editor extends JPanel implements MouseWheelListener, DirectionConstants {

	// Constants for the board elements.
	private final int WALL         = 0;
	private final int BOX          = 1;
	private final int GOAL         = 2;
	private final int PLAYER       = 3;
	private final int EMPTY_SQUARE = 4;

	/** Currently selected object to be put into the board. */
	private int currentlyMarkedObject = WALL;

	// Coordinates of the last clicked position. This is used to ensure that
	// while dragging the mouse not every move by one pixel fires a new event.
	// The new event is only fired when a new square is reached.
	private int lastClickXCoordinate;
	private int lastClickYCoordinate;

	/** Reference to the main object */
	private JSoko application;

	/** Convenience reference to the main board */
	private Board board;
	/** Convenience reference to the main board GUI */
	private MainBoardDisplay boardDisplay;

	/**
	 * Flag indicating whether the board has structurally been changed.
	 * If this flag is <code>true</code> an attempt to leave the editor triggers
	 * the confirmation question: "Do you want to save?"
	 */
	public boolean hasBoardBeenChanged = false;

	/**
	 * Creates an <code>Editor</code>. The created object handles all editor functionality.
	 *
	 * @param application the reference to the main object holding all references
	 */
	public Editor(JSoko application) {
		this.application = application;
	}

	/**
	 * Handles mouse events during editor mode.
	 *
	 * @param evt					the event which just occurred
	 * @param mouseHasBeenDragged	whether the mouse has been dragged
	 * @param mouseXCoordinate x coordinate of the mouse click
	 * @param mouseYCoordinate y coordinate of the mouse click
	 * @param mouseHasBeenClickedWithinBoard whether the mouse has been clicked within the board
	 */
	final public void handleMouseEvent(MouseEvent evt,
			                           boolean mouseHasBeenDragged,
			                           int mouseXCoordinate, int mouseYCoordinate,
			                           boolean mouseHasBeenClickedWithinBoard)
	{
		// Coordinates of a click.
		int x = 0;
		int y = 0;

		// Position of the board.
		int boardXCoordinate = 0;
		int boardYCoordinate = 0;

		// Boolean indicating whether the board has to be extended.
		boolean isBoardToBeExtended = false;

		// Side(s) at which a square has been erased (UP DOWN LEFT RIGHT).  This is an
		// (external) screen side, not an (internal) board side.
		boolean[] externalSideErasedSquare = new boolean[4];

		/**
		 * Here we store the size increments for the board.
		 * See <code>Board.optimizeBoardSizeForEditor()</code>.
		 */
		int[] boardSizeChangements = new int[4];

		// Get the current set skin. This is important to get the current graphic size.
		Skin skin = application.applicationGUI.mainBoardDisplay.getCurrentSkin();

		// Fetch coordinates of the mouse click
		x = evt.getX();
		y = evt.getY();

		// Fetch base coordinates for the board display
		boardXCoordinate = boardDisplay.xOffset;
		boardYCoordinate = boardDisplay.yOffset;

		// The board object selected by the user to be used in this action.
		// If the right mouse button is used the empty square is assumed to be selected.
		int selectedObject = currentlyMarkedObject;
		if((evt.getModifiersEx() & InputEvent.BUTTON3_DOWN_MASK) != 0) {
			selectedObject = EMPTY_SQUARE;
		}

		// Now/here we handle mouse clicks into/inside the board
		if (mouseHasBeenClickedWithinBoard == true) {

			// transform external coordinates into internal coordinates
			{
				final int externalPosition = mouseXCoordinate + Transformation.getOutputLevelWidth() * mouseYCoordinate;
				final int internalPosition = Transformation.getInternalPosition(externalPosition);
				mouseYCoordinate = internalPosition / board.width;
				mouseXCoordinate = internalPosition % board.width;
			}

			// When the mouse is dragged, we shall not put an element for each pixel move,
			// but rather when the move reaches a new square.
			if (mouseHasBeenDragged && mouseXCoordinate == lastClickXCoordinate
					                && mouseYCoordinate == lastClickYCoordinate) {
				return;
			}

			// Putting a wall onto a wall, or erasing an empty square both is a noop,
			// such that we can return immediately.
			if(    (selectedObject == WALL)
				&& board.isWall(mouseXCoordinate, mouseYCoordinate)
			 ||    (selectedObject == EMPTY_SQUARE)
				&& board.isEmptySquare(mouseXCoordinate, mouseYCoordinate))
			{
				return;
			}

			// Flags indicating where the box has to be extended.
			int extendAbove, extendBelow, extendLeft, extendRight;
			extendAbove = extendBelow = extendLeft = extendRight = 0;

			/*
			 * If a new object is put on the border of the current board then the board
			 * is extended so that there is always an empty "border" around the board
			 * that can be clicked by the user.
			 */
			// Check for extending to the left.
			if (mouseXCoordinate == 0) {
				extendLeft = 1;
				mouseXCoordinate++;
			}

			// Check for extending to the right.
			if (mouseXCoordinate == board.width - 1) {
				extendRight = 1;
			}

			// Check for extending the board above.
			if (mouseYCoordinate == 0) {
				extendAbove = 1;
				mouseYCoordinate++;
			}

			// Check for extending the board below.
			if (mouseYCoordinate == board.height - 1) {
				extendBelow = 1;
			}

			// Set the extended flag if the board has to be extended on any side.
			if (extendAbove + extendBelow + extendLeft + extendRight > 0) {
				isBoardToBeExtended = true;
			}

			if (selectedObject == EMPTY_SQUARE) {

				// If the board is to be extended because the user wants to set an empty
				// square at the border of the current board then this needn't to be done
				// (squares at the border of the board are always empty so this wouldn't
				// change anything).
				if (isBoardToBeExtended) {
					return;
				}

				// The board has never to be extended when an empty square is to be set.
				isBoardToBeExtended = false;
			}

			//If the board is to be extended but already has reached its maximum size then
			// just display a message and return.
			if (extendAbove + extendBelow > 0
					&& board.height > Settings.MAXIMUM_BOARDSIZE + 1
					|| extendLeft + extendRight > 0
					&& board.width > Settings.MAXIMUM_BOARDSIZE + 1) {
				boardDisplay.displayInfotext(Texts.getText("maxlevelsize"));

				return;
			}

			// Extend the board if requested.
			if (isBoardToBeExtended) {
				board.extendBoardSize(extendAbove, extendBelow, extendLeft, extendRight);
			} else {
				// If a wall, an empty square or the player is to be set on a square the same object is
				// already set at then return immediately.
				if (       selectedObject == WALL && board.isWall(mouseXCoordinate, mouseYCoordinate)
						|| selectedObject == EMPTY_SQUARE && board.isEmptySquare(mouseXCoordinate, mouseYCoordinate)
						|| selectedObject == PLAYER && board.playerPosition == mouseXCoordinate + board.width * mouseYCoordinate) {
					return;
				}
			}

			// Set the object selected by the user at the clicked position.
			setObject(selectedObject, mouseXCoordinate, mouseYCoordinate);

			hasBoardBeenChanged = true;

			/*
			 * Check if a board object has been "deleted" by setting an empty square.
			 * If yes the result might be an empty column or an empty row. In that case
			 * this empty column/row is removed.
			 */
			if (selectedObject == EMPTY_SQUARE) {

				int position = -1;

				// Check whether all rows up to the top board border are empty.
				for (position = (mouseYCoordinate + 1) * board.width - 1; position >= board.width; position--) {
					if (board.isEmptySquare(position) == false) {
						break;
					}
				}
				if (position < board.width) {
					externalSideErasedSquare[Transformation.getExternalDirection(UP)] = true;
				} else {
					// Check whether after the "deletion" all rows up to the lower board border are empty.
					for (position = mouseYCoordinate * board.width; position <= board.size - board.width; position++) {
						if (board.isEmptySquare(position) == false) {
							break;
						}
					}
					if (position > board.size - board.width) {
						externalSideErasedSquare[Transformation.getExternalDirection(DOWN)] = true;
					}
				}

				// Check whether after the "deletion" all columns up to the left board border are empty.
				int column = 0;
				jumpLabel: for (column = 1; column <= mouseXCoordinate; column++) {
					for (int row = 1; row < board.height - 2; row++) {
						if (board.isEmptySquare(column, row) == false) {
							break jumpLabel;
						}
					}
				}
				if (column > mouseXCoordinate) {
					externalSideErasedSquare[Transformation.getExternalDirection(LEFT)] = true;
				} else {
					// Check whether after the "deletion" all columns up to the right board border are empty.
					jumpLabel: for (column = mouseXCoordinate; column < board.width; column++) {
						for (int row = 1; row < board.height - 2; row++) {
							if (board.isEmptySquare(column, row) == false) {
								break jumpLabel;
							}
						}
					}
					if (column == board.width) {
						externalSideErasedSquare[Transformation.getExternalDirection(RIGHT)] = true;
					}
				}
			}

			// Now that the selected objecthas been set the board size must be adjusted.
			// A change of the board may be necessary when an object has been set at the border
			// of the board or an object has been deleted (by setting an empty square).
			if (isBoardToBeExtended || selectedObject == EMPTY_SQUARE) {
				boardSizeChangements = board.optimizeBoardSizeForEditor(application.applicationGUI);
			}

			// Due to the change of the board size the mouse coordinates must be adjusted
			// so they still correspond to the same position as before the board size change.
			mouseXCoordinate += boardSizeChangements[LEFT];
			mouseYCoordinate += boardSizeChangements[UP];

			// If more than one row/column has been deleted the new coordinates may be
			// invalid. This is checked here.
			if (selectedObject == EMPTY_SQUARE) {
				if (mouseXCoordinate < 0) {
					mouseXCoordinate = 0;
				} else if (mouseXCoordinate > board.width - 1) {
					mouseXCoordinate = board.width - 1;
				}
				if (mouseYCoordinate < 0) {
					mouseYCoordinate = 0;
				} else if (mouseYCoordinate > board.height - 1) {
					mouseYCoordinate = board.height - 1;
				}
			}

			// Remember the location at which we put the object.  we need this information
			// for mouse dragging, when we avoid an action for each pixel move, and wait
			// until we reach a new square.
			lastClickXCoordinate = mouseXCoordinate;
			lastClickYCoordinate = mouseYCoordinate;

			// Since we changed the level (board contents) we have to recalculate whether
			// the board is valid.  As a side effect we get error message texts if not.
			application.isLevelValid();

			// Recalculate which graphic is to be drawn at which location.
			// Then the modified level is displayed.
			boardDisplay.setBoardToDisplay(board);

			/*
			 * Durch die Erweiterung müssen eventuell die Grafikgrößen angepasst werden, so dass
			 * die aktuelle Mausposition nicht mehr auf die gleiche Spielfeldkoordinate
			 * zeigen wird, wie vor der Größenanpassung. Der Mauszeiger wird deshalb auf die
			 * Ausgabekoordinaten des geklickten Feldes gesetzt. Außerdem wird ermittelt,
			 * wo genau er im Feld vor der Erweiterung stand.
			 */
			float xPositionInSquare = ((x - boardXCoordinate) % skin.graphicWidth)  / (float) skin.graphicWidth;
			float yPositionInSquare = ((y - boardYCoordinate) % skin.graphicHeight) / (float) skin.graphicHeight;

			// Draw modified board. This must done immediately because the program
			// must use the new settings if the graphics are scaled.
			boardDisplay.paintImmediately();

			// Quick and dirty: as long as the editor has no own GUI, get the info
			// from the main GUI. This is necessary to have the current graphic size.
			skin = application.applicationGUI.mainBoardDisplay.getCurrentSkin();

			// Falls das Spielfeld in der Größe geändert wurde, wird die Maus entsprechend
			// verschoben, um ein bequemes Vergrößern / Verkleinern zu ermöglichen.
			if ((isBoardToBeExtended == true || boardSizeChangements[UP] != 0
					|| boardSizeChangements[DOWN] != 0
					|| boardSizeChangements[LEFT] != 0 || boardSizeChangements[RIGHT] != 0)

					// Bei Diagonalen wird die Maus auch versetzt, wenn sie nicht gedragged wird
					|| (extendAbove > 0 || extendBelow > 0)
					&& (extendLeft > 0 || extendRight > 0)
					|| (externalSideErasedSquare[UP] || externalSideErasedSquare[DOWN])
					&& (externalSideErasedSquare[LEFT] || externalSideErasedSquare[RIGHT])) {

				// Position im Feld mit den neuen Grafikgrößen umrechnen
				xPositionInSquare *= skin.graphicWidth;
				yPositionInSquare *= skin.graphicHeight;

				// Um die Navigation mit der Maus bequemer zu gestalten, wird die Maus 1 Pixel
				// von dem Rand positioniert, in dessen Richtung das Feld erweitert wurde.
				// So kann schon in wenigen Pixeln wieder ein neues Feld erreicht werden.
				// Falls allerdings ein Feld gelöscht wurde, wird die Maus fünf Pixel in
				// das gelöschte Feld gesetzt, damit bei einer Bewegung nicht gleich das
				// nächste Feld gelöscht wird.
				if (isBoardToBeExtended) {
					// In den Variablen "...Erweitern" ist gespeichert, an welchen Seiten
					// das Spielfeld intern erweitert wurde. Für das Setzen des Mauszeigers
					// muss ermittelt werden, an welcher Seite das Spielfeld auf dem Bildschirm
					// erweitert wurde.
					boolean[] externalSide = new boolean[4];
					if (extendAbove > 0) {
						externalSide[Transformation.getExternalDirection(UP)] = true;
					}
					if (extendBelow > 0) {
						externalSide[Transformation.getExternalDirection(DOWN)] = true;
					}
					if (extendLeft > 0) {
						externalSide[Transformation.getExternalDirection(LEFT)] = true;
					}
					if (extendRight > 0) {
						externalSide[Transformation.getExternalDirection(RIGHT)] = true;
					}

					if (externalSide[UP] == true) {
						yPositionInSquare = 5;
					}
					if (externalSide[DOWN] == true) {
						yPositionInSquare = skin.graphicHeight - 5;
					}
					if (externalSide[LEFT] == true) {
						xPositionInSquare = 5;
					}
					if (externalSide[RIGHT] == true) {
						xPositionInSquare = skin.graphicWidth - 5;
					}
				}
				if (isBoardToBeExtended == false) {
					// In der Variablen "seiteGelöschtesFeld" ist die interne Seite gespeichert,
					// an der ein Feld gelöscht wurde. Da das Spielfeld transformiert aus-
					// gegeben werden kann, muss hier die Seite auf dem Bildschirm ermittelt
					// werden, die der internen Seite entspricht.
					// Der Offset für die Koordinaten wird dann so gewählt, dass die Maus direkt
					// neben dem nächsten potentiell zu löschenden Feld stehen wird.
					if (externalSideErasedSquare[UP] == true) {
						yPositionInSquare = skin.graphicHeight - 5;
					}
					if (externalSideErasedSquare[DOWN] == true) {
						yPositionInSquare = 5;
					}
					if (externalSideErasedSquare[LEFT] == true) {
						xPositionInSquare = skin.graphicHeight - 5;
					}
					if (externalSideErasedSquare[RIGHT] == true) {
						xPositionInSquare = 5;
					}
				}

				// Koordinaten holen, bei denen das Spielfeld beginnt (inklusive relative Position innerhalb des Feldes)
				boardXCoordinate = boardDisplay.getLocationOnScreen().x + boardDisplay.xOffset + (int) xPositionInSquare;
				boardYCoordinate = boardDisplay.getLocationOnScreen().y + boardDisplay.yOffset + (int) yPositionInSquare;

				// Die Mauskoordinaten in externe Koordinaten umrechnen
				int externalPosition = Transformation.getExternalPosition(mouseXCoordinate + mouseYCoordinate * board.width);
				mouseXCoordinate = externalPosition % Transformation.getOutputLevelWidth();
				mouseYCoordinate = externalPosition / Transformation.getOutputLevelWidth();

				// In a multi-screen environment the coordinates of the current screen have to be taken account to.
				Rectangle screenBounds = application.getGraphicsConfiguration() == null ?  new Rectangle()  :  application.getGraphicsConfiguration().getBounds();

				try {
					new Robot().mouseMove(
							boardXCoordinate + mouseXCoordinate * skin.graphicWidth - screenBounds.x,
							boardYCoordinate + mouseYCoordinate * skin.graphicHeight - screenBounds.y);
				} catch (AWTException e1) {
				}
			}

			return;
		}

		// The mouse click was outside of the board.
		// If one of the selectable elements has been clicked, we recognize that now.
		for (int i = 0; i < 5; i++) {
			// x and y position of the object on the screen (top left corner of the graphic).
			int objectLowX = Settings.OBJECT_XOFFSET;
			int objectLowY = Settings.FIRST_OBJECT_YOFFSET
			               + i * (Settings.OBJECTS_YDISTANCE + skin.graphicHeight);
			final int objectHighX = objectLowX + skin.graphicWidth;
			final int objectHighY = objectLowY + skin.graphicHeight;
			if (x >= objectLowX && x < objectHighX && y >= objectLowY && y < objectHighY) {
				currentlyMarkedObject = i;
				application.redraw(false);

				return;
			}
		}
	}

	/**
	 * Puts (a copy of) the currently selected object to the specified board location.
	 *
	 * @param objectToBeSet the board object to be set
	 * @param xCoordinate the x coordinate of the position
	 * @param yCoordinate the y coordinate of the position
	 */
	final private void setObject(int objectToBeSet, int xCoordinate, int yCoordinate) {

		// Die übergebenen Koordinaten in eine Position umrechnen.
		int position = xCoordinate + board.width * yCoordinate;

		switch (objectToBeSet) {

		// Eine Mauer an der geklickten Stelle setzen, falls noch keine vorhanden war
		// (ansonsten würden immer weitere Mauern auf dieses Feld gesetzt! (2. Mauer, ...)
		case WALL:
			if (board.isWall(position) == false) {
				board.setWall(position);
			}
			board.removeBox(position);
			board.removeGoal(position);
			break;

		// Eine Kiste an der geklickten Spielfeldposition setzen.
		// Falls sich bereits eine Kiste auf dem Feld befindet wird ein Zielfeld gesetzt.
		// Falls sich bereits eine Kiste auf einem Zielfeld auf dem Feld befindet,
		// so wird das Zielfeld entfernt.
		case BOX:
			if (board.isBox(position)) {
				if (board.isGoal(position)) {
					board.removeGoal(position);
				} else {
					board.setGoal(position);
				}
			} else {
				board.setBox(position);
			}

			board.removeWall(position);
			break;

		// Ein Zielfeld an der geklickten Spielfeldposition setzen.
		// Falls sich bereits ein Zielfeld auf dem Feld befindet wird eine Kiste gesetzt.
		// Falls sich bereits eine Kiste auf einem Zielfeld auf dem Feld befindet,
		// so wird die Kiste entfernt.
		case GOAL:
			if (board.isGoal(position)) {
				if (board.isBox(position)) {
					board.removeBox(position);
				} else {
					board.setBox(position);
				}
			} else {
				board.setGoal(position);
			}
			board.removeWall(position);
			break;

		// Spieler an die geklickte Position setzen und alle anderen Objekte auf diesem
		// Feld löschen.
		case PLAYER:
			board.setPlayerPosition(position);
			board.removeBox(position);
			board.removeWall(position);
			break;

		// Freies Feld an die geklickte Stelle setzen (= alle anderen Objekte löschen)
		case EMPTY_SQUARE:
			board.removeBox(position);
			board.removeWall(position);
			board.removeGoal(position);
			break;
		}

		// Check if the position is still accessible for the player.
		if (board.isPlayerInLevel() && board.isAccessible(board.playerPosition) == false) {
			board.removePlayer();
		}
	}

	/**
	 * Returns the number of the currently selected object.
	 *
	 * @return number of the currently selected obj
	 */
	final public int getNumberOfSelectedObject() {
		return currentlyMarkedObject;
	}

	/**
	 * Using the mouse wheel is interpreted as a move through the selectable objects.
	 *
	 * @param evt the <code>MouseWheelEvent</code> fired
	 */
	@Override
	final public void mouseWheelMoved(MouseWheelEvent evt) {

		int scrollDirection = evt.getWheelRotation();

		// Scrollen nach oben
		if (scrollDirection < 0 && currentlyMarkedObject > 0) {
			currentlyMarkedObject--;
		}

		// Scrollen nach unten
		if (scrollDirection > 0 && currentlyMarkedObject < 4) {
			currentlyMarkedObject++;
		}

		application.redraw(false);
	}

	/**
	 * This method is called when the editor is activated in a new level.
	 */
	final public void newLevel() {

		board        = application.board;
		boardDisplay = application.applicationGUI.mainBoardDisplay;

		// Since during editor mode the deadlock squares aren't recomputed each time,
		// they also cannot be displayed.
		Settings.showDeadlockFields               = false;
		Debug.debugShowAdvancedSimpleDeadlocks = false;
		Debug.debugShowSimpleDeadlocksForward  = false;
		Debug.debugShowSimpleDeadlocksBackward = false;

		// Da die Editorobjekte Platz vom Spielfeld benötigen müssen die Grafikgrößen
		// bei der nächsten Ausgabe neu berechnet werden.
		application.applicationGUI.mainBoardDisplay.recalculateGraphicSizes();

		/*
		 * Wird der Editor aktiviert, so wird das Spielfeld an allen Seiten um ein Feld
		 * erweitert, damit der Spieler es automatisch erweitern kann (durch Klick neben
		 * das Spielfeld)
		 */
		// Increase the board
		board.extendBoardSize(1, 1, 1, 1);

		// Determine optimal size of the level
		board.optimizeBoardSizeForEditor(application.applicationGUI);

		// Check the validity of the level.  We use this only to calculate the
		// reachable squares of the player.
		application.isLevelValid();

		// Neu Ermitteln, welche Grafik an welcher Position ausgegeben
		// werden muss. Anschließend wird das modifizierte Level ausgegeben.
		boardDisplay.setBoardToDisplay(board);

		// Up to now we have not changed anything ==> no saving necessary
		hasBoardBeenChanged = false;
	}
}