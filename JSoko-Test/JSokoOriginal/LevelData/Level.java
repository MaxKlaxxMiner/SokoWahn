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
package de.sokoban_online.jsoko.leveldata;

import java.util.ArrayList;
import java.util.List;

import de.sokoban_online.jsoko.gui.Transformation;
import de.sokoban_online.jsoko.leveldata.solutions.Solution;
import de.sokoban_online.jsoko.leveldata.solutions.SolutionsManager;
import de.sokoban_online.jsoko.leveldata.solutions.SolutionsManager.SolutionType;
import de.sokoban_online.jsoko.resourceHandling.Texts;



/**
 * Object that contains the data for a Sokoban "Level".
 */
public final class Level {

	/** The width of the level (in columns, inclusive border) */
	private int levelWidth = 0;

	/** The height of the level (in lines, inclusive border). */
	private int levelHeight = 0;

	/** The content of the level, line by line. */
	private List<String> boardData = new ArrayList<String>();

	/** The title of the level. */
	private String levelTitle = "";

	/** Author of this level. */
	private Author author = new Author();

	/** ID of the level in the database (primary key) */
	private int databaseID = -1;

	/**
	 * Indicates whether this is a delegate level which is only stored in the database
	 * in order to store solutions and snapshots for another level.
	 * <p>
	 * If a level isn't stored in the database, yet, but a solution or snapshot
	 * is to be saved for the level JSoko automatically stores a delegate level in
	 * the database in order to save solutions and snapshots.
	 * These automatically imported levels aren't visible to the user and are only
	 * used to store solutions and snapshots for the levels.
	 */
	private boolean isDelegateLevel = false;

	/** Number of the level in the current level collection. */
	private int number = 1;

	/**
	 * String encoding the screen transformation of the level.
	 * @see Transformation
	 */
	private String transformationString = "";

	/** The history object for this level */
	private History history;

	/** The comment of this level */
	private String levelComment = "";

	/** The difficulty of the level set by the user */
	private String difficulty = "";

	/** The number of boxes in this level */
	private int boxCount;

	/** The manager for the solutions of the current level. */
	private final SolutionsManager solutionsManager;

	/**	Snapshots of moves the player has done. */
	private final ArrayList<Snapshot> snapshots; //TODO: same handling as solutions -> store in DB!
	// FFS/hm: snapshots: not yet accessible to the user

	/** The database to store the data in. */
	private final Database database;

	/**
	 * Creates an object for handling the data of one level.
	 *
	 * @param database  the database the level data are to be stored
	 */
	public Level(Database database) {

		this.database = database;

		// The history data of this level are managed by this object
		history = new History();

		// ArrayList holding all solutions of this level.
		solutionsManager = new SolutionsManager(database, this);

		// List holding all snapshots of this level.
		snapshots = new ArrayList<Snapshot>();
	}

	/**
	 * Returns the board as a list of lines.
	 *
	 * @return the board as a list of lines
	 */
	public final List<String> getBoardData() {
		return boardData;
	}

	/**
	 * Returns the board data as a <code>String</code>.
	 *
	 * @return the level data as <code>String</code>
	 */
	public final String getBoardDataAsString() {

		StringBuilder boardDataString = new StringBuilder();
		for (int index = 0; index < boardData.size(); index++) {
			boardDataString.append(boardData.get(index));
			boardDataString.append("\n");
		}

		return boardDataString.toString();
	}

	/**
	 * Sets the board content of the level.
	 *
	 * @param boardData board data for the level
	 */
	public final void setBoardData(List<String> boardData) {
		this.boardData = boardData;
	}

	/**
	 * Sets the board data.
	 *
	 * @param boardDataAsSring  board data as <code>String</code>.
	 *                          Every row must end with \n.
	 */
	public final void setBoardData(String boardDataAsSring) {

		if (boardData == null) {
			boardData = new ArrayList<String>();
		} else {
			boardData.clear();
		}
		String[] boardDataString = boardDataAsSring.split("\n");
		for (String element : boardDataString) {
			boardData.add(element);
		}
	}

	/**
	 * Returns the title of the level.
	 *
	 * @return level title
	 */
	public final String getTitle() {

		// In case no level title has been set, we return a default construction
		return levelTitle != null ? levelTitle : "Level" + Texts.getText("unknown");
	}

	/**
	 * Sets the title of the level.
	 *
	 * @param levelTitle  level title to be set
	 */
	public final void setLevelTitle(String levelTitle) {
		this.levelTitle = levelTitle != null ? levelTitle : Texts.getText("unknown");
	}

	/**
	 * Returns the transformation string of the level.
	 * This string indicates how the level is to be transformed
	 * for display on the screen.
	 * It is retrieved initially from the DB or from a level definition file.
	 *
	 * @return transformation string of the level
	 */
	public final String getTransformationString() {
		return transformationString;
	}

	/**
	 * Sets the transformation string of the level.
	 *
	 * @param transformationString transformation string to be set
	 */
	public final void setTransformationString(String transformationString) {
		this.transformationString = transformationString;
	}

	/**
	 * Returns the <code>History</code> object of this level.
	 *
	 * @return the <code>History</code> of this level
	 */
	public final History getHistory() {
		return history;
	}

	/**
	 * Sets the width of the level.
	 *
	 * @param levelWidth level width to be set
	 */
	public final void setWidth(int levelWidth) {
		this.levelWidth = levelWidth;
	}

	/**
	 * Returns the width of the level.
	 *
	 * @return width of the level
	 */
	public final int getWidth() {
		return levelWidth;
	}

	/**
	 * Sets the height of the level.
	 *
	 * @param levelHeight level height to be set
	 */
	public final void setHeight(int levelHeight) {
		this.levelHeight = levelHeight;
	}

	/**
	 * Returns the height of the level.
	 *
	 * @return height of the level
	 */
	public final int getHeight() {
		return levelHeight;
	}

	/**
	 * Returns the {@code Author} of this {@code Level}.
	 *
	 * @return the {@code Author} of this {@code Level}
	 */
	public Author getAuthor() {
		return author;
	}

	/**
	 * Sets the {@code Author} of this {@code Level}.
	 *
	 * @param author {@code Author} to be set
	 */
	public void setAuthor(Author author) {
		this.author = author;
	}

	/**
	 * Returns the level comment as string.
	 *
	 * @return the level comment
	 */
	public final String getComment() {
		return levelComment;
	}

	/**
	 * Sets the level comment.
	 *
	 * @param levelComment the level comment to set
	 */
	public final void setComment(String levelComment) {
		if (levelComment != null) {
			this.levelComment = levelComment;
		}
	}

	/**
	 * Returns the ID of this level in the database.
	 *
	 * @return the database ID
	 */
	public final int getDatabaseID() {
		return databaseID;
	}

	/**
	 * Sets the ID of this level in the database.
	 *
	 * @param levelID the database ID to set
	 */
	public final void setDatabaseID(int levelID) {
		this.databaseID = levelID;
	}

	/**
	 * Returns whether this level is stored in the database
	 * as delegate level.
	 *
	 * @return <code>true</code> this level is stored as delegate level,
	 * <code>false</code> otherwise
	 */
	public boolean isStoredAsDelegate() {
		return isDelegateLevel;
	}

	/**
	 * Marks this level as delegate level.
	 * <p>
	 * A delegate level is only stored in the database
	 *
	 * @param <code>true</code> mark this level as delegate level,
	 * <code>false</code> otherwise
	 */
	public void setDelegate(boolean isDelegateLevel) {
		this.isDelegateLevel = isDelegateLevel;
	}

	/**
	 * Returns the number of this level in the level collection.
	 *
	 * @return the level number
	 */
	public int getNumber() {
		return number;
	}

	/**
	 * Sets the number of this level in the level collection.
	 *
	 * @param levelNumber the number of the level
	 */
	public void setNumber(int levelNumber) {
		this.number = levelNumber;
	}

	/**
	 * Returns the object managing all solutions of this level.
	 *
	 * @return the solutions
	 */
	public final SolutionsManager getSolutionsManager() {
		return solutionsManager;
	}

	/**
	 * Adds a solution to the set of known solutions.
	 * This is just a shortcut for {@code getSolutionsManager().addSolution(...)}.
	 *
	 * @param solution solution to be added
	 * @return result code from {@link SolutionsManager#addSolution(Solution)}
	 * @see SolutionsManager#addSolution(Solution)
	 */
	public final SolutionType addSolution(Solution solution) {
		return getSolutionsManager().addSolution(solution);
	}

	/**
	 * Deletes a solution from this level.
	 * This is just a shortcut for {@code getSolutionsManager().deleteSolution(...)}.
	 *
	 * @param solution solution to be deleted
	 * @see SolutionsManager#deleteSolution(Solution)
	 */
	public final void deleteSolution(Solution solution) {
		getSolutionsManager().deleteSolution(solution);
	}

	/**
	 * Returns all snapshots saved for this level.
	 *
	 * @return <code>ArrayList</code> containing all <code>Snapshot</code>s.
	 */
	public ArrayList<Snapshot> getSnapshots() {
		return snapshots;
	}

	/**
	 * Adds the passed {@code Snapshot} to this level.
	 *
	 * @param snapshot  {@code Snapshot} to be added
	 */
	public void addSnapshot(Snapshot snapshot) {
		if(snapshot != null) {
			if(snapshot.isAutoSaved()) {
				setSaveGame(snapshot);
			}
			else {
				// Snapshots and solutions have to be unique in JSoko.
				if(!snapshots.contains(snapshot)) {
					snapshots.add(snapshot);
				}
			}
		}
	}

	/**
	 * Removes the passed {@code Snapshot} from this level.
	 *
	 * @param snapshot  {@code Snapshot} to be removed
	 * @return <code>true</code> if this level contained the snapshot,
	 * <code>false</code> otherwise
	 */
	public boolean removeSnapshot(Snapshot snapshot) {
		return snapshots.remove(snapshot);
	}

	/**
	 * Returns the moves that had been done as the level has been saved the last time.
	 *
	 * @return the save game moves
	 */
	public Snapshot getSaveGame() {
		for(Snapshot snapshot : snapshots) {
			if(snapshot.isAutoSaved()) {
				return snapshot;
			}
		}

		// Return a snapshot having no moves history.
		return new Snapshot("", true);
	}

	/**
	 * Sets the passed {@code Snapshot} as new save game for this level.<br>
	 * A level can only have one save game at a time. If a new one is set the old
	 * is overwritten.
	 *
	 * @param saveGame  the {@code Snapshot} to be set as save game for this level
	 */
	public void setSaveGame(Snapshot saveGame) {

		if(!saveGame.isAutoSaved()) {
			return;
		}

		// Remove the currently saved save game.
		for(Snapshot snapshot : snapshots) {
			if(snapshot.isAutoSaved()) {
				snapshots.remove(snapshot);
				break;
			}
		}

		// Add the new save game.
		snapshots.add(saveGame);

		if(isConnectedWithDB()) {
			if(saveGame.getDatabaseID() != Database.NO_ID) {
				database.updateSnapshot(saveGame);
			}
			database.insertSnapshot(saveGame, getDatabaseID());
		}
	}

	/**
	 * Returns the char code of the square at the specified location.
	 *
	 * @param xPosition X coordinate of the square to return a char for
	 * @param yPosition Y coordinate of the square to return a char for
	 * @return			char code for the square, or a blank if the position is outside
	 *                  the implemented part of the board
	 */
	final public int getSquareCharacter(int xPosition, int yPosition) {

		// Select the line by the Y coordinate
		if (yPosition < 0 || yPosition >= boardData.size()) {
			return ' ';
		}
		final String line = boardData.get(yPosition);

		// The level lines need not be filled with spaces at the end.
		// Such a line may be shorter than the level width.
		if (xPosition < 0 || xPosition >= line.length()) {
			return ' ';
		}

		return line.charAt(xPosition);
	}

	/**
	 * Returns the difficulty of this level.
	 *
	 * The difficulty is a subjective value set be the user.
	 *
	 * @return the difficulty of this level
	 */
	public final String getDifficulty() {
		return difficulty;
	}

	/**
	 * Sets the difficulty of this level to the passed value.
	 *
	 * @param difficulty the difficulty to set
	 */
	public final void setDifficulty(String difficulty) {
		this.difficulty = difficulty;
	}

	/**
	 * Returns the number boxes of this level.
	 *
	 * @return the number of boxes
	 */
	public final int getBoxCount() {
		return boxCount;
	}

	/**
	 * Sets the number of boxes of this level.
	 *
	 * @param boxCount the boxCount to set
	 */
	public final void setBoxCount(int boxCount) {
		this.boxCount = boxCount;
	}

	/**
	 * Returns whether this level is connected with the database.
	 * <p>
	 * A connection means the primary key for this level is known in the DB.
	 *
	 * @return <code>true</code> if the level is connected with the DB,
	 * 	      <code>false</code> otherwise
	 */
	public final boolean isConnectedWithDB() {
		return getDatabaseID() != Database.NO_ID;
	}



	@Override
	public int hashCode() {
		final int prime = 31;
		int result = 1;
		result = prime * result + ((author == null) ? 0 : author.hashCode());
		result = prime * result + ((boardData == null) ? 0 : boardData.hashCode());
		result = prime * result + databaseID;
		result = prime * result + number;
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
		Level other = (Level) obj;
		if (author == null) {
			if (other.author != null) {
				return false;
			}
		} else if (!author.equals(other.author)) {
			return false;
		}
		if (boardData == null) {
			if (other.boardData != null) {
				return false;
			}
		} else if (!boardData.equals(other.boardData)) {
			return false;
		}
		if (databaseID != other.databaseID) {
			return false;
		}
		if (number != other.number) {
			return false;
		}
		return true;
	}

	@Override
	public String toString() {
		return "Level [levelTitle=" + levelTitle + ", boardData=" + boardData + "]";
	}


}