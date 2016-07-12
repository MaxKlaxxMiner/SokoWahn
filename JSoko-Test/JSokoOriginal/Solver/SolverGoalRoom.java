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
package de.sokoban_online.jsoko.solver;

import java.util.ArrayList;
import java.util.LinkedList;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.boardpositions.AbsoluteBoardPosition;
import de.sokoban_online.jsoko.boardpositions.AbsoluteBoardPositionMoves;
import de.sokoban_online.jsoko.boardpositions.BoardPosition;
import de.sokoban_online.jsoko.boardpositions.IBoardPosition;
import de.sokoban_online.jsoko.boardpositions.RelativeBoardPositionGoalAreaSolver;
import de.sokoban_online.jsoko.pushesLowerBoundCalculation.LowerBoundCalculation;
import de.sokoban_online.jsoko.resourceHandling.Settings.SearchDirection;



/**
 * This class solves a goal room, by pulling all boxes to the goal room entrance.
 * The solution is returned as solution path array.
 */
public final class SolverGoalRoom extends SolverAStar {

	// The goal room to solve as marker array
    private boolean[] goalRoom;

    // The solver this object has been created in.
    Solver parentSolver = null;


    /**
     * Creates a goal room solver object for solving a goal room.
     *
     * @param application reference to the main object holding all references
     * @param parentSolverObject reference to the parent solver. This reference is needed
     * 							 for checking the "is solver stopped" status.
     * @param solverGUI reference to the GUI of this solver
     */
    public SolverGoalRoom(JSoko application, Solver parentSolverObject, SolverGUI solverGUI) {
        super(application, solverGUI);
        parentSolver = parentSolverObject;
    }


    /**
     * Tries to solve the configuration from the current board.
     * As soon as a box is pulled to a corral forcer, and the player is outside the corral,
     * the box is taken off the board.
	 *
	 * @param corralForcerPosition the position of the corral forcer
	 * @param aGoalRoom boolean area representing the goal room
	 * @return <code>null</code>, or the solution path for the goal room
	 */
	public final int[] searchSolution(int corralForcerPosition, boolean[] aGoalRoom) {

		// Indicates whether a solution has been found
		boolean isSolutionFound = false;

		// Here we remember the start configuration
		//AbsoluteBoardPositionMoves startBoardPosition;

		// Current board position during the search.
		IBoardPosition currentBoardPosition;

		// Pushes lower bound of a configuration
		int lowerBound = 0;

		// Remember the job data in this instance
		goalRoomEntrancePosition = corralForcerPosition;
		this.goalRoom 			 = aGoalRoom;

		// Dies ist eine ArrayList, die die Stellungen in verketteten Listen aufnimmt.
		// An Stelle 0 ist eine Liste mit allen Stellungen, die eine Lösungspfadlänge von 0 haben,
		// an Stelle 1 alle Stellungen mit eine Lösungspfadlänge von 1, ...
		// Standardmäßig wird davon ausgegangen, dass der Lösungspfad höchstens 20 Pushes lang ist.
		// (Ist er länger, wird die Liste automatisch verlängert -> siehe "storeStellung)
		reachedBoardPositions = new ArrayList<LinkedList<IBoardPosition>>(20);
		for(int linkedListNo = 0; linkedListNo < 20; linkedListNo++) {
			reachedBoardPositions.add(new LinkedList<IBoardPosition>());
		}

		// Copy the goals for backwards search from the current box positions.
		board.setGoalsBackwardsSearch();

		// Here we remember the start configuration.
		AbsoluteBoardPositionMoves startBoardPosition = new AbsoluteBoardPositionMoves(board);

		/*
		 * Generate and store the start configurations for the backwards search
		 */

		// Take off all boxes
		for(int boxNo = 0; boxNo < board.goalsCount; boxNo++) {
			board.removeBox(board.boxData.getBoxPosition(boxNo));
		}

		// Calculate the player reachable squares.  Since all boxes are gone,
		// this are all reachable squares.  We use the object of this class to store this,
		// since the global object in the board will be recomputed for new configuration
		// instances.
		playersReachableSquares.update();

		// Put boxes on the goal squares (for backwards start configuration)
		for(int position=board.firstRelevantSquare; position<board.lastRelevantSquare; position++) {
			// If it is a goal, we set the box with the number of the goal on it
			if(board.isGoal(position)) {
				board.boxData.setBoxPosition(board.getGoalNo(position), position);
				board.setBoxWithNo(board.getGoalNo(position), position);
			}
		}

		// For the backwards search we do not yet have any frozen box detection.
		board.boxData.setAllBoxesNotFrozen();

		// The minimal solution path length gets initialized to INFINTE.
		// Minimization over start configurations will assign a real value.
		minimumSolutionPathLength = Integer.MAX_VALUE;

		/*
		 * Mark all configurations as immediately reachable by backwards search,
		 * in which all boxes are the goal squares.  For this we have to set
		 * the player into each closed area.
		 */

		// Store all configurations with boxes on goals, and the player in each closed area.
		// Player positions must avoid walls and boxes (isAccessible), and must be inside
		// of the active part of the board (isSquareReachable).
		for(int position=board.firstRelevantSquare; position<board.lastRelevantSquare;position++) {
			if(board.isAccessible(position) && playersReachableSquares.isSquareReachable(position)) {

				// Den abgeschlossenen Bereich demarkieren (Klasseneigenes Objekt, da bei der
				// Instanziierung der AbsolutenStellung das globale Feld in "board"
				// neu berechnet und damit die alten erreichbaren Felder überschrieben würden!
				playersReachableSquares.reduce(position);

				// Für die Instanziierung des Stellungobjekts ist es wichtig, dass die
				// Spielerposition korrekt gesetzt ist:
				board.playerPosition = position;

				// Store the current configuration as reached by the backwards search
				currentBoardPosition = new AbsoluteBoardPosition(board);
				currentBoardPosition.setSearchDirection(SearchDirection.BACKWARD_GOAL_ROOM);
				positionStorage.storeBoardPosition(currentBoardPosition);

				// Calculate lower bound of this configuration
	    		lowerBound = lowerBoundCalcuation.calculatePushesLowerBoundBackwardsSearch();

	    		// collect minimum over these lower bound values
	    		if( minimumSolutionPathLength > lowerBound ) {
	    			minimumSolutionPathLength = lowerBound;			// take minimum
	    		}

				// Falls eine Kiste schon in der Anfangsstellung auf dem Corralerzwinger steht und der Spieler
				// außerhalb des Zielfeldraumes steht, so kann die Kiste sofort aus dem Raum gezogen werden.
				// Sie kann deshalb sofort vom Feld genommen werden (deaktiv gespeichert werden).
				if(board.isBox(goalRoomEntrancePosition) && aGoalRoom[board.playerPosition] == false) {
					((AbsoluteBoardPosition) currentBoardPosition).setBoxInactive(board.getBoxNo(goalRoomEntrancePosition));
				}

				// Store the current configuration as start configuration for the solver
	        	storeBoardPosition(currentBoardPosition, lowerBound);
			}
		}

		// Falls bereits die Anfangsstellung unlösbar ist kann gleich "unlösbar" ausgegeben
		// und die Suche beendet werden.
		if(minimumSolutionPathLength == LowerBoundCalculation.DEADLOCK) {

		    // Restore the board position present as the solver has been started.
		    board.setBoardPosition(startBoardPosition);

			return null;
		}

		// For the solution search we take all boxes off the board.
		// The solution search will set its boxes itself.
		for(int boxNo = 0; boxNo < board.goalsCount; boxNo++) {
			board.removeBox(board.boxData.getBoxPosition(boxNo));
		}

		// SEARCH for a solution
	    isSolutionFound = backwardSearch();			// <<<-----

	    // Die Anfangsstellung wieder auf dem Spielfeld setzen und ausgeben
		for(int position=board.firstRelevantSquare; position<board.lastRelevantSquare; position++) {
			board.removeBox(position);
		}

		// Alle Kisten aktiv setzen (während der Suche wurden einige Kisten deaktiv gesetzt!)
	    for(int boxNo=0; boxNo<board.boxCount; boxNo++) {
	    	board.boxData.setBoxActive(boxNo);
	    }

	    // Restore the board position present as the solver has been started.
	    board.setBoardPosition(startBoardPosition);

		// Falls keine Lösung gefunden wurde, die Stellungen im Speicher löschen und zurück springen.
		if(isSolutionFound == false){
			positionStorage.clear();
			return null;
		}

		// In dieser ArrayList werden alle Pushes der Lösung abgelegt, um die Lösung Zug für Zug
		// durchgehen zu können
		ArrayList<IBoardPosition> pushes = new ArrayList<IBoardPosition>(solutionBoardPosition.getPushesCount());

		// Es werden alle Stellungen rückwärts abgegangen, die bei der Suche erreicht wurden
	    for(currentBoardPosition = solutionBoardPosition; currentBoardPosition.getPrecedingBoardPosition() != null; currentBoardPosition = currentBoardPosition.getPrecedingBoardPosition()) {
	    	pushes.add(currentBoardPosition);
	    }

	    // Create array into which we store the solution path
	    int[] solutionPath = new int[2 * pushes.size()];

		for(int index=0; index<pushes.size(); index++) {

		    currentBoardPosition = pushes.get(index);

		    // The box pushes are stored turned around, since it was a backwards search.
			int pushedBoxNo 	   = currentBoardPosition.getBoxNo();
			int boxStartPosition   = ((RelativeBoardPositionGoalAreaSolver) currentBoardPosition).getRealPositions()[pushedBoxNo];
			int direction 	       = currentBoardPosition.getDirection();
			int boxTargetPosition  = boxStartPosition - offset[direction] ;

			solutionPath[2*index  ] = boxStartPosition;
			solutionPath[2*index+1] = boxTargetPosition;
        }

		// Clear the data from the hash table to free the memory
		positionStorage.clear();

		return solutionPath;
	}


	/**
	 * Generates all possible valid configurations by pulling (!) boxes.
	 * All generated configurations are stored in the hash table. An already reached
	 * configuration is investigated any further.
	 * In the end we return whether a solution has been found.
	 *
	 * @return <code>true</code> if a  solution is found,<br>
	 *        <code>false</code> if no solution is found
	 */
	 final private boolean backwardSearch() {

		// Nimmt eine Kistenposition auf
		int boxPosition;

		// Nimmt die mögliche neue Kistenposition auf
		int newBoxPosition = 0;

		// Stellungobjekt, dass die aktuelle Stellung aufnimmt und Variable für die
		// zu untersuchende Stellung.
		BoardPosition currentBoardPosition;
		BoardPosition boardPositionToBeAnalyzed;

		// Nimmt den Status einer Stellung auf
		IBoardPosition oldBoardPosition;

		// Nimmt den aktuellen Lowerbound einer Stellung auf
		int pushesLowerBoundCurrentBoardPosition = 0;

        // Positions of a specific board position (box positions and player position)
        int[] positions;

        // box number for looping over all boxes
        int boxNo2 = 0;


		// Die Stellung mit der geringsten geschätzten Lösungspfadlänge weiter untersuchen
		while((boardPositionToBeAnalyzed = getBestBoardPosition()) != null && parentSolver.isCancelled() == false) {

            // Get the box and player positions of the board position.
            positions = boardPositionToBeAnalyzed.getPositions();

			// Das Spielfeld mit der aktuellen Stellung besetzen
			board.setBoardPosition(positions);

            // If all boxes are inactive (-> pulled to the goal room entrance) a solution has been found.
			for(boxNo2 = 0; boxNo2<board.boxCount; boxNo2++) {
			    if(positions[boxNo2] != 0) {
			    	break;
			    }
			}
			if(boxNo2 == board.boxCount) {
			    solutionBoardPosition = boardPositionToBeAnalyzed;
			    return true;
			}

			// Erreichbare Felder des Spielers ermitteln. Da diese Felder auch nach der Deadlock-
			// prüfung noch gebraucht werden, werden sie in einem eigenen Objekt berechnet.
			playersReachableSquares.update();

			// Nun muss geprüft werden, welche Kisten in welche Richtungen gezogen werden können
			// -> welche neuen Stellungen können erzeugt werden
			for(int boxNo = 0; boxNo < board.boxCount; boxNo++) {

				// Kistenposition holen
				boxPosition = positions[boxNo];

			    // Die Kiste kann durch die Zielraumlogik entfernt worden sein,
				// weil sie den Corralerzwinger erreicht hat. In diesem Fall kann sofort
				// zur nächsten Kiste gesprungen werden.
				if(boxPosition == 0) {
					continue;
				}

				// Ziehen in jede Richtung prüfen
				for(int direction = 0; direction < 4; direction++) {
					// Mögliche neue Position der Kiste errechnen
					newBoxPosition = boxPosition + offset[direction];

					// Falls Kiste nicht in die gewünschte Richtung gezogen werden kann,
					// sofort die nächste Richtung probieren (kommt Spieler auf richtige Seite
					// und ist Zielfeld kein simple Deadlockfeld wird geprüft)
					if(board.isAccessibleBox(newBoxPosition) == false ||
					   playersReachableSquares.isSquareReachable(newBoxPosition + offset[direction]) == false) {
						continue;
					}

					// Pull durchführen und Spieler entsprechend setzen.
					board.pushBox(boxPosition, newBoxPosition);
					board.playerPosition = newBoxPosition + offset[direction];

				    // Objekt der aktuellen Stellung erzeugen (mit Referenz zur vorigen Stellung)
					currentBoardPosition = new RelativeBoardPositionGoalAreaSolver(board, boxNo, direction, boardPositionToBeAnalyzed);
					currentBoardPosition.setSearchDirection(SearchDirection.BACKWARD_GOAL_ROOM);

					// Falls die Kiste auf den Corralerzwinger gezogen wurde und der Spieler außerhalb des
					// Zielfeldraums steht, wird sie als deaktiv gespeichert, da sie den Zielfeldraum
					// verlassen kann (sie bekommt dadurch die Position 0 zugewiesen).
					if(board.isBox(goalRoomEntrancePosition) && goalRoom[board.playerPosition] == false) {
						((RelativeBoardPositionGoalAreaSolver) currentBoardPosition).setBoxInactive();
					}

					// Prüfen, ob diese Stellung bereits schon einmal erreicht wurde, indem versucht
					// wird sie aus dem Stellungsspeicher zu lesen
					oldBoardPosition = positionStorage.getBoardPosition(currentBoardPosition);

					// Falls die Stellung bereits vorher erreicht wurde, kann sie verworfen werden.
					if(oldBoardPosition != null && ((BoardPosition) oldBoardPosition).getPushesCount() <= currentBoardPosition.getPushesCount()) {
						// Push der Kiste rückgängig machen. Der Spieler wird so wieso beim nächsten
						// Aufruf wieder umgesetzt. Dies muss hier also nicht extra geschehen.
						board.pushBoxUndo(newBoxPosition, boxPosition);
						continue;
					}

					// Die deaktiven Kisten für die Lowerboundberechnung auf ihr tatsächliches Feld setzen.
					// Da die Lowerboundroutine nur das Kistendatenobjekt verwendet und nicht das Spielfeld
					// selber, reicht es aus die Kistenpositionen direkt im Kistendatenobjekt zu setzen.
					for(boxNo2 = 0; boxNo2 < board.boxCount; boxNo2++) {
					    if(positions[boxNo2] == 0) {
					    	board.boxData.setBoxPosition(boxNo2, goalRoomEntrancePosition);
					    }
					}

					// Es wurde eine neue Stellung erreicht, deren Lowerbound nun errechnet wird.
					pushesLowerBoundCurrentBoardPosition = lowerBoundCalcuation.calculatePushesLowerboundBackwardsSearch(newBoxPosition);

					// Deaktive Kisten wieder auf Position 0 setzen.
					for(boxNo2 = 0; boxNo2 < board.boxCount; boxNo2++) {
                        if(positions[boxNo2] == 0) {
                        	board.boxData.setBoxPosition(boxNo2, 0);
                        }
					}

//					debugcode for goalroom bug
//	if(pushesLowerBoundCurrentBoardPosition != LowerBoundCalculation.DEADLOCK)				 {
//		System.out.println("Lower bound in ="+(pushesLowerBoundCurrentBoardPosition+currentBoardPosition.getPushesCount()));
//
//		if(pushesLowerBoundCurrentBoardPosition+currentBoardPosition.getPushesCount() == 15) {
////							for(boxNo2 = 0; boxNo2 < board.boxCount; boxNo2++) {
////								application.board.setBox(positions[boxNo2]);
////							}
////							application.board.pushBox(boxPosition, newBoxPosition);
////							application.redraw(true);
//		}
//		if((pushesLowerBoundCurrentBoardPosition+currentBoardPosition.getPushesCount()) == 15) {
//
////			 beide Kisten stehen auf Position 27. Lowerbound wird nur mit der aktuellen Position des Spielers berechnet,
////			 was dazu führt, dass auch die deaktive Kiste nur von der einen Seite gezogen werden kann.
////			System.out.println("corralerzwinger:" +goalRoomEntrancePosition);
//
//			System.out.println("lower bound = "+pushesLowerBoundCurrentBoardPosition+", Pushes bisher: "+currentBoardPosition.getPushesCount());
//			for(IBoardPosition b = currentBoardPosition; b.getPrecedingBoardPosition() != null; b = b.getPrecedingBoardPosition()) {
//				application.board.setBoardPosition(b);
//				application.redraw(true);
//			}
//
//			// Ursprung wieder herstellen und den Push durchführen
//			for(boxNo2 = 0; boxNo2 < board.boxCount; boxNo2++) {
//				application.board.setBox(positions[boxNo2]);
//			}
//			application.board.pushBox(boxPosition, newBoxPosition);
//		}
//	}


					// Push der Kiste rückgängig machen. Der Spieler wird sowieso beim nächsten
					// Aufruf wieder umgesetzt. Dies muss hier also nicht extra geschehen.
					board.pushBoxUndo(newBoxPosition, boxPosition);

					// Falls es eine Deadlockstellung ist, wird sofort die nächste Richtung untersucht
					if(pushesLowerBoundCurrentBoardPosition == LowerBoundCalculation.DEADLOCK) {
						continue;
					}

					// Falls eine Lösung gefunden wurde, wird die Stellung gemerkt und zurückgesprungen
					if(pushesLowerBoundCurrentBoardPosition == 0) {
						solutionBoardPosition = currentBoardPosition;
						return true;
					}

					// Hierdrin wird die Anzahl aller bei der Lösungssuche erreichten NichtDeadlock-
					// stellungen errechnet.
					boardPositionsCount++;

					// Stellung speichern.
					positionStorage.storeBoardPosition(currentBoardPosition);

					storeBoardPosition(currentBoardPosition, pushesLowerBoundCurrentBoardPosition);
				}
			}
		}

		return false;
	}
}