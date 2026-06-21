using System;
using System.Collections.Generic;

namespace Thesis.Modules.Practice
{
    [Serializable]
    public class PracticeTaskConfig
    {
        public string gesture_type;
        public float hold_time;
        public float tolerance;
        public float rhythm_speed;
        public int sequence_len;
        public int hint_intensity;
    }

    [Serializable]
    public class PracticeTaskConfigFile
    {
        public string version;
        public List<PracticeTaskConfig> tasks;
    }
}
