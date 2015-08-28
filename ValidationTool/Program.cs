using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace ValidationTool
{
    class Program
    {
        static void Main(string[] args)
        {
            var val = new Validation();
            //val.StartComparison();

            //var itemIds = val.GetItemIdsFromFile(@"G:\sharedstuff\Tools\VSO_ValidationTool\Run-8-27\ItemIDs-1.dat");

            //foreach (var itemId in itemIds)
            //{
            //    var tfsItem = val.GetTfsWorkItem(itemId);

            //    if (tfsItem != null && !string.IsNullOrWhiteSpace(tfsItem.Tags))
            //    {
            //        Console.WriteLine("{0} {1}", tfsItem.Id, tfsItem.Tags.ToString());
            //    }
            //}

            var tfsItem = val.GetTfsWorkItem(1053543);
            //var tfsItemTagList = tfsItem.Tags.Split(';').ToList();

            var vsoItemTag =
                "CordovaPluginRequest; Scenario: MDD; Severity: Pain Point; Source: External Customer; Stream: Interview; Usage: Hypothetical; Test Migrated To DevDiv-Test; Migrated To DevDiv-Test";

            var tfsTagList = CreateTagList(tfsItem.Tags);
            var vsoTagList = CreateTagList(vsoItemTag);

            var l = tfsTagList.Except(vsoTagList);

            Console.WriteLine(""); 


        }

        static IEnumerable<string> CreateTagList(string tags)
        {
            var tokens = tags.Split(';');
            List<string> tagList = new List<string>();
            foreach (var token in tokens)
            {
                if (!token.Trim().Equals("Test Migrated To DevDiv-Test", StringComparison.OrdinalIgnoreCase)
                    && !token.Trim().Equals("Test Test Migrated To DevDiv-Test", StringComparison.OrdinalIgnoreCase)
                    && !token.Trim().Equals("Test Migrated To DevDev-Test", StringComparison.OrdinalIgnoreCase)
                    && !token.Trim().Equals("Migrated To DevDiv-Test", StringComparison.OrdinalIgnoreCase)
                    && !token.Trim().Equals("Migrated To DevDiv VSO", StringComparison.OrdinalIgnoreCase))
                {
                    tagList.Add(token);
                }
            }

            return tagList;
        }

        //static bool RevisionAfterMigration(WorkItem tfsItem)
        //{
        //    var revisions = tfsItem.Revisions;
        //    int migCount = 0;

        //    bool result = false;

        //    for (int i = revisions.Count - 1; i > -0; i--)
        //    {
        //        var hist = revisions[i].Fields["History"].Value;
        //        if (hist != null &&
        //            hist.ToString().IndexOf("Test migration to", StringComparison.OrdinalIgnoreCase) >= 0)
        //        {
        //            migCount = i;
        //            break;
        //        }
        //    }

        //    if (migCount == revisions.Count - 1) return false;

        //    for (int i = migCount+1; i < revisions.Count; i++)
        //    {
        //        //var hist = revisions[i].Fields["History"].Value;
        //        //if (hist != null && !string.IsNullOrWhiteSpace(hist.ToString()))
        //        //{
        //        //    return true;
        //        //}
        //        Console.WriteLine("++++++++++++++++++++++++++++++");
        //        Console.WriteLine("Revision Num = {0}", i);
        //        Console.WriteLine("++++++++++++++++++++++++++++++");
        //        Console.WriteLine(revisions[i].Fields["Changed By"].Value);
        //        if(!(revisions[i].Fields["Changed By"].Value.ToString().IndexOf("vs bld lab", StringComparison.OrdinalIgnoreCase) >= 0))
        //        {
        //            result = true;
        //            break;
        //        }

        //    }

        //    return result;
        //}
    }
}
