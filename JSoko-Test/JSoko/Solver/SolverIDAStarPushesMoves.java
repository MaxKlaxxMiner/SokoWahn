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
import de.sokoban_online.jsoko.board.Board;
import de.sokoban_online.jsoko.boardpositions.BoardPosition;
import de.sokoban_online.jsoko.boardpositions.IBoardPosition;
import de.sokoban_online.jsoko.boardpositions.IBoardPositionMoves;
import de.sokoban_online.jsoko.boardpositions.iterative.AbsoluteBoardPositionMovesIterative;
import de.sokoban_online.jsoko.boardpositions.iterative.IBoardPositionMovesIterative;
import de.sokoban_online.jsoko.boardpositions.iterative.RelativeBoardPositionMovesIterative;
import de.sokoban_online.jsoko.leveldata.solutions.Solution;
import de.sokoban_online.jsoko.pushesLowerBoundCalculation.LowerBoundCalculation;
import de.sokoban_online.jsoko.resourceHandling.Settings.SearchDirection;
import de.sokoban_online.jsoko.resourceHandling.Texts;
import de.sokoban_online.jsoko.utilities.Debug;


/**
 * This class implements a solver which solves a level push optimal with a minimal
 * number of moves.
 */
public class SolverIDAStarPushesMoves extends Solver {

    // Erreichbare Felder des Spielers mit Distanzangaben
    protected Board.PlayersReachableSquaresMoves playersReachableSquaresMoves;

    // Erreichbare Felder des Spielers mit Distanzangaben für die Tunnelmethode
    protected Board.PlayersReachableSquaresMoves playersReachableSquaresMovesTunnel;

	//
	// Instanzvariablen
	//
	// Hierdrin wird die Stellung übergeben, falls eine Lösungsstellung gefunden wurde
    protected IBoardPositionMoves solutionBoardPosition = null;

    // Array, in dem die während der Suche erreichten Stellungen gespeichert werden.
    protected ArrayList<LinkedList<IBoardPositionMoves>> boardPositionQueue;

	// Gibt an, wie lang der minimale Lösungspfad ist.
    protected int shortestSolutionPathLength = 0;

	// Längster Lösungspfad, der bislang analysiert wurde
    private int longestSolutionPathLength = 0;

    // Maximale Anzahl Pushes für eine Lösung in einer bestimmten Iteration.
    // Wird eine Stellung erreicht, die eine höhere Anzahl an Pushes besitzt als der
    // hier gespeicherte Wert, so wird sie zunächst verworfen.
    // Erst wenn die Suche keine neue Stellung mit dieser Lösungspfadlänge mehr findet wird
    // dieser Wert erhöht.
    private int maximumSolutionLengthCurrentIteration = 0;

	// Wenn alle gültigen Stellungen bereits analysiert wurden, ohne dass eine Lösung gefunden
	// wurde, so wird diese Variable entsprechend gesetzt, damit die Suche beendet wird.
    private boolean isSolutionStillPossible = true;


	/**
	 * Constructs an object for solving a level using the IDA*-Algorithm.
	 *
     * @param application Reference to the main object which holds all references
     * @param solverGUI reference to the GUI of this solver
     */
    public SolverIDAStarPushesMoves(JSoko application, SolverGUI solverGUI) {
        super(application, solverGUI);

        // Globale Objekte für die Ermittlung der Spielerdistanzen anlegen.
        playersReachableSquaresMoves          = board.new PlayersReachableSquaresMoves();
        playersReachableSquaresMovesTunnel 	  = board.new PlayersReachableSquaresMoves();
    }


    /**
	 * Versucht, die aktuelle im Spielfeldobjekt abgelegte Stellung zu lösen.
	 */
	@Override
	public Solution searchSolution() {

		// Anzahl Stellungen bis zur Lösung
		boardPositionsCount = 0;

		// Hierdrin wird die Anfangsstellung gespeichert, um bei einer Erhöhung
		// des Lowerbounds immer wieder bei dieser Stellung beginnen zu können.
		IBoardPositionMovesIterative startBoardPosition;

		// Gibt an, wie groß der Lowerbound der Anfangsstellung ist
		int lowerBoundStartBoardPosition = 0;


		// Dies ist der Vector, der die Stellungen in verketteten Listen aufnimmt.
		// An Stelle 0 ist eine Liste mit allen Stellungen, die einen Lowerbound von 0 haben,
		// an Stelle 1 alle Stellungen mit einem Lowerbound von 1, ...
		// Standardmäßig wird davon ausgegangen, dass der Lösungspfad höchstens 20 Pushes lang ist.
		// (Ist er länger, wird die Liste automatisch verlängert -> siehe "storeStellung)
		boardPositionQueue = new ArrayList<LinkedList<IBoardPositionMoves>>(20);

		// Die Suche mit der aktuellen Stellung starten lassen
		IBoardPositionMovesIterative currentBoardPosition = startBoardPosition = new AbsoluteBoardPositionMovesIterative(board);
		currentBoardPosition.setSearchDirection(SearchDirection.FORWARD);

		// Pushes lower bound der Anfangsstellung berechnen. Dieser Wert ist auch gleichzeitig die
		// Mindestlänge des Lösungspfades. Von einer alten Lösungssuche könnten noch geblockte
		// Kisten vorhanden sein.
        board.boxData.setAllBoxesNotFrozen();
		lowerBoundStartBoardPosition = lowerBoundCalcuation.calculatePushesLowerbound();
		shortestSolutionPathLength = lowerBoundStartBoardPosition;

		// Falls bereits die Anfangsstellung unlösbar ist kann gleich "unlösbar" ausgegeben
		// und die Suche beendet werden.
		if(lowerBoundStartBoardPosition == LowerBoundCalculation.DEADLOCK) {
			return null;
		}

		// Während der Suche können Stellungen auftreten, die einen kürzeren Lösungspfad haben
		// als die Anfangsstellung (weil Penaltys wegfallen können!)
		// Deswegen darf der Lowerbound der Anfangsstellung nicht als minimale Lösungspfadlänge
		// genommen werden! Die minimale Lösungspfadlänge wird zumindest auf die richtige
		// Parität gesetzt, wenn schon nicht der Lowerbound direkt gesetzt werden kann :-)
		shortestSolutionPathLength = lowerBoundStartBoardPosition%2;

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
			positionStorage.storeBoardPosition(currentBoardPosition);
		}

		// Aktuelle Stellung als erreichte Stellung speichern und als durch die aktuelle
		// Iteration erreicht kennzeichnen.
		currentBoardPosition.setMaximumSolutionLength((short) maximumSolutionLengthCurrentIteration);
		storeBoardPosition(currentBoardPosition);

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

		    // Dependent on the solver type the respective method is called.
            if(this instanceof SolverAStarPushesMoves) {
				((SolverAStarPushesMoves) this).forwardSearch();
			} else
                if(this instanceof SolverAStarMovesPushes) {
					((SolverAStarMovesPushes) this).forwardSearch();
				} else {
					// Search a solution with the current maximum solution path length.
                    forwardSearch();
				}

		    // Falls eine Lösung gefunden wurde, wird die Suche beendet.
		    if(solutionBoardPosition != null) {
				break;
			}

		    // Es wurde keine Lösung gefunden. Die Obergrenze für den Lowerbound wird deshalb um
		    // 2 erhöht und die Suche erneut bei der Anfangsstellung begonnen.
			maximumSolutionLengthCurrentIteration+=2;

			// Die Lösungspfade werden mit 0 initialisiert (nicht mit Lowerbound, denn der Movespfad
			// beginnt bei Länge 0)
			shortestSolutionPathLength = longestSolutionPathLength = 0;
			storeBoardPosition(startBoardPosition);

			// Info auf dem Bildschirm ausgeben (-> Infotext setzen)
			publish(
			Texts.getText("numberofpositions")+boardPositionsCount+", "+
			Texts.getText("searchdepth")+maximumSolutionLengthCurrentIteration);
		}

		// Info auf dem Bildschirm ausgeben (-> Infotext setzen)
		if(solutionBoardPosition != null) {
			publish(
			Texts.getText("solved") +
            Texts.getText("moves")  + ": " + solutionBoardPosition.getTotalMovesCount() + ", " +
            Texts.getText("pushes") + ": " + solutionBoardPosition.getPushesCount());
		} else {
			publish(Texts.getText("solver.noSolutionFound"));
		}

		if(Debug.isDebugModeActivated) {
			System.out.println("===================================");
			System.out.println("Solution found: " + (solutionBoardPosition != null));
			if(solutionBoardPosition != null) {
                System.out.println("Moves: "+solutionBoardPosition.getTotalMovesCount());
                System.out.println("Pushes: "+solutionBoardPosition.getPushesCount());
            }
			System.out.println("Number of no-deadlockpositions: "+boardPositionsCount);
			System.out.println("Total positions: "+positionStorage.getNumberOfStoredBoardPositions());
			System.out.println("Searchtime: "+(System.currentTimeMillis()-timeStampStart));
		}

		// Falls keine Lösung gefunden wurde, die Stellungen im Speicher löschen und
		// zurück springen.
		if(solutionBoardPosition == null){
			positionStorage.clear();

		    /*
		     * Die Anfangsstellung auf dem Spielfeld setzen. Da bei dem Stellungsobjekt die Spieler-
		     * position "links-oben" gespeichert wurde und nicht die tatsächliche Anfangsstellung
		     * wurde die Originalspielerposition in einer extra Variablen gespeichert.
		     */
		    board.setBoardPosition(startBoardPosition);

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

		/*
	     * Die Anfangsstellung auf dem Spielfeld setzen. Da bei dem Stellungsobjekt die Spieler-
	     * position "links-oben" gespeichert wurde und nicht die tatsächliche Anfangsstellung
	     * wurde die Originalspielerposition in einer extra Variablen gespeichert.
	     */
	    board.setBoardPosition(startBoardPosition);

		// Der aktuelle Index in der History muss gemerkt werden, da der Benutzer genau
		// hier wieder starten soll. Alle Bewegungen, die jetzt eingefügt werden, sollen also
		// "in der Zukunft" liegen.
		int currentIndex = application.movesHistory.getCurrentMovementNo();

		for(int index = 0; index < pushes.size(); index++) {

			BoardPosition boardPosition = (BoardPosition) pushes.get(index);

			int pushedBoxNo 	  = boardPosition.getBoxNo();
			int boxPosition       = boardPosition.getPositions()[pushedBoxNo];
			int direction 	      = boardPosition.getDirection();
			int boxTargetPosition = 0;
			int boxStartPosition  = 0;

			// Kistenposition vor der Verschiebung ermitteln
			boxTargetPosition = boxPosition;
			boxStartPosition  = boxPosition - offset[direction];

			// Spielerpfad auf das Feld neben der Kiste ermitteln
			int[] playerPath = board.playerPath.getPathTo(2*boxStartPosition-boxTargetPosition);

			// Kiste versetzen
			board.removeBox(boxStartPosition);
			board.setBoxWithNo(board.getBoxNo(boxStartPosition), boxTargetPosition);

			// Alle Spielerbewegungen ausführen und in die History eintragen.
            for(int moveNo = 1; moveNo < playerPath.length; moveNo++) {

                board.playerPosition = playerPath[moveNo];

                // Bewegungsrichtung ermitteln
                application.movesHistory.addPlayerMove(
                		board.getMoveDirectionNumber(playerPath[moveNo-1], playerPath[moveNo])
                );
            }

            // Die letzte Bewegung des Spielers wird zusammen mit der verschobenen Kiste gespeichert.
            board.playerPosition = boxStartPosition;
            int movementDirection = board.getMoveDirectionNumber(boxStartPosition, boxTargetPosition);
            application.movesHistory.addMovement(movementDirection, board.getBoxNo(boxStartPosition));
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

		// Create the new solution.
		Solution newSolution = new Solution(application.movesHistory.getLURDFromHistoryTotal());
		newSolution.name = solutionByMeNow();

		return newSolution;
	}


	/**
	 * Erzeugt durch Verschieben alle möglichen gültigen Stellungen. Jede erzeugte Stellung wird in
	 * der Hashtable abgespeichert. Wurde sie bereits vorher einmal durch
	 * Verschieben erreicht, so wird sie übersprungen.
	 * Am Ende wird zurückgegeben wie das Level gelöst werden kann.
	 */
	 final private void forwardSearch() {
		// Nimmt eine Kistenposition auf
		int boxPosition;

		// Nimmt die mögliche neue Kistenposition auf
		int newBoxPosition = 0;

		// Stellungobjekt, dass die aktuelle Stellung aufnimmt
		IBoardPositionMovesIterative currentBoardPositionWithMoves;

		// Die jeweils gerade zu untersuchende Stellung aus der Queue
		IBoardPositionMovesIterative boardPositionToBeAnalyzed;

		// Objekt, um nicht jedes Mal einen Cast machen zu müssen
		IBoardPositionMovesIterative oldBoardPositionWithMoves;

		// Nimmt den Status einer Stellung auf
		IBoardPosition oldBoardPosition;

		// Nimmt den aktuellen Lowerbound einer Stellung auf
		int currentBoardPositionLowerbound = 0;

		// Anzahl Moves, die der Spieler bis zu einer Stellung gegangen ist.
		short numberOfMovesSoFar = 0;

		// Anzahl an Moves der bisher besten gefundenen Lösung
		int numberOfMovesBestSolution = Short.MAX_VALUE;

		// Nimmt die Kistennummer der verschobenen Kiste auf
		int pushedBoxNo = 0;

		//
		// Verarbeitungslogik
		//
		// Die Stellung mit der geringsten geschätzten Lösungspfadlänge weiter untersuchen
		while((boardPositionToBeAnalyzed = (IBoardPositionMovesIterative) getBestBoardPosition()) != null && isCancelled() == false) {

			// Das Spielfeld mit der aktuellen Stellung besetzen
			board.setBoardPosition(boardPositionToBeAnalyzed);

			// Erreichbare Felder des Spielers ermitteln. Da diese Felder auch nach der Deadlock-
			// prüfung noch gebraucht werden, werden sie in einem eigenen Objekt berechnet.
			playersReachableSquaresMoves.update();

			// Get number of last pushed box.
			pushedBoxNo = boardPositionToBeAnalyzed.getBoxNo();

            // If no box has been pushed the pushed box number is set to -1, so the tunnel detection isn't performed.
            if(pushedBoxNo == NO_BOX_PUSHED) {
                pushedBoxNo = -1;
            }

			// Nun muss geprüft werden, welche Kisten in welche Richtungen verschoben werden können
			// -> welche neuen Stellungen können erzeugt werden
			for(int boxCounter = -1, boxNo; boxCounter < board.boxCount; boxCounter++) {

				// Tests haben ergeben, dass eine Lösung schneller gefunden wird, wenn die zuletzt
				// verschobene Kiste sofort wieder verschoben wird. Deshalb wird zu Beginn
				// immer die Kiste, die im letzten Zug schon verschoben wurde erneut verschoben.
			    // Damit kommen diese Stellungen zuerst in die Queue und werden erst nach den
			    // Stellungen, die durch Verschiebungen der anderen Kisten erzeugt werden konnten
			    // wieder herausgeholt.
				if(boxCounter == pushedBoxNo) {
					continue;
				}

				// Die erste Kiste ist immer die im letzten Zug verschobene Kiste, es sei denn
   			    // es gibt keinen letzten Zug (Anfangsstellung = AbsoluteStellung = gibt -1 zurück)
				if(boxCounter == -1) {
					boxNo = pushedBoxNo;

					// Falls sich die verschobene Kiste in einem Tunnel befindet brauchen für diesen
					// Push nur Verschiebungen dieser Kiste geprüft werden!
			        // (Es kann nur die verschobene Kiste in einem Tunnel stehen, da alle anderen
			        // ja schon vorher weitergeschoben worden wären!)
			        if(isBoxInATunnel(pushedBoxNo, boardPositionToBeAnalyzed.getDirection())) {
			            boxCounter = board.goalsCount;
			        }
				} else {
					boxNo = boxCounter;
				}

				// Kistenposition holen
				boxPosition = board.boxData.getBoxPosition(boxNo);

				// Verschieben in jede Richtung prüfen
				for(int direction = 0; direction < 4; direction++) {
					// Mögliche neue Position der Kiste errechnen
					newBoxPosition = boxPosition + offset[direction];

					// Falls die Kiste nicht in die gewünschte Richtung verschoben werden kann,
					// sofort die nächste Richtung probieren (kommt Spieler auf richtige Seite
					// und ist Zielfeld kein simple Deadlockfeld wird geprüft)
					if(playersReachableSquaresMoves.isSquareReachable(boxPosition - offset[direction]) == false
					   || board.isAccessibleBox(newBoxPosition) == false) {
						continue;
					}

					// Push durchführen und den Spieler auch auf das alte Kistenfeld setzen
					board.pushBox(boxPosition, newBoxPosition);
					board.playerPosition = boxPosition;

					// Gesamtanzahl an Moves errechnen
					numberOfMovesSoFar = (short) (boardPositionToBeAnalyzed.getTotalMovesCount() + playersReachableSquaresMoves.getDistance(boxPosition - offset[direction]) + 1);

					// Falls die Stellung nicht weniger Moves als die bisher beste Lösung hat, so kann
					// sofort in der nächsten Richtung weitergesucht werden.
					if(numberOfMovesSoFar >= numberOfMovesBestSolution) {
					    board.pushBoxUndo(newBoxPosition, boxPosition);
					    continue;
					}

				    // Objekt der aktuellen Stellung erzeugen (mit Referenz zur vorigen Stellung)
					currentBoardPositionWithMoves = new RelativeBoardPositionMovesIterative(board, boxNo, direction, boardPositionToBeAnalyzed);

					// Prüfen, ob diese Stellung schon einmal erreicht wurde, indem versucht
					// wird sie aus dem Stellungsspeicher zu lesen.
					oldBoardPosition = positionStorage.getBoardPosition(currentBoardPositionWithMoves);

					// Falls die Stellung bereits in der Hashtable war muss geprüft werden, in welcher
					// Iteration sie gespeichert wurde.
					if(oldBoardPosition instanceof IBoardPositionMovesIterative) {

					    // Um wenigers Casts machen zu müssen.
					    oldBoardPositionWithMoves = (IBoardPositionMovesIterative) oldBoardPosition;

					    int numberOfPushesOldBoardPosition = oldBoardPositionWithMoves.getPushesCount();

					    // Falls die Stellung mit mehr Pushes erreicht wurde als vorher, so wird sofort mit der
					    // nächsten weitergemacht. Dies kann passieren, da die Stellungen mit der geringsten
					    // Movesanzahl vorgezogen werden und es somit möglich ist, dass eine Stellung mit mehr Pushes
					    // aber weniger Moves erreicht wird.
					    if(numberOfPushesOldBoardPosition < currentBoardPositionWithMoves.getPushesCount()) {
					        board.pushBoxUndo(newBoxPosition, boxPosition);
						    continue;
					    }

						// Falls sie in einer früheren Iteration erreicht wurde, so ist es eine gültige Stellung,
						// von der aus weitere Stellungen gesucht werden müssen.
						if(oldBoardPositionWithMoves.getMaximumSolutionLength() < maximumSolutionLengthCurrentIteration) {
						    if(oldBoardPositionWithMoves.getTotalMovesCount() <= numberOfMovesSoFar) {
							    // Die Stellung wurde in einer vorigen Iteration in die Hashtable eingetragen.
								// Sie wird nun als auch als in dieser Iteration erreicht gekennzeichnet,
								// indem die aktelle Lowerboundgrenze in ihr gespeichert wird. Sie wird als
							    // weitere Ausgangsbasis in der Stellungsqueue gespeichert.
						    	oldBoardPositionWithMoves.setMaximumSolutionLength((short) maximumSolutionLengthCurrentIteration);
							    storeBoardPosition(oldBoardPositionWithMoves);
						    }
						    else {
								// Lösungspfadlänge für Moves und Iterationsgrenze in der Stellung speichern.
						        currentBoardPositionWithMoves.setMovesCount(numberOfMovesSoFar);
								currentBoardPositionWithMoves.setMaximumSolutionLength((short) maximumSolutionLengthCurrentIteration);

								// Stellung für die weitere Suche eintragen
								storeBoardPosition(currentBoardPositionWithMoves);
						    }
						    // Push der Kiste rückgängig machen. Der Spieler wird sowieso beim nächsten
							// Aufruf wieder umgesetzt. Dies muss hier also nicht extra geschehen.
							board.pushBoxUndo(newBoxPosition, boxPosition);
						    continue;
						}

						// Die aktuelle Stellung wurde bereits vorher in dieser Iterationsebene erreicht.
						// Falls sie mit mehr oder gleich vielen Moves erreicht wurde kann sofort mit der
						// nächsten Stellung weitergemacht werden.
						// Falls die Stellung mit weniger Moves erreicht wurde, so muss die neue Stellung
						// als neue Ausgangsbasis gespeichert werden.
						if(numberOfMovesSoFar < oldBoardPositionWithMoves.getTotalMovesCount()) {

							// Die Stellung wurde in der aktuellen Iteration bereits schon einmal erreicht,
						    // allerdings nun mit weniger Moves:
							// Moveslösungspfadlänge in der Stellung speichern.
							// Außerdem die Iterationsgrenze in der Stellung speichern.
							currentBoardPositionWithMoves.setMovesCount(numberOfMovesSoFar);
							currentBoardPositionWithMoves.setMaximumSolutionLength((short) maximumSolutionLengthCurrentIteration);

							// Stellung in der Hashtable durch die bessere Stellung ersetzen.
							positionStorage.storeBoardPosition(currentBoardPositionWithMoves);

							// Ausgehend von dieser Stellung weitersuchen
							storeBoardPosition(currentBoardPositionWithMoves);
						}

						// Push der Kiste rückgängig machen. Der Spieler wird sowieso beim nächsten
						// Aufruf wieder umgesetzt. Dies muss hier also nicht extra geschehen.
					    board.pushBoxUndo(newBoxPosition, boxPosition);
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
					// für diese Iteration ist oder aber die zurückgelegten Moves + der Lowerbound größer
					// als die Anzahl an Moves der derzeit besten Lösung sind, wird sofort die nächste
					// Richtung untersucht.
					if(currentBoardPositionLowerbound + currentBoardPositionWithMoves.getPushesCount() > maximumSolutionLengthCurrentIteration ||
					   numberOfMovesSoFar + currentBoardPositionLowerbound >= numberOfMovesBestSolution) {
						continue;
					}

					// Anzahl der bisher durchgeführten Moves in der Stellung speichern.
					// Außerdem die Iterationsgrenze in der Stellung speichern.
					currentBoardPositionWithMoves.setMovesCount(numberOfMovesSoFar);
					currentBoardPositionWithMoves.setMaximumSolutionLength((short) maximumSolutionLengthCurrentIteration);

					// Falls eine Lösung gefunden wurde, wird die Anzahl an Moves und die Stellung
					// selbst als Lösung gespeichert. Die Lösung muss aber nicht zwangsläufig
					// die moveminimale sein.
					if(currentBoardPositionLowerbound == 0) {
					    numberOfMovesBestSolution = numberOfMovesSoFar;
						solutionBoardPosition = currentBoardPositionWithMoves;
						if(Debug.isDebugModeActivated) {
							System.out.println( "Solution Found " + "Moves/Pushes: "
									           +       currentBoardPositionWithMoves.getTotalMovesCount()
									           + "/" + currentBoardPositionWithMoves.getPushesCount());
						}
						continue;
					}

					// Anzahl aller bei der Lösungssuche erreichten NichtDeadlockstellungen errechnen.
					boardPositionsCount++;

					// Display info to screen (as info text)
					if(boardPositionsCount % 3000 == 0) {
						publish(  Texts.getText("numberofpositions") + boardPositionsCount + ", "
								+ Texts.getText("searchdepth") + maximumSolutionLengthCurrentIteration);
					}

					// Stellung in der Hashtable speichern.
					positionStorage.storeBoardPosition(currentBoardPositionWithMoves);

					storeBoardPosition(currentBoardPositionWithMoves);
				}
			}
		}
	}


	/**
	 * Returns the board position with the shortest determined solution path length.
	 *
     * @return board position with the shortest determined solution path length.
     */
    final protected IBoardPositionMoves getBestBoardPosition(){

		// Nimmt die Liste aller Stellungen mit einer bestimmten Lösungspfadlänge auf
		LinkedList<IBoardPositionMoves> boardPositionList;

		for(int solutionLength = shortestSolutionPathLength; solutionLength <= longestSolutionPathLength; solutionLength++) {

			// Liste der Stellungen mit der aktuellen Pfadlänge holen.
			boardPositionList = boardPositionQueue.get(solutionLength);

			if(boardPositionList.size() > 0){
			    shortestSolutionPathLength = solutionLength;

				// Die zuletzt eingefügte Stellung zurückgeben
	    		return boardPositionList.removeLast();
			}
		}

		return null;
	}


	/**
	 * Stores the board configuration with the estimated solution path length.
	 * The length is computed from the moves used up to here plus the exstimated
	 * number of pushes to the end sonfiguration.
	 *
	 * @param boardPosition	 configuration to store
	 */
    final protected void storeBoardPosition(IBoardPositionMoves boardPosition) {

 		// Gesamtpfadlänge für die Stellung errechnen
 		int pathLengthMoves = boardPosition.getTotalMovesCount();

 		// Wenn die Position größer als die Liste ist, wird die Liste erweitert
 		if(pathLengthMoves >= boardPositionQueue.size()) {
			boardPositionQueue.ensureCapacity(pathLengthMoves+1);
			for(int i=pathLengthMoves+1-boardPositionQueue.size(); i>0; i--) {
				boardPositionQueue.add(new LinkedList<IBoardPositionMoves>());
			}
		}

		// Die Stellung an der zu ihrer Pfadlänge gehörenden Position speichern.
		boardPositionQueue.get(pathLengthMoves).add(boardPosition);

		// Zur Sicherheit, falls die Lowerboundberechnung doch einmal überschätzend war
		// (darf eigentlich nie der Fall sein)
		// Es kann aber sein, dass ein Penalty wegfällt und dadurch der Lowerbound
		// plötzlich zu niedrig angesetzt wird.
		if(pathLengthMoves < shortestSolutionPathLength) {
			shortestSolutionPathLength = pathLengthMoves;
		}

		// Längsten Eintrag merken. Diese Information gilt als obere Suchgrenze für die Methode
		// "getBesteStellung", damit die Methode nicht immer die komplette Liste durchsuchen muss.
		if(pathLengthMoves > longestSolutionPathLength) {
			longestSolutionPathLength = pathLengthMoves;
		}

    	// In debug mode the path to the current board position can be displayed.
    	if(Debug.isDisplayPathToCurrentBoardPositionActivated) {
    		debugShowPathToCurrentBoardPosition(boardPosition);
    	}
	}

    /**
     * Returns whether the box with the specified box number is "in a tunnel".
     * This is important to know during solution search, because in that case
     * that box can just be pushed, ignoring all other possible pushes of other boxes.
     * <p>
     * For this solver we consider a box in a tunnel, if:
     * <ul>
	 * <li>the box separates at least one area from the player
	 * <li>the side of the box opposite to the player is adjacent to the separated area
	 * <li>the only valid push of the box is to the square opposite to the player
	 * <li>the player distance to all boxes except the pushed one increases by the push
	 * <li>the box is not on a goal or there is at least one free goal square in the separated area
	 * </ul>
	 *<p>
	 * Example:      <pre>
	 * ##############
	 * -@$---------.#
	 * #####--$----.#
	 *     ########## </pre>
	 * This method is public only for debug purposes.
	 *
     * @param boxNo the number of the box
     * @param pushDirection	the direction the box has been pushed to
     * @return <code>true</code> if the box is in a tunnel,<br>
     * 		  <code>false</code> if the box is not in a tunnel
     */
    final public boolean isBoxInATunnel(int boxNo, int pushDirection) {

	    // Kistenposition
	    int boxPosition;

	    // Neue Kistenposition
	    int newBoxPosition;

	    // Das vom Spieler gesehen hinter der Kiste liegende Feld
	    int oppositeSquare;

	    // Number of goals without a box on them in a corral
	    int goalsWithoutBoxCountCorral = 0;

        // Get the current box position
	    boxPosition = board.boxData.getBoxPosition(boxNo);

	    // Determine the opposite axis of the push direction
        int oppositeAxisDirection = pushDirection == UP || pushDirection == DOWN ? RIGHT : UP;

        /* First let's get the following tunnels (and rotated versions):
         *
         * #$    #$#    $#
         * #@#   #@#   #@#
         */
        if(board.isGoal(boxPosition) == false &&
           board.isWall(boxPosition-offset[pushDirection]+offset[oppositeAxisDirection]) &&
           board.isWall(boxPosition-offset[pushDirection]-offset[oppositeAxisDirection]) &&
           (board.isWall(boxPosition+offset[oppositeAxisDirection]) || board.isWall(boxPosition-offset[oppositeAxisDirection]))) {
            return true;
        }

	    // Falls die Kiste nicht auf einem Corralerzwinger steht, so trennt sie keinen Bereich
	    // vom Spieler ab und es handelt sich nicht um einen Tunnel.
	    if(board.isCorralForcerSquare(boxPosition) == false) {
	    	return false;
	    }

		// Erreichbare Felder des Spielers ermitteln, wenn nur diese eine Kiste auf dem Feld ist
		board.setWall(boxPosition);
		board.playersReachableSquaresOnlyWalls.update();
		board.removeWall(boxPosition);

		// Position des vom Spieler aus gesehen hinter der Kiste liegenden Feldes ermitteln.
		oppositeSquare = 2*boxPosition-board.playerPosition;

		// Falls das dem Spieler gegenüberliegende Feld nicht in einem abgetrennten Bereich liegt,
		// handelt es sich nicht um einen "Tunnel".
		if(board.playersReachableSquaresOnlyWalls.isSquareReachable(oppositeSquare) == true) {
			return false;
		}

	    // Verschieben in alle Richtungen ausprobieren
		for(byte direction=0; direction<4; direction++) {
		    //	Position der Kiste berechnen, auf der sie stehen würde, wenn die Verschiebung
		    // in die durchgeführt worden wäre.
		    newBoxPosition = boxPosition + offset[direction];

		    // Prüfen, ob Spieler auf die eine Seite kommt und die andere Seite frei ist (= keine Mauer,
		    // nicht ".isBetretbar()", denn die Kisten sind ja noch auf dem Feld!) und es kein Deadlockfeld ist.
		    // Falls er es nicht kann, kann gleich in der nächsten Richtung geprüft werden.
		    if(board.isWallOrIllegalSquare(newBoxPosition) ||
		       board.playersReachableSquaresOnlyWalls.isSquareReachable(boxPosition - offset[direction]) == false) {
		    	continue;
		    }

		    // Die Kiste kann theoretisch auf die neue Position geschoben werden. Falls die Position
		    // nicht das gegenüberliegende Feld ist, befindet sich die Kiste nicht in einem "Tunnel".
		    if(newBoxPosition != oppositeSquare) {
		        return false;
		    }
		}

		// Count the number of goals and boxes in the corral.
        for(int position = board.firstRelevantSquare; position < board.lastRelevantSquare; position++) {
        	// Skip positions that are not in the corral
        	if(board.playersReachableSquaresOnlyWalls.isSquareReachable(position) == true ||
        	   position == boxPosition) {
        		continue;
        	}

        	// Count the goals without a box on them.
        	if(board.isGoal(position) == true) {
        		if(board.isBox(position) == false) {
        			goalsWithoutBoxCountCorral++;
        		}
        	}
        }

		/*
		 * Falls die Entfernung des Spielers zu einer Kiste, außer zur verschobenen Kiste,
		 * nicht größer geworden ist, so befindet sich die Kiste nicht in einem "Movestunnel".
		 */
        // Determine direction of the other axis
		byte oppositeDirection = 0;
		if(Math.abs(boxPosition-board.playerPosition) == 1) {	   // vertical
		    oppositeDirection = UP;
		} else {                                                   // horizontal
		    oppositeDirection = RIGHT;
		}

		// Falls sich auf beiden Seiten der Gegenachse Mauern befinden muss die Entfernung zu allen
		// anderen Kisten größer geworden sein und es muss keine vollständige Prüfung durchgeführt werden.
		if(board.isWall(board.playerPosition+offset[oppositeDirection]) == false ||
		   board.isWall(board.playerPosition-offset[oppositeDirection]) == false) {

		    // Vorige Stellung wiederherstellen, Spielerentfernungen ermitteln und danach aktuelle
		    // Stellung wiederherstellen.
			board.setBox(board.playerPosition);
			board.removeBox(boxPosition);
			playersReachableSquaresMovesTunnel.update(2*board.playerPosition-boxPosition);
			board.setBox(boxPosition);
			board.removeBox(board.playerPosition);

			// Falls die Entfernung zu einer anderen Kiste nach dem Verschieben geringer geworden oder
			// gleichgroß geblieben ist befindet sich die Kiste nicht in einem "Tunnel".
			for(int boxNoForDistanceCalculation=0; boxNoForDistanceCalculation<board.boxCount; boxNoForDistanceCalculation++) {
			    // Die Entfernung zur verschobenen Kiste ist irrelevant
			    if(boxNoForDistanceCalculation == boxNo) {
			        continue;
			    }

				int boxPosition2 = board.boxData.getBoxPosition(boxNoForDistanceCalculation);

				for(int directionOffset : offset) {
				    if(playersReachableSquaresMoves.getDistance(boxPosition2+directionOffset) != Board.UNREACHABLE &&
				       playersReachableSquaresMovesTunnel.getDistance(boxPosition2+directionOffset) >= playersReachableSquaresMoves.getDistance(boxPosition2+directionOffset)) {
				        return false;
				    }
			    }
			}
		}

		// It's a tunnel if the box isn't on a goal or there is at least one goal without a box
		// in the corral.
		if(board.isGoal(boxPosition) == false || goalsWithoutBoxCountCorral > 0) {
		    return true;
		}

		// The box isn't located in a tunnel
	    return false;
	}
}