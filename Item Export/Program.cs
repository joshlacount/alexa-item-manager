using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Item_Export
{
    class Program
    {
        static SQLiteDatabase manifest;
        static readonly string installPath = Directory.GetCurrentDirectory();

        static void Main(string[] args)
        {
            Console.WriteLine("Beginning export...");
            manifest = new SQLiteDatabase(installPath + @"\manifest.content");
            string queryString = @"SELECT json_extract(DestinyInventoryItemDefinition.json, '$.displayProperties.name')
                                   FROM DestinyInventoryItemDefinition, json_tree(DestinyInventoryItemDefinition.json, '$')
                                   WHERE json_tree.key = 'equippable' AND json_tree.value = 1
                                   INTERSECT
                                   SELECT json_extract(DestinyInventoryItemDefinition.json, '$.displayProperties.name')
                                   FROM DestinyInventoryItemDefinition, json_tree(DestinyInventoryItemDefinition.json, '$')
                                   WHERE json_tree.key = 'name' AND json_tree.value != ''";
            DataRowCollection rows = manifest.selectQuery(queryString).Rows;

            using (var exportFile = File.OpenWrite(installPath + @"\itemExport.csv"))
            {
                foreach (DataRow r in rows)
                {
                    string itemName = Convert.ToString(r[0]);
                    if (itemName.Contains(','))
                        itemName = '"' + itemName + '"';
                    byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(itemName + "," + Environment.NewLine);
                    exportFile.Write(bytesToSend, 0, bytesToSend.Length);
                }
                exportFile.Dispose();
            }
            Console.WriteLine("Export finished");
            Console.ReadLine();
        }
    }
}
