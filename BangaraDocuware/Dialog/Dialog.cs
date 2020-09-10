using DocuWare.Platform.ServerClient;
using DocuWare.Services.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;

namespace DocuWare.SDK.Samples.dotNetCore.Examples
{

    class Dialog
    {
        public static List<string> result_list = new List<string>();
        public static string message = "";

        static void Main(string[] args)
        {
            Console.WriteLine("Run service!");
            string serverAddress = "https://bangara.docuware.cloud/DocuWare/Platform/";
            string userName = "Pruebas";
            string userPassword = "Pruebas01";
            string fileCabinetId = "8e925261-879c-4bb8-a77d-8d2f18550645";
            string dialogId = "98777eb2-2cbb-4ff7-9427-0e1f8a7b36d7";
            using (Helpers.Authenticator authenticator = new Helpers.Authenticator(serverAddress, userName, userPassword))
            {
                Organization organization = authenticator.Organization;
                if (organization == null)
                {
                    Console.WriteLine("No organization found");
                }
                else
                {
                    Query(organization, fileCabinetId, dialogId);
                    if (result_list.Count() > 0)
                    {
                        Console.WriteLine("Número de archivos encontrados: " + result_list.Count());
                        foreach (var element in result_list)
                        {
                            Console.WriteLine($"{element}");
                            Download(organization, fileCabinetId, dialogId, Int32.Parse(element));
                        }
                    }
                }
            }
            Console.Read();
        }

        public static void Query(Organization organization, string fileCabinetId, string dialogId)
        {
            Console.WriteLine("Query");
            DialogExpression dialogExpression = new DialogExpression()
            {
                Operation = DialogExpressionOperation.And,
                Condition = new List<DialogExpressionCondition>()
                {
                    DialogExpressionCondition.Create("CEDULA", "0704677582*")
                },
                Count = 100,
                SortOrder = new List<SortedField>()
                {
                    SortedField.Create("CEDULA", SortDirection.Desc)
                }
            };
            FileCabinet fileCabinet = organization.GetFileCabinetsFromFilecabinetsRelation().FileCabinet.FirstOrDefault(fc => fc.Id == fileCabinetId);
            if (fileCabinet == null)
            {
                message = "FileCabinet is null!";
            }
            else
            {
                DialogInfos dialogInfos = fileCabinet.GetDialogInfosFromDialogsRelation();

                if (dialogInfos == null)
                {
                    message = "DialogInfos is null!";
                }
                else
                {
                    DialogInfo dialog = dialogInfos.Dialog.FirstOrDefault(d => d.Id == dialogId);

                    if (dialog == null)
                    {
                        message = "Dialog is null!";
                    }
                    else
                    {
                        DocumentsQueryResult documentsQueryResult = dialog.GetDialogFromSelfRelation().GetDocumentsResult(dialogExpression);
                        Console.WriteLine("Query Result");
                        if (documentsQueryResult.Items.Count > 0)
                        {
                            message = "Succesfully";
                            foreach (Document document in documentsQueryResult.Items)
                            {
                                result_list.Add($"{document.Id}");
                                Console.WriteLine($"ID {document.Id}");
                                Console.WriteLine("Fields");
                                document.Fields.ForEach(f => Console.WriteLine($"Name: {f.FieldName} - Item: {f.Item}"));
                            }
                        }
                        else
                        {
                            message = "Not result";
                        }
                    }
                }
            }
        }

        private static void Download(Organization organization, string fileCabinetId, string queryDialogId, int documentId)
        {
            Console.WriteLine("Start download");
            FileCabinet fileCabinet = organization.GetFileCabinetsFromFilecabinetsRelation().FileCabinet.FirstOrDefault(fc => fc.Id == fileCabinetId);
            if (fileCabinet == null)
            {
                Console.WriteLine("FileCabinet is null!");
            }
            else
            {
                Platform.ServerClient.Document document = null;
                DialogExpression dialogExpression = new DialogExpression()
                {
                    Operation = DialogExpressionOperation.And,
                    Condition = new List<DialogExpressionCondition>()
                    {
                        DialogExpressionCondition.Create("DWDOCID", documentId.ToString())
                    },
                    Count = 100,
                    SortOrder = new List<SortedField>()
                    {
                        SortedField.Create("DWDOCID", SortDirection.Desc)
                    }
                };
                DialogInfos dialogInfos = fileCabinet.GetDialogInfosFromDialogsRelation();
                if (dialogInfos == null)
                {
                    Console.WriteLine("Dialog Info(s) is null!");
                }
                else
                {
                    DialogInfo dialog = dialogInfos.Dialog.FirstOrDefault(d => d.Id == queryDialogId);
                    if (dialog == null)
                    {
                        Console.WriteLine("Dialog is null!");
                    }
                    else
                    {
                        DocumentsQueryResult documentsQueryResult = dialog.GetDialogFromSelfRelation().GetDocumentsResult(dialogExpression);
                        Console.WriteLine("Query Result");
                        document = documentsQueryResult.Items.FirstOrDefault();
                        document = document?.GetDocumentFromSelfRelation();
                    }
                }
                if (document == null)
                {
                    Console.WriteLine("Document not exist!");
                }
                else
                {
                    document = document.GetDocumentFromSelfRelation();
                    DeserializedHttpResponse<Stream> deserializedHttpResponse = document.PostToFileDownloadRelationForStreamAsync(new FileDownload()
                    {
                        TargetFileType = FileDownloadType.Auto // FileDownloadType.PDF / FileDownloadType.ZIP
                    }).Result;
                    HttpContentHeaders httpContentHeaders = deserializedHttpResponse.ContentHeaders;
                    string ContentType = httpContentHeaders.ContentType.MediaType;
                    string FileName = deserializedHttpResponse.GetFileName();
                    long? ContentLength = httpContentHeaders.ContentLength;
                    Stream stream = deserializedHttpResponse.Content;
                    // TODO: Pending path to server to move public folder.
                    string curFile = @"C:/Users/LUCHO/Documents/Docuware/" + FileName;
                    // string curFile = @"/Users/crifa/code/score/public/" + FileName;
                    if (File.Exists(curFile))
                    {
                        Console.WriteLine("Document exist, don't downloaded!");
                    }
                    else
                    {
                        Console.WriteLine("Document downloaded succesfuly");
                        using (FileStream fileStream = File.Create(Path.Combine(curFile)))
                        {
                            if (stream.CanSeek)
                            {
                                stream.Seek(0, SeekOrigin.Begin);
                            }
                            stream.CopyTo(fileStream);
                        }
                    }
                }
            }
        }
    }
}