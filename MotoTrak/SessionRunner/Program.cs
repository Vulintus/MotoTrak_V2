using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SessionRunner
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Select a file to analyze";
            dialog.Filter = "MotoTrak File|*.MotoTrak";
            
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string file_name = dialog.FileName;

                MotoTrakBase.MotoTrakConfiguration.GetInstance().ReadConfigurationFile();
                MotoTrakBase.MotoTrakConfiguration.GetInstance().InitializeStageImplementations();
                
                var session = MotoTrakBase.MotoTrakFileRead.ReadFile(file_name);

                var stage_impl = MotoTrakBase.MotoTrakConfiguration.GetInstance().PythonStageImplementations["PythonPullStageImplementation_FWIR.py"];
                var p_stage_impl = stage_impl as MotoTrakBase.PythonTaskImplementation;

                //Iterate over all trials
                for (int t = 0; t < session.Trials.Count; t++)
                {
                    if (session.Trials[t].Result == MotoTrakBase.MotorTrialResult.Miss)
                    {
                        var upper_bound = session.Trials[t].QuantitativeParameters["Upper bound force threshold"];
                        var lower_bound = session.Trials[t].QuantitativeParameters["Lower bound force threshold"];
                        var initiation_thresh = session.Trials[t].QuantitativeParameters["Initiation Threshold"];

                        session.SelectedStage.StageParameters["Upper bound force threshold"].CurrentValue = upper_bound;
                        session.SelectedStage.StageParameters["Lower bound force threshold"].CurrentValue = lower_bound;
                        session.SelectedStage.StageParameters["Initiation Threshold"].CurrentValue = initiation_thresh;
                        session.SelectedStage.StageParameters["Use upper force boundary"].NominalValue = "Yes";

                        var events_found = p_stage_impl.CheckForTrialEvent(session.Trials[t], 1, session.SelectedStage);
                        var trial_events_found = events_found.Select(x => x.Item1).ToList();
                        string new_result = "MISS";
                        if (trial_events_found.Contains(MotoTrakBase.MotorTrialEventType.SuccessfulTrial))
                        {
                            new_result = "HIT";
                        }

                        System.Console.WriteLine("Trial " + (t + 1).ToString() + ", Saved result = MISS, New result = " + new_result);
                    }
                }
                
            }

            //Wait for the user to press a key before exiting the program.
            System.Console.WriteLine("Press any key to finish.");
            System.Console.ReadKey();
        }
    }
}
