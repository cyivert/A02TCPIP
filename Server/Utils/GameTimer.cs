/*
* FILE : GameTimer.cs
* PROJECT : A02 TCPIP
* PROGRAMMER : Cy Iver Torrefranca
* DESCRIPTION :
* Wrapper for Stopwatch to manage game timing with cleaner interface.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordGameServer.Utils
{
    //
    // CLASS : GameTimer
    // DESCRIPTION :
    // This class serves as a wrapper around the Stopwatch class to provide a cleaner and more intuitive interface for managing game timing.
    // PARAMETERS : n/a
    // RETURNS : n/a
    //
    public class GameTimer
    {
        private readonly Stopwatch stopwatch;
        private readonly int durationSeconds;

        //
        // CONSTRUCTOR : GameTimer
        // DESCRIPTION : Initializes a new instance of the GameTimer class with a specified duration in seconds. The timer starts immediately upon creation.
        // PARAMETERS : 
        // int durationSeconds - The total duration of the timer in seconds. This value determines how long the timer will run before it is considered expired.
        // RETURNS : n/a
        //
        public GameTimer(int durationSeconds)
        {
            this.stopwatch = new Stopwatch();
            this.durationSeconds = durationSeconds;
            this.stopwatch.Start();

            return;
        }

        //
        // METHOD : GetElapsedSeconds 
        // DESCRIPTION :
        // Retrieves the total elapsed time in seconds since the timer was started.
        // This method calculates the elapsed time by accessing the Stopwatch's Elapsed property and converting it to total seconds.
        // PARAMETERS : n/a
        // RETURNS : n/a
        //
        public double GetElapsedSeconds()
        {
            double elapsedSeconds = 0.0;

            elapsedSeconds = this.stopwatch.Elapsed.TotalSeconds;

            return elapsedSeconds;
        }

        //
        // METHOD : GetRemainingSeconds
        // DESCRIPTION :
        // This method calculates the remaining time in seconds before the timer expires.
        // It does this by subtracting the elapsed time from the total duration and ensuring that the result is not negative (i.e., it returns 0 if the timer has already expired).
        // PARAMETERS : n/a
        // RETURNS : n/a
        //
        public int GetRemainingSeconds()
        {
            int remaining = 0;
            double elapsed = 0.0;
            int calculated = 0;

            elapsed = this.GetElapsedSeconds();
            calculated = this.durationSeconds - (int)elapsed;
            remaining = Math.Max(0, calculated);

            return remaining;
        }

        //
        // METHOD : IsExpired
        // DESCRIPTION :
        // Determines whether the timer has expired by comparing the elapsed time to the total duration.
        // If the elapsed time is greater than or equal to the duration, the timer is considered expired.
        // PARAMETERS : n/a
        // RETURNS : n/a
        //
        public bool IsExpired()
        {
            bool expired = false;
            double elapsed = 0.0;

            elapsed = this.GetElapsedSeconds();
            expired = elapsed >= this.durationSeconds;

            return expired;
        }

        //
        // METHOD : Stop
        // DESCRIPTION :
        // Stops the timer by calling the Stop method on the underlying Stopwatch instance.
        // This will halt the timer and allow you to retrieve the elapsed time up to the point when it was stopped.
        // PARAMETERS : 
        // RETURNS :
        //
        public void Stop()
        {
            this.stopwatch.Stop();

            return;
        }

        //
        // METHOD : Restart
        // DESCRIPTION :
        // Restarts the timer by calling the Restart method on the underlying Stopwatch instance.
        // This will reset the elapsed time to zero and start the timer again immediately.
        // PARAMETERS : n/a
        // RETURNS : n/a
        //
        public void Restart()
        {
            this.stopwatch.Restart();

            return;
        }
    }

}
