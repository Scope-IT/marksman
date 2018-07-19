using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Management;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using SnipeSharp;


namespace Marksman
{
   

    public class Sentry // Data acquissition
    {
        private Dictionary<string, List<string>> Queries; // Where key = query type, value = query itself
        private Dictionary<string, string> Values; // Internal representation of query results
        private System.Collections.Specialized.NameValueCollection Settings;
        public  Dictionary<string, string> rawResults // Public representation of query results - raw values. Useful for debug
        {
            get { return this.Values;  }
        }



        public Sentry(System.Collections.Specialized.NameValueCollection appSettings) // constructor 
        {
            Queries = new Dictionary<string, List<string>>();
            Settings = appSettings;
        }

        public SnipeSharp.Endpoints.Models.Asset GetAsset(System.Collections.Specialized.NameValueCollection appSettings, SnipeItApi snipe)
        {
            //string manufacturer = GetOutputVariable("Win32_ComputerSystem.Manufacturer");
            string manufacturer = GetOutputVariable("Win32_ComputerSystem.Manufacturer");
            string systemName = GetOutputVariable("Win32_ComputerSystem.Name");
            string serialNumber = GetOutputVariable("Win32_ComputerSystemProduct.IdentifyingNumber");
            string modelTotal = GetOutputVariable("Win32_ComputerSystem.Model");
            string macAddress = GetOutputVariable("Win32_NetworkAdapter.MACAddress");
            string systemType = GetOutputVariable("Win32_ComputerSystem.PCSystemType");
            // TODO: Place in a separate enum class:

            // This enum should be in a separate class for enums
            Dictionary<string, string> PCSystemTypes = new Dictionary<string, string>();
            PCSystemTypes.Add("0", "Undefined");
            PCSystemTypes.Add("1", "Desktop");
            PCSystemTypes.Add("2", "Laptop");
            PCSystemTypes.Add("3", "Workstation");
            PCSystemTypes.Add("4", "Enterprise Server");
            PCSystemTypes.Add("5", "SOHO Server");
            PCSystemTypes.Add("6", "Appliance PC");
            PCSystemTypes.Add("7", "Performance Server");
            PCSystemTypes.Add("8", "Maximum");


            string systemTypeFull = "Undefined";
            try
            {
                systemTypeFull = PCSystemTypes[systemType];
            }
            catch (Exception e)
            {
                Trace.WriteLine("Exception encountered while processing PCSystemType: " + e.ToString());
            }


            // TODO: This only works is in the exact format "ModelName ModelNumber"
            List<String> modelFragments = modelTotal.Split(' ').ToList();
            string modelNumber = modelFragments[modelFragments.Count() - 1];
            string modelMake = modelFragments[0];


            SnipeSharp.Endpoints.SearchFilters.SearchFilter blankSearch = new SnipeSharp.Endpoints.SearchFilters.SearchFilter();

            // should be from config file

            // TODO 
            // Create a universal search & create if not found method
            SnipeSharp.Endpoints.Models.Company currentCompany = new SnipeSharp.Endpoints.Models.Company
            {
                Name = appSettings["Company"]
            };

            SnipeSharp.Endpoints.SearchFilters.SearchFilter companyFilter = new SnipeSharp.Endpoints.SearchFilters.SearchFilter()
            {
                Search = currentCompany.Name
            };

            SnipeSharp.Endpoints.Models.Company searchedCompany = snipe.CompanyManager.FindOne(companyFilter);
            if (searchedCompany == null)
            {
                snipe.CompanyManager.Create(currentCompany);
                currentCompany = snipe.CompanyManager.FindOne(companyFilter);
            } else
            {
                currentCompany = searchedCompany;
            }

            string assetLocation = this.Values["Location"];

 

            SnipeSharp.Endpoints.Models.Location currentLocation = new SnipeSharp.Endpoints.Models.Location
            {
                Name = assetLocation
            };

            SnipeSharp.Endpoints.SearchFilters.SearchFilter locationFilter = new SnipeSharp.Endpoints.SearchFilters.SearchFilter()
            {
                Search = currentLocation.Name
            };

            SnipeSharp.Endpoints.Models.Location searchedLocation = snipe.LocationManager.FindOne(locationFilter);
            if (searchedLocation == null)
            {
                var result = snipe.LocationManager.Create(currentLocation);
                currentLocation = snipe.LocationManager.FindOne(locationFilter);
            }
            else
            {
                currentLocation = searchedLocation;
            }

            SnipeSharp.Endpoints.Models.Manufacturer currentManufacturer = new SnipeSharp.Endpoints.Models.Manufacturer
            {
                Name = manufacturer
            };

            SnipeSharp.Endpoints.SearchFilters.SearchFilter manufacturerFilter = new SnipeSharp.Endpoints.SearchFilters.SearchFilter()
            {
                Search = currentManufacturer.Name
            };


            SnipeSharp.Endpoints.Models.Manufacturer searhedManufacturer = snipe.ManufacturerManager.FindOne(manufacturerFilter);
            if (searhedManufacturer == null)
            {
                snipe.ManufacturerManager.Create(currentManufacturer);
                currentManufacturer = snipe.ManufacturerManager.FindOne(manufacturerFilter);
            }
            else
            {
                currentManufacturer = searhedManufacturer;
            }

            SnipeSharp.Endpoints.Models.Model currentModel = new SnipeSharp.Endpoints.Models.Model
            {
                Name = modelTotal,
                Manufacturer = currentManufacturer,
                Category = snipe.CategoryManager.Get(systemTypeFull),
                ModelNumber = modelNumber,
            };

            SnipeSharp.Endpoints.SearchFilters.SearchFilter modelFilter = new SnipeSharp.Endpoints.SearchFilters.SearchFilter()
            {
                Search = currentModel.Name
            };

            SnipeSharp.Endpoints.Models.Model searchedModel = snipe.ModelManager.FindOne(modelFilter);
            if (searchedModel == null || searchedModel.Manufacturer.Name != currentModel.Manufacturer.Name || searchedModel.Name != currentModel.Name)
            {
                snipe.ModelManager.Create(currentModel);
                currentModel = snipe.ModelManager.FindOne(modelFilter);
            } else
            {
                currentModel = searchedModel;
            }


            Dictionary<string, string> customFields = new Dictionary<string, string>();
            customFields.Add("_snipeit_macaddress_1", macAddress);



            bool isInteractive = false;
            bool interactiveParseSuccess = Boolean.TryParse(appSettings["Interactive"], out isInteractive);
            if (interactiveParseSuccess && isInteractive)
            {
                Console.WriteLine("Enter the computer name: ");
                systemName = Console.ReadLine();
            }

            SnipeSharp.Endpoints.Models.StatusLabel currentStatusLabel = snipe.StatusLabelManager.Get("In Production");
            string warrantyMonths = appSettings["WarrantyMonths"];


            SnipeSharp.Endpoints.Models.Asset currentComputer = new SnipeSharp.Endpoints.Models.Asset
            {
                Company = currentCompany,
                AssetTag = appSettings["AssetTagPrefix"] + "-" + serialNumber, // <-- to be implemented.. somehow, somewhere
                Model = currentModel,
                StatusLabel = currentStatusLabel,
                RtdLocation = currentLocation,
                Name = systemName,
                Serial = serialNumber,
                WarrantyMonths = warrantyMonths,
                CustomFields = customFields,
            };

            return currentComputer;
        }

        public void AddQuery(string queryType, string queryString) { // safely addes queries to the queryList monstrocity, built for expandability (c) 
            List<string> queryList = new List<string>();


            if (this.Queries.ContainsKey(queryType))
            {
                queryList = Queries[queryType];
                queryList.Add(queryString);
                this.Queries[queryType] = queryList;
                return;
            } else
            {
                queryList.Add(queryString);
                this.Queries.Add(queryType, queryList);
                return;
            }
        }


        private void RunWMI() { // runs all WMI queries

            Dictionary<string, string> resultDictionary = new Dictionary<string, string>();
            ManagementObjectCollection queryCollection;


            //Query system for Operating System information
            foreach (string wmiQuery in this.Queries["WMI"])
            {
                int count = 0;

                SelectQuery selectQuery = new SelectQuery(wmiQuery);
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(selectQuery);
            

                queryCollection = searcher.Get();

                foreach (ManagementObject m in queryCollection)
                {
                    // Display all properties.
                    foreach (PropertyData property in m.Properties)
                    {
                        string propertyValue = "<undefined>";
                        if  (!String.IsNullOrWhiteSpace(property.Value.ToString()))
                        {
                            propertyValue = property.Value.ToString().Trim();
                        }
                        if (!resultDictionary.ContainsKey(selectQuery.ClassName + "." + property.Name))
                        {
                            resultDictionary.Add(selectQuery.ClassName + "." + property.Name, propertyValue);
                        }
                        else
                        {
                            resultDictionary.Add(selectQuery.ClassName + "." + property.Name + "." + count.ToString(), propertyValue);
                        }
                    }
                    count++;
                }
            }

            this.Values = resultDictionary;
        }


        private void RunLocation() // Runs all code related to location & location sources
        {
            string location_string = "";
            foreach (string locationQuery in this.Queries["Location"])
            {
                if (locationQuery == "OU")
                {
                    try
                    {
                        int ouLevel;
                        bool ouLevelSuccess = int.TryParse(Settings["OULevel"], out ouLevel);
                        if (!ouLevelSuccess)
                        {
                            ouLevel = 1;
                        }
                        string[] machineOU;
                        using (var context = new PrincipalContext(ContextType.Domain))
                        using (var comp = ComputerPrincipal.FindByIdentity(context, Environment.MachineName))
                            machineOU = comp.DistinguishedName.Split(',').SkipWhile(s => !s.StartsWith("OU=")).ToArray();

                        location_string = machineOU[0].Split('=')[ouLevel];
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine("Could not get location from OU");
                        Trace.WriteLine(e.ToString());
                        Trace.WriteLine("Getting location from config file instead");
                        location_string = Settings["Location"];
                    }
                } else
                {
                    location_string = Settings["Location"];

                }
            }
            this.Values.Add("Location", location_string);

        }




        public string GetOutputVariable(string key)
        {
            if (this.Values.ContainsKey(key))
            {
                return this.Values[key];
            } else
            {
                return "";
            }
        }

        public string GetFormattedVariable(string key, string variable = "", string format="<name>=<var>") // produces formatted output, supposed to throw exception if no results in raw results
        {
            if (String.IsNullOrEmpty(variable))
            {
                format = "<var>";
            }
            if (this.Values.ContainsKey(key))
            {
                return format.Replace("<var>", this.Values[key]).Replace("<name>",variable);
            } else
            {
                return "ERROR: key \"" + key + "\" not found in the results of the query";
            }
        }



        public void Run() // supposed to run all queries of all types and handle per-type errors
        {
            this.RunWMI();
            this.RunLocation();
        }

    }



    class Marksman
    {



        static void Main(string[] args)
        {
            //TextWriterTraceListener listener = new TextWriterTraceListener(DateTime.Today.ToShortDateString() + "marksman.log");
            // listener.Flush();
            //Trace.Listeners.Add(listener);
            Trace.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + ": Started application.");

            var debugTimer = new Stopwatch();
            System.Collections.Specialized.NameValueCollection appSettings = System.Configuration.ConfigurationManager.AppSettings;
            debugTimer.Start();

            SnipeItApi snipe = new SnipeItApi();
            snipe.ApiSettings.ApiToken = appSettings["API"];
            snipe.ApiSettings.BaseUrl = new Uri(appSettings["BaseURI"]);

            // some test queries for analyzing duplicates
            /*
            SnipeSharp.Endpoints.SearchFilters.SearchFilter assetFilter = new SnipeSharp.Endpoints.SearchFilters.SearchFilter()
            {
                Search = "156TR4090-03"
            };

            List<SnipeSharp.Endpoints.Models.Asset> snipeAssets = snipe.AssetManager.FindAll(assetFilter).Rows;

            */
            Sentry mySentry = new Sentry(appSettings); // creating new Sentry (we can have multiple for parallel execution at a later point)

            /* CSystemType
            Data type: uint16
            Access type: Read-only
     
                Qualifiers: MappingStrings ("")

                Type of the computer in use, such as laptop, desktop, or Tablet.
                Under Win32_ComputerSystem class in WMI
             */

            // Adding what we want
            mySentry.AddQuery("WMI", "SELECT Name, Manufacturer, Model, PCSystemType FROM Win32_ComputerSystem");
            mySentry.AddQuery("WMI", "SELECT IdentifyingNumber FROM Win32_ComputerSystemProduct");
            mySentry.AddQuery("WMI", "SELECT Name FROM Win32_BIOS");
            mySentry.AddQuery("WMI", "SELECT Manufacturer,Name,MACAddress FROM Win32_NetworkAdapter WHERE NetEnabled=true AND AdapterTypeId=0 AND netConnectionStatus=2");
            mySentry.AddQuery("WMI", "SELECT Manufacturer,Model,SerialNumber,Size FROM Win32_DiskDrive");
            mySentry.AddQuery("WMI", "SELECT EndingAddress FROM Win32_MemoryArray");
            mySentry.AddQuery("WMI", "SELECT Name FROM Win32_DesktopMonitor");
            mySentry.AddQuery("WMI", "SELECT Manufacturer,Product,SerialNumber FROM Win32_BaseBoard");
            mySentry.AddQuery("WMI", "SELECT Name,NumberOfCores,NumberOfLogicalProcessors FROM Win32_Processor");

            bool getOU = false;
            bool getOUSuccess = Boolean.TryParse(appSettings["OUEnabled"], out getOU);
            if (getOUSuccess && getOU)
            {
                mySentry.AddQuery("Location", "OU");
            } else
            {
                mySentry.AddQuery("Location", "Config");
            }



            mySentry.Run();

            SnipeSharp.Endpoints.Models.Asset currentComputer = mySentry.GetAsset(appSettings, snipe);
            Broker.syncAsset(snipe, currentComputer);
            debugTimer.Stop();

            Trace.WriteLine("Total program execution time " + debugTimer.ElapsedMilliseconds + "ms.");
            Trace.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + ": Exiting application.");
            Trace.WriteLine(" ");
        }
    }
}
