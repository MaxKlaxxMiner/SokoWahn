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
package de.sokoban_online.jsoko.deadlockdetection;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collection;
import java.util.HashMap;
import java.util.Iterator;

import de.sokoban_online.jsoko.board.Board;
import de.sokoban_online.jsoko.board.BoxData;
import de.sokoban_online.jsoko.board.DirectionConstants;
import de.sokoban_online.jsoko.board.Directions;
import de.sokoban_online.jsoko.boardpositions.AbsoluteBoardPosition;
import de.sokoban_online.jsoko.boardpositions.BoardPosition;
import de.sokoban_online.jsoko.boardpositions.CorralBoardPosition;
import de.sokoban_online.jsoko.boardpositions.IBoardPosition;
import de.sokoban_online.jsoko.gui.MainBoardDisplay;
import de.sokoban_online.jsoko.resourceHandling.Settings.SearchDirection;
import de.sokoban_online.jsoko.utilities.Debug;
import de.sokoban_online.jsoko.utilities.IntStack;

/**
 *  Class for detecting corral deadlocks.
 *  <p>
 *  Example level:
 *  <pre>
 *  ########
 *  #.  $  #
 *  #.@$   #
 *  ########
 *  </pre>
 *  Pushing the lower box to the right creates an area the player
 *  can't reach (right from the boxes). This area is called "corral"
 *  and is checked for being a deadlock in this class.
 */
public final class CorralDeadlockDetection implements DirectionConstants {

	/**
	 * Flag indicating that no box has been pushed. This constant is
	 * used as "box number", hence it's necessary that there are
	 * fewer than 511 boxes on the board.
	 */
	private final short NO_BOX_PUSHED = 511;

	/** Direct reference to the board of the current level. */
	private final Board board;

	/** Object for other deadlock detections than corral deadlock detection. */
	private DeadlockDetection deadlockDetection;

	/**
	 * Flag indicating that the corral deadlock detection has to be aborted due to
	 * reaching the time limit.
	 */
	private boolean isCorralDetectionToBeAborted;

	/** Time when to stop the deadlock detection in order to avoid too long runs. */
	private long timeWhenToStopDeadlockDetection;

	/**
	 * Counter for the number of corrals occurred during ALL deadlock detection runs.
	 * Every new found corral gets an own number so corral board positions belonging
	 * to this corral can be identified by checking this number.
	 */
	private int totalingCorralNo = 0;

	/**
	 * Lowest corral number of a specific deadlock detection run. All corrals found during a specific deadlock detection run (that is:
	 * a call of {@link #isDeadlock(int, long)} have a number >= this number. This number is used to identify corral board positions
	 * stored in the storage of previous deadlock detection runs.
	 * This way the storage needn't to be cleared for every run but the stored information can be reused.
	 */
	public int lowestCorralNo = 0;

	/** These variables are used to cache the created arrays that store corral information. */
	private int indexArraysCache = 0;
	private final ArrayList<byte[]> corralArrayCache;

	/**
	 * Storage for the board positions created during the corral detection. The storage
	 * keeps the board positions as long as this object lives.
	 */
	private final BoardPositionStorage boardPositionsStorage;

	/**
	 * Stack for positions used in method {@link #isACorralDeadlock(int, IBoardPosition, int)}.
	 * Instance variable for better performance.
	 */
	private final IntStack positions;


	/**
	 * The {@code CorralDeadlockDetection} detects corral deadlocks.
	 *  <p>
	 *  Example level:
	 *  <pre>
	 *  ########
	 *  #.  $  #
	 *  #.@$   #
	 *  ########
	 *  </pre>
	 *  Pushing the lower box to the right creates an area the player
	 *  can't reach (right from the boxes). This area is called "corral"
	 *  and is checked for being a deadlock.
	 *
	 * @param board  the board of the current level
	 * @param deadlockDetection  the reference to the deadlock detection object */
	public CorralDeadlockDetection(Board board, DeadlockDetection deadlockDetection) {

		this.board = board;
		this.deadlockDetection = deadlockDetection; // used for other deadlock detection methods

		// Create a storage for storing all board positions during the deadlock detection.
		boardPositionsStorage = new BoardPositionStorage(50000);

		positions = new IntStack(DIRS_COUNT * board.boxCount);

		// Cache for the arrays used in the deadlock detection. This cache is used
		// to reuse the arrays of a previous deadlock detection run.
		corralArrayCache = new ArrayList<byte[]>(1500);

	}

	/**
	 * Returns whether the current board position is a corral deadlock.
	 *
	 * @param newBoxPosition  the new position of the pushed box
	 * @param timeWhenToStopDetection  the system time this deadlock detection has to stop the detection
	 *
	 * @return <code>true</code> if the current board position is a corral deadlock, and <code>false</code> if there couldn't be detected a
	 *         corral deadlock in the current board position */
	final public boolean isDeadlock(int newBoxPosition, long timeWhenToStopDetection) {

		// The corral detection is aborted as soon as the time stamp passed to this method has been reached.
		isCorralDetectionToBeAborted = false;

		// The stop time is passed and not the ms because the call of this method also needs some time
		// in some cases. Hence, System.currentTimeMillis() + timeLimit wouldn't be that accurate.
		timeWhenToStopDeadlockDetection = timeWhenToStopDetection;

		// All corrals that are checked in this deadlock run have a corral number >= this number.
		lowestCorralNo = ++totalingCorralNo;

		// We can reuse all stored arrays in the cache since this is a new deadlock detection run.
		indexArraysCache = 0;

		boolean isDeadlock = isACorralDeadlock(newBoxPosition, new AbsoluteBoardPosition(board), 0);

		if(Debug.debugCorral == true) {				// Remove the corral numbers
			MainBoardDisplay.numbersToShow = null;		// from the board
			board.removeAllMarking();
			Debug.debugApplication.redraw(false);
		}

		// Only the classified corrals (-> deadlock or "not deadlock") are useful in the next runs.
		// Hence, the not classified ones can be removed from time to time to keep the storage small.
		if((totalingCorralNo&65535) == 0) {
			Collection<CorralBoardPosition> boardPositions = boardPositionsStorage.values();
			for(Iterator<CorralBoardPosition> iterator = boardPositions.iterator(); iterator.hasNext();) {
				CorralBoardPosition bp = iterator.next();
				if(!bp.isClassified()) {
					iterator.remove();
				}
			}
		}

		return isDeadlock;
	}

	/**
	 * Brute force search to find a way of pushing a box out of the corral or pushing all boxes
	 * to a goal. When none of that is possible the corral is a deadlock situation.
	 *
	 * @param corral  positions that are part of the corral are marked with the passed indicator value
	 * @param corralSquareIndicatorValue  positions marked with this value are part of the corral
	 * @param currentCorralNo  number of the currently analyzed corral
	 * @param currentBoardPosition  the current board position
	 * @param recursionDepth  recursion depth to avoid stack overflows
	 *
	 * @return <code>true</code> deadlock, <code>false</code> not a deadlock
	 */
	private final boolean solveCorral(byte[] corral, byte corralSquareIndicatorValue, int currentCorralNo,
			IBoardPosition currentBoardPosition, int recursionDepth) {

		// Stop search when corral is solved or the stack size may overflow.
		// Note: this method is called from itself after a deadlock check has been performed.
		// Frozen boxes on non-goals (which don't belong to the corral) are therefore considered
		// deadlock by the freeze check. This means is sufficient to check only corral boxes for
		// being on a goal.
		if (board.boxData.isEveryCorralBoxOnAGoal() || recursionDepth > 1000) {
			return false;
		}

		// The reachable player squares are changed (for instance in method removePushableNotCorralBoxes).
		// However, they are needed for checking which box can be pushed to which direction.
		// Hence, the reachable squares are cloned.
		// This method assumes that the player reachable squares are up-to-date at this point!
		// ("new CorralBoardPosition(...)" for instance updates the reachable squares before this method is called)
		Board.PlayersReachableSquares playersReachableSquares = board.playersReachableSquares.getClone();

		// DEBUG: check if reachable squares are really still up-to-date from time to time.
		if(Debug.isDebugModeActivated && (currentCorralNo&7) == 0)  {
			playersReachableSquares.update();
			for(int i=0; i<board.size; i++) {
				if(playersReachableSquares.isSquareReachable(i) != board.playersReachableSquares.isSquareReachable(i)) {
					System.out.println("Failure. Reachable player squares are not up-to-date!");
				}
			}
		}

		for (int boxNo = 0; boxNo < board.boxCount; boxNo++) {

			// Only "active" boxes that are pushable and part of the corral need to be considered.
			if (board.boxData.isBoxInactive(boxNo) || board.boxData.isBoxFrozen(boxNo) || !board.boxData.isBoxInCorral(boxNo)) {
				continue;
			}

			int boxPosition = board.boxData.getBoxPosition(boxNo);

			// Terminate the corral search if it lasts too long or it is too deep (a stack overflow may occur!)
			if (System.currentTimeMillis() > timeWhenToStopDeadlockDetection) {
				isCorralDetectionToBeAborted = true;
				return false;
			}

			// Try to push the box to every direction.
			for (int direction = 0; direction < DIRS_COUNT; direction++) {

				int newBoxPosition = board.getPosition(boxPosition, direction);

				// Continue with next direction if the box can't be pushed to that direction.
				if (!playersReachableSquares.isSquareReachable(board.getPositionAtOppositeDirection(boxPosition, direction))
						|| !board.isAccessibleBox(newBoxPosition)) {
					continue;
				}

				int playerPositionBackup = board.playerPosition;

				// DEBUG: display corral
				if (Debug.debugCorral == true) {
					MainBoardDisplay.numbersToShow = new int[board.size];
					for (int position = 0; position < board.size; position++) {
						board.assignMarking(position, corral[position] == corralSquareIndicatorValue);
						MainBoardDisplay.numbersToShow[position] = corral[position] == corralSquareIndicatorValue ? currentCorralNo : -1;
					}
					Debug.debugApplication.redraw(true);
				}

				// Do the push on the board.
				board.pushBox(boxPosition, newBoxPosition);
				board.playerPosition = boxPosition;

				if (Debug.debugCorral == true) {
					Debug.debugApplication.redraw(true);  // DEBUG: display new board
				}

				// The new board situation has to be saved in the storage => create a board position to be stored.
				CorralBoardPosition newBoardPosition = new CorralBoardPosition(board, boxNo, direction, currentBoardPosition, currentCorralNo);

				// Check if the storage already contains a duplicate of the board position.
				CorralBoardPosition oldPosition = boardPositionsStorage.getBoardPosition(newBoardPosition);

				// Achtung: ist alteStellung.isKeinDeadlock == true, darf
				// trotzdem nicht mit false zurückgesprungen werden!
				// Denn es kann ja sein, dass diese Stellung vorher
				// mit einem kleinen Corral untersucht wurde und dabei kein
				// Deadlock war. Nun ist dieses Corral aber innerhalb eines
				// größeren Corrals aufgetreten und ist möglicherweise nicht lösbar!

				// Continue with the next direction if:
				// - the current board position has been analyzed (and therefore stored) before
				// 	 being identified as a deadlock
				// - the current board position has been reached before during the analysis of
				//   the current corral
				// - the current board position is a deadlock
				// Note: if oldPosition.
				if (oldPosition != null
						&& (oldPosition.isCorralDeadlock() || oldPosition.getCorralNo() == currentCorralNo)
						|| isADeadlock(newBoxPosition, newBoardPosition, recursionDepth+1)) {

					// Undo the push.
					board.pushBoxUndo(newBoxPosition, boxPosition);
					board.playerPosition = playerPositionBackup;

					if (Debug.debugCorral == true) {
						Debug.debugApplication.redraw(true); // DEBUG: display new board
					}

					// Falls die Deadlockprüfungen durchlaufen wurden und ein Deadlock
					// erkannt wurde, so wird dieses Deadlock hier nicht für die aktuelle
					// Stellung gespeichert, denn:
					// sollte durch den Zug ein weiteres Corral entstanden sein, würde die
					// Stellung in "isACorralDeadlock" bereits als Deadlock markiert werden.
					// In allen anderen Fällen ist die Deadlockerkennung recht schnell und kann
					// deshalb jedes Mal erneut durchgeführt werden!
					// (vielleicht später einmal ändern, falls "n Kisten für n-y Zielfelder"-Deadlocks
					// auch eine lange Laufzeit zur Erkennung benötigen)
					// Es kann aber natürlich auch sein, dass sehr viele Züge gemacht werden müssen,
					// bis festgestellt wurde, dass es ein Deadlock ist, ohne dass dabei ein neues
					// Corral aufgetreten ist. In diesem Fall könnte es sich auch lohnen die Stellung
					// als Deadlock zu speichern)

					// Spielfeldsituation wird bereits für dieses Corral analysiert oder ist eine Deadlocksituation
					// -> nächste Richtung untersuchen
					continue;
				}

				// Falls die neue Position der Kiste nicht im Corral ist, sofort mit "false" zurück-
				// springen, so dass nicht weitergesucht wird, denn es ist dadurch ja kein Deadlock.
				if (corral[newBoxPosition] != corralSquareIndicatorValue) {

					// Push der Kiste rückgängig machen und Spieler wieder zurücksetzen
					board.pushBoxUndo(newBoxPosition, boxPosition);
					board.playerPosition = playerPositionBackup;
					if (Debug.debugCorral == true) {
						Debug.debugApplication.redraw(true);
					}

					return false;
				}

				// Alle verschiebbaren NichtCorralKisten vom Feld nehmen
				boolean isABoxSetInactive = removePushableNotCorralBoxes();

				// Die erreichte Stellung als während der Corralanalyse erreicht kennzeichnen,
				// indem ein RelativeStellungobjekt zur Stellung gespeichert wird.
				// (um Rekursionen zu vermeiden)
				// Obwohl dieser Code hier nur durchlaufen wird, nachdem etwas weiter oben
				// deadlockprüfungen(...) mit dem Ergebnis "Kein Deadlock" durchlaufen wurde,
				// darf die Stellung nicht als KeindDeadlock gespeichert werden!!!
				// Denn die Deadlockprüfungen erkennen ja nicht jedes Deadlock!
				// Um also auch etwas kompliziertere Corraldeadlocks erkennen zu müssen,
				// muss hier solange geprüft werden, bis feststeht, dass das Corral
				// nicht durchbrochen werden kann.

				// Durch das Deaktivieren verschiebbarer Kisten kann eine neue Stellung entstanden sein.
				// In diesem Fall muss nun erneut ein neues Stellungsobjekt erzeugt werden, das relativ
				// zur übergebenen Stellung ("aktuelleStellung") gesehen werden kann.
				if (isABoxSetInactive == true) {
					newBoardPosition = new CorralBoardPosition(board, boxNo, direction, currentBoardPosition, currentCorralNo);
				}

				boardPositionsStorage.storeBoardPosition(newBoardPosition);

				// Durch das eventuelle Deaktivieren einiger Kisten kann eine neue Stellung entstanden
				// sein. Wurden Kisten deaktiviert, so kann keine Suchstellung herausgekommen sein
				// (= es kann keine Stellung herausgekommen sein, die eine Suchrichtung hat).
				// Wurden keine Kisten deaktivert, so hat sich die Stellung nicht geändert.
				// Die Stellung wurde aber schon weiter oben auf Gleichheit mit einer Suchstellung
				// geprüft, so dass dies hier nicht noch einmal gemacht werden muss.

				// Eine Ebene tiefer weitersuchen
				boolean isSolvingToBeContinued = solveCorral(corral, corralSquareIndicatorValue, currentCorralNo, newBoardPosition, recursionDepth+1);

				// Push der Kiste rückgängig machen und Spieler wieder zurücksetzen
				board.pushBoxUndo(newBoxPosition, boxPosition);
				board.playerPosition = playerPositionBackup;

				if (Debug.debugCorral == true) {
					Debug.debugApplication.redraw(true);
				}

				// Falls in tieferen Suchebenen bewiesen wurde, dass es kein Deadlock ist
				// (weitersuchen == false), dann muss dieses Ergebnis sofort zurückgegeben werden,
				// da die Suche somit abgebrochen werden kann.
				if (isSolvingToBeContinued == false) {
					return false;
				}
			}
		}

		// Es wurde noch kein Beweis dafür gefunden, dass es sich nicht um ein Deadlock handelt
		// (da ansonsten vorher ein "return false" ausgeführt worden wäre).
		// Deshalb muss in der Ebene höher weitergesucht werden (bzw. true an die aufrufende
		// Funktion zurückgegeben werden)
		return true;
	}

	/**
	 * Nimmt alle in der derzeitigen Stellung befindlichen Kisten vom Feld, die nicht zum Corral gehören. Alle unverschiebbaren könnten eine
	 * Corralösung verhindern, und bleiben damit auf dem Feld, damit diese Deadlocks auch erkannt werden. Die verschiebbaren könnten zwar
	 * auch nach der Verschiebung noch ein Corralösen verhindern, dies ist aber nur sehr schwer herauszufinden. Deshalb werden alle
	 * verschiebbaren Kisten, die nicht zum Corral gehören pauschal vom Feld genommen.
	 *
	 * @return <code>true</code> at least one box has been removed <code>false</code> no box has been removed */
	final private boolean removePushableNotCorralBoxes() {

		// Gibt an, ob mindestens eine Kiste verschoben wurde
		boolean atLeastOneBoxHasBeenPushed = false;

		// Achtung: Falls eine Kiste verschoben werden konnte, kann dies bedeuten,
		// dass eine vorher geprüfte Kiste jetzt auch verschoben werden kann!
		// Erreichbare Felder ermitteln
		board.playersReachableSquares.update();

		boolean aBoxHasBeenPushed = true;

		while (aBoxHasBeenPushed == true) {
			aBoxHasBeenPushed = false;

			for (int boxNo = 0; boxNo < board.boxCount; boxNo++) {

				// Deaktive und Blockerkisten können nicht verschoben werden.
				// Außerdem dürfen die Kisten, die im aktuellen Corral sind nicht entfernt werden.
				if (board.boxData.isBoxInactive(boxNo) || board.boxData.isBoxFrozen(boxNo) || board.boxData.isBoxInCorral(boxNo)) {
					continue;
				}

				int boxPosition = board.boxData.getBoxPosition(boxNo);

				// Versuchen die Kiste in alle Richtungen zu verschieben
				for (int direction = 0; direction < DIRS_COUNT; direction++) {

					// Mögliche neue Koordinaten der Kiste errechnen
					int newBoxPosition = board.getPositionAtOppositeDirection(boxPosition, direction);

					// Falls Kiste nicht verschoben werden kann sofort die nächste Richtung probieren
					if (!board.playersReachableSquares.isSquareReachable(board.getPosition(boxPosition, direction))
				  	 || !board.isAccessibleBox(newBoxPosition)) {
						continue;
					}

					// Kiste wieder auf das Originalfeld setzen
					board.boxData.setBoxPosition(boxNo, boxPosition);

					// Die Kiste kann verschoben werden ohne ein Deadlock zu
					// erzeugen und muss deshalb vom Feld genommen werden.
					board.removeBox(boxPosition);

					board.boxData.setBoxInactive(boxNo);
					aBoxHasBeenPushed = true;
					atLeastOneBoxHasBeenPushed = true;

					// Erreichbare Felder des Spielers neu ermitteln, da eine Kisten vom Feld genommen wurde.
					// Da die Felder des Spielers gleich bleiben, bis auf die Tatsache, dass eine Box fehlt,
					// genügt es den Spieler auf die Kistenposition zu setzen und die erreichbaren Felder
					// zu erweitern. Damit sucht die Routine nur die "neuen" erreichbaren Felder,
					// anstatt noch mal alle erreichbaren Felder zu suchen!
					board.playersReachableSquares.enlarge(boxPosition);

					// Kein Prüfen in einer anderen Richtung für diese Kiste mehr notwendig
					break;
				}
			}
		}

		return atLeastOneBoxHasBeenPushed;
	}

	/**
	 * Diese Methode wird bei der Suche nach Corraldeadlocks aufgerufen. Während der Corralanalyse können "Untercorrals" auftreten, so dass
	 * diese Methode von "löseCorral" direkt aufgerufen wird.
	 * <p>
	 * The current board position is checked for deadlocks.
	 *
	 * Dieser Methode gibt verlässliche "negative" Beurteilungen. D.h. wenn diese Methode eine Stellung als Deadlockstellung einstuft, dann
	 * kann sie definitiv nicht mehr gelöst werden.
	 *
	 * @param newBoxPosition  new position of the pushed box
	 * @param currentPosition current board position
	 * @param recursionDepth  recursion depth to avoid stack overflows
	 *
	 * @return <code>true</code>board position is a deadlock; <code>false</code> otherwise
	 */
	private final boolean isADeadlock(int newBoxPosition, BoardPosition currentPosition, int recursionDepth) {

		// For the freeze check frozen boxes aren't marked, because the corral deadlock detection doesn't benefit
		// much from this information. If used all boxes have to be set as "No blocker" after every push and
		// every time the blocker boxes have to determined new. This would last too long.
		// The normal deadlock detection (#deadlockDetection.isDeadlock) can't
		// be used because the corral deadlock detection has to be called using
		// the internal method "isACorralDeadlock".
		return board.isSimpleDeadlockSquare(newBoxPosition)
				|| deadlockDetection.freezeDeadlockDetection.isDeadlock(newBoxPosition, false)
				|| deadlockDetection.closedDiagonalDeadlockDetection.isDeadlock(newBoxPosition)
				|| deadlockDetection.bipartiteDeadlockDetection.isDeadlock(SearchDirection.FORWARD)
				|| isACorralDeadlock(newBoxPosition, currentPosition, recursionDepth+1);
	}

	/**
	 * Checks if there is an area the player can't reach which makes the level unsolvable.
	 *
	 * @param newBoxPosition  new box position
	 * @param currentPosition current board position on the board
	 * @param recursionDepth  recursion depth to avoid stack overflows
	 *
	 * @return {@code true} = deadlock, {@code false} = no deadlock
	 */
	private final boolean isACorralDeadlock(int newBoxPosition, IBoardPosition currentPosition, int recursionDepth) {

		// Constant indicating potential corral squares -> starting from positions marked with
		// this constants corrals are searched.
		final byte POTENTIAL_CORRAL_SQUARE = 1;

		// Create a new array for storing the corral information.
		final byte[] corral = getCorralArray();

		// Value for marking the positions belonging to the current corral.
		byte corralSquareIndicatorValue = -1;

		// Create a backup of the current box data.
		final BoxData boxDataBackup = (BoxData) board.boxData.clone();

		/* Alle potentiellen Corralfelder ermitteln. Dazu werden von der verschobenen Kiste aus alle direkt angrenzenden Kisten ermittelt.
		 * Von diesen Kisten werden wieder alle angrenzenden Kisten ermittelt, ... Als potentielle Corralfelder gelten alle Felder, die
		 * direkt an eine der ermittelten Kisten angrenzen. */
		// Die Position der verschobenen Kiste ist der Ausgangspunkt
		positions.add(newBoxPosition);
		corral[newBoxPosition] = POTENTIAL_CORRAL_SQUARE;

		while (!positions.isEmpty()) {
			int position = positions.remove();

			// Falls das Feld eine Kiste enthält sind auch alle umliegenden freien
			// Felder dieser Kiste potentielle Corralfelder.
			if (board.isBox(position)) {
				for (int direction = 0; direction < DIRS_COUNT; direction++) {
					int neighborSquare = board.getPosition(position, direction);
					if (corral[neighborSquare] != POTENTIAL_CORRAL_SQUARE && !board.isWall(neighborSquare)) {
						positions.add(neighborSquare);
						corral[neighborSquare] = POTENTIAL_CORRAL_SQUARE;
					}
				}
			}
		}

		/* Die ermittelten Felder sind Felder in potentiellen Corrals (Corral = Bereich, den der Spieler nicht erreichen kann). Diese
		 * Corrals werden nun nacheinander auf Deadlocksituationen überprüft. */
		// Alle Corrals nacheinander abarbeiten
		for (int position = board.firstRelevantSquare; position < board.lastRelevantSquare && !isCorralDetectionToBeAborted; position++) {

			if (corral[position] != POTENTIAL_CORRAL_SQUARE || board.isBox(position)) {
				continue;
			}

			// Vom gefundenen Corralfeld ausgehend alle vom Spieler erreichbaren
			// Felder mit einem eindeutigen Wert markieren.
			board.playersReachableSquares.update(corral, --corralSquareIndicatorValue, position);

			// Falls das Corral den Spieler enthält so ist es irrelevant
			if (corral[board.playerPosition] == corralSquareIndicatorValue) {
				continue;
			}

			// Alle Kisten durchgehen
			for (int boxNo = 0; boxNo < board.boxCount; boxNo++) {

				// Deaktive Kisten überspringen
				if (board.boxData.isBoxInactive(boxNo)) {
					continue;
				}

				// Kistenposition holen
				int boxPosition = board.boxData.getBoxPosition(boxNo);

				int boxPositionUp    = board.getPosition(boxPosition, UP);
				int boxPositionDown  = board.getPosition(boxPosition, DOWN);
				int boxPositionLeft  = board.getPosition(boxPosition, LEFT);
				int boxPositionRight = board.getPosition(boxPosition, RIGHT);

				if (corral[boxPositionUp]    == corralSquareIndicatorValue && !board.isBox(boxPositionUp)
				 || corral[boxPositionDown]  == corralSquareIndicatorValue && !board.isBox(boxPositionDown)
				 || corral[boxPositionLeft]  == corralSquareIndicatorValue && !board.isBox(boxPositionLeft)
				 || corral[boxPositionRight] == corralSquareIndicatorValue && !board.isBox(boxPositionRight)) {

					// Die Kiste gehört zum Corral, da sie ein Corral Nachbarfeld hat.
					// Das Feld der Kiste selbst gehört deshalb auch zum Corral!
					// Corralfelder mit Kiste gelten nicht als Corralnachbarfelder!
					corral[boxPosition] = corralSquareIndicatorValue;
					board.boxData.setBoxInCorral(boxNo);
				} else {
					// Die Kiste gehört nicht zum Corral, deshalb entfernen.
					// (Sie kann noch aus einer höheren Ebene als im Corral befindlich gesetzt sein!)
					board.boxData.removeBoxFromCorral(boxNo);
				}
			}

			/* Jetzt sind alle Kisten, die direkt an das Corral angrenzen als solche markiert. */

			// Nun werden dem Corral geblockte Nachbarkisten der bisherigen Corralkisten hinzugefügt:
			for (int boxNo = 0; boxNo < board.boxCount; boxNo++) {

				// Nur Corralkisten verarbeiten
				if (board.boxData.isBoxInCorral(boxNo) == false) {
					continue;
				}

				// Kistenposition holen
				int boxPosition = board.boxData.getBoxPosition(boxNo);

				// Alle geblockten Nachbarkisten sollen auch zum Corral gehören.
				for (int direction = 0; direction < DIRS_COUNT; direction++) {
					int neighborSquare = board.getPosition(boxPosition, direction);

					// Falls auf dem Nachbarfeld keine Kiste steht oder sie schon zum Corral
					// gehört, kann gleich in der nächsten Richtung weitergesucht werden.
					if (!board.isBox(neighborSquare) || board.boxData.isBoxInCorral(board.getBoxNo(neighborSquare))) {
						continue;
					}

					/* Nun wird geprüft, ob die Kiste auf der anderen Achse geblockt ist. */
					// Hilfsvariablen, die die Positionen der Nachbarfelder auf einer Achse aufnehmen
					int orthogonalDirection  = Directions.getOrthogonalDirection(direction);
					int neighbor1 = board.getPosition(neighborSquare, orthogonalDirection);
					int neighbor2 = board.getPositionAtOppositeDirection(neighborSquare, orthogonalDirection);

					if (
							// Blocked by a wall
							board.isWall(neighbor1) || board.isWall(neighbor2)
							||

							// Blocked by a box of the corral
							(board.isBox(neighbor1) && board.boxData.isBoxInCorral(board.getBoxNo(neighbor1)))
							|| (board.isBox(neighbor2) && board.boxData.isBoxInCorral(board.getBoxNo(neighbor2))) ||

							// Immoveable due to deadlock squares
							(board.isSimpleDeadlockSquare(neighbor1) && board.isSimpleDeadlockSquare(neighbor2))) {
						// Kiste und Feld der Kiste ins Corral aufnehmen
						corral[neighborSquare] = corralSquareIndicatorValue;
						board.boxData.setBoxInCorral(board.getBoxNo(neighborSquare));
					}
				}
			}

			boolean isDeadlock = false;

			// Hinweis: Wenn beim Rausschieben ein neues Corral entsteht erkennt
			// "bipartite Deadlockprüfung" das Corral nicht!
			// Deshalb muss auch ein Corral mit nur einer Kiste geprüft werden!

			// Bevor die Spielfeldsituation analysiert wird, wird geprüft, ob sie vielleicht
			// schon vorher einmal klassifiziert wurde bzw. ob sie bereits in einer höheren Ebene
			// untersucht wird (dann könnte nämlich eine Endlosschleife entstehen).
			// Das Hauptcorral ist das allererste Corral, was untersucht wurde.
			// Dazu müssen erst einmal alle NichtCorralKisten, die verschoben werden können, vom
			// Feld genommen werden.
			removePushableNotCorralBoxes();

			// Falls verschiebbare Kisten vom Feld genommen wurden, ist eine
			// neue Stellung entstanden. Es wurde dabei aber keine Kiste verschoben.
			// Es wird deshalb ein neues Stellungsobjekt angelegt.
			CorralBoardPosition newBoardPosition = new CorralBoardPosition(board, NO_BOX_PUSHED, 0, currentPosition, ++totalingCorralNo);

			// Neue Stellung speichern (Beim ersten Mal wird eine Suchstellung übergeben. Diese
			// Suchstellung wurde aber noch nicht gespeichert! Dies geschieht erst, wenn die
			// Deadlockprüfungen abgeschlossen sind. Deshalb erfolgt hier keine Doppelspeicherung)
			CorralBoardPosition oldPosition = boardPositionsStorage.storeBoardPosition(newBoardPosition);

			// Das neue Corral nur untersuchen, falls:
			// 1. Es vorher noch nie untersucht wurde -> == null
			// 2. Die Stellung noch nicht klassifiziert ist -> "isBeeingAnalyzed"
			// ABER: "isBeeingAnalyzed" kann ja auch auftreten, wenn das selbe Corral bei der
			// Corralanalyse erneut auftritt. Aus diesem Grund darf es nur untersucht werden,
			// wenn es eine Stellung von einem alten Corral ist -> CorralNr < g_NrHauptcorral"
			// Analyze corral if it hasn't been analyzed before (or it has been analyzed but
			// not considered a deadlock / not deadlock).
			if (oldPosition == null
					|| (oldPosition.isBeeingAnalyzed() && oldPosition.getCorralNo() < lowestCorralNo)) {

				// Jedes Corral erhält eine eigene Nummer. Dies muss in einer globalen Variable geschehen,
				// da auch diese Methode rekursiv aufgerufen werden kann!
				// ("isACorralDeadlock" ruft "search" auf, "search" ruft "Deadlockprüfungen" auf,
				// "Deadlockprüfungen" ruft "isACorralDeadlock" auf, ....)
				// Diese Nummer wird benötigt, um in der Hashtable der erreichten Positionen für
				// jedes Corral "eigene" erreichte Stellung zu speichern.

				// Die neue Stellung wurde als erreicht markiert, indem sie in der
				// Hashtable gespeichert wurde.
				// Dies ist wichtig, da in Leveln wie diesem:
				// ############
				// #          #
				// #   #$ #   #
				// #####+ #####
				//     ####
				// sonst eine Endlosschleife entsteht, weil beim Versuch das eine Corral zu lösen
				// das andere Corral erzeugt wird und umgekehrt!

				// Prüfen, ob es sich um ein Deadlock handelt
				isDeadlock = solveCorral(corral, corralSquareIndicatorValue, totalingCorralNo, newBoardPosition, recursionDepth+1);

				// Klassifizierung des Corrals speichern.
				if (isDeadlock == false) {
					newBoardPosition.setNotCorralDeadlock();
				} else {
					newBoardPosition.setCorralDeadlock();
				}
			}

			// Spielfeld für den nächsten Durchlauf wieder in den
			// Ursprungszustand zurück setzen
			for (int boxNo = 0; boxNo < board.boxCount; boxNo++) {
				// Kisten des aktuellen Corrals löschen
				if (board.boxData.isBoxActive(boxNo)) {
					board.removeBox(board.boxData.getBoxPosition(boxNo));
				}

				// Originalkisten im Feld wieder setzen, falls sie aktiv sind
				if (boxDataBackup.isBoxActive(boxNo)) {
					board.setBoxWithNo(boxNo, boxDataBackup.getBoxPosition(boxNo));
				}
			}

			// Originalzustand des Kistendatenobjektes wieder herstellen
			board.boxData = (BoxData) boxDataBackup.clone();

			// Falls ein Deadlock gefunden wurde, kann sofort zurückgesprungen werden
			if (isDeadlock || oldPosition != null && oldPosition.isCorralDeadlock()) {
				return true;
			}

			// Das aktuell analysierte Corral war kein Deadlock.
			// Es wird jetzt automatisch das nächste Corral geprüft,falls es ein solches gibt!
			// Erst wenn alle Corrals der aktuellen Stellung überprüft wurden,
			// steht fest, ob es tatsächlich kein Deadlock ist!
		}

		return false;
	}

	/**
	 * The deadlock detection needs many byte arrays of the size "board.size". Therefore this method
	 * stores the created arrays in a list. When the next deadlock detection is started these arrays
	 * can be reused which results in a better performance.
	 *
	 * @return byte array for corral information
	 */
	final private byte[] getCorralArray() {

		// For every call of "isDeadlock" this counter is set back to 0.
		// Then this method can reuse the arrays stored in the previous call of "isDeadlock".
		indexArraysCache++;

		// Return a new array if none resuable is available at the momement.
		if (indexArraysCache > corralArrayCache.size()) {
			byte[] corral = new byte[board.size];
			corralArrayCache.add(corral);
			return corral;
		}

		// Reuse an array already stored.
		byte[] corral = corralArrayCache.get(indexArraysCache - 1);
		Arrays.fill(corral, (byte) 0);

		return corral;
	}

	/**
	 * DEBUG: show statistics about the stored corral board positions.
	 */
	public void debugShowStatistic() {
		boardPositionsStorage.debugShowStatistic();
	}

	/**
	 * Storage for {@code CorralBoardPosition}s.
	 * <<p>
	 * A hash table is used to store the board positions.
	 * Board positions with the same hash value are stored in a linked list
	 * in the same slot of the hash table.
	 */
	@SuppressWarnings("serial")
	public class BoardPositionStorage extends HashMap<CorralBoardPosition, CorralBoardPosition> {

		/**
		 * Creates an object for storing board positions in a hash table.
		 *
		 * @param initialCapacity	the initial capacity of this hash table.
		 */
		public BoardPositionStorage(int initialCapacity) {
			super(initialCapacity);
		}

		/**
		 * Stores the passed board position in this storage.
		 * The calling method assumed the passed board position really to be stored
		 * in the hash table, viz. when the board position is changed later in the
		 * program it's assumed to also be changed in the hash table.
		 * Nevertheless, in many cases this method knows that the passed board position
		 * will never be changed. Therefore the board position isn't saved when an equivalent
		 * board position is already stored in the hash table.
		 *
		 * @param boardPosition  board position to be stored
		 * @return an equivalent board position to the passed one that has
		 *         been replaced by the new passed board position.
		 */
		public CorralBoardPosition storeBoardPosition(CorralBoardPosition boardPosition) {

			// Get the board position that is equal to the passed one.
			CorralBoardPosition oldCorralBoardPosition = get(boardPosition);

			// If there isn't an equivalent board position in the hash table store the passed one and return.
			if (oldCorralBoardPosition == null) {
				return put(boardPosition, boardPosition);
			}

			// Der Fall, dass die alte Corralstellung von einer Corralanalyse
			// eines alten Corrals stammt.
			if (oldCorralBoardPosition.getCorralNo() < lowestCorralNo) {
				put(boardPosition, boardPosition);

				if (oldCorralBoardPosition.isCorralDeadlock()) {
					boardPosition.setCorralDeadlock();
				}
				if (oldCorralBoardPosition.isNotCorralDeadlock()) {
					boardPosition.setNotCorralDeadlock();
				}

				return oldCorralBoardPosition;
			}

			// Die Corralstellung gehört zur aktuellen Suchebene. Die aktuelle Stellung ist also schon
			// gespeichert. Deshalb muss die Stellung nicht gespeichert werden und es kann gleich
			// zurückgesprungen werden.
			// Was ist wenn die gleiche Stellung jetzt eine neue  Klassifizierung gespeichert werden soll ?
			// -> In diesem Fall wird die Klassifzierung direkt im Objekt gesetzt und nicht über
			// diese Speichermethode.
			// (siehe "isACorralDeadlock")
			if (oldCorralBoardPosition.getCorralNo() == boardPosition.getCorralNo()) {
				return oldCorralBoardPosition;
			}

			// Die Corralstellung gehört zum aktuellen Hauptcorral.
			// Denn der Fall, dass es eine Stellung zu einem alten Hauptcorral
			// ist, wurde weiter oben bereits abgefangen (-> "< g_NrHauptcorral")
			// (Hauptcorral = das erste Corral, was erkannt wird während der
			// Corralanalyse. Beim Versuch dieses Hauptcorral zu durchbrechen
			// können dann Subcorrals entstehen, die dann in tieferliegenden
			// Suchebenen analysiert werden).
			// Die alte Stellung ist eine also eine Stellung, die in einer höheren
			// Suchebene bereits erreicht wurde.
			// Diese höhere Ebene kann bereits klassifiziert worden sein!
			// Dieser Fall tritt z.B. ein, wenn das gleiche Subcorral mehrmals
			// während der Suche erreicht wird und bereits beim ersten mal
			// als Deadlock klassifiziert wurde.
			if (oldCorralBoardPosition.getCorralNo() < boardPosition.getCorralNo()) {
				// Falls die alte Stellung bereits klassifiziert ist,
				// so reicht es die neue Corralnr zu setzen (wenn dies überhaupt
				// notwendig ist ?!)
				if (oldCorralBoardPosition.isClassified()) {
					oldCorralBoardPosition.setCorralNo(boardPosition.getCorralNo());
				} else {
					// Es darf nicht einfach die neue Corralnr gesetzt werden, denn sonst würde
					// dieses Corral ja plötzlich zum aktuellen Corral gehören und für das Corral
					// in der höheren Ebene gälte es nicht mehr als schon erreicht!
					put(boardPosition, boardPosition);
				}

				return oldCorralBoardPosition;
			}

			// Die alte Stellung stammt von einem Corral in einer tieferen Ebene
			if (oldCorralBoardPosition.getCorralNo() > boardPosition.getCorralNo()) {
				put(boardPosition, boardPosition);
				if (oldCorralBoardPosition.isCorralDeadlock()) {
					boardPosition.setCorralDeadlock();
				}

				// Eine vorhandene NichtDeadlock Klassifizierung wird
				// immer übernommen, obwohl nicht sicher ist, dass das
				// aktuelle Corral auf der höheren Ebene wirklich gleich
				// groß ist! Wenn es größer ist, kann es ja sein, dass die
				// gleiche Stellung doch ein Deadlock für dieses größere
				// Corral ist.
				// Dieser besondere Fall wird in "löseCorral" behandelt,
				// so dass hier einfach die Klassifizierung übernommen
				// werden kann.
				if (oldCorralBoardPosition.isNotCorralDeadlock()) {
					boardPosition.setNotCorralDeadlock();
				}

				return oldCorralBoardPosition;
			}

			if (Debug.isDebugModeActivated) {
				System.out.println("This line should never be reached");
			}

			return oldCorralBoardPosition;
		}

		/**
		 * Returns the board position stored under the passed key.
		 * <p>
		 * This is equal to {@link #get(Object)}. It's an own method
		 * because there is also an extra method to store; additionally
		 * this extra method can be used to find all calls in JSoko
		 * to get a board position from this storage using Eclipse tools.
		 *
		 * @param boardPosition  the "key" to return the board position for
		 * @return the stored board position
		 */
		public CorralBoardPosition getBoardPosition(CorralBoardPosition boardPosition) {
			return get(boardPosition);
		}

		/**
		 * Debug method: prints statistics about the stored board positions.
		 */
		public void debugShowStatistic() {

			int deadlocksFound = 0;
			int consideredNotADeadlock = 0;
			int beingAnalyzed = 0;
			int errors = 0;

			Collection<CorralBoardPosition> collection = values();
			for (CorralBoardPosition bp : collection) {
				if(bp.isBeeingAnalyzed()) {
					beingAnalyzed++;
				}
				if(bp.isCorralDeadlock()) {
					deadlocksFound++;
				}
				if(bp.isNotCorralDeadlock()) {
					consideredNotADeadlock++;
				}
				if(bp.isCorralDeadlock() && bp.isNotCorralDeadlock()) {
					errors++;
				}
			}

			System.out.println("\n\ncorral storage statistics");
			System.out.println("-------------------------\n");
			System.out.println("Number of stored board positions: " + size());
			System.out.println("Number of deadlocks:      " + deadlocksFound);
			System.out.println("Number of not deadlocks:  " + consideredNotADeadlock);
			System.out.println("Number of being analyzed: " + beingAnalyzed);
			if(errors > 0) {
				System.out.println("Number of errors: "+errors);
			}
			System.out.println("-------------------------\n");
		}
	}
}
