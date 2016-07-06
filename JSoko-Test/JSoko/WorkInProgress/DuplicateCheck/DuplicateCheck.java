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
package de.sokoban_online.jsoko.workInProgress.duplicateCheck;

import java.awt.Dimension;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashSet;
import java.util.List;
import java.util.ListIterator;
import java.util.Set;

/**
 * This class contains methods for normalizing a level to avoid duplicates
 * in the database.
 *
 * @author Paweł Kłeczek
 */
public final class DuplicateCheck {

	/**
	 * Enumeration of all possible level's transformations.
	 * Prefix "flipped" means that after being rotated the level is also
	 * flipped horizontally.
	 */
	private enum Transformations {
        ROTATED_0, ROTATED_90, ROTATED_180, ROTATED_270,
		FLIPPED_ROTATED_0,   FLIPPED_ROTATED_90,
		FLIPPED_ROTATED_180, FLIPPED_ROTATED_270
	};

	/** Copy of the level which should be normalized. */
	private static List<String> inputLevel;

	/** Dimensions of the level which should be normalized. */
	private static Dimension levelDimensions = new Dimension();


	/**
	 * Computes dimensions of the input level.
	 */
	private static void computeLevelDimensions() {
        for (ListIterator<String> it = inputLevel.listIterator(); it.hasNext(); ) {
            levelDimensions.width = Math.max(levelDimensions.width, it.next().length());
            levelDimensions.height++;
        }
	}


	/**
	 * Returns the desired row of the normalized level.
	 *
	 * @param rowNo          row index in the normalized level
	 * @param transformation transformation which should be applied
	 * @return normalized row
	 */
	private static String getNormalizedRow(int rowNo, Transformations transformation) {
		// Obtain desired row of the transformed level.
		String currentLine = "";
		switch (transformation) {
			case ROTATED_0:
			case FLIPPED_ROTATED_0:
				currentLine = inputLevel.get(rowNo);
				break;
			case ROTATED_90:
			case FLIPPED_ROTATED_90:
				for (int i = 0; i < levelDimensions.height; i++) {
					currentLine += inputLevel.get(i).charAt(levelDimensions.width-rowNo-1);
				}
				break;
			case ROTATED_180:
			case FLIPPED_ROTATED_180:
				currentLine = new StringBuilder(inputLevel.get(levelDimensions.height-rowNo-1)).reverse().toString();
				break;
			case ROTATED_270:
			case FLIPPED_ROTATED_270:
				for (int i = levelDimensions.height-1; i >= 0; i--) {
					currentLine += inputLevel.get(i).charAt(rowNo);
				}
				break;
		}

		/* Flip the line horizontally. */
		if(        transformation == Transformations.FLIPPED_ROTATED_0
				|| transformation == Transformations.FLIPPED_ROTATED_90
				|| transformation == Transformations.FLIPPED_ROTATED_180
				|| transformation == Transformations.FLIPPED_ROTATED_270 ) {
			currentLine = new StringBuilder(currentLine).reverse().toString();
		}

		return currentLine;
	}


	/**
	 * Converts the given level into a normalized level.
	 *
	 * @param level level to be normalized
	 * @return normalized level
	 */
    public static ArrayList<String> normalize(List<String> level) {

        /* Computed normalized level. */
        ArrayList<String> normalizedLevel = new ArrayList<String>();

        /* "Lowest" line possibility (dynamically updated). */
        String lowestLine = null;

        /* Line obtained using current transformation. */
        String currentLine = null;

        /*
         * Set of transformations which (so far) resulted in obtaining the
         * normalized level. At the very beginning all transformations are
		 * regarded as "best".
         */
        Set<Transformations> bestSolutions =
        		new HashSet<Transformations>(Arrays.asList(Transformations.values()));

        /*
         * New set of "best" transformations (constructed from scratch in each
         * loop's iteration.
         */
        Set<Transformations> newBestSolutions = new HashSet<Transformations>();

		inputLevel = level;
		computeLevelDimensions();

        for (int rowNo = 0; rowNo < Math.max(levelDimensions.width, levelDimensions.height); rowNo++) {

            // Compare results of each of already-chosen "best" transformations
			// applied to the next line of *normalized* (!) level to narrow the
			// number of solutions.
            for (Transformations transformation : bestSolutions) {

				// Skip row numbers which do not exist in the level normalized
				// using current transformation.
                if (       transformation == Transformations.ROTATED_0
						|| transformation == Transformations.FLIPPED_ROTATED_0
                        || transformation == Transformations.ROTATED_180
						|| transformation == Transformations.FLIPPED_ROTATED_180) {

                    /*
                     * In this transformations the number of rows in ultimate
                     * level is equal to the number of rows in the original level.
                     */
                    if (rowNo >= levelDimensions.height)
                        continue;
                } else {

                    /*
                     * In other transformations the number of rows in ultimate
                     * level is equal to the number of columns in the original level.
                     */
                    if (rowNo >= levelDimensions.width)
                        continue;
                }

				currentLine = getNormalizedRow(rowNo, transformation);

                /*
                 * If currently processed transformation results in "lowest"
                 * solution substitute lowestLine and bestSolutions with new
				 * values.
                 * If currently processed transformation is as low as the one
                 * which is currently "lowest" then only mark that it is
                 * another candidate for "best" transformation.
                 */
                if (currentLine.compareTo(lowestLine) < 0) {
                    lowestLine = currentLine;
                    newBestSolutions.clear();
                    newBestSolutions.add(transformation);
                } 
                else if (currentLine.compareTo(lowestLine) == 0) {
                    newBestSolutions.add(transformation);
                }
            }

            // Retain only "best" solutions.
            bestSolutions.retainAll(newBestSolutions);

            normalizedLevel.add(lowestLine);
        }

        return normalizedLevel;
    }
}