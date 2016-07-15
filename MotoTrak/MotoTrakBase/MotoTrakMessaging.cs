using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoTrakBase
{
    /// <summary>
    /// This class handles messages that are logged throughout the executing of MotoTrak code.
    /// These messages could be displayed to the user or dumped to a log file.
    /// 
    /// This class is implemented as a singleton so that it can implement the INotifyPropertyChanged
    /// interface, allowing it to notify listeners of when the message log has been updated.
    /// We cannot do this with a static class, hence a singleton class was chosen instead.
    /// </summary>
    public class MotoTrakMessaging : NotifyPropertyChangedObject
    {
        #region Singleton

        private static volatile MotoTrakMessaging _instance = null;
        private static Object _instance_lock = new object();

        private MotoTrakMessaging()
        {
            //empty
        }

        /// <summary>
        /// Returns the singleton instance of the messaging system.
        /// </summary>
        /// <returns>The singleton instance of this class.</returns>
        public static MotoTrakMessaging GetInstance ()
        {
            if (_instance == null)
            {
                lock (_instance_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new MotoTrakMessaging();
                    }
                }
            }

            return _instance;
        }

        #endregion

        #region Private properties

        private SynchronizedCollection<string> _messages = new SynchronizedCollection<string>();

        #endregion

        #region Public properties

        /// <summary>
        /// The private property that we use for the messages log.
        /// This is a SynchronizedCollection, so it should have all locking mechanisms built into it
        /// and it should be thread-safe.
        /// </summary>
        private SynchronizedCollection<string> Messages
        {
            get
            {
                return _messages;
            }
            set
            {
                _messages = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds a new message to the log
        /// </summary>
        /// <param name="msg">The message to be added to the log</param>
        public void AddMessage ( string msg )
        {
            //Make sure the msg being passed in is not null or empty
            if (!string.IsNullOrEmpty(msg))
            {
                //Add the message to the synchronized collection
                Messages.Add(msg);

                //Notify any listeners
                NotifyPropertyChanged("Messages");
            }
        }

        /// <summary>
        /// Clears all the messages in the log.
        /// </summary>
        public void ClearMessages ()
        {
            //Clear the synchronized collection
            Messages.Clear();
            
            //Notify any listeners
            NotifyPropertyChanged("Messages");
        }

        /// <summary>
        /// Returns a List object containing all messages in the log.
        /// </summary>
        /// <returns>A list of all messages</returns>
        public List<string> RetrieveAllMessages ()
        {
            try
            {
                //Convert the synchronized collection to a list and return it
                List<string> result = Messages.ToList();
                return result;
            }
            catch
            {
                //In the event of an error, simply return an empty list of no messages
                return new List<string>();
            }
        }

        /// <summary>
        /// Returns the most recent message in the log
        /// </summary>
        /// <returns>The most recent message</returns>
        public string RetrieveLastMessage ()
        {
            //Retrieve the latest message and return it.  If no messages exist, 
            //then this should return a string.Empty.
            string result = Messages.LastOrDefault();
            return result;
        }

        /// <summary>
        /// Returns the number of messages currently in the log
        /// </summary>
        /// <returns>The number of messages in the log</returns>
        public int RetrieveMessageCount ()
        {
            return Messages.Count;
        }

        #endregion
    }
}
