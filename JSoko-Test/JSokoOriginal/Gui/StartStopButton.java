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

import java.awt.Color;
import javax.swing.BorderFactory;
import javax.swing.JButton;
import javax.swing.border.Border;
import javax.swing.border.LineBorder;

import de.sokoban_online.jsoko.resourceHandling.Texts;


/**
 * This class specializes a {@link JButton} to have a start- and stop-text,
 * along with a corresponding pair of action commands.
 * <p>
 * We also optionally add a colored border, different for start and stop.
 * <p>
 * We also optionally add a background color to the button,
 * different for start and stop.
 * But the look and feel is free to ignore button colors,
 * so this may have no effect.
 * <p>
 * Here we do NOT maintain a status of the button, nor do we perform any
 * actions bound to it.  We merely help to set up its GUI properties.
 * 
 * @author Heiner Marxen
 */
@SuppressWarnings("serial")
public class StartStopButton extends JButton {

	/**
	 * Our default color (RGB) for the border for "start".
	 */
	public static final int borderColorStartDft = 0x88CC88;	// medium green
	/**
	 * Our default color (RGB) for the border for "stop".
	 */
	public static final int borderColorStopDft  = 0xCC8888;	// medium red

	/**
	 * Our default color (RGB) for the button background for "start".
	 */
	public static final int bgColorStartDft     = 0xCCFFCC;	// light green
	/**
	 * Our default color (RGB) for the button background for "stop".
	 */
	public static final int bgColorStopDft      = 0xFFCCCC;	// light red
	
	/**
	 * The thickness we use for the colored line borders.
	 * @see BorderFactory#createLineBorder(Color)
	 */
	public static final int borderThicknessDft = 2;

	public static final boolean useBorderDft  = true;
	public static final boolean useBgColorDft = false;
	public static final boolean useRoundedCornersDft = true;
	
	
	private String startkey;
	private String startActionCommand;
	private String stopkey;
	private String stopActionCommand;
	
	/**
	 * Whether we add a colored border to indicate "start" or "stop".
	 */
	private boolean useBorder;

	/**
	 * Whether we try to set a background color.
	 */
	private boolean useBgColor;
	
	/**
	 * Whether we demand the border to have rounded corners.
	 */
	private boolean useRoundedCorners;
	
	/**
	 * The currently effective color (RGB) we use for the border for "start".
	 */
	private int borderColorStart = borderColorStartDft;
	/**
	 * The currently effective color (RGB) we use for the border for "stop".
	 */
	private int borderColorStop  = borderColorStopDft;
	
	/**
	 * The currently effective background color (RGB) we use for "start".
	 */
	private int bgColorStart = bgColorStartDft;
	/**
	 * The currently effective background color (RGB) we use for "stop".
	 */
	private int bgColorStop  = bgColorStopDft;
	
	
	/**
	 * The main constructor.
	 * Sets the button to the "start" version.
	 * 
	 * @param startkey     text key for the button text of the "start" version
	 * @param startActionCommand action command string for the "start" version
	 * @param stopkey      text key for the button text of the "stop" version
	 * @param stopActionCommand  action command string for the "stop" version
	 * @see Texts#getText(String, Object...)
	 */
	public StartStopButton( String startkey, String startActionCommand,
			                String  stopkey, String  stopActionCommand )
	{
		super();
		
		this.startkey           = startkey;
		this.startActionCommand = startActionCommand;
		this.stopkey            = stopkey;
		this.stopActionCommand  = stopActionCommand;
		
		this.useBorder  = useBorderDft;
		this.useBgColor = useBgColorDft;
		this.useRoundedCorners = useRoundedCornersDft;
		
		setToStart();
	}
	
	/**
	 * Sets up the button for the "start" version.
	 */
	public void setToStart() {
		setTo(true);
	}
	
	/**
	 * Sets up the button for the "stop" version.
	 */
	public void setToStop() {
		setTo(false);
	}
	
	/**
	 * Sets up the button for the indicated version.
	 * 
	 * @param forStart whether to set up for the "start" version
	 */
	private void setTo( boolean forStart ) {
		String textkey = (forStart ? startkey           : stopkey          );
		String action  = (forStart ? startActionCommand : stopActionCommand);
		
		setText(Texts.getText( textkey ));
		setActionCommand( action );
		
		setupBorder(forStart);
		setupBgColor(forStart);
	}
	
	/**
	 * Partial setup regarding the border.
	 * 
	 * @param forStart whether to set up for the "start" version
	 */
	private void setupBorder(boolean forStart) {
		setBorder( useBorder ? makeBorder(forStart) : null );
	}
	
	/**
	 * Partial setup regarding the background color.
	 * 
	 * @param forStart whether to set up for the "start" version
	 */
	private void setupBgColor(boolean forStart) {
		if (useBgColor) {
			int rgb = (forStart ? bgColorStart : bgColorStop);
			
			setBackground( new Color(rgb) );
			setOpaque(false);
		} else {
			// FFS/hm: should we actively put away a background color?
		}
	}

	/**
	 * Creates a border as we would use it for the given border color (RGB).
	 * 
	 * @param rgb the color to be used for the button border
	 * @param roundedCorners whether the border shall have rounded corners
	 * @return    the resulting {@link Border}
	 */
	private static Border makeRgbBorder(int rgb, boolean roundedCorners) {
		Color c = new Color(rgb);
		int   thickness = borderThicknessDft;
		
		// The BorderFactory does not offer LineBorder's with rounded corners.
		if (roundedCorners) {
			return new LineBorder(c, thickness, roundedCorners);
		}
		return BorderFactory.createLineBorder(c, thickness);
	}

	/**
	 * Create the border for the indicated version.
	 * Does <em>not</em> look at {@link #useBorder}.
	 * 
	 * @param forStart whether to create it for the "start" version
	 * @return         border for the indicated version
	 */
	public Border makeBorder(boolean forStart) {
		int rgb = (forStart ? borderColorStart : borderColorStop);
		return makeRgbBorder(rgb, useRoundedCorners);
	}
	
	/**
	 * Creates and returns a border for the "start" version.
	 * Does <em>not</em> look at {@link #useBorder}.
	 * 
	 * @return border for a "start" version
	 */
	public Border makeStartBorder() {
		return makeBorder(true);
	}
	
	/**
	 * Creates and returns a border for the "stop" version.
	 * Does <em>not</em> look at {@link #useBorder}.
	 * 
	 * @return border for a "stop" version
	 */
	public Border makeStopBorder() {
		return makeBorder(false);
	}

	
	/**
	 * @return the useBorder
	 */
	public boolean isUseBorder() {
		return useBorder;
	}

	/**
	 * Does not have effect before the next {@link #setToStart()}
	 * or {@link #setToStop()}.
	 * @param useBorder the useBorder to set
	 */
	public void setUseBorder(boolean useBorder) {
		this.useBorder = useBorder;
	}

	/**
	 * @return the useBgColor
	 */
	public boolean isUseBgColor() {
		return useBgColor;
	}

	/**
	 * Does not have effect before the next {@link #setToStart()}
	 * or {@link #setToStop()}.
	 * @param useBgColor the useBgColor to set
	 */
	public void setUseBgColor(boolean useBgColor) {
		this.useBgColor = useBgColor;
	}

	/**
	 * @return the borderColorStart
	 */
	public int getBorderColorStart() {
		return borderColorStart;
	}

	/**
	 * Does not have effect before the next {@link #setToStart()}.
	 * @param borderColorStart the borderColorStart to set
	 */
	public void setBorderColorStart(int borderColorStart) {
		this.borderColorStart = borderColorStart;
	}

	/**
	 * @return the borderColorStop
	 */
	public int getBorderColorStop() {
		return borderColorStop;
	}

	/**
	 * Does not have effect before the next {@link #setToStop()}.
	 * @param borderColorStop the borderColorStop to set
	 */
	public void setBorderColorStop(int borderColorStop) {
		this.borderColorStop = borderColorStop;
	}

	/**
	 * @return the bgColorStart
	 */
	public int getBgColorStart() {
		return bgColorStart;
	}

	/**
	 * Does not have effect before the next {@link #setToStart()}.
	 * @param bgColorStart the bgColorStart to set
	 */
	public void setBgColorStart(int bgColorStart) {
		this.bgColorStart = bgColorStart;
	}

	/**
	 * @return the bgColorStop
	 */
	public int getBgColorStop() {
		return bgColorStop;
	}

	/**
	 * Does not have effect before the next {@link #setToStop()}.
	 * @param bgColorStop the bgColorStop to set
	 */
	public void setBgColorStop(int bgColorStop) {
		this.bgColorStop = bgColorStop;
	}

}
