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

import java.awt.BorderLayout;
import java.awt.Color;
import java.awt.Cursor;
import java.awt.Dimension;
import java.awt.Font;
import java.awt.Graphics;
import java.awt.Graphics2D;
import java.awt.GridLayout;
import java.awt.Image;
import java.awt.Rectangle;
import java.awt.datatransfer.DataFlavor;
import java.awt.dnd.DnDConstants;
import java.awt.dnd.DropTarget;
import java.awt.dnd.DropTargetAdapter;
import java.awt.dnd.DropTargetDragEvent;
import java.awt.dnd.DropTargetDropEvent;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.awt.event.InputEvent;
import java.awt.event.KeyEvent;
import java.awt.event.KeyListener;
import java.awt.event.MouseAdapter;
import java.awt.event.MouseEvent;
import java.awt.event.MouseWheelEvent;
import java.awt.event.MouseWheelListener;
import java.awt.image.BufferedImage;
import java.io.FileNotFoundException;
import java.lang.reflect.InvocationTargetException;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Properties;
import java.util.TooManyListenersException;
import java.util.TreeSet;
import java.util.concurrent.atomic.AtomicBoolean;

import javax.swing.BorderFactory;
import javax.swing.JComponent;
import javax.swing.JLabel;
import javax.swing.JPanel;
import javax.swing.JPopupMenu;
import javax.swing.JSlider;
import javax.swing.SwingConstants;
import javax.swing.SwingUtilities;
import javax.swing.Timer;
import javax.swing.event.ChangeEvent;
import javax.swing.event.ChangeListener;
import javax.swing.plaf.basic.BasicSliderUI;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.board.Board;
import de.sokoban_online.jsoko.board.DirectionConstants;
import de.sokoban_online.jsoko.boardpositions.AbsoluteBoardPosition;
import de.sokoban_online.jsoko.resourceHandling.Settings;
import de.sokoban_online.jsoko.resourceHandling.Settings.SearchDirection;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.solver.Influence;
import de.sokoban_online.jsoko.utilities.Debug;
import de.sokoban_online.jsoko.utilities.GraphicUtilities;
import de.sokoban_online.jsoko.utilities.IntStack;
import de.sokoban_online.jsoko.utilities.Utilities;
import de.sokoban_online.jsoko.workInProgress.BoxGoalsRatio;

/**
 * This class paints the main panel containing the board and the status bar. It
 * also handles some of the events regarding the graphical output (e.g. rotating
 * the display of the board).
 * <p>
 * Since this board is considerably more complex than the simple
 * {@link BoardDisplay}, the implementation is independent.
 */

@SuppressWarnings("serial")
public final class MainBoardDisplay extends JPanel implements KeyListener,
		DirectionConstants {

	/** Reference to the main object of this program. */
	JSoko application;

	/** Direct reference to the board object. */
	Board board;

	// Squares which are completely unreachable for the player are not drawn at
	// all.
	// (With Björns graphics we draw an empty square, and with Gerrys graphics
	// we draw background)
	private final byte GRAPHIC_NO_GRAPHIC = -1;
	// The graphics (of the board) are computed at runtime:
	// a different graphic is drawn when the location is occupied by a player or
	// a box.
	private final byte GRAPHIC_DYNAMIC = 0;
	// private final byte BEAUTY_GRAPHIC = 1;
	private final byte GRAPHIC_FIXED_GRAPHIC = 2;

	private byte[] graphicStatus;

	/**
	 * x offset of the display of the board in the panel. Public for easier
	 * access.
	 */
	public int xOffset = 0;

	/**
	 * y offset of the display of the board in the panel. Public for easier
	 * access.
	 */
	public int yOffset = 0;

	/**
	 * Graphics of the current skin to be shown on the screen. Each time the
	 * size of this panel changes or the level size changes these graphics are
	 * scaled to fit into the panel.
	 */
	public Skin skin;

	/**
	 * To ensure best quality when scaling a skin object, the original size
	 * graphics is used for scaling.
	 */
	private Skin originalSkin;

	/**
	 * Contains the current width and height of the panel to be used for the
	 * board
	 */
	private int currentWindowWidth = -1;
	private int currentWindowHeight = -1;

	/** The current factor we use to scale the graphics */
	private float scalingFactor = 0;

	/**
	 * Maximum scaling of the graphics. Usually this is 1, but the user may set
	 * higher values. The higher the scaling factor the worse the quality is.
	 */
	private int maximumScaling = 1;

	/** data to be shown in the "infobar" */
	private String infoString;

	/**
	 * The background image as loaded (= original size) is used as basis for the
	 * scaled version we actually use.
	 */
	private BufferedImage backgroundGraphicOriginalSize;
	/**
	 * The scaled background image we actually use, (fitting this JPanel).
	 */
	BufferedImage backgroundGraphic;

	/**
	 * Image which contains all graphics which don't change during game play.
	 * These graphics are all drawn to this single image in order to draw just
	 * this single image every time the GUI has to be redrawn => better
	 * performance.
	 */
	private BufferedImage fixedGraphicsImage;

	/**
	 * At the bottom of the GUI an infobar is drawn. This infobar is saved in
	 * this image without any text for better performance. Every time an info
	 * text is to be drawn this image is used as "background" for the info text.
	 */
	private Image infobarImage;

	/** The graphic height of the "infobar" */
	private final int infobarHeight = 26;

	/** X-Coordinate of the level title. */
	private final int levelTitleXPosition = 280;

	/**
	 * Distance from the window edges for the infobar on the top (displaying
	 * moves, pushes, level title, level no).
	 */
	private final int topInfoBarInset = 10;

	/**
	 * Number of pixels to the right border of the window to draw the last
	 * letter of the level no.
	 */
	private final int levelNoPixelDistanceRightEdge = 10;

	/** Popup for displaying information. */
	private JPopupMenu popup;

	/** View direction of the player. */
	private byte viewDirection = 0;

	/**
	 * Indicates whether the level size has changed and the graphic sizes have
	 * to be recalculated.
	 */
	boolean isRecalculationNecessary = false;

	/**
	 * Maximal (default) font size we try to use for the level title.
	 */
	private static final int LEVEL_TITLE_MAX_FONTSIZE = 14;

	/**
	 * Minimal font size we try to use for the level title.
	 */
	private static final int LEVEL_TITLE_MIN_FONTSIZE = 7;

	/**
	 * Font size of the level title. This value is changed later so the level
	 * title fits into the available space. A value of 0 (or less) suppresses
	 * drawing of the level title.
	 */
	private int levelTitleFontSize = LEVEL_TITLE_MAX_FONTSIZE;

	/**
	 * Current (prefix) length of the level title to show.
	 */
	private int levelTitleShowLength;

	/**
	 * Minimum length to which we truncate the level title text. The standard
	 * title "Level nnnn" should fit into this.
	 */
	private static final int LEVEL_TITEL_MIN_LENGTH = 10;

	/** Flag indicating whether the infobar is to be displayed */
	private boolean isInfoBarVisible = true;

	/** A JSlider for browsing through the history. */
	JSlider historySlider;

	/** The panel containing the history slider. */
	JPanel historySliderPanel;

	/**
	 * Last selected box position. This position is used to compare it with the
	 * current selected box position.
	 */
	int lastSelectedBoxPosition = -1;

	/** Timer for showing animations of the skin */
	Timer animationTimer;

	// Colors
	Color colorDisplayMovesPushes;
	Color colorBackgroundMovesPushes;
	Color colorInfobar;
	Color colorInfobarFrame;
	Color colorInfobarText;
	Color colorSelectedObjectFrame;
	Color colorNumberOfBoxesGoals;
	Color colorSelectedBoxFrame;

	/**
	 * DEBUG: Array of numbers to be shown at a specific position. Just for
	 * debug! If "null", nothing is shown. Otherwise the numbers are shown.
	 */
	public static int numbersToShow[] = new int[0];

	/**
	 * Creates a new object for displaying the GUI of this program.
	 *
	 * @param application
	 *            reference to the main Object which holds all references
	 */
	public MainBoardDisplay(final JSoko application) {

		this.application = application;
		board = application.board;

		// Get the maximum scaling factor from the settings.
		maximumScaling = Settings.getInt("maximumScaling", 3);

		// Set the current skin in this GUI.
		setSkin(Settings.currentSkin);

		// Be sure any skin has been loaded.
		if (originalSkin == null) {
			TreeSet<Properties> skins = SkinManagement.getAvailableSkins();
			for (Properties skinSettings : skins) {
				setSkin(skinSettings.getProperty("settingsFilePath"));
				if (originalSkin != null) {
					break;
				}
			}
			if (originalSkin == null) {
				System.exit(-1);
			}
		}

		// Add a mouse listener and a mouse motion listener.
		MouseEventHandler mouseEventHandler = new MouseEventHandler();
		addMouseListener(mouseEventHandler);
		addMouseMotionListener(mouseEventHandler);

		// The main panel must have the focus and is the only component that is
		// focusable.
		addKeyListener(this);
		setFocusable(true);
		requestFocusInWindow();

		// Let his panel handle drop events.
		DropTarget dropTarget = new DropTarget();
		dropTarget.setComponent(this);
		try {
			dropTarget.addDropTargetListener(new DropTargetHandler());
		} catch (TooManyListenersException e) { /* do nothing. */
		}

		setLayout(new BorderLayout());
		add(getMovementHistorySlider(), BorderLayout.NORTH);
		historySliderPanel.setVisible(true);

	}

	/**
	 * Main paint method of this GUI. This method draws the board and two status
	 * bars.
	 * <p>
	 * For a better performance all objects are drawn in this single panel
	 * instead of using an own <code>JPanel</code> for every square.
	 *
	 * @param graphic
	 *            graphic context
	 */
	@Override
	public final void paintComponent(Graphics graphic) {

		// Although this method paints the whole area of the panel ensure that
		// the panel
		// is cleared before painting. Calling super is recommended by its
		// documentation.
		super.paintComponent(graphic);

		Graphics2D g2D = (Graphics2D) graphic;

		// The panel must have a minimum size for drawing.
		if (getWidth() < 100 || getHeight() < 100) {
			setSize(100, 100);
		}

		// When the width or height of the window changed, or the level size
		// changed,
		// or a new level has been loaded, the graphics have to be rescaled,
		// and we have to recompute the space for the level title.
		if (currentWindowWidth != getWidth()
				|| currentWindowHeight != getHeight()
				|| isRecalculationNecessary) {
			newGraphicSizeCalculation(g2D);
		}

		Rectangle clipRectangle = g2D.getClipBounds();
		debugShowClip(clipRectangle);
		int yPositionClipArea = (clipRectangle == null) ? 0 : clipRectangle.y;

		// If only the infobar has to be redrawn skip the other elements.
		if (yPositionClipArea < (getHeight() - infobarHeight)) {
			// No, something above the infobar is to be drawn

			// Display the graphic of all graphics which never change during
			// game play.
			drawFixedGraphics(g2D);

			// Draw the editor graphics.
			drawEditorElements(g2D);

			// Draw the board.
			drawBoard(g2D, clipRectangle);
		}

		// Draw the infobar.
		drawInfobar(g2D);

		// Show debug information.
		drawDebugInformation(g2D);
	}

	// static private int dbgClipCnt = 0;
	private void debugShowClip(Rectangle clipRectangle) {
		// if (clipRectangle == null) {
		// System.out.println("CLIP: null");
		// } else {
		// System.out.println((++dbgClipCnt)
		// + " CLIP: x=" + clipRectangle.x
		// + " y=" + clipRectangle.y
		// + " w=" + clipRectangle.width
		// + " h=" + clipRectangle.height);
		// }
	}

	// static private int dbgSqCnt = 0;
	private BufferedImage debugShowSquare(int x, int y, BufferedImage graphic) {
		// System.out.println((++dbgSqCnt) + " SQ: x=" + x + " y=" + y );
		// return Utilities.getLightnessChangedImage(graphic, +50);
		return graphic;
	}

	/**
	 * Computes and returns whether the rectangle given by the left upper corner
	 * and both dimensions is completely outside the specified clip rectangle.
	 * When the clip rectangle is missing, it is considered to be arbitrarily
	 * large.
	 *
	 * @param x
	 *            x of upper left corner
	 * @param y
	 *            y of upper left corner
	 * @param xlen
	 *            width
	 * @param ylen
	 *            height
	 * @param clip
	 *            rectangle we compare against
	 * @return whether (x,y,xlen,ylen) is completely outside of "clip"
	 */
	static private boolean isOutsideClip(int x, int y, int xlen, int ylen,
			Rectangle clip) {

		// Non-existing rectangle stands for the complete plane => nothing is
		// outside of the complete plane.
		// We are "outside" of "clip", if we do not intersect with it.
		return clip != null && !clip.intersects(x, y, xlen, ylen);

	}

	/**
	 * Every time the size of the window changes or important changes of the GUI
	 * are to be drawn the sizes of the GUI elements have to be recalculated.
	 *
	 * @param g2D
	 *            graphic context to use for size calculations
	 */
	private final void newGraphicSizeCalculation(Graphics2D g2D) {

		// Set flag to false because the new sizes are calculated now.
		isRecalculationNecessary = false;

		// Get current size of the window.
		currentWindowWidth = getWidth();
		currentWindowHeight = getHeight();

		// Scale the background image to the window size.
		if (backgroundGraphic != null
				&& (backgroundGraphic.getWidth(this) != currentWindowWidth || backgroundGraphic
						.getHeight(this) != currentWindowHeight)) {
			backgroundGraphic = GraphicUtilities.getScaledInstance(
					backgroundGraphicOriginalSize, currentWindowWidth,
					currentWindowHeight);
		}

		// If the editor is activated it is shown of the left side of the GUI.
		// The pixel for it must reduce the size of the graphics for the board.
		int editorOffset = application.isEditorModeActivated() ? Settings.X_OFFSET_EDITORELEMENTS
				: 0;

		// Check how much we have to scale the graphics.
		// We subtract 90 pixels for "informations", and some more from the
		// width
		// for a border on both sides.
		float widthScalingFactor = (currentWindowWidth - 2
				* Settings.X_BORDER_OFFSET - editorOffset)
				/ (float) Transformation.getOutputLevelWidth()
				/ originalSkin.graphicWidth;
		float heightScalingFactor = (currentWindowHeight - 90)
				/ (float) Transformation.getOutputLevelHeight()
				/ originalSkin.graphicHeight;

		// Since width and height shall be scaled with the same factor,
		// we determine which one has to scaled more.
		// Hence, the smaller factor wins!
		scalingFactor = Math.min(widthScalingFactor, heightScalingFactor);

		// Enlarge graphics no more than with the maximal scaling factor.
		scalingFactor = Math.min(scalingFactor, maximumScaling);

		// Calculate new graphic width and height.
		int scaledGraphicsWidth = Math.round(originalSkin.graphicWidth
				* scalingFactor);
		int scaledGraphicsHeight = Math.round(originalSkin.graphicHeight
				* scalingFactor);

		// Get a scaled version of the skin.
		skin = originalSkin.getScaledVersion(scaledGraphicsWidth,
				scaledGraphicsHeight);

		// Calculate the new x- and y-offset for drawing the board centered.
		xOffset = (currentWindowWidth - editorOffset - Transformation
				.getOutputLevelWidth() * skin.graphicWidth)
				/ 2 + editorOffset;
		yOffset = (currentWindowHeight - Transformation.getOutputLevelHeight()
				* skin.graphicHeight + 30) / 2;

		// Get the image containing all graphics that don't change during game
		// play.
		fixedGraphicsImage = getImageOfFixedGraphics();

		// Create a new image which is used as "background" for the infobar.
		BufferedImage infobar = new BufferedImage(currentWindowWidth,
				infobarHeight, BufferedImage.TYPE_INT_ARGB);
		Graphics2D biContextInfobar = infobar.createGraphics();

		// Depending on the skin type the background graphic is painted or the
		// "empty square" graphic.
		if (skin.isBackgroundImageSupported) {
			biContextInfobar.drawImage(backgroundGraphic, 0, 0,
					currentWindowWidth, infobarHeight, 0, currentWindowHeight
							- infobarHeight, currentWindowWidth,
					currentWindowHeight, this);
		} else {
			for (int x2 = 0; x2 < getWidth(); x2 += skin.graphicWidth) {
				for (int y2 = 0; y2 < getHeight(); y2 += skin.graphicHeight) {
					biContextInfobar.drawImage(skin.emptySquare, x2, y2, this);
				}
			}
		}

		// Draw background for the infobar.
		biContextInfobar.setColor(colorInfobar);
		biContextInfobar.fillRoundRect(5, 0, currentWindowWidth - 10, 22, 25,
				25);

		biContextInfobar.setColor(colorInfobarFrame);
		biContextInfobar.drawRoundRect(5, 0, currentWindowWidth - 10, 22, 25,
				25);

		infobarImage = infobar;

		newGraphicSizeLevelTitle(g2D);
	}

	/**
	 * This is that part of the calculation in
	 * {@link #newGraphicSizeCalculation(Graphics2D)}, which determines how to
	 * show the level title text.
	 *
	 * @param g2D
	 *            graphic context to use for size calculations
	 */
	private final void newGraphicSizeLevelTitle(Graphics2D g2D) {

		// The level title is drawn starting at position "levelTitleXPosition".
		// The level number is drawn with a distance of
		// "levelNoPixelDistanceRightEdge"
		// pixels from the right border of the window. There should be a gap of
		// 15 pixel
		// between the level title and the level number information.
		int maximumWidthLevelTitle = currentWindowWidth - levelTitleXPosition
				- levelNoPixelDistanceRightEdge - 15;
		final String title = application.currentLevel.getTitle();

		levelTitleFontSize = LEVEL_TITLE_MAX_FONTSIZE;
		levelTitleShowLength = title.length();

		// Now we check, whether we would fit into the available space.
		// If not, we consider to reduce the font size in order to fit.
		for (;;) {
			String showTitle = getLevelTitleToShow();

			g2D.setFont(new Font("SansSerif", Font.BOLD, levelTitleFontSize));
			if (g2D.getFontMetrics().stringWidth(showTitle) <= maximumWidthLevelTitle) {
				// Hey! That works!
				return;
			}

			// Does not (yet) fit into the available space.
			// Lets try to reduce the font size...
			if (levelTitleFontSize > LEVEL_TITLE_MIN_FONTSIZE) {
				levelTitleFontSize--;
				continue;
			}

			// We cannot reduce the font size, but we may truncate the title
			// text...
			if (levelTitleShowLength > LEVEL_TITEL_MIN_LENGTH) {
				levelTitleShowLength--;
				continue;
			}

			// We do not have any further meaningful reduction options.
			// Hence... we omit the level title.
			levelTitleFontSize = 0;
			return;
		}
	}

	/**
	 * Constructs the current level title text to show. It may be truncated.
	 *
	 * @return current level title text to show
	 */
	private final String getLevelTitleToShow() {
		String title = application.currentLevel.getTitle();

		// if too long, clip it and add ellipsis for truncated part
		title = Utilities.clipToEllipsis(title, levelTitleShowLength);

		return title;
	}

	/**
	 * Draw all editor dependent graphics (if editor is activated).
	 *
	 * @param g2d
	 */
	private final void drawEditorElements(Graphics2D g2D) {

		// In editor mode the game board is shifted right,
		// and at the left side we offer a selection of objects to put into the
		// board.
		if (application.isEditorModeActivated()) {

			// Distances for positioning of the editor objects the user can
			// select.
			// x and y position of the first object and the space between the
			// objects.
			int objectsXOffset = Settings.OBJECT_XOFFSET;
			int firstObjectYOffset = Settings.FIRST_OBJECT_YOFFSET;
			int objectsYDistance = skin.graphicHeight
					+ Settings.OBJECTS_YDISTANCE;

			// The selected object gets a frame
			g2D.setColor(colorSelectedObjectFrame);
			g2D.fillRect(objectsXOffset - 3, firstObjectYOffset
					+ application.editor.getNumberOfSelectedObject()
					* objectsYDistance - 3, skin.graphicWidth + 6,
					skin.graphicHeight + 6);

			// Wall
			g2D.drawImage(skin.wall_no_neighbor, objectsXOffset,
					firstObjectYOffset, this);

			// Box. A box may be transparent, hence draw an empty square, too.
			g2D.drawImage(skin.emptySquare, objectsXOffset, firstObjectYOffset
					+ objectsYDistance, this);
			g2D.drawImage(skin.box, objectsXOffset, firstObjectYOffset
					+ objectsYDistance, this);

			// Goal. A goal may be transparent, hence draw an empty square, too.
			g2D.drawImage(skin.emptySquare, objectsXOffset, firstObjectYOffset
					+ 2 * objectsYDistance, this);
			g2D.drawImage(skin.goal, objectsXOffset, firstObjectYOffset + 2
					* objectsYDistance, this);

			// Player. The player may be transparent, hence draw an empty
			// square, too.
			g2D.drawImage(skin.emptySquare, objectsXOffset, firstObjectYOffset
					+ 3 * objectsYDistance, this);
			g2D.drawImage(skin.playerWithViewInDirection[UP], objectsXOffset,
					firstObjectYOffset + 3 * objectsYDistance, this);

			// Empty square
			g2D.drawImage(skin.emptySquare, objectsXOffset, firstObjectYOffset
					+ 4 * objectsYDistance, this);

			// In some skins (e.g. in Björns skin) the empty graphic cannot be
			// distinguished from the background.
			// Hence, the empty graphics get a black frame.

			if (backgroundGraphic == null
					|| skin.isBackgroundImageSupported == false) {
				g2D.setColor(Color.black);
				g2D.draw3DRect(objectsXOffset, firstObjectYOffset + 4
						* (skin.graphicHeight + Settings.OBJECTS_YDISTANCE),
						skin.graphicWidth, skin.graphicHeight, true);
			}

			// Number of boxes / goals
			g2D.setFont(new Font(null, Font.BOLD, 16));
			g2D.setColor(colorNumberOfBoxesGoals);
			g2D.drawString("" + board.boxCount + "/" + board.goalsCount,
					objectsXOffset, firstObjectYOffset + 5 * objectsYDistance
							+ 20);
		}
	}

	/**
	 * This method just draws the prepared fixed graphics. If there is a small
	 * enough clip rectangle, we try to only draw part of the image.
	 *
	 * @param g2D
	 *            graphic context to be used for drawing
	 */
	private final void drawFixedGraphics(Graphics2D g2D) {

		if (fixedGraphicsImage != null) {

			Rectangle clip = g2D.getClipBounds();

			// Draw the clip area of the image to the screen or all of the
			// graphic if there is no valid clip.
			if (clip != null) {
				g2D.drawImage(fixedGraphicsImage, clip.x, clip.y, clip.x
						+ clip.width, clip.y + clip.height, clip.x, clip.y,
						clip.x + clip.width, clip.y + clip.height, null);
			} else {
				g2D.drawImage(fixedGraphicsImage, 0, 0, this);
			}
		}
	}

	/**
	 * This method draws the board elements (walls, boxes, ...) to the GUI.
	 *
	 * @param g2D
	 *            graphic context to be used for drawing
	 * @param clip
	 *            the current clip rectangle for drawing (or <code>null</code>)
	 */
	private final void drawBoard(final Graphics2D g2D, Rectangle clip) {
		/*
		 * Currently we use the following from them the "board" to decide
		 * output: - board.playerPosition - board.isGoal(position) -
		 * board.isBoxOnGoal(boxPosition) -
		 * board.boxReachableSquares.isSquareReachable(position) -
		 * board.playersReachableSquares.isSquareReachable(position) At least
		 * these values have to be stable.
		 */

		// Graphic to be drawn.
		BufferedImage graphicToDraw = null;

		// Draw the proper graphic for every square of the board.
		for (int externalPosition = 0; externalPosition < board.size; externalPosition++) {

			// Calculate the internal position.
			final int position = Transformation
					.getInternalPosition(externalPosition);

			// Only player reachable squares must be redrawn.
			if (graphicStatus[position] != GRAPHIC_DYNAMIC) {
				continue;
			}

			// Get the x- and y-coordinate of the external position.
			final int xCoordinate = externalPosition
					% Transformation.getOutputLevelWidth();
			final int yCoordinate = externalPosition
					/ Transformation.getOutputLevelWidth();

			final int squareX = xOffset + xCoordinate * skin.graphicWidth;
			final int squareY = yOffset + yCoordinate * skin.graphicHeight;

			if (isOutsideClip(squareX, squareY, skin.graphicWidth,
					skin.graphicHeight, clip)) {
				continue;
			}
			// Draw the graphic to the screen.
			graphicToDraw = findSquareGraphic(position);
			graphicToDraw = debugShowSquare(squareX, squareY, graphicToDraw); // debug:
																				// print
																				// coordinates
																				// +
																				// change
																				// graphic
			g2D.drawImage(graphicToDraw, squareX, squareY, this);

			final int innerSqX = squareX
					+ (int) Math.round(skin.graphicWidth * 0.15);
			final int innerSqY = squareY
					+ (int) Math.round(skin.graphicHeight * 0.15);

			// Draw another graphic when the reachable squares of a box are to
			// be highlighted.
			if (Settings.showReachableBoxPositions == true
					&& application.isABoxSelected()
					&& board.boxReachableSquares.isSquareReachable(position)) {
				BufferedImage reachableGraphic = board.isGoal(position) ? skin.reachableBoxOnGoal
						: skin.reachableBox;
				g2D.drawImage(reachableGraphic, innerSqX, innerSqY, this);
			}

			// Draw the player reachable squares if requested.
			if (application.isHighLightingOfPlayerReachableSquaresActivated()
					&& board.playersReachableSquares
							.isSquareReachable(position)) {
				// The player itself is not marked as reached by itself,
				// but is rather highlighted by a border around it.
				if (position != board.playerPosition) {
					BufferedImage reachableGraphic = board.isGoal(position) ? skin.reachablePlayerSquareOnGoal
							: skin.reachablePlayerSquare;
					g2D.drawImage(reachableGraphic, innerSqX, innerSqY,
							(int) (skin.graphicWidth * 0.7),
							(int) (skin.graphicHeight * 0.7), this);
				}
			}
		}

		// Draw player if the level has one.
		if (board.playerPosition != -1) {

			// Calculate the coordinates of the player on the board.
			int playerOutputPosition = Transformation
					.getExternalPosition(board.playerPosition);
			int playerOutputXCoordinate = playerOutputPosition
					% Transformation.getOutputLevelWidth();
			int playerOutputYCoordinate = playerOutputPosition
					/ Transformation.getOutputLevelWidth();

			final int squareX = playerOutputXCoordinate * skin.graphicWidth
					+ xOffset;
			final int squareY = playerOutputYCoordinate * skin.graphicHeight
					+ yOffset;

			// Draw player graphic to the screen.
			g2D.drawImage(findPlayerGraphic(board.playerPosition), squareX,
					squareY, this);

			// Draw a graphic when the reachable squares of a box are to be
			// highlighted
			// and the player position is reachable, too.
			if (Settings.showReachableBoxPositions == true
					&& application.isABoxSelected()
					&& board.boxReachableSquares
							.isSquareReachable(board.playerPosition)) {
				g2D.drawImage(skin.reachableBox,
						squareX + (int) Math.round(skin.graphicWidth * 0.15),
						squareY + (int) Math.round(skin.graphicHeight * 0.15),
						this);
			}

			// Draw a rectangle border around the player if it is selected.
			if (application.isHighLightingOfPlayerReachableSquaresActivated()) {
				g2D.setColor(colorSelectedBoxFrame);
				g2D.drawRect(squareX, squareY, skin.graphicWidth - 1,
						skin.graphicHeight - 1);
			}
		}

		// Initialize the position of the last selected box. This position is
		// used to
		// check whether the user selected a box and then directly selected
		// another box.
		if (application.isABoxSelected() == false) {
			lastSelectedBoxPosition = -1;
		} else {
			// Draw a border around the selected box.

			// Calculate x- and y-coordinate.
			final int boxPosition = application.getSelectedBoxPosition();

			// Get the animation to be shown.
			ArrayList<BufferedImage> animationGraphics = board
					.isBoxOnGoal(boxPosition) ? skin.boxOnGoalAnimation
					: skin.boxAnimation;

			// If the skin offers animation of the box, showing animations is
			// enabled
			// in the settings and a new box has been selected then show the
			// animation.
			// Also draw a border around the selected box.
			if (animationGraphics.size() > 0
					&& Settings.getBool("showSkinAnimations") == true
					&& boxPosition != lastSelectedBoxPosition) {

				ActionListener animationTask = new ActionListener() {

					// Constant indicating how much animation graphics are to be
					// drawn.
					final int ANIMATION_FRAMES = 30;

					// Index of the animation sequence.
					private int animationIndex = 0;

					// Position of the selected box used to check whether
					// another box
					// may have been selected in the meantime.
					private int selectedBoxPosition = boxPosition;

					// Backup of the reference to the animation graphics. This
					// reference is used to detect a change of the graphics.
					ArrayList<BufferedImage> animationGraphicsBackup = null;

					// The animation graphics to be drawn (creates from the
					// "currentAnimationGraphics").
					ArrayList<BufferedImage> animationGraphics = new ArrayList<BufferedImage>(
							0);

					// Animation graphics of the currently loaded skin.
					ArrayList<BufferedImage> currentAnimationGraphics;

					@Override
					public void actionPerformed(ActionEvent evt) {

						// The SwingTimer which fired this event.
						Timer timer = (Timer) evt.getSource();

						// Get the current graphics (they may have changed their
						// size). The user may also have loaded a new board in
						// the mean time. Ensure the position is still a valid
						// one.
						if (boxPosition < board.size) {
							currentAnimationGraphics = board
									.isBoxOnGoal(boxPosition) ? skin.boxOnGoalAnimation
									: skin.boxAnimation;
						} else {
							currentAnimationGraphics = null;
						}

						// Stop this thread if the box isn't selected anymore,
						// another box has been selected,
						// the animations have been disabled or the skin doesn't
						// support animations.
						if (application.isABoxSelected() == false
								|| application.getSelectedBoxPosition() != selectedBoxPosition
								|| Settings.getBool("showSkinAnimations") == false
								|| currentAnimationGraphics == null
								|| currentAnimationGraphics.size() == 0) {

							// Cancel the animation.
							timer.stop();

							// Initialize the last selected box position. This
							// ensures that the animation immediately starts if
							// the animation is stopped due to the user having
							// selected a skin not supporting animations and now
							// having selected a skin supporting animations.
							lastSelectedBoxPosition = -1;

							return;
						}

						// For a smooth animation this GUI uses alpha blending
						// for
						// creating more animation frames. The calculation is
						// done if the
						// animation graphics have changed (for example due to
						// scaling).
						if (currentAnimationGraphics != animationGraphicsBackup) {
							animationGraphicsBackup = currentAnimationGraphics;

							animationGraphics = new ArrayList<BufferedImage>(
									ANIMATION_FRAMES);

							// Number of interpolated graphics between two
							// original graphics.
							int betweenGraphics = (ANIMATION_FRAMES - currentAnimationGraphics
									.size()) / currentAnimationGraphics.size();

							// Transparent steps for the blending graphics.
							float transparentStepValue = 1.0f / (betweenGraphics + 1);

							// Add additional graphics to the animation for a
							// smoother animation sequence.
							for (int graphicNo = 0; graphicNo < currentAnimationGraphics
									.size(); graphicNo++) {

								BufferedImage currentGraphic = currentAnimationGraphics
										.get(graphicNo);
								BufferedImage nextGraphic = currentAnimationGraphics
										.get((graphicNo + 1)
												% currentAnimationGraphics
														.size());

								// Add the original graphic to the new animation
								// graphics list.
								animationGraphics.add(currentGraphic);

								// Create new frames by alpha-blending for a
								// smooth animation.
								for (int i = 1; i <= betweenGraphics; i++) {
									BufferedImage alphaBlendImage = GraphicUtilities
											.getTransparentImage(
													currentGraphic,
													1
															- i
															* transparentStepValue);
									Graphics2D g2D = alphaBlendImage
											.createGraphics();
									g2D.drawImage(GraphicUtilities
											.getTransparentImage(nextGraphic, i
													* transparentStepValue), 0,
											0, null);
									g2D.dispose();

									// Add the new "between" graphic to the
									// animation graphics.
									animationGraphics.add(alphaBlendImage);
								}
							}
						}

						// Calculate the index of the next animation graphic. If
						// the skin has changed in the meantime this may
						// result in the animation continuing in the middle of
						// the animation of the new skin. However, skin changes
						// while the animation is running aren't that often.
						if (++animationIndex >= animationGraphics.size()) {
							animationIndex = 0;
						}

						BufferedImage bi = animationGraphics
								.get(animationIndex);

						// Get the graphics of the GUI.
						Graphics2D g2D = (Graphics2D) getGraphics();

						// Create a temporary buffered image for drawing all
						// into this image before showing the graphic.
						BufferedImage temp = new BufferedImage(bi.getWidth(),
								bi.getHeight(), bi.getType());
						Graphics graphicsTemp = temp.getGraphics();
						graphicsTemp.drawImage(bi, 0, 0, null);

						// Draw a rectangle border around the selected box.
						graphicsTemp.setColor(colorSelectedBoxFrame);
						graphicsTemp.drawRect(0, 0, bi.getWidth() - 1,
								bi.getHeight() - 1);

						// Draw the animation graphic.
						int x = Transformation.getExternalPosition(boxPosition)
								% Transformation.getOutputLevelWidth();
						int y = Transformation.getExternalPosition(boxPosition)
								/ Transformation.getOutputLevelWidth();
						g2D.drawImage(temp, x * skin.graphicWidth + xOffset, y
								* skin.graphicHeight + yOffset, null);
					}
				};

				// The current selected box becomes the last selected box.
				lastSelectedBoxPosition = boxPosition;

				// Create a timer to show a new frame of the animation every x
				// milliseconds.
				animationTimer = new Timer(Settings.getInt(
						"skinAnimationDelay", 35), animationTask);
				animationTimer.setInitialDelay(0);
				animationTimer.start();
			} else {
				int x = Transformation.getExternalPosition(application
						.getSelectedBoxPosition())
						% Transformation.getOutputLevelWidth();
				int y = Transformation.getExternalPosition(application
						.getSelectedBoxPosition())
						/ Transformation.getOutputLevelWidth();

				// Draw a rectangle border around the selected box.
				g2D.setColor(colorSelectedBoxFrame);
				g2D.drawRect(x * skin.graphicWidth + xOffset, y
						* skin.graphicHeight + yOffset, skin.graphicWidth - 2,
						skin.graphicHeight - 2);
				// g2D.setColor(colorSelectedBoxFrame);
				// int xOutput = x * skin.graphicWidth + xOffset - 1;
				// int yOutput = y * skin.graphicWidth + yOffset - 1;
				// // Left Upper Corner
				// g2D.drawLine(xOutput, yOutput, xOutput + 5, yOutput);
				// g2D.drawLine(xOutput, yOutput, xOutput, yOutput + 5);
				// // Right Upper Corner
				// g2D.drawLine(xOutput + skin.graphicWidth - 5, yOutput,
				// xOutput + skin.graphicWidth + 1, yOutput);
				// g2D.drawLine(xOutput + skin.graphicWidth + 1, yOutput,
				// xOutput + skin.graphicWidth + 1, yOutput + 5);
				// // Left Lower Corner
				// g2D.drawLine(xOutput, yOutput + skin.graphicHeight - 5,
				// xOutput, yOutput + skin.graphicHeight + 1);
				// g2D.drawLine(xOutput, yOutput + skin.graphicHeight + 1,
				// xOutput + 5, yOutput + skin.graphicHeight + 1);
				// // Right Lower Corner
				// g2D.drawLine(xOutput + skin.graphicWidth - 5, yOutput +
				// skin.graphicHeight + 1, xOutput + skin.graphicWidth + 1,
				// yOutput + skin.graphicHeight + 1);
				// g2D.drawLine(xOutput + skin.graphicWidth + 1, yOutput +
				// skin.graphicHeight - 5, xOutput + skin.graphicWidth + 1,
				// yOutput + skin.graphicHeight + 1);
			}
		}
	}

	/**
	 * Inspects the model to determine which basic graphic from the skin is to
	 * be drawn for one square, given by its internal position.
	 *
	 * @param position
	 *            internal position of the square
	 * @return the image to be drawn for the square
	 */
	private BufferedImage findSquareGraphic(int position) {
		BufferedImage graphicToDraw = null;

		// Empty square.
		if (board.isEmptySquare(position)) {
			graphicToDraw = skin.emptySquare;

			// Show deadlock square if they are to be shown.
			if (Settings.showDeadlockFields
					&& board.isSimpleDeadlockSquare((position))) {
				graphicToDraw = skin.deadlockSquare;
			}
		}

		// For debugging proposes showing the deadlock squares can be limited to
		// specific deadlock squares.
		if (Debug.debugShowSimpleDeadlocksForward
				&& board.isSimpleDeadlockSquareForwardsDebug(position)
				|| Debug.debugShowSimpleDeadlocksBackward
				&& board.isSimpleDeadlockSquareBackwardsDebug(position)
				|| Debug.debugShowAdvancedSimpleDeadlocks
				&& board.isAdvancedSimpleDeadlockSquareForwards(position)) {
			graphicToDraw = skin.deadlockSquare;
		}

		// Goal
		if (board.isGoal(position)) {
			graphicToDraw = skin.goal;
		}

		// Box / box on goal.
		if (board.isBox(position)) {
			if (board.isBoxOnGoal(position)) {
				graphicToDraw = skin.boxOnGoal;
			} else {
				// Show deadlock square if they are to be shown.
				if (Settings.showDeadlockFields
						&& board.isSimpleDeadlockSquare((position))) {
					graphicToDraw = skin.boxOnDeadlockSquare;
				} else {
					graphicToDraw = skin.box;
				}
			}
		}
		return graphicToDraw;
	}

	/**
	 * Inspects the model to determine which basic graphic from the skin is to
	 * be drawn for the player on the specified internal position.
	 *
	 * @param position
	 *            internal player position
	 * @return graphic to draw for the player at that position
	 */
	private BufferedImage findPlayerGraphic(int position) {
		BufferedImage[] playerGraphics;

		// Check whether the normal player graphics have to be used
		// or the "player on deadlock square" graphics.
		if (Settings.showDeadlockFields
				&& board.isSimpleDeadlockSquare(position)) {
			playerGraphics = skin.playerOnDeadlockSquareWithDirection;
		} else {
			if (board.isGoal(position)) {
				playerGraphics = skin.playerOnGoalWithViewInDirection;
			} else {
				playerGraphics = skin.playerWithViewInDirection;
			}
		}

		// From the selected vector choose by view direction of the player
		final int showDirection = Transformation
				.getExternalDirection(viewDirection);

		return playerGraphics[showDirection];
	}

	/**
	 * Draws the infobar at the bottom of the GUI.
	 *
	 * @param g2D
	 *            Graphics context of the GUI panel
	 */
	private final void drawInfobar(Graphics2D g2D) {

		// Return immediately if the infobar isn't visible.
		if (isInfoBarVisible == false) {
			return;
		}

		int infobarY = getHeight() - infobarHeight; // minimal Y we draw to
		Rectangle clipRectangle = g2D.getClipBounds();

		// Check, whether the infobar is completely clipped away, by checking
		// just Y
		if (clipRectangle != null) {
			int clipYmax = clipRectangle.y + clipRectangle.height - 1;
			if (infobarY > clipYmax) {
				// System.out.println("infobarY=" + infobarY + " > clipYmax=" +
				// clipYmax);
				return;
			}
		}

		// Draw background for the infobar
		g2D.drawImage(infobarImage, 0, infobarY, this);

		// Set color of info text.
		g2D.setColor(colorInfobarText);

		// Set font of info text.
		g2D.setFont(new Font("SansSerif", Font.BOLD, 14));

		// Draw info text.
		g2D.drawString(infoString, 12, getHeight() - 10);

	}

	/**
	 * Draws debug information. This is only done if debug mode is enabled.
	 *
	 * @param g2D
	 *            Graphics context of the GUI panel
	 */
	private final void drawDebugInformation(Graphics2D g2D) {

		// Return immediately if the debug mode isn't enabled.
		if (Debug.isDebugModeActivated == false) {
			return;
		}

		// // Graphics2D g = (Graphics2D) getGraphics(); // delete old
		// // drawBoard(g, null); // clip area rect
		// g2D.setColor(new Color(0xBF, 0xEF, 0xFF, 135));
		// if(g2D.getClipBounds().x > 0)
		// g2D.fillRect(g2D.getClipBounds().x, g2D.getClipBounds().y,
		// g2D.getClipBounds().width, g2D.getClipBounds().height);

		if (Debug.debugShowAllPositionIndices == false
				&& numbersToShow != null) {
			g2D.setColor(Color.YELLOW);
			for (int position = 0; position < numbersToShow.length; position++) {
				if (numbersToShow[position] == -1) {
					continue;
				}
				int xCoordinate = position
						% Transformation.getOutputLevelWidth();
				int yCoordinate = position
						/ Transformation.getOutputLevelWidth();

				g2D.drawString("" + numbersToShow[position], xCoordinate
						* skin.graphicWidth + xOffset + skin.graphicWidth / 2
						- 5, yCoordinate * skin.graphicHeight + yOffset
						+ skin.graphicHeight / 2 + 5);
			}
		}

		if (Debug.debugShowLevelIsInDBStatus) {
			g2D.setColor(Color.YELLOW);
			g2D.setFont(new Font(null, Font.BOLD, 14));
			g2D.drawString(
					"level connected: " + application.currentLevel.isConnectedWithDB()
				  + ", ID: " + application.currentLevel.getDatabaseID()
				  + ", delegate: "+ application.currentLevel.isStoredAsDelegate(), 12, 60);
			g2D.drawString("collection ID: "
					+ application.currentLevelCollection.getDatabaseID(), 12,
					80);
		}

		for (int position = 0; position < board.size; position++) {
			int internalPosition = Transformation.getInternalPosition(position);
			int xCoordinate = position % Transformation.getOutputLevelWidth();
			int yCoordinate = position / Transformation.getOutputLevelWidth();

			try {
				// Draw a rectangle on "special" squares.
				if (board.isMarked(internalPosition)
						|| Debug.debugShowCorralForcer
						&& board.isCorralForcerSquare(internalPosition)
						|| Debug.debugShowBoxDistanceBackward
						&& board.isGoalBackwardsSearch(internalPosition)
						|| Debug.debugShowLowerBoundBackward
						&& board.isGoalBackwardsSearch(internalPosition)) {
					g2D.setColor(new Color(0, 80, 175, 180));
					g2D.fill3DRect(xCoordinate * skin.graphicWidth + xOffset
							+ skin.graphicWidth / 3, yCoordinate
							* skin.graphicHeight + yOffset + skin.graphicHeight
							/ 3, skin.graphicWidth / 3, skin.graphicHeight / 3,
							true);
				}
			} catch (Exception e) {
				// Editor may have changed board size but not changed backward
				// goal array.
			}
		}

		// Show box data if requested
		if (Debug.debugShowBoxData) {
			popup = new JPopupMenu();
			JPanel panel = new JPanel();
			panel.setLayout(new GridLayout(board.boxCount + 1, 1));
			panel.add(new JLabel("Boxdata:"));
			for (int boxNo = 0; boxNo < board.boxCount; boxNo++) {
				String zeile = "BoxNo. " + boxNo + " : ";

				String pos = ""
						+ (board.boxData.getBoxPosition(boxNo) % board.width);
				if (pos.length() < 2) {
					pos = "  " + pos;
				}
				zeile += "x=" + pos;
				pos = "" + (board.boxData.getBoxPosition(boxNo) / board.width);
				if (pos.length() < 2) {
					pos = " " + pos;
				}
				zeile += ", y=" + pos;

				if (board.boxData.isBoxActive(boxNo)) {
					zeile += " Status: active";
				}
				if (board.boxData.isBoxInactive(boxNo)) {
					zeile += "Status: inactive";
				}
				if (board.boxData.isBoxFrozen(boxNo)) {
					zeile += ", frozen";
				} else {
					zeile += ", not frozen";
				}

				panel.add(new JLabel(zeile));
			}

			popup.add(panel);
			popup.show(this, getWidth() - 100, getHeight() / 2 - 200);

			Debug.debugShowBoxData = false;
		}

		// Debug mode: if requested, we show the the box distances
		if (Debug.debugShowBoxDistanceForward
				|| Debug.debugShowBoxDistanceBackward) {
			g2D.setColor(Color.YELLOW);
			g2D.setFont(new Font(null, Font.BOLD, 14));
			int distance = 0;

			for (int position = board.firstRelevantSquare; position < board.lastRelevantSquare; position++) {
				int xOutput = position % Transformation.getOutputLevelWidth();
				int yOutput = position / Transformation.getOutputLevelWidth();
				if (Debug.debugShowBoxDistanceForward) {
					distance = board.distances.getBoxDistanceForwardsPosition(
							Debug.debugSquarePosition, position);
				} else {
					distance = board.distances.getBoxDistanceBackwardPosition(Debug.debugSquarePosition, position);
				}
				if (distance == Board.UNREACHABLE) {
					continue;
				}
				g2D.drawString("" + distance, xOutput * skin.graphicWidth
						+ xOffset + skin.graphicWidth / 2 - 5, yOutput
						* skin.graphicHeight + yOffset + skin.graphicHeight / 2
						+ 5);
			}
		}

		// Debug mode: Show influence values if requested.
		if (Debug.debugShowInfluenceValues
				|| Debug.debugShowInfluenceColors) {
			Influence influence = new Influence(board);
			influence.calculateInfluenceValues();
			g2D.setColor(Color.YELLOW);
			g2D.setFont(new Font(null, Font.BOLD, 14));

			board.distances.updateBoxDistances(SearchDirection.FORWARD, false); // Calculate
																				// the
																				// box
																				// push
																				// distance
																				// from
																				// every
																				// position
																				// to
																				// every
																				// other
																				// position

			int influenceValue = 0;
			for (int position = board.firstRelevantSquare; position < board.lastRelevantSquare; position++) {
				if (board.isOuterSquareOrWall(position)) {
					continue;
				}
				int xOutput = position % Transformation.getOutputLevelWidth();
				int yOutput = position / Transformation.getOutputLevelWidth();
				influenceValue = influence
						.getInfluenceDistance(
								Debug.debugSquarePosition > board.lastRelevantSquare ? board.playerPosition
										: Debug.debugSquarePosition,
								position);

				// Color the squares according to their influence if requested.
				if (Debug.debugShowInfluenceColors == true) {
					BufferedImage graphicToDraw = skin.emptySquare;
					if (board.isBox(position)) {
						graphicToDraw = skin.box;
					}
					if (board.playerPosition == position) {
						graphicToDraw = skin.playerWithViewInDirection[UP];
					}
					if (board.isGoal(position)) {
						graphicToDraw = skin.goal;
						if (board.isBox(position)) {
							graphicToDraw = skin.boxOnGoal;
						}
						if (board.playerPosition == position) {
							graphicToDraw = skin.playerOnGoalWithViewInDirection[UP];
						}
					}

					BufferedImage bi = new BufferedImage(skin.graphicWidth,
							skin.graphicHeight, BufferedImage.TYPE_INT_RGB);
					Graphics2D biContext = bi.createGraphics();
					biContext.drawImage(graphicToDraw, 0, 0, skin.graphicWidth,
							skin.graphicHeight, null);
					GraphicUtilities.changeLightness(bi,
							6 * influenceValue - 140);
					g2D.drawImage(bi, xOutput * skin.graphicWidth + xOffset,
							yOutput * skin.graphicHeight + yOffset, this);
				}

				// Show the influence values if requested.
				if (Debug.debugShowInfluenceValues == true) {
					g2D.drawString("" + influenceValue, xOutput
							* skin.graphicWidth + xOffset + skin.graphicWidth
							/ 2 - 5, yOutput * skin.graphicHeight + yOffset
							+ skin.graphicHeight / 2 + 5);
				}
			}
		}

		// Debug mode: Show areas having different reachable goals.
		if (Debug.debugShowDifferentReachableGoalsAreas) {
			BoxGoalsRatio boxGoalsRation = new BoxGoalsRatio(board);
			boxGoalsRation.updateAreaData();
			g2D.setColor(Color.YELLOW);
			g2D.setFont(new Font(null, Font.BOLD, 14));

			int opaqueValue = 40;
			Color[] colors = new Color[] {
					new Color(0xFF, 0x33, 0x66, opaqueValue),
					new Color(0x66, 0x99, 0xFF, opaqueValue),
					new Color(0x99, 0xFF, 0x66, opaqueValue),
					new Color(0xFF, 0xFF, 0x5C, opaqueValue),
					new Color(0xFF, 0x66, 0x66, opaqueValue),
					new Color(0xFF, 0x4D, 0x00, opaqueValue),
					new Color(0x99, 0x66, 0xCC, opaqueValue),
					new Color(0x99, 0x33, 0x99, opaqueValue) };

			for (int position = board.firstRelevantSquare; position < board.lastRelevantSquare; position++) {
				if (board.isOuterSquareOrWall(position)) {
					continue;
				}
				int xOutput = position % Transformation.getOutputLevelWidth();
				int yOutput = position / Transformation.getOutputLevelWidth();
				int areaNo = boxGoalsRation.getAreaNumber(position);

				if (areaNo == -1) {
					continue;
				}

				g2D.setColor(colors[areaNo % (colors.length - 1)]);
				g2D.fillRect(xOutput * skin.graphicWidth + xOffset, yOutput
						* skin.graphicHeight + yOffset, skin.graphicWidth,
						skin.graphicHeight);
			}
		}

		// Debug mode: if requested, we show all position indexes
		if (Debug.debugShowAllPositionIndices) {
			g2D.setColor(Color.YELLOW);
			g2D.setFont(new Font("MONOSPACED", Font.PLAIN, 12));

			for (int position = board.firstRelevantSquare; position < board.lastRelevantSquare; position++) {
				int xOutput = position % Transformation.getOutputLevelWidth();
				int yOutput = position / Transformation.getOutputLevelWidth();

				if (board.isOuterSquareOrWall(position)) {
					continue;
				}
				g2D.drawString("" + position, xOutput * skin.graphicWidth
						+ xOffset + skin.graphicWidth / 2 - 5, yOutput
						* skin.graphicHeight + yOffset + skin.graphicHeight / 2
						+ 5);
			}
		}

		// If requested we show the hash value
		if (Debug.debugShowHashvalue) {
			g2D.setColor(Color.YELLOW);
			g2D.setFont(new Font(null, Font.BOLD, 14));
			g2D.drawString("Hashvalue: "
					+ new AbsoluteBoardPosition(board).hashCode(), 10,
					getHeight() - 60);
		}

		// If requested we show the maximally available RAM
		if (Debug.debugShowMaximumRAM) {
			g2D.setColor(Color.YELLOW);
			g2D.setFont(new Font(null, Font.BOLD, 14));
			g2D.drawString("Maximum RAM: " + Runtime.getRuntime().maxMemory(),
					10, getHeight() - 40);
		}

		// Display the graphic size if needed.
		if (Debug.debugShowGraphicSize) {
			g2D.setColor(Color.YELLOW);
			g2D.setFont(new Font(null, Font.BOLD, 14));
			g2D.drawString("Graphic size = " + skin.graphicWidth, 150,
					getHeight() - 10);
		}
	}

	/**
	 * Sets the board this GUI shows.
	 *
	 * @param board
	 *            board to be shown
	 */
	public void setBoardToDisplay(Board board) {

		// Save the reference.
		this.board = board;

		// Create new array for storing information about which positions have
		// to be drawn when repainting.
		graphicStatus = new byte[board.size];

		// Initialize the debug array used for showing numbers on the board.
		if(numbersToShow != null) {
			Arrays.fill(numbersToShow, -1);
		}

		// Reset the transformation of the board.
		transformBoard(-1);
	}

	/**
	 * Sets the info text which is to be displayed.
	 *
	 * @param infotext
	 *            The info text to set
	 */
	public final void displayInfotext(String infotext) {

		// It is not seldom the case, that the infoString is replaced by equal
		// content.
		// FFS: suppress repaint? mixing with changes to "isInfoBarVisible"?
		infoString = infotext;

		// Repaint the infobar if necessary.
		if (isInfoBarVisible) {
			repaint(0, getHeight() - infobarHeight, getWidth(), infobarHeight);
		}
	}

	/**
	 * Loads a new background graphic.
	 *
	 * @param graphicFilePath
	 *            path to the graphic to be loaded
	 * @throws FileNotFoundException
	 *             file could not be loaded exception
	 */
	final public void setBackgroundGraphic(String graphicFilePath)
			throws FileNotFoundException {

		// For testing drawing own backgrounds in debug mode is possible:
		if (Debug.debugDrawOwnSkin) {
			backgroundGraphicOriginalSize = new BufferedImage(getWidth(),
					getHeight(), BufferedImage.TYPE_INT_RGB);
			Graphics2D g2 = backgroundGraphicOriginalSize.createGraphics();
			java.awt.GradientPaint Cyclic;
			// Cyclic = new java.awt.GradientPaint (160F, 20F, Color.blue,
			// 260F, 90F, Color.green, true);
			Cyclic = new java.awt.GradientPaint(160F, 20F, Color.white, 260F,
					90F, Color.darkGray, true);
			g2.setPaint(Cyclic);
			g2.fillRect(0, 0, backgroundGraphicOriginalSize.getWidth(),
					backgroundGraphicOriginalSize.getHeight());

			// java.awt.geom.Point2D center = new
			// java.awt.geom.Point2D.Float(backgroundGraphic.getWidth()/2,
			// backgroundGraphic.getHeight()/2);
			// float radius = backgroundGraphic.getWidth();
			// float[] dist = {0.1f, 0.75f};
			// Color[] colors = {Color.WHITE, Color.darkGray};
			// java.awt.RadialGradientPaint rp = new
			// java.awt.RadialGradientPaint(center, radius, dist, colors);
			// g2.setPaint(rp);
			// g2.fillRect (0, 0, backgroundGraphic.getWidth(),
			// backgroundGraphic.getHeight());

			backgroundGraphic = backgroundGraphicOriginalSize;
			return;
		}

		// Load the background graphic.
		backgroundGraphicOriginalSize = Utilities
				.loadBufferedImage(graphicFilePath);

		// Set the new background graphic as current background.
		backgroundGraphic = backgroundGraphicOriginalSize;

		// Save the graphic name in the settings.
		Settings.set("backgroundImageFile", graphicFilePath);

		// The new background graphic has to be resized the next time the GUI is
		// displayed.
		isRecalculationNecessary = true;

		// Show the new background image.
		repaint();
	}

	/**
	 * Loads the given skin and sets it as active skin in the game.
	 *
	 * @param skinSettingsFile
	 *            path to the settings file of the skin to be loaded
	 */
	final public void setSkin(String skinSettingsFile) {

		// Load the new skin.
		try {
			originalSkin = new Skin(skinSettingsFile);
		} catch (FileNotFoundException e) {
			// Display a message because the skin couldn't be loaded.
			MessageDialogs.showExceptionError(this, e);
			return;
		} catch (Exception e2) {
			if (Debug.isDebugModeActivated) {
				e2.printStackTrace();
			}

			// Display a message because the skin couldn't be loaded.
			MessageDialogs.showErrorTextKey(this, "message.skinLoadError");
			return;
		}

		// The program uses a copy of the skin in order to keep the original
		// skin as template for scaling.
		skin = originalSkin.getScaledVersion(originalSkin.graphicWidth,
				originalSkin.graphicHeight);

		// Load graphic for the background if none has been loaded yet.
		if (backgroundGraphic == null && skin.isBackgroundImageSupported) {
			try {
				setBackgroundGraphic(Settings.get("backgroundImageFile"));
			} catch (FileNotFoundException e) {
				// Display a message because the image couldn't be loaded.
				MessageDialogs.showExceptionWarning(this, e);
			}
		}

		// Get the colors for showing the GUI elements. These colors are set in
		// the settings but may be overwritten by the skin.
		colorDisplayMovesPushes = getColor("colorDisplayMovesPushes");
		colorBackgroundMovesPushes = getColor("colorBackgroundMovesPushes");
		colorInfobar = getColor("colorInfobar");
		colorInfobarFrame = getColor("colorInfobarFrame");
		colorInfobarText = getColor("colorInfobarText");
		colorSelectedObjectFrame = getColor("colorSelectedObjectFrame");
		colorNumberOfBoxesGoals = getColor("colorNumberOfBoxesGoals");
		colorSelectedBoxFrame = getColor("colorSelectedBoxFrame");

		// Set the current skin in the settings.
		Settings.currentSkin = skinSettingsFile;

		// The graphic sizes have to be recalculated.
		isRecalculationNecessary = true;
		repaint();
	}

	/**
	 * Returns the currently used skin.
	 *
	 * @return the <code>Skin</code> currently used
	 */
	public Skin getCurrentSkin() {
		return skin;
	}

	/**
	 * Sets a new delay for the animations that are shown.
	 * <p>
	 * Depending on the skin there may be an animation for a selected box and
	 * the selected player.
	 *
	 * @param delay
	 *            the delay in milliseconds
	 */
	public void setSkinAnimationDelay(int delay) {

		// Save the delay in the settings.
		Settings.set("skinAnimationDelay", "" + delay);

		// If a timer is running set the new delay value.
		if (animationTimer != null && animationTimer.isRunning()) {
			animationTimer.setDelay(delay);
		}
	}

	/**
	 * Sets the view direction of the player to the passed direction.
	 * <p>
	 * The view direction is important for some skins in order to be able to
	 * show the correct graphic.
	 *
	 * @param viewDirection
	 *            the view direction of the player
	 */
	public void setViewDirection(int viewDirection) {
		this.viewDirection = (byte) viewDirection;
	}

	/**
	 * Sets the maximum factor for scaling the graphics.
	 * <p>
	 * Some skins offer bad quality graphics which shouldn't be scaled too much.
	 * Hence the user can set a maximum scaling factor. This way the graphics
	 * aren't scaled beyond this factor even if there is enough space for the
	 * graphics to be drawn.
	 *
	 * @param maximumScalingFactor
	 *            the new factor to be set
	 */
	public void setMaximumScalingFactor(int maximumScalingFactor) {

		maximumScaling = maximumScalingFactor;

		recalculateGraphicSizes();
		repaint();

		// Save the zooming factor.
		Settings.set("maximumScaling", "" + maximumScaling);
	}

	/**
	 * Transforms the board (rotation and mirroring)
	 *
	 * @param transformationValue
	 *            kind of transformation
	 */
	public void transformBoard(int transformationValue) {

		// Tell it to the Transformation class.
		Transformation.transform(transformationValue);

		// Since the transformation may have exchanged height and width, we
		// force a recalculation of the graphics, as if we had loaded a new level.
		isRecalculationNecessary = true;

		// Show new graphics
		repaint();
	}

	/**
	 * Sets the flag specifying whether the infobar is to be shown or not.
	 *
	 * @param visibleStatus  visibility status of the infobar to set
	 */
	public void setInfoBarVisible(boolean visibleStatus) {
		isInfoBarVisible = visibleStatus;
	}

	/**
	 * Returns a <code>BufferedImage</code> of the all fixed graphics.
	 * <p>
	 * The background image, the walls of the level and other elements to be
	 * drawn that never change during the game. These elements are drawn to one
	 * image which is shown every time the GUI must be repainted. This way only
	 * "dynamic" elements which change during the game must be redrawn on top of
	 * this image.
	 *
	 * @return the image of all fixed graphics
	 */
	private BufferedImage getImageOfFixedGraphics() {

		// Position on the board.
		int position = -1;

		// Board width and height for easier access.
		int boardWidth = board.width;
		int boardHeight = board.height;

		// Array for determining the wall graphic to be shown at a specific
		// position. The graphics are ordered in a
		// specific way in order for easily determination of the graphic to be
		// drawn.
		BufferedImage[] wallGraphics = new BufferedImage[] {
				skin.wall_no_neighbor, // 0
				skin.wall_neighbor_left, // 1
				skin.wall_neighbor_right, // 2
				skin.wall_neighbor_left_right, // 3
				skin.wall_neighbor_above, // 4
				skin.wall_neighbor_above_left, // 5
				skin.wall_neighbor_above_right, // 6
				skin.wall_neighbor_above_left_right, // 7
				skin.wall_neighbor_below, // 8
				skin.wall_neighbor_below_left, // 9
				skin.wall_neighbor_below_right, // 10
				skin.wall_neighbor_below_left_right, // 11
				skin.wall_neighbor_above_below, // 12
				skin.wall_neighbor_above_below_left, // 13
				skin.wall_neighbor_above_below_right, // 14
				skin.wall_neighbor_above_below_left_right }; // 15

		// Create a new buffered image for the fixed graphics.
		BufferedImage fixedGraphicsImage = new BufferedImage(
				currentWindowWidth, currentWindowHeight,
				BufferedImage.TYPE_INT_ARGB);
		Graphics2D g2D = fixedGraphicsImage.createGraphics();

		// Copy the background image into the image.
		if (backgroundGraphic != null && skin.isBackgroundImageSupported) {
			g2D.drawImage(backgroundGraphic, 0, 0, null);
		} else {
			// The empty square graphic is used as background.
			for (int x2 = 0; x2 < getWidth(); x2 += skin.graphicWidth) {
				for (int y2 = 0; y2 < getHeight(); y2 += skin.graphicHeight) {
					g2D.drawImage(skin.emptySquare, x2, y2, this);
				}
			}
		}

		// Empty squares are drawn when the player can reach a box or a goal
		// from them.
		// In order to be able to correctly display levels like Sasquatch 41 the
		// player is
		// placed to every goal and every box position and then the reachable
		// squares of the player are determined.
		boolean[] specialSquares = identifySpecialSquares();

		// Check for every position which graphic has to be drawn.
		for (int externalPosition = 0; externalPosition < board.size; externalPosition++) {

			// Get the internal position.
			position = Transformation.getInternalPosition(externalPosition);

			// Split the external position in x- and y-coordinate.
			int externalXCoordinate = externalPosition
					% Transformation.getOutputLevelWidth();
			int externalYCoordinate = externalPosition
					/ Transformation.getOutputLevelWidth();

			// Initialize the graphic status for every position.
			graphicStatus[position] = GRAPHIC_NO_GRAPHIC;

			// If the player can reach the position the graphic to be drawn
			// can't be precalculated. Nevertheless, because some
			// skins have a transparent graphic for the player / for the box,
			// the empty square / goal graphic has to be
			// drawn on every square.
			if (board.isOuterSquareOrWall(position) == false
					&& specialSquares[position] == false) {
				graphicStatus[position] = GRAPHIC_DYNAMIC;

				// Draw a goal.
				if (board.isGoal(position)) {
					g2D.drawImage(skin.goal, externalXCoordinate
							* skin.graphicWidth + xOffset, externalYCoordinate
							* skin.graphicHeight + yOffset, this);
					continue;
				}

				// Draw an empty square.
				g2D.drawImage(skin.emptySquare, externalXCoordinate
						* skin.graphicWidth + xOffset, externalYCoordinate
						* skin.graphicHeight + yOffset, this);

				continue;
			}

			// If it's a wall or a square that is a special square then the
			// graphic never changes during game play.
			if (board.isWall(position) || specialSquares[position] == true) {
				graphicStatus[position] = GRAPHIC_FIXED_GRAPHIC;
			}

			// Empty squares that are special squares are drawn as empty square,
			// other empty squares aren't drawn.
			// (Note: player reachable empty squares are already handled in the
			// condition above).
			if (board.isEmptySquare(position)) {
				if (board.isOuterSquareOrWall(position) == false
						|| specialSquares[position] == true) {
					g2D.drawImage(skin.emptySquare, externalXCoordinate
							* skin.graphicWidth + xOffset, externalYCoordinate
							* skin.graphicHeight + yOffset, this);
				}
				continue;
			}

			if (board.isBoxOnGoal(position)) {
				g2D.drawImage(skin.boxOnGoal, externalXCoordinate
						* skin.graphicWidth + xOffset, externalYCoordinate
						* skin.graphicHeight + yOffset, this);
				continue;
			}

			if (board.isBox(position)) {
				g2D.drawImage(skin.box, externalXCoordinate * skin.graphicWidth
						+ xOffset, externalYCoordinate * skin.graphicHeight
						+ yOffset, this);
				continue;
			}

			if (board.isGoal(position)) {
				g2D.drawImage(skin.goal, externalXCoordinate
						* skin.graphicWidth + xOffset, externalYCoordinate
						* skin.graphicHeight + yOffset, this);
				continue;
			}
		}

		// Draw the walls if they are to be drawn.
		if (Settings.getBool("showWalls") == true) {

			// Extra loop for the walls. The board might be shown transformed.
			// Hence, loop over the !!external!! board representation
			// to determine which wall has to be shown at which position. Hence,
			// all positions are external and transformed to an
			// internal position where necessary.
			boardWidth = Transformation.getOutputLevelWidth();
			boardHeight = Transformation.getOutputLevelHeight();
			int leftCutPixel = 0;
			int rightCutPixel = 0;
			int aboveCutPixel = 0;
			int belowCutPixel = 0;

			boolean isNoGraphicLeftAbove = false;
			boolean isNoGraphicRightAbove = false;
			boolean isNoGraphicLeftBelow = false;
			boolean isNoGraphicRightBelow = false;
			boolean isRelevantCornerWall = false;

			// x and y coordinates of the destination area a graphic is to be
			// drawn to.
			int destinationAreaX1 = -1;
			int destinationAreaY1 = -1;
			int destinationAreaX2 = -1;
			int destinationAreaY2 = -1;

			for (int y = 0; y < boardHeight; y++) {
				for (int x = 0; x < boardWidth; x++) {

					// Calculate the internal and external position.
					position = y * boardWidth + x;

					// Jump over all squares not a wall.
					if (!board.isWall(Transformation
							.getInternalPosition(position))) {
						continue;
					}

					// Calculate the coordinates of the rectangle the graphic is
					// to be drawn to.

					// top left corner of the destination area
					destinationAreaX1 = x * skin.graphicWidth + xOffset;
					destinationAreaY1 = y * skin.graphicHeight + yOffset;

					destinationAreaX2 = x * skin.graphicWidth + xOffset
							+ skin.graphicWidth;
					// bottom right corner of the destination area
					destinationAreaY2 = y * skin.graphicHeight + yOffset
							+ skin.graphicHeight;

					// Number of the graphic in the permutation array to be
					// drawn.
					int wallValue = 0;

					// Add 1 if there is a wall to the left.
					if (x > 0
							&& board.isWall(Transformation
									.getInternalPosition(position - 1))) {
						wallValue |= 1;
					}

					// Add 2 if there is a wall to the right.
					if (x < boardWidth - 1
							&& board.isWall(Transformation
									.getInternalPosition(position + 1))) {
						wallValue |= 2;
					}

					// Add 4 if there is a wall above.
					if (y > 0
							&& board.isWall(Transformation
									.getInternalPosition(position - boardWidth))) {
						wallValue |= 4;
					}

					// Add 8 if there is a wall below.
					if (y < boardHeight - 1
							&& board.isWall(Transformation
									.getInternalPosition(position + boardWidth))) {
						wallValue |= 8;
					}

					// Initialize the areas to cut from the image.
					leftCutPixel = rightCutPixel = aboveCutPixel = belowCutPixel = 0;

					/**
					 * If there isn't any level element at a specific side, cut
					 * some pixels of the image have to be cut at that side.
					 */
					if (x == 0
							|| graphicStatus[Transformation
									.getInternalPosition(position - 1)] == GRAPHIC_NO_GRAPHIC) {
						leftCutPixel = skin.leftBorder;
					}

					if (x == boardWidth - 1
							|| graphicStatus[Transformation
									.getInternalPosition(position + 1)] == GRAPHIC_NO_GRAPHIC) {
						rightCutPixel = skin.rightBorder;
					}

					if (y == 0
							|| graphicStatus[Transformation
									.getInternalPosition(position - boardWidth)] == GRAPHIC_NO_GRAPHIC) {
						aboveCutPixel = skin.aboveBorder;
					}

					if (y == boardHeight - 1
							|| graphicStatus[Transformation
									.getInternalPosition(position + boardWidth)] == GRAPHIC_NO_GRAPHIC) {
						belowCutPixel = skin.belowBorder;
					}

					/**
					 * Special treatment for graphics that represent a corner.
					 * The little square in the corner is drawn as background.
					 * However, it mustn't be drawn (so the background remains
					 * shown) in the case that the adjacent graphic is a
					 * "no graphic". Therefore some of the walls are always
					 * drawn "cut" and then it is checked whether parts of the
					 * graphic have to be drawn additionally because there isn't
					 * any "no graphic" in the neighborhood. Therefore: check
					 * for "no graphic" left-above, right-above, left-below and
					 * right-below.
					 *
					 * c%c %%% a graphic split into 9 sub images. c%c
					 *
					 * c = corners that are only drawn when no "no graphic"
					 * square is in the neighborhood. To to this only the middle
					 * part of the graphic is drawn. The four other parts are
					 * drawn later in the coding.
					 */
					// Initialize the flags.
					isNoGraphicLeftAbove = isNoGraphicRightAbove = isNoGraphicLeftBelow = isNoGraphicRightBelow = isRelevantCornerWall = false;

					if (wallValue == 5 || // wall_neighbor_above_left
							wallValue == 6 || // wall_neighbor_above_right
							wallValue == 7 || // wall_neighbor_above_left_right
							wallValue == 9 || // wall_neighbor_below_left
							wallValue == 10 || // wall_neighbor_below_right
							wallValue == 11 || // wall_neighbor_below_left_right
							wallValue == 13 || // wall_neighbor_above_below_left
							wallValue == 14 || // wall_neighbor_above_below_right
							wallValue == 15) { // wall_neighbor_above_below_left_right

						if (x == 0
								|| y == 0
								|| graphicStatus[Transformation
										.getInternalPosition(position
												- boardWidth - 1)] == GRAPHIC_NO_GRAPHIC) {
							isNoGraphicLeftAbove = true;
						}

						if (x == boardWidth - 1
								|| y == 0
								|| graphicStatus[Transformation
										.getInternalPosition(position
												- boardWidth + 1)] == GRAPHIC_NO_GRAPHIC) {
							isNoGraphicRightAbove = true;
						}

						if (x == 0
								|| y == boardHeight - 1
								|| graphicStatus[Transformation
										.getInternalPosition(position
												+ boardWidth - 1)] == GRAPHIC_NO_GRAPHIC) {
							isNoGraphicLeftBelow = true;
						}

						if (x == boardWidth - 1
								|| y == boardHeight - 1
								|| graphicStatus[Transformation
										.getInternalPosition(position
												+ boardWidth + 1)] == GRAPHIC_NO_GRAPHIC) {
							isNoGraphicRightBelow = true;
						}

						// It's a wall containing a corner. However, this is
						// only relevant if one of the diagonal neighbor squares
						// is a "no graphic" square.
						if (isNoGraphicLeftAbove || isNoGraphicRightAbove
								|| isNoGraphicLeftBelow
								|| isNoGraphicRightBelow) {
							isRelevantCornerWall = true;
						}
					}

					/**
					 * Corner walls can't just be drawn to the screen. They are
					 * drawn cut at every side. Hence, there may be too many
					 * cuts and therefore some additionally parts of the graphic
					 * have to be drawn later, in order to ensure every
					 * necessary part is drawn.
					 */
					if (isRelevantCornerWall) {

						// Draw the graphic with cuts on every side.
						g2D.drawImage(wallGraphics[wallValue],
								destinationAreaX1 + skin.leftBorder,
								destinationAreaY1 + skin.aboveBorder,
								destinationAreaX2 - skin.rightBorder,
								destinationAreaY2 - skin.belowBorder,
								skin.leftBorder, skin.aboveBorder,
								skin.graphicWidth - skin.rightBorder,
								skin.graphicHeight - skin.belowBorder, this);

						// Draw the top of the graphic if there is any other
						// graphic above it.
						if (aboveCutPixel == 0) {
							int leftCut = leftCutPixel > 0
									|| isNoGraphicLeftAbove == true ? skin.leftBorder
									: 0;
							int rightCut = rightCutPixel > 0
									|| isNoGraphicRightAbove == true ? skin.rightBorder
									: 0;
							g2D.drawImage(wallGraphics[wallValue],
									destinationAreaX1 + leftCut,
									destinationAreaY1, destinationAreaX2
											- rightCut, destinationAreaY1
											+ skin.aboveBorder, leftCut, 0,
									skin.graphicWidth - rightCut,
									skin.aboveBorder, this);
						}

						// Draw the bottom of the graphic if there is any other
						// graphic below it.
						if (belowCutPixel == 0) {
							int leftCut = leftCutPixel > 0
									|| isNoGraphicLeftBelow == true ? skin.leftBorder
									: 0;
							int rightCut = rightCutPixel > 0
									|| isNoGraphicRightBelow == true ? skin.rightBorder
									: 0;
							g2D.drawImage(wallGraphics[wallValue],
									destinationAreaX1 + leftCut,
									destinationAreaY2 - skin.belowBorder,
									destinationAreaX2 - rightCut,
									destinationAreaY2, leftCut,
									skin.graphicHeight - skin.belowBorder,
									skin.graphicWidth - rightCut,
									skin.graphicHeight, this);
						}

						// Draw the left side of the graphic if there is any
						// other graphic at the left.
						if (leftCutPixel == 0) {
							g2D.drawImage(wallGraphics[wallValue],
									destinationAreaX1, destinationAreaY1
											+ skin.aboveBorder,
									destinationAreaX1 + skin.leftBorder,
									destinationAreaY2 - skin.belowBorder, 0,
									skin.aboveBorder, skin.leftBorder,
									skin.graphicHeight - skin.belowBorder, this);
						}

						// Draw the right side of the graphic if there is any
						// other graphic at the right.
						if (rightCutPixel == 0) {
							g2D.drawImage(wallGraphics[wallValue],
									destinationAreaX2 - skin.rightBorder,
									destinationAreaY1 + skin.aboveBorder,
									destinationAreaX2, destinationAreaY2
											- skin.belowBorder,
									skin.graphicWidth - skin.rightBorder,
									skin.aboveBorder, skin.graphicWidth,
									skin.graphicHeight - skin.belowBorder, this);
						}
					} else {
						/**
						 * Draw wall graphic to the screen.
						 */
						g2D.drawImage(wallGraphics[wallValue],
								destinationAreaX1 + leftCutPixel,
								destinationAreaY1 + aboveCutPixel,
								destinationAreaX2 - rightCutPixel,
								destinationAreaY2 - belowCutPixel,
								leftCutPixel, aboveCutPixel, skin.graphicWidth
										- rightCutPixel, skin.graphicHeight
										- belowCutPixel, this);
					}

					/*
					 * If a block of 4 walls has occurred draw a beauty graphic
					 * on top of them.
					 *
					 * ## <- block of 4 walls ##
					 */
					if ((wallValue & 5) == 5
							&& x > 0
							&& y > 0
							&& board.isWall(Transformation
									.getInternalPosition(position - boardWidth
											- 1))) {
						g2D.drawImage(skin.wall_beauty_graphic,
								destinationAreaX1 - skin.graphicWidth
										+ skin.beautyGraphicXOffset,
								destinationAreaY1 - skin.graphicHeight
										+ skin.beautyGraphicYOffset, this);
					}
				}
			}
		}

		// Release the system resources.
		g2D.dispose();

		// Return the graphic containing all graphics which won't change during
		// game play.
		return fixedGraphicsImage;
	}

	/**
	 * Sets the flag indication that the sizes must be recalculated. If e.g. we
	 * switch into editor mode, we have less space for the board, since we have
	 * to draw some objects for the editor.
	 */
	final public void recalculateGraphicSizes() {
		isRecalculationNecessary = true;
	}

	/**
	 * Basically, empty squares are drawn, if the player can reach a box or a
	 * goal from them. But then there are levels like Sasquatch 41. In order to
	 * display such levels in a "correct" way, we also "set" the player on all
	 * outside squares with a box or a goal, which the player could not reach,
	 * normally. What is reachable from there also shall be drawn as
	 * "empty square", instead of as background.
	 *
	 * @return boolean array containing the information which squares are to be
	 *         displayed although the player can't reach them (normally the
	 *         background graphic would be displayed at these positions)
	 */
	final private boolean[] identifySpecialSquares() {

		// Array for the special squares
		boolean[] specialSquares = new boolean[board.size];;

		// Stack for positions to be analyzed
		IntStack positionsToBeAnalyzed = new IntStack(board.size);

		// Search all goals and boxes, which do not yet belong to the board.
		for (int position = 0; position < board.size; position++) {

			// Skip all squares which belong to the board, anyhow.
			if (board.isOuterSquareOrWall(position) == false) {
				continue;
			}

			if (board.isGoal(position) || board.isBox(position)) {

				/*
				 * Now we compute the squares which are reachable by the player.
				 * When the player steps onto a boarder square, it is not judged
				 * as "belongs to the board", since border squares are verboten
				 * for the player.
				 */

				// We start at the position of the box or goal
				positionsToBeAnalyzed.clear(); // Just to be sure it's empty
				positionsToBeAnalyzed.add(position);

				while (!positionsToBeAnalyzed.isEmpty()) {
					int playerPosition = positionsToBeAnalyzed.remove();

					// Check whether we have a border square position.
					// If so, the level is not valid, and we do not search more
					// squares
					// from this one.
					if (playerPosition < board.width
							|| playerPosition > board.size - board.width
							|| playerPosition % board.width == 0
							|| playerPosition % board.width == board.width - 1) {
						continue;
					}

					// Mark square as "reached"
					specialSquares[playerPosition] = true;

					// Add all reachable squares if they haven't been reached before.
					for (int direction = 0; direction < DirectionConstants.DIRS_COUNT; direction++) {
						final int newPlayerPosition = board.getPosition(playerPosition, direction);
						if (!board.isWall(newPlayerPosition) && !specialSquares[newPlayerPosition]) {
							positionsToBeAnalyzed.add(newPlayerPosition);
						}
					}
				}
			}
		}

		return specialSquares;
	}

	/**
	 * Returns a Color object of the color represented by the given string.
	 *
	 * @param propertyKey
	 *            String containing the 4 values of the color
	 * @return Colorobject
	 */
	private Color getColor(String propertyKey) {

		// If the skin defines the colors get the colors from there. Otherwise
		// take the colors from the settings.ini file.
		String colorProperty = skin.properties.getProperty(propertyKey);
		if (colorProperty == null) {
			colorProperty = Settings.get(propertyKey);
		}

		String[] colorString = colorProperty.split(",");

		if (colorString.length == 4) {
			try {
				return new Color(Integer.valueOf(colorString[0].trim())
						.intValue(), Integer.valueOf(colorString[1].trim())
						.intValue(), Integer.valueOf(colorString[2].trim())
						.intValue(), Integer.valueOf(colorString[3].trim())
						.intValue());
			} catch (NumberFormatException e) {
			}
		}
		if (colorString.length == 3) {
			try {
				return new Color(Integer.valueOf(colorString[0].trim())
						.intValue(), Integer.valueOf(colorString[1].trim())
						.intValue(), Integer.valueOf(colorString[2].trim())
						.intValue());
			} catch (NumberFormatException e) {
			}
		}

		// Dummy
		return new Color(0, 0, 0);
	}

	@Override
	public final void keyPressed(KeyEvent evt) {
		/* not relevant */
	}

	@Override
	public final void keyTyped(KeyEvent evt) {
		/* not relevant */
	}

	/**
	 * Handling of the key events.
	 *
	 * @param evt
	 *            key event
	 */
	@Override
	public final void keyReleased(KeyEvent evt) {
		// Inform the Sokoban client about the key event.
		application.keyEvent(evt);
	}

	/**
	 * Repaints the GUI immediately. This is the sledge-hammer method:
	 * everything is painted, again.
	 */
	public void paintImmediately() {
		paintImmediately(0, 0, getWidth(), getHeight());
	}

	/**
	 * Repaints this GUI and waits until the repaint is done.
	 */
	public void repaintAndWait() {

		// If were are on the event dispatcher thread then it's easy.
		if (SwingUtilities.isEventDispatchThread()) {
			paintImmediately();
			return;
		}

		// Paint the new board on the EDT and wait until the EDT has finished
		// painting.
		final AtomicBoolean atomicBool = new AtomicBoolean();
		try {
			SwingUtilities.invokeAndWait(new Runnable() {
				@Override
				public void run() {
					paintImmediately();
					atomicBool.set(true);
				}
			});
		} catch (InterruptedException e) {
			Thread.currentThread().interrupt();
		} catch (InvocationTargetException e) { /* just continue. */
		}

		// Wait for the painting to be finished.
		while (atomicBool.get() == false) {
			try {
				Thread.sleep(5);
			} catch (InterruptedException e) {
				Thread.currentThread().interrupt();
			}
		}
	}

	/**
	 * Repaints part of the GUI. Only the passed internal positions are
	 * considered changed.
	 *
	 * @param waitForPainting
	 *            if <code>true</code> then the method returns only if the
	 *            painting has finished - even if the thread has been
	 *            interrupted
	 * @param positions
	 *            internal positions to be repainted
	 */
	public void paintRegion(boolean waitForPainting, int... positions) {

		final MinMaxXY mima = new MinMaxXY();

		// Calculate the union area of all positions to be repainted.
		for (int position : positions) {
			mima.addInternal(position);
		}

		// Nothing to draw? Then return immediately.
		if (mima.isEmpty()) {
			return;
		}

		// Calculate the width and height (in board squares) of the area to be
		// repainted.
		final int xlen = mima.maxX - mima.minX + 1;
		final int ylen = mima.maxY - mima.minY + 1;

		// If were are on the event dispatcher thread then it's easy: just paint
		// the rect.
		if (SwingUtilities.isEventDispatchThread()) {
			paintExtSqRect(mima.minX, mima.minY, xlen, ylen);
			return;
		}

		// Paint the new board on the EDT and wait until the EDT has finished
		// painting if requested.
		final AtomicBoolean atomicBool = new AtomicBoolean();
		try {
			SwingUtilities.invokeAndWait(new Runnable() {
				@Override
				public void run() {
					paintExtSqRect(mima.minX, mima.minY, xlen, ylen);
					atomicBool.set(true);
				}
			});
		} catch (InterruptedException e) {
			Thread.currentThread().interrupt();
		} catch (InvocationTargetException e) { /* just continue. */
		}

		// Wait for the painting to be finished if requested by
		// "waitForPainting".
		while (waitForPainting == true && atomicBool.get() == false) {
			try {
				Thread.sleep(5);
			} catch (InterruptedException e) {
				Thread.currentThread().interrupt();
			}
		}

	}

	/**
	 * Repaints part of the GUI immediately. Only a rectangle inside the board
	 * is considered to be changed, specified in external board square
	 * coordinates. But, the number of moves and pushes has also be considered
	 * to be changed, which implies some more updates.
	 *
	 * @param xmin
	 *            upper left X of board square rectangle
	 * @param ymin
	 *            upper left Y of board square rectangle
	 * @param extWidth
	 *            width of rectangle in board squares
	 * @param extHeight
	 *            height of rectangle in board squares
	 */
	private void paintExtSqRect(int xmin, int ymin, int extWidth, int extHeight) {
		int pixXmin = xOffset + xmin * skin.graphicWidth;
		int pixYmin = yOffset + ymin * skin.graphicHeight;
		int pixWidth = extWidth * skin.graphicWidth;
		int pixHeight = extHeight * skin.graphicHeight;

		if (pixWidth > 0 || pixHeight > 0) {
			repaint(pixXmin, pixYmin, pixWidth, pixHeight);
			paintMovesPushes();
		}
	}

	/**
	 * In this class we collect 2D coordinates by an overall minimum and maximum
	 * value, for both components separately. This is yet another kind of
	 * (abstract) rectangle.
	 *
	 * @author Heiner Marxen
	 */
	private class MinMaxXY {
		int minX;
		int maxX;
		int minY;
		int maxY;

		/**
		 * Set the collected values back to the initial values, indicating an
		 * empty range.
		 */
		public void clear() {
			minX = Integer.MAX_VALUE;
			maxX = Integer.MIN_VALUE;
			minY = Integer.MAX_VALUE;
			maxY = Integer.MIN_VALUE;
		}

		/**
		 * Default constructor sets up an empty area
		 */
		public MinMaxXY() {
			clear();
		}

		/**
		 * Returns whether the collected area is empty.
		 *
		 * @return whether the area is empty
		 */
		public boolean isEmpty() {
			return (minX > maxX) || (minY > maxY);
		}

		/**
		 * Adds a point to the area, given by its components.
		 *
		 * @param x
		 * @param y
		 */
		public void add(int x, int y) {
			minX = Math.min(minX, x);
			maxX = Math.max(maxX, x);
			minY = Math.min(minY, y);
			maxY = Math.max(maxY, y);
		}

		/**
		 * Collect another external position. External positions are what is
		 * handled in the GUI, they reflect screen positions after any
		 * transformation.
		 *
		 * @param externalPos
		 *            <code>-1</code>. or external position to collect
		 */
		public void addExternal(int externalPos) {
			if (externalPos != -1) {
				int externalWidth = Transformation.getOutputLevelWidth();
				int extX = externalPos % externalWidth;
				int extY = externalPos / externalWidth;

				add(extX, extY);
			}
		}

		/**
		 * Collect another internal position (from the model). Before it is
		 * collected, it must be translated to an external position according to
		 * the current transformation.
		 *
		 * @param internalPos
		 *            <code>-1</code>. or internal position to collect
		 */
		public void addInternal(int internalPos) {
			if (internalPos != -1) {
				int externalPos = Transformation
						.getExternalPosition(internalPos);

				addExternal(externalPos);
			}
		}
	}

	/**
	 * Repaints part of the GUI. Just the number of moves and the number of
	 * pushes is considered changed.
	 */
	private void paintMovesPushes() {
		// Moves and pushes are displayed as part of the history slider panel.
		if (historySliderPanel != null) {
			// We have to include the textual representation, as well as the
			// graphical
			// slider representation. That includes most of the area.
			int w = historySliderPanel.getWidth();
			int h = historySliderPanel.getHeight();

			// Redraw the slider. This needn't to be done immediately. Repaint
			// is much
			// faster then paintImmediately.
			historySliderPanel.repaint(0, 0, w, h);
		}
	}

	/**
	 * Returns a <code>JSlider</code> in a panel for browsing through the
	 * movement history.
	 *
	 * @return a <code>JPanel</code> containing the history slider
	 */
	final public JPanel getMovementHistorySlider() {

		// Slider for browsing through the history.
		historySlider = new MovementsHistorySlider();

		// Using the mouse wheel opens the movement history slider and changes
		// its value.
		MouseWheelListener sliderMouseWheelListener = new MouseWheelListener() {

			@Override
			public void mouseWheelMoved(MouseWheelEvent e) {

				// Adjust the slider value according to the scroll direction.
				if (e.getWheelRotation() < 0) {
					application.actionPerformed(new ActionEvent(historySlider,
							0, "jumpToNextHistoryBrowserMovement"));
				} else {
					application.actionPerformed(new ActionEvent(historySlider,
							0, "jumpToPreviousHistoryBrowserMovement"));
				}
			}
		};

		// The slider should be adjustable by using the mouse wheel.
		historySlider.addMouseWheelListener(sliderMouseWheelListener);
		addMouseWheelListener(sliderMouseWheelListener);

		// Create a panel for the slider and return it.
		// In this panel the moves and pushes are displayed besides the slider.
		historySliderPanel = new JPanel(new BorderLayout(0, 0)) {
			@Override
			public final void paintComponent(Graphics graphic) {

				Graphics2D g2D = (Graphics2D) graphic;

				// Draw the background.
				if (skin.isBackgroundImageSupported) {
					g2D.drawImage(backgroundGraphic, 0, 0, null);
				} else {
					for (int x2 = 0; x2 < getWidth(); x2 += skin.graphicWidth) {
						for (int y2 = 0; y2 < getHeight(); y2 += skin.graphicHeight) {
							g2D.drawImage(skin.emptySquare, x2, y2, this);
						}
					}
				}

				// Draw a background to indicate that this is not only the
				// normal background image.
				g2D.setColor(colorBackgroundMovesPushes);
				g2D.fillRoundRect(topInfoBarInset, topInfoBarInset,
						currentWindowWidth - 2 * topInfoBarInset, 30, 20, 25);

				g2D.setFont(new Font("SansSerif", Font.BOLD, 14));

				int movesStringWidth = g2D.getFontMetrics().stringWidth(
						Texts.getText("moves") + ": " + application.movesCount);

				// Draw the number of moves and pushes.
				g2D.setColor(colorDisplayMovesPushes);
				g2D.drawString(Texts.getText("moves") + ": "
						+ application.movesCount, 2 * 10, 30);
				g2D.drawString(Texts.getText("pushes") + ": "
						+ application.pushesCount,
						2 * 10 + movesStringWidth + 20, 30);

				// // Draw level title (if there is enough space that is
				// levelTitleFontsize > 0)
				// Level title is already visible in the combox box
				// if(levelTitleFontSize > 0) {
				// g2D.setFont(new Font("SansSerif", Font.BOLD,
				// levelTitleFontSize));
				// g2D.drawString(getLevelTitleToShow(), levelTitleXPosition,
				// 30);
				// }

				// Display level number.
				g2D.setFont(new Font("SansSerif", Font.BOLD, 14));
				String levelNumberText = Texts.getText("levelnumber") + ": "
						+ application.currentLevel.getNumber() + "/"
						+ application.currentLevelCollection.getLevelsCount();
				int levelNumberStringWidth = g2D.getFontMetrics().stringWidth(
						levelNumberText);
				g2D.drawString(levelNumberText, currentWindowWidth
						- levelNoPixelDistanceRightEdge - topInfoBarInset
						- levelNumberStringWidth, 30);
			}

		};
		historySliderPanel.add(historySlider, BorderLayout.SOUTH);
		historySliderPanel.setPreferredSize(new Dimension(10, 40));
		Texts.helpBroker.enableHelpKey(historySliderPanel, "game-view.HistorySlider", null); // Enable help for slider

		return historySliderPanel;
	}

	private class MovementsHistorySlider extends JSlider {

		// Flag, indicating whether a fired event is a relevant change event.
		protected boolean isEventRelevant = false;

		public MovementsHistorySlider() {

			setOrientation(SwingConstants.HORIZONTAL);
			setUI(new HistorySliderUI(this));
			// Height is important: the higher the easier it is for the user
			// to click into the slider
			setPreferredSize(new Dimension(10, 15));
			setOpaque(false);

			addChangeListener(new ChangeListener() {

				// If the value changes jump immediately to the set movement.
				@Override
				public void stateChanged(ChangeEvent e) {

					// If the editor or the solver is activated the slider
					// mustn't be shown.
					// They both have no own class for displaying the board, yet
					// :(
					if (application.isEditorModeActivated()
							|| application.solverGUI != null
							|| isEventRelevant == false) {
						isEventRelevant = true; // Ensure the flag is set to
												// true for the next event
						return;
					}

					// The user has selected a new value => jump to the selected
					// history movement.
					application.actionPerformed(new ActionEvent(historySlider,
							0, "jumpToMovementHistorySlider"));

					// The application may refuse handling the event due to
					// other pending events.
					// In this case the slider needs to be set back to the
					// current value.
					// Therefore the paint method is called here.
					historySlider.paintImmediately(0, 0, getWidth(),
							getHeight());
				}
			});
		}

		@Override
		public void updateUI() {
			super.updateUI();
			// Always set the own UI.
			setUI(new HistorySliderUI(this));
		}

		/**
		 * An own UI class for the history slider.
		 */
		private class HistorySliderUI extends BasicSliderUI {

			// The left and right border.
			private final int xBorderWidth = 10;
			private MovementsHistorySlider slider;

			public HistorySliderUI(MovementsHistorySlider slider) {
				super(slider);
				this.slider = slider;
				MouseEventHandlerSlider mouseHandlerSlider = new MouseEventHandlerSlider();
				addMouseListener(mouseHandlerSlider);
				addMouseMotionListener(mouseHandlerSlider);

				// Ensure that all L&Fs don't draw a border.
				setBorder(BorderFactory.createEmptyBorder());
				setFocusable(false);
			}

			@Override
			public void paint(Graphics g, JComponent c) {

				Graphics2D g2D = (Graphics2D) g;

				// If the editor or the solver is activated the slider mustn't
				// be shown.
				// They both have no own class for displaying the board, yet :(
				if (application.isEditorModeActivated()
						|| application.solverGUI != null) {
					return;
				}

				// Get the current movementNo and the maximum and compare it
				// with the
				// current slider settings. If the settings have changed they
				// are set
				// to the new values.
				final int movementNo = application.movesHistory
						.getHistoryBrowserMovementNoFromMovementNo();
				final int maxMovements = application.movesHistory
						.getHistoryBrowserMovementsCount();
				if (slider.getMaximum() != maxMovements) {
					slider.isEventRelevant = false;
					slider.setMaximum(maxMovements);
				}
				if (slider.getValue() != movementNo) {
					slider.isEventRelevant = false;
					slider.setValue(movementNo);
				}

				// Get the position of slider.
				int sliderPosition = thumbRect.x - thumbRect.width / 2;

				// A rect is drawn as huge as the one for the background to
				// ensure the
				// round slider corners look exactly like the background rect.
				// However, only a small area of 7 pixels of the slider is
				// really drawn to the screen.
				g2D.setClip(xBorderWidth, 8, sliderPosition, 15);
				g2D.setColor(new Color(166, 213, 245, 100));
				g2D.fillRoundRect(xBorderWidth, -slider.getHeight(),
						currentWindowWidth - 2 * xBorderWidth, 30, 20, 25);
			}

			@Override
			protected void calculateTrackBuffer() {
				// No gap at the top and the bottom of the slider but a border
				// left and right.
				trackBuffer = xBorderWidth;
			}

			@Override
			protected void scrollDueToClickInTrack(int direction) {

				// If the editor or the solver is activated the slider mustn't
				// be shown.
				// They both have no own class for displaying the board, yet :(
				if (application.isEditorModeActivated()
						|| application.solverGUI != null) {
					return;
				}

				int value = slider.getValue();
				if (slider.getOrientation() == JSlider.HORIZONTAL) {
					value = this.valueForXPosition(slider.getMousePosition().x);
				} else if (slider.getOrientation() == JSlider.VERTICAL) {
					value = this.valueForYPosition(slider.getMousePosition().y);
				}
				slider.setValue(value);

				// Simulate a click in the thumb area to start dragging.
				// InputEvent.BUTTON1_MASK is necessary because only left mouse
				// button clicks can start a drag.
				trackListener.mousePressed(new MouseEvent(slider, 0, 0l,
						InputEvent.BUTTON1_MASK, thumbRect.x, thumbRect.y, 1,
						false));
			}

			@Override
			protected Dimension getThumbSize() {
				return new Dimension(10, slider.getHeight());
			}
		}

		private class MouseEventHandlerSlider extends MouseAdapter {

			private String oldText = "";
			private int oldSliderPosition = -1;

			// Timestamp when the mouse entered the slider.
			private long enterTimestamp;

			// Cursor when the mouse entered the slider.
			private Cursor cursorWhenMouseEntered;

			// First reaction delay: If the user just moves through the slider
			// area this shouldn't result in an action.
			// Therefore it's checked whether the user stayed at least some
			// time in the slider area.
			private long actionDelay = 60; // 60 milli seconds

			/*
			 * (non-Javadoc)
			 *
			 * @see
			 * java.awt.event.MouseAdapter#mouseExited(java.awt.event.MouseEvent
			 * )
			 */
			@Override
			public void mouseExited(MouseEvent e) {
				// If the slider value hasn't been changed the game status
				// hasn't changed either. Hence the old info string
				// is still valid => set back the old info string.
				if (((JSlider) e.getSource()).getValue() == oldSliderPosition) {
					displayInfotext(oldText);
				} else {
					// Initialize for next check.
					oldSliderPosition = -1;

					// The slider value has changed. Hence, the status bar has
					// to be erased.
					displayInfotext("");
				}

				// Set back the cursor that had been set when the mouse entered
				// the slider.
				historySlider.setCursor(cursorWhenMouseEntered);
			}

			@Override
			public void mouseEntered(MouseEvent e) {
				oldText = infoString;
				enterTimestamp = e.getWhen();
				cursorWhenMouseEntered = getCursor();
				oldSliderPosition = ((JSlider) e.getSource()).getValue();
			}

			/*
			 * (non-Javadoc)
			 *
			 * @see
			 * java.awt.event.MouseAdapter#mouseMoved(java.awt.event.MouseEvent)
			 */
			@Override
			public void mouseMoved(MouseEvent e) {

				// The info text is only changed if the user stayed at least
				// "actionDelay"
				// milli seconds in the slider.
				if (e.getWhen() - enterTimestamp < actionDelay) {
					return;
				}

				// Display the hand cursor to indicate that the slider can be
				// used for browsing.
				historySlider.setCursor(Cursor
						.getPredefinedCursor(Cursor.HAND_CURSOR));

				HistorySliderUI sliderUI = (HistorySliderUI) ((JSlider) e
						.getSource()).getUI();

				// Calculate the value that would be set for the current mouse
				// position.
				int newSliderValue = sliderUI.valueForXPosition(e.getX());

				int[] movesPushes = application.movesHistory
						.getMovesPushesFromHistoryBrowserMovementNo(newSliderValue);

				// Display text "Click to jump to mmm moves / ppp pushes
				displayInfotext(Texts.getText("historySlider.jumpToMovement",
						movesPushes[0], movesPushes[1]));
			}
		}
	}

	/**
	 * Mouse listener for class MainBoardDisplay.
	 */
	private class MouseEventHandler extends MouseAdapter {

		// Flag indicating whether the mouse is dragged.
		private boolean isMouseDragged = false;

		// Timestamp when the last drag started.
		private long startOfLastDrag = 0;

		public MouseEventHandler() {
		}

		/**
		 * Handles mouse pressed events.
		 *
		 * @param evt
		 *            the MouseEvent that has been fired
		 */
		@Override
		public final void mousePressed(MouseEvent evt) {

			// Request the focus every time the mouse is pressed.
			if (!hasFocus()) {
				requestFocusInWindow();
			}

			boolean mouseHasBeenClickedWithinBoard = true;

			int x = evt.getX();
			int y = evt.getY();

			// Calculate row and column of the mouse click
			int mouseXCoordinate = ((x - xOffset) / skin.graphicWidth);
			int mouseYCoordinate = ((y - yOffset) / skin.graphicHeight);

			// Determine whether the mouse has been clicked within the board.
			// (Note: the calculation above rounds the result. Hence, don't
			// check for mouse coordinate < 0)
			if (mouseXCoordinate >= Transformation.getOutputLevelWidth()
					|| mouseYCoordinate >= Transformation
							.getOutputLevelHeight() || x < xOffset
					|| y < yOffset) {
				mouseHasBeenClickedWithinBoard = false;
			}

			// Let the Sokoban client handle the mouse event.
			application.mousePressedEvent(evt, isMouseDragged,
					mouseXCoordinate, mouseYCoordinate,
					mouseHasBeenClickedWithinBoard);
		}

		/**
		 * Handles the mouse event "mouseDragged".
		 *
		 * @param evt
		 *            the event that has been fired
		 */
		@Override
		public void mouseDragged(MouseEvent evt) {

			if (isMouseDragged == false) {

				startOfLastDrag = evt.getWhen();

				// Save the dragged status.
				isMouseDragged = true;
			}

			mousePressed(evt);
		}

		/*
		 * (non-Javadoc)
		 *
		 * @see
		 * java.awt.event.MouseAdapter#mouseReleased(java.awt.event.MouseEvent)
		 */
		@Override
		public void mouseReleased(MouseEvent evt) {

			// As long as the editor has no extra panel it uses this class, too.
			// Therefore
			// this is special coding: editor doesn't need mouse released
			// events.
			if (application.isEditorModeActivated()) {

				// Releasing the mouse button ends a drag.
				isMouseDragged = false;

				return;
			}

			// The mouse release is relevant in the case it has been dragged.
			if (isMouseDragged == true) {

				// Sometimes one clicks and thereby moves the mouse a little bit
				// which
				// starts a drag. However, the intended action is just a simple
				// click.
				// Therefore it's checked here how long the drag has lasted. If
				// it is less
				// than 100 milliseconds it is ignored and therefore treated as
				// simple click.
				if ((evt.getWhen() - startOfLastDrag) < 100) {
					return;
				}

				// Releasing the mouse button ends a drag.
				isMouseDragged = false;

				// Handle the release the same as a press.
				mousePressed(evt);
			}
		}
	}

	/**
	 * Handles drag&drop for class MainBoardDisplay.
	 */
	private class DropTargetHandler extends DropTargetAdapter {

		public DropTargetHandler() {
		}

		/*
		 * (non-Javadoc)
		 *
		 * @see
		 * java.awt.dnd.DropTargetAdapter#dragOver(java.awt.dnd.DropTargetDragEvent
		 * )
		 */
		@Override
		public void dragOver(DropTargetDragEvent dtde) {

			DataFlavor[] dataFlavors = dtde.getCurrentDataFlavors();

			// Check if there is a supported flavor.
			for (DataFlavor currentFlavor : dataFlavors) {
				if ((DataFlavor.javaFileListFlavor.equals(currentFlavor) || DataFlavor.stringFlavor
						.equals(currentFlavor))
						&& (dtde.getSourceActions() & DnDConstants.ACTION_COPY_OR_MOVE) != 0) {
					return;
				}
			}

			// There hasn't been found any supported flavor or the action is not
			// wrong.
			// Therefore the drag is rejected.
			dtde.rejectDrag();
		}

		/*
		 * (non-Javadoc)
		 *
		 * @see
		 * java.awt.dnd.DropTargetListener#drop(java.awt.dnd.DropTargetDropEvent
		 * )
		 */
		@Override
		public void drop(DropTargetDropEvent dtde) {
			application.dropEvent(dtde);
		}

	}
}