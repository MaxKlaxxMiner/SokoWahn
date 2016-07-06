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
package de.sokoban_online.jsoko.optimizer.GUI;

import java.awt.Color;
import java.awt.Graphics2D;
import java.awt.Point;
import java.awt.event.KeyAdapter;
import java.awt.event.KeyEvent;
import java.awt.event.MouseEvent;
import java.awt.image.BufferedImage;
import java.awt.image.RescaleOp;
import java.util.ArrayList;
import java.util.Arrays;

import de.sokoban_online.jsoko.gui.BoardDisplay;
import de.sokoban_online.jsoko.leveldata.Level;
import de.sokoban_online.jsoko.resourceHandling.Settings;


/**
 * Class for displaying the board in the optimizer.
 * <p>
 * This class extends the class {@link BoardDisplay} by adding the possibility for marking squares.
 */
@SuppressWarnings("serial")
public class OptimizerBoardDisplay extends BoardDisplay {

	/**
	 * The squares that have been marked by the user.
	 * The optimizer will only use these marked squares as squares for repositioning boxes
	 * in the generation phase.
	 * Note: also walls and "not active squares (never reachable for the player)" are marked
	 * internally, but they aren't highlighted when painted to the screen.
	 * If the optimizer changes the board (for instance by calling "setBoardToDisplay")
	 * because the user has restricted the pushes range to be optimized the marked squares aren't deleted!
	 * This ensures that all marked squares "survive" when the user later sets back the setting
	 * "optimize whole solution" instead of only a specific pushes range.
	 */
	private boolean[] markedSquares = null;

	/** Indicating whether the CRTL-key has been pressed while using the mouse. */
	private boolean isRectangleMarkingModeActivated = false;

	/** Start position of marking when using a rectangle for marking. */
	private Point startDragPosition = null;
	/** End position of marking when using a rectangle for marking. */
	private Point endDragPosition   = null;

	/** Flag indicating whether a "mouse released" event should be discarded. */
	private boolean isMouseReleasedEventCancelled = false;

	/** An image of the current screen for buffering the current screen. */
	private BufferedImage screenCopy = null;

	/** If some squares are marked to be "relevant" for optimizing the other area is drawn with this color on top of it. */
	private final Color inactiveAreaColor;

	/** Color of the area that is selected for marking it. */
	private final Color markingAreaColor;

	/** Factor the marked area is drawn brighter. */
	private final float markedAreaBrightnessFactor;


	/**
	 * Creates a new object for displaying the board in the optimizer.
	 */
	public OptimizerBoardDisplay() {
		this(null);
	}

	/**
	 * Creates a new object for displaying the board of the passed level in the optimizer.
	 *
	 * @param level the <code>Level</code> to be displayed
	 */
	public OptimizerBoardDisplay(Level level) {
		super(level);
		addKeyListener(new KeyEventHandler());

		// Read the settings from the settings.ini file.
		inactiveAreaColor 			= Settings.getColor("inactiveAreaColor", new Color(20, 20, 20, 80));
		markingAreaColor  			= Settings.getColor("markingAreaColor",  new Color(0xBF, 0xEF, 0xFF, 135));
		markedAreaBrightnessFactor  = Settings.getFloat("markedAreaBrightnessFactor", 1.2f);
	}


	@Override
	public void mousePressed(MouseEvent e) {

		// Pressing the mouse initializes the flag. The user may have released the mouse
		// on another GUI element with the result that the "released" event isn't fired
		// at this JPanel.
		isMouseReleasedEventCancelled = false;

		// Request the focus for key events.
		requestFocusInWindow();

		super.mousePressed(e);
	}

	@Override
	public void mouseReleased(MouseEvent e) {

		// If the event is to be discarded then just return.
		if( isMouseReleasedEventCancelled == true) {
			isMouseReleasedEventCancelled =  false;
			return;
		}

		Point clickCoordinates = e.getPoint();

		// Releasing the mouse outside the board while dragging is treated as releasing it
		// at the border of the board.
		if(isRectangleMarkingModeActivated == true) {
			adjustToBoard(clickCoordinates);
		}

		// Get the board position the mouse has been released at.
		int position = getBoardPosition(clickCoordinates.x, clickCoordinates.y);

		// Call the method handling mouse dragged events if the mouse has been dragged in the board.
		if(position >= 0) {
			mouseReleasedAt(Transformation.getInternalPosition(position), e);
		}
	}

    @Override
	protected void mouseReleasedAt(int position, MouseEvent e) {

		if( markedSquares == null) {
			markedSquares =  new boolean[board.width * board.height];
		}

		// Check whether the user has used the rectangle mode for selecting the area to be optimized.
		if( isRectangleMarkingModeActivated == true ) {
			isRectangleMarkingModeActivated = false;

			// Mark all squares that are in the rectangle drawn by the user (or remove
			// the marking if the user hasn't used the first mouse button or pressed
			// the CTRL-key while pressing the mouse button).
			ArrayList<Integer> selectedSquares = getBoardPositions(startDragPosition, endDragPosition);
			for(int positionToMark : selectedSquares) {
				if(graphicStatus[positionToMark] == GRAPHIC_DYNAMIC) {  // Only mark positions that are to be drawn as marked
					markedSquares[positionToMark] = e.getButton() == MouseEvent.BUTTON1 && e.isControlDown() == false;
				}
			}

			repaint();
		} else {
			// Mark the clicked square. If another mouse button than the first
			// is used or the CTRL-key is down the square marking is removed.
			if(graphicStatus[position] == GRAPHIC_DYNAMIC) {			// Only mark positions that are to be drawn as marked
				markedSquares[position] = e.getButton() == MouseEvent.BUTTON1 && e.isControlDown() == false;
				repaint();
			}
		}
	}

	@Override
	public void mouseDragged(MouseEvent e) {

		Point p = e.getPoint();

		// If the drag is outside the board the mouse position is treated as being at the edge of the board.
		adjustToBoard(p);

		// Get the board position the mouse has been dragged at.
		int position = getBoardPosition(p.x, p.y);

		// Call the method handling mouse dragged events if the mouse has been dragged in the board.
		if(position >= 0) {
			mouseDraggedAt(Transformation.getInternalPosition(position), e);
		}
	}

	@Override
	protected void mouseDraggedAt(int position, MouseEvent e) {

		if( markedSquares == null ) {
			markedSquares = new boolean[board.width * board.height];
		}

		// If the marking mode isn't activated yet then create a copy of the current screen.
		// This increases the performance because the marked area can be drawn on top of
		// this copy instead of drawing the whole panel every time the marked area changes.
		if(isRectangleMarkingModeActivated == false) {
			if( screenCopy == null || screenCopy.getWidth() != fixedGraphicsImage.getWidth() || screenCopy.getHeight() != fixedGraphicsImage.getHeight()) {
				screenCopy = new BufferedImage(fixedGraphicsImage.getWidth(), fixedGraphicsImage.getHeight(), BufferedImage.TYPE_INT_ARGB);
			}
			paint(screenCopy.createGraphics());

			// Save the start position of the drag and activate the marking mode.
			isRectangleMarkingModeActivated = true;
			startDragPosition = e.getPoint();
		}

		// The current position is the new end position of the drag.
		endDragPosition = e.getPoint();

		repaint();
	}

	/**
	 * Returns an array of booleans indicating which squares have been marked by the user.
	 *
	 * @return an <code>boolean</code> array indicating which squares have been marked
	 */
	public boolean[] getMarkedSquares() {

		boolean[] markedSquaresTmp = new boolean[board.width * board.height];
		boolean isAtLeastOneSquareMarked = false;

		// The marked squares stay marked even when a new board is set! This way the user may restrict
		// the optimizer to only optimize a specific pushes range (which sets a new board from this pushes
		// range) and later setting back the optimizer to "optimize whole solution" instead of a pushes range
		// without loosing the marked squares.
		// Since the board may have been changed in the meantime (due to setting a pushes range some squares
		// may have become walls) every position has to be checked to be an active level element that is:
		// is is a "dynamic" graphic. This is important because otherwise a previously marked box that now
		// (due to a selected pushes range) may has become a wall is still marked but displayed as a wall
		// (which is never displayed "marked"/highlighted.
		if(markedSquares != null) {
			for(int position=0; position<markedSquares.length;position++) {
				if(markedSquares[position] == true && graphicStatus[position] == GRAPHIC_DYNAMIC) {
					markedSquaresTmp[position] = true;
					isAtLeastOneSquareMarked = true;
				}
			}
		}

		// If none of the squares have been marked this means the user hasn't restricted the
		// area to be optimized => return all squares as marked.
		if(isAtLeastOneSquareMarked == false) {
			Arrays.fill(markedSquaresTmp, true);
		}
		return markedSquaresTmp;
	}

	/**
	 * Removes all markings of squares.
	 */
	public void removeAllMarkings() {
		markedSquares = null;
	}

	/**
	 * This method draws the board elements (walls, boxes, ...)
	 * to the passed graphic context.
	 *
	 * @param g2D graphic context
	 */
	@Override
	protected void drawBoard(Graphics2D g2D) {

		// x and y coordinate of a position.
		int xCoordinate;
		int yCoordinate;

		// ResacleOp for brightness control.
		final RescaleOp rescaleBright = new RescaleOp(markedAreaBrightnessFactor, 0.0f, null);

		boolean isASquareMarked = false;
		if(markedSquares != null) {
			for(boolean marked : markedSquares) {
				if(marked) {
					isASquareMarked = true;
					break;
				}
			}
		}

		// Display the graphic of all graphics which never change during game play.
		if(isASquareMarked) {
			BufferedImage bi = new BufferedImage(fixedGraphicsImage.getWidth(), fixedGraphicsImage.getHeight(), BufferedImage.TYPE_INT_RGB);
			Graphics2D biContext = bi.createGraphics();
			biContext.drawImage(fixedGraphicsImage, 0, 0, null);
			biContext.setColor(inactiveAreaColor);
			biContext.fillRect(0,0, fixedGraphicsImage.getWidth(), fixedGraphicsImage.getHeight());
			g2D.drawImage(bi, 0, 0, this);
		}
		else {
			g2D.drawImage(fixedGraphicsImage, 0, 0, this);
		}

		// Graphic to be drawn.
		BufferedImage graphicToDraw = null;

		// If the marking mode is activated all that could have changed is the marking area.
		// Hence, just draw it.
		if(isRectangleMarkingModeActivated) {

			// Draw background.
			g2D.drawImage(screenCopy, 0, 0, null);

			g2D.setColor(markingAreaColor);
			g2D.fillRect(Math.min(startDragPosition.x, endDragPosition.x), Math.min(startDragPosition.y, endDragPosition.y),
					     Math.abs(endDragPosition.x - startDragPosition.x),
					     Math.abs(endDragPosition.y - startDragPosition.y));

			return;
		}

		// The proper graphic for every square of the board is drawn.
		for (int externalPosition = board.size; --externalPosition != -1;) {

			// Calculate the internal position.
			int position = Transformation.getInternalPosition(externalPosition);

			// Only player reachable squares must be redrawn.
			if(graphicStatus[position] != GRAPHIC_DYNAMIC) {
				continue;
			}

			// Empty square is the default.
			graphicToDraw = skin.emptySquare;

			// Goal
			if (board.isGoal(position)) {
				graphicToDraw = skin.goal;
			}

			// Box / box on goal.
			if (board.isBox(position)) {
				graphicToDraw = skin.box;
			}

			if(board.isBoxOnGoal(position)) {
				graphicToDraw = skin.boxOnGoal;
			}

			if(board.playerPosition == position) {
				final int externalDirection = Transformation.getExternalDirection(viewDirection);
				if(board.isGoal(position)) {
					graphicToDraw = skin.playerOnGoalWithViewInDirection[externalDirection];
				} else {
					graphicToDraw = skin.playerWithViewInDirection[externalDirection];
				}
			}

			// Get the x- and y-coordinate of the external position.
			xCoordinate = externalPosition % Transformation.getOutputLevelWidth();
			yCoordinate = externalPosition / Transformation.getOutputLevelWidth();

			// Highlight the graphic if it is marked.
			if(isASquareMarked) {

				if(markedSquares[position] == true) {

					// Some skins have transparent backgrounds for the player and box graphics. However, the whole square should
					// be highlighted when selected. Therefore the empty square graphic is drawn as background first.
					BufferedImage bi = new BufferedImage(skin.graphicWidth, skin.graphicHeight, BufferedImage.TYPE_INT_RGB);
					Graphics2D biContext = bi.createGraphics();
					biContext.drawImage(skin.emptySquare, 0, 0, null);
					biContext.drawImage(graphicToDraw, 0, 0, null);
					graphicToDraw = rescaleBright.filter(bi, null);
				}
				else {
					BufferedImage bi = new BufferedImage(skin.graphicWidth, skin.graphicHeight, BufferedImage.TYPE_INT_RGB);
					Graphics2D biContext = bi.createGraphics();
					biContext.drawImage(skin.emptySquare, 0, 0, null);
					biContext.drawImage(graphicToDraw, 0, 0, null);
					biContext.setColor(inactiveAreaColor);
					biContext.fillRect(0,0, skin.graphicWidth, skin.graphicHeight);
					graphicToDraw = bi;
				}
			}

			// Draw the graphic to the screen.
			g2D.drawImage(graphicToDraw, xCoordinate * skin.graphicWidth + xOffset, yCoordinate * skin.graphicHeight + yOffset, this);
		}

	}

	/**
	 * Handles all key event for class {@link OptimizerBoardDisplay}.
	 */
	private class KeyEventHandler extends KeyAdapter {

		@Override
		public void keyTyped(KeyEvent e) {

			// Pressing the escape key cancels the rectangle selection.
			if(e.getKeyChar() == KeyEvent.VK_ESCAPE && isRectangleMarkingModeActivated == true) {
				isRectangleMarkingModeActivated = false;

				// The user must still release the mouse button to finish the drag. This action must be ignored.
				isMouseReleasedEventCancelled = true;

				repaint();
			}
		}
	}

}