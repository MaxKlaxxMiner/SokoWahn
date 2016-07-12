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
import de.sokoban_online.jsoko.boardpositions.BoardPosition;
import de.sokoban_online.jsoko.boardpositions.IBoardPosition;
import de.sokoban_online.jsoko.boardpositions.iterative.AbsoluteBoardPositionIterative;
import de.sokoban_online.jsoko.boardpositions.iterative.AbsoluteBoardPositionMovesIterative;
import de.sokoban_online.jsoko.boardpositions.iterative.IBoardPositionIterative;
import de.sokoban_online.jsoko.boardpositions.iterative.RelativeBoardPositionIterative;
import de.sokoban_online.jsoko.leveldata.solutions.Solution;
import de.sokoban_online.jsoko.pushesLowerBoundCalculation.LowerBoundCalculation;
import de.sokoban_online.jsoko.resourceHandling.Settings.SearchDirection;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Debug;



/**
 * In this solver class we try to solve a level using the IDA* algorithm, the
 * iterative deepening version of the A* algorithm, which needs less memory than plain A*.
 * See e.g. http://en.wikipedia.org/wiki/IDA*
 */
public class SolverIDAStar extends SolverAStar {

    // Maximale Anzahl Pushes für eine Lösung in einer bestimmten Iteration.
    // Wird eine Stellung erreicht, die eine höhere Anzahl an Pushes besitzt als der
    // hier gespeicherte Wert, so wird sie zunächst verworfen.
    // Erst wenn die Suche keine neue Stellung mit dieser Lösungspfadlänge mehr findet wird
    // dieser Wert erhöht.
    int maximumSolutionLengthCurrentIteration = 0;

	// Wenn alle gültigen Stellungen bereits analysiert wurden, ohne dass eine Lösung gefunden
	// wurde, so wird diese Variable entsprechend gesetzt, damit die Suche beendet wird.
	boolean isSolutionStillPossible = true;

	/**
	 * Creates an IDA*-Solver.
	 *
	 * @param application	Reference to the main object holding all references.
	 * @param solverGUI reference to the GUI of this solver
	 */
	public SolverIDAStar(JSoko application, SolverGUI solverGUI) {
		super(application, solverGUI);
	}

	/**
	 * Tries to solve the current configuration from the board.
	 */
	@Override
	public Solution searchSolution() {

		boolean isSolutionFound = false;

		// Anzahl Stellungen bis zur Lösung
		boardPositionsCount = 0;

		// Hierdrin wird die Anfangsstellung gespeichert, um bei einer Erhöhung
		// des Lowerbounds immer wieder bei dieser Stellung beginnen zu können.
		AbsoluteBoardPositionMovesIterative startBoardPosition;

		// Gibt an, wie groß der Lowerbound der Anfangsstellung ist
		int lowerBoundStartBoardPosition = 0;


		// Dies ist der Vector, der die Stellungen in verketteten Listen aufnimmt.
		// An Stelle 0 ist eine Liste mit allen Stellungen, die einen Lowerbound von 0 haben,
		// an Stelle 1 alle Stellungen mit einem Lowerbound von 1, ...
		// Standardmäßig wird davon ausgegangen, dass der Lösungspfad höchstens 20 Pushes lang ist.
		// (Ist er länger, wird die Liste automatisch verlängert -> siehe "storeStellung)
		reachedBoardPositions = new ArrayList<LinkedList<IBoardPosition>>(20);
		for(int linkedListNo = 0; linkedListNo < 20; linkedListNo++) {
			reachedBoardPositions.add(new LinkedList<IBoardPosition>());
		}

		// Die Suche mit der aktuellen Stellung starten lassen
		IBoardPositionIterative currentBoardPosition = new AbsoluteBoardPositionIterative(board);
		startBoardPosition = new AbsoluteBoardPositionMovesIterative(board);

		// Lowerbound der Anfangsstellung berechnen. Dieser Wert ist auch gleichzeitig die
		// Mindestlänge des Lösungspfades. Von einer alten Lösungssuche könnten noch geblockte
		// Kisten vorhanden sein ...
        board.boxData.setAllBoxesNotFrozen();
		lowerBoundStartBoardPosition = lowerBoundCalcuation.calculatePushesLowerbound();
		if(lowerBoundStartBoardPosition == LowerBoundCalculation.DEADLOCK) {
			return null;
		}

		minimumSolutionPathLength = lowerBoundStartBoardPosition;

		// Die erste Iteration startet mit einer Lowerboundobergrenze = des Lowerbounds der Anfangsstellung
		maximumSolutionLengthCurrentIteration = lowerBoundStartBoardPosition;

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

		// Aktuelle Stellung als erreichte Stellung speichern und als durch die aktuelle
		// Iteration erreicht kennzeichnen.
		storeBoardPosition(currentBoardPosition, lowerBoundStartBoardPosition);
		currentBoardPosition.setMaximumSolutionLength((short) maximumSolutionLengthCurrentIteration);

		// Die Anfangsstellungen für die Statistik auch mitzählen
		boardPositionsCount++;

		// Zu Beginn stehen keine Kisten auf dem Spielfeld. Diese werden erst bei Beginn
		// der Suche gesetzt
		for(int boxNo = 0; boxNo < board.boxCount; boxNo++) {
			board.removeBox(board.boxData.getBoxPosition(boxNo));
		}

		// Startzeit merken
		long timeStampStart = System.currentTimeMillis();

		// Nach einer Lösung suchen
		while(isSolutionStillPossible == true) {

			// Grundsätzlich wird davon ausgegangen, dass keine Lösung mehr möglich ist. Erst
			// wenn eine NichtDeadlock-Stellung gefunden wird, wird diese Variable umgesetzt.
			isSolutionStillPossible = false;

		    // Lösung mit der aktuellen Lowerboundobergrenze suchen
		    isSolutionFound = forwardSearch();

		    // Falls eine Lösung gefunden wurde, wird die Suche beendet.
		    if(isSolutionFound == true) {
				break;
			}

		    // Es wurde keine Lösung gefunden. Die Obergrenze für den Lowerbound wird deshalb um
		    // 2 erhöht und die Suche erneut bei der Anfangsstellung begonnen.
			maximumSolutionLengthCurrentIteration+=2;
			minimumSolutionPathLength = longestSolutionPath = lowerBoundStartBoardPosition;
			storeBoardPosition(startBoardPosition, lowerBoundStartBoardPosition);

			// Info auf dem Bildschirm ausgeben (-> Infotext setzen)
			publish(
			Texts.getText("numberofpositions")+boardPositionsCount+", "+
			Texts.getText("searchdepth")+maximumSolutionLengthCurrentIteration);
		}

		// Info auf dem Bildschirm ausgeben (-> Infotext setzen)
		if(isSolutionFound == true) {
			publish(
			Texts.getText("solved") +
			Texts.getText("pushes") + ": " + solutionBoardPosition.getPushesCount() + ", "+
			Texts.getText("numberofpositions") + boardPositionsCount);
		} else {
			publish(Texts.getText("solver.noSolutionFound"));
		}

		if(Debug.isDebugModeActivated) {
			System.out.println("===================================");
			System.out.println("Solution found: " + isSolutionFound);
			if(isSolutionFound == true) {
				System.out.println("Pushes: "+solutionBoardPosition.getPushesCount());
			}
			System.out.println("Number of no-deadlockpositions: "+boardPositionsCount);
			System.out.println("Total positions: "+positionStorage.getNumberOfStoredBoardPositions());
			System.out.println("Searchtime: "+(System.currentTimeMillis()-timeStampStart));
		}

		// Restore the start board position.
	    board.setBoardPosition(startBoardPosition);

		// Falls keine Lösung gefunden wurde, die Stellungen im Speicher löschen und
		// zurück springen.
		if(isSolutionFound == false) {
			positionStorage.clear();
			return null;
		}

		// In dieser ArrayList werden alle Pushes der Lösung abgelegt, um die Lösung Push für Push
		// durchgehen zu können
		ArrayList<IBoardPosition> pushes = new ArrayList<IBoardPosition>(solutionBoardPosition.getPushesCount());

		// Es werden alle Stellungen rückwärts abgegangen, die bei der Suche erreicht wurden
	    for(IBoardPosition boardPosition = solutionBoardPosition; boardPosition.getPrecedingBoardPosition() !=  null; boardPosition = boardPosition.getPrecedingBoardPosition()) {
	        if(boardPosition.getBoxNo() != NO_BOX_PUSHED) {
				pushes.add(0, boardPosition);
			}
	    }

	    // Restore the start board position.
	    board.setBoardPosition(startBoardPosition);

		// Der aktuelle Index in der History muss gemerkt werden, da der Benutzer genau
		// hier wieder starten soll. Alle Bewegungen, die jetzt eingefügt werden, sollen also
		// "in der Zukunft" liegen.
		int currentIndex = application.movesHistory.getCurrentMovementNo();

		// Alle Verschiebungen in die History eintragen.
		for(IBoardPosition boardPosition : pushes) {
			int pushedBoxNo = boardPosition.getBoxNo();
			int direction 	= boardPosition.getDirection();
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
        if(Debug.debugShowHashTableStatistic == true) {
			positionStorage.printStatisticDebug();
		}

		// Daten aus der Hashtable wieder löschen, damit der Speicher frei wird
		positionStorage.clear();

		// Die Lösung ein wenig optimieren. Dabei werden auch die Spielerbewegungen in die History eingetragen.
		optimizeSolution();

		// Create the new solution.
		Solution newSolution = new Solution(application.movesHistory.getLURDFromHistoryTotal());
		newSolution.name = solutionByMeNow();

		return newSolution;
	}


	/**
	 * Generates all possible legal configurations by pushing boxes.
	 * Each generated configuration is stored in the hash table.
	 * Had the configuration already been reached by pushing, it is skipped.
	 * In the end we return the success of the search.
	 *
	 * @return <code>true</code> if a solution is found, and
	 *        <code>false</code> if no sultion has been found
	 */
	@Override
	protected final boolean forwardSearch() {

		// Nimmt eine Kistenposition auf
		int boxPosition;

		// Nimmt die mögliche neue Kistenposition auf
		int newBoxPosition = 0;

		// Stellungobjekt, dass die aktuelle Stellung aufnimmt
		IBoardPositionIterative currentBoardPosition;

		// This board position is not an iterative one because it must hold references to "BoardPosition"s from the goal room analysis method.
		IBoardPosition boardPositionToBeAnalyzed;

		// Nimmt den Status einer Stellung auf
		IBoardPosition oldBoardPosition;

		// Nimmt den aktuellen Lowerbound einer Stellung auf
		int currentBoardPositionLowerbound = 0;

		// In diesem Bitarray sind die mit den Kistennummern korrespondierenden Bits gesetzt,
		// wenn diese Kiste für weitere Pushes relevant ist.
		boolean[] relevantBoxes = null;


		// Die Stellung mit der geringsten geschätzten Lösungspfadlänge weiter untersuchen
		while((boardPositionToBeAnalyzed = getBestBoardPosition()) != null && isCancelled() == false) {

			// Das Spielfeld mit der aktuellen Stellung besetzen
			board.setBoardPosition(boardPositionToBeAnalyzed);

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
			for (int boxCounter = -1, boxNo; boxCounter < board.boxCount; boxCounter++) {

				// Tests haben ergeben, dass eine Lösung schneller gefunden wird, wenn die zuletzt
				// verschobene Kiste sofort wieder verschoben wird. Deshalb wird zu Beginn
				// immer die Kiste, die im letzten Zug schon verschoben wurde erneut verschoben.
			    // Damit kommen diese Stellungen zuerst in die Queue und werden erst nach den
			    // Stellungen, die durch Verschiebungen der anderen Kisten erzeugt werden konnten
			    // wieder herausgeholt ... dies scheint tatsächlich effektiver zu sein ...
			    // Deshalb ist es auch nicht sinnvoll die Stellungen, die durch die zuletzt verschobene
			    // Kiste erzeugt werden können als letztes in die Queue zu schieben. Auf diese Weise
			    // würde die Lösung zwar automatisch ein kontinuierliches Veschieben der gleichen
			    // Kiste bevorzugen (was oft weniger Moves zur Folge hat). Denn dann wären mehr
			    // Stellungen und auch mehr Zeit bis zur Lösung nötig.
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
						boxCounter = board.goalsCount;
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

					// Falls die Kiste nicht in die gewünschte Richtung verschoben werden kann,
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
					currentBoardPosition = new RelativeBoardPositionIterative(board, boxNo, direction, boardPositionToBeAnalyzed);

					// Prüfen, ob diese Stellung schon einmal erreicht wurde, indem versucht
					// wird sie aus dem Stellungsspeicher zu lesen.
					oldBoardPosition = positionStorage.getBoardPosition(currentBoardPosition);

					// Falls die Stellung bereits in der Hashtable war muss geprüft werden, in welcher
					// Iteration sie gespeichert wurde.
					if(oldBoardPosition != null) {

						// Push der Kiste rückgängig machen. Der Spieler wird sowieso beim nächsten
						// Aufruf wieder umgesetzt. Dies muss hier also nicht extra geschehen.
						board.pushBoxUndo(newBoxPosition, boxPosition);

						// Falls die aktuelle Stellung bereits vorher in dieser Iterationsebene erreicht wurde,
						// dann kann gleich die nächste Richtung probiert werden, da die Stellung doppelt ist.
						// Falls sie in einer früheren Iteration erreicht wurde, so ist es eine gültige Stellung,
						// von der aus weitere Stellungen gesucht werden müssen.
						if(((IBoardPositionIterative) oldBoardPosition).getMaximumSolutionLength() != maximumSolutionLengthCurrentIteration) {
						    // Stellung wurde in einer vorigen Iteration in die Hashtable eingetragen.
							// Sie wird nun als auch in dieser Iteration erreicht gekennzeichnet,
							// indem die aktelle Lowerboundgrenze in ihr gespeichert wird. Sie wird als
						    // weitere Ausgangsbasis in der Stellungsqueue gespeichert.
							// Damit zunächst die neuen Stellungen, die in dieser Iteration gefunden wurden
							// verarbeitet werden, werden die alten Stellungen mit einem höhren Lowerbound gespeichert als
						    // alle Stellungen, die in dieser Iteration neu gefunden werden.
						    ((IBoardPositionIterative)oldBoardPosition).setMaximumSolutionLength((short) maximumSolutionLengthCurrentIteration);
						    storeBoardPosition(oldBoardPosition, maximumSolutionLengthCurrentIteration - ((BoardPosition)oldBoardPosition).getPushesCount() + 2);
						}

						continue;
					}


					/*
					 * Die Stellung ist noch nicht in der Hashtable enthalten.
					 * Es handelt ich also um eine "neue" Stellung. Ihr Lowerbound muss deshalb erst
					 * noch berechnet werden, um ihn mit der aktuellen Höchstgrenze an Pushes für diese Iteration
					 * vergleichen zu können. Dadurch wird auch automatisch geprüft, ob sie eventuell eine
					 * Deadlockstellung ist (dann wäre der Lowerbound = UNENDLICH)
					 * Für die Stellungen, die bereits in der Hashtable gespeichert waren muss dies nicht getan
					 * werden, da sie bereits auf Deadlock geprüft wurden und auf jeden Fall eine kleinere
					 * Pushanzahl bis zum Ziel besitzen müssen als die aktuelle Obergrenze, denn ansonsten
					 * wären sie nicht schon in der Hashtable eingetragen.
					 */
					currentBoardPositionLowerbound = lowerBoundCalcuation.calculatePushesLowerBound(newBoxPosition);

					// Push der Kiste rückgängig machen. Der Spieler wird sowieso beim nächsten
					// Aufruf wieder umgesetzt. Dies muss hier also nicht extra geschehen.
					board.pushBoxUndo(newBoxPosition, boxPosition);

					// Solange noch NoDeadlockstellungen gefunden werden ist auch noch eine Lösung möglich
					if(currentBoardPositionLowerbound == LowerBoundCalculation.DEADLOCK) {
					    continue;
					}

					isSolutionStillPossible = true;

					// Falls der Lösungspfad für die aktuelle Stellung größer als die derzeitige Obergrenze
					// für diese Iteration ist, wird sofort die nächste Richtung untersucht
					if(currentBoardPositionLowerbound + currentBoardPosition.getPushesCount() > maximumSolutionLengthCurrentIteration) {
						continue;
					}

					// Falls eine Lösung gefunden wurde, wird die Stellung gemerkt und zurückgesprungen
					if(currentBoardPositionLowerbound == 0) {
						solutionBoardPosition = currentBoardPosition;
						return true;
					}

					// Anzahl aller bei der Lösungssuche erreichten NichtDeadlockstellungen errechnen.
					boardPositionsCount++;

					// Info auf dem Bildschirm ausgeben (-> Infotext setzen)
					if(boardPositionsCount%5000 == 0) {
						publish(Texts.getText("numberofpositions")+boardPositionsCount+", "+
								Texts.getText("searchdepth")+maximumSolutionLengthCurrentIteration);
					}

					// Kennzeichnen in welcher Iteration die Stellung erstellt wurde und speichern der Stellung.
					currentBoardPosition.setMaximumSolutionLength((short) maximumSolutionLengthCurrentIteration);
					positionStorage.storeBoardPosition(currentBoardPosition);

					storeBoardPosition(currentBoardPosition, currentBoardPositionLowerbound);
				}
			}
		}

		// No solution found in the current iteration / with the current maximum solutionlength
		return false;
	}

	/* (non-Javadoc)
	 * @see de.sokoban_online.jsoko.Solver.SolverAStar#getBestBoardPosition()
	 */
	@Override
	protected BoardPosition getBestBoardPosition(){

		// Nimmt die Liste aller Stellungen mit einer bestimmten Lösungspfadlänge auf
		LinkedList<IBoardPosition> boardPositionList;

		for(int solutionLength = minimumSolutionPathLength; solutionLength <= longestSolutionPath; solutionLength+=2) {

			// Liste der Stellungen mit der aktuellen Pfadlänge holen. Da sich die Pfadlänge
			// immer in 2er Schritten erhöht wird ein Pfad der Länge x an Stelle x/2 gespeichert.
			boardPositionList = reachedBoardPositions.get(solutionLength/2);

			if(boardPositionList.size() > 0){
			    minimumSolutionPathLength = solutionLength;

				// Return last added boardposition
	    		return (BoardPosition) boardPositionList.removeLast();
			}
		}

		return null;
	}
}