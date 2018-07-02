using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnipeSharp;
using System.Diagnostics;

namespace Marksman
{
    class Broker
    {


        public static void syncAsset(SnipeItApi snipe, SnipeSharp.Endpoints.Models.Asset currentAsset)
        {
            try
            {
                SnipeSharp.Endpoints.SearchFilters.SearchFilter assetFilter = new SnipeSharp.Endpoints.SearchFilters.SearchFilter()
                {
                    Search = currentAsset.AssetTag
                };

                SnipeSharp.Endpoints.Models.Asset foundAsset = snipe.AssetManager.FindOne(assetFilter);
                if (foundAsset != null && foundAsset.AssetTag == currentAsset.AssetTag)
                {
                    Trace.WriteLine("Asset already exists in db. Not added.");
                }
                else
                {
                    var response = snipe.AssetManager.Create(currentAsset);
                    Trace.WriteLine("Response recieved from SnipeIT server after attempting to add asset: ");
                    Trace.WriteLine(response);
                }
            } catch (Exception e)
            {
                Trace.WriteLine("Exception encountered while adding asset: " + e.ToString());
            }

        }
    }
}
