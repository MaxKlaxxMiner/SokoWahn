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
package de.sokoban_online.jsoko.utilities;

import de.sokoban_online.jsoko.resourceHandling.Settings;


/**
 * In this class we manage sequences of delays, which are used to
 * create an observable motion of the player and/or packets.
 * Between the calls to {@link Thread#sleep(long)} we expect some time to be
 * consumed by display of graphics and possibly also by computations in the model.
 * Hence, real time calculations are used, to not sleep that amount of time,
 * which has been wasted, already, by other means.
 * <p>
 * A <code>Delays</code> object has two major parameters:
 * a basic delay time (in milliseconds),
 * and the intended length of the sequence of delay steps.
 * <p>
 * The sequence length (if positive) is used to reduce the effective delays
 * to reduce the linear increase of total time of long sequences.
 * 
 * @author Heiner Marxen
 */
public class Delays {

	/**
	 * The intended delay step in milliseconds.
	 * Should be nonnegative.
	 */
	// FFS: use float for more accurate computations?
	private int stepMillis;
	
	/**
	 * The intended count of steps in this series of delays.
	 * If positive (and large enough) we use it to somewhat reduce
	 * the delay steps for long series of moves.
	 */
	private int intendedLength;
	
	
	/**
	 * The last measured real time stamp.
	 */
	private long lastNow;
	
	
	/**
	 * Sets the basic delay step to be used, in milliseconds.
	 * 
	 * @param msStep delay time in milliseconds
	 */
	public void setStep(int msStep) {
		stepMillis = msStep;
	}
	
	/**
	 * Returns the current delay time in milliseconds.
	 * 
	 * @return current delay time in milliseconds
	 */
	public int getStep() {
		return stepMillis;
	}
	
	/**
	 * Sets the intended sequence length of delay steps.
	 * Positive values are used to somewhat reduce the total time of the
	 * sequence by reducing the effective single step delay time.
	 * 
	 * @param len intended sequence length of delay steps
	 */
	public void setLength(int len) {
		intendedLength = len;
	}
	
	/**
	 * Returns the current intended sequence length of delay steps.
	 * @return the current intended sequence length of delay steps
	 */
	public int getLength() {
		return intendedLength;
	}
	
	/**
	 * Creates a "dummy" object, with zero delay and no sequence length.
	 */
	public Delays() {
	}
	
	/**
	 * Creates an object with the specified delay step time,
	 * and the specified intended sequence length.
	 * 
	 * @param stepmillis step delay time in milliseconds
	 * @param len        intended sequence length of delay steps
	 */
	public Delays(int stepmillis, int len) {
		setStep(stepmillis);
		setLength(len);
		lastNow = System.currentTimeMillis();
	}
	
	
	/**
	 * Construct and return a new object for the standard step delay
	 * from the Settings.
	 * 
	 * @return new standard <code>Delays</code> object
	 */
	static public Delays makeDelayNormal() {
		return makeDelayNormal(0);
	}
	
	/**
	 * Construct and return a new object for the standard step delay
	 * from the Settings, and the indicated intended sequence length.
	 * 
	 * @param seqLength intended length of the sequence
	 * @return new standard <code>Delays</code> object
	 */
	static public Delays makeDelayNormal(int seqLength) {
		return new Delays(Settings.delayValue, seqLength);
	}
	
	/**
	 * Construct and return a new object for the undo/redo step delay
	 * from the Settings, and the indicated intended sequence length.
	 * 
	 * @return new undo/redo <code>Delays</code> object
	 */
	static public Delays makeDelayUndoRedo() {
		return makeDelayUndoRedo(0);
	}
	
	/**
	 * Construct and return a new object for the undo/redo step delay
	 * from the Settings, and the indicated intended sequence length.
	 * 
	 * @param seqLength intended length of the sequence
	 * @return new undo/redo <code>Delays</code> object
	 */
	static public Delays makeDelayUndoRedo(int seqLength) {
		return new Delays(Settings.delayValueUndoRedo, seqLength);
	}
	

	//=========================================================================
	// The following debug reporting should be generalized into a kind of logger 

	private static int	debugCnt = 0;
	
	private final boolean isLocalDebug() {
		return Debug.isDebugModeActivated && Debug.debugShowDelays;
	}
	
	private StringBuilder makeDebugPrefix(String funcname) {
		StringBuilder sb = new StringBuilder();
		
		sb.append("Delays ");
		sb.append(++debugCnt);
		if ((funcname != null) && !funcname.isEmpty()) {
			sb.append(" ");
			sb.append(funcname);
			sb.append("()");
		}
		sb.append(": ");
		return sb;
	}
	
	private final void shoDebug(String funcname, String line) {
		if ((line != null) && !line.isEmpty()) {
			StringBuilder sb = makeDebugPrefix(funcname);
			sb.append(line);
			System.out.println( sb.toString() );
		}
	}
	
	private final void shoDebug(String line) {
		shoDebug(null, line);
	}
	
	private final void condShoSL() {
		if (isLocalDebug()) {
			shoDebug(  "stepMillis=" + stepMillis
					 + " intendedLength=" + intendedLength );
		}
	}
	
	//=========================================================================
	/* Computation of effective delay step.
	 * 
	 * For a non-positive value of the intended sequence length of delay steps
	 * we need not compute anything: we just use the nominal delay.
	 * These reduction considerations are for positive values of the
	 * intended length of the sequence.
	 * 
	 * Letting the total time of the sequence increase linearly with the
	 * length of the sequence is ok for short sequences, but for longer
	 * sequences the user becomes impatient.  As an example lets assume
	 * the user has chosen a small delay time like 20 ms (speed 230).
	 * A sequence length of 100 steps (what is quite possible in larger
	 * levels), already results in a total time of 2000 ms = 2 seconds.
	 * 
	 * The classical approach (until Feb-2011, version 1.61) was to reduce
	 * the single step delay by (intendedLength / 10).  On a second look
	 * that is not really that good, since now the total time depends
	 * on the intendedLength squared and negated (!).
	 * 
	 * L = intendedLength
	 * D = stepMillis
	 * 
	 * T = L * (D - L/10)
	 *   = L*D - L^2/10
	 * 
	 * Numerically the total time drops into negative values.
	 * 
	 * Ok, the question is, in what way do we reduce the total time
	 *     T = L * D
	 * such that it still is monotone in L?
	 * 
	 * Let us try an approach where T(L) is O(sqrt(L)), i.e. T increases
	 * with L, not linearly as O(L^1), but rather as O(L^0.5).
	 * We derive the mathematical formula for such a computation.
	 * We search for a function
	 * 
	 *  R(L) = reduced length <= L * D
	 *  R(L) ~ O(sqrt(L))
	 * 
	 * For small values of L we want to be approximately linear.  Hence:
	 * 
	 *  R(0)  = 0
	 *  R'(0) = 1
	 * 
	 * Note: when we want to anchor this at 1 instead of 0, we just shift
	 * the argument L by one:  h(L) = 1 + R(L-1)  and fix h(0) = 0.
	 * 
	 * For the inverse function L(R) the conditions are similar:
	 *  L(0)  = 0
	 *  L'(0) = 1
	 * but computation with this O(R^2) function is easier. In general:
	 *  L(R)  = a*R^2 + b*R + c
	 *  L'(R) = 2a*R  + b
	 * 
	 * From L(0)  = 0 we have a*0^2 + b*0 + c = 0  <==>  c=0
	 * From L'(0) = 1 we have 2a*0 + b = 1         <==>  b=1
	 * Hence:
	 *  L(R)  = a*R^2 + R  =  R(aR + 1)
	 * leaving one free parameter "a".
	 * 
	 * In order to determine the free parameter "a" we fix another point
	 * of the function R, e.g. R(10) = 9.  More generally:
	 *  R(M) = N              with 1 <= N <= M
	 * We want the sequence length M to be handled as if it were N, only.
	 * 
	 * In terms of L(R) we now have:
	 *       L(N) = M
	 *  <==> a*N^2 + N = M
	 *  <==> a = (M - N) / N^2
	 * That completely fixes the function L(R), and indirectly also R(L):
	 * 
	 *  a     = (M - N) / N^2
	 *  L(R)  = a*R^2 + R
	 * 
	 * With u = 1 / (2a) ...
	 * 
	 *  L/a = R^2 + (1/a)R
	 *  2uL + u^2 = R^2 + 2uR + u^2 = (R + u)^2
	 *  R + u = sqrt( 2uL + u^2 )
	 * 
	 *  R(L) = -u + sqrt( 2uL + u^2 )
	 * 
	 * With u = N^2 / (2(M-N)) we yield:
	 * 
	 *            N^2           N^2*L     N^4
	 *  R(L) = - ------ + sqrt( ----- + -------- )
	 *           2(M-N)          M-N    4(M-N)^2
	 * 
	 * Better use the first formula with "u".
	 * 
	 * Here are some results for M=10 and N=9:
	 *   0 -->  0
	 *   1 -->   .99
	 *   2 -->  1.96
	 *   3 -->  2.91
	 *   4 -->  3.85
	 *   5 -->  4.77
	 *  10 -->  9.16
	 *  15 --> 13.24
	 *  20 --> 17.08
	 *  30 --> 24.16
	 *  50 --> 36.60
	 * 100 --> 61.80
	 * 150 --> 82.28
	 * 
	 * Here are some results for M=5 and N=4:
	 *   0 -->  0
	 *   1 -->   .94
	 *   2 -->  1.80
	 *   3 -->  2.59
	 *   4 -->  3.33
	 *   5 -->  4.02
	 *  10 -->  7.03
	 *  15 -->  9.53
	 *  20 --> 11.73
	 *  30 --> 15.52
	 *  50 --> 21.70
	 * 100 --> 33.32
	 * 150 --> 42.34
	 * 
	 * I have tested / experimented with this using Haikemono level #28.
	 */
	
	/*
	 * Additionally, we want the delays before pushes to be somewhat longer
	 * than normal delays, as computed up to now.  This shall reflect
	 * (a) the impression of harder work to do
	 * (b) the increase mental effort to follow a box movement together
	 *     with the movement of the player.
	 * Currently we just add 30%.
	 */
	
	/**
	 * Based on the current values of step delta and sequence length
	 * compute the value for one effective step in a mathematical sense,
	 * i.e. we compute a <code>float</code> value.
	 * 
	 * @param slowstep whether this delay shall be extra large
	 * @return current effective step in milliseconds
	 */
	private float effStep( boolean slowstep ) {
		/*
		 * FFS/hm: configuration of argOff (0 or 1), and of M and N
		 * We could fix N = M-1, and let the user choose M>=2 (or M=0)
		 */
		final int argOff = 0;
		final int M = 5;
		final int N = 4;
		
		// Pushes shall be slower, and hence have a longer delay.
		final float slowFactor = 1.30f;
		
		float step = stepMillis;
		
		condShoSL();
		
		if (step > 0.0f) {
			// There is something we may want to reduce
			
			if (intendedLength > argOff && (N > 0)) {
				// This was the classic / naive reduction
				//step -= ((float)intendedLength) / 10.0;
				
				// See the large comment above.
				final float fM = M;
				final float fN = N;
				final float a  = (fM - fN) / (fN * fN);
				final float u  = 1.0f / (2.0f * a);
				final float fL = intendedLength - argOff;
				float fR;
				
				fR  = -u + (float) Math.sqrt( 2.0f*u*fL + u*u );
				fR += argOff;
				step = step * fR / (float)intendedLength;
				
				if (isLocalDebug()) {
					shoDebug("effStep",  "iL=" + intendedLength + " oL=" + fR
							           + " step="+step);
				}
			}
		}
		
		// We apply the "slow" logic after the complicated computation,
		// and change the result in a way, that is independent of the
		// intended length.
		if (slowstep && (slowFactor > 0)) {
			step *= slowFactor;
		}
		
		// Be sure not to tell negative values
		if( step < 0 ) {
			step = 0;
		}

		return step;
	}
	
	/**
	 * Based of the mathematical value from {@link #effStep(boolean)}
	 * we compute a meaningful approximation,
	 * and return an <code>int</code> value.
	 * 
	 * @param slowstep whether this delay shall be extra large
	 * @return integral approximation of current delay in milliseconds
	 */
	private int effStepInt( boolean slowstep ) {
		float step = effStep(slowstep);
		int  istep = Math.round(step);
		
		// When we would tell a zero delay (or even less), but the original
		// intention was a positive delay, we shall return the smallest
		// possible positive value: 1.
		if( istep <= 0 ) {
			if( stepMillis > 0 ) {
				istep = 1;
			}
		}
		
		// Never consider negative values
		if( istep < 0 ) {
			istep = 0;
		}
		
		return istep;
	}
	
	//=========================================================================

	/**
	 * We are going to wait somewhat to create an observable motion.
	 * If the wait amount is zero, we do not even call {@link Thread#sleep(long)}.
	 * 
	 * @param reassertIntr whether a possible <code>InterruptedException</code>
	 *                      is to be reasserted (<em>not</em> rethrown).
	 *                      Else it is ignored.
	 * @param slowstep whether this delay shall be extra large
	 */
	public void sleep(boolean reassertIntr, boolean slowstep) {
		
		// In the case of the a nominal zero delay, we will not really do any
		// effect, and hence need not even to determine "now".
		if( stepMillis <= 0 ) {
			return;
		}
				
		// Some delay may have happened, already.  We determine "now"...
		final long now  = System.currentTimeMillis();
		
		// Calculate the current effective step value
		final int  step = effStepInt(slowstep);
		
		// Based on the last time stamp find the intended next one,
		// and look how much there is still to wait to reach that next one
		final long next = lastNow + step;
		final long msDelay = next - now;
		
		// Check, whether there is left any positive delay to be done.
		if (msDelay > 0) {
			try {
				Thread.sleep(msDelay);
			} catch (InterruptedException e) {
				if (reassertIntr) {
					Thread.currentThread().interrupt();
				}
			}
			
			// Setup a new basis for the next delay.
			//lastNow = next;
			lastNow = System.currentTimeMillis();
		} else {
			// We do not wait just now, but we have to prepare the next step
			lastNow = now;
		}
		
		if (isLocalDebug()) {
			shoDebug("sleep", "len=" + intendedLength + ", msDelay=" + msDelay);
		}
	}
	
	/**
	 * We are going to wait somewhat to create an observable motion.
	 * If the wait amount is zero, we do not even call {@link Thread#sleep(long)}.
	 * 
	 * @param reassertIntr whether a possible <code>InterruptedException</code>
	 *                      is to be reasserted (<em>not</em> rethrown).
	 *                      Else it is ignored.
	 */
	public void sleep(boolean reassertIntr) {
		sleep(reassertIntr, false);
	}
}
