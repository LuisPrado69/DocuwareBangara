using DocuWare.Platform.ServerClient;
using System.Collections.Generic;
using ValidationWebAPI.Models;
using System.Web.Http;
using System;

namespace ValidationWebAPI.Controllers
{
    public class VerifyFilesController : ApiController
    {
        [System.Web.Http.HttpPost]
        public IHttpActionResult Post(DlgInfos dlgInfos)
        {
            PlatformClient platformClient = null;
            try
            {
                string dialogId = dlgInfos.DialogGuid;
                string FileCabinetId = dlgInfos.FileCabinetGuid;
                string OrganizationName = dlgInfos.OrganizationName;
                string UserName = dlgInfos.UserName;
                string userPassword = dlgInfos.userPassword;
                string serverUrl = dlgInfos.serverUrl;
                string FieldName = dlgInfos.FieldName;
                string DateValueStart = dlgInfos.DateValueStart;
                string DateValueEnd = dlgInfos.DateValueEnd;
                string message = "";
                List<string> result = new List<string>();
                platformClient = new PlatformClient(serverUrl, OrganizationName, UserName, userPassword);
                var query = new DialogExpression()
                {
                    Operation = DialogExpressionOperation.And,
                    Condition = new List<DialogExpressionCondition>()
                    {
                        DialogExpressionCondition.Create(FieldName, DateValueStart, DateValueEnd)
                    },
                    Count = 9999
                };
                var fieldList = platformClient.GetDocumentsByQuery(dlgInfos.FileCabinetGuid, false, query);
                if (fieldList.Count > 0)
                {
                    message = "Succesfully";
                    foreach (Document document in fieldList)
                    {
                        result.Add($"{document.Fields[14]}");
                    }
                }
                else
                {
                    message = "Not result";
                }
                return Json(new ValidationResponseModel(ValidationResponseStatus.OK, message, result));
            }
            catch (Exception ex)
            {
                return Json(CreateErrorResponse(ex));
            }
            finally
            {
                platformClient?.Logout();
            }
        }

        private ValidationResponseModel CreateErrorResponse(Exception ex)
        {
            return new ValidationResponseModel(ValidationResponseStatus.Failed, ex.Message, null);
        }
    }
}