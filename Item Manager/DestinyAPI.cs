using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace Item_Manager
{
    static class DestinyAPI
    {
        private const string TITAN = "0";
        private const string HUNTER = "1";
        private const string WARLOCK = "2";

        private static SQLiteDatabase manifest = new SQLiteDatabase(Directory.GetCurrentDirectory() + @"\manifest.content");

        private static string dMembershipId;
        private static Dictionary<string, string> characters = new Dictionary<string, string>();
        private static string membershipId = "";
        private static string membershipType = "4";

        static DestinyAPI()
        {
            DestinyAPI.GetId();
            DestinyAPI.GetCharacterId();
        }

        public static void Test()
        {
            Console.WriteLine(dMembershipId);
        }

        private static void GetId()
        {
            var response = Oauth.AccessResourceGet(string.Format("/User/GetMembershipsById/{0}/{1}", membershipId, membershipType));
            dMembershipId = response.Response.destinyMemberships[0].membershipId;
            Console.WriteLine("Destiny Id: " + dMembershipId);
        }

        private static dynamic GetProfile(string component)
        {
            var profileJson = Oauth.AccessResourceGet(string.Format("/Destiny2/{0}/Profile/{1}?components={2}", membershipType, dMembershipId, component));
            return profileJson;
        }

        private static void GetCharacterId()
        {
            IEnumerable<KeyValuePair<string, JToken>> json = GetProfile("200").Response.characters.data;
            foreach (var item in json)
            {
                characters.Add(Convert.ToString(item.Value["classType"]), item.Key);
            }
        }

        public static dynamic GetCharacterInfo(string classType, string component)
        {
            var json = Oauth.AccessResourceGet(string.Format("/Destiny2/{0}/Profile/{1}/Character/{2}?components={3}", membershipType, dMembershipId, characters[classType], component));
            return json;
        }

        public static void GetDestinyManifest()
        {
            var contentPath = Oauth.AccessResourceGet("/Destiny2/Manifest").Response.mobileWorldContentPaths.en;
            string maniUrl = "http://www.bungie.net" + contentPath;
            Console.WriteLine(maniUrl);
            /*
            using (HttpClient client = new HttpClient())
            {
                var responseTask = client.GetAsync(maniUrl);
                responseTask.Wait();
                using (StreamWriter sw = new StreamWriter(Directory.GetCurrentDirectory() + @"\MANZIP.content"))
                {
                    sw.Write(responseTask.Result.Content.);
                    sw.Close();
                }
            }
            */
        }
        
        public static string GetItemHash(string itemName)
        {
            string itemHash;
            string queryString = string.Format(@"SELECT json_extract(DestinyInventoryItemDefinition.json, '$.hash')
                                   FROM DestinyInventoryItemDefinition, json_tree(DestinyInventoryItemDefinition.json, '$')
                                   WHERE json_tree.key = 'name' AND json_tree.value = '{0}'
                                   INTERSECT
                                   SELECT json_extract(DestinyInventoryItemDefinition.json, '$.hash')
                                   FROM DestinyInventoryItemDefinition, json_tree(DestinyInventoryItemDefinition.json, '$')
                                   WHERE json_tree.key = 'equippable' AND json_tree.value = 1", itemName);
            itemHash = Convert.ToString(manifest.selectQuery(queryString).Rows[0][0]);
            Console.WriteLine(itemHash);
            return itemHash;
        }
        
        public static string GetItemId(string itemHash, string classType)
        {
            string itemId = null;

            var items = GetCharacterInfo(classType, "205").Response.equipment.data.items;
            foreach (var item in items)
            {
                if (item["itemHash"] == itemHash)
                    itemId = item["itemInstanceId"];
            }

            if (itemId == null)
            {
                items = GetCharacterInfo(classType, "201").Response.inventory.data.items;
                foreach(var item in items)
                {
                    if (item["itemHash"] == itemHash)
                        itemId = item["itemInstanceId"];
                }
            }
            Console.WriteLine(itemId);
            return itemId;
        }

        public static string GetItemId(string itemHash)
        {
            string itemId = null;

            var items = GetProfile("102").Response.profileInventory.data.items;
            foreach (var item in items)
            {
                if (item["itemHash"] == itemHash)
                    itemId = item["itemInstanceId"];
            }

            return itemId;
        }

        public static int TransferItem(string itemName, string className, string transferToVault)
        {
            string itemHash = GetItemHash(itemName);
            string itemId = null;
            string classType = null;
            
            switch(className)
            {
                case "Titan":
                    classType = TITAN;
                    break;
                case "Hunter":
                    classType = HUNTER;
                    break;
                case "Warlock":
                    classType = WARLOCK;
                    break;
                default:
                    break;
            }

            if (transferToVault == "true")
                itemId = GetItemId(itemHash, classType);
            else if (transferToVault == "false")
                itemId = GetItemId(itemHash);

            if (itemHash != null && itemId != null)
            {
                var content = new Dictionary<string, string>
                {
                    { "itemReferenceHash", itemHash },
                    { "stackSize", "1" },
                    { "transferToVault", transferToVault },
                    { "itemId", itemId },
                    { "characterId", characters[classType] },
                    { "membershipType", membershipType }
                };
                var response = Oauth.AccessResourcePost("/Destiny2/Actions/Items/TransferItem/", content);
                if ((int)response["ErrorCode"] == 1)
                    return ErrorCodes.TRANSFERSUCCESS;
            }

            return ErrorCodes.TRANSFERFAIL;
        }

        public static int EquipItem(string itemName, string classType)
        {
            string itemId = GetItemId(GetItemHash(itemName), classType);

            if (itemId != null)
            {
                var content = new Dictionary<string, string>
                {
                    { "itemId", itemId },
                    { "characterId", characters[classType] },
                    { "membershipType", membershipType }
                };

                var response = Oauth.AccessResourcePost("/Destiny2/Actions/Items/EquipItem/", content);
                if ((int)response["ErrorCode"] == 1)
                    return ErrorCodes.EQUIPSUCCESS;
            }

            return ErrorCodes.EQUIPFAIL;
        }
    }
}
