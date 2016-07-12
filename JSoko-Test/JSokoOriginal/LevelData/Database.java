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

import java.io.File;
import java.io.IOException;
import java.net.URLDecoder;
import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Statement;
import java.sql.Timestamp;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Date;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;

import javax.swing.event.EventListenerList;

import org.apache.commons.io.FileUtils;

import de.sokoban_online.jsoko.gui.Transformation;
import de.sokoban_online.jsoko.leveldata.levelmanagement.DatabaseDataEvent;
import de.sokoban_online.jsoko.leveldata.levelmanagement.DatabaseDataEvent.EventAction;
import de.sokoban_online.jsoko.leveldata.levelmanagement.DatabaseEventListener;
import de.sokoban_online.jsoko.leveldata.solutions.Solution;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Debug;
import de.sokoban_online.jsoko.utilities.Utilities;
import de.sokoban_online.jsoko.utilities.OSSpecific.OSSpecific;


/**
 * This class is an interface to the database which stores all level data, including solutions. It manages a connection and handles details
 * of SQL. Here we also handle the upgrading of the DB contents to extended data models.
 */
public class Database implements DatabaseEventListener {

	/** Constant representing no connection to the database -> invalid ID. */
	public static final int NO_ID = -1;

	/** The version of this software regarding the data model of the DB. */
	private final String currentDatabaseVersion = "1.3";

	/** Flag indicating whether the database is blocked by another process. */
	private static boolean isBlockedByAnotherProcess = false;

	/** The connection to the database. */
	public Connection conn;

	/**
	 * Dummy author for saving delegate levels. Delegate levels are levels that are automatically saved by JSoko to store solutions for the
	 * user for levels which haven't been imported to the database by the user. Since the authors for these levels aren't important all
	 * delegate levels are saved using this dummy author.
	 */
	private final Author DUMMY_AUTHOR = new Author(NO_ID, Texts.getText("theSokobanGame"), "", "", "");

	/** ID of the collection that contains the delegate levels. The value of this variable must be read by calling {@link #getDelegateLevelsCollectionID()} */
	private int delegateLevelsCollectionID = NO_ID;

	/** A list of event listeners for this database. */
    private EventListenerList listenerList = new EventListenerList();

	/**
	 * Creates an object to access the database stored in the passed file path.<br>
	 * If there isn't any database stored at the passed file path a new database is created.<br>
	 * An existing database is automatically updated to the database version needed for the current JSoko version.
	 *
	 * @param databaseFilePath  path to the file the database is stored in
	 * @throws SQLException thrown when the database can't be accessed due to an error
	 */
	private Database(String databaseFilePath) throws SQLException {

		// Load the HSQL Database Engine JDBC driver
		try {
			Class.forName("org.hsqldb.jdbcDriver").newInstance();
			String databaseURL = URLDecoder.decode(databaseFilePath, "UTF-8");
			conn = DriverManager.getConnection("jdbc:hsqldb:" + databaseURL, "sa", "");
		} catch (SQLException e) {
			if (e.getMessage().startsWith("The database is already in use by another process")
		  	 || e.getMessage().startsWith("File input/output") || e.getMessage().startsWith("Database lock acquisition failure")) {
				isBlockedByAnotherProcess = true;
				throw new SQLException(Texts.getText("databaseIsBlocked"));
			}
		}
		catch(Exception e) {
			throw new SQLException(e.getLocalizedMessage(), e);
		}

		try {
			// Check if there is a database by selecting something from the DB.
			query("SELECT version from databaseInformation");

			// Update the database if necessary.
			updateToLatestDatabaseVersion();
		}
		catch(SQLException e) {
			/* There is no database, yet => create one. This shouldn't happen since JSoko 1.74 uses a default DB in this case. */
			createDatabaseTables();
			update("INSERT INTO databaseInformation(version) VALUES(" + currentDatabaseVersion + ")");
		}
	}

	/**
	 * Returns the database for managing level data in JSoko.
	 * <p>
	 * If there is no database in the application directory, yet, the default
	 * database bundled with the JSoko installation package is copied to the application directory.
	 *
	 * @return the {@code Database} of JSoko or null when an error occurred
	 * @throws SQLException thrown when the database can't be accessed due to an error
	 */
	public static Database connectToJSokoDB() throws SQLException {

		final String databasePath = OSSpecific.getAppDataDirectory()+"database/jsokoDB";

		// Copy the bundled default database of JSoko since there is no database, yet.
		if(!new File(databasePath+".data").exists()) {
			try {
				FileUtils.copyDirectory(new File(Utilities.getBaseFolder()+"defaultDatabase"), new File(OSSpecific.getAppDataDirectory()+"database/"));
			} catch (IOException e) {
				// Do nothing here. The database will create a new fresh database without any content.
				if(Debug.isDebugModeActivated) {
					e.printStackTrace();
				}
			}
		}

		return Database.connectTo(databasePath);
	}


	/**
	 * Creates a connection and also checks the version of the DB contents against the version of this software.<br>
	 * When this software is newer than the database contents, those contents are upgraded immediately to fit this software version.
	 * <p>
	 * When the database isn't used anymore {@link #shutdown()} must be called.
	 *
	 * @param databaseFilePath
	 *            path to the file the database is stored in
	 * @throws SQLException thrown when the database can't be accessed due to an error
	 */
	public static Database connectTo(String databaseFilePath) throws SQLException {
		return new Database(databaseFilePath);
	}

	/**
	 * Checks the version of the connected database and updates it if necessary.
	 */
	private void updateToLatestDatabaseVersion() {

		ResultSet result;
		// Check if it is necessary to upgrade the database.
		String dbversion = null;
		try {
			result = query("SELECT version from databaseInformation");

			if (result.next() == true) {
				dbversion = result.getString("version");
			}

			// Check if the current database is older than the one this program uses.
			// FFS/hm: string comparison is not exactly what we mean.
			if (dbversion != null && dbversion.compareTo(currentDatabaseVersion) < 0) {
				// The version in the DB is smaller than the version this
				// software is for: we may want an upgrade of the DB contents.

				if (dbversion.equals("1.0")) {
					// As of version 1.48 of JSoko the solutions order is saved in the database as well as
					// information how a solution is displayed / highlighted.
					// Therefore new columns are added to the solutions table.
					update("ALTER TABLE solutions ADD COLUMN orderValue    INTEGER        DEFAULT 0  BEFORE lastChanged");
					update("ALTER TABLE solutions ADD COLUMN highLightData VARCHAR(5000)  DEFAULT '' BEFORE lastChanged");

					// Set version to 1.1
					update("UPDATE databaseInformation SET version = " + currentDatabaseVersion);
				}

				if (dbversion.equals("1.0") || dbversion.equals("1.1")) {
					// Remove constraint for later upgrades to HSQLDB 2.X
					update("ALTER TABLE COLLECTIONLEVEL DROP CONSTRAINT SYS_CT_59");

					// New data for savegames.
					update("ALTER TABLE savegames ADD COLUMN orderValue    INTEGER        DEFAULT 0  BEFORE lastChanged");
					update("ALTER TABLE savegames ADD COLUMN highLightData VARCHAR(5000)  DEFAULT '' BEFORE lastChanged");

					// Create indices for faster selects.
					update("CREATE INDEX IX_authorData_name ON authorData (name)");
					update("CREATE INDEX IX_levelData_boardData  ON levelData (boardData)");
					update("CREATE INDEX IX_levelData_levelTitle ON levelData (levelTitle)");
				}

				if (dbversion.equals("1.0") || dbversion.equals("1.1") || dbversion.equals("1.2")) {

					// Set sizes higher than 32KB for those columns that need a higher size because HSQLDB uses
					// 32KB for varchar as default when migrating from version 1.8 to version 2.X
					// update("ALTER TABLE levelData  		ALTER COLUMN boardData     		SET DATA TYPE VARCHAR(1M)");
					// update("ALTER TABLE levelData  		ALTER COLUMN levelComment  		SET DATA TYPE VARCHAR(1M)");
					// update("ALTER TABLE levelData  		ALTER COLUMN levelTitle    		SET DATA TYPE VARCHAR(1M)");
					//
					// update("ALTER TABLE solutions  		ALTER COLUMN solutionLURD  		SET DATA TYPE VARCHAR(100M)");
					// update("ALTER TABLE solutions  		ALTER COLUMN solutionComment 	SET DATA TYPE VARCHAR(1M)");
					//
					// update("ALTER TABLE collectionData	ALTER COLUMN collectionComment 	SET DATA TYPE VARCHAR(1M)");
					// update("ALTER TABLE collectionData	ALTER COLUMN collectionTitle 	SET DATA TYPE VARCHAR(1M)");
					//
					// update("ALTER TABLE snapshots  		ALTER COLUMN snapshotLURD  		SET DATA TYPE VARCHAR(100M)");
					//
					// update("ALTER TABLE authorData 		ALTER COLUMN name  				SET DATA TYPE VARCHAR(1M)");
					// update("ALTER TABLE authorData 		ALTER COLUMN email  			SET DATA TYPE VARCHAR(1M)");
					// update("ALTER TABLE authorData 		ALTER COLUMN homepage  			SET DATA TYPE VARCHAR(1M)");
					// update("ALTER TABLE authorData 		ALTER COLUMN authorComment  	SET DATA TYPE VARCHAR(1M)");

					// Add column for saving the information whether the snapshot has been saved by JSoko when the level has been left.
					// SaveGame = auto saving of a snapshot by JSoko
					// snapshot = saving a snapshot by the user
					// both is saved in the saveGames table.
					update("ALTER TABLE savegames ADD COLUMN isAutoSaved BOOLEAN  DEFAULT false BEFORE lastChanged");

					// If a level isn't stored in the database but solutions / snapshots have to be stored
					// then a level is automatically imported to the database by JSoko as a "delegate level".

					// This flag indicates this auto imported level.
					update("ALTER TABLE levelData ADD COLUMN isDelegate BOOLEAN  DEFAULT false BEFORE lastChanged");

					// Delegate levels are stored in a special collection for delegate levels.
					update("ALTER TABLE collectionData ADD COLUMN isDelegeLevelsCollection BOOLEAN  DEFAULT false  BEFORE lastChanged");

					// Savegame is just a special case of a snapshot => rename table to snapshots.
					update("ALTER TABLE savegames RENAME TO snapshots");
					update("ALTER TABLE snapshots ALTER COLUMN savegameID   RENAME TO snapshotID");
					update("ALTER TABLE snapshots ALTER COLUMN savegameLURD RENAME TO snapshotLURD");

					/**
					 *  ATTENTION: if something is changed here (new columns, ...)
					 *  the default DB bundled with JSoko must be created with the newest DB version!!!
					 *  See folder: development/Level collections bundled with JSoko  for the level collection files to import
					 *  Ensure that no "unregistered" collection is created in the default DB!
					 */

				}

				update("UPDATE databaseInformation SET version = " + currentDatabaseVersion);

			}
		} catch (SQLException e) {
			e.printStackTrace();
		}
	}

	/** Creates the database tables. */
	final private void createDatabaseTables() {

		/* Create the database. If there is already one, the exception is ignored (inside update()), although we will get some console output. */

		// Database version
		update("CREATE CACHED TABLE databaseInformation(version VARCHAR(10) )");

		// Authors
		update("CREATE MEMORY TABLE authorData("
				+ "authorID 	 INTEGER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,"
				+ "name			 VARCHAR_IGNORECASE(1000000)  	DEFAULT '',"
				+ "email		 VARCHAR_IGNORECASE(1000000)	DEFAULT '',"
				+ "homepage 	 VARCHAR_IGNORECASE(1000000)	DEFAULT '',"
				+ "authorComment VARCHAR_IGNORECASE(1000000)	DEFAULT '',"
				+ "lastChanged 	 TIMESTAMP						NOT NULL)");
		update("CREATE INDEX IX_authorData_name ON authorData (name)");

		// Collection data
		update("CREATE MEMORY TABLE collectionData("
				+ "collectionID 	 		INTEGER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,"
				+ "collectionTitle 	 		VARCHAR_IGNORECASE(1000000) DEFAULT '',		"
				+ "authorID			 		INTEGER   				 	NOT NULL,		"
				+ "collectionComment 		VARCHAR_IGNORECASE(1000000) DEFAULT '',		"
				+ "isDelegeLevelsCollection BOOLEAN						DEFAULT false, 	" // true = collection for storing delegate levels
				+ "lastChanged 		 		TIMESTAMP 					NOT NULL,		"
				+ "FOREIGN KEY (authorID) REFERENCES authorData (authorID))");

		// Level data
		update("CREATE CACHED TABLE levelData("
				+ "levelID    		INTEGER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,"
				+ "levelTitle 		VARCHAR_IGNORECASE(1000000)	DEFAULT '',"
				+ "authorID			INTEGER	  		 			NOT NULL,"
				+ "boardData		VARCHAR(1000000) 			DEFAULT '',"
				+ "width   		    TINYINT	  		 			DEFAULT 0,"
				+ "height  			TINYINT	  		 			DEFAULT 0,"
				+ "levelComment 	VARCHAR_IGNORECASE(1000000) DEFAULT '',"
				+ "numberOfBoxes    TINYINT   					DEFAULT 0,"
				+ "view             VARCHAR_IGNORECASE(100)   	DEFAULT '',"
				+ "difficulty       VARCHAR_IGNORECASE(100)	 	DEFAULT '',"
				+ "isDelegate       BOOLEAN                    	DEFAULT false, " // true = level is stored as delegate to stroe solutions and snapshots for a "normal" level
				+ "lastChanged 		TIMESTAMP 		 			NOT NULL,"
				+ "FOREIGN KEY (authorID) REFERENCES authorData (authorID))");
		update("CREATE INDEX IX_levelData_boardData  ON levelData (boardData)");
		update("CREATE INDEX IX_levelData_levelTitle ON levelData (levelTitle)");

		// Position of a level in a collection
		update("CREATE MEMORY TABLE collectionLevel("
				+ "collectionID 	 INTEGER NOT NULL,"
				+ "levelID		     INTEGER NOT NULL,"
				+ "levelNumber       INTEGER NOT NULL,"
				+ "PRIMARY KEY (collectionID, levelID),"
				+ "FOREIGN KEY (collectionID) REFERENCES collectionData (collectionID),"
				+ "FOREIGN KEY (levelID) 	  REFERENCES levelData (levelID))");

		// Solutions
		update("CREATE CACHED TABLE solutions("
				+ "solutionID 	 	INTEGER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,"
				+ "solutionName		VARCHAR_IGNORECASE(100)   	DEFAULT '',"
				+ "isOwnSolution    BOOLEAN	  					DEFAULT false,"
				+ "levelID			INTEGER   					NOT NULL,"
				+ "solutionLURD		VARCHAR(100000000)			DEFAULT '',"
				+ "movesCount		INTEGER	  					DEFAULT 0,"
				+ "pushesCount		INTEGER	  					DEFAULT 0,"
				+ "boxLines         INTEGER	  					DEFAULT 0,"
				+ "boxChanges       INTEGER	  					DEFAULT 0,"
				+ "pushingSessions  INTEGER	  					DEFAULT 0,"
				+ "solutionComment  VARCHAR_IGNORECASE(1000000)	DEFAULT '',"
				+ "orderValue 		INTEGER	  					DEFAULT 0,"
				+ "highLightData	VARCHAR(5000)   			DEFAULT ''," // additional data: color of solution, ... separated by ;
				+ "lastChanged 		TIMESTAMP 					NOT NULL,"
				+ "FOREIGN KEY (levelID) REFERENCES levelData (levelID),"
				+ "UNIQUE (levelID, solutionLURD))");

		// Snapshots
		update("CREATE CACHED TABLE snapshots("
				+ "snapshotID 	 	INTEGER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,"
				+ "snapshotLURD		VARCHAR(100000000) 			DEFAULT '',"
				+ "levelID			INTEGER   	 				NOT NULL,"
				+ "savegameComment  VARCHAR_IGNORECASE(5000)   	DEFAULT '',"
				+ "orderValue 		INTEGER	  					DEFAULT 0,"
				+ "highLightData	VARCHAR(5000)   			DEFAULT '',"
				+ "isAutoSaved      BOOLEAN						DEFAULT false," // auto saved = saved by JSoko when the level is closed to restore the history when reopening the level
				+ "lastChanged 		TIMESTAMP 					NOT NULL," + "FOREIGN KEY (levelID) REFERENCES levelData (levelID))");

	}

	/**
	 * Returns the level data of a specific level stored in the database.
	 *
	 * @param levelID  ID of the level the data are returned for
	 * @return the level data of the level represented by the passed level ID, or {@code null}
	 */
	final public Level getLevel(int levelID) {

		// Select the relevant level data from the database.
		try {
			ResultSet result = query("SELECT * "
					+ "FROM levelData l INNER JOIN authorData         a  on l.authorID 	    = a.authorID 	  "
					+ "				  LEFT OUTER JOIN collectionLevel cl on l.levelID 	    = cl.levelID 	  "
					+ " 			  LEFT OUTER JOIN collectionData  cd on cl.collectionID = cd.collectionID " + "WHERE levelID = " + levelID);
			if (result == null || result.next() == false) {
				return null;
			}

			// Create a new Level object.
			Level level = new Level(this);

			/* Store the level data in the Level object. */
			level.setLevelTitle(result.getString("levelTitle"));

			String[] boardDataStrings = result.getString("boardData").split("\n");
			List<String> boardData = Arrays.asList(boardDataStrings);

			level.setBoardData(boardData);
			level.setWidth(result.getInt("width"));
			level.setHeight(result.getInt("height"));
			level.setComment(result.getString("levelComment"));
			level.setTransformationString(result.getString("view"));
			level.setBoxCount(result.getInt("numberOfBoxes"));
			level.setDifficulty(result.getString("difficulty"));
			level.setNumber(result.getInt("levelNumber"));
			level.setDelegate(result.getBoolean("isDelegate"));

			level.setAuthor(getAuthorFromResult(result));

			// Load all solutions of the level.
			ArrayList<Solution> solutions = getSolutions(levelID);
			for (Solution solution : solutions) {

				// Solutions from the database have always been verified before they have been stored.
				solution.isSolutionVerified = true;

				level.addSolution(solution);
			}

			// Load all snapshots of the level.
			for (Snapshot snapshot : getSnapshots(levelID)) {
				level.addSnapshot(snapshot);
			}

			// The database ID is set as last data. Otherwise adding solutions or snapshots
			// to the level would result in unnecessary tries to store the solutions/snapshots
			// in the database again.
			level.setDatabaseID(levelID);

			// Return the Level object.
			return level;

		} catch (SQLException e) {
			e.printStackTrace();
		}

		return null;
	}

	/**
	 * Returns a new {@code Author} using the data of the passed {@ResultSet}.
	 *
	 * @param result
	 *            the {@code ResultSet} to extract the data of
	 * @return a new {@code Author}
	 * @throws SQLException
	 */
	private Author getAuthorFromResult(ResultSet result) throws SQLException {

		int databaseID    = result.getInt("authorID");
		String name       = result.getString("name");
		String email      = result.getString("email");
		String websiteURL = result.getString("homepage");
		String comment 	  = result.getString("authorComment");

		return new Author(databaseID, name, email, websiteURL, comment);
	}

	/**
	 * Returns the {@code Snapshot}s stored in the database for the passed level.
	 *
	 * @param levelID  the database ID of the level the {@code Snapshot}s are to be returned for
	 * @return the stored {@code Snapshot}s of the passed level */
	public ArrayList<Snapshot> getSnapshots(int levelID) {

		ArrayList<Snapshot> snapshots = new ArrayList<Snapshot>();

		if (levelID < 0) {
			return snapshots;
		}

		try {
			// Select all snapshots from the database.
			ResultSet result = query("SELECT  * FROM snapshots WHERE levelID = " + levelID);
			if (result == null) {
				return snapshots;
			}

			while (result.next()) {
				Snapshot snapshot = new Snapshot(result.getString("snapshotLURD"));
				snapshot.setDatabaseID(result.getInt("snapshotID"));
				snapshot.setComment(result.getString("savegameComment"));
				snapshot.setOrderValue(result.getInt("orderValue"));
				snapshot.setHighLightData(result.getString("highLightData"));
				snapshot.setAutoSaved(result.getBoolean("isAutoSaved"));
				snapshot.setLastChanged(result.getTimestamp("lastChanged"));

				snapshots.add(snapshot);
			}
		} catch (SQLException e) {
			if (Debug.isDebugModeActivated) {
				e.printStackTrace();
			}
		}

		return snapshots;
	}

	/**
	 * Returns all levels whose boards are identical to the board passed as String.
	 *
	 * @param boardData
	 *            the board of a level as <code>String</code>
	 * @return {@code ArrayList} of all levels whose boards are identical to the board passed as {@code String}
	 */
	public ArrayList<Level> getLevel(String boardData) {

		ArrayList<Level> levels = new ArrayList<Level>();

		try {
			// Try to read the level from the database.
			ResultSet result = query("SELECT levelID FROM levelData WHERE boardData = '" + boardData + "'");

			while (result.next()) {
				// Add the level to the list.
				levels.add(getLevel(result.getInt("levelID")));
			}
		} catch (SQLException e) {
			e.printStackTrace();
		}

		// Return the found levels.
		return levels;
	}

	/**
	 * Stores the passed level data in the database.
	 * <p>
	 * This methods sets the resulting database IDs in the passed {@code Level}.<br>
	 * If the level had already been stored before it's stored again using another ID.
	 * However, authors are only stored once comparing the author name.
	 *
	 * @param level  the level to be saved
	 */
	public void insertLevel(Level level) {

		// The result set of a select.
		ResultSet result;

		// The level ID in the database
		int levelID = -1;

		try {
			// Check if the author is already stored in the database (comparing the author name).
			// If not save the author.
			int authorID = getAuthorID(level.getAuthor());
			if(authorID == NO_ID) {
				authorID = insertAuthor(level.getAuthor());
			}

			// Create a new author containing the database ID and set this author for the level.
			Author author = level.getAuthor().setDatabaseID(authorID);
			level.setAuthor(author);

			// Insert the level data to the database.
			PreparedStatement p =
					prepareStatement("INSERT INTO levelData (levelTitle, authorID, boardData, width, height, "
					+ "levelComment, numberOfBoxes, view, difficulty, isDelegate, lastChanged) values(?, ?, ?, ?, ?, ?, ?, ?, ?, ?, CURRENT_TIMESTAMP)");
			p.clearParameters();
			int parameterIndex = 1;
			p.setString(parameterIndex++,  level.getTitle());
			p.setInt(parameterIndex++, 	   authorID);
			p.setString(parameterIndex++,  level.getBoardDataAsString());
			p.setInt(parameterIndex++, 	   level.getWidth());
			p.setInt(parameterIndex++, 	   level.getHeight());
			p.setString(parameterIndex++,  level.getComment());
			p.setInt(parameterIndex++, 	   level.getBoxCount());
			p.setString(parameterIndex++,  level.getTransformationString());
			p.setString(parameterIndex++,  level.getDifficulty());
			p.setBoolean(parameterIndex++, level.isStoredAsDelegate());
			p.executeUpdate();

			// Set the levelID of the level.
			result = query("call IDENTITY()");
			if (result.next()) {
				levelID = result.getInt(1);
			}
			level.setDatabaseID(levelID);

			// Insert the solutions of the level.
			for (Solution solution : level.getSolutionsManager().getSolutions()) {
				insertSolution(solution, levelID);
			}

			// Insert the snapshots and the save game.
			for(Snapshot snapshot : level.getSnapshots()) {
				insertSnapshot(snapshot, levelID);
			}

			fireDatabaseDataChanged(new DatabaseDataEvent(this, Level.class, EventAction.INSERT, levelID));

		} catch (SQLException e) {
			e.printStackTrace();
		}
	}

	/**
	 * Stores the passed level data in the database.
	 * <p>
	 * This methods sets the resulting database IDs in the passed {@code Level}.
	 *
	 * @param level  the level to be saved
	 */
	public void insertAsDelegateLevel(Level level) {

		// Mark the level as delegate level.
		level.setDelegate(true);

		insertLevel(level);

		// Add the level to the delegate levels collection.
		addLevelToCollection(level.getDatabaseID(), getDelegateLevelsCollectionID());
	}

	/**
	 * Updates the following data of a level:<br>
	 * <ul>
	 * 	<li> title 		</li>
	 *  <li> authorID 	</li>
	 *  <li> comment 	</li>
	 *  <li> difficulty </li>
	 *  <li> timestamp 	</li>
	 * </ul>
	 *
	 * @param levelID  			the database ID of the level to be updated
	 * @param newTitle  		the new title to be set
	 * @param newAuthorID  		the new ID of an author to be set
	 * @param newLevelComment   the new level comment to be set
	 * @param newDifficulty  	the new difficulty to be set
	 * @param newTimestamp  	the new timestamp to be set
	 */
	public void updateLevel(int levelID, String newTitle, int newAuthorID, String newLevelComment, String newDifficulty, Timestamp newTimestamp) {

		// Update the data on the database.
		String statement = "UPDATE levelData SET levelTitle = ?, authorID = ?, levelComment = ?, difficulty = ?, lastChanged = ? WHERE levelID = ?";
		PreparedStatement p = prepareStatement(statement);
		int parameterIndex = 1;
		try {
			p.setString(parameterIndex++, 	 newTitle);
			p.setInt(parameterIndex++, 		 newAuthorID);
			p.setString(parameterIndex++, 	 newLevelComment);
			p.setString(parameterIndex++, 	 newDifficulty);
			p.setTimestamp(parameterIndex++, newTimestamp);
			p.setInt(parameterIndex++, 		 levelID);

			p.executeUpdate();
		}catch(SQLException e) {e.printStackTrace();}

		fireDatabaseDataChanged(new DatabaseDataEvent(this, Level.class, EventAction.CHANGE, levelID));
	}

	/**
	 * Deletes the level stored under the passed ID.
	 *
	 * @param levelID  database ID of the level to be deleted
	 */
	public void deleteLevel(int levelID) {

		update("DELETE FROM solutions where levelID       = " + levelID);
		update("DELETE FROM snapshots where levelID       = " + levelID);
		update("DELETE FROM collectionLevel where levelID = " + levelID);
		update("DELETE FROM levelData where levelID       = " + levelID);

		fireDatabaseDataChanged(new DatabaseDataEvent(this, Level.class, EventAction.DELETE, levelID));
	}

	/**
	 * Assigns the passed level ID to the passed collection ID.
	 * <p>
	 * This adds the level to the collection. If the level is already assigned to the collection it's not assigned twice.
	 *
	 * @param levelID
	 *            the ID of the level to be added
	 * @param collectionID
	 *            the ID of the level collection the level is added to
	 * @return <code>true</code> when level has been added, <code>false</code> when level is already assigned
	 */
	public boolean addLevelToCollection(int levelID, int collectionID) {

		int newLevelNumber = 1;

		// Initialize the new level number with the current highest one.
		try {
			ResultSet result = null;

			// Return "false" if the level is already assigned.
			result = query("SELECT levelID FROM collectionLevel " + " WHERE levelID      = " + levelID + "   AND collectionID = " + collectionID);
			if (result == null || result.next()) {
				return false;
			}

			result = query("SELECT MAX(levelNumber)  " + " FROM collectionLevel    " + " WHERE collectionID    = " + collectionID);
			result.next();
			newLevelNumber = result.getInt(1) + 1;

		} catch (SQLException e2) {
			e2.printStackTrace();
			return false;
		}

		// Assign the level to the collection.
		boolean isUpdated = update("INSERT INTO collectionLevel (collectionID, levelID, levelNumber) " +
				"values(" + collectionID + ", " + levelID + ", " + newLevelNumber + ")");

		if(isUpdated) {
			fireDatabaseDataChanged(new DatabaseDataEvent(this, LevelCollection.class, EventAction.CHANGE, collectionID));
		}

		return isUpdated;
	}

	/**
	 * Returns the ID of the level collection containing the delegate levels.
	 * <p>
	 * The levels that are saved automatically by JSoko (-> delegate levels) are assigned to a special collection. The ID of this collection
	 * is returned by this method.
	 * <p>
	 * If necessary the collection for the delegates is stored in the db by this method.
	 *
	 * @return the ID of the delegate levels collection
	 */
	public int getDelegateLevelsCollectionID() {

		if (delegateLevelsCollectionID != NO_ID) {
			return delegateLevelsCollectionID;
		}

		// Check whether a collection for the delegates is already stored in the db.
		try {
			ResultSet result = null;
			result = query("SELECT collectionID  FROM collectionData  WHERE isDelegeLevelsCollection = true ");
			if (result != null && result.next()) {
				delegateLevelsCollectionID = result.getInt("collectionID");
			} else {
				LevelCollection delegatesCollection = new LevelCollection.Builder().setAuthor(DUMMY_AUTHOR).setDelegate(true)
						.setTitle(Texts.getText("database.delegateLevelCollection"))
						.setComment(Texts.getText("database.delegateLevelCollection.comment")).build();
				delegatesCollection = insertLevelCollectionWithoutLevels(delegatesCollection);
				delegateLevelsCollectionID = delegatesCollection.getDatabaseID();
			}

		} catch (SQLException e2) {
			if (Debug.isDebugModeActivated) {
				e2.printStackTrace();
			}
		}

		return delegateLevelsCollectionID;
	}

	/**
	 * Sets the levels stored under the passed IDs as levels of the passed collection.
	 * <p>
	 * This method removes all currently assigned levels of the passed collection and sets the passed levels as new levels of the
	 * collection.
	 *
	 * @param collectionID
	 *            collection to assign the levels to
	 * @param levelIDs
	 *            the IDs of the levels to be assigned in ascending order regarding the level numbers
	 */
	public void setCollectionLevels(int collectionID, ArrayList<Integer> levelIDs) {

		// Delete the assignments of the whole collection.
		update("DELETE FROM collectionLevel WHERE collectionID = " + collectionID);

		// Insert the levels.
		int levelNumber = 1;
		for (int levelID : levelIDs) {
			update("INSERT INTO collectionLevel (collectionID, levelID, levelNumber) values("
					+ collectionID + "," + levelID + "," + levelNumber + ")");
			levelNumber++;
		}

		fireDatabaseDataChanged(new DatabaseDataEvent(this, LevelCollection.class, EventAction.CHANGE, collectionID));
	}

	/**
	 * Returns the <code>LevelCollection</code> stored by the passed ID. If the collection is not known in the DB, {@code null} is returned
	 *
	 * @param collectionID
	 *            ID of the collection to retrieve
	 * @return the <code>LevelCollection</code>, or {@code null}
	 */
	public LevelCollection getLevelCollection(int collectionID) {

		LevelCollection.Builder levelCollectionBuilder = new LevelCollection.Builder();

		try {
			// Select the levelIDs of the levels assigned to the passed collection
			ResultSet result = query("SELECT levelID FROM collectionLevel " +
									 "WHERE collectionID = " + collectionID +
									 " ORDER BY levelNumber");

			// Add all levels of the collection to a list.
			ArrayList<Level> collectionLevels = new ArrayList<Level>();
			while (result.next()) {
				collectionLevels.add(getLevel(result.getInt("levelID")));
			}

			// Read the collection data.
			result = query("SELECT * FROM collectionData  cd  INNER JOIN  authorData ad  ON cd.authorID = ad.authorID"
					+ "  WHERE collectionID = " + collectionID);

			while (result.next()) {
				levelCollectionBuilder.setDatabaseID(result.getInt("collectionID"))
					.setTitle(result.getString("collectionTitle"))
					.setComment(result.getString("collectionComment"))
					.setDelegate(result.getBoolean("isDelegeLevelsCollection"))
					.setAuthor(getAuthorFromResult(result))
					.setLevels(collectionLevels);
			}
		} catch (SQLException e) {
			e.printStackTrace();
			return null;
		}

		LevelCollection levelCollection = levelCollectionBuilder.build();

		return levelCollection.isConnectedWithDatabase() ? levelCollection : null;
	}

	/**
	 * Updates the following data of a collection:<br>
	 * <ul>
	 * 	<li> title     </li>
	 *  <li> authorID  </li>
	 *  <li> comment   </li>
	 *  <li> timestamp </li>
	 * </ul>
	 *
	 * @param collectionID
	 * @param newTitle
	 * @param newAuthorID
	 * @param newComment
	 * @param newTimestamp
	 */
	public void updateCollectionData(int collectionID, String newTitle, int newAuthorID, String newComment, Timestamp newTimestamp) {

		// Update the data on the database.
		String statement = "UPDATE collectionData "
						 + " SET collectionTitle = ?, authorID = ?, collectionComment = ?, lastChanged = ? "
						 + " WHERE collectionID = ?";
		PreparedStatement p = prepareStatement(statement);
		int parameterIndex = 1;
		try {
			p.setString(   parameterIndex++, newTitle);
			p.setInt(      parameterIndex++, newAuthorID);
			p.setString(   parameterIndex++, newComment);
			p.setTimestamp(parameterIndex++, newTimestamp);
			p.setInt(      parameterIndex++, collectionID);

			p.executeUpdate();

			fireDatabaseDataChanged(new DatabaseDataEvent(this, LevelCollection.class, EventAction.CHANGE, collectionID));

		}catch(SQLException e) {e.printStackTrace();}

	}

	/**
	 * Sets new level numbers for the levels of the passed level collection.
	 * <p>
	 * This method must be called when the collection levels change in order to keep the level numbers up to date.
	 *
	 * @param collectionID
	 *            ID of the collection to renumber the levels for
	 */
	public void updateLevelNumbers(int collectionID) {

		try {
			ResultSet result = query("SELECT levelID FROM collectionLevel "
					+ " WHERE collectionID = " + collectionID
					+ " ORDER BY levelNumber");

			int newLevelNumber = 1;
			while (result.next()) {
				update("UPDATE collectionLevel  "
						+ " SET levelNumber    = " + (newLevelNumber++)
						+ " WHERE collectionID = " + collectionID
						+ " AND levelID        = " + result.getInt("levelID"));
			}

			fireDatabaseDataChanged(new DatabaseDataEvent(this, LevelCollection.class, EventAction.CHANGE, new ArrayList<Integer>(collectionID)));

		} catch (SQLException e2) {
			e2.printStackTrace();
		}
	}

	/**
	 * Returns a {@code List} of all collections stored in the database.
	 * <p>
	 * The returned {@code LevelCollection}s are only filled with the following data from the database:
	 * <ul>
	 * <li>database ID
	 * <li>title
	 * <li>comment
	 * </ul>
	 *
	 * @return {@code List} of {@code LevelCollection}s
	 */
	public List<LevelCollection> getCollectionsInfo() {

		ArrayList<LevelCollection> levelCollections = new ArrayList<LevelCollection>();

		try {
			// Read the collection data.
			ResultSet result = query("SELECT * FROM collectionData");

			while (result.next()) {
				LevelCollection levelCollection = new LevelCollection.Builder()
					.setDatabaseID(result.getInt("collectionID"))
					.setTitle(result.getString("collectionTitle"))
					.setComment(result.getString("collectionComment"))
					.setDelegate(result.getBoolean("isDelegeLevelsCollection")).build();
				levelCollections.add(levelCollection);
			}
		} catch (SQLException e) { /* do nothing but return an empty list. */
		}

		return levelCollections;
	}

	/**
	 * Returns the titles and the IDs of all level collections the passed levelID is assigned to in a <code>HashMap</code>, mapping the
	 * collection ID to its title.
	 *
	 * @param levelID
	 *            the ID of the level the assigned collections are returned for
	 * @return <code>HashMap</code> containing the titles and the IDs of all collections the level is assigned to
	 */
	public HashMap<Integer, String> getInfoAboutAssignedCollections(int levelID) {

		// Title and ID of all collections the level is assigned to.
		HashMap<Integer, String> collectionInfos = new HashMap<Integer, String>(3);

		try {
			// Try to read the level from the database.
			ResultSet result =
					query("SELECT collectionTitle, collectionID	" +
						  "FROM collectionLevel cl inner join collectionData cd "
						+ "  on cl.collectionID = cd.collectionID 			    "
						+ " WHERE cl.levelID = " + levelID);

			// Get the collections the level in the database is assigned to
			// and store the relevant data.
			while (result.next()) {
				// Save the information in the return object.
				collectionInfos.put(result.getInt("collectionID"), result.getString("collectionTitle"));
			}
		} catch (SQLException e) { /* do nothing */
		}

		return collectionInfos;
	}

	/**
	 * Stores the current collection in the database.
	 * <p>
	 * This may result in the level being stored in the database multiple times.<br>
	 * Even if the level is currently only stored as delegate the delegate isn't deleted
	 * after saving the level as regular one.
	 *
	 * @param levelCollection  collection to be saved
	 * @return the saved {@code LevelCollection} containing all database IDs or {@code null} if the collection couldn't be saved
	 */
	public LevelCollection insertLevelCollection(LevelCollection levelCollection) {

		// The number of the level in the collection.
		int levelNumber = 1;

		// The levels that have been saved in this database.
		ArrayList<Level> savedLevels = new ArrayList<Level>();

		// Save the collection data in the database.
		try {
			levelCollection = insertLevelCollectionWithoutLevels(levelCollection);

			// Save all levels of the collection.
			for (Level level : levelCollection) {

				// The level may be a delegate. However, after being saved it's just a normal level.
				level.setDelegate(false);

				// Delegate stays in the db! If it were deleted then the logic when loading the level from
				// hard disk must be changed so that the solutions of regular levels are also added to the delegate level,
				// in order to keep the solutions for the delegate in case it is loaded from disk again.

				// Save the level in the database.
				insertLevel(level);

				// Add the level to the end of the collection.
				update("INSERT INTO collectionLevel (collectionID, levelID, levelNumber) values(" + levelCollection.getDatabaseID() + ","
						+ level.getDatabaseID() + "," + levelNumber + ")");

				// Increase the level number for the next level.
				levelNumber++;

				savedLevels.add(level);
			}

			// Create a new level collection containing the database IDs of the levels.
			levelCollection = levelCollection.getBuilder().setLevels(savedLevels).build();

		} catch (SQLException e) {
			if (Debug.isDebugModeActivated) {
				e.printStackTrace();
			}
		}

		return levelCollection;
	}

	/**
	 * Stores the passed collection without saving the contained levels.
	 * <p>
	 * This methods returns a new level collection containing the database IDs for the saved author and the saved collection.
	 *
	 * @param levelCollection
	 *            collection to be saved
	 * @return the resulting {@code LevelCollection} containing the database IDs
	 * @throws SQLException
	 */
	public LevelCollection insertLevelCollectionWithoutLevels(LevelCollection levelCollection) throws SQLException {

		// Create a new author containing the database ID and set this author for the level collection.

		int authorID = getAuthorID(levelCollection.getAuthor());
		if(authorID == NO_ID) {
			authorID = insertAuthor(levelCollection.getAuthor());
		};
		Author newLevelCollectionAuthor = levelCollection.getAuthor().setDatabaseID(authorID);
		levelCollection = levelCollection.setAuthor(newLevelCollectionAuthor);

		// Insert the collection data.
		PreparedStatement p = prepareStatement("INSERT INTO collectionData (collectionTitle, authorID, collectionComment, isDelegeLevelsCollection, lastChanged) values(?, ?, ?, ?, CURRENT_TIMESTAMP)");
		p.clearParameters();
		p.setString(1, levelCollection.getTitle());
		p.setInt(2, authorID);
		p.setString(3, levelCollection.getComment());
		p.setBoolean(4, levelCollection.isDelegateLevelsCollection());
		p.executeUpdate();

		// Get the ID of the collection and save it.
		ResultSet result = query("call IDENTITY()");
		if (result.next()) {
			levelCollection = levelCollection.setDatabaseID(result.getInt(1));
		}

		fireDatabaseDataChanged(new DatabaseDataEvent(this, LevelCollection.class, EventAction.INSERT, levelCollection.getDatabaseID()));

		return levelCollection;
	}

	/**
	 * Deletes the collection stored under the passed database ID.
	 * <p>
	 * The collection data is deleted and all levels that have only be assigned to this collection but not to any other collection. If the
	 * author of the collection isn't used by any other collection or level the author is also deleted from the database.
	 *
	 * @param collectionID
	 *            the ID of the level collection to be deleted
	 */
	public void deleteLevelCollection(int collectionID) {

		LevelCollection collection = getLevelCollection(collectionID);

		// Delete the assignment of all levels of the collection.
		update("DELETE FROM collectionLevel WHERE collectionID = " + collectionID);

		HashSet<Integer> authorIDs = new HashSet<Integer>();
		authorIDs.add(collection.getAuthor().getDatabaseID());

		// Delete the levels of the collection that aren't assigned to any collection anymore.
		for (Level level : collection.getLevels()) {
			try {
				ResultSet result = query("SELECT levelID FROM collectionLevel WHERE levelID = " + level.getDatabaseID());
				if (!result.next()) {
					deleteLevel(level.getDatabaseID());
					authorIDs.add(level.getAuthor().getDatabaseID());
				}
			} catch (SQLException e) {
				e.printStackTrace();
			}
		}

		// Delete the collection data.
		update("DELETE FROM collectionData WHERE collectionID = " + collectionID);

		// Delete the authors of the collection and the levels if they aren't used anymore.
		for (int authorID : authorIDs) {
			try {
				ResultSet result = query("SELECT authorID FROM levelData 	   WHERE authorID = " + authorID);
				ResultSet result2 = query("SELECT authorID FROM collectionData WHERE authorID = " + authorID);
				if (!result.next() && !result2.next()) {
					deleteAuthor(authorID);
				}
			} catch (SQLException e) {
				e.printStackTrace();
			}
		}

		// Check if the user deleted the collection containing the delegate levels.
		if (collectionID == delegateLevelsCollectionID) {
			delegateLevelsCollectionID = NO_ID;
		}

		fireDatabaseDataChanged(new DatabaseDataEvent(this, LevelCollection.class, EventAction.DELETE, collectionID));
	}

	/**
	 * Saves the transformation of the passed level.
	 * <p>
	 * The transformation represents the current view on the level in the GUI.
	 *
	 * @see Transformation
	 *
	 * @param level
	 *            the {@code Level} to save the transformation for.
	 * @return <code>true</code> if the transformation has been updated, <code>false</code> otherwise
	 */
	public boolean updateTranformation(Level level) {

		// Ensure the solution is already saved.
		if (!level.isConnectedWithDB()) {
			return false;
		}

		try {
			// Prepared Statement for database access.
			PreparedStatement p;

			// Update the data. Note: the view change doesn't change the "lastChanged" time stamp.
			p = prepareStatement("UPDATE levelData SET view = ?" + "WHERE levelID = ?");

			int parameterIndex = 1;
			p.clearParameters();
			p.setString(parameterIndex++, level.getTransformationString());
			p.setInt(parameterIndex++, level.getDatabaseID());

			p.executeUpdate();

			fireDatabaseDataChanged(new DatabaseDataEvent(this, Level.class, EventAction.CHANGE, level.getDatabaseID()));

		} catch (SQLException e) {
			e.printStackTrace();
			return false;
		}

		return true;
	}

	/**
	 * Stores the passed {@code Solution} in the DB and sets the database ID of the solution in the solution.
	 * <p>
	 * If the solution is already stored for the level it isn't stored twice and no data are saved.<br>
	 * Use {@link #updateSolution(Solution)} for updating the data of existing solutions.
	 *
	 * @param solution
	 *            the {@code Solution} to store
	 * @param levelID
	 *            ID of the level the solution is saved to
	 */
	public void insertSolution(Solution solution, int levelID) {

		try {
			// Prepared Statement for database access.
			PreparedStatement p;

			// Try to read the current solution from the database. If the solution is already stored return immediately.
			p = prepareStatement("SELECT solutionID	FROM solutions " + "WHERE levelID 	 = " + levelID + "AND   solutionLURD = ?	");

			p.setString(1, solution.lurd);
			if (p.executeQuery().next()) {
				return;
			}

			// Insert the new solution.
			p = prepareStatement("INSERT INTO solutions (solutionName, isOwnSolution,"
					+ "levelID, solutionLURD, pushesCount, movesCount, boxLines, boxChanges,"
					+ "pushingSessions, solutionComment, orderValue, highLightData, lastChanged) "
					+ "values(?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, CURRENT_TIMESTAMP)");

			int parameterIndex = 1;
			p.clearParameters();
			p.setString(parameterIndex++, solution.name);
			p.setBoolean(parameterIndex++, solution.isOwnSolution);
			p.setInt(parameterIndex++, levelID);
			p.setString(parameterIndex++, solution.lurd);
			p.setInt(parameterIndex++, solution.pushesCount);
			p.setInt(parameterIndex++, solution.movesCount);
			p.setInt(parameterIndex++, solution.boxLines);
			p.setInt(parameterIndex++, solution.boxChanges);
			p.setInt(parameterIndex++, solution.pushingSessions);
			// FFS/hm: playerLines
			p.setString(parameterIndex++, solution.comment);
			p.setInt(parameterIndex++, solution.orderValue);
			p.setString(parameterIndex++, solution.highLightData);

			p.executeUpdate();

			// Get the ID of the stored solution.
			ResultSet result = query("call IDENTITY()");
			if (result.next()) {
				solution.databaseID = result.getInt(1);
				solution.setLastChanged(new Date(System.currentTimeMillis()));

				fireDatabaseDataChanged(new DatabaseDataEvent(this, Solution.class, EventAction.INSERT, solution.databaseID));
				fireDatabaseDataChanged(new DatabaseDataEvent(this, Level.class, EventAction.CHANGE, levelID));
			}

		} catch (SQLException e) {
			e.printStackTrace();
		}
	}

	/**
	 * Saves the passed solution in the database by overwriting the already saved values.
	 *
	 * @param solution
	 *            the solution to be saved
	 * @return <code>true</code> if the solution has been updated, and <br>
	 *         <code>false</code> otherwise (error occurred) */
	public boolean updateSolution(Solution solution) {

		// Ensure the solution is already saved.
		if (solution.databaseID == NO_ID) {
			return false;
		}

		try {
			// Prepared Statement for database access.
			PreparedStatement p;

			// Update the data that could have changed.
			p = prepareStatement("UPDATE solutions SET solutionName = ?, " + "isOwnSolution = ?, " + "solutionComment = ?, "
					+ "orderValue = ?, " + "highLightData = ?, " + "lastChanged = CURRENT_TIMESTAMP " + "WHERE solutionID = ?");

			int parameterIndex = 1;
			p.clearParameters();
			p.setString(parameterIndex++, solution.name);
			p.setBoolean(parameterIndex++, solution.isOwnSolution);
			p.setString(parameterIndex++, solution.comment);
			p.setInt(parameterIndex++, solution.orderValue);
			p.setString(parameterIndex++, solution.highLightData);
			p.setInt(parameterIndex++, solution.databaseID);

			p.executeUpdate();

		} catch (SQLException e) {
			e.printStackTrace();
			return false;
		}

		solution.setLastChanged(new Date(System.currentTimeMillis()));

		fireDatabaseDataChanged(new DatabaseDataEvent(this, Solution.class, EventAction.CHANGE, solution.databaseID));

		return true;
	}

	/**
	 * Deletes the {@code Solution} stored under the passed ID from this database.
	 *
	 * @param solutionID
	 *            ID of the solution in this database
	 * @return <code>true</code> the solution has been deleted, <code>false</code> error while deleting
	 */
	public boolean deleteSolution(int solutionID) {
		if(solutionID < 0) {
			return false;
		}

		if(update("DELETE FROM solutions where solutionID = " + solutionID)) {
			fireDatabaseDataChanged(new DatabaseDataEvent(this, Solution.class, EventAction.DELETE, solutionID));
			return true;
		}

		return false;
	}

	/**
	 * Returns the {@code Solution}s stored in the database for the passed level.
	 *
	 * @param level  levelID  the database ID of the level the {@code Snapshot}s are to be returned for
	 * @return the stored {@code Solution}s of the passed level
	 */
	public ArrayList<Solution> getSolutions(int levelID) {

		ArrayList<Solution> solutions = new ArrayList<Solution>();

		if (levelID < 0) {
			return solutions;
		}

		// Add all solutions of the level.
		try {
			// Select all solutions of the level.
			ResultSet result = query("SELECT * FROM solutions " + "WHERE levelID = " + levelID + "ORDER BY orderValue");
			if (result == null) {
				return solutions;
			}

			while (result.next()) {
				Solution solution = new Solution(result.getString("solutionLURD"), result.getInt("solutionID"));
				solution.name = result.getString("solutionName");
				solution.isOwnSolution = result.getBoolean("isOwnSolution");
				solution.movesCount = result.getInt("movesCount");
				solution.pushesCount = result.getInt("pushesCount");
				solution.boxLines = result.getInt("boxLines");
				solution.boxChanges = result.getInt("boxChanges");
				solution.pushingSessions = result.getInt("pushingSessions");
				// FFS/hm: playerLines
				solution.comment = result.getString("solutionComment");
				solution.orderValue = result.getInt("orderValue");
				solution.highLightData = result.getString("highLightData");
				solution.setLastChanged(result.getTimestamp("lastChanged"));

				solutions.add(solution);
			}
		} catch (SQLException e) {
			if (Debug.isDebugModeActivated) {
				e.printStackTrace();
			}
		}

		return solutions;
	}

	/**
	 * Returns the number of stored solutions for the passed level ID.
	 *
	 * @param levelID
	 *            ID of the level
	 * @return number of stored solutions
	 */
	public int getSolutionsCount(int levelID) {

		try {
			ResultSet result = query("SELECT COUNT(*) " + "FROM solutions  " + "WHERE levelID = " + levelID);
			while (result.next()) {
				return result.getInt(1);
			}
		} catch (SQLException e2) {}

		return 0;
	}

	/**
	 * Stores the passed {@code Snapshot} in the DB and sets the database ID in the passed snapshot.
	 * Additionally the {@code lastChanged} time stamp is updated to the current time in the passed snapshot.
	 * <p>
	 * If the same snapshot (comparing lurd-string and isAutoSaved flag) already exists the
	 * snapshot is saved using a new database ID. To update a snapshot the update method must be called.
	 *
	 * @param snahpshot  the {@code Snapshot} to store
	 * @param levelID  ID of the level the snapshot is saved for
	 */
	public void insertSnapshot(Snapshot snapshot, int levelID) {

		if (snapshot == null) {
			return;
		}

		try {
			// Prepared Statement for database access.
			PreparedStatement p;

			// Insert the new solution.
			p = prepareStatement("INSERT INTO snapshots (snapshotLURD, levelID,"
					+ "savegameComment, orderValue, highLightData, isAutoSaved, lastChanged) "
					+ "values(?, ?, ?, ?, ?, ?, CURRENT_TIMESTAMP)");

			int parameterIndex = 1;
			p.clearParameters();
			p.setString( parameterIndex++, snapshot.getLURD());
			p.setInt(	 parameterIndex++, levelID);
			p.setString( parameterIndex++, snapshot.getComment());
			p.setInt(	 parameterIndex++, snapshot.getOrderValue());
			p.setString( parameterIndex++, snapshot.getHighLightData());
			p.setBoolean(parameterIndex++, snapshot.isAutoSaved());

			p.executeUpdate();

			// Get the ID of the stored snapshot.
			ResultSet result = query("call IDENTITY()");
			if (result.next()) {
				snapshot.setDatabaseID(result.getInt(1));
				snapshot.setLastChanged(new Date(System.currentTimeMillis()));

				fireDatabaseDataChanged(new DatabaseDataEvent(this, Snapshot.class, EventAction.INSERT, snapshot.getDatabaseID()));
				fireDatabaseDataChanged(new DatabaseDataEvent(this, Level.class,    EventAction.CHANGE, levelID));
			}

		} catch (SQLException e) {
			e.printStackTrace();
		}
	}

	/**
	 * Saves the passed {@code Snapshot} in the database by overwriting the already saved values.
	 * <p>
	 * The new time stamp is set in the passed {@code Snapshot}.
	 *
	 * @param snapshot the {@code Snapshot} to be saved
	 *
	 */
	public void updateSnapshot(Snapshot snapshot) {

		// Ensure the snapshot is already saved.
		if (snapshot.getDatabaseID() == NO_ID) {
			return;
		}

		try {
			// Prepared Statement for database access.
			PreparedStatement p;

			p = prepareStatement("UPDATE snapshots "
					+ "SET snapshotLURD = ?, "
				//  + " levelID 		= ?, "	// can't be changed by the user
					+ " savegameComment = ?, "
					+ " orderValue 		= ?, "
					+ " highLightData 	= ?, "
					+ " isAutoSaved 	= ?, "
					+ " lastChanged 	= CURRENT_TIMESTAMP "
					+ "WHERE snapshotID = ?");

			int parameterIndex = 1;
			p.clearParameters();
			p.setString( parameterIndex++, snapshot.getLURD());
			p.setString( parameterIndex++, snapshot.getComment());
			p.setInt(	 parameterIndex++, snapshot.getOrderValue());
			p.setString( parameterIndex++, snapshot.getHighLightData());
			p.setBoolean(parameterIndex++, snapshot.isAutoSaved());
			p.setInt(	 parameterIndex++, snapshot.getDatabaseID());

			p.executeUpdate();

		} catch (SQLException e) {
			e.printStackTrace();
			return;
		}

		snapshot.setLastChanged(new Date(System.currentTimeMillis()));

		fireDatabaseDataChanged(new DatabaseDataEvent(this, Snapshot.class, EventAction.CHANGE, snapshot.getDatabaseID()));
	}

	/**
	 * Deletes the {@code Snapshot} stored under the passed ID from this database.
	 *
	 * @param snapshotID  ID of the {@code Snapshot} in this database
	 * @return <code>true</code> the solution has been deleted, <code>false</code> error while deleting
	 */
	public boolean deleteSnapshot(int snapshotID) {
		if(snapshotID < 0) {
			return false;
		}

		boolean isDeletionSuccessful = update("DELETE FROM snapshots where snapshotID = " + snapshotID);
		if(isDeletionSuccessful) {
			fireDatabaseDataChanged(new DatabaseDataEvent(this, Snapshot.class, EventAction.DELETE, snapshotID));
		}

		return isDeletionSuccessful;
	}

	/**
	 * Returns the ID of the passed {@code Author}. The author is selected according to the name.
	 *
	 * @param author  {@code Author} to return the database ID for
	 * @return database ID of the stored author or {@link #NO_ID}
	 * @throws SQLException
	 */
	public int getAuthorID(Author author) {

		PreparedStatement p = prepareStatement("SELECT authorID FROM authorData WHERE name = ?");
		try {
			p.clearParameters();
			p.setString(1, author.getName());
			ResultSet result = p.executeQuery();
			return result.next() ? result.getInt("authorID") : NO_ID;
		} catch (SQLException e) {
			return NO_ID;
		}
	}

	/**
	 * Stores the passed {@code Author} in the database.
	 * <p>
	 * If the passed {@code Author} already contains a database ID this ID is ignored! The {@code Author} is always saved as new one in the
	 * database.
	 *
	 * @param author
	 *            {@code Author} to be saved
	 * @return database ID of the stored author or {@link #NO_ID} if an error occurred while saving
	 */
	public int insertAuthor(Author author) {

		int newAuthorID = NO_ID;
		try {
			PreparedStatement p = prepareStatement("INSERT INTO authorData (name, email, homepage, authorComment, lastChanged) values(?, ?, ?, ?, CURRENT_TIMESTAMP)");
			p.clearParameters();
			p.setString(1, author.getName());
			p.setString(2, author.getEmail());
			p.setString(3, author.getWebsiteURL());
			p.setString(4, author.getComment());
			p.executeUpdate();

			// Get author ID.
			ResultSet result = query("call IDENTITY()");
			result.next();
			newAuthorID = result.getInt(1);

			fireDatabaseDataChanged(new DatabaseDataEvent(this, Author.class, EventAction.INSERT, newAuthorID));

		} catch (SQLException e) {
			if (Debug.isDebugModeActivated) {
				e.printStackTrace();
			}
		}

		return newAuthorID;
	}

	/**
	 * Updates the following data of an author:<br>
	 * <ul>
	 *   <li> name      </li>
	 *   <li> email     </li>
	 *   <li> homepage  </li>
	 *   <li> comment   </li>
	 *   <li> timestamp </li>
	 * </ul>
	 *
	 * @param authorID  the ID of the author to be changed
	 * @param newName  the new name to be set
	 * @param newEMail the new e-mail to be set
	 * @param newHomepage  the new website URL to be set
	 * @param newComment  the new comment to be set
	 * @param newTimestamp  the new timestamp to be set
	 */
	public void updateAuthor(int authorID, String newName, String newEMail, String newHomepage, String newComment, Timestamp newTimestamp) {

		// Update the data on the database.
		String statement = "UPDATE authorData SET name = ?, email = ?, homepage = ?, authorComment = ?, lastChanged = ? WHERE authorID = ?";
		PreparedStatement p = prepareStatement(statement);
		int parameterIndex = 1;
		try {
			p.setString(parameterIndex++, newName);
			p.setString(parameterIndex++, newEMail);
			p.setString(parameterIndex++, newHomepage);
			p.setString(parameterIndex++, newHomepage);
			p.setTimestamp(parameterIndex++, newTimestamp);
			p.setInt(parameterIndex++, authorID);

			p.executeUpdate();

			fireDatabaseDataChanged(new DatabaseDataEvent(this, Author.class, EventAction.CHANGE, authorID));

		}catch(SQLException e) {e.printStackTrace();}
	}

	/**
	 * Deletes the author stored under passed ID from this database.
	 *
	 * @param author
	 *            the ID of the author to be deleted
	 * @return <code>true</code> author has been deleted, <code>false</code> an error has occurred
	 */
	public boolean deleteAuthor(int authorID) {
		boolean isUpdated = false;
		if (authorID != NO_ID) {
			isUpdated = update("DELETE FROM authorData WHERE authorID = " + authorID);
			fireDatabaseDataChanged(new DatabaseDataEvent(this, Author.class, EventAction.DELETE, authorID));
		}

		return isUpdated;
	}

	/**
	 * Returns all {@code Author}s stored in this database.<br>
	 * The authors returned are ordered ascending by their names.
	 *
	 * @return {@code List} of all {@code Author}s
	 */
	public List<Author> getAllAuthors() {
		ArrayList<Author> authors = new ArrayList<Author>();

		try {
			ResultSet result = query("SELECT * FROM authorData ORDER BY name");
			while (result.next()) {
				authors.add(getAuthorFromResult(result));
			}
		} catch (SQLException e) {
			e.printStackTrace();
		}

		return authors;
	}

	/**
	 * Executes a query on the already existing DB connection.
	 *
	 * @param expression
	 *            the query to be performed
	 * @return The result of the query
	 * @throws SQLException
	 */
	public ResultSet query(String expression) throws SQLException {

		ResultSet rs = null;
		Statement st = null;
		try {
			st = conn.createStatement();

			// Run the query
			rs = st.executeQuery(expression);
		} finally {
			// Close the Statement object, it is no longer used
			try {
				if (st != null) {
					st.close();
				}
			} catch (SQLException e) {
				e.printStackTrace();
			}
		}

		return rs;
	}

	/**
	 * Method used for the SQL commands CREATE, DROP, INSERT and UPDATE
	 *
	 * @param expression
	 *            the SQL command to be executed
	 * @return <code>true</code> no error occurred, <code>false</code> error while update occurred
	 */
	public boolean update(String expression) {

		try {
			Statement st = conn.createStatement();

			// Run the query.
			if (st.executeUpdate(expression) == -1) {
				System.out.println("db error: " + expression);
			}

			// Close the Statement object.
			st.close();
			return true;

		} catch (Exception e) {
			e.printStackTrace();
			return false;
		}
	}

	/** @param rs */
	// private final void dump(ResultSet rs) {
	//
	// try {
	// ResultSetMetaData meta = rs.getMetaData();
	// int colmax = meta.getColumnCount();
	// int i;
	// Object o = null;
	//
	// for (; rs.next(); ) {
	// for (i = 1; i <= colmax; ++i) {
	// o = rs.getObject(i);
	//
	// // with 1 not 0
	// if(o != null)
	// System.out.print(o.toString() + " \n");
	// else
	// System.out.println("null");
	// }
	//
	// System.out.println(" ");
	// }
	// }
	// catch (SQLException e) {
	// e.printStackTrace();
	// }
	// }

	/**
	 * Returns a prepared statement created with the passed expression.
	 *
	 * @param expression
	 *            the expression the prepared statement is created with
	 * @return the created prepared statement
	 */
	public PreparedStatement prepareStatement(String expression) {

		try {
			return conn.prepareStatement(expression);
		} catch (SQLException e) {
			e.printStackTrace();
		}

		return null;
	}

	/** Shutdown the database. */
	public void shutdown() {

		try {
			if (conn != null) {
				Statement st = conn.createStatement();

				// Shut down the data base connection.
				st.execute("SHUTDOWN");

				// Finally, close the connection
				conn.close();
				st.close();
			}
		} catch (SQLException e) {
			e.printStackTrace();
		}
	}

	/**
	 * Returns whether the database is blocked by another process.
	 *
	 * @return <code>true</code> if the database is blocked by another process, and<br>
	 *         <code>false</code> if the database is not blocked by another process
	 */
	public static boolean isBlockedByAnotherProcess() {
		return isBlockedByAnotherProcess;
	}


	/**
	 * Adds an <code>DatabaseEventListener</code> to this GUI.
	 * <p>
	 * Events are fired for:
	 * <ul>
	 *   <li>Levels</li>
	 *   <li>Level collections</li>
	 *   <li>Authors</li>
	 *   <li>Solutions</li>
	 *   <li>Snapshots</li>
	 * </ul>
	 * The listener can only be sure that an event is fired for the entity that is processed in a method.
	 * For instance:
	 * if a solution is deleted only an event for the solution is fired, but not necessarily an event
	 * for the level the solution has been deleted for.
	 *
	 * @param l the <code>DatabaseEventListener</code> to be added
	 *
	 * @see #EventAction
	 */
	public void addDatabaseEventListener(DatabaseEventListener l) {
		listenerList.add(DatabaseEventListener.class, l);
	}

	/**
	 * Removes an <code>DatabaseEventListener</code> from this GUI.
	 *
	 * @param l the listener to be removed
	 */
	public void removeDatabaseEventListener(DatabaseEventListener l) {
		listenerList.remove(DatabaseEventListener.class, l);
	}

	/**
	 * Notifies all listeners that have registered interest for
	 * notification on this event type.
	 *
	 * @param event  the <code>DatabaseDataEvent</code> object
	 */
	protected void fireDatabaseDataChanged(DatabaseDataEvent event) {
		// Guaranteed to return a non-null array
		Object[] listeners = listenerList.getListenerList();
		// Process the listeners last to first, notifying
		// those that are interested in this event
		for (int i = listeners.length-2; i>=0; i-=2) {
			if (listeners[i]==DatabaseEventListener.class) {
				((DatabaseEventListener)listeners[i+1]).databaseEventFired(event);
			}
		}
	}

	@Override
	public void databaseEventFired(DatabaseDataEvent event) {

		// Inform all listeners.
		fireDatabaseDataChanged(event);
	}
}
