using Box.V2;
using Box.V2.Auth;
using Box.V2.Config;
using Box.V2.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.IO;
using System.Threading.Tasks;
using CRES.DataContract;
using Box.V2.JWTAuth;
using System.Collections;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;

namespace CRES.Services.Infrastructure
{
    public class BoxHelper
    {
        ////Box Standard Authentication
        //string Box_Client_Id = ConfigurationManager.AppSettings["Box_Client_Id"]; 
        //string Box_Client_Secret = ConfigurationManager.AppSettings["Box_Client_Secret"]; 
        //string Box_Acces_Token = ConfigurationManager.AppSettings["Box_Acces_Token"];
        //string Box_Client_Folder = ConfigurationManager.AppSettings["Box_Client_Folder"]; 
        //string Box_SDK_Url = ConfigurationManager.AppSettings["Box_SDK_Url"];

        //Box JWT Authentication
        IConfigurationSection Sectionroot = null;
        string BOX_CLIENT_ID = "";
        string BOX_CLIENT_SECRET = "";
        string ENTERPRISE_ID = "";
        string JWT_PRIVATE_KEY_PASSWORD = "";
        string JWT_PUBLIC_KEY_ID = "";
        string Box_Client_Folder = "";


        BoxClient client = null;

        public BoxHelper()
        {
            GetConfigSetting();
            string BOX_CLIENT_ID = Sectionroot.GetSection("Box_Client_Id").Value;
            string BOX_CLIENT_SECRET = Sectionroot.GetSection("Box_Client_Secret").Value;
            string ENTERPRISE_ID = Sectionroot.GetSection("Box_EnterpriseId").Value;
            string JWT_PRIVATE_KEY_PASSWORD = Sectionroot.GetSection("Box_PrivateKeyPassword").Value;
            string JWT_PUBLIC_KEY_ID = Sectionroot.GetSection("Box_PublicKeyId").Value;
            string Box_Client_Folder = Sectionroot.GetSection("Box_Client_Folder").Value;

            //  var privateKey = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "private_key.pem.example"));
            var privateKey = File.ReadAllText(Directory.GetCurrentDirectory() + @"\wwwroot\private_key.pem.example");

            var boxConfig = new BoxConfig(BOX_CLIENT_ID, BOX_CLIENT_SECRET, ENTERPRISE_ID, privateKey, JWT_PRIVATE_KEY_PASSWORD, JWT_PUBLIC_KEY_ID);
            var boxJWT = new BoxJWTAuth(boxConfig);
            var adminToken = boxJWT.AdminToken();

            client = boxJWT.AdminClient(adminToken);

        }

        public BoxHelper(string servicer)
        {
            //BW-Berkadia and wells
            if (servicer.ToLower() == "berkadiawells")
            {
                //var reader = new StreamReader("C:\\BoxConfig\\config.json");
                var reader = new StreamReader(Directory.GetCurrentDirectory() + @"\wwwroot\config.json");
                var json = reader.ReadToEnd();
                var config = BoxConfig.CreateFromJsonString(json);
                var boxJWT = new BoxJWTAuth(config);
                var adminToken = boxJWT.AdminToken();
                client = boxJWT.AdminClient(adminToken);
            }
        }

        public void GetConfigSetting()
        {
            if (Sectionroot == null)
            {
                IConfigurationBuilder builder = new ConfigurationBuilder();
                builder.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
                var root = builder.Build();
                Sectionroot = root.GetSection("Application");
            }
        }

        //public async Task ListFolder()
        //{
        //    var list = await client.Files.ListFolderAsync(string.Empty);
        //    return list;

        //}
        //upload to root folder only
        public async Task UploadFile(string uploadTofolder, DocumentDataContract _docDC, Stream fileStream)
        {

            //create folder
            //BoxFolderRequest folderReq = new BoxFolderRequest()
            //{
            //    Name = "DevContainer",
            //    Parent = new BoxRequestEntity() { Id = "0" }
            //};

            //var newFolder = await client.FoldersManager.CreateAsync(folderReq);


            //get folder id by folder name
            var result = await client.SearchManager.SearchAsync(Box_Client_Folder, type: "folder");

            ///var result = await client.FoldersManager.GetInformationAsync(Box_Client_Folder, type: "folder");



            string FolderID = "";
            if (result.TotalCount > 0)
            {

                foreach (BoxItem item in result.Entries)
                {
                    FolderID = item.Id;
                }
            }
            if (!string.IsNullOrEmpty(FolderID))
            {

                var fileRequest = new BoxFileRequest
                {
                    Name = _docDC.FileName,
                    Parent = new BoxFolderRequest { Id = FolderID }
                };

                BoxFile b = await client.FilesManager.UploadAsync(fileRequest, fileStream);
                _docDC.DocumentStorageID = b.Id;

            }
        }

        //upload at deal level
        public async Task<string> UploadFileToFolder(string folderID, DocumentDataContract _docDC, Stream fileStream)
        {
            string _DocumentStorageID = "";
            try
            {

                if (string.IsNullOrEmpty(folderID))
                {
                    BoxCollection<BoxItem> items = await client.FoldersManager.GetFolderItemsAsync("0", 1);
                    var ParentfolderID = items.Entries[0].Id;

                    //BoxCollection<BoxItem> Invoiceitems = await client.FoldersManager.GetFolderItemsAsync(ParentfolderID, 10, 0);
                    //for (int i = 0; i < Invoiceitems.Entries.Count; i++)
                    //{
                    //    if (Invoiceitems.Entries[i].Name == _docDC.FolderName)
                    //    {
                    //        folderID = Invoiceitems.Entries[i].Id;
                    //    }
                    //}



                    var fileRequest = new BoxFileRequest
                    {
                        Name = _docDC.FileName,
                        Parent = new BoxFolderRequest { Id = ParentfolderID }
                    };

                    BoxFile b = await client.FilesManager.UploadAsync(fileRequest, fileStream);
                    _docDC.DocumentStorageID = b.Id;
                    _DocumentStorageID = b.Name;
                }

            }
            catch (Exception ex)
            {
                string sr = "";
                throw;
            }
            return _DocumentStorageID;
        }





        public async Task<MemoryStream> DownloadFile(DocumentDataContract _docDC)
        {
            MemoryStream mem = new MemoryStream();
            try
            {


                string InvoicefolderID = "";
                string InvoiceFileId = "";
                //  var files = GetDocumentFromBox(_docDC.FolderName, "");

                BoxCollection<BoxItem> items = await client.FoldersManager.GetFolderItemsAsync("0", 1);
                var ParentfolderID = items.Entries[0].Id;



                //BoxCollection<BoxItem> Invoiceitems = await client.FoldersManager.GetFolderItemsAsync(ParentfolderID, 10, 0);
                //for (int i = 0; i < Invoiceitems.Entries.Count; i++)
                //{
                //    if (Invoiceitems.Entries[i].Name == _docDC.FolderName)
                //    {
                //        InvoicefolderID = Invoiceitems.Entries[i].Id;
                //    }
                //}

                //     BoxCollection<BoxItem> InvoiceFiles = await client.FoldersManager.GetFolderItemsAsync(InvoicefolderID, 1000);

                int limit = 1000;
                int offset = 0;
                BoxCollection<BoxItem> result;
                do
                {
                    //await client.SearchManager.SearchAsync(limit:1000, ancestorFolderIds: new List<string>() { ParentfolderID }, keyword: "ACORE Draw Fee Invoice – W Hollywood - 21-1377-DR-0006");
                    //string folderName = "ACORE Draw Fee Invoice – W Hollywood - 21-1377-DR-0006";
                    //string folderName = "\"ACORE Draw Fee Invoice – W Hollywood - 21-1377-DR-0006\"";
                    //var items1 = await client.SearchManager.QueryAsync(limit: 50, ancestorFolderIds: new List<string>() { ParentfolderID }, type: "file", scope: "user_content", query: ""+"21-1377-DR-0006"+"");
                    result = await client.FoldersManager.GetFolderItemsAsync(ParentfolderID, limit, offset);
                    offset += limit;

                    if (result.Entries.Where(i => i.Name == _docDC.FileName).Count() > 0)
                    {

                        InvoiceFileId = result.Entries.Where(i => i.Name == _docDC.FileName).FirstOrDefault().Id;
                        break;
                    }

                } while (offset < result.TotalCount);

                //BoxCollection<BoxItem> InvoiceFiles = await client.FoldersManager.GetFolderItemsAsync(ParentfolderID, 1000);

                //for (int j = 0; j < InvoiceFiles.Entries.Count; j++)
                //{
                //    if (InvoiceFiles.Entries[j].Name == _docDC.FileName)
                //    {
                //        InvoiceFileId = InvoiceFiles.Entries[j].Id;
                //    }
                //}


                using (Stream stream = await client.FilesManager.DownloadAsync(InvoiceFileId))
                {
                    int bytesRead;
                    var buffer = new byte[8192];
                    do
                    {
                        bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        await mem.WriteAsync(buffer, 0, bytesRead);
                    } while (bytesRead > 0);
                }
            }
            catch (Exception ex)
            {

                string srexp = ex.Message;
            }
            return mem;

        }

        public async Task<MemoryStream> DownloadFileByID(string ID,string FileExtension)
        {
            MemoryStream mem = new MemoryStream();

            if (!string.IsNullOrEmpty(ID))
                {
                try
                {
                    using (Stream stream = await client.FilesManager.DownloadAsync(ID))
                    {
                        if (FileExtension.ToLower() == "zip")
                        {
                            Stream unzippedEntryStream = null; // Unzipped data from a file in the archive
                            ZipArchive archive = new ZipArchive(stream);

                            unzippedEntryStream = archive.Entries[0].Open(); // .Open will return a stream
                            int bytesRead1;
                            var buffer1 = new byte[8192];
                            do
                            {
                                bytesRead1 = await unzippedEntryStream.ReadAsync(buffer1, 0, buffer1.Length);
                                await mem.WriteAsync(buffer1, 0, bytesRead1);
                            } while (bytesRead1 > 0);
                        }
                        else
                        {

                            int bytesRead;
                            var buffer = new byte[8192];
                            do
                            {
                                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                                await mem.WriteAsync(buffer, 0, bytesRead);
                            } while (bytesRead > 0);
                        }



                    }
                }
                catch (Exception ex)
                {

                    string srexp = ex.Message;
                }
            }
            return mem;

        }

        //get documents from box either by id or name
        public async Task<BoxCollection<BoxItem>> GetDocumentFromBox(string folderName, string folderId)
        {
            string folderID = "";
            BoxCollection<BoxItem> docs = null;
            if (!string.IsNullOrEmpty(folderId))
            {
                folderID = folderId;
            }
            else
            {
                
                //for testing
                /*
                try
                {
                    BoxCollection<BoxItem> folder_n = await client.FoldersManager.GetFolderItemsAsync("157444905203", 1);
                    //BoxCollection<BoxCollaboration> boxcol = await client.FoldersManager.GetCollaborationsAsync("157444905203", null);

                }
                catch (Exception ex)
                {
                    string str = ex.Message;
                }
                */
               //


                BoxCollection<BoxItem> items = await client.FoldersManager.GetFolderItemsAsync("0", 1);
                folderID = items.Entries[0].Id;

                createNewFolderById(folderID, "Invoice");
                //  var docs1 = await client.FoldersManager.GetFolderItemsAsync(folderID, 1);
                //  BoxCollection<BoxItem> fileitems = await client.FilesManager.GetFileTasks("0", 1);

                //var searchResult = await client.FoldersManager.GetFolderItemsAsync("0", 1).Result;
                //if (searchResult != null && searchResult.Entries.Count > 0)
                //    folderID = searchResult.Entries.FirstOrDefault().Id;
            }

            if (!string.IsNullOrEmpty(folderID))
            {
                var retFields = new List<string> { BoxFolder.FieldItemCollection, BoxFolder.FieldOwnedBy, BoxFolder.FieldCreatedAt, BoxFolder.FieldSize, BoxFolder.FieldModifiedAt, BoxFolder.FieldParent, BoxFolder.FieldName, BoxFolder.FieldItemStatus };
                docs = await client.FoldersManager.GetFolderItemsAsync(folderID, 1000, fields: retFields);
            }

            return docs;
        }

        public async Task<BoxCollection<BoxItem>> GetDocumentFromBoxForWellsBerkadia(string folderName, string folderId)
        {
            string folderID = "";
            BoxCollection<BoxItem> docs = null;
            if (!string.IsNullOrEmpty(folderId))
            {
                folderID = folderId;
            }
            else
            {

                //for testing
                /*
                try
                {
                    BoxCollection<BoxItem> folder_n = await client.FoldersManager.GetFolderItemsAsync("157444905203", 1);
                    //BoxCollection<BoxCollaboration> boxcol = await client.FoldersManager.GetCollaborationsAsync("157444905203", null);

                }
                catch (Exception ex)
                {
                    string str = ex.Message;
                }
                */
                //

                //BoxCollection<BoxItem> items = await client.FoldersManager.GetFolderItemsAsync("0", 1);
                
                BoxCollection<BoxItem> items = await client.FoldersManager.GetFolderItemsAsync("0", 50);

                var items1=items.Entries.Where(i => i.Name.ToLower() == folderName.ToLower());
                if (items1!=null)
                {
                    folderID = items1.FirstOrDefault().Id;
                }
               
            }

            if (!string.IsNullOrEmpty(folderID))
            {
                var retFields = new List<string> { BoxFolder.FieldItemCollection, BoxFolder.FieldOwnedBy, BoxFolder.FieldCreatedAt, BoxFolder.FieldSize, BoxFolder.FieldModifiedAt, BoxFolder.FieldParent, BoxFolder.FieldName, BoxFolder.FieldItemStatus };
                docs = await client.FoldersManager.GetFolderItemsAsync(folderID, 1000, fields: retFields);
            }

            return docs;
        }


        public Task<BoxFolder> CreateNewFolder(BoxClient userClient)
        {
            var folderRequest = new BoxFolderRequest
            {
                Description = "Test folder",
                Name = "Misc",
                Parent = new BoxFolderRequest { Id = "0" }
            };
            return userClient.FoldersManager.CreateAsync(folderRequest);
        }


        public string createNewFolder(string FolderName)
        {
            //  var resultParent =  client.FoldersManager.GetFolderItemsAsync(Box_Client_Folder, 100, 0, null);

            var folderReq = new BoxFolderRequest()
            {
                Name = FolderName,
                Parent = new BoxRequestEntity() { Id = "0" }
            };
            var f = client.FoldersManager.CreateAsync(folderReq);


            return f.Result.Id;
        }

        public string createNewFolderById(string ParentFolderId, String FolderName)
        {
            //  var resultParent =  client.FoldersManager.GetFolderItemsAsync(Box_Client_Folder, 100, 0, null);

            var folderReq = new BoxFolderRequest()
            {
                Name = FolderName,
                Parent = new BoxRequestEntity() { Id = ParentFolderId }
            };
            var f = client.FoldersManager.CreateAsync(folderReq);


            return f.Result.Id;

        }
        public async Task<string> NewCheckAndCreateBoxFolder(string parentfolderName, string folderName)
        {
            string parentFolderID = "";
            string folderIDToUpload = "";

            string RootFolderID = (await client.FoldersManager.GetFolderItemsAsync(Box_Client_Folder, 50)).Entries.FirstOrDefault().Id;
            if (!string.IsNullOrEmpty(parentfolderName))
            {
                //var resultParent = await client.FoldersManager.GetItemsAsync(parentfolderName, ancestorFolderIds: new List<string>() { RootFolderID });
                //if (resultParent != null && resultParent.Entries.Count > 0)
                //{
                //    parentFolderID = resultParent.Entries.FirstOrDefault().Id;
                //}
                //else
                //{
                //    var boxFolderRequest = new BoxFolderRequest
                //    {
                //        Name = parentfolderName,
                //        Parent = new BoxRequestEntity
                //        {
                //            Id = RootFolderID
                //        }
                //    };
                //    parentFolderID = client.FoldersManager.CreateAsync(boxFolderRequest).Result.Id;
                //}
                //
                var resultfolder = await client.SearchManager.SearchAsync(folderName, type: "folder", ancestorFolderIds: new List<string>() { parentFolderID });
                if (resultfolder != null && resultfolder.Entries.Count > 0)
                {
                    folderIDToUpload = resultfolder.Entries.FirstOrDefault().Id;
                }
                else
                {
                    var boxFolderRequest = new BoxFolderRequest
                    {
                        Name = folderName,
                        Parent = new BoxRequestEntity
                        {
                            Id = parentFolderID
                        }
                    };
                    folderIDToUpload = client.FoldersManager.CreateAsync(boxFolderRequest).Result.Id;
                }
                //
            }
            else
            {
                var result = await client.SearchManager.SearchAsync(folderName, type: "folder", ancestorFolderIds: new List<string>() { RootFolderID });
                if (result != null && result.Entries.Count > 0)
                {
                    folderIDToUpload = result.Entries.FirstOrDefault().Id;
                }
                else
                {
                    var boxFolderRequest = new BoxFolderRequest
                    {
                        Name = folderName,
                        Parent = new BoxRequestEntity
                        {
                            Id = RootFolderID
                        }
                    };
                    folderIDToUpload = client.FoldersManager.CreateAsync(boxFolderRequest).Result.Id;
                }
            }
            return folderIDToUpload;
        }


        public async Task<string> CheckAndCreateBoxFolder(string parentfolderName, string folderName)
        {
            string parentFolderID = "";
            string folderIDToUpload = "";

            string RootFolderID = (await client.SearchManager.SearchAsync(Box_Client_Folder, type: "folder")).Entries.FirstOrDefault().Id;
            if (!string.IsNullOrEmpty(parentfolderName))
            {
                var resultParent = await client.SearchManager.SearchAsync(parentfolderName, type: "folder", ancestorFolderIds: new List<string>() { RootFolderID });
                if (resultParent != null && resultParent.Entries.Count > 0)
                {
                    parentFolderID = resultParent.Entries.FirstOrDefault().Id;
                }
                else
                {
                    var boxFolderRequest = new BoxFolderRequest
                    {
                        Name = parentfolderName,
                        Parent = new BoxRequestEntity
                        {
                            Id = RootFolderID
                        }
                    };
                    parentFolderID = client.FoldersManager.CreateAsync(boxFolderRequest).Result.Id;
                }
                //
                var resultfolder = await client.SearchManager.SearchAsync(folderName, type: "folder", ancestorFolderIds: new List<string>() { parentFolderID });
                if (resultfolder != null && resultfolder.Entries.Count > 0)
                {
                    folderIDToUpload = resultfolder.Entries.FirstOrDefault().Id;
                }
                else
                {
                    var boxFolderRequest = new BoxFolderRequest
                    {
                        Name = folderName,
                        Parent = new BoxRequestEntity
                        {
                            Id = parentFolderID
                        }
                    };
                    folderIDToUpload = client.FoldersManager.CreateAsync(boxFolderRequest).Result.Id;
                }
                //
            }
            else
            {
                var result = await client.SearchManager.SearchAsync(folderName, type: "folder", ancestorFolderIds: new List<string>() { RootFolderID });
                if (result != null && result.Entries.Count > 0)
                {
                    folderIDToUpload = result.Entries.FirstOrDefault().Id;
                }
                else
                {
                    var boxFolderRequest = new BoxFolderRequest
                    {
                        Name = folderName,
                        Parent = new BoxRequestEntity
                        {
                            Id = RootFolderID
                        }
                    };
                    folderIDToUpload = client.FoldersManager.CreateAsync(boxFolderRequest).Result.Id;
                }
            }
            return folderIDToUpload;
        }
    }
}