using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace WCFSLibrary
{
    [DataContract]
    public class TimeoutFault
    {
        [DataMember]
        public String Message { get; set; }
        public TimeoutFault(String message)
        {
            Message = message;
        }
    }

    [DataContract]
    public class AllocationData
    {
        [DataMember]
        public Double Energy { get; set; }
        [DataMember]
        public int Count { get; set; }
        [DataMember]
        public int[] Map { get; set; }
    }

    [DataContract]
    public class ConfigData
    {
        [DataMember]
        public Double Duration { get; set; }
        [DataMember]
        public int NumberOfTasks { get; set; }
        [DataMember]
        public int NumberOfProcessors { get; set; }
        //[DataMember]
        //public String Path { get; set; }
        [DataMember]
        public Double[] TaskRuntimes { get; set; }
        [DataMember]
        public Double[] AllocatedRuntimes { get; set; }
        [DataMember]
        public int[] InvalidAllocations { get; set; }
        [DataMember]
        public Double[] Energies { get; set; }
    }
}
