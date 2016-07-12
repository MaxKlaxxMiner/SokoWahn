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

import java.awt.Graphics;
import java.awt.Graphics2D;
import java.awt.Point;
import java.awt.event.MouseEvent;
import java.awt.event.MouseListener;
import java.awt.event.MouseMotionListener;
import java.awt.image.BufferedImage;
import java.io.FileNotFoundException;
import java.util.ArrayList;
import java.util.HashMap;

import javax.swing.JPanel;

import de.sokoban_online.jsoko.board.Board;
import de.sokoban_online.jsoko.board.DirectionConstants;
import de.sokoban_online.jsoko.leveldata.Level;
import de.sokoban_online.jsoko.resourceHandling.Settings;
import de.sokoban_online.jsoko.utilities.GraphicUtilities;
import de.sokoban_online.jsoko.utilities.IntStack;
import de.sokoban_online.jsoko.utilities.Utilities;


/**
 * Instances of this class paint a board in a JPanel.
 * <p>
 * Since this board is much simpler than the {@link MainBoardDisplay} this implementation
 * is independent.
 */
@SuppressWarnings("serial")
public class BoardDisplay extends JPanel implements MouseListener, MouseMotionListener {

	/** The board containing the data to draw. */
	protected Board board;

	/** x offset of the display of the board in the panel. */
	protected int xOffset = 0;

	/** y offset of the display of the board in the panel. */
	protected int yOffset = 0;

	/**
	 * Note: this is a fake Transformation object because the real Transformation class can't be used at
	 * at the moment. This is just in order to make this class work.
	 */
	protected DummyTransformation Transformation = new DummyTransformation();

	/** Factor for scaling the board graphics. */
	protected float scalingFactor = 0;

	// Graphics of the current skin to be shown on the screen. Each time the size of this panel changes or
	// the level size changes these graphics are scaled to fit into the panel. To ensure best quality when
	// scaling a skin object holding the original size graphics is used for scaling.
	protected Skin skin;
	protected Skin originalSkin;

	// Image which contains all graphics which don't change during game play. These graphics are all drawn to this single
	// image in order to draw just this single image every time the GUI has to be redrawn => better performance.
	protected BufferedImage fixedGraphicsImage;

	// The graphics (of the board) are computed at runtime:
	// a different graphic is drawn when the location is occupied by a player or a box.
	protected final byte GRAPHIC_NO_GRAPHIC    = -1;
	protected final byte GRAPHIC_DYNAMIC       =  0;
	protected final byte BEAUTY_GRAPHIC        =  1;
	protected final byte GRAPHIC_FIXED_GRAPHIC =  2;

	protected byte[] graphicStatus;

	// Width and height of the panel the graphics are drawn to.
	protected int currentWindowWidth  = -1;
	protected int currentWindowHeight = -1;

	// The background image as loaded (= original size) and scaled version (to fit into this JPanel).
	protected BufferedImage backgroundGraphicOriginalSize;
	protected BufferedImage backgroundGraphic;

	/** View direction of the player. */
	protected byte viewDirection = 0;

	/**
	 * Flag, indicating whether a complete recalculation of
	 * the graphic sizes has to be done.
	 */
	protected boolean isRecalculationNecessary = false;

	/** The level that is displayed by this BoardDisplay */
	protected Level displayedLevel = null;

	// Caches of the skin and the background graphic.
	private static final HashMap<String, Skin> skinCache = new HashMap<String, Skin>(1);
	private static final HashMap<String, BufferedImage> backgroundGraphicCache = new HashMap<String, BufferedImage>(1);

	/**
	 * Creates a new object for displaying the board.
	 */
	public BoardDisplay() {
		this(null);
	}

	/**
	 * Creates a new object for displaying the board of the passed level.
	 *
	 * @param levelToBeDisplayed the <code>Level</code> to be displayed
	 */
	public BoardDisplay(Level levelToBeDisplayed) {

		// Set the current skin.
		setSkin(Settings.currentSkin);

		// Set an empty board as initial board.
		board = new Board();

		// Handle mouse events in the class.
		addMouseListener(this);
		addMouseMotionListener(this);

		if(levelToBeDisplayed != null) {
			setLevelToDisplay(levelToBeDisplayed);
		}
	}

	/**
	 * Main paint method of this panel.
	 *
	 *@param  graphic graphic context
	 */
	@Override
	protected void paintComponent(Graphics graphic) {

		// If there isn't a board set to draw, yet, nothing can be drawn.
		if (board == null) {
			return;
		}

		// Cast to Graphics2D to be able to use the more sophisticated methods of it.
		Graphics2D g2D = (Graphics2D) graphic;

		// The panel must have a minimum of size for drawing.
		if (getWidth() < 50 || getHeight() < 50) {
			setSize(50, 50);
		}

		// Calculate new graphic sizes if the window size has changed or a recalculation is requested.
		if (currentWindowWidth != getWidth() || currentWindowHeight != getHeight() || isRecalculationNecessary) {
			newGraphicSizeCalculation(g2D);
		}

		// Draw the board.
		drawBoard(g2D);
	}

	/**
	 * Every time the size of the window changes or important changes
	 * of the GUI are to be drawn, the sizes of the GUI elements
	 * have to be recalculated.
	 *
	 * @param g2D graphic context
	 */
	protected void newGraphicSizeCalculation(Graphics2D g2D) {

		// Set flag to false because the new sizes are calculated now.
		isRecalculationNecessary = false;

		// Get current size of the window.
		currentWindowWidth  = getWidth();
		currentWindowHeight = getHeight();

		// Scale the background image to the window size.
		if (backgroundGraphic != null && (backgroundGraphic.getWidth(this)  != currentWindowWidth ||
								          backgroundGraphic.getHeight(this) != currentWindowHeight)) {
			// Try to load the graphic having the proper size from the cache. The graphical level browser
			// contains many objects of this class. Hence, this improves the performance.
			backgroundGraphic = backgroundGraphicCache.get(Integer.toString((currentWindowWidth << 16) + currentWindowHeight));

			if(backgroundGraphic == null) {
				backgroundGraphic = GraphicUtilities.getScaledInstance(backgroundGraphicOriginalSize, currentWindowWidth, currentWindowHeight);
				backgroundGraphicCache.clear(); // don't let the cache grow
				backgroundGraphicCache.put(Integer.toString((currentWindowWidth << 16) + currentWindowHeight), backgroundGraphic);
			}
		}

		// Calculate a proper scaling factor.
		float widthScalingFactor  = currentWindowWidth  / (float) board.width  / originalSkin.graphicWidth;
		float heightScalingFactor = currentWindowHeight / (float) board.height / originalSkin.graphicHeight;

		// Width and height have to be scaled using the same factor.
		// Therefore choose the smaller one, i.e. the minimum of both.
		scalingFactor = Math.min(widthScalingFactor, heightScalingFactor);

		// Don't scale up.
		//scalingFactor = Math.min(scalingFactor, 1.0f);
		if (scalingFactor > 1) {
			scalingFactor = 1;
		}

		// Calculate new graphic width and height.
		int scaledGraphicsWidth  = (int) (originalSkin.graphicWidth  * scalingFactor);
		int scaledGraphicsHeight = (int) (originalSkin.graphicHeight * scalingFactor);

		// Get a scaled version of the skin.
		skin = skinCache.get(Integer.toString((scaledGraphicsWidth << 16) + scaledGraphicsHeight));
		if(skin == null) {
			skin = originalSkin.getScaledVersion(scaledGraphicsWidth, scaledGraphicsHeight);
			if(skinCache.size() > 10) {
				// max 10 objects to ensure low memory usage FFS: use soft reference cache
				skinCache.clear();
			}
			skinCache.put(Integer.toString((scaledGraphicsWidth << 16) + scaledGraphicsHeight), skin);
		}

		// Calculate an offset so the board is drawn centered.
		xOffset = (currentWindowWidth  - board.width  * scaledGraphicsWidth ) / 2;
		yOffset = (currentWindowHeight - board.height * scaledGraphicsHeight) / 2;

		// Get the image containing all graphics that don't change during game play.
		fixedGraphicsImage = getImageOfFixedGraphics();
	}


	/**
	 * This method draws the board elements (walls, boxes, ...)
	 * to the passed graphic context.
	 *
	 * @param g2D graphic context
	 */
	protected void drawBoard(Graphics2D g2D) {

		// x and y coordinate of a position.
		int xCoordinate;
		int yCoordinate;

		// Display the graphic of all graphics which never change during game play.
		g2D.drawImage(fixedGraphicsImage, 0, 0, this);

		// Graphic to be drawn.
		BufferedImage graphicToDraw = null;

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
				if(board.isGoal(position)) {
					graphicToDraw = skin.playerOnGoalWithViewInDirection[Transformation.getExternalDirection(viewDirection)];
				} else {
					graphicToDraw = skin.playerWithViewInDirection[Transformation.getExternalDirection(viewDirection)];
				}
			}

			// Get the x- and y-coordinate of the external position.
			xCoordinate = externalPosition % Transformation.getOutputLevelWidth();
			yCoordinate = externalPosition / Transformation.getOutputLevelWidth();

			// Draw the graphic to the screen.
			g2D.drawImage( graphicToDraw,
					       xCoordinate * skin.graphicWidth  + xOffset,
					       yCoordinate * skin.graphicHeight + yOffset,
					       this);
		}
	}

	/**
	 * Returns a <code>BufferedImage</code> of the all fixed graphics.
	 * <p>
	 * The background image, the walls of the level and other elements to be
	 * drawn never change during the game. These elements are drawn to one
	 * image which is shown every time the GUI must be repainted.
	 * This way only "dynamic" elements which change during the game
	 * must be redrawn on top of this image.
	 *
	 * @return the image of all fixed graphics
	 */
	protected BufferedImage getImageOfFixedGraphics() {

		// Position on the board.
		int position = -1;

		// Board width and height for easier access.
		int boardWidth  = board.width;
		int boardHeight = board.height;

		// Array for determining the wall graphic to be shown at a specific position.
		// The graphics are ordered in a specific way to enable easy
		// determination of the graphic to be drawn.
		BufferedImage[] wallGraphics = new BufferedImage[] {
				skin.wall_no_neighbor,						//  0
				skin.wall_neighbor_left,					//  1
				skin.wall_neighbor_right,					//  2
				skin.wall_neighbor_left_right,				//  3
				skin.wall_neighbor_above,					//  4
				skin.wall_neighbor_above_left,				//  5
				skin.wall_neighbor_above_right,				//  6
				skin.wall_neighbor_above_left_right,		//  7
				skin.wall_neighbor_below,					//  8
				skin.wall_neighbor_below_left,				//  9
				skin.wall_neighbor_below_right,				// 10
				skin.wall_neighbor_below_left_right,		// 11
				skin.wall_neighbor_above_below,				// 12
				skin.wall_neighbor_above_below_left,		// 13
				skin.wall_neighbor_above_below_right,		// 14
				skin.wall_neighbor_above_below_left_right}; // 15

		// Create a new buffered image for the fixed graphics.
		BufferedImage fixedGraphicsImage = new BufferedImage(currentWindowWidth, currentWindowHeight, BufferedImage.TYPE_INT_ARGB);
		Graphics2D g2D = fixedGraphicsImage.createGraphics();

		// Copy the background image into the image.
		if(backgroundGraphic != null && skin.isBackgroundImageSupported) {
			g2D.drawImage(backgroundGraphic, 0, 0, null);
		} else {
			// The empty square graphic is used as background.
			for (int x2 = 0; x2 < getWidth(); x2 += skin.graphicWidth) {
				for (int y2 = 0; y2 < getHeight(); y2 += skin.graphicHeight) {
					g2D.drawImage(skin.emptySquare, x2, y2, this);
				}
			}
		}

		// Empty squares are drawn when the player can reach a box or a goal from them.
		// In order to be able to correctly display levels like Sasquatch 41 the player is
		// placed to every goal and every box position and then the reachable squares of the
		// player are determined.
		boolean[] changableSquare = identifyInnerBoardSquares();

		// Check for every position which graphic has to be drawn.
		for (int externalPosition = 0; externalPosition < board.width*board.height; externalPosition++) {

			// Get the internal position.
			position = Transformation.getInternalPosition(externalPosition);

			// Split the external position in x- and y-coordinate.
			final int externalXCoordinate = externalPosition % Transformation.getOutputLevelWidth();
			final int externalYCoordinate = externalPosition / Transformation.getOutputLevelWidth();

			final int squareX = externalXCoordinate * skin.graphicWidth  + xOffset;
			final int squareY = externalYCoordinate * skin.graphicHeight + yOffset;

			// Initialize the graphic status for every position.
			graphicStatus[position] = GRAPHIC_NO_GRAPHIC;

			// If the player can reach the position the graphic to be drawn can't be
			// precalculated. Nevertheless, because some skins have a transparent graphic
			// for the player / for the box, the empty square / goal graphic has to be
			// drawn on every square.
			if (changableSquare[position] == true) {
				graphicStatus[position] = GRAPHIC_DYNAMIC;

				// Draw a goal.
				if (board.isGoal(position)) {
					g2D.drawImage(skin.goal, squareX, squareY, this);
					continue;
				}

				// Draw an empty square.
				g2D.drawImage(skin.emptySquare, squareX, squareY, this);

				continue;
			}

			// If it's a wall then the graphic never changes during game play.
			if(board.isWall(position)) {
				graphicStatus[position] = GRAPHIC_FIXED_GRAPHIC;
				continue;
			}

			// Empty squares that are special squares are drawn as empty square, other empty squares aren't drawn.
			// (Note: player reachable empty squares are already handled in the condition above).
			if (board.isEmptySquare(position)) {
				if (changableSquare[position]) {
					g2D.drawImage(skin.emptySquare, squareX, squareY, this);
				}
				continue;
			}

			if (board.isBoxOnGoal(position)) {
				g2D.drawImage(skin.boxOnGoal, squareX, squareY, this);
				continue;
			}

			if (board.isBox(position)) {
				g2D.drawImage(skin.box, squareX, squareY, this);
				continue;
			}

			if (board.isGoal(position)) {
				g2D.drawImage(skin.goal, squareX, squareY, this);
				continue;
			}
		}

		// Draw the walls if they are to be drawn.
		if(Settings.getBool("showWalls")) {

			// Extra loop for the walls. The board might be shown transformed.
			// Hence, loop over the !!external!! board representation to determine
			// which wall has to be shown at which position. Hence, all positions
			// are external and transformed to an internal position where necessary.
			boardWidth  = Transformation.getOutputLevelWidth();
			boardHeight = Transformation.getOutputLevelHeight();
			int leftCutPixel = 0;
			int rightCutPixel = 0;
			int aboveCutPixel = 0;
			int belowCutPixel = 0;

			boolean isNoGraphicLeftAbove  = false;
			boolean isNoGraphicRightAbove = false;
			boolean isNoGraphicLeftBelow  = false;
			boolean isNoGraphicRightBelow = false;
			boolean isRelevantCornerWall  = false;

			// x and y coordinates of the destination area a graphic is to be drawn to.
			int destinationAreaX1 = -1;
			int destinationAreaY1 = -1;
			int destinationAreaX2 = -1;
			int destinationAreaY2 = -1;

			for(int y=0; y<boardHeight; y++) {
				for(int x=0; x<boardWidth; x++) {

					// Calculate the internal and external position.
					position = y*boardWidth + x;

					// Jump over all squares not a wall.
					if (!board.isWall(Transformation.getInternalPosition(position))) {
						continue;
					}

					// Calculate the coordinates of the rectangle the graphic is to be drawn to.
					destinationAreaX1 = x * skin.graphicWidth  + xOffset;						   // top left corner of the destination area
					destinationAreaY1 = y * skin.graphicHeight + yOffset;						   // top left corner of the destination area
					destinationAreaX2 = x * skin.graphicWidth  + xOffset + skin.graphicWidth;  // bottom right corner of the destination area
					destinationAreaY2 = y * skin.graphicHeight + yOffset + skin.graphicHeight; // bottom right corner of the destination area

					// Number of the graphic in the permutation array to be drawn.
					int wallValue = 0;

					// Add 1 if there is a wall to the left.
					if (x > 0 && board.isWall(Transformation.getInternalPosition(position - 1))) {
						wallValue |= 1;
					}

					// Add 2 if there is a wall to the right.
					if (x < boardWidth - 1 && board.isWall(Transformation.getInternalPosition(position + 1))) {
						wallValue |= 2;
					}

					// Add 4 if there is a wall above.
					if (y > 0 && board.isWall(Transformation.getInternalPosition(position-boardWidth))) {
						wallValue |= 4;
					}

					// Add 8 if there is a wall below.
					if (y < boardHeight - 1 && board.isWall(Transformation.getInternalPosition(position+boardWidth))) {
						wallValue |= 8;
					}


					// Initialize the areas to cut from the image.
					leftCutPixel = rightCutPixel = aboveCutPixel = belowCutPixel = 0;

					/**
					 * If there isn't any level element at a specific side,
					 * cut some pixels of the image have to be cut at that side.
					 */
					if (x == 0 || graphicStatus[Transformation.getInternalPosition(position-1)] == GRAPHIC_NO_GRAPHIC) {
						leftCutPixel = skin.leftBorder;
					}

					if (x == boardWidth - 1 || graphicStatus[Transformation.getInternalPosition(position+1)] == GRAPHIC_NO_GRAPHIC) {
						rightCutPixel = skin.rightBorder;
					}

					if (y == 0 || graphicStatus[Transformation.getInternalPosition(position-boardWidth)] == GRAPHIC_NO_GRAPHIC) {
						aboveCutPixel = skin.aboveBorder;
					}

					if (y == boardHeight - 1 || graphicStatus[Transformation.getInternalPosition(position+boardWidth)] == GRAPHIC_NO_GRAPHIC) {
						belowCutPixel = skin.belowBorder;
					}

					/**
					 * Special treatment for graphics that represent a corner. The little
					 * square in the corner is drawn as background.
					 * However, it mustn't be drawn (so the background remains shown) in
					 * the case that the adjacent graphic is a "no graphic".
					 * Therefore some of the walls are always drawn "cut" and then it is
					 * checked whether parts of the graphic have to be drawn additionally
					 * because there isn't any "no graphic" in the neighborhood.
					 * Therefore: check for "no graphic" left-above, right-above,
					 * left-below and right-below.
					 *
					 *     c%c
					 *     %%%    a graphic split into 9 sub images.
					 *     c%c
					 *
					 * c = corners that are only drawn when no "no graphic" square is in
					 * the neighborhood. To to this only the middle part of the graphic
					 * is drawn. The four other parts are drawn later in the coding.
					 */
					// Initialize the flags.
					isNoGraphicLeftAbove  = false;
					isNoGraphicRightAbove = false;
					isNoGraphicLeftBelow  = false;
					isNoGraphicRightBelow = false;
					isRelevantCornerWall  = false;

					if(     wallValue ==  5 ||  // wall_neighbor_above_left
							wallValue ==  6 ||  // wall_neighbor_above_right
							wallValue ==  7 ||  // wall_neighbor_above_left_right
							wallValue ==  9 ||  // wall_neighbor_below_left
							wallValue == 10 ||  // wall_neighbor_below_right
							wallValue == 11 ||  // wall_neighbor_below_left_right
							wallValue == 13 ||  // wall_neighbor_above_below_left
							wallValue == 14 ||  // wall_neighbor_above_below_right
							wallValue == 15)  // wall_neighbor_above_below_left_right
					{
						if(x == 0 || y == 0 || graphicStatus[Transformation.getInternalPosition(position - boardWidth - 1)] == GRAPHIC_NO_GRAPHIC) {
							isNoGraphicLeftAbove = true;
						}

						if(x == boardWidth - 1 || y == 0 || graphicStatus[Transformation.getInternalPosition(position - boardWidth + 1)] == GRAPHIC_NO_GRAPHIC) {
							isNoGraphicRightAbove = true;
						}

						if(x == 0 || y == boardHeight - 1 || graphicStatus[Transformation.getInternalPosition(position + boardWidth - 1)] == GRAPHIC_NO_GRAPHIC) {
							isNoGraphicLeftBelow = true;
						}

						if(x == boardWidth - 1 || y == boardHeight - 1 || graphicStatus[Transformation.getInternalPosition(position + boardWidth + 1)] == GRAPHIC_NO_GRAPHIC) {
							isNoGraphicRightBelow = true;
						}

						// It's a wall containing a corner. However, this is only relevant
						// if one of the diagonal neighbor squares is a "no graphic" square.
						if(isNoGraphicLeftAbove || isNoGraphicRightAbove || isNoGraphicLeftBelow || isNoGraphicRightBelow) {
							isRelevantCornerWall = true;
						}
					}


					/**
					 * Corner walls can't just be drawn to the screen.
					 * They are drawn cut at every side. Hence, there may be
					 * too many cuts and therefore some additionally parts
					 * of the graphic have to be drawn later, in order to
					 * ensure every necessary part is drawn.
					 */
					if(isRelevantCornerWall == true) {

						// Draw the graphic with cuts on every side.
						g2D.drawImage( wallGraphics[wallValue],
								       destinationAreaX1 + skin.leftBorder,
								       destinationAreaY1 + skin.aboveBorder,
								       destinationAreaX2 - skin.rightBorder,
								       destinationAreaY2 - skin.belowBorder,
								       skin.leftBorder,
								       skin.aboveBorder,
								       skin.graphicWidth  - skin.rightBorder,
								       skin.graphicHeight - skin.belowBorder,
								       this );

						// Draw the top of the graphic if there is any other graphic above it.
						if(aboveCutPixel == 0) {
							int leftCut  =  leftCutPixel > 0 || isNoGraphicLeftAbove  == true ? skin.leftBorder  : 0;
							int rightCut = rightCutPixel > 0 || isNoGraphicRightAbove == true ? skin.rightBorder : 0;
							g2D.drawImage(wallGraphics[wallValue],
									destinationAreaX1 + leftCut, destinationAreaY1, destinationAreaX2 - rightCut, destinationAreaY1 + skin.aboveBorder,
									leftCut, 0, skin.graphicWidth - rightCut, skin.aboveBorder, this);
						}

						// Draw the bottom of the graphic if there is any other graphic below it.
						if(belowCutPixel == 0) {
							int leftCut  =  leftCutPixel > 0 || isNoGraphicLeftBelow  == true ? skin.leftBorder  : 0;
							int rightCut = rightCutPixel > 0 || isNoGraphicRightBelow == true ? skin.rightBorder : 0;
							g2D.drawImage(wallGraphics[wallValue],
									destinationAreaX1 + leftCut, destinationAreaY2 - skin.belowBorder, destinationAreaX2 - rightCut,  destinationAreaY2,
									leftCut, skin.graphicHeight - skin.belowBorder, skin.graphicWidth - rightCut, skin.graphicHeight, this);
						}

						// Draw the left side of the graphic if there is any other graphic at the left.
						if(leftCutPixel == 0) {
							g2D.drawImage(wallGraphics[wallValue],
									destinationAreaX1, destinationAreaY1 + skin.aboveBorder, destinationAreaX1 + skin.leftBorder,  destinationAreaY2 - skin.belowBorder,
									0, skin.aboveBorder, skin.leftBorder, skin.graphicHeight - skin.belowBorder, this);
						}

						// Draw the right side of the graphic if there is any other graphic at the right.
						if(rightCutPixel == 0) {
							g2D.drawImage(wallGraphics[wallValue],
									destinationAreaX2 - skin.rightBorder, destinationAreaY1 + skin.aboveBorder, destinationAreaX2,  destinationAreaY2 - skin.belowBorder,
									skin.graphicWidth - skin.rightBorder, skin.aboveBorder, skin.graphicWidth, skin.graphicHeight - skin.belowBorder, this);
						}
					}
					else {
						/**
						 * Draw wall graphic to the screen.
						 */
						g2D.drawImage(wallGraphics[wallValue],
								destinationAreaX1 + leftCutPixel, destinationAreaY1 + aboveCutPixel, destinationAreaX2 - rightCutPixel,  destinationAreaY2 - belowCutPixel,
								leftCutPixel, aboveCutPixel, skin.graphicWidth - rightCutPixel, skin.graphicHeight - belowCutPixel, this);
					}

					/*
					 * If a block of 4 walls has occurred draw a beauty graphic on top of them.
					 *
					 *     ##  <- block of 4 walls
					 *     ##
					 */
					if ((wallValue & 5) == 5 && x > 0 && y > 0 && board.isWall(Transformation.getInternalPosition(position - boardWidth - 1))) {
						final int imgX = destinationAreaX1 - skin.graphicWidth  + skin.beautyGraphicXOffset;
						final int imgY = destinationAreaY1 - skin.graphicHeight + skin.beautyGraphicYOffset;
						g2D.drawImage(skin.wall_beauty_graphic, imgX, imgY, this);
					}
				}
			}
		}

		// Release the system resources.
		g2D.dispose();

		// Return the graphic containing all graphics which won't change during game play.
		return fixedGraphicsImage;
	}

	/**
	 * Loads a new background graphic.
	 *
	 * @param graphicname	Name of the graphic to be loaded
	 * @throws FileNotFoundException file could not be loaded exception
	 */
	final public void loadBackgroundGraphic(String graphicname) throws FileNotFoundException {

		// Get the background graphic from the cache.
		backgroundGraphicOriginalSize = backgroundGraphicCache.get(Settings.get("graphicname"));

		// If it hasn't been cached load it and cache it.
		if(backgroundGraphicOriginalSize == null) {
			backgroundGraphicOriginalSize = Utilities.loadBufferedImage(graphicname);
			backgroundGraphicCache.clear(); // only cache the latest graphic specified in the Settings.
			backgroundGraphicCache.put(Settings.get("graphicname"), backgroundGraphicOriginalSize);
		}

		// Set the new background graphic as current background.
		backgroundGraphic = backgroundGraphicOriginalSize;

		// The new background graphic has to be resized the next time the GUI is displayed.
		isRecalculationNecessary = true;
	}

	/**
	 * Loads the given skin and sets it as active skin.
	 *
	 * @param skinSettingsFile path to the settings file of the skin to be loaded
	 */
	protected void setSkin(String skinSettingsFile) {

		// Get the skin from the cache.
		originalSkin = skinCache.get(skinSettingsFile);

		// If the skin isn't cached then load it and cache it. Only one skin is cached at a time.
		if(originalSkin == null) {
			try {
				originalSkin = new Skin(skinSettingsFile);
			} catch (FileNotFoundException e) {
				// Display a message because the skin couldn't be loaded.
				MessageDialogs.showExceptionError(this, e);
				return;
			}
			catch(Exception e2) {
				// Display a message because the skin couldn't be loaded.
				MessageDialogs.showErrorTextKey(this, "message.skinLoadError");
				return;
			}

			// Remove all previously cached skins.
			skinCache.clear();
			skinCache.put(skinSettingsFile, originalSkin);
		}

		// Set the skin as current skin.
		skin = originalSkin;

		// Load graphic for the background if none has been loaded yet.
		if (backgroundGraphic == null && skin.isBackgroundImageSupported) {
			try {
				loadBackgroundGraphic(Settings.get("backgroundImageFile"));
			} catch (FileNotFoundException e) {
				// Display a message because the image couldn't be loaded.
				MessageDialogs.showExceptionWarning(this, e);
			}
		}

		// The graphic sizes have to be recalculated.
		isRecalculationNecessary = true;
		repaint();
	}


	/**
	 * Sets the flag that a recalculation has to be done.
	 */
	public void recalculateGraphicSizes() {
		isRecalculationNecessary = true;
	}

	/**
	 * Identifies inner board squares and returns this information via a boolean array.
	 * <p>
	 * This information is used to draw proper graphics: empty squares outside the player reachable area
	 * aren't drawn. Hence, the background graphic is visible at these positions.
	 *
	 * @return boolean array containing the information which squares belong to the board
	 */
	protected boolean[] identifyInnerBoardSquares() {

		// true = this square belongs to the board. Hence, there must be drawn a graphic for this square.
		boolean[] innerSquares = new boolean[board.size];

		// Stack for the positions to be analyzed.
		IntStack positionsToBeAnalyzed = new IntStack(board.size);

		// The player is also placed to every goal and every box and the reachable squares
		// are marked. This is done, because some levels have areas outside the reachable
		// area of the player which should be treated as reachable for a better look.
		for (int position = board.size; --position != -1;) {

			// Positions already marked as visited can be jumped over.
			if (innerSquares[position] == true) {
				continue;
			}

			// All goal positions and box positions as well as the player position itself are taken as start position.
			if (board.playerPosition == position || board.isGoal(position) || board.isBox(position)) {

				/*
				 * Now the reachable squares of the player are marked
				 * as visited = marked as inner board squares.
				 */

				// Add the start position to the stack.
				positionsToBeAnalyzed.clear(); // just to be sure it's empty
				positionsToBeAnalyzed.add(position);

				while (!positionsToBeAnalyzed.isEmpty()) {
					int playerPosition = positionsToBeAnalyzed.remove();

					// Discard position if the player reached the border of the level (-> invalid level).
					if (       playerPosition < board.width
							|| playerPosition > board.size - board.width
							|| playerPosition % board.width == 0
							|| playerPosition % board.width == board.width - 1) {
						continue;
					}

					// Mark the square as inner square.
					innerSquares[playerPosition] = true;

					// Add all reachable neighbor squares to the stack if they haven't been reached before.
					for (int direction = 0; direction < DirectionConstants.DIRS_COUNT; direction++) {
						final int newPlayerPosition = board.getPosition(playerPosition, direction);
						if (!board.isWall(newPlayerPosition) && !innerSquares[newPlayerPosition]) {
							positionsToBeAnalyzed.add(newPlayerPosition);
						}
					}
				}
			}
		}

		return innerSquares;
	}

	/**
	 * Sets the passed level as level to be displayed in this Panel.
	 *
	 * @param levelToBeDisplayed  the <code>Level</code> to be displayed
	 */
	public void setLevelToDisplay(Level levelToBeDisplayed) {

		// Create an own board.
		board = new Board();

		// Set the passed level on the board.
		try {
			board.setBoardFromString(levelToBeDisplayed.getBoardDataAsString());
		} catch (Exception e) {
			// Show the error message.
			MessageDialogs.showExceptionError(this, e);
		}

		// Array containing the information which graphic has to be drawn at a specific position.
		graphicStatus = new byte[board.height * board.width];

		// New level means every thing has to be recalculated to refresh the Panel.
		isRecalculationNecessary = true;

		displayedLevel = levelToBeDisplayed;

		// Repaint this Panel.
		repaint();
	}

	/**
	 * Sets the passed board to be displayed.
	 *
	 * @param boardAsString the board data as <code>String</code>
	 */
	public void setBoardToDisplay(String boardAsString) {

		// Create an new board.
		board = new Board();

		// Create an own board from the board of the level.
		try {
			board.setBoardFromString(boardAsString);
		} catch (Exception e) {
			e.printStackTrace();
		}

		// Array containing the information which graphic has to be drawn at a specific position.
		graphicStatus = new byte[board.height * board.width];

		// New level means every thing has to be recalculated to refresh the Panel.
		isRecalculationNecessary = true;

		// Repaint this Panel.
		repaint();
	}

	/**
	 * Sets the passed board to be displayed.
	 *
	 * @param board the board to be displayed
	 */
	public void setBoardToDisplay(Board board) {

		this.board = board;

		// Array containing the information which graphic has to be drawn at a specific position.
		graphicStatus = new byte[board.height * board.width];

		// New level means every thing has to be recalculated to refresh the Panel.
		isRecalculationNecessary = true;

		// Repaint this Panel.
		repaint();
	}

	/**
	 * Returns the <code>Board</code> displayed in this class.
	 *
	 * @return the <code>Board</code> of this class
	 */
	public Board getBoard() {
		return board;
	}

	/**
	 * Returns the <code>Level</code> displayed in this class
	 *
	 * @return the <code>Level</code> of this class
	 */
	public Level getDisplayedLevel() {
		return displayedLevel;
	}

	/**
	 * Returns the current width of a square from the current scaled skin.
	 * @return width of a square
	 */
	public int getSquareWidth() {
		if (skin == null) {
			return 0;
		}
		return skin.graphicWidth;
	}

	/**
	 * Returns the current height of a square from the current scaled skin.
	 * @return height of a square
	 */
	public int getSquareHeight() {
		if (skin == null) {
			return 0;
		}
		return skin.graphicHeight;
	}

	/**
	 * Fake transformation class until the real Transformation class
	 * can be used at some time.
	 */
	protected class DummyTransformation {

		public int getExternalDirection(int direction) {
			return direction;
		}

		public int getInternalPosition(int position) {
			return position;
		}

		public int getOutputLevelWidth() {
			return board.width;
		}

		public int getOutputLevelHeight() {
			return board.height;
		}
	}


	/**
	 * This method is called when the user pressed the mouse on a board element in the GUI.
	 * <p>
	 * The passed position is already transformed to an internal position.
	 *
	 * @param position  position in the board
	 * @param e the <code>MouseEvent</code> that represents the mouse press
	 */
	protected void mousePressedAt(int position, MouseEvent e) {}

	@Override
	public void mouseClicked(MouseEvent e) {}

	@Override
	public void mouseEntered(MouseEvent e) {}

	@Override
	public void mouseExited(MouseEvent e) {}

	@Override
	public void mousePressed(MouseEvent e) {

		// Get the board position the mouse has been dragged at.
		int position = getBoardPosition(e.getX(), e.getY());

		// Call the method handling mouse pressing events if the mouse
		// has been dragged in the board.
		if(position >= 0) {
			mousePressedAt(Transformation.getInternalPosition(position), e);
		}
	}

	@Override
	public void mouseReleased(MouseEvent e) {

		// Get the board position the mouse has been released at.
		int position = getBoardPosition(e.getX(), e.getY());

		// Call the method handling mouse dragged events if the mouse has been released in the board.
		if(position >= 0) {
			mouseReleasedAt(Transformation.getInternalPosition(position), e);
		}
	}


	@Override
	public void mouseDragged(MouseEvent e) {

		// Get the board position the mouse has been dragged at.
		int position = getBoardPosition(e.getX(), e.getY());

		// Call the method handling mouse dragged events if the mouse has been dragged in the board.
		if(position >= 0) {
			mouseDraggedAt(Transformation.getInternalPosition(position), e);
		}
	}

	@Override
	public void mouseMoved(MouseEvent e) {}

	/**
	 * This method is called when the user dragged the mouse
	 * on a board element in the GUI.
	 * <p>
	 * The passed position is already transformed to an internal position.
	 *
	 * @param position  position in the board
	 * @param e the <code>MouseEvent</code> that represents the mouse press
	 */
	protected void mouseDraggedAt(int position, MouseEvent e) {}

	/**
	 * This method is called when the user released the mouse
	 * on a board element in the GUI.
	 * <p>
	 * The passed position is already transformed to an internal position.
	 *
	 * @param position  position in the board
	 * @param e the <code>MouseEvent</code> that represents the mouse press
	 */
	protected void mouseReleasedAt(int position, MouseEvent e) {}


	/**
	 * Returns the internal board position of a position in this panel.
	 *
	 * @param x  the x coordinate in this panel
	 * @param y  the y coordinate in this panel
	 * @return  the internal board position or -1 if the coordinates are not on the board
	 */
	protected int getBoardPosition(int x, int y) {

		// Calculate the coordinates of the mouse click in this panel.
		int mouseXCoordinate = ((x - xOffset) / skin.graphicWidth);
		int mouseYCoordinate = ((y - yOffset) / skin.graphicHeight);

		// Determine whether the mouse has been clicked within the board.
		// (Note: the calculation above rounds the result. Hence, don't check for mouse coordinate < 0)
		if (mouseXCoordinate >= Transformation.getOutputLevelWidth()
		 || mouseYCoordinate >= Transformation.getOutputLevelHeight()
		 || x < xOffset || y < yOffset) {
			return -1;
		}

		// Calculate the internal position.
		return Transformation.getInternalPosition(mouseXCoordinate + Transformation.getOutputLevelWidth() * mouseYCoordinate);
	}

	/**
	 * Returns all square positions located in the passed rectangle
	 * as an <code>ArrayList</code>.
	 *
	 * @param start  the  first selected point of the rectangle
	 * @param end    the second selected point of the rectangle
	 * @return  <code>ArrayList</code> containing the positions of all selected squares
	 */
	protected ArrayList<Integer> getBoardPositions(Point start, Point end) {

		// The user may have selected areas outside the board. Hence, ensure only coordinates on the board are used.
		adjustToBoard(start);
		adjustToBoard(end);

		// Determine which point is the top left one and which the bottom right one.
		int topLeft     = getBoardPosition(start.x < end.x ? start.x : end.x, start.y < end.y ? start.y : end.y);
		int bottomRight = getBoardPosition(start.x > end.x ? start.x : end.x, start.y > end.y ? start.y : end.y);

		// Calculate the x coordinate of both points.
		int mostLeftXCoordinate  = topLeft     % board.width;
		int mostRightXCoordinate = bottomRight % board.width;

		// Add all selected positions to the list.
		ArrayList<Integer> selectedSquares = new ArrayList<Integer>();
		for(int position = topLeft; position <= bottomRight; position++) {
			int xCoordinate = position % board.width;
			if(xCoordinate >= mostLeftXCoordinate && xCoordinate <= mostRightXCoordinate) {
				selectedSquares.add(position);
			}
		}

		return selectedSquares;
	}

	/**
	 * If the coordinates of the point aren't located on the board
	 * they are adjusted so they are on the edge of the board.
	 *
	 * @param p  <code>Point</code> containing the data which is adjusted
	 */
	protected void adjustToBoard(Point p) {

		// Clip both point coordinates at their allowed minimum (inclusive).
		if( p.x < xOffset ) {
			p.x = xOffset;
		}
		if( p.y < yOffset ) {
			p.y = yOffset;
		}

		// Compute maximal point coordinates, inclusive...
		int xmax = xOffset + Transformation.getOutputLevelWidth()  * skin.graphicWidth  - 1;
		int ymax = yOffset + Transformation.getOutputLevelHeight() * skin.graphicHeight - 1;

		// ... and use them to clip off larger values:
		if( p.x > xmax ) {
			p.x = xmax;
		}
		if( p.y > ymax ) {
			p.y = ymax;
		}
	}
}