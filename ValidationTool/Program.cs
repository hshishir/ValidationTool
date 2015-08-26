using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Client;
using System.Configuration;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace ValidationTool
{
    class Program
    {
        static void Main(string[] args)
        {

            //var tfsItem = GetTfsWorkItem(1054115);
            //var vsoItem = GetVsoWorkItem(Convert.ToInt32(tfsItem.Fields["Mirrored TFS ID"].Value));
            //Compare(vsoItem, tfsItem);

            Validation val = new Validation();
            val.Start();

        }

        static WorkItem GetVsoWorkItem(int id)
        {
            var vsoServerUri = new Uri(ConfigurationManager.AppSettings["VsoServer"]);
            var vsoServer = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(vsoServerUri);
            var store = vsoServer.GetService<WorkItemStore>();
            var project = store.Projects["DevDiv"];

            return store.GetWorkItem(id);
        }

        static WorkItem GetTfsWorkItem(int id)
        {
            var tfsServerUrl = ConfigurationManager.AppSettings["TfsServer"];
            var tfsServerUri = new Uri(tfsServerUrl);
            var tfsServer = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tfsServerUri);
            var store = tfsServer.GetService<WorkItemStore>();
            var project = store.Projects["DevDiv"];

            return store.GetWorkItem(id);
        }

        static bool Compare(WorkItem vsoItem, WorkItem tfsItem)
        {
            bool result;

            // Compare title
            result = string.Equals(vsoItem.Title, tfsItem.Title);
            Console.WriteLine("Title={0}", result);

            // Compare area path
            result = string.Equals(vsoItem.AreaPath, tfsItem.AreaPath);
            Console.WriteLine("AreaPath={0}", result);

            // Compare iteration path
            result = string.Equals(vsoItem.IterationPath, tfsItem.IterationPath);
            Console.WriteLine("IterationPath={0}", result);

            // Compare hyperlinkcount
            result = vsoItem.HyperLinkCount == tfsItem.HyperLinkCount;
           Console.WriteLine("HyperLinkCount={0}", result);

            // Revision count
            result = vsoItem.Revisions.Count == tfsItem.Revisions.Count;
            Console.WriteLine("Vso Revisions [{0}], Tfs Revisions [{1}]", vsoItem.Revisions.Count, tfsItem.Revisions.Count);

            // Revision count
            result = vsoItem.Links.Count == tfsItem.Links.Count;
            Console.WriteLine("Vso Links [{0}], Tfs Links [{1}]", vsoItem.Links.Count, tfsItem.Links.Count);

            //Console.WriteLine("VSO Item Revisions");
            //foreach (Revision rev in tfsItem.Revisions)
            //{
            //    foreach (Field field in tfsItem.Fields)
            //    {
            //        Console.WriteLine(rev.Fields[field.Name].Value);
            //    }
            //}

            //result = vsoItem.Fields["Repro Steps"].Value.Equals(tfsItem.Fields["Repro Steps"].Value);
            //Console.WriteLine("Repro Steps={0}",result);

            //Console.WriteLine(tfsItem.Fields["History"].Value);

            //foreach (Field field in tfsItem.Fields)
            //{
            //    //if(field.Name.Contains("History"))
            //    Console.WriteLine(field.Name);
            //}

            return result;
        }
    }
}
