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
package de.sokoban_online.jsoko.utilities.listener;

import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;

/**
 * This class specializes the general {@link ListenerSet} for
 * <ul>
 * <li>the event type {@link ActionEvent}
 * <li>the listener type {@link ActionListener}
 * <li>the event receiving method
 * {@link ActionListener#actionPerformed(ActionEvent)}
 * </ul>
 * 
 * @author Heiner Marxen
 */
public class ActionListenerSet extends ListenerSet<ActionListener, ActionEvent> {

	/**
	 * Our local caller object as an immutable singleton.
	 */
	private static ListenerCaller<ActionListener, ActionEvent> mycaller = null;

	/**
	 * Eventually creates and returns the singleton caller object we use in this
	 * class, fixing the method
	 * {@link ActionListener#actionPerformed(ActionEvent)} as event consumer.
	 * 
	 * @return our locally used caller (immutable singleton)
	 */
	public static final ListenerCaller<ActionListener, ActionEvent> makeCaller() {
		if (mycaller == null) {
			// Create the singleton
			mycaller = (new ListenerCaller<ActionListener, ActionEvent>() {

				@Override
				public void call(ActionListener listener, ActionEvent event) {
					if (listener != null) {
						listener.actionPerformed(event);
					}
				}
			});
		}
		return mycaller;
	}

	/**
	 * Creates a new listener set for action events, which uses the caller
	 * created by {@link #makeCaller()}.
	 */
	public ActionListenerSet() {
		super(makeCaller());
	}
}
