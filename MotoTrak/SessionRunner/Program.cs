using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SessionRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            MotoTrakBase.MotoTrakConfiguration.GetInstance().ReadConfigurationFile();
            MotoTrakBase.MotoTrakConfiguration.GetInstance().InitializeStageImplementations();

            string file_name = @"C:\Users\dtp110020\Desktop\539_20170330T170616_P3NS.MotoTrak";
            var session = MotoTrakBase.MotoTrakFileRead.ReadFile(file_name);
            session.SelectedStage.StageParameters["Upper bound force threshold"].CurrentValue = 20;
            session.SelectedStage.StageParameters["Lower bound force threshold"].CurrentValue = 5;
            session.SelectedStage.StageParameters["Initiation Threshold"].CurrentValue = 3;
            session.SelectedStage.StageParameters["Use upper force boundary"].NominalValue = "Yes";
            
            var stage_impl = MotoTrakBase.MotoTrakConfiguration.GetInstance().PythonStageImplementations["PythonPullStageImplementation_FWIR.py"];
            var p_stage_impl = stage_impl as MotoTrakBase.PythonStageImplementation;

            var events_found = p_stage_impl.CheckForTrialEvent(session.Trials[8], 1, session.SelectedStage);

        }
    }
}
