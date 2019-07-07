using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnipeSharp;
using System.Diagnostics;
using SnipeSharp.Endpoints.Models;
using SnipeSharp.Endpoints.SearchFilters;
using SnipeSharp.Common;
using System.Net;

namespace Marksman
{
    class Broker
    {
        public Broker()
        {

        }

        public bool IsIdentical(Asset a1, Asset a2)
        {
            // We only need to check fields that are being populated by the Marksman agent
            // i.e.. not ID, since that is managed by DB

            if (a1.AssetTag != a2.AssetTag || a1.Serial != a2.Serial || a1.Name != a2.Name)
            {
                return false;
            }

            if (a1.WarrantyMonths != a2.WarrantyMonths)
            {
                return false;
            }

            if (a1.Location?.Id != a2.Location?.Id)
            {
                return false;
            }

            // Checking sub-object IDs
            if (a1.Company?.Id != a2.Company?.Id || a1.Model?.Id != a2.Model?.Id ||
                a1.StatusLabel?.Id != a2.StatusLabel?.Id)
            {
                return false;
            }

            // Should be something for custom fields -> for now leaving blank

            return true;
        }

        public bool CheckConnection(NameValueCollection appSettings)
        {
            // This method might seem overly complicated for what it is doing (simply
            // checking a connection to the Snipe-IT instance. However, there are a lot
            // of different ways that the connection can fail (usually related to improperly
            // set values in the config file).

            // This method allows a set of specific, descriptive error messages to be passed
            // showing exactly what kind of configuration problem needs to be fixed.
            
            string uri = "";
            string query = "users?limit=0";
            string baseUri = appSettings["BaseURI"];

            // Note: The program should be able to handle a BaseURI that has a trailing '/' or not.

            if (baseUri.EndsWith("/")){
                uri = baseUri + query; 
            } else
            {
                uri = baseUri + "/" + query;
            }

            HttpWebRequest request;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(uri);
                request.Headers["Authorization"] = "Bearer " + appSettings["API"];
                request.Accept = "application/json";
            }
            catch (System.NotSupportedException e)
            {
                Console.WriteLine(e);
                Console.WriteLine("Please double-check the BaseURI key in your <appSettings>\nblock of the Marksman config file and ensure it points to your instance of Snipe-IT.");
                return false;
            }
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Console.WriteLine("HTTP 200: Connection to Snipe-IT instance succeded.");
                    return true;
                } else {
                    Console.WriteLine("HTTP {0}", response.StatusCode);
                    Console.WriteLine("Unexpected HTTP response code, could not connect to Snipe-IT instance.");
                    return false;
                }
            } catch (WebException e)
            {
                HttpWebResponse r = (HttpWebResponse)e.Response;
                if (r == null)
                {
                    Console.WriteLine(e);
                    Console.WriteLine("Please double-check the BaseURI key in your <appSettings>\nblock of the Marksman config file and ensure it points to your instance of Snipe-IT.");
                }
                else if (r.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("HTTP 403: Unauthorized. Please check the API key value in your <appSettings>\nblock of the Marksman config file and ensure it has been set to a valid key.");
                }
                else if (r.StatusCode == HttpStatusCode.NotFound)
                {
                    Console.WriteLine("HTTP 404: URL not found. Please double-check the BaseURI key in your <appSettings>\nblock of the Marksman config file and ensure it points to your instance of Snipe-IT.");
                } else
                {
                    Console.WriteLine("Unexpected error, could not connect to Snipe-IT instance.");
                    Console.WriteLine(e);
                }
                return false;
            }
        }

        public List<IRequestResponse> SyncAll(SnipeItApi snipe, Asset currentAsset, Model currentModel, Manufacturer currentManufacturer,
            Category currentCategory, Company currentCompany, StatusLabel currentStatusLabel, Location currentLocation)
        {
            
            // Let's try to simplify the logic into a repeatable structure:

            // Each of these categories (Asset, Model, Location, etc.) has a single
            // value that uniquely specifies it, which must be:
            // a) Uniquely associated to it in the physical world
            // b) Not the database unique ID

            // Why can't it be the database unique ID? Because the uniqueID is not something that 
            // is inherently associated to the computer's hardware, it's just a number that SnipeIT
            // uses internally.

            // By category: Unique identifiers

            // Asset: computer serial (string)
            // Model: model full name (string)
            // Category: model types, as given by the WMIC fullnames (string)
            // Manufacturer: full name (string)
            // Company: full name (string)
            // StatusLabel: full name (string)
            // Location: full name (string)

            // Really, we only need the update functionality for the assets, as the computer name can change,
            // along with its location, etc.

            // However we are not in change of things like a computer manufacturer changing the name of
            // its company. So those have a simpler functionality.


            List<IRequestResponse> messages = new List<IRequestResponse>();

            messages.Add(snipe.ManufacturerManager.Create(currentManufacturer));
            SearchFilter manufacturerFilter = new SearchFilter(currentManufacturer.Name);
            Manufacturer updatedManufacturer = snipe.ManufacturerManager.FindOne(manufacturerFilter);

            messages.Add(snipe.CategoryManager.Create(currentCategory));
            SearchFilter categoryFilter = new SearchFilter(currentCategory.Name);
            Category updatedCategory = snipe.CategoryManager.FindOne(categoryFilter);

            currentModel.Manufacturer = updatedManufacturer;
            currentModel.Category = updatedCategory;
            messages.Add(snipe.ModelManager.Create(currentModel));
            SearchFilter modelFilter = new SearchFilter(currentModel.Name);
            Model updatedModel = snipe.ModelManager.FindOne(modelFilter);

            messages.Add(snipe.CompanyManager.Create(currentCompany));
            SearchFilter companyFilter = new SearchFilter(currentCompany.Name);
            Company updatedCompany = snipe.CompanyManager.FindOne(companyFilter);

            messages.Add(snipe.StatusLabelManager.Create(currentStatusLabel));
            SearchFilter statusLabelFilter = new SearchFilter(currentStatusLabel.Name);
            StatusLabel updatedStatusLabel = snipe.StatusLabelManager.FindOne(statusLabelFilter);

            messages.Add(snipe.LocationManager.Create(currentLocation));
            SearchFilter locationFilter = new SearchFilter(currentLocation.Name);
            Location updatedLocation = snipe.LocationManager.FindOne(locationFilter);

            currentAsset.Model = updatedModel;
            currentAsset.Company = updatedCompany;
            currentAsset.StatusLabel = updatedStatusLabel;
            currentAsset.Location = updatedLocation;

            string currentSerial = currentAsset.Serial;

            Asset dbAsset = snipe.AssetManager.FindBySerial(currentSerial);

            if (dbAsset == null)
            {
                Console.WriteLine("Asset does not exist in database, creating...");
                snipe.AssetManager.Create(currentAsset);
            } else
            {
                Console.WriteLine("Asset already exists in db. Checking for consistency.");
                bool isIdentical = IsIdentical(currentAsset, dbAsset);
                if (isIdentical)
                {
                    Console.WriteLine("No changes required! Asset already exists and is up-to-date.");
                } else
                {
                    Console.WriteLine("Changes in asset detected. Updating:");

                    // Setting old ID for consistency
                    currentAsset.Id = dbAsset.Id;
                    currentAsset.LastCheckout = dbAsset.LastCheckout;
                    currentAsset.AssignedTo = dbAsset.AssignedTo;
                    messages.Add(snipe.AssetManager.Update(currentAsset));

                }
            }
            
            return messages;
        }
    }
}
