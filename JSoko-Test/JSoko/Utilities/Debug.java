/**
 *  JSoko - A Java implementation of the game of Sokoban
 *  Copyright (c) 2014 by Matthias Meger, Germany
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
package de.sokoban_online.jsoko.utilities;

import java.awt.event.ActionEvent;
import java.awt.event.KeyEvent;
import java.lang.annotation.Documented;
import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;
import java.lang.reflect.Field;
import java.lang.reflect.Modifier;
import java.util.ArrayList;
import java.util.Collections;
import java.util.Comparator;
import java.util.List;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.resourceHandling.Settings;

public class Debug {

	/** Flag specifying whether the debug mode is activated. */
	public static boolean isDebugModeActivated = false;

	/** Flag specifying whether this class should display debug information about annotations. */
	public static boolean isSettingsDebugModeActivated = false;

	/** Flag specifying whether timing information have to be printed when the program is started. */
	public static boolean isTimingDebugModeActivated = false;

	/** Flag specifying whether the optimizer is to to offer the "find settings" check box. */
	public static boolean isFindSettingsActivated = false;

	/** Debug: Create an own skin by drawing the graphics with Java. */
	public static boolean debugDrawOwnSkin = false;

	/** Flag specifying whether the box data are to be shown after every push. */
	public static boolean debugShowBoxData = false;

	/** Flag specifying whether the lower bound forward for every reached board position is to be shown. */
	public static boolean debugShowLowerBoundForward = false;

	/** Flag specifying whether the lower bound backward for every reached board position is to be shown. */
	public static boolean debugShowLowerBoundBackward = false;

	/** Flag specifying whether the forward distance for a box is to be displayed. */
	public static boolean debugShowBoxDistanceForward = false;

	/** Flag specifying whether the backward distance for a box is to be displayed. */
	public static boolean debugShowBoxDistanceBackward = false;

	/** If debug is active the position of a clicked square is saved in this variable. */
	public static int debugSquarePosition = 0;

	/** Flag specifying whether the penalty squares are to be displayed. */
	public static boolean debugShowPenaltyFields = false;

	/** Flag specifying whether every penalty situation found is to be displayed successively. */
	public static boolean debugShowPenaltySituationsSeparately = false;

	/** Flag specifying whether areas having different reachable goals are to be highlighted. */
	public static boolean debugShowDifferentReachableGoalsAreas = false;

	/** Flag specifying whether all "closed diagonal deadlocks" are to be displayed. */
	public static boolean debugShowAllClosedDiagonalDeadlocks = false;

	/** Access to the main object of this application for debug purposes. */
	public static JSoko debugApplication;


	/**
	 * Since the class {@code Settings} does not (at runtime) change its set of static fields, nor do these change their annotation set, we need not compute
	 * such data more than once. Here we cache the result of this computation.
	 */
	public static volatile List<DebugField> menuDebugFields;

	/** Debug: flag specifying whether the simple deadlock squares forward are to be shown. */
	@DebugVar(menuOrder = 1, menuText = "Show simple deadlocks forward", doRepaint = true)
	public static boolean debugShowSimpleDeadlocksForward = false;

	/** Debug: flag specifying whether the simple deadlock squares backward are to be shown. */
	@DebugVar(menuOrder = 2, menuText = "Show simple deadlocks backward", doRepaint = true)
	public static boolean debugShowSimpleDeadlocksBackward = false;

	/** Debug: flag specifying whether the simple advanced deadlock squares forward are to be shown. */
	@DebugVar(menuOrder = 3, menuText = "Show advanced simple deadlocks", doRepaint = true)
	public static boolean debugShowAdvancedSimpleDeadlocks = false;

	/**
	 * Debug: flag specifying whether the hash value for every reached board position is to be shown.
	 */
	@DebugVar(menuOrder = 11, menuText = "Show hashvalue", doRedraw = true)
	public static boolean debugShowHashvalue = false;

	/** Debug: flag specifying whether the corral detection is to be analyzed. */
	@DebugVar(menuOrder = 12, menuText = "Debugmode corral")
	public static boolean debugCorral = false;

	/** Debug: flag specifying whether the corral forcer are to be shown. */
	@DebugVar(menuOrder = 13, menuText = "Show corralforcer", doRedraw = true)
	public static boolean debugShowCorralForcer = false;

	/** Debug: flag specifying whether the existence of an I-corral is to be displayed. */
	@DebugVar(menuOrder = 14, menuText = "Show I-Corral")
	public static boolean debugShowICorrals = false;

	/** Debug: flag specifying whether all found I-corrals are to be displayed. */
	@DebugVar(menuOrder = 15, menuText = "Show I-Corral during solving")
	public static boolean debugShowCorralsWhileSolving = false;

	/**
	 * Debug: flag specifying whether the distance for the player between two squares is to be displayed.
	 */
	@DebugVar(menuOrder = 21, menuText = "Show player distance", doZeroPos = true)
	public static boolean debugShowPlayerDistance = false;

	/** Debug: Flag specifying whether the influence values are to be shown. */
	@DebugVar(menuOrder = 22, menuText = "Show influence values")
	public static boolean debugShowInfluenceValues = false;

	/**
	 * Debug: Flag specifying whether the influence values are shown by coloring the squares.
	 */
	@DebugVar(menuOrder = 23, menuText = "Show influence colors")
	public static boolean debugShowInfluenceColors = false;

	/**
	 * Debug: flag specifying whether the the "push tunnel" status of the pushed box is to be displayed.
	 */
	@DebugVar(menuOrder = 88, menuText = "Show box-in-tunnel status push")
	public static boolean debugShowBoxInTunnelStatusPush = false;

	/**
	 * Debug: flag specifying whether the the "move tunnel" status of the pushed box is to be displayed.
	 */
	@DebugVar(menuOrder = 89, menuText = "Show box-in-tunnel status move")
	public static boolean debugShowBoxInTunnelStatusMove = false;

	/**
	 * Debug: flag specifying whether the board index of the clicked square is to be displayed.
	 */
	@DebugVar(menuOrder = 93, menuText = "Show positionindex", doRedraw = true)
	public static boolean debugShowPositionIndex = false;

	/**
	 * Debug: flag specifying whether all internal positions of all squares are to be displayed.
	 */
	@DebugVar(menuOrder = 94, menuText = "Show all position indices", doRedraw = true)
	public static boolean debugShowAllPositionIndices = false;

	/**
	 * Debug: flag specifying whether the maximum available RAM for the program is to be displayed.
	 */
	@DebugVar(menuOrder = 95, menuText = "Show max. available RAM", doRedraw = true)
	public static boolean debugShowMaximumRAM = false;

	/**
	 * Debug: flag specifying whether a hash table statistic is to be displayed after the solving.
	 */
	@DebugVar(menuOrder = 96, menuText = "Show hash table statistic")
	public static boolean debugShowHashTableStatistic = false;

	/**
	 * Debug: flag specifying whether the path to a board position is to be displayed during solving.
	 */
	public static boolean isDisplayPathToCurrentBoardPositionActivated = false;

	/** Debug: Flag specifying whether the size of the graphics is to be displayed. */
	@DebugVar(menuOrder = 97, menuText = "Show graphic size", doRedraw = true)
	public static boolean debugShowGraphicSize = false;

	/** Debug: Flag specifying whether the marking mode is enabled or not. */
	@DebugVar(menuOrder = 98, menuText = "Marking mode", doRedraw = true)
	public static boolean debugMarkingModeEnabled = false;

	/** Debug: Flag specifying whether the box data have to be shown. */
	public static boolean debugBoxDataAreToBeShown = false;

	/** Debug: Flag specifying whether the "level is in Database" information is to be shown. */
	@DebugVar(menuOrder = 99, menuText = "Show DB Status", doRedraw = true)
	public static boolean debugShowLevelIsInDBStatus = false;

	/** Debug: show the animation delay values. */
	@DebugVar(menuOrder = 99.2, menuText = "Show Delays")
	public static boolean debugShowDelays = false;

	/** Array for checking if the debug mode is to be activated. */
	private static final int[] debug = new int[5];

	/**
	 * This annotation {@code DebugVar} shall be used for static fields of the class {@link Settings}, which are debug variables. The advantage of this
	 * annotation is, that the maintenance of the corresponding menu entry and its action tends to be completely done by the annotation (and its systematic
	 * usage elsewhere).
	 * <p>
	 * We offer a sorted list of annotated fields, ready for the generation of menu entries, and the handling of actions from their check box, to manipulate the
	 * current value of the debug variable.
	 *
	 * @author Heiner Marxen
	 */
	@Documented
	@Target(ElementType.FIELD)
	@Retention(RetentionPolicy.RUNTIME)
	public @interface DebugVar {
		/**
		 * Whether this variable shall have an entry in the menu for debug variables.
		 *
		 * @return whether to enter it into the debug menu
		 */
		boolean forMenu() default true;

		/**
		 * Since reflection does offer the variables in no particular order, we want an explicit ordering schema, to produce a predictable order of menu
		 * entries. We sort them by this value.
		 *
		 * @return sorting order for menu
		 * @see Class#getDeclaredFields()
		 * @see Debug#getMenuDebugFields()
		 */
		double menuOrder() default 999.999;

		/**
		 * If the menu text is empty, the name of the variable shall be used as menu text.
		 *
		 * @return explicitly annotated text for menu item
		 */
		String menuText() default "";

		/**
		 * If the action name is empty, the name of the variable shall be used as an action name.
		 *
		 * @return the action name as annotated explicitly
		 */
		String actionName() default "";

		/**
		 * Whether the action code should include a {@code Settings.debugSquarePosition = 0;}.
		 *
		 * @return whether to zero {@code debugSquarePosition}
		 */
		boolean doZeroPos() default false;

		/**
		 * Whether the action code should include a {@code repaint()}.
		 *
		 * @return whether to do a {@code repaint()}
		 */
		boolean doRepaint() default false;

		/**
		 * Whether the action code should include a {@code redraw(false)}.
		 *
		 * @return whether to do a {@code redraw(false)}
		 */
		boolean doRedraw() default false;

		/**
		 * Whether the action code described by this annotation is incomplete, and the action code should goon to thing about a reaction, even after having done
		 * what this annotation describes.
		 *
		 * @return whether this annotation describes an incomplete action
		 */
		boolean doGoon() default false;
	}

	/**
	 * This class pairs a {@link Field} (from {@code Settings}) with its Annotation runtime instance of type {@link DebugVar}. A list of such pairs is needed to
	 * generate the menu entries that shall be provided for the annotated fields, and to handle their corresponding action event.
	 *
	 * @author Heiner Marxen
	 */
	public static class DebugField {
		/**
		 * The reflection data for the debug variable
		 */
		public final Field field;

		/**
		 * The annotation data for the debug variable
		 */
		public final DebugVar anno;

		/**
		 * The effective action name, taken either from the annotation or from the name of the field. Is never {@code null} nor empty.
		 */
		public final String actionName;

		/**
		 * The effective text for the menu entry, taken either from the annotation or from the name of the field. Is never {@code null} nor empty.
		 */
		public final String menuText;

		public DebugField(Field field, DebugVar anno) {
			this.field = field;
			this.anno = anno;
			this.actionName = selNonEmpty(anno.actionName(), field.getName());
			this.menuText = selNonEmpty(anno.menuText(), field.getName());
		}

		static private String selNonEmpty(String... args) {
			for (String a : args) {
				if ((a != null) && (a.length() > 0)) {
					return a;
				}
			}
			// Did not find anything...
			return "?";
		}

		/**
		 * Retrieves the current value of the debug variable described by this object, by using reflection.
		 *
		 * @return current value of the described debug variable
		 */
		public boolean getValue() {
			/*
			 * Since we here are inside of class Debug, we should not get an IllegalAccessException. Hence, we catch the exceptions here, and return false,
			 * just in case.
			 */
			try {
				return field.getBoolean(null);
			} catch (IllegalArgumentException e) {
				e.printStackTrace();
			} catch (IllegalAccessException e) {
				e.printStackTrace();
			}
			return false;
		}

		/**
		 * Sets the value of the debug variable described by this object by using reflection.
		 *
		 * @param value
		 *            to be assigned to the described debug variable
		 */
		public void setValue(boolean value) {
			/*
			 * Regarding exceptions see comment in "getValue"
			 */
			try {
				field.setBoolean(null, value);
				System.out.println("Setting: DebugVar: " + field.getName() + " set to " + value);
			} catch (IllegalArgumentException e) {
				e.printStackTrace();
			} catch (IllegalAccessException e) {
				e.printStackTrace();
			}
			// well... forget it.
		}
	}

	/**
	 * Provides a comparator object able to sort a list of {@link DebugField} in the way the menu builder wants it: sorted by increasing
	 * {@link DebugVar#menuOrder()}. Tie breaking is done by the name of the field.
	 *
	 * @author Heiner Marxen
	 */
	public static class MenuComparator implements Comparator<DebugField> {
		public MenuComparator() {
		}

		@Override
		public int compare(DebugField x, DebugField y) {
			int cmp = Double.compare(x.anno.menuOrder(), y.anno.menuOrder());
			if (cmp == 0) {
				cmp = x.field.getName().compareTo(y.field.getName());
			}
			return cmp;
		}
	}

	public static void keyPressed(KeyEvent keyEvent) {

		int c = keyEvent.getKeyCode();

		switch (c) {
		// Entering "debug" sets the debug menu on/off
		case KeyEvent.VK_D:
		case KeyEvent.VK_E:
		case KeyEvent.VK_B:
		case KeyEvent.VK_U:
		case KeyEvent.VK_G:
			// shift down content of buffer "debug"
			for (int i = 1; i < debug.length; i++) {
				debug[i - 1] = debug[i];
			}
			debug[4] = c;		// append current character
			break;
		}

		if (   debug[0] == KeyEvent.VK_D
			&& debug[1] == KeyEvent.VK_E
			&& debug[2] == KeyEvent.VK_B
			&& debug[3] == KeyEvent.VK_U
			&& debug[4] == KeyEvent.VK_G ) {

			Debug.isDebugModeActivated = ! Debug.isDebugModeActivated;	// flip it
			debugApplication.applicationGUI.setDebugMenuVisible(Debug.isDebugModeActivated);
			debugApplication.applicationGUI.setNewLanguage(); // Create new MenuBar for menu item that are debug mode dependent

			// Initialize the debug string: we just "consumed" its content.
			debug[4] = 'X';
		}
	}

	public static void actionPerformed(ActionEvent action) {

	}

	/**
	 * Check whether the user has activated any debug mode.
	 *
	 * @param argv the parameters passed at JSoko start
	 */
	public static void checkParameters(String[] argv) {
		for(String parameter : argv) {
			if(parameter.equalsIgnoreCase("-debug")) {
				Debug.isDebugModeActivated = true;
			}
			if(parameter.equalsIgnoreCase("-debugSettings")) {
				Debug.isSettingsDebugModeActivated = true;
			}
			if(parameter.equalsIgnoreCase("-debugTiming")) {
				Debug.isTimingDebugModeActivated = true;
			}
			if(parameter.equalsIgnoreCase("-debugRalf")) {
				Debug.isFindSettingsActivated = true;
			}
		}
	}

	public static List<DebugField> mkMenuDebugFields() {
		ArrayList<DebugField> fldList = new ArrayList<DebugField>();

		for (Field cand : Debug.class.getDeclaredFields()) {
			if (!Modifier.isStatic(cand.getModifiers())) {
				continue;
			}
			if (!Modifier.isPublic(cand.getModifiers())) {
				continue;
			}
			if (!boolean.class.equals(cand.getType())) {
				continue;
			}

			final DebugVar anno = cand.getAnnotation(DebugVar.class);
			if (anno == null) {
				continue;
			}
			if (!anno.forMenu()) {
				continue;
			}
			// We have: @DebugVar public static boolean Debug.varname;

			fldList.add(new DebugField(cand, anno));
		}

		Collections.sort(fldList, new Debug.MenuComparator());

		fldList.trimToSize();
		// return fldList;
		return Collections.unmodifiableList(fldList);
	}

	/**
	 * Returns the list of field/annotation pairs of those annotated variables of this {@code Debug} class, which are to be included in the menu. It is
	 * sorted as specified by the {@link DebugVar} annotations.
	 * <p>
	 * The {@link List} is computed only once, and should not be altered.
	 *
	 * @return the list of debug variables to be included in the menu
	 */
	public static List<DebugField> getMenuDebugFields() {
		if (Debug.menuDebugFields == null) {
			synchronized (Debug.class) {
				if (Debug.menuDebugFields == null) {
					Debug.menuDebugFields = mkMenuDebugFields();
				}
			}
		}
		return Debug.menuDebugFields;
	}

	/**
	 * Within the list of {@code DebugField}, as {@link getMenuDebugFields} returns it, we search for an element with the specified string as its
	 * {@link DebugField#actionName}.
	 *
	 * @param action
	 *            the action name to search.
	 * @return the object with the wanted action name, or {@code null}.
	 */
	public static DebugField findActionDebugField(String action) {
		for (DebugField cand : getMenuDebugFields()) {
			if (cand.actionName.equals(action)) {
				return cand;
			}
		}
		return null;
	}
}
