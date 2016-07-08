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
import java.util.ListIterator;

import de.sokoban_online.jsoko.JSoko;
import de.sokoban_online.jsoko.board.Board;
import de.sokoban_online.jsoko.boardpositions.AbsoluteBoardPosition;
import de.sokoban_online.jsoko.boardpositions.AbsoluteBoardPositionMoves;
import de.sokoban_online.jsoko.boardpositions.BoardPosition;
import de.sokoban_online.jsoko.boardpositions.IBoardPosition;
import de.sokoban_online.jsoko.boardpositions.RelativeBoardPosition;
import de.sokoban_online.jsoko.leveldata.solutions.Solution;
import de.sokoban_online.jsoko.pushesLowerBoundCalculation.LowerBoundCalculation;
import de.sokoban_online.jsoko.resourceHandling.Settings.SearchDirection;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Debug;
import de.sokoban_online.jsoko.utilities.Utilities;


/**
 * In this solver class we try to solve a level using the A* (A-star) algorithm.
 * See e.g. http://en.wikipedia.org/wiki/A*_search_algorithm
 */
public class SolverAStar extends Solver {

	// Hierdrin wird die Stellung übergeben, falls eine Lösungsstellung gefunden wurde
	IBoardPosition solutionBoardPosition = null;

	/** Collection storing the configurations reached during the search */
    ArrayList<LinkedList<IBoardPosition>> reachedBoardPositions;

    /** Contains the length of the shortest solution path */
	int minimumSolutionPathLength = 0;

	/** Length of longest solution path analyzed up to now */
	int longestSolutionPath = 0;


	/**
	 * Creates an A*-Solver.
	 *
	 * @param application the reference to the main object holding all references
	 * @param solverGUI reference to the GUI of this solver
	 */
	public SolverAStar(JSoko application, SolverGUI solverGUI) {
		super(application, solverGUI);
	}

	/**
	 * Tries to solve the configuration from the current board.
	 */
	@Override
	public Solution searchSolution() {

		// Flag, indicating whether a solution has been found by the solver.
		boolean isSolutionFound = false;

		// Number of board positions visited until a solution has been found.
		boardPositionsCount = 0;

		// Backup the board position present when the solver has been started
		// to set it back, when the solver is closed.
		AbsoluteBoardPositionMoves startBoardPosition;

		// Lower bound of the start board position.
		int lowerBoundStartBoardPosition = 0;


		// Dies ist eine ArrayList, die die Stellungen in verketteten Listen aufnimmt.
		// An Stelle 0 ist eine Liste mit allen Stellungen, die eine Lösungspfadlänge von 0 haben,
		// an Stelle 1 alle Stellungen mit eine Lösungspfadlänge von 1, ...
		// Standardmäßig wird davon ausgegangen, dass der Lösungspfad höchstens 20 Pushes lang ist.
		// (Ist er länger, wird die Liste automatisch verlängert -> siehe "storeStellung)
		reachedBoardPositions = new ArrayList<LinkedList<IBoardPosition>>(20);
		for(int linkedlistNr = 0; linkedlistNr < 20; linkedlistNr++) {
			reachedBoardPositions.add(new LinkedList<IBoardPosition>());
		}

		// Let the search start with the current board position.
		IBoardPosition currentBoardPosition = new AbsoluteBoardPosition(board);

		// Backup the start board position.
		startBoardPosition = new AbsoluteBoardPositionMoves(board);

		// Calculate the pushes lower bound for the start board position which is also the
		// minimum solution length. Ensure that no frozen boxes are there from previous runs.
        board.boxData.setAllBoxesNotFrozen();

		lowerBoundStartBoardPosition = lowerBoundCalcuation.calculatePushesLowerbound();
		if(lowerBoundStartBoardPosition == LowerBoundCalculation.DEADLOCK) {
			return null;
		}
		minimumSolutionPathLength = lowerBoundStartBoardPosition;

		// Spielfeldsituation als erreicht kennzeichnen.
		// Als Richtung wird 0 mitgegeben, da eine Richtung mitgegeben werden muss.
		// Sie ist aber irrelevant, da in der Anfangsstellung ja keine Kiste verschoben wurde.
		// Als Kistennr wird der Index im Array der Anfangspositionen mitgegeben!
		// Falls die Anfangsstellung schon gelöst war, wird trotzdem eine Lösung gesucht, da es
		// auch Level gibt, die im gelösten Zustand starten. In diesem Fall darf die Anfangs-
		// stellung aber nicht als bereits erreicht gekennzeichnet werden.
		if(lowerBoundStartBoardPosition != 0) {
			currentBoardPosition.setSearchDirection(SearchDirection.FORWARD);
			positionStorage.storeBoardPosition(currentBoardPosition);
		}

		// Aktuelle Stellung als erreichte Stellung speichern
		storeBoardPosition(currentBoardPosition, lowerBoundStartBoardPosition);

		// Während der Suche können Stellungen auftreten, die einen kürzeren Lösungspfad haben
		// als die Anfangsstellung (weil Penaltys wegfallen können!)
		// Deswegen darf der Lowerbound der Anfangsstellung nicht als minimale Lösungspfadlänge
		// genommen werden! Die minimale Lösungspfadlänge wird zumindest auf die richtige
		// Parität gesetzt, wenn schon nicht der Lowerbound direkt gesetzt werden kann :-)
		minimumSolutionPathLength = lowerBoundStartBoardPosition%2;

		// Die Anfangsstellung für die Statistik auch mitzählen
		boardPositionsCount++;

		// Zu Beginn stehen keine Kisten auf dem Spielfeld. Diese werden erst bei Beginn
		// der Suche gesetzt
		for(int boxNo = 0; boxNo < board.boxCount; boxNo++) {
			board.removeBox(board.boxData.getBoxPosition(boxNo));
		}

		// Startzeit merken
		long startTimeStamp = System.currentTimeMillis();

		// Nach einer Lösung suchen
		isSolutionFound = forwardSearch();

		// Display an info on the screen.
		if(isSolutionFound == true) {
			publish(
			Texts.getText("solved") +
			Texts.getText("pushes") + ": " + solutionBoardPosition.getPushesCount() + ", "+
			Texts.getText("numberofpositions") + boardPositionsCount + " " );
		}
		else {
			publish(Texts.getText("solver.noSolutionFound"));
		}

		if(Debug.isDebugModeActivated) {
			System.out.println("===================================");
			System.out.println("Solution found: " + isSolutionFound);
			if(isSolutionFound == true) {
				System.out.println("Number of pushes: "+solutionBoardPosition.getPushesCount());
			}
			System.out.println("No Deadlockpositions: "+boardPositionsCount);
			System.out.println("Total positions: "+positionStorage.getNumberOfStoredBoardPositions());
			System.out.println("Time for search: "+(System.currentTimeMillis()-startTimeStamp));
		}

		// If no solution has been found clear the position storage and set back
		// the board position that has been set as the solver has been started.
		if(isSolutionFound == false){
			positionStorage.clear();

		    // Restore the start board position.
		    board.setBoardPosition(startBoardPosition);

			return null;
		}

		// In dieser ArrayList werden alle Pushes der Lösung abgelegt, um die Lösung Push für Push durchgehen zu können.
		ArrayList<IBoardPosition> pushes = new ArrayList<IBoardPosition>(solutionBoardPosition.getPushesCount());

		// Es werden alle Stellungen rückwärts abgegangen, die bei der Suche erreicht wurden.
	    for( currentBoardPosition = solutionBoardPosition
	       ; currentBoardPosition.getPrecedingBoardPosition() !=  null
	       ; currentBoardPosition = currentBoardPosition.getPrecedingBoardPosition())
	    {
	        if(currentBoardPosition.getBoxNo() != NO_BOX_PUSHED) {
	            pushes.add(0, currentBoardPosition);
	        }
	    }

	    // Restore the start board position.
	    board.setBoardPosition(startBoardPosition);

		// Der aktuelle Index in der History muss gemerkt werden, da der Benutzer genau
		// hier wieder starten soll. Alle Bewegungen, die jetzt eingefügt werden, sollen also
		// "in der Zukunft" liegen.
		int currentIndex = application.movesHistory.getCurrentMovementNo();

		// Alle Verschiebungen in die History eintragen.
		for(ListIterator<IBoardPosition> i = pushes.listIterator();i.hasNext();) {
			currentBoardPosition = i.next();

			int pushedBoxNo = currentBoardPosition.getBoxNo();
			int direction   = currentBoardPosition.getDirection();
			application.movesHistory.addMovement(direction, pushedBoxNo);
		}

		// Den aktuellen Zug in der History wieder auf den Wert setzen, auf den er vor dem
		// Einfügen der neuen Züge stand. Dadurch kann der Spieler mit "redo" die Züge durchgehen.
		application.movesHistory.setMovementNo(currentIndex);

		// Die Anfangsstellung auf dem Spielfeld setzen.
		for(int position=board.firstRelevantSquare; position<board.lastRelevantSquare; position++) {
			board.removeBox(position);
		}
		board.setBoardPosition(startBoardPosition);

		// Show a hash table statistic if requested.
		if(Debug.debugShowHashTableStatistic) {
			positionStorage.printStatisticDebug();
		}

		// Clear the data from the hash table, to free that memory.
		positionStorage.clear();

		// Somewhat optimize the solution.
		// That also enters the player moves into the history.
		optimizeSolution();

		// Create the new solution.
		Solution newSolution = new Solution(application.movesHistory.getLURDFromHistoryTotal());
		newSolution.name = solutionByMeNow();

		return newSolution;
	}


	/**
	 * Generates successor configurations by performing all legal pushes.
	 * Each generated configuration is stored in the hash table.
	 * Had the configuration already been reached by a push, we skip it.<br>
	 * After the solution has been found the solution path is put into the
	 * movementHistory by calling method {@link #optimizeSolution()}
	 * inside method {@link #searchSolution()}.
	 *
	 * @return <code>true</code> if a solution is found, and<br>
	 *        <code>false</code> if no solution is found
	 */
	protected boolean forwardSearch() {

		// Nimmt eine Kistenposition auf
		int boxPosition;

		// Nimmt die mögliche neue Kistenposition auf
		int newBoxPosition = 0;

		// Stellungobjekt, dass die aktuelle Stellung aufnimmt
		IBoardPosition currentBoardPosition;

		IBoardPosition boardPositionToBeAnalyzed;

		// Nimmt den Status einer Stellung auf
		IBoardPosition oldBoardPosition;

		// Nimmt den aktuellen Lowerbound einer Stellung auf
		int lowerBoundCurrentBoardPosition = 0;

		// In diesem Bitarray sind die mit den Kistennummern korrespondierenden Bits gesetzt,
		// wenn diese Kiste für weitere Pushes relevant ist.
		boolean[] relevantBoxes = null;


		// Die Stellung mit der geringsten geschätzten Lösungspfadlänge weiter untersuchen
		while((boardPositionToBeAnalyzed = getBestBoardPosition()) != null && isCancelled() == false) {

			// Das Spielfeld mit der aktuellen Stellung besetzen
			board.setBoardPosition(boardPositionToBeAnalyzed);

			// Only for debugging: show board positions.
			if(solverGUI.isShowBoardPositionsActivated.isSelected()) {
				displayBoard();
			}

			// Erreichbare Felder des Spielers ermitteln. Da diese Felder auch nach der Deadlock-
			// prüfung noch gebraucht werden, werden sie in einem eigenen Objekt berechnet.
			playersReachableSquares.update();

			int pushedBoxNo = boardPositionToBeAnalyzed.getBoxNo();

			// Falls das Level eine Zielraumlösung hat, so wird der Zielraum mit der vorberechneten
			// Lösung gelöst. Falls dadurch das Level insgesamt gelöst wurde wird sofort mit true
			// zurückgesprungen.
			// Da die Blockerkisten noch von der letzten Stellung gesetzt sind müssen sie und die
			// Kistendistanzen neu ermittelt werden.
			if(goalRoomSolutionPath != null) {
			    board.boxData.setAllBoxesNotFrozen();
			    deadlockDetection.freezeDeadlockDetection.isDeadlock(board.boxData.getBoxPosition(0), true);
			    board.distances.updateBoxDistances(SearchDirection.FORWARD, true);
			    boardPositionToBeAnalyzed = pushBoxGoalRoomLevel(boardPositionToBeAnalyzed);

			    // Falls es keine weitere zu untersuchende Stellung gibt, bedeutet das, dass das
			    // Level gelöst wurde.
			    if(boardPositionToBeAnalyzed == null) {
			        return true;
			    }
			}

			// Ermitteln, welche Kisten für den nächsten Push relevant sind.
			relevantBoxes = identifyRelevantBoxes();

            // If no box has been pushed the pushed box number is set to -1, so the tunnel detection isn't performed.
            if(pushedBoxNo == NO_BOX_PUSHED) {
				pushedBoxNo = -1;
			}

			// Nun muss geprüft werden, welche Kisten in welche Richtungen verschoben werden können
			// -> welche neuen Stellungen können erzeugt werden
			for(int boxCounter = -1, boxNo; boxCounter < board.boxCount; boxCounter++) {

				//The box pushed last time is pushed first (when boxCounter is -1) for tunnel detection.
				if(boxCounter == pushedBoxNo) {
					continue;
				}

				// Die erste Kiste ist immer die im letzten Zug verschobene Kiste, es sei denn
				// es gibt keinen letzten Zug (Anfangsstellung = AbsoluteStellung = gibt -1 zurück)
				if(boxCounter == -1) {
					boxNo = pushedBoxNo;

					// Falls sich die verschobene Kiste in einem Tunnel befindet und nicht auf einem
					// Zielfeld steht, brauchen für diesen Push nur Verschiebungen dieser Kiste
				    // geprüft werden!
			        // (Es kann nur die verschobene Kiste in einem Tunnel stehen, da alle anderen
			        // ja schon vorher weitergeschoben worden wären!)
			        if(isBoxInATunnel(pushedBoxNo, boardPositionToBeAnalyzed.getDirection())) {
						boxCounter = board.boxCount;
					}
				} else {
					boxNo = boxCounter;
				}

			    // Falls es kein Tunnel ist, so wird geprüft, ob ein I-Corral vorliegt. In diesem Fall
			    // können die nicht relevanten Kisten übersprungen werden.
			    // (Bei einem Tunnel wird sowieso nur eine Kiste verschoben, so dass auch ein I-Corral
			    // die Anzahl der zu verschiebenen Kisten nicht mehr reduzieren könnte)
				if(boxCounter < board.boxCount && relevantBoxes != null && relevantBoxes[boxNo] == false) {
					continue;
				}

				// Kistenposition holen
				boxPosition = board.boxData.getBoxPosition(boxNo);

				// Falls die Kiste im Zielraum liegt, so wird sie durch die spezielle Zielraumlogik verschoben.
				if(goalRoomSquares != null && goalRoomSquares[boxPosition] == true) {
				    // Falls die Kiste in einem Tunnel steht müssen die anderen Kisten trotzdem
				    // untersucht werden, da die aktuelle Kiste ignoriert wird.
				    if(boxCounter == board.goalsCount) {
				        boxCounter = -1;
				    }
				    continue;
				}

				// Verschieben in jede Richtung prüfen
				for(int direction = 0; direction < 4; direction++) {
					// Mögliche neue Position der Kiste errechnen
					newBoxPosition = boxPosition + offset[direction];

					// Falls Kiste nicht in die gewünschte Richtung verschoben werden kann,
					// sofort die nächste Richtung probieren (kommt Spieler auf richtige Seite
					// und ist Zielfeld kein simple Deadlockfeld wird geprüft)
					if(playersReachableSquares.isSquareReachable(boxPosition - offset[direction]) == false
						|| board.isAccessibleBox(newBoxPosition) == false) {
						continue;
					}

					// Push durchführen und den Spieler auch auf das alte Kistenfeld setzen
					board.pushBox(boxPosition, newBoxPosition);
					board.playerPosition = boxPosition;

				    // Objekt der aktuellen Stellung erzeugen (mit Referenz zur vorigen Stellung)
					currentBoardPosition = new RelativeBoardPosition(board, boxNo, direction, boardPositionToBeAnalyzed);
					currentBoardPosition.setSearchDirection(SearchDirection.FORWARD);

					// Prüfen, ob diese Stellung bereits schon einmal erreicht wurde, indem versucht
					// wird sie aus dem Stellungsspeicher zu lesen
					oldBoardPosition = positionStorage.getBoardPosition(currentBoardPosition);

					// Falls die Stellung bereits vorher erreicht wurde, kann sie verworfen werden.
					if(oldBoardPosition != null
							&& oldBoardPosition.getPushesCount() <= currentBoardPosition.getPushesCount()) {
						// Push der Kiste rückgängig machen. Der Spieler wird sowieso beim nächsten
						// Aufruf wieder umgesetzt. Dies muss hier also nicht extra geschehen.
						board.pushBoxUndo(newBoxPosition, boxPosition);
						continue;
					}

					// Es wurde eine neue Stellung erreicht, deren Lowerbound nun errechnet wird.
					lowerBoundCurrentBoardPosition = lowerBoundCalcuation.calculatePushesLowerBound(newBoxPosition);

					// Push der Kiste rückgängig machen. Der Spieler wird sowieso beim nächsten
					// Aufruf wieder umgesetzt. Dies muss hier also nicht extra geschehen.
					board.pushBoxUndo(newBoxPosition, boxPosition);

					// Falls es eine Deadlockstellung ist, wird sofort die nächste Richtung untersucht
					if(lowerBoundCurrentBoardPosition == LowerBoundCalculation.DEADLOCK) {
						continue;
					}

					// Falls eine Lösung gefunden wurde, wird die Stellung gemerkt und zurückgesprungen
					if(lowerBoundCurrentBoardPosition == 0) {
						solutionBoardPosition = currentBoardPosition;
						return true;
					}

					// Hierdrin wird die Anzahl aller bei der Lösungssuche erreichten NichtDeadlock-
					// stellungen errechnet.
					boardPositionsCount++;

					if((boardPositionsCount % 500) == 0) {

						// Throw "out of memory" if less than 15MB RAM is free.
						if(Utilities.getMaxUsableRAMinMiB() <= 15) {
							isSolverStoppedDueToOutOfMemory = true;
							cancel(true);
						}
					}

					// Stellung in der Hashtable speichern, um keine Stellung doppelt zu untersuchen.
					positionStorage.storeBoardPosition(currentBoardPosition);

					// Stellung in die Queue der noch zu untersuchenden Stellungen eintragen.
					storeBoardPosition(currentBoardPosition, lowerBoundCurrentBoardPosition);
				}
			}
		}

		return false;
	}


	/**
	 * Calculates and returns, whether the box, specified by its box number,
	 * is in a tunnel.
	 * This is important for the solution search, since a box, which is in a tunnel
	 * can be forced to be pushed, and all other possible pushes can be ignored!
	 * <p>
	 * This method is public only for debugging purposes.
	 *
	 * @param boxNo the number of the box
	 * @param pushDirection	the direction the box has been pushed to
	 * @return	<code>true</code> if the box is in a tunnel, and<br>
	 * 		   <code>false</code> if the box is not in a tunnel
	 */
	public boolean isBoxInATunnel(int boxNo, int pushDirection){

	    // Kistenposition
	    int boxPosition;

	    // Neue Kistenposition
	    int newBoxPosition;

        // Get current box position
        boxPosition = board.boxData.getBoxPosition(boxNo);

        // Determine the opposite axis of the push direction
        int oppositeAxis = UP;
        if(pushDirection == UP || pushDirection == DOWN) {
            oppositeAxis = RIGHT;
        }

        /* First let's get the following tunnels (and rotated versions):
         *
         * #$    #$#    $#
         * #@#   #@#   #@#
         */
        if( board.isGoal(boxPosition) == false &&
            board.isWall(boxPosition-offset[pushDirection]+offset[oppositeAxis]) &&
            board.isWall(boxPosition-offset[pushDirection]-offset[oppositeAxis]) &&
           (board.isWall(boxPosition+offset[oppositeAxis]) || board.isWall(boxPosition-offset[oppositeAxis]))) {
            return true;
        }


	    /*
		 * Prüfen, ob durch Verschieben der Kiste nur Sicherheitsstellungen erzeugt werden können
		 * oder ob die Kiste überhaupt nur in eine Richtung verschoben werden kann.
		 *
		 * Sicherheitsstellung =
		 *            #
		 *   #$# oder $
		 *	          #
		 */
		// Erreichbare Felder des Spielers ermitteln, wenn nur diese eine Kiste auf dem Feld ist
		board.setWall(boxPosition);
		board.playersReachableSquaresOnlyWalls.update();
		board.removeWall(boxPosition);

		// Zählt die Anzahl der möglichen Verschiebungen (bzw. Ziehungen)
		byte pushableDirectionsCount = 0;

		// Zählt die Anzahl an Sicherheitsstellungen, die entstanden sind
		byte safeSituationsCount = 0;

		// Anzahl der Richtungen, in der die Kiste tatsächlich verschoben werden kann.
		// (Dabei werden auch die anderen Kisten auf dem Spielfeld berücksichtigt)
		byte reallyPushableDirectionsCount = 0;

		// Anzahl Felder, auf die der Spieler die Kiste verschieben kann, die vor der
		// Verschiebung nicht durch den Spieler erreicht werden konnten (= Felder im Corral)
		byte squaresInCorralCount = 0;

		// Anzahl der NichtMauer-Seitenfelder der Kiste
		byte notWallNeighborSquaresCount = 0;

	    // Verschieben in alle Richtungen ausprobieren
		for(byte direction=0; direction<4; direction++) {
		    //	Position der Kiste berechnen, auf der sie stehen würde, wenn die Verschiebung
		    // in die verschiebbare Richtung durchgeführt worden wäre.
		    newBoxPosition = boxPosition + offset[direction];

		    // Anzahl der NichtMauerSeitenfelder berechnen
		    if(board.isWall(newBoxPosition) == false) {
		    	notWallNeighborSquaresCount++;
		    }

		    // Prüfen, ob Spieler auf die eine Seite kommt und die andere Seite frei ist (= keine Mauer,
		    // nicht ".isBetretbar()", denn die Kisten sind ja noch auf dem Feld!)
		    // Falls er es nicht kann, kann gleich in der nächsten Richtung geprüft werden.
		    if(board.isWall(newBoxPosition) ||
		            // Die geblockten Kisten müssten vorher extra ermittelt werden, dies wird
		            // aber im Solver erst später getan ...
		            board.playersReachableSquaresOnlyWalls.isSquareReachable(boxPosition - offset[direction]) == false) {
				continue;
			}

		    // Kiste kann auf die gewünschte Position geschoben werden
		    pushableDirectionsCount++;

		    // Prüfen, ob die Kiste auch tatsächlich verschoben werden kann (unter Berücksichtigung,
		    // dass noch weitere Kisten auf dem Feld sind - die aktuell erreichbaren Felder des
		    // Spielers liegen im globalen Array bereits vor)
		    if( playersReachableSquares.isSquareReachable(boxPosition - offset[direction]) ) {
		    	reallyPushableDirectionsCount++;
		    }

		    // Falls der Spieler das neue Feld der Kiste vorher nicht erreichen konnte, ist es ein
		    // Corralfeld und für diese Richtung bliebe sowieso nichts anderes übrig, als die Kiste
		    // auf dieses Feld zu schieben. Diese Corralfelder werden gezählt.
		    if(board.playersReachableSquaresOnlyWalls.isSquareReachable(newBoxPosition) == false) {
		    	squaresInCorralCount++;
		    }

		    // Prüfen, ob eine Sicherheitsstellung erreicht wurde
		    // (Unabhängig von der Verschiebungsrichtung können immer beide Achsen geprüft werden!)
		    if(board.isWall(newBoxPosition+offset[UP]) && board.isWall(newBoxPosition+offset[DOWN])
		    		|| board.isWall(newBoxPosition-1) && board.isWall(newBoxPosition+1)) {
				safeSituationsCount++;
			}
		}

		/*
		 * Wenn die Kiste nur in Sicherheitsstellungen befördert werden kann, dann ist sie
		 * quasi in einem Tunnel.
		 * Dies gilt allerdings nur, falls sie wirklich alle diese Sicherheitsstellungen mit dem
		 * nächsten Push erreichen kann.
		 *
		 * Falls es nur zwei Sicherheitsstellungen gibt
		 *
		 * Falls allerdings nur 2 Pushes überhaupt möglich sind und beides Sicherheitsstellungen sind
		 * und überhaupt nur zwei NichtMauerfelder um die Kiste herum sind, so kann die Kiste
		 * auf jeden Fall sofort weitergeschoben werden.
		 * Es handelt sich in diesen Fällen um klassische Tunnel. l_anzahlNichtMauerSeitenfelder darf
		 * nicht weggelassen werden!
		 * -------------------------------------------------------------------------------------------
		 * Beispiel: # #
		 *            $#
		 *           # #
		 * Diese Kiste befindet sich nicht in einem Tunnel, da sie von links in den Tunnel geschoben
		 * worden sein kann.
		 * Beispiellevel, das sonst nicht optimal gelöst werden kann:
		 *
		 * "Hayley", by Lee J Haywood
		 *
		 *	#######
		 *	###  .#
		 *	# $$# #
		 *	# @$. #
		 *	#. $# #
		 *	#   . #
		 *	#######
		 *
		 * Besonderheit: Die Kiste trennt das Spielfeld in mehrere Bereiche auf (Corrals). Falls
		 * sie mit dem nächsten Push nur in vorher nicht durch den Spieler erreichbare Corrals
		 * geschoben werden kann, dann gilt die Kiste auch als in einem Tunnel.
		 */
		if(
			// "Normaler" Tunnel
				pushableDirectionsCount == 2 && safeSituationsCount == 2 &&
				notWallNeighborSquaresCount == 2
			||

			// Kiste kann nur in Sicherheitsstellungen verschoben werden und alle diese Verschiebungen
			// sind derzeit auch möglich.
				pushableDirectionsCount > 0 && pushableDirectionsCount == safeSituationsCount
				&& pushableDirectionsCount == reallyPushableDirectionsCount
			||

			// Durch alle möglichen Verschiebungen der Kiste würden Corralfelder erreicht, die vorher
			// nicht durch den Spieler erreichbar waren. In diesem Fall gilt die Kiste als "im Tunnel",
			// wenn alle diese Verschiebungen tatsächlich möglich sind und die Kiste auch tatsächlich in
			// mindestens ein Corral geschoben werden kann.
				pushableDirectionsCount == squaresInCorralCount &&
				pushableDirectionsCount == reallyPushableDirectionsCount &&
				reallyPushableDirectionsCount > 0)
		{

			// Es ist nun klar, dass sich die Kiste in einem "Tunnel" befindet.
			// Falls die Kiste auf einem Zielfeld steht, so ist ein Tunnel allerdings nur dann relevant,
			// falls die Verschiebungen alle in vorher nicht erreichbare Corralfelder enden und
			// in den Corrals mindestens 1 weiteres noch unbesetztes Zielfeld vorhanden ist.
			if(board.isGoal(boxPosition)) {
				if(pushableDirectionsCount == squaresInCorralCount) {
					for(int position = board.firstRelevantSquare; position < board.lastRelevantSquare; position++) {
						if(board.playersReachableSquaresOnlyWalls.isSquareReachable(position) == false &&
								board.isGoal(position) && board.isBox(position) == false) {
							return true;
						}
					}
				}

				// Box is in a tunnel, but on a goalsquare. Therefore it isn't in a tunnel after all.
				return false;
			}

			// The box is in a tunnel.
			return true;
		}

		// The box isn't in a tunnel.
		return false;
	}


	/**
	 * Returns the board position having the shortest determined solution path length.
	 *
	 * @return the board position having the shortest determined solution path length
	 */
	protected BoardPosition getBestBoardPosition(){

		// Nimmt die Liste aller Stellungen mit einer bestimmten Lösungspfadlänge auf
		LinkedList<IBoardPosition> boardPositionList;

		for(int solutionLength = minimumSolutionPathLength; solutionLength <= longestSolutionPath; solutionLength+=2) {

			// Liste der Stellungen mit der aktuellen Pfadlänge holen. Da sich die Pfadlänge
			// immer in 2er Schritten erhöht wird ein Pfad der Länge x an Stelle x/2 gespeichert.
			boardPositionList = reachedBoardPositions.get(solutionLength/2);

			if(boardPositionList.size() > 0){
				// Falls die neue minimale Lösungspfadlänge größer ist als die alte, so wird sie am Bildschirm ausgegeben.
				if(minimumSolutionPathLength < solutionLength) {
					// Info auf dem Bildschirm ausgeben (-> Infotext setzen)
					publish(
					Texts.getText("numberofpositions")+boardPositionsCount+", "+
					Texts.getText("searchdepth")+solutionLength);
				}

				minimumSolutionPathLength = solutionLength;

				// Die zuletzt eingefügte Stellung zurückgeben
	    		return (BoardPosition) boardPositionList.removeLast();
			}
		}

		return null;
	}


	/**
	 * Stores the passed configuration with the passed estimated solution path length.
	 * The length is computed as sum of the already used pushes and the estimated
	 * number of pushes between here and the end configuration.
	 *
	 * @param boardPosition	 configuration to be stored
	 * @param pushesLowerBound  estimated lower bound for the pushes needed to reach the end configuration
	 */
	 protected void storeBoardPosition(IBoardPosition boardPosition, int pushesLowerBound) {

		// Gesamtpfadlänge für die Stellung
		int pathLength;

		// In dieser Variable wird die Position der Stellung im Array errechnet. Je nach Pfadlänge
		// wird die Stellung in einer unterschiedlichen Liste gespeichert. Dabei wird aus Speicherplatz
		// gründen nicht Pfadlänge = Positionsindex gesetzt, sondern die Position errechnet:
		// Position = Pfadlänge / 2, da die Pfadlänge immer in 2er Schritten erhöht werden kann,
		// weil in Sokoban das Abweichen vom perfekten Pfad immer mindestens 2 Pushes extra kostet.
		// Da der Pfad immer mindestens so lang sein muss wie der Lowerbound der Anfangsstellung
		// wird der Lowerbound der Anfangsstellung außerdem vorher noch abgezogen.
		int listIndex;


 		// Gesamtpfadlänge für die Stellung errechnen
 		pathLength = boardPosition.getPushesCount() + pushesLowerBound;

 		listIndex = pathLength/2;

 		// Wenn die Position größer als die Liste ist, wird die Liste erweitert
 		if(listIndex >= reachedBoardPositions.size()) {
			reachedBoardPositions.ensureCapacity(listIndex+1);
			for(int i=listIndex+1-reachedBoardPositions.size(); i>0; i--) {
				reachedBoardPositions.add(new LinkedList<IBoardPosition>());
			}
		}

		// Die Stellung an der zu ihrer Pfadlänge gehörenden Position speichern.
		reachedBoardPositions.get(listIndex).add(boardPosition);

		// Zur Sicherheit, falls die Lowerboundberechnung doch einmal überschätzend war
		// (darf eigentlich nie der Fall sein)
		// Es kann aber sein, dass ein Penalty wegfällt und dadurch der Lowerbound
		// plötzlich zu niedrig angesetzt wird.
		if(pathLength < minimumSolutionPathLength) {
			minimumSolutionPathLength = pathLength;
//			if(showDebug == true) {
//			    System.out.println("Lowerbound überschätzend!!!");
//			    redraw(true);
//			}
		}

		// Längsten Eintrag merken. Diese Information gilt als obere Suchgrenze für die Methode
		// "getBesteStellung", damit die Methode nicht immer die komplette Liste durchsuchen muss.
		if(pathLength > longestSolutionPath) {
			longestSolutionPath = pathLength;
		}

		// Display the current status every 3000 positions
		if(boardPositionsCount%3000 == 0) {
			publish(Texts.getText("numberofpositions")+boardPositionsCount+", "+
					Texts.getText("searchdepth")+minimumSolutionPathLength);
		}

    	// In debug mode the path to the current board position can be displayed.
    	if(Debug.isDisplayPathToCurrentBoardPositionActivated) {
    		debugShowPathToCurrentBoardPosition(boardPosition);
    	}
	}


	/**
	 * This method is called in goal room levels.  In such levels we first solve the
	 * goal room, and then the boxes are pushed one by one to the goal room entrance,
	 * and then to its goal by the already calculated goal room solution.
	 *
	 * @param boardPositionToBeAnalyzed	current configuration from which we search for
	 *                                  more configurations
	 * @return the configuration with which the search has to be continued.
	 *         <code>null</code> if there are no further configurations, because the level
	 *         is solved
	 */
	protected IBoardPosition pushBoxGoalRoomLevel(IBoardPosition boardPositionToBeAnalyzed) {

		//
		// Lokale Daten
		//
		// Nimmt den Pfad einer Kiste zum Zielfeldraumeingang auf
		int[] solutionPath = null;

		// Solange eine Kiste in den Zielfeldraum geschoben werden konnte, solange wird
		// nach weiteren Kisten gesucht.
		boolean isSearchToBeContinued = true;

		// Gibt an, ob alle Kisten im Zielfeldraum sind
		boolean isEveryBoxLocatedInGoalroom = false;

		// Eigenes Objekt für die vom Zielfeldraumeingang erreichbaren Felder des Spielers.
		// Es wird benötigt, da bei der Pfadberechnung das Originalobjekt in g_spielfeld verändert wird
		// und deshalb nicht benutzt werden kann.
		Board.PlayersReachableSquares reachableSquaresFromEntrance = board.new PlayersReachableSquares();

		//
		// Verarbeitungslogik
		//
		// Solange Kisten in den Zielfeldraum geschoben werden können werden weitere Kisten gesucht.
		while(isSearchToBeContinued == true) {
			isSearchToBeContinued = false;

			// Falls keine Kiste im Zielfeldraumeingang steht wird versucht eine Kiste dorthin zu schieben.
			if(board.isBox(goalRoomEntrancePosition) == false) {

				// Erreichbare Felder vom Zielfeldraumeingang ermitteln. Nur Kisten, die von hier
				// erreicht werden können können derzeit theoretisch in den Zielfeldraum geschoben werden.
				reachableSquaresFromEntrance.update(goalRoomEntrancePosition);

				// Alle Kisten durchgehen und prüfen, ob sie in den Zielfeldraumeingang geschoben werden können.
				for(int boxNo=0; boxNo<board.boxCount; boxNo++) {
					int boxPosition = board.boxData.getBoxPosition(boxNo);

					// Kisten, die im Zielfeldraum sind können übersprungen werden.
					if(goalRoomSquares[boxPosition] == true) {
					    continue;
					}

					solutionPath = null;

					// Falls die Kiste vom Spieler derzeit erreicht werden kann und eine Verbindung
					// zum Zielfeldraum hat wird versucht ein Pfad zum Zielfeldraum für sie zu finden.
					for(int direction=0; direction<4; direction++) {
						if(playersReachableSquares.isSquareReachable(boxPosition+offset[direction]) &&
								reachableSquaresFromEntrance.isSquareReachable(boxPosition-offset[direction])) {
							solutionPath = getPathToGoalRoomEntrance(boxPosition);
							break;
						}
					}

					// Falls die Kiste nicht pushoptimal zum Eingang geschoben werden kann sofort die nächste Kiste untersucht werden.
					if(solutionPath == null) {
						continue;
					}

					int boxStartPosition = boxPosition;
					int boxTargetPosition;
					// Kiste in den Zielfeldraumeingang schieben
					for(int i = 1; i < solutionPath.length; i++) {
						boxTargetPosition = solutionPath[i];

						// Push durchführen und den Spieler auch auf das alte Kistenfeld setzen
						// Außerdem Objekt der aktuellen Stellung erzeugen (mit Referenz zur vorigen Stellung)
						board.pushBox(boxStartPosition, boxTargetPosition);
						board.playerPosition = boxStartPosition;

						int movementDirection = board.getMoveDirection(boxStartPosition, boxTargetPosition);
						boardPositionToBeAnalyzed = new RelativeBoardPosition(board, board.getBoxNo(boxStartPosition), movementDirection, boardPositionToBeAnalyzed);
						boardPositionToBeAnalyzed.setSearchDirection(SearchDirection.FORWARD);

						boardPositionsCount++;

						boxStartPosition = boxTargetPosition;
					}

					// Erreichbare Felder für den Solver neu berechnen (Achtung: Eigenes globales Objekt)
					playersReachableSquares.update();

					// Es konnte eine Kiste auf den Zielfeldraumeingang geschoben werden.
					// Es muss deshalb nicht weitergesucht werden.
					break;
				}
			}


		    // Falls auf der Position des Zielfeldraumeingangs eine Kiste ist, so werden
		    // die Verschiebungen der Kiste nun vorberechnet durchgeführt.
		    if(board.isBox(goalRoomEntrancePosition)) {

		        // Kistenstart- und Zielposition im Lösungspfad
		        int boxStartPosition  = 0;
		        int boxTargetPosition = 0;

		        // Index im Lösungspfad
		        int index = 0;

		        // Anzahl bereits im Zielfeldraum befindlicher Kisten ermitteln
		        int boxesInGoalRoomCount = 0;
		        for(int boxNo=0; boxNo<board.boxCount; boxNo++) {
					if(goalRoomSquares[board.boxData.getBoxPosition(boxNo)] == true) {
						boxesInGoalRoomCount++;
					}
				}

		        // Der Corralerzwinger selbst gehört nicht zum Zielfeldraum. Sobald also
		        // AnzahlKisten-1 Kisten im Zielfeldraum sind, sind im Prinzip alle Kisten drin,
		        // eine steht nur gerade auf dem Corralerzwinger.
		        if(boxesInGoalRoomCount == board.boxCount-1) {
					isEveryBoxLocatedInGoalroom = true;
				}

		        /*
		         *  Jede Kiste geht über den Eingang in den Zielfeldraum.
		         *  Es muss nun auf die entsprechende Lösungssequenz für die aktuelle Kiste
		         *  im Lösungspfad positioniert werden.
                 *  Sobald eine Kiste von außen auf den Corralerzwinger geschoben wurde
                 *  wird sie als Kiste im Zielfeldraum gezählt.
		         */
		        for(index = 0; index < goalRoomSolutionPath.length; index+=2) {
		            boxStartPosition  = goalRoomSolutionPath[index];
                    boxTargetPosition = goalRoomSolutionPath[index+1];
		            if(boxStartPosition == goalRoomEntrancePosition &&
                       goalRoomSquares[2*boxStartPosition-boxTargetPosition] == false) {
		                if(--boxesInGoalRoomCount < 0) {
		                	break;
		                }
		            }
		        }

		        // Lösungspfad so weit wie möglich durchgehen und die Kisten entsprechend verschieben.
			    while(index < goalRoomSolutionPath.length) {
				    boxStartPosition  = goalRoomSolutionPath[index  ];
				    boxTargetPosition = goalRoomSolutionPath[index+1];

				    if(board.isBox(boxStartPosition) && playersReachableSquares.isSquareReachable(2*boxStartPosition-boxTargetPosition)) {

						// Push durchführen und den Spieler auch auf das alte Kistenfeld setzen
				        // Außerdem Objekt der aktuellen Stellung erzeugen (mit Referenz zur vorigen Stellung)
						board.pushBox(boxStartPosition, boxTargetPosition);
						board.playerPosition = boxStartPosition;

				        int movementDirection = board.getMoveDirectionNumber(boxStartPosition, boxTargetPosition);
				        boardPositionToBeAnalyzed = new RelativeBoardPosition(board, board.getBoxNo(boxStartPosition), movementDirection, boardPositionToBeAnalyzed);
				        boardPositionToBeAnalyzed.setSearchDirection(SearchDirection.FORWARD);

						boardPositionsCount++;
				        playersReachableSquares.update();

						/*
						 * Aus Performancegründen werden die Stellungen nicht in der Hashtable gespeichert.
						 */

				        index+=2;
				    } else {
			            // Der nächste Push des Lösungpfades konnte nicht mehr durchgeführt werden.
			            // Es wurde aber eine Kiste in den Zielfeldraum verschoben. Es muss nun nach
			            // weiteren Kisten gesucht werden, die in den Zielfeldraum verschoben werden
			            // können.
				        isSearchToBeContinued = true;
			            break;
			        }
				}
		        // Falls der Zielraumlösungspfad komplett abgearbeitet wurde -> Lösung gefunden.
		        // Es ist allerdins wichtig, dass alle Kisten im Zielfeldraum sind. Falls
		        // der Corralerzwinger selbst ein Zielfeld ist ist er nicht im Lösungspfad
		        // enthalten und es würde so mit schon bei der vorletzten Kisten zurückgesprungen.
		        if(index == goalRoomSolutionPath.length &&
		           isEveryBoxLocatedInGoalroom == true) {
		            solutionBoardPosition = boardPositionToBeAnalyzed;
					return null;
		        }
		    }
	    }

	    // Das Level wurde noch nicht gelöst.
	    return boardPositionToBeAnalyzed;
	}


	/**
	 * Determines whether (and how) a box on the board, specified by its position,
	 * can be pushed to the goal room entrance in a push optimal fashion.
	 * What is "push optimal" is defined by the box distances object, the calculation
	 * of which we trigger here.
	 * <p>
	 * If we cannot find such a path, we return <code>null</code>.
	 *
	 * @param boxStartPosition	location of the box which we want to push to the solution
	 *                          room entrance
	 * @return  path of the box to the solution room entrance,
	 *          or <code>null</code> if no such path exists.
	 */
	 private int[] getPathToGoalRoomEntrance(int boxStartPosition) {

	    // Minimale Länge des Pfades der Kiste bis zum Zielfeldraumeingang.
	    int minimumPathLength;

	    // Zielpfad, der die Kiste zum Zielfeldraumeingang führt.
	    int[] boxPath;

	    // Backup der Spielerposition
	    int playerPositionBackup;


	    // Spielerposition beim Aufruf merken, um sie später wieder setzen zu können.
	    playerPositionBackup = board.playerPosition;

	    // Minimale Länge des Pfades der Kiste bis zum Zielfeldraumeingang errechnen.
	    board.distances.calculateBoxDistancesForwards(goalRoomEntrancePosition);
	    minimumPathLength = board.distances.getBoxDistanceForwardsPosition(boxStartPosition, goalRoomEntrancePosition);

	    // Falls die Kiste den Zielfeldraumeingang nicht erreichen kann kann auch kein Pfad ermittelt werden.
	    if(minimumPathLength == Board.UNREACHABLE) {
	        return null;
	    }

	    // Hierdrin wird der Zielpfad abgelegt.
	    boxPath = new int[minimumPathLength+1];

	    // Diese Arrays können aus Performancegründen global angelegt werden. Allerdings gibt es
	    // nicht sehr viele Zielfeldraumlevel ...
        int[] boxPositionsStack   = new int[board.size];
        int[] playerPositionStack = new int[board.size];
        int[] boxDistances        = new int[board.size];

        // Anfangssituation in den Stack aufnehmen
        int topOfStack = -1;
        boxPositionsStack[++topOfStack] = boxStartPosition;
        playerPositionStack[topOfStack] = board.playerPosition;
        boxDistances[topOfStack]    	= minimumPathLength;

        // Einen pushoptimalen Pfad der Kiste zum Zielfeldraumeingang suchen
        while(topOfStack != -1) {
            int playerPosition = playerPositionStack[topOfStack];
            int boxPosition    = boxPositionsStack[topOfStack];
            int distance       = boxDistances[topOfStack--];

            // Die neue Kistenposition im Zielpfad eintragen
            boxPath[minimumPathLength-distance] = boxPosition;

            // Falls die Kiste auf dem Zielfeld steht wird das Spielfeld wieder in den Originalzustand
            // gesetzt und der Zielpfad zurückgegeben.
            if(distance == 0) {
                board.setBox(boxStartPosition);
                board.setPlayerPosition(playerPositionBackup);
                return boxPath;
            }

            // Kiste auf die jeweilige Position setzen und Spieler setzen, um die erreichbaren
            // Spielerfelder ermitteln zu können.
            board.setBox(boxPosition);
            board.setPlayerPosition(playerPosition);
            board.playersReachableSquares.update();

            for(int direction=0; direction<4; direction++) {
                int newBoxPosition = boxPosition + offset[direction];

                // Falls das Feld nicht betretbar ist oder der Spieler nicht auf die Position zum Verschieben
                // gelangen kann, kann sofort in der nächsten Richtung weitergemacht werden.
                if(board.isAccessibleBox(newBoxPosition) == false ||
                   board.playersReachableSquares.isSquareReachable(boxPosition-offset[direction]) == false) {
                    continue;
                }

                // Die Kistendistanz muss genau um eins niedriger sein als die bisherige Distanz.
                // Die Distanz ist spielerpositionsabhängig. Deshalb muss der Spieler korrekt gesetzt werden.
                board.setPlayerPosition(boxPosition);
                int newBoxDistance = board.distances.getBoxDistanceForwardsPosition(newBoxPosition, goalRoomEntrancePosition);
                if(newBoxDistance >= distance) {
                    continue;
                }

                // Neue Positionen in die Stacks aufnehmen.
                boxPositionsStack[++topOfStack] = newBoxPosition;
                playerPositionStack[topOfStack] = boxPosition;
                boxDistances[topOfStack]		= newBoxDistance;
            }
            board.removeBox(boxPosition);
        }

	    // Spielfeld wieder in den Zustand wie beim Aufruf setzen.
        board.setBox(boxStartPosition);
        board.setPlayerPosition(playerPositionBackup);

	    return null;
	}
}