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

import java.awt.datatransfer.DataFlavor;
import java.awt.datatransfer.Transferable;
import java.awt.datatransfer.UnsupportedFlavorException;
import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.FileNotFoundException;
import java.io.FileWriter;
import java.io.IOException;
import java.io.PrintWriter;
import java.sql.SQLException;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

import javax.swing.SwingUtilities;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.gui.MessageDialogs;
import de.sokoban_online.jsoko.gui.Transformation;
import de.sokoban_online.jsoko.leveldata.solutions.Solution;
import de.sokoban_online.jsoko.leveldata.solutions.SolutionsManager;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Utilities;



/**
 * Manages all level data.
 * This includes reading and writing levels and level collections from/to
 * files, the clipboard and the data base.
 */
public final class LevelsIO {

	private final DataParser dataParser;

	// Names of the additional collections (collections outside the main jar-file)
	//	private ArrayList<String> additionalCollectionNames;

	/** Database for storing the level data. Public for easier access. */
	public Database database;

	/**
	 * Creates a <code>Levels</code> object for storing data from one level.
	 *
	 * @param application the reference to the main object holding all references
	 */
	public LevelsIO(JSoko application) {

		dataParser = new DataParser(application);

		try {
			database = Database.connectToJSokoDB();
		} catch (final SQLException e) {
			database = null;
			SwingUtilities.invokeLater(new Runnable() {
				@Override
				public void run() {
					MessageDialogs.showFailureString(null, e.getLocalizedMessage());
				}
			});
		}
	}

	/**
	 * Loads the level collection stored in the passed file.
	 *
	 * @param collectionFilePath path and name of the collection to load
	 *
	 * @return the <code>LevelCollection</code> created from the read in data
	 * @throws IOException the collection file couldn't be read
	 */
	public final LevelCollection getLevelCollectionFromFile(String collectionFilePath) throws IOException {

		// ArrayList, for storing the read data.
		List<String> inputData = new ArrayList<String>(1000);

		// Create BufferedReader for the input file.
		BufferedReader levelFile = Utilities.getBufferedReader(collectionFilePath);

		// The file hasn't been found => return null.
		if(levelFile == null) {
			throw new FileNotFoundException(Texts.getText("message.fileMissing", collectionFilePath));
		}

		// Read in line by line of the input data.
		String levelDataRow;
		try {
			while ((levelDataRow = levelFile.readLine()) != null) {
				inputData.add(levelDataRow);
			}
		} finally {
			levelFile.close();
		}

		// Parse the read data and return the collection created from that data.
		// The level collection to be returned.
		LevelCollection levelCollection = dataParser.extractData(inputData, collectionFilePath);

		// Return the collection.
		return levelCollection;

	}

	//	/**
	//	 * Reads in an additional collection.
	//	 *
	//	 * @param levelCollectionName	Name of the collection to be read.
	//	 */
	//	final private LevelCollection loadAdditionalLevelCollection(String levelCollectionName) throws NoDataFoundException {
	//
	//		// Array for storing the whole leveldata.
	//		byte[] leveldataBuffer = null;
	//
	//		// Holding one row of a level
	//		StringBuilder levelRow = new StringBuilder();
	//
	//		// ArrayList holding all rows of all levels
	//		ArrayList<String> leveldataAsArrayList = new ArrayList<String>();
	//
	//		// Number of read bytes
	//		int numberOfReadBytes = 0;
	//
	//		// The number before the "!" identifies the file of the collection to be loaded.
	//		int indexOfExclamationPoint = levelCollectionName.indexOf("!");
	//
	//		// Name of the entry holding the relevant level data
	//		String entryName = levelCollectionName.substring(indexOfExclamationPoint+1);
	//
	//		// Open the file with the additional collections.
	//		InputStream additionalCollectionsFile = null;
	//		try {
	//			additionalCollectionsFile = getClass().getResourceAsStream("/"+Settings.get("additionalCollectionsFile")+levelCollectionName.substring(0, indexOfExclamationPoint)+".zip");
	//		} catch(Exception e) {}
	//
	//		// Immediately return if there aren't any additional collections available.
	//		if(additionalCollectionsFile == null)
	//			return;
	//
	//		// Open the file as zip.
	//		ZipInputStream zip_additionalCollectionsFile = new ZipInputStream(additionalCollectionsFile);
	//
	//		try {
	//			ZipEntry entry;
	//
	//			// Search the entry with the correct name and read the leveldata.
	//			while((entry = zip_additionalCollectionsFile.getNextEntry()) != null && entryName.equals(entry.getName()) == false);
	//			if(entry != null && entry.getSize() > 0) {
	//				leveldataBuffer = new byte[500];
	//				while((numberOfReadBytes = zip_additionalCollectionsFile.read(leveldataBuffer)) > 0) {
	//
	//					for(int i=0; i<numberOfReadBytes; i++) {
	//						switch(leveldataBuffer[i]) {
	//							case '\r':
	//								continue;
	//							case '\n':
	//								leveldataAsArrayList.add(levelRow.toString());
	//								levelRow = new StringBuilder();
	//								continue;
	//							default:
	//								levelRow.append((char)leveldataBuffer[i]);
	//						}
	//					}
	//				}
	//				// The last level row needn't to end with "\n", so it is added in every case.
	//				leveldataAsArrayList.add(levelRow.toString());
	//			}
	//			additionalCollectionsFile.close();
	//		} catch(IOException e) {}
	//
	//		// Extract all level data.
	//		if(leveldataBuffer != null)
	//			extractData(leveldataAsArrayList, levelCollectionName);
	//	}

	/**
	 * Imports data from the passed Transferable-object and tries to extract level data from it.
	 * <p>
	 * This method is only used by the application.
	 *
	 * @param transferable the transferable object tried to extract level data from
	 *
	 * @return the <code>LevelCollection</code> created from the read in data
	 */
	final public LevelCollection getLevelCollectionFromStringDataFlavor(Transferable transferable) {

		// Level data from the clipboard.
		List<String> levelData = new ArrayList<String>();

		// Check if the stringFlavor is supported. If not, return an empty level collection.
		if (transferable == null || transferable.isDataFlavorSupported(DataFlavor.stringFlavor) == false) {
			return new LevelCollection.Builder().build();
		}

		try {
			String transferString = ((String) transferable.getTransferData(DataFlavor.stringFlavor));
			transferString = transferString.replaceAll("\\r\\n|\\r", "\n"); // Ensure there is only \n
			String[] levelDataString = transferString.split("\n");

			// The method "extractLevelData" needs the data to be in List (or any subtype).
			levelData.addAll(Arrays.asList(levelDataString));
		} catch (UnsupportedFlavorException ex) {
			ex.printStackTrace();
		} catch (IOException ex) {
			ex.printStackTrace();
		}

//		if(Settings.isDebugModeActivated) {
//			try {
//				levelDataAsArrayList = FormatConverter.getXSBList(levelDataAsArrayList);
//			} catch (ParseException e) {
//				e.printStackTrace();
//			}
//		}

		// Extract the level data from the clipboard data and return the LevelCollection created from that data.
		return dataParser.extractData(levelData, null);
	}

	//	/**
	//	 * This method reads in the names of all additional level files that are available.
	//	 */
	//	public final void readInAvailableCollectionNames() {
	//
	//		InputStream additionalCollectionsFile = null;
	//
	//		// Read in all additional collection files.
	//		for(int collectionNo = 1; ; collectionNo++) {
	//
	//			try {
	//				additionalCollectionsFile = getClass().getResourceAsStream("/"+Settings.get("additionalCollectionsFile")+collectionNo+".zip");
	//			} catch(Exception e) {
	//				additionalCollectionsFile = null;
	//			}
	//
	//			// Immediately return if there aren't any additional collections more available.
	//			if(additionalCollectionsFile == null)
	//				return;
	//
	//			// Open the file as zip and read all filenames.
	//			ZipInputStream zip_additionalCollectionsFile = new ZipInputStream(additionalCollectionsFile);
	//
	//			try {
	//				ZipEntry entry;
	//
	//				while((entry = zip_additionalCollectionsFile.getNextEntry()) != null) {
	//					if(entry.isDirectory() == false)
	//						additionalCollectionNames.add(collectionNo+"!"+entry.getName());
	//				}
	//
	//				additionalCollectionsFile.close();
	//			} catch(IOException e) {}
	//			catch(IllegalArgumentException e) {}  // Java Bug: The files mustn't contain umlauts :-(
	//		}
	//	}
	//
	//
	//	/**
	//	 * Returns an <code>ArrayList</code> of all available collection names.
	//	 *
	//	 * @return the <code>ArrayList</code> containing all additional collection names
	//	 */
	//	public final ArrayList<String> getAdditionalCollectionNames() {
	//		return additionalCollectionNames;
	//	}


	/**
	 * Saves the passed level using the passed file name.
	 *
	 * @param level the <code>Level</code> to save
	 * @param fileName the file the level is to be saved to
	 * @throws IOException thrown when the level couldn't be saved
	 */
	final public void saveLevel(Level level, String fileName) throws IOException {

		// Create a PrintWriter for writing the data to hard disk.
		PrintWriter levelFile = new PrintWriter(fileName);

		// Write the level data to the file.
		writeLevelToFile(level, levelFile);

		// Check the error status.
		boolean isFileSavingFailed = levelFile.checkError();

		// Close the file.
		levelFile.close();

		// Throw exception in the case of an error.
		if (isFileSavingFailed) {
			throw new IOException(Texts.getText("errorBySaving"));
		}
	}

	/**
	 * Saves the passed collection to the passed file.
	 *
	 * @param levelCollection the level collection to be saved
	 * @param fileName the file the level is to be saved to
	 * @throws IOException thrown when the level couldn't be saved
	 */
	final public void saveCollection(LevelCollection levelCollection, String fileName) throws IOException {

		// Create a PrintWriter for writing the data to hard disk.
		PrintWriter collectionFile = new PrintWriter(new BufferedWriter(new FileWriter(fileName)));

		// Write the collection data
		if (!levelCollection.getTitle().isEmpty()) {
			collectionFile.println("Title: " + levelCollection.getTitle());
		}

		Author collectionAuthor = levelCollection.getAuthor();
		if ( ! collectionAuthor.getName().equals(Texts.getText("unknown")) ) {
			collectionFile.println("Author: " + collectionAuthor.getName());
		}
		if (!collectionAuthor.getEmail().isEmpty()) {
			collectionFile.println("Email: " + collectionAuthor.getEmail());
		}
		if (!collectionAuthor.getWebsiteURL().isEmpty()) {
			collectionFile.println("Homepage: " + collectionAuthor.getWebsiteURL());
		}
		if (!collectionAuthor.getComment().isEmpty()) {
			collectionFile.println("Author comment: " + collectionAuthor.getComment());
		}
		if (!levelCollection.getComment().isEmpty()) {
			collectionFile.println(levelCollection.getComment());
		}

		// Loop over all levels of the collection and write their data to the file.
		// If the author of the level is identical to the collection author
		// then don't write the author data for such levels.
		for (Level level : levelCollection) {
			Author levelAuthor = level.getAuthor();
			if(levelAuthor.equals(collectionAuthor)) {
				level.setAuthor(new Author());
			}
			writeLevelToFile(level, collectionFile);
			level.setAuthor(levelAuthor);
		}

		// Check the error status.
		boolean isFileSavingFailed = collectionFile.checkError();

		// Close the file.
		collectionFile.close();

		// Throw exception in the case of an error.
		if (isFileSavingFailed) {
			throw new IOException(Texts.getText("errorBySaving"));
		}
	}

	/**
	 * Writes the passed level data into the passed file.
	 *
	 * @param level the level to be saved
	 * @param PrintWriter the file to write to
	 */
	final private void writeLevelToFile(Level level, PrintWriter file) {

		// Stores the board data of the level.
		List<String> boardData;

		file.println();
		file.println();
		file.println(level.getTitle());
		file.println();

		// Get the board data of the level.
		boardData = level.getBoardData();

		// Write the board to the file.
		for (String boardRow : boardData) {
			file.println(boardRow);
		}

		// Empty line between board and transformation data.
		file.println();

		// Save the transformation.
		if (!Transformation.getTransformationAsString().isEmpty()) {

			// Write the transformation string.
			file.write(Transformation.getTransformationAsString());

			// Write empty lines.
			file.println();
			file.println();
		}

		// Write the additional level data.
		if (level.getComment().length() > 0) {
			file.println(level.getComment());
		}

		Author author = level.getAuthor();
		if ( ! author.getName().equals(Texts.getText("unknown")) ) {
			file.println("Author: " + author.getName());
		}
		if (author.getEmail().length() > 0) {
			file.println("Email: " + author.getEmail());
		}
		if (author.getWebsiteURL().length() > 0) {
			file.println("Homepage: " + author.getWebsiteURL());
		}
		if (author.getComment().length() > 0) {
			file.println("Author comment: " + author.getComment());
		}
		if (level.getDifficulty().length() > 0) {
			file.println("Difficulty: " + level.getDifficulty());
		}

		// Save the solution information.
		SolutionsManager solutions = level.getSolutionsManager();

		for (int solutionNo = 0; solutionNo < solutions.getSolutionCount(); solutionNo++) {
			Solution solution = solutions.getSolution(solutionNo);

			file.println();
			file.println("Solution " + solution.movesCount + "/" + solution.pushesCount);
			// FFS/hm: also write minor metrics?
			file.println(solution.lurd);
			if (solution.name.length() > 0) {
				file.println("Solution name: " + solution.name);
			}
			if (solution.isOwnSolution) {
				file.println("Own solution: yes");
			}
			if (solution.comment.length() > 0) {
				file.println("Solution comment: " + solution.comment);
				file.println("Solution comment end:");
			}
		}

		// Get the LURD-representation of the history.
		String historyLURD = level.getHistory().getHistoryAsSaveGame();

		// Save the history string if there is any
		if (!historyLURD.isEmpty()) {
			file.println();
			file.println("Savegame:");
			file.println(historyLURD);
		}
	}
}