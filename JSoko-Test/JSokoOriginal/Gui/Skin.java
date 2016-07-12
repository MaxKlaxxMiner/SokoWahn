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

import java.awt.AlphaComposite;
import java.awt.Color;
import java.awt.Graphics2D;
import java.awt.Point;
import java.awt.Transparency;
import java.awt.image.BufferedImage;
import java.io.BufferedReader;
import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.DataOutputStream;
import java.io.File;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.InputStream;
import java.util.ArrayList;
import java.util.List;
import java.util.Properties;

import javax.imageio.ImageIO;

import de.sokoban_online.jsoko.board.DirectionConstants;
import de.sokoban_online.jsoko.resourceHandling.Settings;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.BASE64;
import de.sokoban_online.jsoko.utilities.Debug;
import de.sokoban_online.jsoko.utilities.GraphicUtilities;
import de.sokoban_online.jsoko.utilities.Utilities;


/**
 * Class containing all data of a skin.
 * A skin contains the graphics that are drawn to the screen when a board is shown.
 * <p>
 * Currently the variables are public for easier access.
 */
 public class Skin implements DirectionConstants {
	
	/** Skin information: name of the skin */
	public String name;
	/** Skin information: author of the skin */
	public String author;
	/** Skin information: copyright */
	public String copyright;
	/** Skin information: descriptive text */
	public String description;
	/** Skin information: web home page */
	public String website;
	/** Skin information: email address */
	public String email;
	
	/** Path to the settings file of the skin */
	public String settingsFilePath;
	
	/** Properties of this skin. */
	Properties properties = new Properties();
	
	/**
	 * Borders of the graphics. These borders are used to cut pixels from the
	 * graphics if an "outside" square (= a square where only the background
	 * image is shown) is next to it.
	 * This is needed because the graphics contain the core element to be
	 * shown and also some parts of an empty square graphic.
	 * The parts that contain the empty square colors mustn't be shown
	 * when an outside square is next to the current square.
	 */
	int aboveBorder = 0;
	int belowBorder = 0;
	int leftBorder  = 0;
	int rightBorder = 0;
	
	/** Width of the skin graphics. */
	public int graphicWidth;
	
	/** Height of the skin graphics. */
	public int graphicHeight;
	
	/**
	 * Flag, indicating whether the skin supports showing a background image.
	 * If set to false the empty square graphic is drawn as background.
	 */
	boolean isBackgroundImageSupported = true;
		
	/** Graphics for the player looking in every direction (up, down, left, right). */
	public BufferedImage[] playerWithViewInDirection = new BufferedImage[4];
	
	/** Graphics for the player on a goal looking in every direction (up, down, left, right). */
	public BufferedImage[] playerOnGoalWithViewInDirection = new BufferedImage[4];
	
	/** Graphic for a box. */
	public BufferedImage box;
	
	/** Graphic for a box on a goal. */
	public BufferedImage boxOnGoal;
	
	/** Graphic for drawing an empty square. */
	public BufferedImage emptySquare;
	
	/** Graphic for a goal. */
	public BufferedImage goal;
	
	/**
	 * Wall having no neighbor wall.
	 * <pre>
	 * ---
	 * -#-
	 * ---</pre>
	 */
	BufferedImage wall_no_neighbor;
	
	/**
	 * Walls having one neighbor wall.
	 * <pre>
	 * -#-  ---  ---  ---
	 * -#-  -#-  ##-  -##
	 * ---  -#-  ---  ---</pre>
	 */
	BufferedImage wall_neighbor_above;
	BufferedImage wall_neighbor_below;
	BufferedImage wall_neighbor_left;
	BufferedImage wall_neighbor_right;
			
	/**
	 * Walls having two neighbor walls.
	 * <pre>
	 * -#-  -#-  -#-  ---  ---  ---
	 * -#-  ##-  -##  ##-  -##  ###
	 * -#-  ---  ---  -#-  -#-  ---</pre>
	 */
	BufferedImage wall_neighbor_above_below;
	BufferedImage wall_neighbor_above_left;
	BufferedImage wall_neighbor_above_right;
	BufferedImage wall_neighbor_below_left;
	BufferedImage wall_neighbor_below_right;
	BufferedImage wall_neighbor_left_right;
	
	/**
	 * Walls having three neighbor walls.
	 * <pre>
	 * -#-  -#-  -#-  ---
	 * ###  -##  ##-  ###
	 * ---  -#-  -#-  -#-</pre>
	 */
	BufferedImage wall_neighbor_above_left_right;
	BufferedImage wall_neighbor_above_below_right;
	BufferedImage wall_neighbor_above_below_left;
	BufferedImage wall_neighbor_below_left_right;
	
	
	/**
	 * Wall having four neighbors.
	 * <pre>
	 * -#-
	 * ###
	 * -#-</pre>
	 */
	BufferedImage wall_neighbor_above_below_left_right;
	
	/**
	 *  Wall for blocks of 4 walls. This wall image is drawn
	 *  in the middle of such a 4-walls block to hide the 
	 *  hole in the middle of the wall graphics.
	 * <pre> 
	 *  ##-
	 *  ##-
	 *  ---</pre> 
	 */
	BufferedImage wall_beauty_graphic;
	
	/**
	 * Location of the beauty graphic.
	 */
	int beautyGraphicXOffset;
	int beautyGraphicYOffset; 

	/**
	 * Graphics for animations of the player and the box.
	 */
	ArrayList<BufferedImage> playerAnimation 	   = new ArrayList<BufferedImage>();
	ArrayList<BufferedImage> playerOnGoalAnimation = new ArrayList<BufferedImage>();
	ArrayList<BufferedImage> boxAnimation 		   = new ArrayList<BufferedImage>();
	ArrayList<BufferedImage> boxOnGoalAnimation    = new ArrayList<BufferedImage>();
	
	/** Deadlock squares are displayed a little bit darker than the other squares. */
	BufferedImage deadlockSquare;
	BufferedImage boxOnDeadlockSquare;
	BufferedImage[] playerOnDeadlockSquareWithDirection = new BufferedImage[4];
	
	/** If the reachable squares of the player or a box are shown
	 *  special graphics are used, which are stored here.
	 */
	BufferedImage reachableBox;
	BufferedImage reachableBoxOnGoal;
	BufferedImage reachablePlayerSquare;
	BufferedImage reachablePlayerSquareOnGoal;
	
	/**
	 * Copy constructor. 
	 */
	private Skin() {}
	
	/**
	 * Creates a new skin object and loads the skin corresponding to the passed skin settings file.
	 * 
	 * @param pathToSkinSettingsFile  absolute or relative path to the properties file of the skin
	 * @throws FileNotFoundException either the properties file or the skin graphic file hasn't been found
	 */
	public Skin(String pathToSkinSettingsFile) throws FileNotFoundException {
		
		// BufferedReader for reading the settings of the skin.
		BufferedReader settings = null;
		
		try {
			// This method may be used for loading skin from any path. However, the default settings
			// have only relative paths. Hence, first try to load the file from the class path.
			settings = Utilities.getBufferedReader(pathToSkinSettingsFile);	
			
			// Load the settings.
			properties.load(settings);
			
		} catch (Exception e) {
			throw new FileNotFoundException(Texts.getText("message.fileMissing", pathToSkinSettingsFile));
		}
		finally {
			if(settings != null) {
				try {
					settings.close();
				} catch (IOException e) {}
			}
		}
		
		// Path to the settings file of the skin.
		settingsFilePath = pathToSkinSettingsFile;
		
		// If it is a skin in the skn-format use special coding for loading it.
		if(pathToSkinSettingsFile.endsWith(".skn")) {
			try {
				// Pass a new BufferedReader to the loading method
				// (the other BufferedReader has already been read to the end).
				loadSKNSkin(Utilities.getBufferedReader(pathToSkinSettingsFile));
			} catch (IOException e) {
				if(Debug.isDebugModeActivated) {
					e.printStackTrace();
				}
			}
		} else {
			// Load the graphics of the skin.
			loadSkinOtherFormat(settings, pathToSkinSettingsFile);
		}
		
		// Create additionally needed graphics.
		createAdditionalGraphics();
		
		// Set "unknown" as name and author if there isn't anything else set, yet.
		if(name == null || name.isEmpty()) {
			name   = Texts.getText("unknown", "Unknown");
		}
		if(author == null || author.isEmpty()) {
			author = Texts.getText("unknown", "Unknown");
		}
		
		// DEBUG: if set then draw own skin graphics.
		if(Debug.debugDrawOwnSkin) {
			debugModedrawOwnSkinGraphics();
		}
	}
	
	/**
	 * Scales the skin graphics to the passed size and returns the scaled graphics
	 * as object of the class <code>Skin</code>.
	 * <p>
	 * The graphic width and height variables are set to the new size.
	 * However, all properties of the skin remain as they are!
	 * 
	 * @param scaledGraphicsWidth  width  to scale the graphics to
	 * @param scaledGraphicsHeight height to scale the graphics to
	 *
	 * @return scaled version of the passed <code>Skin</code>
	 */
	public Skin getScaledVersion(int scaledGraphicsWidth, int scaledGraphicsHeight) {
		
		// Create a new skin which is filled with the scaled graphics.
		Skin scaledSkin = new Skin();
		
		// Copy all none image variables.
		scaledSkin.name			= name;
		scaledSkin.author		= author;
		scaledSkin.copyright	= copyright;
		scaledSkin.description  = description;
		scaledSkin.website 		= website;
		scaledSkin.email		= email;
		scaledSkin.settingsFilePath = settingsFilePath;
		scaledSkin.properties = new Properties();
		scaledSkin.isBackgroundImageSupported = isBackgroundImageSupported;
		
		/** New size of the graphics. */
		scaledSkin.graphicWidth  = scaledGraphicsWidth;
		scaledSkin.graphicHeight = scaledGraphicsHeight;
		
		// Player on empty square.
		scaledSkin.playerWithViewInDirection = new BufferedImage[playerWithViewInDirection.length];
		for(int graphicNo=0; graphicNo < playerWithViewInDirection.length; graphicNo++) {
			scaledSkin.playerWithViewInDirection[graphicNo] = GraphicUtilities.getScaledInstance(playerWithViewInDirection[graphicNo], scaledGraphicsWidth, scaledGraphicsHeight);
		}
		
		// Player on deadlock square.
		scaledSkin.playerOnDeadlockSquareWithDirection = new BufferedImage[playerOnDeadlockSquareWithDirection.length];
		for(int graphicNo=0; graphicNo<playerOnDeadlockSquareWithDirection.length; graphicNo++) {
			scaledSkin.playerOnDeadlockSquareWithDirection[graphicNo] = GraphicUtilities.getScaledInstance(playerOnDeadlockSquareWithDirection[graphicNo], scaledGraphicsWidth, scaledGraphicsHeight);
		}
		
		// Player on goal.
		scaledSkin.playerOnGoalWithViewInDirection = new BufferedImage[playerOnGoalWithViewInDirection.length];
		for(int graphicNo=0; graphicNo<playerOnGoalWithViewInDirection.length; graphicNo++) {
			scaledSkin.playerOnGoalWithViewInDirection[graphicNo] = GraphicUtilities.getScaledInstance(playerOnGoalWithViewInDirection[graphicNo], scaledGraphicsWidth, scaledGraphicsHeight);
		}
		
		scaledSkin.box 		 = GraphicUtilities.getScaledInstance(box, scaledGraphicsWidth, scaledGraphicsHeight);
		scaledSkin.boxOnGoal = GraphicUtilities.getScaledInstance(boxOnGoal, scaledGraphicsWidth, scaledGraphicsHeight);
		scaledSkin.boxOnDeadlockSquare = GraphicUtilities.getScaledInstance(boxOnDeadlockSquare, scaledGraphicsWidth, scaledGraphicsHeight);
		
		scaledSkin.emptySquare = GraphicUtilities.getScaledInstance(emptySquare, scaledGraphicsWidth, scaledGraphicsHeight);
		
		scaledSkin.deadlockSquare = GraphicUtilities.getScaledInstance(deadlockSquare, scaledGraphicsWidth, scaledGraphicsHeight); 
		
		scaledSkin.goal = GraphicUtilities.getScaledInstance(goal, scaledGraphicsWidth, scaledGraphicsHeight);
		
		scaledSkin.wall_no_neighbor    = GraphicUtilities.getScaledInstance(wall_no_neighbor, scaledGraphicsWidth, scaledGraphicsHeight);
		scaledSkin.wall_neighbor_above = GraphicUtilities.getScaledInstance(wall_neighbor_above, scaledGraphicsWidth, scaledGraphicsHeight);
		scaledSkin.wall_neighbor_below = GraphicUtilities.getScaledInstance(wall_neighbor_below, scaledGraphicsWidth, scaledGraphicsHeight);
		
		
		scaledSkin.wall_neighbor_left 					= GraphicUtilities.getScaledInstance(wall_neighbor_left, scaledGraphicsWidth, scaledGraphicsHeight);
		scaledSkin.wall_neighbor_right 					= GraphicUtilities.getScaledInstance(wall_neighbor_right, scaledGraphicsWidth, scaledGraphicsHeight);
		scaledSkin.wall_neighbor_above_below 			= GraphicUtilities.getScaledInstance(wall_neighbor_above_below, scaledGraphicsWidth, scaledGraphicsHeight);
		scaledSkin.wall_neighbor_above_left 			= GraphicUtilities.getScaledInstance(wall_neighbor_above_left, scaledGraphicsWidth, scaledGraphicsHeight);
		scaledSkin.wall_neighbor_above_right 			= GraphicUtilities.getScaledInstance(wall_neighbor_above_right, scaledGraphicsWidth, scaledGraphicsHeight);
		scaledSkin.wall_neighbor_below_left 			= GraphicUtilities.getScaledInstance(wall_neighbor_below_left, scaledGraphicsWidth, scaledGraphicsHeight);
		scaledSkin.wall_neighbor_below_right 			= GraphicUtilities.getScaledInstance(wall_neighbor_below_right, scaledGraphicsWidth, scaledGraphicsHeight);
		scaledSkin.wall_neighbor_left_right 			= GraphicUtilities.getScaledInstance(wall_neighbor_left_right, scaledGraphicsWidth, scaledGraphicsHeight);
		scaledSkin.wall_neighbor_above_left_right 	    = GraphicUtilities.getScaledInstance(wall_neighbor_above_left_right, scaledGraphicsWidth, scaledGraphicsHeight);
		scaledSkin.wall_neighbor_above_below_right 	    = GraphicUtilities.getScaledInstance(wall_neighbor_above_below_right, scaledGraphicsWidth, scaledGraphicsHeight);
		scaledSkin.wall_neighbor_above_below_left 	    = GraphicUtilities.getScaledInstance(wall_neighbor_above_below_left, scaledGraphicsWidth, scaledGraphicsHeight);
		scaledSkin.wall_neighbor_below_left_right		= GraphicUtilities.getScaledInstance(wall_neighbor_below_left_right, scaledGraphicsWidth, scaledGraphicsHeight);
		scaledSkin.wall_neighbor_above_below_left_right = GraphicUtilities.getScaledInstance(wall_neighbor_above_below_left_right, scaledGraphicsWidth, scaledGraphicsHeight);
		scaledSkin.wall_beauty_graphic 					= GraphicUtilities.getScaledInstance(wall_beauty_graphic, scaledGraphicsWidth, scaledGraphicsHeight);
		
		// Player animation graphics.
		scaledSkin.playerAnimation = new ArrayList<BufferedImage>();
		for(BufferedImage image : playerAnimation) {
			scaledSkin.playerAnimation.add(GraphicUtilities.getScaledInstance(image, scaledGraphicsWidth, scaledGraphicsHeight));
		}
		
		// Player on goal animation graphics.
		scaledSkin.playerOnGoalAnimation = new ArrayList<BufferedImage>();
		for(BufferedImage image : playerOnGoalAnimation) {
			scaledSkin.playerOnGoalAnimation.add(GraphicUtilities.getScaledInstance(image, scaledGraphicsWidth, scaledGraphicsHeight));
		}
		
		// Box animation graphics.
		scaledSkin.boxAnimation = new ArrayList<BufferedImage>();
		for(BufferedImage image : boxAnimation) {
			scaledSkin.boxAnimation.add(GraphicUtilities.getScaledInstance(image, scaledGraphicsWidth, scaledGraphicsHeight));
		}
			
		// Box on goal animation graphics.
		scaledSkin.boxOnGoalAnimation = new ArrayList<BufferedImage>();
		for(BufferedImage image : boxOnGoalAnimation) {
			scaledSkin.boxOnGoalAnimation.add(GraphicUtilities.getScaledInstance(image, scaledGraphicsWidth, scaledGraphicsHeight));
		}
				
		// The graphics for showing the reachable squares are only 70% of the size compared to the other graphics.
		int scaledWidth  = (int) Math.round(scaledGraphicsWidth  * 0.7);
		int scaledHeight = (int) Math.round(scaledGraphicsHeight * 0.7);
		scaledSkin.reachableBox 	     	   = GraphicUtilities.getScaledInstance(reachableBox, scaledWidth, scaledHeight);
		scaledSkin.reachableBoxOnGoal	 	   = GraphicUtilities.getScaledInstance(reachableBoxOnGoal, scaledWidth, scaledHeight);
		scaledSkin.reachablePlayerSquare 	   = GraphicUtilities.getScaledInstance(reachablePlayerSquare, scaledWidth, scaledHeight);
		scaledSkin.reachablePlayerSquareOnGoal = GraphicUtilities.getScaledInstance(reachablePlayerSquareOnGoal, scaledWidth, scaledHeight);
		 
		/**
		 * Scale the borders, too.
		 * These borders are used to cut pixels from the graphics if an "outside" square (= a square 
		 * where only the background image is shown) is next to it.
		 */
		float scaleFactor = (float) scaledGraphicsWidth / graphicWidth;
		scaledSkin.aboveBorder = Math.round(aboveBorder * scaleFactor);
		scaledSkin.belowBorder = Math.round(belowBorder * scaleFactor);
		scaledSkin.leftBorder  = Math.round(leftBorder  * scaleFactor);
		scaledSkin.rightBorder = Math.round(rightBorder * scaleFactor);
		
		// The beauty graphic has to be repositioned, too.
		scaledSkin.beautyGraphicXOffset = Math.round(beautyGraphicXOffset * scaleFactor);
		scaledSkin.beautyGraphicYOffset = Math.round(beautyGraphicYOffset * scaleFactor);
		
		return scaledSkin;
	}
	
	/**
	 * Loads the graphics for the skin and creates every graphic necessary for displaying the skin.
	 * <p>
	 * Either: The graphics to be used are located in a huge single graphic. All graphic images are extracted
	 * from this entire graphic and saved in an array.
	 * Or: the skin offers all needed graphics ready to use in single graphic files.
	 *  
	 * @param skinSettingsFileReader  <code>BufferedReader</code> containing the graphics of the skin
	 * @param skinSettingsFilePath  absolute or relative path to the properties file of the skin
	 * @throws FileNotFoundException  file could not be loaded exception
	 */
	private void loadSkinOtherFormat(BufferedReader skinSettingsFileReader, String skinSettingsFilePath) throws FileNotFoundException {
		
		/** 
		 * Get the settings of the skin. 
		 */		
		// The width and height of the graphics.
		graphicWidth  = getInt("real_width",  50);
		graphicHeight = getInt("real_height", 50);
		
		/** Extract the border values of the graphics. */
		aboveBorder = getInt("topBorder",    0);
		belowBorder = getInt("bottomBorder", 0);
		leftBorder  = getInt("leftBorder",   0);
		rightBorder = getInt("rightBorder",  0);
		
		// Flag, indicating whether the empty square graphic is drawn as background or a real graphic
		isBackgroundImageSupported = getBool("isBackgroundImageSupported", true); 
		
		int nameSeparatorIndex = skinSettingsFilePath.lastIndexOf(File.separator);
		if(nameSeparatorIndex == -1) {
			nameSeparatorIndex = skinSettingsFilePath.lastIndexOf("/");
		}
		
		String graphicFilePath = skinSettingsFilePath.substring(0, nameSeparatorIndex) + "/" + getString("combined_file");
									
		// BufferedImage used for several actions.
		BufferedImage biTemp;
		
		// Load the image containing all graphics of the skin.
		BufferedImage entireGraphic;
		InputStream inputStream = null;
		try {
			inputStream = Utilities.getInputStream(graphicFilePath);
			entireGraphic = ImageIO.read(inputStream);
		} catch (Exception e1) {
			throw new FileNotFoundException(Texts.getText("message.fileMissing", graphicFilePath));
		}
		finally {
			if(inputStream != null)
				try {
					inputStream.close();
				} catch (IOException e) {}
		}
		
		/** Extract the graphics from the entire graphic. */
		playerWithViewInDirection[UP] 				= extractGraphicTwoCoordinates(entireGraphic, "mover_up");
		playerWithViewInDirection[DOWN] 			= extractGraphicTwoCoordinates(entireGraphic, "mover_down");
		playerWithViewInDirection[LEFT] 			= extractGraphicTwoCoordinates(entireGraphic, "mover_left");
		playerWithViewInDirection[RIGHT] 			= extractGraphicTwoCoordinates(entireGraphic, "mover_right");
		playerOnGoalWithViewInDirection[UP] 		= extractGraphicTwoCoordinates(entireGraphic, "mover_up_store");
		playerOnGoalWithViewInDirection[DOWN] 		= extractGraphicTwoCoordinates(entireGraphic, "mover_down_store");
		playerOnGoalWithViewInDirection[LEFT] 		= extractGraphicTwoCoordinates(entireGraphic, "mover_left_store");
		playerOnGoalWithViewInDirection[RIGHT] 	= extractGraphicTwoCoordinates(entireGraphic, "mover_right_store");
		box 												= extractGraphicTwoCoordinates(entireGraphic, "object");
		boxOnGoal 											= extractGraphicTwoCoordinates(entireGraphic, "object_store");
		emptySquare 										= extractGraphicTwoCoordinates(entireGraphic, "ground");
		goal 												= extractGraphicTwoCoordinates(entireGraphic, "store");
		wall_neighbor_above_below_left_right 				= extractGraphicTwoCoordinates(entireGraphic, "wall_u_d_l_r");
		wall_neighbor_left_right 							= extractGraphicTwoCoordinates(entireGraphic, "wall_l_r");
		wall_neighbor_above_below 							= extractGraphicTwoCoordinates(entireGraphic, "wall_u_d");
		wall_no_neighbor									= extractGraphicTwoCoordinates(entireGraphic, "wall");
		wall_beauty_graphic 								= extractGraphicTwoCoordinates(entireGraphic, "wall_top");
				
		/** Some skins may also offer all wall graphics. Hence, try to load them, too. */
		wall_neighbor_above_right 							= extractGraphicTwoCoordinates(entireGraphic, "wall_u_r");
		wall_neighbor_above_left_right 						= extractGraphicTwoCoordinates(entireGraphic, "wall_u_l_r");
		wall_neighbor_above_left 							= extractGraphicTwoCoordinates(entireGraphic, "wall_u_l");
		wall_neighbor_above_below_right 					= extractGraphicTwoCoordinates(entireGraphic, "wall_u_d_r");
		wall_neighbor_above_below_left 						= extractGraphicTwoCoordinates(entireGraphic, "wall_u_d_l");
		wall_neighbor_below_right 							= extractGraphicTwoCoordinates(entireGraphic, "wall_d_r");
		wall_neighbor_below_left_right 						= extractGraphicTwoCoordinates(entireGraphic, "wall_d_l_r");
		wall_neighbor_below_left 							= extractGraphicTwoCoordinates(entireGraphic, "wall_d_l");
		wall_neighbor_right 								= extractGraphicTwoCoordinates(entireGraphic, "wall_r");
		wall_neighbor_left 									= extractGraphicTwoCoordinates(entireGraphic, "wall_l");
		wall_neighbor_above 								= extractGraphicTwoCoordinates(entireGraphic, "wall_u");
		wall_neighbor_below 								= extractGraphicTwoCoordinates(entireGraphic, "wall_d");
		
		// Offset of the beauty graphic. As default the beauty graphic is drawn centered.
		beautyGraphicXOffset = getInt("wall_top_x", graphicWidth  / 2);
		beautyGraphicYOffset = getInt("wall_top_y", graphicHeight / 2);
		
		// Box animation graphics.
		for(int i=1; ; i++) {
			biTemp = extractGraphicTwoCoordinates(entireGraphic, "object_animation"+i);
			if(biTemp == null)
				break;
			
			// Add the graphic as animation graphic.
			boxAnimation.add(biTemp);
		}
		
		// Box on goal animation graphics.
		for(int i=1; ; i++) {
			biTemp = extractGraphicTwoCoordinates(entireGraphic, "object_store_animation"+i);
			if(biTemp == null)
				break;
			
			// Add the graphic as animation graphic.
			boxOnGoalAnimation.add(biTemp);
		}
	}
	
	/**
	 * Loads the skin represented by the passed file. The skin must have the "skn" format.
	 * 
	 * @param skinSettingsFile <code>BufferedReader</code> for the file containing the skin data
	 * @throws IOException occurred IOException while reading the skin data
	 */
	private void loadSKNSkin(BufferedReader skinSettingsFile) throws IOException {
		
		/** General skin information */
		name        = getString("Title", Texts.getText("unknown"));
		author      = getString("Copyright", Texts.getText("unknown"));
		copyright   = getString("Copyright", "");
//		description = skin doesn't support a description
		website	 	= getString("Url", "");
		email		= getString("Email", "");
		
		// Flag, indicating whether the empty square graphic is drawn as background or a real graphic
		isBackgroundImageSupported = getBool("isBackgroundImageSupported", true);
		
		// Graphic containing all graphics of the skin.
		BufferedImage entireGraphic;
		
		// ArrayList, for storing the read data.
		ArrayList<String> skinSettingsData = new ArrayList<String>(1000);
		
		// Stores one data row read from the file.
		String dataRow;
		
		// Read in line by line of the settings file.
		try {
			while ((dataRow = skinSettingsFile.readLine()) != null) {
				skinSettingsData.add(dataRow);
			}
		} finally {
			skinSettingsFile.close();
		}
		
		// Data of the image containing all skin graphics as list of strings and as StringBuilder.
		List<String> imageData = null;
		StringBuilder imageDataString = new StringBuilder();
		
		// Get a sublist of all image data. The image data is stored between the key strings:
		// [IMAGE] and [END]
		int start = skinSettingsData.indexOf("[IMAGE]");
		int end   = skinSettingsData.indexOf("[END]");
		imageData = skinSettingsData.subList(start+1, end);
		
		// Concat the data to one huge string.
		for(String s : imageData) {
			imageDataString = imageDataString.append(s);
		}
		
		// The image data is base64 encoded. It is now decoded. 
		ByteArrayOutputStream baos = new ByteArrayOutputStream(); 
		DataOutputStream out = new DataOutputStream( baos );
		BASE64 decoder = new BASE64();
		try {
			out.write(decoder.decodeBuffer(imageDataString.toString()));
		} 
		finally {
			out.close();
		}
		
		// Debug: write data to file.
//		File f = new File("./bin/test2.txt");
//		baos.writeTo(new FileOutputStream(f));
		
		// Create a buffered image from the data.
		// An EOF Exception or any other results in the skin not being loaded.
		try {
			entireGraphic = ImageIO.read(new ByteArrayInputStream(baos.toByteArray()));
		}
		catch(Exception e) {
			if(Debug.isDebugModeActivated) {
				e.printStackTrace();
			}
			return;
		}

		// DEBUG: write the bitmap to a file.
//		ImageIO.write(entireGraphic, "bmp", new File("./bin/test.bmp"));
		
		// The width and height of the graphics.
		graphicWidth  = entireGraphic.getHeight();
		graphicHeight = graphicWidth;
							
		// Load the image containing all graphics of the skin. This is implemented when skins occur
		// that have the graphic file outside the skn-file.
//		try {
//			entireGraphic = ImageIO.read(...);
//		} catch (Exception e1) {
//			throw new FileNotFoundException(Texts.getText("message.fileMissing", graphicFile.getPath()));
//		}
		
		/** Extract the graphics from the entire graphic. */
		playerWithViewInDirection[UP] 				= extractGraphicOneCoordinate(entireGraphic, "Man_Up");
		playerWithViewInDirection[DOWN] 			= extractGraphicOneCoordinate(entireGraphic, "Man_Down");
		playerWithViewInDirection[LEFT] 			= extractGraphicOneCoordinate(entireGraphic, "Man_Left");
		playerWithViewInDirection[RIGHT] 			= extractGraphicOneCoordinate(entireGraphic, "Man_Right");
		playerOnGoalWithViewInDirection[UP] 		= extractGraphicOneCoordinate(entireGraphic, "Man_Up_Goal");
		playerOnGoalWithViewInDirection[DOWN] 		= extractGraphicOneCoordinate(entireGraphic, "Man_Down_Goal");
		playerOnGoalWithViewInDirection[LEFT] 		= extractGraphicOneCoordinate(entireGraphic, "Man_Left_Goal");
		playerOnGoalWithViewInDirection[RIGHT] 	= extractGraphicOneCoordinate(entireGraphic, "Man_Right_Goal");
		box 												= extractGraphicOneCoordinate(entireGraphic, "Pack");
		boxOnGoal 											= extractGraphicOneCoordinate(entireGraphic, "Pack_Goal");
		emptySquare 										= extractGraphicOneCoordinate(entireGraphic, "Floor");
		goal 												= extractGraphicOneCoordinate(entireGraphic, "Goal");
		wall_neighbor_above_below_left_right 				= extractGraphicOneCoordinate(entireGraphic, "Wall_F");
		wall_neighbor_left_right 							= extractGraphicOneCoordinate(entireGraphic, "Wall_A");
		wall_neighbor_above_below 							= extractGraphicOneCoordinate(entireGraphic, "Wall_5");
		wall_no_neighbor									= extractGraphicOneCoordinate(entireGraphic, "Wall_0");
		wall_beauty_graphic 								= extractGraphicOneCoordinate(entireGraphic, "Wall_Top");
				
		/** Some skins may also offer all wall graphics. Hence, try to load them, too. */
		wall_neighbor_above_right 							= extractGraphicOneCoordinate(entireGraphic, "Wall_3");
		wall_neighbor_above_left_right 						= extractGraphicOneCoordinate(entireGraphic, "Wall_B");
		wall_neighbor_above_left 							= extractGraphicOneCoordinate(entireGraphic, "Wall_9");
		wall_neighbor_above_below_right 					= extractGraphicOneCoordinate(entireGraphic, "Wall_7");
		wall_neighbor_above_below_left 						= extractGraphicOneCoordinate(entireGraphic, "Wall_D");
		wall_neighbor_below_right 							= extractGraphicOneCoordinate(entireGraphic, "Wall_6");
		wall_neighbor_below_left_right 						= extractGraphicOneCoordinate(entireGraphic, "Wall_E");
		wall_neighbor_below_left 							= extractGraphicOneCoordinate(entireGraphic, "Wall_C");
		wall_neighbor_right 								= extractGraphicOneCoordinate(entireGraphic, "Wall_2");
		wall_neighbor_left 									= extractGraphicOneCoordinate(entireGraphic, "Wall_8");
		wall_neighbor_above 								= extractGraphicOneCoordinate(entireGraphic, "Wall_1");
		wall_neighbor_below 								= extractGraphicOneCoordinate(entireGraphic, "Wall_4");
		
		// If a single wall graphic is in the skin this wall is used for as graphic for all wall graphics.
		BufferedImage wall = extractGraphicOneCoordinate(entireGraphic, "Wall");
		if(wall != null) {
			wall_neighbor_above_below_left_right = wall_neighbor_left_right = wall_neighbor_above_below =
			wall_no_neighbor = wall_neighbor_above_right  = wall_neighbor_above_left_right =
			wall_neighbor_above_left = wall_neighbor_above_below_right = wall_neighbor_above_below_left = wall_neighbor_below_right =
			wall_neighbor_below_left_right = wall_neighbor_below_left = wall_neighbor_right =  wall_neighbor_left =
			wall_neighbor_above = wall_neighbor_below = wall;
		}
		
		// Use the man graphic for all directions if there aren't any graphics for the man, yet.
		BufferedImage man = extractGraphicOneCoordinate(entireGraphic, "Man");
		if(man != null && playerWithViewInDirection[UP] == null) {
			playerWithViewInDirection[UP] = playerWithViewInDirection[DOWN] =
			playerWithViewInDirection[LEFT] = playerWithViewInDirection[RIGHT] = man;
		}
		
		// If only one "man on goal"-graphic is in the skin this graphic is used for all directions.
		BufferedImage man_on_goal = extractGraphicOneCoordinate(entireGraphic, "Man_Goal");
		if(man_on_goal != null && playerOnGoalWithViewInDirection[UP] == null) {
			playerOnGoalWithViewInDirection[UP] = playerOnGoalWithViewInDirection[DOWN] =
			playerOnGoalWithViewInDirection[LEFT] = playerOnGoalWithViewInDirection[RIGHT] = man_on_goal;
		}
		
		
		/**
		 * Some of the images have transparent colors. These images have to be made transparent now.
		 * To support all skin types the transparency is determined in two ways:
		 * 1. compare a graphic with the basis graphic (example: compare "box graphic" with "empty square" graphic.
		 * 2. take the left top color of the graphic and make this color transparent in the whole graphic
		 */
		// Get the information which graphics have to be transparent.
		String isImageTransparent = getString("Transparent", "");
	
		for(int imageNo=0; imageNo<isImageTransparent.length(); imageNo++) {
			
			// Only if the character is a "1" the graphic is transparent.
			if(isImageTransparent.charAt(imageNo) != '1') {
				continue;
			}
			
			// Wall
			if(imageNo == getInt("Wall", -1)) {
				continue;
			}

			// Empty square
			if(imageNo == getInt("Floor", -1)) {
				emptySquare = GraphicUtilities.makeColorTransparent00(emptySquare);
				continue;
			}

			// Goal
			if(imageNo == getInt("Goal", -1)) {
				goal = getTransparentGraphic(goal, emptySquare, 5); 
				continue;
			}			
			
			// Box on goal
			if(imageNo == getInt("Pack_Goal", -1)) {
				boxOnGoal = GraphicUtilities.makeColorTransparent00(boxOnGoal);
				boxOnGoal = getTransparentGraphic(boxOnGoal, goal, 5);
				continue;
			}
			
			// Player on goal
			if(imageNo == getInt("Man_Up_Goal", -1) || imageNo == getInt("Man_Goal", -1)) {
				playerOnGoalWithViewInDirection[UP] = GraphicUtilities.makeColorTransparent00(playerOnGoalWithViewInDirection[UP]);
				playerOnGoalWithViewInDirection[UP] = getTransparentGraphic(playerOnGoalWithViewInDirection[UP], goal, 5);
			}
			
			if(imageNo == getInt("Man_Down_Goal", -1) || imageNo == getInt("Man_Goal", -1)) {
				playerOnGoalWithViewInDirection[DOWN] = GraphicUtilities.makeColorTransparent00(playerOnGoalWithViewInDirection[DOWN]);
				playerOnGoalWithViewInDirection[DOWN] = getTransparentGraphic(playerOnGoalWithViewInDirection[DOWN], goal, 5);
			}
			
			if(imageNo == getInt("Man_Left_Goal", -1) || imageNo == getInt("Man_Goal", -1)) {
				playerOnGoalWithViewInDirection[LEFT] = GraphicUtilities.makeColorTransparent00(playerOnGoalWithViewInDirection[LEFT]);
				playerOnGoalWithViewInDirection[LEFT] = getTransparentGraphic(playerOnGoalWithViewInDirection[LEFT], goal, 5);
			}
			
			if(imageNo == getInt("Man_Right_Goal", -1) || imageNo == getInt("Man_Goal", -1)) {
				playerOnGoalWithViewInDirection[RIGHT] = GraphicUtilities.makeColorTransparent00(playerOnGoalWithViewInDirection[RIGHT]);
				playerOnGoalWithViewInDirection[RIGHT] = getTransparentGraphic(playerOnGoalWithViewInDirection[RIGHT], goal, 5);
			}
		}	
		
		/**
		 * JSoko needs some of the graphics as transparent ones in any case.
		 * These graphics are created now.
		 */
		
		// Box
		box = GraphicUtilities.makeColorTransparent00(box);
		box = getTransparentGraphic(box, emptySquare, 5);

		// Player on empty square.
		for(int direction=0; direction<4; direction++) {
			playerWithViewInDirection[direction] = GraphicUtilities.makeColorTransparent00(playerWithViewInDirection[direction]);
			playerWithViewInDirection[direction] = getTransparentGraphic(playerWithViewInDirection[direction], emptySquare, 5);
		}

		// If the skin uses the same graphic for player and player on goal this means the graphics have to be transparent.
		if(getInt("MAN", -1) == getInt("Man_Goal", -2) || getInt("Man_Up", -1) == getInt("Man_Up_Goal", -2)) {
			for(int direction=0; direction<4; direction++) {
				playerOnGoalWithViewInDirection[direction] = playerWithViewInDirection[direction];
			}
		}
		
		// Some skins don't have special graphics for player on goal. In this case the player has a transparent graphic.
		// At this point the player graphic has already been made transparent, therefore just take the existing graphics.
		for(int direction=0; direction<4; direction++) {
			if(playerOnGoalWithViewInDirection[direction] == null) {
				playerOnGoalWithViewInDirection[direction] = playerWithViewInDirection[direction];
			}	
		}
		
		// Get the values for the wall cap (beauty graphic) offset.
		beautyGraphicXOffset = getInt("Wall_Top_X", -1);
		beautyGraphicYOffset = getInt("Wall_Top_Y", -1);
		
		if(beautyGraphicXOffset == -1) {
			beautyGraphicXOffset = graphicWidth / 2;
		}
		if(beautyGraphicYOffset == -1) {
			beautyGraphicYOffset = graphicWidth / 2;
		}
		
		try {
			// Some additional information have to be read from an extra settings file for the skn-skins.
			// File the settings of the skin are stored in.
			Properties additionalSettings = new Properties();
			additionalSettings.load(Utilities.getBufferedReader(Settings.get("skinAdditionalSettingsForSkn")));
			
			// Get the settings of this skin.
			String value = additionalSettings.getProperty(name.replaceAll(" ", ""));
			if(value != null) {
				// The number of pixels to be cut from the graphic is stored with a "," as separator.
				String[] values = value.split(",");
				if(values.length == 4) {
					aboveBorder = Integer.parseInt(values[0].trim());
					belowBorder = Integer.parseInt(values[1].trim());
					leftBorder  = Integer.parseInt(values[2].trim());
					rightBorder = Integer.parseInt(values[3].trim());
				}
			}
		} catch (Exception e) {}
	}


	/**
	 * Returns a transparent graphic containing the source image having transparent pixels
	 * at those positions where the color of the pixel of the source image and the color
	 * of the "compare image" are akin with regard of the passed tolerance value.
	 * <p>
	 * Therefore the "sourceImage" is copied and all pixels are set transparent that have
	 * a color akin to the color of the passed image "imageToBeComparedWith".
	 * Two colors are akin enough to cause a transparent result pixel, if all 3 color
	 * components (R, G and B) differ by less than the tolerance value.
	 * The alpha component of both images is ignored for this comparison.
	 *           
	 * @param sourceImage	the image which is the basis of the image to be returned 
	 * @param imageToBeComparedWith	the image to be compared with
	 * @param toleranceValue	the tolerance value
	 * @return   <code>Image</code> for displaying reachable squares of a box
	 */
	private BufferedImage getTransparentGraphic(BufferedImage sourceImage, BufferedImage imageToBeComparedWith, int toleranceValue) {
					
		// Copy the source image to the target image.
		BufferedImage imageToBeReturned = new BufferedImage(sourceImage.getWidth(),
				                                            sourceImage.getHeight(),
				                                            BufferedImage.TYPE_INT_ARGB);
		Graphics2D biContext = imageToBeReturned.createGraphics();
		biContext.setComposite(AlphaComposite.Src);	
		biContext.drawImage(sourceImage, null, 0, 0);		// copy "sourceImage"
		
		// If the color of a pixel is akin to the color of the corresponding pixel
		// in the "compare image" then make the pixel 100% transparent.
		// For this we delete the alpha byte at selected pixels (we use TYPE_INT_ARGB).
		for (int col = imageToBeReturned.getWidth(); --col >= 0; ) {
			for (int row = imageToBeReturned.getHeight(); --row >= 0; ) {
	
				// Get the color of the pixel in both graphics
				final int sourceColor  = sourceImage.getRGB(col, row);
				final int compareColor = imageToBeComparedWith.getRGB(col, row);
	
				// Extract R, G and B components of both pixels
				final int compareRed   = (compareColor >>> 16) & 0xFF;
				final int compareGreen = (compareColor >>>  8) & 0xFF;
				final int compareBlue  = (compareColor >>>  0) & 0xFF;
	
				final int red   = (sourceColor >>> 16) & 0xFF;
				final int green = (sourceColor >>>  8) & 0xFF;
				final int blue  = (sourceColor >>>  0) & 0xFF;

				// If the colors differ less than toleranceValue the pixel is set 100% transparent.
				if( Math.abs(compareRed   - red)   < toleranceValue &&
					Math.abs(compareGreen - green) < toleranceValue &&
					Math.abs(compareBlue  - blue)  < toleranceValue)
				{
					// delete alpha byte...
					imageToBeReturned.setRGB(col, row, (sourceColor & 0x00FFFFFF));
				}
			}
		}

		return imageToBeReturned;
	}
	
	/**
	 * Some of the graphics have to be created using parts of other graphics,
	 * because the skin graphic image doesn't contain all graphics.
	 * This method creates all additionally needed graphics.
	 */
	private void createAdditionalGraphics() {
		
		// BufferedImage used for several actions.
		BufferedImage biTemp;
		
		// Graphic context of a buffered image.
		Graphics2D g2D;
		
		/*
		 * Because of shadows in the graphics the graphics can't just be rotated but have to
		 * be created in multiple steps. Furthermore some graphics have to be created using parts
		 * of the other graphics.
		 */
		// The corner area of the graphics are needed to create new graphics.
		int corner = getInt("borderSize", 1);
		int border = graphicWidth - corner;
		
		BufferedImage boundedAbove     = wall_neighbor_left_right.getSubimage(0, 0, graphicWidth, corner);
		BufferedImage boundedBelow     = wall_neighbor_left_right.getSubimage(0, border, graphicWidth, corner);
		BufferedImage boundedLeft      = wall_neighbor_above_below.getSubimage(0, 0, corner, graphicHeight);
		BufferedImage boundedRight     = wall_neighbor_above_below.getSubimage(border, 0, corner, graphicHeight);
		BufferedImage wallJustLeft     = wall_no_neighbor.getSubimage(border, 0, corner, graphicHeight);
		BufferedImage wallJustRight    = wall_no_neighbor.getSubimage(0, 0, corner, graphicHeight);
		BufferedImage wallJustBelow    = wall_no_neighbor.getSubimage(0, 0, graphicWidth, corner);
		BufferedImage wallJustAbove    = wall_no_neighbor.getSubimage(0, border, graphicWidth, corner);
		BufferedImage cornerLeftAbove  = wall_no_neighbor.getSubimage(0, 0, corner, corner);
		BufferedImage cornerRightAbove = wall_no_neighbor.getSubimage(border, 0, corner, corner);
		BufferedImage cornerLeftBelow  = wall_no_neighbor.getSubimage(0, border, corner, corner);
		BufferedImage cornerRightBelow = wall_no_neighbor.getSubimage(border, border, corner, corner);
		
		
		/**
		 *  Now the missing wall graphics are created.
		 */
		// wall above and right.
		if(wall_neighbor_above_right == null) {
			biTemp = new BufferedImage(graphicWidth, graphicHeight, BufferedImage.TYPE_INT_RGB);
			g2D = biTemp.createGraphics();
			g2D.drawImage(wall_neighbor_above_below_left_right, 0, 0, null);
			g2D.drawImage(boundedBelow, 0, border, null);
			g2D.drawImage(boundedLeft, 0, 0, null);
			g2D.drawImage(cornerLeftBelow, 0, border, null);
			wall_neighbor_above_right = biTemp;
			g2D.dispose();
		}
	
		// wall above, left and right.
		if(wall_neighbor_above_left_right == null) {
			biTemp = new BufferedImage(graphicWidth, graphicHeight, BufferedImage.TYPE_INT_RGB);
			g2D = biTemp.createGraphics();
			g2D.drawImage(wall_neighbor_above_below_left_right, 0, 0, null);
			g2D.drawImage(boundedBelow, 0, border, null);
			wall_neighbor_above_left_right = biTemp;
			g2D.dispose();
		}
	
		// wall above and left.
		if(wall_neighbor_above_left == null) {
			biTemp = new BufferedImage(graphicWidth, graphicHeight, BufferedImage.TYPE_INT_RGB);
			g2D = biTemp.createGraphics();
			g2D.drawImage(wall_neighbor_above_below_left_right, 0, 0, null);
			g2D.drawImage(boundedBelow, 0, border, null);
			g2D.drawImage(boundedRight, border, 0, null);
			g2D.drawImage(cornerRightBelow, border, border, null);
			wall_neighbor_above_left = biTemp;
			g2D.dispose();
		}
	
		// wall above, below and right.
		if(wall_neighbor_above_below_right == null) {
			biTemp = new BufferedImage(graphicWidth, graphicHeight, BufferedImage.TYPE_INT_RGB);
			g2D = biTemp.createGraphics();
			g2D.drawImage(wall_neighbor_above_below_left_right, 0, 0, null);
			g2D.drawImage(boundedLeft, 0, 0, null);
			wall_neighbor_above_below_right = biTemp;
			g2D.dispose();
		}
	
		// wall above, below and left.
		if(wall_neighbor_above_below_left == null) {
			biTemp = new BufferedImage(graphicWidth, graphicHeight, BufferedImage.TYPE_INT_RGB);
			g2D = biTemp.createGraphics();
			g2D.drawImage(wall_neighbor_above_below_left_right, 0, 0, null);
			g2D.drawImage(boundedRight, border, 0, null);
			wall_neighbor_above_below_left = biTemp;
			g2D.dispose();
		}
	
		// wall below and right.
		if(wall_neighbor_below_right == null) {
			biTemp = new BufferedImage(graphicWidth, graphicHeight, BufferedImage.TYPE_INT_RGB); 
			g2D = biTemp.createGraphics();
			g2D.drawImage(wall_neighbor_above_below_left_right, 0, 0, null);
			g2D.drawImage(boundedLeft, 0, 0, null);
			g2D.drawImage(boundedAbove, 0, 0, null);
			g2D.drawImage(cornerLeftAbove, 0, 0, null);
			wall_neighbor_below_right = biTemp;
			g2D.dispose();
		}
	
		// wall below, left and right.
		if(wall_neighbor_below_left_right == null) {
			biTemp = new BufferedImage(graphicWidth, graphicHeight, BufferedImage.TYPE_INT_RGB);
			g2D = biTemp.createGraphics();
			g2D.drawImage(wall_neighbor_above_below_left_right, 0, 0, null);
			g2D.drawImage(boundedAbove, 0, 0, null);
			wall_neighbor_below_left_right = biTemp;
			g2D.dispose();
		}
	
		// wall below and left.
		if(wall_neighbor_below_left == null) {
			biTemp = new BufferedImage(graphicWidth, graphicHeight, BufferedImage.TYPE_INT_RGB);
			g2D = biTemp.createGraphics();
			g2D.drawImage(wall_neighbor_above_below_left_right, 0, 0, null);
			g2D.drawImage(boundedAbove, 0, 0, null);
			g2D.drawImage(boundedRight, border, 0, null);
			g2D.drawImage(cornerRightAbove, border, 0, null);
			wall_neighbor_below_left = biTemp;
			g2D.dispose();
		}
	
		// wall right.
		if(wall_neighbor_right == null) {
			biTemp = new BufferedImage(graphicWidth, graphicHeight, BufferedImage.TYPE_INT_RGB);
			g2D = biTemp.createGraphics();
			g2D.drawImage(wall_neighbor_left_right, 0, 0, null);
			g2D.drawImage(wallJustRight, 0, 0, null);
			wall_neighbor_right = biTemp;
			g2D.dispose();
		}
	
		// wall left.
		if(wall_neighbor_left == null) {
			biTemp = new BufferedImage(graphicWidth, graphicHeight, BufferedImage.TYPE_INT_RGB);
			g2D = biTemp.createGraphics();
			g2D.drawImage(wall_neighbor_left_right, 0, 0, null);
			g2D.drawImage(wallJustLeft, border, 0, null);
			wall_neighbor_left = biTemp;
			g2D.dispose();
		}
	
		// wall above.
		if(wall_neighbor_above == null) {
			biTemp = new BufferedImage(graphicWidth, graphicHeight, BufferedImage.TYPE_INT_RGB);
			g2D = biTemp.createGraphics();
			g2D.drawImage(wall_neighbor_above_below, 0, 0, null);
			g2D.drawImage(wallJustAbove, 0, border, null);
			wall_neighbor_above = biTemp;
			g2D.dispose();
		}
	
		// wall below.
		if(wall_neighbor_below == null) {
			biTemp = new BufferedImage(graphicWidth, graphicHeight, BufferedImage.TYPE_INT_RGB);
			g2D = biTemp.createGraphics();
			g2D.drawImage(wall_neighbor_above_below, 0, 0, null);
			g2D.drawImage(wallJustBelow, 0, 0, null);
			wall_neighbor_below = biTemp;
			g2D.dispose();
		}
		
		// Deadlock squares are displayed shaded. These graphics are created now.
		BufferedImage bi = new BufferedImage(graphicWidth, graphicHeight, BufferedImage.TYPE_INT_RGB);
		Graphics2D biContext = bi.createGraphics();
		biContext.drawImage(emptySquare, 0, 0, null);
		GraphicUtilities.changeLightness(bi, -30); // all colors - 30
		deadlockSquare = bi;
	
		// Create shadowed player graphics for all 4 directions.
		for (int viewDirection = 0; viewDirection < 4; viewDirection++) {
			bi = new BufferedImage(graphicWidth, graphicHeight, BufferedImage.TYPE_INT_RGB);
			biContext = bi.createGraphics();
			biContext.drawImage(emptySquare, 0, 0, null);
			biContext.drawImage(playerWithViewInDirection[viewDirection], 0, 0, null);
			GraphicUtilities.changeLightness(bi, -30);
			playerOnDeadlockSquareWithDirection[viewDirection] = bi;
		}
		
		// Create a shadowed box on empty square graphic.
		bi = new BufferedImage(graphicWidth, graphicHeight, BufferedImage.TYPE_INT_ARGB);
		biContext = bi.createGraphics();
		biContext.drawImage(emptySquare, 0, 0, null);
		biContext.drawImage(box, 0, 0, null);
		GraphicUtilities.changeLightness(bi, -30);
		boxOnDeadlockSquare = bi;
		
		// Create the graphics for showing the reachable squares of a box / of the player.
		reachableBox = box.getTransparency() == Transparency.TRANSLUCENT ? box : getTransparentGraphic(box, emptySquare, 5);
		reachableBoxOnGoal = boxOnGoal.getTransparency() == Transparency.TRANSLUCENT ? boxOnGoal : getTransparentGraphic(boxOnGoal, goal, 5); 
		reachablePlayerSquare = playerWithViewInDirection[0].getTransparency() == Transparency.TRANSLUCENT ? playerWithViewInDirection[0] : getTransparentGraphic(playerWithViewInDirection[0], emptySquare, 5);
		reachablePlayerSquareOnGoal = playerOnGoalWithViewInDirection[0].getTransparency() == Transparency.TRANSLUCENT ? playerOnGoalWithViewInDirection[0] : getTransparentGraphic(playerOnGoalWithViewInDirection[0], goal, 5);
		
		// These graphics are only 70% of the size compared to the other graphics.
		// They are made transparent so the user immediately can see
		// that these graphics aren't "real" boxes.
		int scaledWidth  = (int) Math.round(graphicWidth  * 0.7);
		int scaledHeight = (int) Math.round(graphicHeight * 0.7);
		reachableBox = GraphicUtilities.getTransparentImage(GraphicUtilities.getScaledInstance(reachableBox, scaledWidth, scaledHeight), 0.4f);
		reachableBoxOnGoal = GraphicUtilities.getTransparentImage(GraphicUtilities.getScaledInstance(reachableBoxOnGoal, scaledWidth, scaledHeight), 0.4f);	
		reachablePlayerSquare = GraphicUtilities.getTransparentImage(GraphicUtilities.getScaledInstance(reachablePlayerSquare, scaledWidth, scaledHeight), 0.4f);
		reachablePlayerSquareOnGoal = GraphicUtilities.getTransparentImage(GraphicUtilities.getScaledInstance(reachablePlayerSquareOnGoal, scaledWidth, scaledHeight), 0.4f);
		
		
		// The beauty graphic (wall cap) is flipped horizontally and vertically for a better look.
		//
		// 12  becomes  43
		// 34           21
		if(wall_beauty_graphic != null) {
			
			int halfWidth  = graphicWidth  / 2;
			int halfHeight = graphicHeight / 2;
						
			BufferedImage flippedBeautyGraphic = new BufferedImage(wall_beauty_graphic.getWidth(), wall_beauty_graphic.getHeight(), BufferedImage.TYPE_4BYTE_ABGR);
			g2D = flippedBeautyGraphic.createGraphics();
			
			// Quadrant 4 is drawn to quadrant 1.
			g2D.drawImage(wall_beauty_graphic,
					      0, 0, halfWidth, halfHeight,
					      halfWidth, halfHeight, graphicWidth, graphicHeight, null);
			
			// Quadrant 3 is drawn to quadrant 2.
			g2D.drawImage(wall_beauty_graphic,
					      halfWidth, 0, graphicWidth, halfHeight,
					      0, halfHeight, halfWidth, graphicHeight, null);
			
			// Quadrant 2 is drawn to quadrant 3.
			g2D.drawImage(wall_beauty_graphic,
					      0, halfHeight, halfWidth, graphicHeight,
					      halfWidth, 0, graphicWidth, halfHeight, null);
			
			// Quadrant 1 is drawn to quadrant 4.
			g2D.drawImage(wall_beauty_graphic,
					      halfWidth, halfHeight, graphicWidth, graphicHeight,
					      0, 0, halfWidth, halfHeight, null);
			
			// The new graphic becomes the beauty graphic.
			wall_beauty_graphic = flippedBeautyGraphic;
			
			g2D.dispose();
		}
	}
	
	/**
	 * Extracts the graphic specified by the passed name from the passed graphic
	 * which contains all graphics of the skin.
	 * <p>
	 * This method is only used for reducing the lines of code when extracting
	 * the graphics from the entire graphic containing all skin graphics.
	 * This method assumes that the position of the graphic to be extracted
	 * is stored in the settings as "x, y" coordinates.
	 * 
	 * @param entireGraphic  skin graphic containing all skin graphics
	 * @param graphicName  the name of the graphic to extract
	 * @return the extracted <code>BufferedImage</code>
	 */
	private BufferedImage extractGraphicTwoCoordinates(BufferedImage entireGraphic, String graphicName) {
	
		try {
			Point graphicCoordinates = getPoint(graphicName);
			if(graphicCoordinates != null) {
				return entireGraphic.getSubimage(graphicCoordinates.x * graphicWidth,
						                         graphicCoordinates.y * graphicHeight,
						                         graphicWidth, graphicHeight);
			}
		} catch (Exception e) {
			if(Debug.isDebugModeActivated) {
				e.printStackTrace();
			}
		}
		
		return null;
	}
	
	/**
	 * Extracts the graphic specified by the passed name from the passed graphic
	 * which contains all graphics of the skin.
	 * <p>
	 * This method is only used for reducing the lines of code when extracting
	 * the graphics from the entire graphic containing all skin graphics.
	 * This method assumes that only the x-coordinate is relevant for locating
	 * a sub graphic in the entire graphic. 
	 * 
	 * @param entireGraphic  skin graphic containing all skin graphics
	 * @param graphicName  the name of the graphic to extract
	 * @return the extracted <code>BufferedImage</code>
	 */
	private BufferedImage extractGraphicOneCoordinate(BufferedImage entireGraphic, String graphicName) {
	
		try {
			int xCoordinate = getInt(graphicName, -1);
			if(xCoordinate == -1) {
				return null;
			}
			return entireGraphic.getSubimage(xCoordinate * graphicWidth, 0, graphicWidth, graphicHeight);
		} catch (Exception e) {
			if(Debug.isDebugModeActivated) {
				e.printStackTrace();
			}
		}
		
		return null;
	}
	
	/**
	 * Erase any comments of the passed property string.
	 * A comment starts at the first '#' (hash sign).
	 * Also, blanks and tabs are trimmed from the (right) end of the result.
	 * 
	 * @param propertyValue value of a property as a String
	 * @return trimmed value
	 */
	private String trimValue(String propertyValue) {
	
		if (propertyValue == null || propertyValue.length() == 0) {
			return "";
		}
	
		int lastpos = propertyValue.indexOf('#');
		if (lastpos == -1) {
			lastpos = propertyValue.length() - 1;
		}
		
		
		for(; lastpos >= 0 ; --lastpos ) {
			char c = propertyValue.charAt(lastpos);
			if (c != ' ' && c != '\t') {
				break;
			}
		}
		
		// Now, "lastpos" indexes the last char to be retained (or -1 if there is none)
		
		// Return the trimmed value.
		return propertyValue.substring(0, lastpos + 1);
	}
	
	/**
	 * Returns the value of the property corresponding to the passed name as an <code>int</code>.
	 * 
	 * @param name name of property
	 * @param defaultValue value to be set if the property value can't be set
	 * 
	 * @return <code>int</code> value of the property
	 */
	private int getInt(String name, int ... defaultValue) {
	
		// Get the value of the property.
		String propertyValue = trimValue(properties.getProperty(name));
	
		// If the the property couldn't be found set the default value if there is one.
		if (propertyValue == null) {
			if(defaultValue.length > 0) {
				return defaultValue[0];
			}
		}
		
		try {
			return Integer.parseInt(propertyValue);
		}catch(Exception e) {
			if(defaultValue.length > 0) {
				return defaultValue[0];
			}
			throw(new NumberFormatException(e.getLocalizedMessage()));
		}
	}
	
	/**
	 * Returns the value of the property corresponding to the passed name as an <code>Point</code>.
	 * <p>
	 * The integers of the property string to be read must be separated by a ",".
	 * 
	 * @param name name of property
	 * @param defaultValue values to be set if the property values can't be set
	 * 
	 * @return <code>Point</code> containing the two values of the property
	 */
	private Point getPoint(String name, Point ... defaultValue) {
	
		// Get the value of the property.
		String propertyValue = trimValue(properties.getProperty(name));
	
		// If the the property couldn't be found set the default value if there is one.
		if (propertyValue == null) {
			if(defaultValue.length > 0) {
				return defaultValue[0];
			}
			return null;
		}			
	
		// Split the property string into two strings containing the integers.
		String[] ints = propertyValue.split(","); 
		if(ints.length != 2 && defaultValue.length > 0) {
			return defaultValue[0];
		}
		
		try {
			// Return the integers as a point.
			return new Point(Integer.parseInt(ints[0].trim()), Integer.parseInt(ints[1].trim()));
		}catch(NumberFormatException e) {
			if(defaultValue.length > 0) {
				return defaultValue[0];
			}
			return null;
		}
	}
	
	/**
	 * Returns the value of the property corresponding to the passed name
	 * as an <code>boolean</code>.
	 * 
	 * @param name name of property	
	 * @param defaultValue value to be set if the property value can't be set
	 * 
	 * @return <code>true</code> if the property value equals string "true"<br>
	 *        <code>false</code> otherwise
	 */
	private boolean getBool(String name, boolean ...defaultValue) {
	
		// Get the value of the property.
		String propertyValue = trimValue(properties.getProperty(name));
	
		// If the the property couldn't be found set the default value.
		if (propertyValue == null || propertyValue.isEmpty()) {
			if(defaultValue.length > 0) {
				return defaultValue[0];
			}
		}
	
		// Return whether the property contains the value "true".
		return ("true".equals(propertyValue));
	}
	
	/**
	 * Returns the string corresponding to the passed property name.
	 * 
	 * @param name name of property	
	 * @param defaultValue value to be set if the property value can't be set
	 * @return value of the property as string or null, if no property is found
	 */
	private String getString(String name, String ... defaultValue) {
	
		// Get the value of the property.
		String propertyValue = trimValue(properties.getProperty(name));
	
		// If the the property couldn't be found set the default value.
		if (propertyValue == null) {
			if(defaultValue.length > 0) {
				return defaultValue[0];
			}
		}
	
		return propertyValue;
	}
	
	
	/**
	 * Method for testing drawing the skin graphics using Java coding.
	 * This method is only used when the corresponding checkbox in the debug menu is set.
	 */
	private void debugModedrawOwnSkinGraphics() {
		
		// Create a new empty square having a darker border.
		BufferedImage biTemp = new BufferedImage(50, 50, BufferedImage.TYPE_INT_RGB);
		Graphics2D g2D = biTemp.createGraphics();
		g2D.drawImage(emptySquare, 0, 0, null);
		g2D.setColor(Color.darkGray.brighter());
		g2D.fillRect(0, 0, 50, 50);
		g2D.setColor(Color.gray);
		g2D.fillRect(1, 1, 49, 49);
		emptySquare = biTemp;
		g2D.dispose();
		
		// Create the box graphic.
		biTemp = new BufferedImage(50, 50, BufferedImage.TYPE_INT_RGB);
		g2D = biTemp.createGraphics();
		g2D.drawImage(emptySquare, 0, 0, null);
		java.awt.GradientPaint Cyclic = new java.awt.GradientPaint (10F, 10F, Color.blue, 30F, 30F, Color.red, true);
		g2D.setPaint (Cyclic);
		g2D.fillRect(10, 10, 30, 30);
		box = biTemp;
		g2D.dispose();
		
		// Create the goal graphic.
		biTemp = new BufferedImage(50, 50, BufferedImage.TYPE_INT_RGB);
		g2D = biTemp.createGraphics();
		g2D.drawImage(emptySquare, 0, 0, null);
		g2D.setColor(Color.red);
		java.awt.geom.Point2D center = new java.awt.geom.Point2D.Float(biTemp.getWidth()/2, biTemp.getHeight()/2);
	    float radius = biTemp.getWidth();
	    float[] dist = {0.1f, 0.75f};
	    Color[] colors = {Color.WHITE,  Color.darkGray};
	    java.awt.RadialGradientPaint rp = new java.awt.RadialGradientPaint(center, radius, dist, colors);
	    g2D.setPaint(rp);
	    g2D.fillOval(5, 5, 40, 40);
		goal = biTemp;
		g2D.dispose();
		
		// Create the box on goal graphic.
		biTemp = new BufferedImage(50, 50, BufferedImage.TYPE_INT_RGB);
		g2D = biTemp.createGraphics();
		g2D.drawImage(goal, 0, 0, null);
		Cyclic = new java.awt.GradientPaint (10F, 10F, Color.orange, 30F, 30F, Color.red, true);
		g2D.setPaint (Cyclic);
		g2D.fillRect(10, 10, 30, 30);
		boxOnGoal = biTemp;
		g2D.dispose();
		
		// Create the player graphics.
		biTemp = new BufferedImage(50, 50, BufferedImage.TYPE_INT_RGB);
		g2D = biTemp.createGraphics();
		g2D.drawImage(emptySquare, 0, 0, null);
		g2D.setColor(Color.blue);
		Cyclic = new java.awt.GradientPaint (5F, 5F, Color.blue, 10F, 10F, Color.green, true);
	    g2D.setPaint (Cyclic);
	    g2D.fillOval(10, 10, 30, 30);
		for( int i=0; i<4; i++ ) {
			playerWithViewInDirection[i] = biTemp;
		}
		g2D.dispose();
		
		// Create the player on goal graphics.
		biTemp = new BufferedImage(50, 50, BufferedImage.TYPE_INT_RGB);
		g2D = biTemp.createGraphics();
		g2D.drawImage(goal, 0, 0, null);
		// Draw player
		g2D.setColor(Color.blue);
		Cyclic = new java.awt.GradientPaint (5F, 5F, Color.blue, 10F, 10F, Color.green, true);
	    g2D.setPaint (Cyclic);
	    g2D.fillOval(10, 10, 30, 30);
		for( int i=0; i<4; i++ ) {
			playerOnGoalWithViewInDirection[i] = biTemp;
		}
		g2D.dispose();
		
		// Create the wall graphic.
		biTemp = new BufferedImage(50, 50, BufferedImage.TYPE_INT_RGB);
		g2D = biTemp.createGraphics();
		Color wallC;
		//wallC = Color.white;				// mm proposal
		//wallC = Color.magenta;
		//wallC = new Color(0xCD2626);		// firebrick3
		wallC = new Color(0x8B1A1A);		// firebrick4 (hm favorite)
		Cyclic = new java.awt.GradientPaint (0F, 0F, wallC, 10F, 10F, Color.darkGray, true);
		g2D.setPaint (Cyclic);
	    g2D.fillRect (0, 0, 50, 50);
		wall_no_neighbor=
		 wall_neighbor_above=
		 wall_neighbor_below=
		 wall_neighbor_left=
		 wall_neighbor_right=
		 wall_neighbor_above_below=
		 wall_neighbor_above_left=
		 wall_neighbor_above_right=
		 wall_neighbor_below_left=
		 wall_neighbor_below_right=		
		 wall_neighbor_left_right=
		 wall_neighbor_above_left_right=
		 wall_neighbor_above_below_right=
		 wall_neighbor_above_below_left=
		 wall_neighbor_below_left_right=
		 wall_neighbor_above_below_left_right=
		 wall_beauty_graphic= biTemp;
		g2D.dispose();
		 
		wall_beauty_graphic= null; // no beauty graphic needed for this skin
		 
		/*
		 * Graphics for animations of the box.
		 */
		boxAnimation.clear();
		for(int i=0; i<10; i++) {
			boxAnimation.add(GraphicUtilities.getLightnessChangedImage(box, 6*i));
		}
		boxOnGoalAnimation.clear();	
		for(int i=0; i<10; i++) {
			boxOnGoalAnimation.add(GraphicUtilities.getLightnessChangedImage(box, 6*i));
		}
		
		// Create additional graphics depending on the created ones.
		createAdditionalGraphics();
		 
		aboveBorder = 0;
		belowBorder = 0;
		leftBorder  = 0;
		rightBorder = 0;
	}
}