using MotoTrakBase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MotoTrakCalibration
{
    /// <summary>
    /// A model class that handles all device streaming for calibration purposes
    /// </summary>
    public class DeviceStreamModel : NotifyPropertyChangedObject
    {
        #region Singleton Instance

        private static DeviceStreamModel _instance = null;
        private static object _instance_lock = new object();

        private DeviceStreamModel ()
        {
            //empty
        }

        /// <summary>
        /// Returns the singleton instance
        /// </summary>
        public static DeviceStreamModel GetInstance ()
        {
            if (_instance == null)
            {
                lock(_instance_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new DeviceStreamModel();
                    }
                }
            }

            return _instance;
        }

        #endregion

        #region The buffer

        private SynchronizedCollection<double> _buffer = new SynchronizedCollection<double>();

        /// <summary>
        /// The data buffer containing all incoming signal data
        /// </summary>
        public SynchronizedCollection<double> DataBuffer
        {
            get
            {
                return _buffer;
            }
            set
            {
                _buffer = value;
            }
        }

        public int BufferSize = 500;

        #endregion

        #region Background Thread stuff

        private BackgroundWorker _background_thread = new BackgroundWorker();

        /// <summary>
        /// Starts streaming from the motor board
        /// </summary>
        public void StartStreaming ()
        {
            //Clear the current serial buffer
            MotorBoard.GetInstance().ClearStream();

            //Set streaming parameters
            MotorBoard.GetInstance().SetStreamingPeriod(10);

            //Enable streaming on the motor board
            MotorBoard.GetInstance().EnableStreaming(1);
            
            //Start a background thread to collect the incoming data
            _background_thread.WorkerSupportsCancellation = true;
            _background_thread.WorkerReportsProgress = true;
            _background_thread.DoWork += _background_thread_DoWork;
            _background_thread.ProgressChanged += _background_thread_ProgressChanged; ;
            _background_thread.RunWorkerCompleted += _background_thread_RunWorkerCompleted; ;
            _background_thread.RunWorkerAsync();
        }

        /// <summary>
        /// Stops streaming from the motor board
        /// </summary>
        public void StopStreaming ()
        {
            if (_background_thread.IsBusy)
            {
                //Cancel the background thread
                _background_thread.CancelAsync();
            }
        }

        private void _background_thread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //This function runs when the background thread has finished or has been cancelled.

            //Disable streaming on the motor board
            MotorBoard.GetInstance().EnableStreaming(0);

            //Read in any data sitting on the buffer
            MotorBoard.GetInstance().ClearStream();
        }

        private void _background_thread_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //Notify the UI that the data buffer has been updated
            NotifyPropertyChanged("DataBuffer");
        }

        private void _background_thread_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!_background_thread.CancellationPending)
            {
                //Get the new stream data from the motor board
                var new_stream_data = MotorBoard.GetInstance().ReadStream();

                //Isolate out the device signal
                var new_signal_data = new_stream_data.Select(x => x[1]).ToList();

                //Add the new device data to the buffer
                foreach (var n in new_signal_data)
                {
                    DataBuffer.Add(n);
                }

                //Remove old data values to make sure the buffer does not get bigger than its intended size
                while (DataBuffer.Count > BufferSize)
                {
                    DataBuffer.RemoveAt(0);
                }

                //Report new data to the UI if there was some that was read in
                if (new_stream_data.Count > 0)
                {
                    _background_thread.ReportProgress(0);
                }

                //Sleep the thread for awhile so we don't consume the CPU
                Thread.Sleep(33);
            }

            //Indicate that we have handled the event
            e.Cancel = true;
        }

        #endregion
    }
}
