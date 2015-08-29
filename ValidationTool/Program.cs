using System;
using System.Collections.Generic;
using System.IO;

namespace ValidationTool
{
    class Program
    {
        static void Main(string[] args)
        {
            var val = new Validation();
            val.StartComparison();

            //Split(@"G:\sharedstuff\Tools\VSO_ValidationTool\E2EMigrated-AllParts.txt", 7000);

        }

        static void Split(string fileName, int splitNum)
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

            var fileItemList = new List<int>();
            int count = 0;
            int fileCount = 1;

            for (int i = 0; i < itemList.Count; i++)
            {
                fileItemList.Add(itemList[i]);
                count++;

                if (count == splitNum)
                {
                    var itemIdFileName = string.Format("ItemIDs-{0}.dat", fileCount);
                    using (var sw = new StreamWriter(itemIdFileName))
                    {
                        foreach (var fl in fileItemList)
                        {
                            sw.WriteLine(fl);
                        }
                    }

                    count = 0;
                    fileCount++;
                    fileItemList.Clear();
                }
            }

            if (fileItemList.Count > 0)
            {
                var itemIdFileName = string.Format("ItemIDs-{0}.dat", fileCount);
                using (var sw = new StreamWriter(itemIdFileName))
                {
                    foreach (var fl in fileItemList)
                    {
                        sw.WriteLine(fl);
                    }
                }
            }
        }
    }
}
