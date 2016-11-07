using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// This class enapsulated functionality of the autopositioner into one object.
    /// </summary>
    public class MotoTrakAutopositioner
    {
        #region Singleton

        private static MotoTrakAutopositioner _instance = null;

        private MotoTrakAutopositioner()
        {
            //empty
        }

        /// <summary>
        /// Get the only instance of the MotoTrakAutopositioner that will be allowed.
        /// </summary>
        /// <returns>The MotoTrakAutopositioner instance</returns>
        public static MotoTrakAutopositioner GetInstance()
        {
            if (_instance == null)
            {
                _instance = new MotoTrakAutopositioner();
            }

            return _instance;
        }

        #endregion

        #region Private data members

        private Queue<double> _positions_to_visit = new Queue<double>();
        private DateTime _most_recent_move_time = DateTime.MinValue;
        private double _time_to_wait_inbetween_moves = 5.0;

        #endregion

        #region Public properties

        /// <summary>
        /// Indicates whether there are elements in the queue that the autopositioner needs to move to
        /// </summary>
        public bool ContainsElementsInQueue
        {
            get
            {
                return (_positions_to_visit.Count > 0);
            }
        }

        /// <summary>
        /// Indicates whether the autopositioner is ready to move to the next position in the queue
        /// </summary>
        public bool ReadyToMove
        {
            get
            {
                bool enough_time_elapsed = DateTime.Now >= (_most_recent_move_time.AddSeconds(_time_to_wait_inbetween_moves));
                return enough_time_elapsed;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Adds a position to the queue for the autopositioner to visit.
        /// </summary>
        /// <param name="new_position">The position that should be visited by the autopositioner.</param>
        public void SetPosition ( double new_position )
        {
            _positions_to_visit.Enqueue(new_position);
        }

        /// <summary>
        /// Run through the autopositioner code.  If there has been enough time (default N = 5 seconds) since the last move
        /// of the autopositioner, it pops the next element from the queue and moves to that position.
        /// </summary>
        public void RunAutopositioner ( )
        {
            var board = MotorBoard.GetInstance();
            bool enough_time_elapsed = DateTime.Now >= (_most_recent_move_time.AddSeconds(_time_to_wait_inbetween_moves));
            if (enough_time_elapsed)
            {
                _most_recent_move_time = DateTime.Now;
                if (_positions_to_visit.Count > 0)
                {
                    var new_position_to_set = _positions_to_visit.Dequeue();
                    double actual_position = Math.Round(10 * (board.AutopositionerOffset - 10 * new_position_to_set));
                    board.Autopositioner(actual_position);
                }
            }
        }

        /// <summary>
        /// Resets the autopositioner
        /// </summary>
        public void ResetAutopositioner ()
        {
            //Clear the list of positions to visit
            _positions_to_visit.Clear();

            //Set the most recent move time to now
            _most_recent_move_time = DateTime.Now;

            //Set the autopositioner to the zero-point
            MotorBoard.GetInstance().Autopositioner(0);
        }

        #endregion
    }
}
