using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace ValidationTool
{
    public class Validation
    {
        private TfsTeamProjectCollection _tfsServer;
        private TfsTeamProjectCollection _vsoServer;
        private WorkItemStore _vsoStore;
        private WorkItemStore _tfsStore;
        private List<string> _commonFields;
       
        // logs
        private Logging _errorLog;
        private Logging _commentLog;
        private Logging _exceptionLog;
        private Logging _valFieldErrorLog;
        private Logging _valTagErrorLog;
        private Logging _valPostMigrationUpdateLog;
        private Logging _fullLog;
        
        private List<Task> _taskList;

        private List<string> _itemTypesToValidate; 

        private int _migratedItemCount;
        private int _itemFailedFieldValidationCount;
        private int _itemFailedTagValidationCount;
        private int _itemPostMigrationUpdateCount;
        private int _notMigratedItemCount;
        private int _misMatchedItemCount;
        private int _exceptionItemCount;

        public Validation()
        {
            _tfsServer = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(ConfigurationManager.AppSettings["TfsServer"]));
            _vsoServer = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(ConfigurationManager.AppSettings["VsoServer"]));
            _vsoStore = _vsoServer.GetService<WorkItemStore>();
            _tfsStore = _tfsServer.GetService<WorkItemStore>();

            var runDateTime = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");

            var dataFilePath = ConfigurationManager.AppSettings["DataFilePath"];
            var dataDir = string.IsNullOrWhiteSpace(dataFilePath) ? Directory.GetCurrentDirectory() : dataFilePath;
            var dirName = string.Format("{0}\\Log-{1}",dataDir,runDateTime);
            

            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }
            
            _errorLog = new Logging(string.Format("{0}\\Error.txt", dirName));
            _commentLog = new Logging(string.Format("{0}\\Status.txt", dirName));
            _valFieldErrorLog = new Logging(string.Format("{0}\\FieldError.txt", dirName));
            _valTagErrorLog = new Logging(string.Format("{0}\\TagError.txt", dirName));
            _valPostMigrationUpdateLog = new Logging(string.Format("{0}\\PostMigrationUpdate.txt", dirName));


            _fullLog = new Logging(string.Format("{0}\\FullLog.txt", dirName));

            _commonFields = new List<string>();
            _itemTypesToValidate = new List<string>();

            _taskList = new List<Task>();

            var fields = ConfigurationManager.AppSettings["CommonFields"].Split(',');
            foreach (var field in fields)
            {
                _commonFields.Add(field);
            }

            var types = ConfigurationManager.AppSettings["WorkItemTypes"].Split(',');
            foreach (var type in types)
            {
                _itemTypesToValidate.Add(type);
            }
        }

        public List<int> GetItemIdsFromFile(string fileName)
        {
            var items = File.ReadAllLines(fileName);
            var itemList = new List<int>();
            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    itemList.Add(Convert.ToInt32(item.Trim()));
                }
            }

            return itemList;
        }

        public WorkItem GetVsoWorkItem(int id)
        {
            return _vsoStore.GetWorkItem(id);
        }

        public WorkItem GetTfsWorkItem(int id)
        {
            return _tfsStore.GetWorkItem(id);
        }

        public void StartComparison()
        {
            var sw = Stopwatch.StartNew();

            // Get all data files
            var filePath = ConfigurationManager.AppSettings["DataFilePath"];
            
            var files = (!string.IsNullOrWhiteSpace(filePath)) ? Directory.GetFiles(filePath, "*.dat") : Directory.GetFiles(Directory.GetCurrentDirectory(), "*.dat");

            foreach (var file in files)
            {
                var fileName = file;
                var t = Task.Factory.StartNew(() =>
                {
                    Console.WriteLine("Starting task for {0}", fileName);
                    _fullLog.Log("Starting task for {0}", fileName);
                    Compare(fileName);

                });

                _taskList.Add(t);
            }

            Task.WaitAll(_taskList.ToArray());
            sw.Stop();

            var sb = new StringBuilder();
            sb.AppendLine(string.Format("\n\nMigrated item count: {0}", _migratedItemCount));
            sb.AppendLine(string.Format("Not migrated item count: {0}", _notMigratedItemCount));
            sb.AppendLine(string.Format("Mis-matched item count: {0}", _misMatchedItemCount));
            sb.AppendLine(string.Format("Exception item count: {0}", _exceptionItemCount));
            sb.AppendLine(string.Format("Field error count: {0}", _itemFailedFieldValidationCount));
            sb.AppendLine(string.Format("Tag error count: {0}", _itemFailedTagValidationCount));
            sb.AppendLine(string.Format("Post Migration update count: {0}", _itemPostMigrationUpdateCount));
            
            sb.AppendLine(String.Format("Total time: {0}", sw.Elapsed.TotalSeconds));
            
            Console.WriteLine(sb.ToString());
            _fullLog.Log(sb.ToString());
            _commentLog.Log(sb.ToString());
        }

        
        public void Compare(string fileName)
        {
            var itemIds = GetItemIdsFromFile(fileName);
            var str = string.Format("Total items: {0}", itemIds.Count);
            Console.WriteLine(str);
            _fullLog.Log(str);
            _migratedItemCount += itemIds.Count;

            foreach (var itemId in itemIds)
            {
                int vsoItemId = -1;

                try
                {
                    var tfsItem = GetTfsWorkItem(itemId);

                    if (string.IsNullOrWhiteSpace(tfsItem.Fields["Mirrored TFS ID"].Value.ToString()))
                    {
                        _notMigratedItemCount++;
                        _errorLog.Log("{0};{1}", tfsItem.Id, "NOTMIGRATED");
                        continue;
                    }

                    vsoItemId = Convert.ToInt32(tfsItem.Fields["Mirrored TFS ID"].Value);
                    var vsoItem = GetVsoWorkItem(vsoItemId);
                    
                    if (vsoItem == null)
                    {
                        _notMigratedItemCount++;
                        _errorLog.Log("{0};{1};{2};{3}", tfsItem.Id, vsoItemId, tfsItem.Type.Name, "VSO ITEM NOT FOUND");
                        continue;
                    }

                    Console.WriteLine("Compare TfsItem id={0}, VsoItemId={1}, type={2}", tfsItem.Id, vsoItem.Id, tfsItem.Type.Name);
                    _fullLog.Log("Compare TfsItem id={0}, VsoItemId={1}, type={2}", tfsItem.Id, vsoItem.Id, tfsItem.Type.Name);

                    if (!string.IsNullOrWhiteSpace(vsoItem.Fields["Mirrored TFS ID"].Value.ToString()) 
                        && !vsoItem.Fields["Mirrored TFS ID"].Value.Equals(tfsItem.Id.ToString()))
                    {
                        _misMatchedItemCount++;
                        _errorLog.Log("{0};{1};{2};{3};", tfsItem.Id, vsoItem.Id, tfsItem.Type.Name,"MISMATCHED");
                        continue;
                    }

                    if (!CompareItems(vsoItem, tfsItem))
                    {
                        //_itemFailedValidationCount++;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("TfsItem={0}, VsoItem={1} Exception={2}", itemId, vsoItemId, e.Message);
                    _fullLog.Log("TfsItem={0}, VsoItem={1} Exception={2}", itemId, vsoItemId, e.Message);
                    _exceptionItemCount++;
                    _errorLog.Log("TfsItem={0}, VsoItem={1} Exception={2}", itemId, vsoItemId, e.Message);
                }
            }
            
        }

        private bool CompareItems(WorkItem vsoItem, WorkItem tfsItem)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0};{1};{2};", tfsItem.Id, vsoItem.Id, vsoItem.Type.Name);

            bool result = CompareCommonFields(vsoItem, tfsItem, sb);
            string itemType = vsoItem.Type.Name;

            if(_itemTypesToValidate.Find(s => s.IndexOf(itemType, StringComparison.OrdinalIgnoreCase) >= 0) != null)
            {
                result &= CompareItemSpecificFields(vsoItem, tfsItem, itemType, sb);
            }

            // Compare tags
            var r = CompareTags(vsoItem, tfsItem);
            if (!r)
            {
                sb.AppendFormat(";{0}", "TAGSDONTMATCH");
                _valTagErrorLog.Log(sb.ToString());
                _itemFailedTagValidationCount++;
            }

            result &= r;

            if (!result)
            {
                var rmig = RevisionAfterMigration(tfsItem);
                if(rmig)
                {
                    sb.AppendFormat(";{0}", "UPDATEDPOSTMIGRATION");
                    _valPostMigrationUpdateLog.Log(sb.ToString());
                    _itemPostMigrationUpdateCount++;
                }
                else
                {
                    _valFieldErrorLog.Log(sb.ToString());
                    _itemFailedFieldValidationCount++;
                }
                
            }

            return result;
        }

        private bool CompareCommonFields(WorkItem vsoItem, WorkItem tfsItem, StringBuilder sb)
        {
            var result = true;

            foreach (var field in _commonFields)
            {
                if (field.Equals("HyperLinkCount", StringComparison.OrdinalIgnoreCase))
                {
                    var r = vsoItem.HyperLinkCount == tfsItem.HyperLinkCount;
                    if (!r)
                    {
                        sb.AppendFormat(" {0}", field);
                    }
                    result &= r;
                }
                else if (field.Equals("Priority", StringComparison.OrdinalIgnoreCase))
                {
                    var r = vsoItem.Fields[field].Value == tfsItem.Fields[field].Value;
                    if (!r)
                    {
                        sb.AppendFormat(" {0}", field);
                    }
                    result &= r;
                }
                else if (field.Equals("Attachments", StringComparison.OrdinalIgnoreCase))
                {
                    var r = vsoItem.Attachments.Count == tfsItem.Attachments.Count;
                    if (!r)
                    {
                        sb.AppendFormat(" {0}", field);
                    }
                    result &= r;
                }
                else if (field.Equals("Title", StringComparison.OrdinalIgnoreCase))
                {
                    var r = IsNotCommaDifference(vsoItem.Fields[field].Value.ToString(),
                        tfsItem.Fields[field].Value.ToString());

                    if (!r)
                    {
                        sb.AppendFormat(" {0}", field);
                    }

                    result &= r;
                }
                else if (field.Equals("Created Date", StringComparison.OrdinalIgnoreCase))
                {
                    var r = DateTime.Compare(vsoItem.CreatedDate, tfsItem.CreatedDate) == 0;

                    if (!r)
                    {
                        sb.AppendFormat(" {0}", field);
                    }

                    result &= r;
                }
                else if (field.Equals("Assigned To", StringComparison.OrdinalIgnoreCase))
                {
                    var vsoAssignedTo = vsoItem.Fields[field].Value.ToString();
                    var tfsAssignedTo = tfsItem.Fields[field].Value.ToString();

                    var r = vsoAssignedTo.ToLower().Contains(tfsAssignedTo.ToLower()) || tfsAssignedTo.ToLower().Contains(vsoAssignedTo.ToLower());

                    if (!r)
                    {
                        sb.AppendFormat(" {0}", field);
                    }

                    result &= r;
                }
                else
                {
                    var r = vsoItem.Fields[field].Value.Equals(tfsItem.Fields[field].Value);
                    if (!r)
                    {
                        sb.AppendFormat(" {0}", field);
                    }
                    result &= r;
                }

            }

            return result;
        }

        private bool CompareItemSpecificFields(WorkItem vsoItem, WorkItem tfsItem, string itemType, StringBuilder sb)
        {
            var item = ConfigurationManager.AppSettings[itemType];
            if (string.IsNullOrWhiteSpace(item))
            {
                return true;
            }

            var result = true;
            var itemFields = item.Split(',');

            foreach (var itemField in itemFields)
            {
                if (itemField.Equals("Source Branch", StringComparison.OrdinalIgnoreCase)
                    || itemField.Equals("Target Branch", StringComparison.OrdinalIgnoreCase))
                {
                    var r = vsoItem.Fields[itemField].Value.Equals(tfsItem.Fields[itemField].Value);
                    if (!r)
                    {
                        sb.AppendFormat(" {0}", itemField);
                    }

                    result &= r;
                }
                else if (itemField.Equals("Description", StringComparison.OrdinalIgnoreCase))
                {
                    var r = IsNotCommaDifference(vsoItem.Fields[itemField].Value.ToString(),
                        tfsItem.Fields[itemField].Value.ToString());
                    
                    if(!r)
                        {
                            sb.AppendFormat(" {0}", itemField);
                        }
                        
                        result &= r;
                }
                else if (itemField.Equals("Repro Steps", StringComparison.OrdinalIgnoreCase))
                {
                    var r = IsNotCommaDifference(vsoItem.Fields[itemField].Value.ToString(),
                        tfsItem.Fields[itemField].Value.ToString());
                    
                    if(!r)
                        {
                            sb.AppendFormat(" {0}", itemField);
                        }
                    
                    result &= r;
                }
                else if (itemField.Equals("Priority", StringComparison.OrdinalIgnoreCase))
                {
                    var r = Convert.ToInt32(vsoItem.Fields[itemField].Value) == Convert.ToInt32(tfsItem.Fields[itemField].Value);
                    if (!r)
                    {
                        sb.AppendFormat(" {0}", itemField);
                    }
                    result &= r;
                }
            }

            return result;
        }

        private bool CompareTags(WorkItem vsoItem, WorkItem tfsItem)
        {
            if (string.IsNullOrWhiteSpace(vsoItem.Tags) && string.IsNullOrWhiteSpace(tfsItem.Tags))
            {
                return true;
            }

            var vsoTagList = CreateTagList(vsoItem.Tags);
            var tfsTagList = CreateTagList(tfsItem.Tags);

            return vsoTagList.SetEquals(tfsTagList);
        }

        private bool RevisionAfterMigration(WorkItem tfsItem)
        {
            var revisions = tfsItem.Revisions;
            int migCount = 0;

            for (int i = revisions.Count -1; i >= 0; i--)
            {
                var hist = revisions[i].Fields["History"].Value;
                if (hist != null &&
                    hist.ToString().IndexOf("Test migration to", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    migCount = i;
                    break;
                }
            }

            if (migCount == revisions.Count - 1) return false;

            var result = false;

            for (int i = migCount + 1; i < revisions.Count ; i++)
            {
                var changedBy = revisions[i].Fields["Changed By"].Value.ToString();
                if (!(changedBy.IndexOf("vs bld lab", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        private bool IsNotCommaDifference(string s1, string s2)
        {
            string regex = @"(<.+?>|&nbsp;|&#160;)";
            var x1 = Regex.Replace(s1, regex, "").Trim();
            var x2 = Regex.Replace(s2, regex, "").Trim();

            x1 = x1.Replace("\n", "").Replace("\t", "");
            x2 = x2.Replace("\n", "").Replace("\t", "");

            if ((x1.StartsWith("From: ", StringComparison.OrdinalIgnoreCase)) &&
                x2.StartsWith("From: ", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return x1.Equals(x2);
        }

        private HashSet<string> CreateTagList(string tags)
        {
            var tokens = tags.Split(';');
            HashSet<string> tagList = new HashSet<string>();
            foreach (var token in tokens)
            {
                if (!token.Trim().Equals("Test Migrated To DevDiv-Test", StringComparison.OrdinalIgnoreCase)
                    && !token.Trim().Equals("Test Test Migrated To DevDiv-Test", StringComparison.OrdinalIgnoreCase)
                    && !token.Trim().Equals("Test Migrated To DevDev-Test", StringComparison.OrdinalIgnoreCase)
                    && !token.Trim().Equals("Migrated To DevDiv-Test", StringComparison.OrdinalIgnoreCase)
                    && !token.Trim().Equals("Migrated To DevDiv VSO", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(token.Trim()))
                {
                    tagList.Add(token.Trim());
                }
            }

            return tagList;
        }
    }
}
