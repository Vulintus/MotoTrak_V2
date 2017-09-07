using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace MotoTrakBase
{
    /// <summary>
    /// An interface that will be implemented by classes that define communication behavior with a MotoTrak
    /// controller
    /// </summary>
    public interface IMotorBoard
    {
        double AutopositionerOffset { get; set; }
        string ComPort { get; }
        bool IsSerialConnectionValid { get; }
        SerialPort SerialConnection { get; }

        void Autopositioner(double pos);
        int CalGrams();
        int CheckVersion();
        void ClearStream();
        bool ConnectToArduino(string portName);
        void DisconnectFromArduino();
        bool DoesSketchMeetMinimumRequirements();
        void EnableStreaming(int streamingMode);
        void Feed();
        int GetBaseline();
        int GetBoardDeviceValue();
        string GetBoothLabel();
        int GetFeedDuration();
        MotorDevice GetMotorDevice();
        int GetStimulusDuration();
        int GetStreamingPeriod();
        bool IsArduinoSketchValid();
        void KnobToggle(int toggle_value);
        int NPerCalGrams();
        int ReadDevice();
        List<List<Int64>> ReadStream();
        bool SerialConnectionHasCharactersToRead();
        void SetBaseline(int baseline);
        void SetBoothNumber(int boothNumber);
        void SetCalGrams(int cal_grams);
        void SetFeedDuration(int duration);
        void SetLights(int input);
        void SetNPerCalGrams(int n_per_cal_grams);
        void SetStimulusDuration(int duration);
        void SetStreamingPeriod(int period);
        void Stimulate();
        void TriggerFeeder();
        void TriggerStim();
    }
}