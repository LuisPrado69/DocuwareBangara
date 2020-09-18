using DocuWare.Platform.ServerClient;
using DocuWare.Services.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http.Headers;
using ValidationWebAPI.Models.Exceptions;

namespace ValidationWebAPI.Models
{
    class PlatformClient
    {
        readonly ServiceConnection _connector;
        readonly Organization _org;

        const string UrlFormatString = @"{0}/docuware/platform";

        /// <summary> Constructor creating connection using DocuWare user's credential. </summary>
        /// <param name="serverUrl"> The URL of the server Platform is running on. </param>
        /// <param name="organizationName"> Name of the organization you want the client to be connected to.</param>
        /// <param name="userName"> User to use when connecting to the organization specified.</param>
        /// <param name="userPassword"> Password of the user specified. </param>
        /// <remarks>
        /// When this constructor is running a connection to DocuWare Platform will be created; this action potentially consumes a license.
        /// "Potentially" means that not every run of this constructor will consume a license, in many cases the license will be re-used.
        /// We are not going into the details here because the underlying implementation regarding license consumption can be changed in the future.
        /// </remarks>
        public PlatformClient(string serverUrl, string organizationName, string userName, string userPassword)
        {

            _connector = ServiceConnection.Create(new Uri(string.Format(UrlFormatString, serverUrl)),
                                                      userName,
                                                      userPassword,
                                                      organizationName);

            _org = _connector.Organizations[0];
        }

        public Organization Organization
        {
            get { return _connector?.Organizations.FirstOrDefault()?.GetOrganizationFromSelfRelation(); }
        }

        /// <summary>
        /// Gives you access to all file cabinets the user that has been used when creating this instance of PlatformClient has access to.
        /// </summary>
        public IEnumerable<FileCabinet> GetAllFileCabinetsUserHasAccessTo()
        {
            return (from fileCabinet in _org.GetFileCabinetsFromFilecabinetsRelation().FileCabinet
                    where fileCabinet.IsBasket == false
                    select fileCabinet);
        }

        /// <summary>
        /// Gives you access to document trays (baskets) the user that has been used when creating this instance of PlatformClient has access to.
        /// </summary>
        public IEnumerable<FileCabinet> GetAllDocumentTraysUserHasAccessTo()
        {
            return (from fileCabinet in _org.GetFileCabinetsFromFilecabinetsRelation().FileCabinet
                    where fileCabinet.IsBasket
                    select fileCabinet);
        }

        /// <summary> Use it to access to a particular file cabinet. </summary>
        /// <param name="fileCabinetName"> Name of the file cabinet (case insensitive). </param>
        /// <returns> FileCabinet or null if no one found. </returns>
        public FileCabinet GetFileCabinet(string fileCabinetName)
        {
            return (from fileCabinet in GetAllFileCabinetsUserHasAccessTo()
                    where string.Compare(fileCabinet.Id, fileCabinetName, StringComparison.InvariantCultureIgnoreCase) == 0
                    select fileCabinet).SingleOrDefault();
        }

        /// <summary> Use it to access to a particular document tray (basket). </summary>
        /// <param name="documentTrayName"> Name of the file cabinet (case insensitive). </param>
        /// <returns> Document tray (basket) or null if no one found. </returns>
        public FileCabinet GetDocumentTray(string documentTrayName)
        {
            return (from documentTray in GetAllDocumentTraysUserHasAccessTo()
                    where string.Compare(documentTray.Name, documentTrayName, StringComparison.InvariantCultureIgnoreCase) == 0
                    select documentTray).SingleOrDefault();
        }

        /// <summary> Use it if you are searching for documents. </summary>
        /// <param name="targetName"> The name if file cabinet or document tray (basket). </param>
        /// <param name="isDocumentTray"> Specifies if the target is a document tray (basket). </param>
        /// <param name="query"> Searching criteria. </param>
        /// <returns> List containing documents found. </returns>
        public List<Document> GetDocumentsByQuery(string targetName, bool isDocumentTray, DialogExpression query)
        {
            var target = isDocumentTray ? GetDocumentTray(targetName) : GetFileCabinet(targetName);
            var searchDialog = getDefaultSearchDialog(target);

            return runQueryForDocuments(searchDialog, query).Items;
        }

        #region Private methods

        private Dialog getDefaultSearchDialog(FileCabinet fileCabinet)
        {
            return fileCabinet.GetDialogInfosFromSearchesRelation().Dialog.FirstOrDefault(dlg => dlg.IsDefault == !fileCabinet.IsBasket)?.GetDialogFromSelfRelation();
        }

        private DocumentsQueryResult runQueryForDocuments(Dialog dialog, DialogExpression query)
        {
            return dialog.Query.PostToDialogExpressionRelationForDocumentsQueryResult(query);
        }

        #endregion

        public void IsDuplicate(DlgInfos dlgInfos)
        {

            var query = new DialogExpression()
            {
                Operation = DialogExpressionOperation.And,
                Condition = GetDialogExpressionConditions(dlgInfos)
            };
            int count = GetDocumentsByQuery(dlgInfos.FileCabinetGuid, isDocumentTray: false, query: query).Count;
            if (count > 0)
            {
                throw new DuplicateDocumentException("Document with same index data already exists!");
            }

        }

        public static List<DialogExpressionCondition> GetDialogExpressionConditions(DlgInfos dlgInfos)
        {
            List<DialogExpressionCondition> dialogExpressionConditions = new List<DialogExpressionCondition>();
            foreach (var item in dlgInfos.Values)
            {
                if (item.Item != null && item.ItemElementName.ToLower() == "string")
                {
                    dialogExpressionConditions.Add(DialogExpressionCondition.Create(fieldName: item.FieldName, value: item.Item.ToString()));
                }
                if (item.Item != null && item.ItemElementName.ToLower() == "decimal")
                {
                    dialogExpressionConditions.Add(DialogExpressionCondition.Create(fieldName: item.FieldName, value: item.Item.ToString()));
                }
                if (item.Item != null && item.ItemElementName.ToLower() == "int")
                {
                    dialogExpressionConditions.Add(DialogExpressionCondition.Create(fieldName: item.FieldName, value: item.Item.ToString()));
                }
                if (item.Item != null && item.ItemElementName.ToLower() == "memo")
                {
                    dialogExpressionConditions.Add(DialogExpressionCondition.Create(fieldName: item.FieldName, value: item.Item.ToString()));
                }
                if (item.Item != null && item.ItemElementName.ToLower() == "date")
                {
                    DateTime dateTime = DateTime.Parse(item.Item.ToString());
                    dialogExpressionConditions.Add(DialogExpressionCondition.Create(fieldName: item.FieldName, value: dateTime.Date.ToString("s", CultureInfo.CreateSpecificCulture("en-US"))));
                }
                if (item.Item != null && item.ItemElementName.ToLower() == "datetime")
                {
                    DateTime dateTime = DateTime.Parse(item.Item.ToString());
                    dialogExpressionConditions.Add(DialogExpressionCondition.Create(fieldName: item.FieldName, value: dateTime.ToString("s", CultureInfo.CreateSpecificCulture("en-US"))));
                }
                if (item.Item != null && item.ItemElementName.ToLower() == "keywords")
                {
                    var key = JsonConvert.DeserializeObject<DocumentIndexFieldKeywords>(item.Item.ToString());

                    foreach (var keyword in key.Keyword)
                    {
                        dialogExpressionConditions.Add(DialogExpressionCondition.Create(item.FieldName, keyword));
                    }
                }
            }
            return dialogExpressionConditions;
        }

        public void Logout()
        {
            _connector.Disconnect();
        }

        public void DownloadDocumentThumbnail(Document document, string thumbmailFilePath)
        {
            var thumbnail = document.GetStreamFromThumbnailRelation();
            using (var thumbnailFile = System.IO.File.Create(thumbmailFilePath))
            {
                thumbnail.CopyTo(thumbnailFile);
            }
        }

        public List<string> Download(Organization organization, string fileCabinetId, string queryDialogId, string documentId, string FieldValue, string locationResult)
        {
            List<string> result = new List<string>();
            Console.WriteLine("Start download");
            FileCabinet fileCabinet = organization.GetFileCabinetsFromFilecabinetsRelation().FileCabinet.FirstOrDefault(fc => fc.Id == fileCabinetId);
            if (fileCabinet == null)
            {
                Console.WriteLine("FileCabinet is null!");
            }
            else
            {
                DocuWare.Platform.ServerClient.Document document = null;
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
                        TargetFileType = FileDownloadType.Auto
                    }).Result;
                    HttpContentHeaders httpContentHeaders = deserializedHttpResponse.ContentHeaders;
                    string ContentType = httpContentHeaders.ContentType.MediaType;
                    string FileName = deserializedHttpResponse.GetFileName();
                    long? ContentLength = httpContentHeaders.ContentLength;
                    Stream stream = deserializedHttpResponse.Content;
                    // Path to server to move public folder.
                    Directory.CreateDirectory(locationResult);
                    locationResult = locationResult + "/" + FileName;
                    // string curFile = @"/Users/crifa/code/score/public/" + FileName;
                    result.Add(FileName);
                    Console.WriteLine("Document download succesfuly");
                    using (FileStream fileStream = File.Create(Path.Combine(locationResult)))
                    {
                        if (stream.CanSeek)
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                        }
                        stream.CopyTo(fileStream);
                    }
                }
            }
            return result;
        }

        public void unzipFile(string FileName, string FieldValue, string location)
        {
            string[] words = FileName.Split('.');
            if (words[1] == "zip")
            {
                string locationZip = location + "/" + FileName;
                string locationResult = location + "/" + words[0];
                Directory.CreateDirectory(locationResult);
                string zipPath = locationZip;
                string extractPath = locationResult;
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        {
                            entry.ExtractToFile(Path.Combine(extractPath, entry.FullName), true);
                        }
                    }
                }
            }
        }

        public string[] ProcessDirectory(string targetDirectory, string FileName)
        {
            string[] words = FileName.Split('.');
            string[] filePaths = Directory.GetFiles(targetDirectory + "/" + words[0]);
            return filePaths;
        }
    }
    public class DlgField
    {
        public string FieldName { get; set; }
        public string ItemElementName { get; set; }
        public object Item { get; set; }
    }

    public class DlgInfos
    {
        public DateTime TimeStamp { get; set; }
        public string UserName { get; set; }
        public string OrganizationName { get; set; }
        public string FileCabinetGuid { get; set; }
        public string DialogGuid { get; set; }
        public string DialogType { get; set; }
        public string userPassword { get; set; }
        public string serverUrl { get; set; }
        public string FieldName { get; set; }
        public string FieldValue { get; set; }
        public string DateValueStart { get; set; }
        public string DateValueEnd { get; set; }
        public List<DlgField> Values { get; set; }
    }
}
