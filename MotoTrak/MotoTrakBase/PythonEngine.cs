using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using System.Collections.Generic;

namespace MotoTrakBase
{
    /// <summary>
    /// This is a singleton class that defines the Python scripting engine for MotoTrak.
    /// </summary>
    public class PythonEngine
    {
        #region Private data members

        private ScriptEngine _pythonEngine;

        #endregion

        #region Singleton class

        private static PythonEngine _instance = null;

        private PythonEngine()
        {
            Dictionary<string, object> options = new Dictionary<string, object>();
            options["Debug"] = true;

            PythonScriptingEngine = Python.CreateEngine(options);
        }

        public static PythonEngine GetInstance()
        {
            if (_instance == null)
            {
                _instance = new PythonEngine();
            }

            return _instance;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// The python scripting engine
        /// </summary>
        public ScriptEngine PythonScriptingEngine
        {
            get
            {
                return _pythonEngine;
            }
            private set
            {
                _pythonEngine = value;
            }
        }

        #endregion
    }
}
