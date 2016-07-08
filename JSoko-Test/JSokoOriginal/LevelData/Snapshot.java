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
package de.sokoban_online.jsoko.leveldata;

import java.util.Date;

/**
 * A Snapshot represents the moves the player has made for reaching a specific
 * board position.
 * <p>
 * Example: <br>
 * <code>uuurrr</code>  means: the player has moved 3 times up and three times right.
 */
public class Snapshot {

	/** ID of the save game in the database (primary key). -1 = not saved in database */
	private int databaseID = -1;

	/** lurd representation of the moves. */
	private String lurd = "";

	/** Comment text */
	private String comment = "";

	/** Order information to display the snapshots in a specific order in the GUI. */
	private int orderValue = 0;

	/** Data for highlighting a snapshot in the GUI. */
	private String highLightData = "";

	/** <code>true</code> = the snapshot has been saved by JSoko as the user has closed the level -> SaveGame. false = snapshot manually saved by the user. */
	private boolean isAutoSaved = false;

	/**
	 * Last time this snapshots values have been changed.
	 * This is a point in time (either a Date or a Timestamp).
	 * Its string representation depends on the current locale,
	 * which may change during runtime.
	 */
	private Date lastChanged = null;


	/**
	 * Creates a new snapshot representing the moves passed in the lurd format.
	 *
	 * @param lurd  moves this snapshot represents
	 */
	public Snapshot(String lurd) {
		this.lurd = lurd;
		lastChanged = new Date();
	}

	/**
	 * Creates a new snapshot representing the moves passed in the lurd format.
	 *
	 * @param lurd  moves this snapshot represents
	 * @param isAutoSaved  flag indicating whether this snapshot has been saved automatically by JSoko
	 */
	public Snapshot(String lurd, boolean isAutoSaved) {
		this(lurd);
		this.isAutoSaved = isAutoSaved;
	}

	/**
	 * Returns the ID in the database of this snapshot.
	 *
	 * @return ID in the database
	 */
	public int getDatabaseID() {
		return databaseID;
	}

	/**
	 * Sets the passed database ID for this snapshot.
	 *
	 * @param snapshotID to be set
	 */
	public void setDatabaseID(int snapshotID) {
		this.databaseID = snapshotID;
	}

	/**
	 * Returns the moves of this snapshot in lurd format.
	 * <p>
	 * The position the player has been when saving the snapshot can be marked with an "*" in the lurd string. <br>
	 * Example: udl*rrru<br>
	 * In this example the player has played three moves. The snapshot also contains four "future" moves the player
	 * as undone which can be redone.
	 *
	 * @return the moves of this snapshot
	 */
	public String getLURD() {
		return lurd;
	}

	/**
	 * Sets the moves of this snapshot as lurd string.
	 *
	 * @param lurd
	 */
	public void setLurd(String lurd) {
		this.lurd = lurd;
	}

	/**
	 * Returns the comment text of this snapshot.
	 *
	 * @return the comment text of this snapshot.
	 */
	public String getComment() {
		return comment;
	}

	/**
	 * Sets the comment text of this snapshot.
	 *
	 * @param comment  comment text to be set
	 */
	public void setComment(String comment) {
		this.comment = comment;
	}

	/**
	 * Returns the order value of this snapshot.
	 * <p>
	 * This value is used to order the snapshots in the GUI.
	 *
	 * @return the order value of this snapshot
	 */
	public int getOrderValue() {
		return orderValue;
	}

	/**
	 * Sets the order value of this snapshot.
	 *
	 * @param orderValue  the order value to be set
	 */
	public void setOrderValue(int orderValue) {
		this.orderValue = orderValue;
	}

	/**
	 * Returns the data for highlighting the snapshot in the GUI.
	 *
	 * @return  a {@code String} containing the data for highlighting the snapshot in the GUI
	 */
	public String getHighLightData() {
		return highLightData;
	}

	/**
	 * Sets the data for highlighting the snapshot in the GUI.
	 *
	 * @param highLightData  a {@code String} containing the data for highlighting the snapshot in the GUI
	 */
	public void setHighLightData(String highLightData) {
		this.highLightData = highLightData;
	}

	/**
	 * Returns whether this snapshot has been automatically saved by JSoko.
	 * <p>
	 * automatically saved means: the level has been left by the user and JSoko saved
	 * a snapshot for being able of restoring the board position when the level is reopened later -> SaveGame
	 * If this flag isn't set, then this snapshot is a real snapshot saved by the user.
	 *
	 * @return <code>true</code> this snapshot has been saved by JSoko
	 */
	public boolean isAutoSaved() {
		return isAutoSaved;
	}

	/**
	 * Marks this snapshot as being saved by JSoko.
	 *
	 * @param isAutoSaved
	 */
	public void setAutoSaved(boolean isAutoSaved) {
		this.isAutoSaved = isAutoSaved;
	}


	/**
	 * Sets the {@code lastChanged} data to the specified point in time.
	 *
	 * @param changedAt the time to be remembered as {@code lastChanged} data
	 */
	public void setLastChanged(Date changedAt) {
		if(changedAt != null) {
			lastChanged = changedAt;
		}
	}

	/**
	 * Retrieves the {@code lastChanged} data as a {@code Date} object.
	 *
	 * @return the {@code lastChanged} data as a {@code Date} object
	 */
	public Date getLastChanged() {
		return lastChanged;
	}


	/* (non-Javadoc)
	 * @see java.lang.Object#toString()
	 */
	@Override
	public String toString() {
		return lurd;
	}

	@Override
	public int hashCode() {
		final int prime = 31;
		int result = 1;
		result = prime * result + (isAutoSaved ? 1231 : 1237);
		result = prime * result + ((lurd == null) ? 0 : lurd.hashCode());
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
		Snapshot other = (Snapshot) obj;
		if (isAutoSaved != other.isAutoSaved) {
			return false;
		}
		if (lurd == null) {
			if (other.lurd != null) {
				return false;
			}
		} else if (!lurd.equals(other.lurd)) {
			return false;
		}
		return true;
	}
}
