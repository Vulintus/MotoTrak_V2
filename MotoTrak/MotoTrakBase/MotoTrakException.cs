using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MotoTrakBase
{
    /// <summary>
    /// A quick exception class to notify of any problems we have.
    /// </summary>
    public class MotoTrakException : Exception
    {
        public MotoTrakExceptionType ExceptionType = MotoTrakExceptionType.Unknown;

        public MotoTrakException() : base() { }
        public MotoTrakException(MotoTrakExceptionType t, string message) : base(message) { ExceptionType = t; }
        public MotoTrakException(MotoTrakExceptionType t, string message, System.Exception inner) : base(message, inner) { ExceptionType = t; }

        // A constructor is needed for serialization when an 
        // exception propagates from a remoting server to the client.  
        protected MotoTrakException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        { }
    }
}
