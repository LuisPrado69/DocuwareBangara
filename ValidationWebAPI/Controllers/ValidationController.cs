using DocuWare.Platform.ServerClient;
using System.Collections.Generic;
using ValidationWebAPI.Models;
using System.Web.Http;
using System;

namespace ValidationWebAPI.Controllers
{
    public class ValidationController : ApiController
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
                string FieldValue = dlgInfos.FieldValue;
                string message = "";
                string locationResult = @"/Users/crifa/code/score/public/" + FieldValue;
                // string locationResult = @"C:/Users/LUCHO/Documents/Docuware/" + FieldValue;
                List<string> resultFolders = new List<string>();
                List<string> result = new List<string>();
                platformClient = new PlatformClient(serverUrl, OrganizationName, UserName, userPassword);
                var query = new DialogExpression()
                {
                    Operation = DialogExpressionOperation.And,
                    Condition = new List<DialogExpressionCondition>()
                    {
                        DialogExpressionCondition.Create(fieldName: FieldName, value: FieldValue)
                    },
                    SortOrder = new List<SortedField>
                    {
                        SortedField.Create(FieldName, SortDirection.Desc)
                    }
                };
                var fieldList = platformClient.GetDocumentsByQuery(dlgInfos.FileCabinetGuid, false, query);
                if (fieldList.Count > 0)
                {
                    message = "Succesfully";
                    foreach (Document document in fieldList)
                    {
                        List<string> fileResult = new List<string>();
                        fileResult = platformClient.Download(platformClient.Organization, FileCabinetId, dialogId, $"{document.Id}", FieldValue, locationResult);
                        resultFolders.Add(fileResult[0]);
                    }
                    foreach (string file in resultFolders)
                    {
                        string[] words = file.Split('.');
                        if (words[1] == "zip")
                        {
                            platformClient.unzipFile(file, FieldValue, locationResult);
                        }
                    }
                    foreach (string file in resultFolders)
                    {
                        string[] words = file.Split('.');
                        if (words[1] == "zip")
                        {
                            string[] test = platformClient.ProcessDirectory(locationResult, file);
                            foreach (string filePath in test)
                            {
                                string pathCharacter = filePath.Replace("/Users/crifa/code/score/public", "");
                                result.Add(pathCharacter);
                            }
                        }
                        else
                        {
                            result.Add("/" + FieldValue + "/"+ file);
                        }

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