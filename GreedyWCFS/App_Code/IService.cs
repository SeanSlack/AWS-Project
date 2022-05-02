using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using WCFSLibrary;

[ServiceContract]
public interface IService
{
	[OperationContract]
    [FaultContract(typeof(TimeoutFault))]
    AllocationData GetAllocationsGreedy(int deadline, ConfigData cd, int ID);
}
