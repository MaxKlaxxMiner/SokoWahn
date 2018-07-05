package de.sokoban_online.jsoko.workInProgress;

import java.util.ArrayList;
import java.util.Arrays;

import de.sokoban_online.jsoko.board.Board;

public class BoxGoalsRatio {
	
	/** Constant indicating that a position doesn't belong to any "box area" on the board. */
	private static final int NONE = -1;
	
	private final Board board;
	private int[] areaNumbers;
	private int[] goalsInAreaCount;
	private int[] boxesInAreaCount;
	
	// TODO: comments
	public BoxGoalsRatio(Board board) {
		this.board = board;
		areaNumbers = new int[board.size];
		Arrays.fill(areaNumbers, NONE);
	}
	
	public void updateAreaData() {
	
		
		int[] reachableGoalPositions = new int[board.goalsCount];

		ArrayList<int[]> reachableGoalPositionsSet = new ArrayList<int[]>();
		
		int[] goalPositions = board.getGoalPositions();
		
		for(int position=board.firstRelevantSquare; position<board.lastRelevantSquare; position++) {
			if(!board.isWallOrIllegalSquare(position)) {
				
				int reachableGoalsCount = 0;
				for(int goalPosition : goalPositions) {
					if(board.distances.getBoxDistanceForwardsPosition(position, goalPosition) != Board.UNREACHABLE) {
						reachableGoalPositions[reachableGoalsCount++] = goalPosition;
					}
				}
				int[] reachableGoalPos = Arrays.copyOf(reachableGoalPositions, reachableGoalsCount);
				
				int areaNo = 0;
				for(int[] goalPositionsOfArea : reachableGoalPositionsSet) {
					if(Arrays.equals(goalPositionsOfArea, reachableGoalPos)) {
						break;
					}
					areaNo++;
				}
				if(areaNo == reachableGoalPositionsSet.size()) {
					reachableGoalPositionsSet.add(reachableGoalPos);
				}

				areaNumbers[position] = areaNo;				
			}
		}
		
		goalsInAreaCount = new int[reachableGoalPositionsSet.size()];
		boxesInAreaCount = new int[reachableGoalPositionsSet.size()];
		for(int position=board.firstRelevantSquare; position<board.lastRelevantSquare; position++) {
			int areaNumber = areaNumbers[position]; 
			if(areaNumber != NONE) {
				if(board.isGoal(position)) {
					goalsInAreaCount[areaNumber]++;
				}
				if(board.isBox(position)) {
					boxesInAreaCount[areaNumber]++;
				}
			}
		}		
	}
	
	public int[] getAreaNumbers() {
		return areaNumbers.clone();
	}
	
	public int getAreaNumber(int position) {
		return areaNumbers[position];
	}
}
