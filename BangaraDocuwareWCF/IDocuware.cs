using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace BangaraDocuwareWCF
{
    // NOTA: puede usar el comando "Rename" del menú "Refactorizar" para cambiar el nombre de interfaz "IDocuware" en el código y en el archivo de configuración a la vez.
    [ServiceContract]
    public interface IDocuware
    {
        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/GetFiles/{EmpId}", RequestFormat =WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json, BodyStyle =WebMessageBodyStyle.Wrapped)]
        string GetFiles(string EmpId);
    }
}
