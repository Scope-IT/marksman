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
            // Returns a list of messages with return info.
            // This could be broken down further


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
            messages.Add(snipe.AssetManager.Create(currentAsset));

            return messages;
        }
    }
}
