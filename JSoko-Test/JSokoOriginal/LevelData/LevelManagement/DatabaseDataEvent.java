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
package de.sokoban_online.jsoko.leveldata.levelmanagement;

import java.util.Arrays;
import java.util.Collections;
import java.util.EventObject;
import java.util.List;

/**
 * Defines an event that encapsulates changes to the database.
 */
public class DatabaseDataEvent extends EventObject {

	/**
	 * Action that has been performed for an object.
	 * <p>
	 * Possible actions:
	 * <li>{@link #INSERT}</li>
	 * <li>{@link #DELETE}</li>
	 * <li>{@link #CHANGE}</li>
	 */
	public enum EventAction {
		/** Event fired due to an insertion. */ INSERT,
		/** Event fired due to a deletion. 	 */ DELETE,
		/** Event fired due to a change. 	 */ CHANGE
	};


    /** Type of the object the event has been fired for. */
    private final Class eventObjectClass;

    /** Type of the action that resulted in the event to be fired. */
    private final EventAction eventAction;

    /** IDs of the objects for which the event has been fired. */
    private final List<Integer> databaseIDs;


    /**
     * Constructs a DatabaseDataEvent.
     *
     * @param source  the source Object (typically <code>this</code>)
     * @param objectClass class of the object the event has been fired for
     * @param eventAction  type of the action that resulted in the event to be fired
     * @param databaseIDs  the IDs of all objects the event has been fired for
     */
    public DatabaseDataEvent(Object source, Class objectClass, EventAction eventAction, List<Integer> databaseIDs) {
		super(source);
		this.eventObjectClass  = objectClass;
		this.eventAction   	   = eventAction;
		this.databaseIDs 	   = Collections.unmodifiableList(databaseIDs);
	}

    /**
     * Constructs a DatabaseDataEvent.
     *
     * @param source  the source Object (typically <code>this</code>)
     * @param objectClass class of the object the event has been fired for
     * @param eventAction  type of the action that resulted in the event to be fired
     * @param databaseID   the ID of the object the event has been fired for
     */
    public DatabaseDataEvent(Object source, Class objectClass, EventAction eventAction, int databaseID) {
		this(source, objectClass, eventAction, Arrays.asList(databaseID));
	}

    /**
     * Returns the class of the object the event has been fired for.
     *
     * @return the class of the object
     */
    public Class getEventObjectClass() {
    	return eventObjectClass;
    }

    /**
     * Returns the type of action that resulted in the event to be fired.
     *
     * @return  type of event action
     */
    public EventAction getEventAction() {
    	return eventAction;
    }

    /**
     * Returns the database IDs of all objects the event has been fired for.
     *
     * @return {@code List} of database IDs
     */
    public List<Integer>getDatabaseIDs() {
    	return databaseIDs;
    }

	@Override
	public int hashCode() {
		final int prime = 31;
		int result = 1;
		result = prime * result + ((eventAction == null) ? 0 : eventAction.hashCode());
		result = prime * result + ((eventObjectClass == null) ? 0 : eventObjectClass.hashCode());
		return result;
	}

	@Override
	public boolean equals(Object obj) {
		if (this == obj) {
			return true;
		}
		if (obj == null) {
			return false;
		}
		if (getClass() != obj.getClass()) {
			return false;
		}
		DatabaseDataEvent other = (DatabaseDataEvent) obj;
		if (eventAction != other.eventAction) {
			return false;
		}
		if (eventObjectClass == null) {
			if (other.eventObjectClass != null) {
				return false;
			}
		} else if (!eventObjectClass.equals(other.eventObjectClass)) {
			return false;
		}
		return true;
	}

	@Override
	public String toString() {
		return "DatabaseDataEvent [eventObjectClass=" + eventObjectClass + ", eventAction=" + eventAction + ", databaseIDs=" + databaseIDs + "]";
	}
}

