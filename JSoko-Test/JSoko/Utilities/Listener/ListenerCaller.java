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
 * Instances of this class provide a type safe combination of a listener
 * type L, an event type E, and some method of L to be called with an E,
 * whenever a notification is to be done.
 * 
 * Since we cannot abstract over the names of methods, instances of this
 * class are necessary to implement a general, type safe event listener set
 * like {@link ListenerSet}.
 * 
 * @param <L> the type of the listener
 * @param <E> the type of the event
 * 
 * @author Heiner Marxen
 */
public abstract class ListenerCaller<L,E> {

	/**
	 * The abstract method to perform the notification call.
	 * Each instance of a ListenerCaller must not only bind two types,
	 * L and E, but must also provide an implementation of this method,
	 * which binds the appropriate method of L to this caller object.
	 * 
	 * @param listener the object to be notified
	 * @param event    the event to be passed to the notification method
	 */
	public abstract void call(L listener, E event);
	
	/**
	 * A static factory method to construct a ListenerCaller for a listener
	 * of type {@link ActionListener} which will accept an event of type
	 * {@link ActionEvent} by the method "actionPerformed".
	 * 
	 * This method is here only for demonstration purposes how to construct
	 * an instance of this class. Most implementers will want to construct
	 * just a singleton object (of the extended type).
	 * 
	 * @return a Caller combining ActionListener and ActionEvent with method
	 *          "actionPerformed"
	 */
	public static final ListenerCaller<ActionListener, ActionEvent>
	makeActionListenerCaller() {
		return (new ListenerCaller<ActionListener, ActionEvent>() {
			@Override
			public void call(ActionListener listener, ActionEvent ev) {
				if (listener != null) {
					listener.actionPerformed(ev);
				}
			}
		});
	}
}
