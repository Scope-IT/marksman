using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnipeSharp;
using System.Diagnostics;
using SnipeSharp.Endpoints.Models;
using SnipeSharp.Endpoints.SearchFilters;
using SnipeSharp.Common;

namespace Marksman
{
    class Broker
    {
        public Broker()
        {

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
