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
import java.util.ListIterator;

import de.sokoban_online.jsoko.JSoko;
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
 * This class contains methods which try to solve a level,
 * using a "breadth first search".
 */
public final class SolverBFS extends Solver {

    // Drei Arrays anlegen, in dem alle erreichten Spielfeldstellungen gespeichert werden.
    // Eins für Vorwärtssuche, eins für Rückwärtssuche und eins für die aktuelle Suche
    IBoardPositionIterative[] boardPositionsForward;
    IBoardPositionIterative[] boardPositionsBackward;
    IBoardPositionIterative[] currentBoardPositions;

    // Hierdrin werden die RelativeStellungsobjekte gespeichert, wenn ein "Treffpunkt" der
    // beiden Richtungen gefunden wurde. Das eine Objekt enthält dann alle Stellungen der Vorwärts-
    // suche bis zum Treffpunkt, das andere die Stellungen der Rückwärtssuche bis zum Treffpunkt.
    IBoardPositionIterative boardPositionForward;
    IBoardPositionIterative boardPositionBackward;

    // Anzahl der Spielfeldsituationen, die in g_vorwärtsSuchePositionen bzw.
    // g_rückwärtsSuchePositionen gespeichert sind.
    int boardPositionsForwardSearchCount;
    int boardPositionsBackwardSearchCount;

    // Dieser Wert gibt die Anzahl der Positionen an, die maximal pro Zug gespeichert werden
    // können. Ist dieser Wert z.B. 50, so könnten von einer bestimmten Spielfeldsituation
    // ausgehend nur 50 verschiedene Züge der nächsten Zugmöglichkeit gespeichert werden,
    // obwohl mit dem nächsten Zug vielleicht 60 verschiedene Züge möglich wären.
    int maximumNumberOfBoardPositionsPerPush = 500000;

    // Lowerboundoberwert für die aktuelle Iteration
    // Wird eine Stellung erreicht, die einen höheren Lowerbound besitzt, als der
    // hier gespeicherte Wert, so wird sie zunächst verworfen.
    // Erst wenn eine Suchrichtung keine neue Stellung mit einem Lowerbound <= den hier
    // gespeichertem Lowerbound mehr findet, wird der Lowerbound erhöht.
    int maximumSolutionLengthCurrentIteration = 0;

	// Anzahl Verschiebungen bis zur Lösung je Suchrichtung
	int forwardPushesCount;
	int backwardPushesCount;

	// Wenn alle gültigen Stellungen bereits analysiert wurden, ohne dass eine Lösung gefunden
	// wurde, so wird diese Variable entsprechend gesetzt, damit die Suche beendet wird.
	boolean isSolutionStillPossible = true;


	/**
	 * Creates a breath first solver.
	 *
	 * @param application the reference to the main object holding all references
	 * @param solverGUI reference to the GUI of this solver
	 */
	public SolverBFS(JSoko application, SolverGUI solverGUI) {
		super(application, solverGUI);
	}

	/**
	 * Versucht, die aktuelle im Spielfeldobjekt abgelegte Stellung zu lösen.
	 */
	@Override
	public final Solution searchSolution() {

		// Zeigt an, ob eine Lösung gefunden wurde
		boolean isSolutionFound = false;

		// Anzahl Pushes der beiden Suchrichtungen
		forwardPushesCount  = 0;
		backwardPushesCount = 0;

		// Hierdrin wird die Anfangsstellung für die Vorwärtssuche gespeichert, um
		// bei einer Erhöhung des Lowerbounds immer wieder bei dieser Stellung beginnen zu können
		AbsoluteBoardPositionMovesIterative startBoardPosition;

		// Hierdrin werden die Anfangsstellungen der Rückwärtssuche gespeichert, um
		// bei einer Erhöhung des Lowerbounds immer wieder bei diesen Stellungen beginnen zu können
		IBoardPositionIterative[] startPositionsBackwardSearch;

		// Nimmt die Anzahl der Anfangsstellungen der Rückwärtssuche auf
		int startPositionsBackwardSearchCount;


		// Die Arrays, die die Stellungen aufnehmen anlegen
		boardPositionsForward  = new IBoardPositionIterative[maximumNumberOfBoardPositionsPerPush];
		boardPositionsBackward = new IBoardPositionIterative[maximumNumberOfBoardPositionsPerPush];
		currentBoardPositions  = new IBoardPositionIterative[maximumNumberOfBoardPositionsPerPush];
		boardPositionsForwardSearchCount  = 0;
		boardPositionsBackwardSearchCount = 0;

		// Die derzeitigen Kistenfelder als Zielfelder für die Rückwärtssuche setzen
		board.setGoalsBackwardsSearch();

		// Alle Stellungen die mehr Pushes bis zur Lösung benötigen als durch diesen
		// Wert vorgegeben wird, werden in dieser Iteration verworfen!
		// Anzahl Pushes bis zur Lösung = bisher benötigte Pushes + aktueller Lowerbound der Stellung
		// Falls die Vorwärtssuche aktiviert ist (>= 0) wird der Lowerbound mit dem Vorwärts lower bound
		// initialisiert, ansonsten mit dem Rückwärts lower bound
		// TODO: Für Rückwärts muss man den kleinsten Lowerbound aller möglichen Endstellungen ermitteln!

		board.boxData.setAllBoxesNotFrozen();
		boolean isDeadlock = deadlockDetection.isDeadlock();

		// Falls bereits die Anfangsstellung unlösbar ist kann gleich "unlösbar" ausgegeben
		// und die Suche beendet werden.
		if(isDeadlock) {
			return null;
		}

		// Calculate the pushes lower bound for solving the level.
		maximumSolutionLengthCurrentIteration = lowerBoundCalcuation.calculatePushesLowerbound();
		if(maximumSolutionLengthCurrentIteration == LowerBoundCalculation.DEADLOCK) {
			return null;
		}

		// Die Vorwärtssuche mit der aktuellen Stellung starten lassen
		IBoardPositionIterative currentBoardPosition = new AbsoluteBoardPositionIterative(board);
		startBoardPosition = new AbsoluteBoardPositionMovesIterative(board);

		// Aktuelle Stellung als durch die aktuelle Iteration erreicht kennzeichnen
		currentBoardPosition.setMaximumSolutionLength((short) maximumSolutionLengthCurrentIteration);

		/*
		 * Vorwärtsstellung speichern
		 */

		// Spielfeldsituation als durch die Vorwärtssuche erreicht kennzeichnen.
		// Als Richtung wird 0 mitgegeben, da eine Richtung mitgegeben werden muss.
		// Sie ist aber irrelevant, da in der Anfangsstellung ja keine Kiste verschoben wurde.
		// Als Kistennr wird der Index im Array der Anfangspositionen mitgegeben!
		currentBoardPosition.setSearchDirection(SearchDirection.FORWARD);
		positionStorage.storeBoardPosition(currentBoardPosition);

		boardPositionsForward[boardPositionsForwardSearchCount++] = currentBoardPosition;

		/*
		 * Rückwärtsstellungen speichern
		 */

		// Alle Kisten vom Feld nehmen, um die Anfangsstellung für die Rückwärtssuche zu erstellen
		board.removeAllBoxes();

		// Die erreichbaren Felder des Spielers ermitteln. Da keine Kisten auf
		// dem Feld sind, sind dies alle Felder, die überhaupt erreichbar sind.
		// Es wird das Klasseneigene Objekt genommen, da das globale Objekt im Spielfeld
		// bei der Instanziierung einer Stellung neu berrechnet = überschrieben wird.
		playersReachableSquares.update();

		// Alle Kisten auf Zielfelder stellen, um die Startposition für die Rückwärtssuche herzustellen.
		for(int position=board.firstRelevantSquare; position<board.lastRelevantSquare; position++) {
			// Falls es ein Zielfeld ist, wird die dem Zielfeld entsprechende Kiste darauf gesetzt
			if(board.isGoal(position) && board.isOuterSquareOrWall(position) == false) {
				board.boxData.setBoxPosition(board.getGoalNo(position), position);
				board.setBoxWithNo(board.getGoalNo(position), position);
			}
		}

		/*
		 * Alle Stellungen, in denen alle Kisten auf Zielfeldern stehen können
		 * sofort als durch die Rückwärtssuche erreicht gekennzeichnet werden.
		 * Dazu muss der Spieler in jeden abgeschlossenen Bereich gesetzt
		 * werden. Später prüfen, ob einige der Endpositionen vielleicht
		 * unmöglich sind!
		 */

		// Nun auch noch alle Stellungen speichern, wo die Kisten auch alle auf Zielfeldern stehen,
		// der Spieler aber in anderen abgeschlossenen Bereichen steht.
		// (Nur die Felder berücksichtigen, auf denen keine Kiste steht -> isBetretbar()
		// und die zum aktiven Feld gehören.
		for(int position=board.firstRelevantSquare; position<board.lastRelevantSquare;position++) {
			if(board.isAccessible(position) && playersReachableSquares.isSquareReachable(position)) {

				// Den abgeschlossenen Bereich demarkieren (Klasseneigenes Objekt, da bei der
				// Instanziierung der AbsolutenStellung das globale Feld in g_spielfeld
				// neu berechnet und damit die alten erreichbaren Felder überschrieben würden!
				playersReachableSquares.reduce(position);

				// Für die Instanziierung des Stellungobjekts ist es wichtig, dass die
				// Spielerposition korrekt gesetzt ist:
				board.playerPosition = position;

				// Die aktuelle Situation als durch die Rückwärtssuche sowohl im Rückwärtssucharray
				// als auch in der Hashtable als erreicht speichern
				currentBoardPosition = new AbsoluteBoardPositionIterative(board);

				// Anfangsstellung als erreicht markieren
				currentBoardPosition.setSearchDirection(SearchDirection.BACKWARD);
				positionStorage.storeBoardPosition(currentBoardPosition);

				// Rückwärtssuche mit dieser Stellung starten lassen
				boardPositionsBackward[boardPositionsBackwardSearchCount++] = currentBoardPosition;
			}
		}

        // Die Anfangsstellungen der Rückwärtssuche merken, um bei einer Erhöhung des Lowerbounds
	    // (neuer Iteration) immer wieder bei diesen Stellungen starten zu können.
		startPositionsBackwardSearch = new IBoardPositionIterative[boardPositionsBackwardSearchCount];
		for(int index = 0; index < boardPositionsBackwardSearchCount; index++) {
			startPositionsBackwardSearch[index] = boardPositionsBackward[index];
		}
		startPositionsBackwardSearchCount = boardPositionsBackwardSearchCount;

		// Falls die Startposition auch schon von der Vorwärtssuche erreicht wurde, ist die Startposition
		// auch gleich die Endposition -> Lösung gefunden
		// l_lösungGefunden = (l_statusDerStellung != null &&
		// l_statusDerStellung.intValue() == VORWÄRTSSUCHE);

		// Die Anfangsstellungen für die Statistik auch mitzählen (Rückwärts + eine Vorwärts)
		boardPositionsCount = boardPositionsBackwardSearchCount + 1;

		// Zu Beginn stehen keine Kisten auf dem Spielfeld. Diese werden erst bei Beginn
		// der Vorwärts- bzw. Rückwärtssuche gesetzt
		for(int boxNo = 0; boxNo < board.goalsCount; boxNo++) {
			board.removeBox(board.boxData.getBoxPosition(boxNo));
		}

		if(Debug.isDebugModeActivated) {
			System.out.println("Maximum Solutionlength= "+maximumSolutionLengthCurrentIteration);
		}

		long timeStampStart = System.currentTimeMillis();

		// Hauptschleife!
		// Hier werden alle im nächsten Zug erzeugbaren Spielfeld-
		// situation durch Vorwärtsschieben bzw. Rückwärtsziehen erzeugt und geprüft,
		// ob die Stellungen durch die jeweils andere Suchrichtung bereits erreicht wurden.
		while(isSolutionFound == false && isCancelled() == false) {

			// Falls bei der Vorwärts- oder Rückwärtssuche kein weiterer Zug mehr möglich
			// war oder aber es wurden in dieser Iteration schon die maximale Anzahl an Pushes
			// durchgeführt ohne eine Lösung zu finden, so muss eine neue Iteration mit erhöhter
			// Anzahl an maximalen Pushes gestartet werden.
			if(boardPositionsForwardSearchCount == 0 || boardPositionsBackwardSearchCount == 0 ||
			   forwardPushesCount + backwardPushesCount >= maximumSolutionLengthCurrentIteration) {

				// Eine Kiste die vom optimalen Pfad geschoben werden muss, benötigt auch noch
				// einen weiteren Push, um wieder auf den optimalen Pfad zu kommen!
				// Deshalb immer plus 2! Somit steht bereits am Anfang fest, ob der Lowerbound
				// ungerade oder gerade ist.
				maximumSolutionLengthCurrentIteration+=2;
				boardPositionsForward[0] = startBoardPosition;
				boardPositionsForwardSearchCount = 1;

				if(Debug.isDebugModeActivated) {
					System.out.println("Maximum Solutionlength new Iteration = "+maximumSolutionLengthCurrentIteration);
				}

				// Die Rückwärtssuche beginnt auch wieder bei den Anfangsstellungen
				for(int index = 0; index < startPositionsBackwardSearchCount; index++) {
					boardPositionsBackward[index] = startPositionsBackwardSearch[index];
				}
				boardPositionsBackwardSearchCount = startPositionsBackwardSearchCount;

				// Die Suche beginnt wieder bei 0 Pushes; und zwar für beide Richtungen!
				forwardPushesCount  = 0;
				backwardPushesCount = 0;

				// Falls in der letzten Iteration keine neue NoDeadlockstellung erreicht wurde oder
				// die maximale Suchtiefe bereits "unendlich" ist, ist keine Lösung mehr möglich.
				if(isSolutionStillPossible == false || maximumSolutionLengthCurrentIteration >= Integer.MAX_VALUE-3) {
					break;
				}

				// Grundsätzlich wird davon ausgegangen, dass keine Lösung mehr möglich ist. Erst
				// wenn eine Suchrichtung eine NichtDeadlock-Stellung findet, wird diese
				// Variable umgesetzt.
				isSolutionStillPossible = false;
			}

			// Immer dort weitersuchen, wo die wenigsten Zugmöglichkeiten im letzten Zug vorhanden
			// waren, da dies am schnellsten geht. Da die Rückwärtssuche wesentlich schneller geht,
			// wird erst weiter vorwärts gesucht, wenn die Rückwärtsstellungen mehr als doppelt so viele
			// sind, wie es Vorwärtsstellungen sind -> doch wieder geändert, da Speicherplatz wichtiger
			// ist als Schnelligkeit, deswegen wird dort weitergesucht, wo die wenigsten Stellungen
			// durchsucht werden müssen!
			// g_bevorzugteSuchrichtung ist ein Wert der normalerweise 0 ist. Wenn aber eine Suchrichtung
			// bevorzugt werden soll, dann kann der Benutzer per Klick im Menü dieser Variablen einen bestimmten
			// Wert geben (entweder einen positiven oder einen negativen) und damit die Bevorzugung einer
			// Suchrichtung steuern.
			if(application.preferredSearchDirection + boardPositionsForwardSearchCount < boardPositionsBackwardSearchCount) {
				isSolutionFound = forwardSearch();

				if(Debug.isDebugModeActivated) {
					System.out.println("Pushes: " + (forwardPushesCount+backwardPushesCount) +
				    ", total positions: " + boardPositionsCount+" forward "+boardPositionsForwardSearchCount);
				}
			}
			else {
			    isSolutionFound = backwardSearch();

			    if(Debug.isDebugModeActivated) {
			    	System.out.println("Pushes: " + (forwardPushesCount+backwardPushesCount)+
					", total positions: " + boardPositionsCount+ " backward " + boardPositionsBackwardSearchCount);
			    }
			}

			// Info auf dem Bildschirm ausgeben (-> Infotext setzen) (Pushes: ..., Anzahl Stellungen: ..., Suchtiefe: ...)
			publish(
			Texts.getText("pushes")+": "+(forwardPushesCount+backwardPushesCount)+", "+
			Texts.getText("numberofpositions")+boardPositionsCount+", "+
			Texts.getText("searchdepth")+maximumSolutionLengthCurrentIteration);
		}

		// Die Anzahl der Pushes kann nicht aus g_anzahlVorwärtsPushes+g_anzahlRückwärtsPushes errechnet
		// werden, da es sein kann, dass in der letzten Iteration nicht alle Pushes ausgeführt
		// werden mussten. Beispiel:
		// 1. Iteration: 10 Pushebenen vorwärts- und 15 Pushebenen rückwärts durchsucht
		// 2. Iteration: Aufgrund der höheren maximalen Pushesanzahl werden jetzt nur Rückwärts-
		// stellungen erzeugt (zur Vereinfachung mal angenommen).
		// Es wird nun die 18. Pushebene für die Rückwärtssuche analysiert. Für die Vorwärtssuche
		// wurde (in dieser Iteration!) noch keine einzige Pushebene analysiert.
		// Nun wird in Pushebene 18 der Rückwärtssuche eine Stellung aus der Pushebene 9 der Vorwärts-
		// suche erzeugt. Lösung gefunden mit 27 Pushes.
		// g_anzahlVorwärtsPushes+g_anzahlRückwärtsPushes würde aber 18 ergeben!

		// Info auf dem Bildschirm ausgeben (-> Infotext setzen)
		if(isSolutionFound == true) {
			publish(
			Texts.getText("solved")+
			Texts.getText("pushes")+": "+(boardPositionForward.getPushesCount()+boardPositionBackward.getPushesCount())+", "+
			Texts.getText("numberofpositions")+boardPositionsCount);
		} else {
			publish(Texts.getText("solver.noSolutionFound"));
		}

		if(Debug.isDebugModeActivated) {
			System.out.println("===================================");
			System.out.println("Solution found: " + isSolutionFound);
			if(isSolutionFound == true) {
				System.out.println("Pushes: "+(boardPositionForward.getPushesCount()+boardPositionBackward.getPushesCount()));
			}
			System.out.println("Number of no-deadlockpositions: "+boardPositionsCount);
			System.out.println("Total positions: "+positionStorage.getNumberOfStoredBoardPositions());
			System.out.println("Searchtime: "+(System.currentTimeMillis()-timeStampStart));
		}

		// Falls keine Lösung gefunden wurde, die Stellungen im Speicher löschen und
		// zurück springen.
		if(isSolutionFound == false){
			positionStorage.clear();

			// Restore the start board position.
		    board.setBoardPosition(startBoardPosition);

			return null;
		}

		// In diesem Vektor werden alle Pushes der Lösung abgelegt, um die Lösung Zug für Zug
		// durchgehen zu können
		ArrayList<IBoardPositionIterative> movements = new ArrayList<IBoardPositionIterative>(forwardPushesCount+backwardPushesCount);

		// Als erstes werden alle Stellungen rückwärts abgegangen, die durch die
		// Vorwärtssuche erreicht wurden
	    for(currentBoardPosition = boardPositionForward; currentBoardPosition.getPrecedingBoardPosition() !=  null; currentBoardPosition = (IBoardPositionIterative) currentBoardPosition.getPrecedingBoardPosition()) {
	        if(currentBoardPosition.getBoxNo() != NO_BOX_PUSHED) {
				movements.add(0, currentBoardPosition);
			}
	    }

	    // Nun werden die Stellungen, die durch die Rückwärtssuche erreicht wurden angehängt
	    for(currentBoardPosition = boardPositionBackward; currentBoardPosition.getPrecedingBoardPosition() !=  null; currentBoardPosition = (IBoardPositionIterative) currentBoardPosition.getPrecedingBoardPosition()) {
	        if(currentBoardPosition.getBoxNo() != NO_BOX_PUSHED) {
				movements.add(currentBoardPosition);
			}
	    }

	    // Restore the start board position.
	    board.setBoardPosition(startBoardPosition);

		// Die Spielerbewegungen nach und nach durchgehen und in der History ablegen, so als wenn
		// sie durch den Spieler abgegangen wären.
		// Der aktuelle Index in der History muss gemerkt werden, da der Benutzer genau
		// hier wieder starten soll. Alle Bewegungen, die jetzt eingefügt werden, sollen also
		// "in der Zukunft" liegen.
		int currentMovement = application.movesHistory.getCurrentMovementNo();

		for(ListIterator<IBoardPositionIterative> i = movements.listIterator();i.hasNext();) {

		    currentBoardPosition = i.next();

			int pushedBoxNo 	  = currentBoardPosition.getBoxNo();
			int boxPosition       = currentBoardPosition.getPositions()[pushedBoxNo];
			int direction 	      = currentBoardPosition.getDirection();
			int boxTargetPosition = 0;
			int boxStartPosition  = 0;

			// Kistenposition vor der Verschiebung ermitteln (Richtung ist in Bezug
			// zur Suchrichtung zu betrachten!)
			if(currentBoardPosition.getSearchDirection() == SearchDirection.FORWARD) {
				boxTargetPosition = boxPosition;
				boxStartPosition  = boxPosition - offset[direction];
			}
			else {
				boxStartPosition  = boxPosition;
				boxTargetPosition = boxPosition - offset[direction];
			}

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
		application.movesHistory.setMovementNo(currentMovement);

	    // Die Anfangsstellung auf dem Spielfeld setzen.
		for(int position=board.firstRelevantSquare; position<board.lastRelevantSquare; position++) {
		    board.removeBox(position);
		}
	    board.setBoardPosition(startBoardPosition);

        // Show a hash table statistic if requested.
        if(Debug.debugShowHashTableStatistic) {
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
	 * Erzeugt durch Verschieben alle möglichen Stellungen, die durch einen
	 * einzigen Push einer Kisten erzeugbar sind. Jede erzeugte Stellung wird in
	 * der Hashtable abgespeichert. Wurde sie bereits vorher einmal durch
	 * Verschieben erreicht, so wird sie übersprungen. Wurde sie vorher bereits
	 * einmal bei der Rückwärts (="Zieh")-Suche erreicht, so wurde eine Lösung
	 * für das Level gefunden.
	 *
	 * @return true = Lösung gefunden; false = keine Lösung gefunden
	 */
	private final boolean forwardSearch() {

		// Die Stellungen dieses Durchlaufs gehören zu einer neuen Suchtiefe.
		// (sollte keine weitere Stellung möglich sein ist das Erhöhen an dieser Stelle eigentlich
		// falsch, aber in diesem Fall wird sowieso eine neue Iteration gestartet)
		forwardPushesCount++;

		// Maximalen Lowerbound für Stellungen der aktuellen Suchtiefe errechnen
		// Dazu werden von der maximalen Pushesanzahl, die für die aktuelle Iterationstiefe vorgegeben ist,
		// die Anzahl der bereits durchgeführten Pushes bis einschließlich dieser Ebene abgezogen.
		int maximumLowerbound = maximumSolutionLengthCurrentIteration - forwardPushesCount;

		// Die aktuelle Stellungszahl beginnt bei 0
		int boardPositionsCurrentPushCount = 0;

		// Alle im letzten Zug erzeugten Stellungen durchgehen und prüfen,
		// welche Stellungen im nächsten Zug durch Schieben (= Vorwärtssuche) erzeugt werden können
		for(int positionNo = 0; positionNo < boardPositionsForwardSearchCount; positionNo++) {

			// Das Spielfeld mit der aktuellen Stellung besetzen
			board.setBoardPosition(boardPositionsForward[positionNo]);

			// Erreichbare Felder des Spielers ermitteln. Da diese Felder auch nach der Deadlock-
			// prüfung noch gebraucht werden, werden sie in einem eigenen Objekt berechnet.
			playersReachableSquares.update();

			// Get number of last pushed box.
			int pushedBoxNo  = boardPositionsForward[positionNo].getBoxNo();

			// Ermitteln, welche Kisten für den nächsten Push relevant sind.
			boolean[] relevantBoxes = identifyRelevantBoxes();

            // If no box has been pushed the pushed box number is set to -1, so the tunnel detection isn't performed.
            if(pushedBoxNo == NO_BOX_PUSHED) {
				pushedBoxNo = -1;
			}

			// Nun muss geprüft werden, welche Kisten in welche Richtungen verschoben werden können
			// -> welche neuen Stellungen können erzeugt werden
			for(int boxCounter = -1, boxNo; boxCounter < board.goalsCount; boxCounter++) {

			    // Die aktuell zu untersuchende Kiste ist die mit der Nummer in "kistenzähler"
			    boxNo = boxCounter;

			    // Beim ersten Durchgang wird immer die zuletzt verschobene Kiste betrachtet,
			    // damit eine Kiste möglichst in einem Stück geschoben wird.
			    if(boxCounter == pushedBoxNo) {
					continue;
				}
			    if(boxCounter == -1){
			        boxNo = pushedBoxNo;

					// Falls sich die verschobene Kiste in einem Tunnel befindet und nicht auf einem
					// Zielfeld steht, brauchen für diesen Push nur Verschiebungen dieser Kiste
				    // geprüft werden!
			        // (Es kann nur die verschobene Kiste in einem Tunnel stehen, da alle anderen
			        // ja schon vorher weitergeschoben worden wären!)
			        if(isBoxInTunnel(pushedBoxNo, SearchDirection.FORWARD)) {
						boxCounter = board.goalsCount;
					}
			    }

			    // Falls es kein Tunnel ist, so wird geprüft, ob ein I-Corral vorliegt. In diesem Fall
			    // können die nicht relevanten Kisten übersprungen werden.
			    // (Bei einem Tunnel wird sowieso nur eine Kiste verschoben, so dass auch ein I-Corral
			    // die Anzahl der zu verschiebenen Kisten nicht mehr reduzieren könnte)
				if(boxCounter < board.boxCount && relevantBoxes != null && relevantBoxes[boxNo] == false) {
					continue;
				}

				// Kistenposition holen
				int boxPosition = board.boxData.getBoxPosition(boxNo);

				// Verschieben in jede Richtung prüfen
				for(int direction = 0; direction < 4; direction++) {
					// Mögliche neue Position der Kiste errechnen
					int newBoxPosition = boxPosition + offset[direction];

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
					// Kennzeichnen zu welcher Suchrichtung sie gehört und in welcher Iteration sie erstellt wurde
					RelativeBoardPositionIterative currentBoardPosition = new RelativeBoardPositionIterative(board, boxNo, direction, boardPositionsForward[positionNo]);
					currentBoardPosition.setSearchDirection(SearchDirection.FORWARD);
					currentBoardPosition.setMaximumSolutionLength((short) maximumSolutionLengthCurrentIteration);

					// Prüfen, ob diese Stellung bereits schon einmal erreicht wurde, indem versucht
					// wird sie aus dem Stellungsspeicher zu lesen
					IBoardPosition oldBoardPosition = positionStorage.getBoardPosition(currentBoardPosition);

					// Falls die Stellung bereits vorher in der Vorwärtssuche erreicht wurde,
					// muss geprüft werden, ob sie in dieser Iteration schon erreicht wurde
					// oder in einer früheren Iteration.
					// Wurde sie bereits vorher durch die Rückwärtssuche erreicht, so wurde eine Lösung gefunden!
					// Wurde sie vorher noch nie erreicht, so wird sie als neue Stellung im Vorwärtsarray aufgenommen.
					if(oldBoardPosition != null) {
						IBoardPositionIterative searchOldBoardPosition = (IBoardPositionIterative) oldBoardPosition;

                        if(searchOldBoardPosition.getSearchDirection() == SearchDirection.BACKWARD) {

							// Push der Kiste rückgängig machen. Der Spieler wird so wieso beim nächsten
							// Aufruf wieder umgesetzt.
							// Dies muss geschehen, auch wenn eine Lösung gefunden wurde, da sonst die
							// allgemeine globale Variable mit den Anzahl der Pushes um eins zu hoch wäre.
							board.pushBoxUndo(newBoxPosition, boxPosition);

							// Es könnte folgender Fall eingetreten sein:
							// In der 1. Iteration wurden 10 Suchebenen (= alle Stellungen die mit
							// höchstens 10 Pushes aus der Anfangsstellung erzeugbar sind) in der
							// Rückwärtssuche analysiert und 8 in der Vorwärtssuche.
							// Dann wird die 2. Iteration gestartet (mit einer höheren maximalen Anzahl
							// Pushes). Jetzt findet die Vorwärtssuche in Pushebene 6 eine Stellung
							// die die Rückwärtssuche in Suchtiefe 10 erreicht hatte.
							// => Lösung gefunden mit 16 Pushes. Diese Lösung muss aber nicht optimal
							// sein! Es könnte sein, dass in der Vorwärtssuche in Pushebene 7 eine
							// Stellung erzeugt werden kann, die in der Rückwärtssuche in Tiefe 3
							// aufgetreten ist! => Lösung mit 10 Pushes!
							// Eine Lösung ist nur Pushoptimal, wenn sie kleiner oder gleich der
							// maximalen Pushanzahl für die aktuelle Iteration ist!
							// Falls eine nicht optimale Lösung gefunden wird, kann die Stellung verworfen werden,
							// da sie ja bereits durch die andere Suchrichtung weiterverfolgt wird.
							if(searchOldBoardPosition.getPushesCount() + currentBoardPosition.getPushesCount() > maximumSolutionLengthCurrentIteration) {
								continue;
							}

							// Stellung wurde auch bereits durch die
							// Rückwärtssuche erreicht => Lösung gefunden!
						    // Es wird nun in den globalen Variablen abgelegt,
						    // welche Stellung als letztes durch die Vorwärts-
						    // und welches zuletzt durch die Rückwärtssuche
						    // erreicht wurde.
							boardPositionBackward = searchOldBoardPosition;
							boardPositionForward  = currentBoardPosition;

							// Die Anzahl der Stellungen in einer globalen Variablen übergeben
							// ("++", da dieser Zug mitgezählt werden muss, das Hochzählen aber
							// noch nicht erfolgt ist (ist erst ein paar Codezeilen unter dieser)
							boardPositionsForwardSearchCount = ++boardPositionsCurrentPushCount;

							// Zurückgeben, dass eine Lösung gefunden wurde.
							return true;
                        }

						/*
                         * The board position had already been reached by the forward search.
						 */

						// Wenn die aktuelle Stellung bereits vorher in dieser Iterationsebene erreicht wurde,
						// dann kann gleich die nächste Richtung probiert werden, da die Stellung doppelt ist.
						if(searchOldBoardPosition.getMaximumSolutionLength() == maximumSolutionLengthCurrentIteration) {
							// Push der Kiste rückgängig machen. Der Spieler wird so wieso beim nächsten
							// Aufruf wieder umgesetzt. Dies muss hier also nicht extra geschehen.
							board.pushBoxUndo(newBoxPosition, boxPosition);
							continue;
						}

						// Stellung wurde in einer vorigen Iteration in die Hashtable eingetragen.
						// Sie wird nun als auch in dieser Iteration erreicht gekennzeichnet,
						// indem die aktelle Lowerboundgrenze in ihr gespeichert wird.
						searchOldBoardPosition.setMaximumSolutionLength((short) maximumSolutionLengthCurrentIteration);
					}

					// Die Stellung ist noch nicht in der Hashtable enthalten. Ihr Lowerbound muss deshalb erst
					// noch berechnet werden, um ihn mit der aktuellen Höchstgrenze an Pushes für diese Iteration
					// vergleichen zu können. Dadurch wird auch automatisch geprüft, ob sie eventuell eine
					// Deadlockstellung ist (dann wäre der Lowerbound = UNENDLICH)
					// Für die Stellungen, die bereits in der Hashtable gespeichert waren muss dies nicht getan
					// werden, da sie bereits auf Deadlock geprüft wurden und auf jeden Fall eine kleinere
					// Pushanzahl bis zum Ziel besitzen müssen als die aktuelle Obergrenze, denn ansonsten
					// wären sie nicht schon in der Hashtable eingetragen.
					else {
						int pushesLowerbound = lowerBoundCalcuation.calculatePushesLowerBound(newBoxPosition);

						// As long as there are more reachable board positions the search must continue.
						isSolutionStillPossible |= pushesLowerbound != LowerBoundCalculation.DEADLOCK;

						// Falls die Stellung mehr Pushes für die Lösung benötigt als maximal in dieser
						// Iteration benötigt werden darf, so wird sie verworfen
						if(pushesLowerbound > maximumLowerbound) {
							// Push der Kiste rückgängig machen. Der Spieler wird so wieso beim nächsten
							// Aufruf wieder umgesetzt. Dies muss hier also nicht extra geschehen.
							board.pushBoxUndo(newBoxPosition, boxPosition);
							continue;
						}

						// Count the reached no-deadlock board positions.
						boardPositionsCount++;
					}

					// Push der Kiste rückgängig machen. Der Spieler wird sowieso beim nächsten
					// Aufruf wieder umgesetzt. Dies muss hier also nicht extra geschehen.
					board.pushBoxUndo(newBoxPosition, boxPosition);

					// Stellung speichern.
					oldBoardPosition = positionStorage.storeBoardPosition(currentBoardPosition);

					// Neue Stellung durch Vorwärtssuche erreicht. Sie muss nun abgespeichert werden.
					currentBoardPositions[boardPositionsCurrentPushCount++] = currentBoardPosition;
				}
			}
		}

		// Die in diesem Zug erzeugten Stellungen sind die neuen Vorwärtsstellungen. Das Array der alten
		// Vorwärtsstellungen kann für den nächsten Durchlauf überschrieben werden und somit
		// als Array für die im nächsten Zug erzeugbaren Stellungen dienen
		IBoardPositionIterative[] tempReference = boardPositionsForward;
		boardPositionsForward 		 		 = currentBoardPositions;
		currentBoardPositions 		 		 = tempReference;
		boardPositionsForwardSearchCount     = boardPositionsCurrentPushCount;

		return false;
	}


	/**
	 * Erzeugt durch Ziehen alle möglichen Stellungen, die durch ein einziges
	 * Mal Ziehen einer Kisten erzeugbar sind. Jede erzeugte Stellung wird in
	 * der Hashtable abgespeichert. Wurde sie bereits vorher einmal durch Ziehen
	 * erreicht, so wird sie übersprungen. Wurde sie vorher bereits einmal bei
	 * der Vorwärts (=schiebe)-Suche erreicht, so wurde eine Lösung für das
	 * Level gefunden.
	 *
	 * @return true = Lösung gefunden; false = keine Lösung gefunden
	 */
	private final boolean backwardSearch() {

		// Die Stellungen dieses Durchlaufs gehören zu einer neuen Suchtiefe.
		// (sollte keine weitere Stellung möglich sein ist das Erhöhen an dieser Stelle eigentlich
		// falsch, aber in diesem Fall wird sowieso eine neue Iteration gestartet)
		backwardPushesCount++;

		// Maximalen Lowerbound für Stellungen der aktuellen Suchtiefe errechnen.
		// Dazu werden von der maximalen Pushesanzahl, die für die aktuelle Iterationstiefe vorgegeben ist,
		// die Anzahl der bereits durchgeführten Pushes bis einschließlich! dieser Ebene abgezogen.
		int maximumLowerbound = maximumSolutionLengthCurrentIteration - backwardPushesCount;

		// Die aktuelle Stellungszahl beginnt bei 0
		int boardPositionsCurrentPushCount = 0;

		// Alle im letzten Zug erzeugten Stellungen durchgehen und prüfen, welche Stellungen im
		// nächsten Zug durch Ziehen (= Rückwärtssuche!) erzeugt werden können
		for(int positionNo = 0; positionNo < boardPositionsBackwardSearchCount; positionNo++) {

			// Das Spielfeld mit der aktuellen Stellung besetzen
			board.setBoardPosition(boardPositionsBackward[positionNo]);

			// Erreichbare Felder des Spielers ermitteln
			playersReachableSquares.update();

			// Get number of last pushed box.
			int pushedBoxNo  = boardPositionsBackward[positionNo].getBoxNo();

            // If no box has been pushed the pushed box number is set to -1, so the tunnel detection isn't performed.
            if(pushedBoxNo == NO_BOX_PUSHED) {
				pushedBoxNo = -1;
			}

			// Nun muss geprüft werden, welche Kisten in welche Richtungen verschoben werden können
			// -> welche neuen Stellungen können erzeugt werden
			for(int boxCounter = -1, boxNo; boxCounter < board.goalsCount; boxCounter++) {

			    // Die aktuell zu untersuchende Kiste ist die mit der Nummer in "kistenzähler"
			    boxNo = boxCounter;

			    // Beim ersten Durchgang wird immer die zuletzt verschobene Kiste betrachtet,
			    // damit eine Kiste möglichst in einem Stück geschoben wird.
			    if(boxCounter == pushedBoxNo) {
					continue;
				}
			    if(boxCounter == -1){
			        boxNo = pushedBoxNo;

					// Falls sich die verschobene Kiste in einem Tunnel befindet und nicht auf einem
					// Zielfeld steht, brauchen für diesen Push nur Verschiebungen dieser Kiste
				    // geprüft werden!
			        // (Es kann nur die verschobene Kiste in einem Tunnel stehen, da alle anderen
			        // ja schon vorher weitergeschoben worden wären!)
			        if(isBoxInTunnel(pushedBoxNo, SearchDirection.BACKWARD)) {
						boxCounter = board.goalsCount;
					}
			    }

				// Kistenposition holen
				int boxPosition = board.boxData.getBoxPosition(boxNo);

				// Ziehen in jede Richtung prüfen
				for(int direction = 0; direction < 4; direction++) {

					// Mögliche neue Position der Kiste errechnen
					int newBoxPosition  = boxPosition + offset[direction];

					// Falls Kiste nicht in die gewünschte Richtung gezogen werden kann,
					// sofort die nächste Richtung probieren. (es wird geprüft, ob der Spieler auf die
					// richtige Seite gelangen und dann einen Schritt rückwärts gehen kann und das
					// Zielfeld kein simple Deadlockfeld ist)
					if(board.isAccessibleBox(newBoxPosition) == false
					   || playersReachableSquares.isSquareReachable(newBoxPosition + offset[direction]) == false) {
						continue;
					}

					// Pull durchführen (Für die Instanziierung eines Stellungsobjekts muss
					// die Spielerposition korrekt gesetzt sein!)
					board.pushBox(boxPosition, newBoxPosition);
					board.playerPosition = newBoxPosition + offset[direction];

					// Only for debugging: show board positions.
					if(solverGUI.isShowBoardPositionsActivated.isSelected()) {
						displayBoard();
					}

					// Objekt der aktuellen Stellung erzeugen (mit Referenz zur vorigen Stellung)
					// Kennzeichnen zu welcher Suchrichtung sie gehört und in welcher Iteration sie erstellt wurde
					RelativeBoardPositionIterative currentBoardPosition = new RelativeBoardPositionIterative(board, boxNo, direction, boardPositionsBackward[positionNo]);
					currentBoardPosition.setSearchDirection(SearchDirection.BACKWARD);
					currentBoardPosition.setMaximumSolutionLength((short) maximumSolutionLengthCurrentIteration);

					// Prüfen, ob diese Stellung bereits schon einmal erreicht wurde, indem versucht
					// wird sie aus dem Stellungsspeicher zu lesen
					IBoardPosition oldBoardPosition = positionStorage.getBoardPosition(currentBoardPosition);

					// Falls die Stellung bereits vorher in der Rückwärtssuche erreicht wurde,
					// muss geprüft werden, ob sie in dieser Iteration schon erreicht wurde
					// oder in einer früheren Iteration.
					// Wurde sie bereits vorher durch die Vorwärtssuche erreicht, so wurde eine Lösung gefunden!
					// Wurde sie vorher noch nie erreicht, so wird sie als neue Stellung im Rückwärtsarray aufgenommen.
					if(oldBoardPosition != null){
						IBoardPositionIterative searchOldBoardPosition = (IBoardPositionIterative) oldBoardPosition;

                        if(searchOldBoardPosition.getSearchDirection() == SearchDirection.FORWARD) {

                            // Push der Kiste rückgängig machen. Der Spieler wird so wieso beim nächsten
                            // Aufruf wieder umgesetzt.
                            // Dies muss geschehen, auch wenn eine Lösung gefunden wurde, da sonst die
                            // allgemeine globale Variable mit den Anzahl der Pushes um eins zu hoch wäre.
                            board.pushBoxUndo(newBoxPosition, boxPosition);

                            // Es könnte folgender Fall eingetreten sein:
                            // In der 1. Iteration wurden 10 Suchebenen (= alle Stellungen die mit
                            // höchstens 10 Pushes aus der Anfangsstellung erzeugbar sind) in der
                            // Vorwärtssuche analysiert und 8 in der Rückwärtssuche.
                            // Dann wird die 2. Iteration gestartet (mit einer höheren maximalen Anzahl
                            // Pushes). Jetzt findet die Rückwärtssuche in Suchtiefe 6 eine Stellung
                            // die die Vorwärtssuche in Suchtiefe 10 erreicht hatte.
                            // => Lösung gefunden mit 16 Pushes. Diese Lösung muss aber nicht optimal
                            // sein! Es könnte sein, dass in der Rückwärtssuche z.B. in Pushebene 7 eine
                            // Stellung erzeugt werden kann, die in der Vorwärtssuche in Tiefe 3
                            // aufgetreten ist! => Lösung mit 10 Pushes!
                            // Eine Lösung ist nur pushoptimal, wenn sie kleiner oder gleich der
                            // maximalen Pushanzahl für die aktuelle Iteration ist!
                            // Falls eine nicht optimale Lösung gefunden wird, kann die Stellung verworfen werden,
                            // da sie ja bereits durch die andere Suchrichtung weiterverfolgt wird.
                            if(currentBoardPosition.getPushesCount() + searchOldBoardPosition.getPushesCount() > maximumSolutionLengthCurrentIteration) {
								continue;
							}

                            // Stellung wurde auch bereits durch die
                            // Vorwärtssuche erreicht => Lösung gefunden!
                            // Es wird nun in den globalen Variablen abgelegt,
                            // welche Stellung als letztes durch die Vorwärts-
                            // und welches zuletzt durch die Rückwärtssuche
                            // erreicht wurde.
                            boardPositionBackward = currentBoardPosition;
                            boardPositionForward  = searchOldBoardPosition;

                            // Die Anzahl der Stellungen in einer globalen Variablen übergeben
                            // ("++", da dieser Zug mitgezählt werden muss, das Hochzählen aber
                            // noch nicht erfolgt ist (ist erst ein paar Codezeilen unter dieser)
                            boardPositionsBackwardSearchCount = ++boardPositionsCurrentPushCount;

                            // Zurückgeben, dass eine Lösung gefunden wurde.
                            return true;
                        }

                        /*
                         * Board position has already been reached by the backward search earlier.
                         */
						// Wenn die aktuelle Stellung bereits vorher in dieser Iterationsebene erreicht wurde,
						// dann kann gleich die nächste Richtung probiert werden, da die Stellung doppelt ist.
						if(searchOldBoardPosition.getMaximumSolutionLength() == maximumSolutionLengthCurrentIteration) {
							// Push der Kiste rückgängig machen. Der Spieler wird so wieso beim nächsten
							// Aufruf wieder umgesetzt. Dies muss hier also nicht extra geschehen.
							board.pushBoxUndo(newBoxPosition, boxPosition);
							continue;
						}

						// Stellung wurde in einer vorigen Iteration in die Hashtable eingetragen.
						// Sie wird nun als auch in dieser Iteration erreicht gekennzeichnet,
						// indem die aktelle Lowerboundgrenze in ihr gespeichert wird.
						searchOldBoardPosition.setMaximumSolutionLength((short) maximumSolutionLengthCurrentIteration);
					}
					else {
						// Die Stellung ist noch nicht in der Hashtable enthalten. Ihr Lowerbound muss deshalb erst
						// noch berechnet werden, um ihn mit der aktuellen Höchstgrenze an Pushes für diese Iteration
						// vergleichen zu können.
						// Für die Stellungen, die bereits in der Hashtable gespeichert waren muss dies nicht getan
						// werden, da sie bereits auf Deadlock geprüft wurden und auf jeden Fall eine kleinere
						// Pushanzahl bis zum Ziel besitzen müssen als die aktuelle Obergrenze, denn ansonsten
						// wären sie nicht schon in der Hashtable eingetragen.

						int pushesLowerbound = lowerBoundCalcuation.calculatePushesLowerboundBackwardsSearch(newBoxPosition);
						isSolutionStillPossible |= pushesLowerbound != LowerBoundCalculation.DEADLOCK;

						// Falls die Stellung mehr Pushes für die Lösung benötigt als maximal in dieser
						// Iteration benötigt werden darf, so wird sie verworfen
						if(pushesLowerbound > maximumLowerbound) {
							// Push der Kiste rückgängig machen. Der Spieler wird sowieso beim nächsten
							// Aufruf wieder umgesetzt. Dies muss hier also nicht extra geschehen.
							board.pushBoxUndo(newBoxPosition, boxPosition);
							continue;
						}

						// Hierdrin wird die Anzahl aller bei der Lösungssuche erreichten NichtDeadlock-
						// stellungen errechnet.
						boardPositionsCount++;
					}

					// Push der Kiste rückgängig machen. Der Spieler wird so wieso beim nächsten
					// Aufruf wieder umgesetzt. Dies muss hier also nicht extra geschehen.
					board.pushBoxUndo(newBoxPosition, boxPosition);

					// Stellung speichern.
					oldBoardPosition = positionStorage.storeBoardPosition(currentBoardPosition);

					// Neue Stellung durch Rückwärtssuche erreicht. Sie muss nun abgespeichert werden.
					currentBoardPositions[boardPositionsCurrentPushCount++] = currentBoardPosition;
				}
			}
		}

		// Die in diesem Zug erzeugten Stellungen sind die neuen Rückwärtsstellungen. Das Array der alten
		// Rückwärtsstellungen kann für den nächsten Durchlauf überschrieben werden und somit
		// als Array für die im nächsten Zug erzeugbaren Stellungen dienen
		IBoardPositionIterative[] tempReference = boardPositionsBackward;
		boardPositionsBackward 	 	 		 		  = currentBoardPositions;
		currentBoardPositions 		 		 		  = tempReference;
		boardPositionsBackwardSearchCount    	      = boardPositionsCurrentPushCount;

		return false;
	}


	/**
	 * Gibt zurück, ob sich die Kiste mit der übergebenen Kistennr in einem Tunnel
	 * befindet. Dies ist bei der Lösungssuche wichtig, denn sollte dies der Fall
	 * sein, so kann diese Kiste verschoben werden und die möglichen Veschiebungen
	 * der anderen Kisten brauchen für diesen Push nicht berücksichtigt werden!!!
	 *
	 * @param boxNo the number of the box
	 * @param searchDirection the direction the box has been pushed to
	 * @return <code>true</code> the box is in a tunnel
	 * 			<code>false</code> the box is not in a tunnel
	 */
	private final boolean isBoxInTunnel(int boxNo, SearchDirection searchDirection){

	    // Current and new box position.
	    int boxPosition;
	    int newBoxPosition;


	    boxPosition = board.boxData.getBoxPosition(boxNo);

	    // Falls die Kiste auf einem Zielfeld steht, so is ein eventueller Tunnel irrelevant.
	    // Dabei muss Unterschieden werden, in welcher Richtung gesucht wird.
	    if(searchDirection == SearchDirection.FORWARD  && board.isGoal(boxPosition) ||
	       searchDirection == SearchDirection.BACKWARD && board.isGoalBackwardsSearch(boxPosition)) {
			return false;
		}

	    /*
		  * Prüfen, ob durch Verschieben der Kiste nur Sicherheitsstellungen erzeugt werden können
		  * oder ob die Kiste überhaupt nur in eine Richtung verschoben werden kann.
		  *
		  * Sicherheitsstellung =
		  *    		   #
		  *   #$#  or  $
		  *	           #
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

	     // Verschieben in alle Richtungen ausprobieren
		 for(byte direction=0; direction<4; direction++) {
		     if(searchDirection == SearchDirection.FORWARD) {
				// Prüfen, ob Spieler auf die eine Seite kommt und die andere Seite frei ist (= keine Mauer,
			     // nicht ".isBetretbar()", denn die Kisten sind ja noch auf dem Feld!)
			     // Falls er es nicht kann, kann gleich in der nächsten Richtung geprüft werden.
			     if(board.isWall(boxPosition + offset[direction]) ||
		             board.playersReachableSquaresOnlyWalls.isSquareReachable(boxPosition - offset[direction]) == false) {
					continue;
				}
			}

		     //	Prüfen, ob der Spieler die Kiste in die jeweilige Richtung ziehen kann. Falls nicht,
		     // gleich in der nächsten Richtung weiterprobieren.
		     if(searchDirection == SearchDirection.BACKWARD) {
				if(board.playersReachableSquaresOnlyWalls.isSquareReachable(boxPosition + offset[direction]) == false ||
		            board.playersReachableSquaresOnlyWalls.isSquareReachable(boxPosition + 2*offset[direction]) == false) {
					continue;
				}
			}

		     // Kiste kann auf die gewünschte Position befördert werden
		     pushableDirectionsCount++;

		     // Prüfen, ob die Kiste auch tatsächlich verschoben werden kann (unter Berücksichtigung,
		     // dass noch weitere Kisten auf dem Feld sind - die aktuell erreichbaren Felder des
		     // Spielers liegen im globalen Array bereits vor)
		     if(searchDirection == SearchDirection.FORWARD && playersReachableSquares.isSquareReachable(boxPosition - offset[direction]) ||
		     	searchDirection == SearchDirection.BACKWARD && board.playersReachableSquaresOnlyWalls.isSquareReachable(boxPosition + 2*offset[direction])) {
				reallyPushableDirectionsCount++;
			}

		     //	Position der Kiste berechnen, auf der sie stehen würde, wenn die Verschiebung
		     // in die verschiebbare Richtung durchgeführt worden wäre.
		     newBoxPosition = boxPosition + offset[direction];

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
		  * Wenn die Kiste nur in eine Richtung verschoben werden kann , dann befindet sie sich
		  * ebenfalls in einem Tunnel und sie kann gleich noch einmal geschoben werden.
		  * Dies gilt allerdings nur, falls sie wirklich alle diese Sicherheitsstellungen mit dem
		  * nächsten Push erreichen kann.
		  */
	     if((pushableDirectionsCount > 0 && pushableDirectionsCount == safeSituationsCount ||
			    pushableDirectionsCount == 1 && searchDirection == SearchDirection.FORWARD) &&
				pushableDirectionsCount == reallyPushableDirectionsCount) {
			return true;
		}

		 // Die Kiste befindet sich nicht in einem Tunnel
	     return false;
	}
}