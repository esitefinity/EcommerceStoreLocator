using System;
using System.Linq;
using System.Web.UI.WebControls;
using Telerik.Sitefinity.DynamicModules;
using Telerik.Sitefinity.Utilities.TypeConverters;
using System.Device.Location;
using Telerik.Sitefinity.DynamicModules.Model;
using System.Net;
using System.IO;
using System.Xml;
using System.ComponentModel;

/*
 * Handy References:
 * https://developers.google.com/maps/documentation/geocoding/
 * http://www.sitefinity.com/documentation/documentationarticles/installation-and-administration-guide/system-settings/registering-a-new-widget-in-sitefinity/
 * https://developers.google.com/maps/documentation/javascript/overlays?hl=en-US#AddingOverlays
 * http://www.sitefinity.com/blogs/gabe-sumners-blog/2012/01/12/building_real-world_modules_with_sitefinity_rsquo_s_new_module_builder
 * http://www.sitefinity.com/blogs/josh-morales-blog/2012/01/19/retrieving_data_from_dynamic_modules_using_the_module_builder_api
*/

/*
 * BEFORE YOU BEGIN:
 * The ASCX page contains a link to the google maps api. This api requires you to get a "key" before you can link
 * to their maps. You must change the url text "API_KEY" with your own unique key given to you by Google.
 *      <script type="text/javascript" src="https://maps.googleapis.com/maps/api/js?key=API_KEY&v=3.exp&sensor=false"></script>
 *
 * Getting a key is free and easy.  Check out this link for more information:
 *      https://developers.google.com/maps/documentation/javascript/tutorial#api_key
 * 
 * NOTE: The distance drop down list "ddlDistance" will NOT fire its ddlDistance_SelectedIndexChanged event when the first
 * item in the list is selected by default when the control exists on a Sitefinity page.  To make this work you must
 * edit the Titles and Properties section of the Sitefinity page (in the backend) that contains this Store Locator widget
 * and set the "Advnaced options", "Enable View State" checkbox to true.  If you do not enable the view state property of
 * the Sitefinity page, after you select a distance from the drop down, and then re-select the first item ("All Stores"), 
 * the widget will not refresh and the display will be blank.
 * 
 * The store's "Notes" field is not displayed on the ascx page but you could add a simple tag:
 *      <%# Eval("Notes")%>
 * to display the store notes as well.
 * 
 * This project uses the System.Device.Location namespace which is specific to .Net Framework 4.5!
 * A reference to the System.Device assembly was included in the project's references.
 * If you want to use an earlier version of .Net (or not include the System.Device assembly) you
 * would need to make the following changes to the code:
 * Create your own class called GeoCoordinate with two properties of type double (longitude and latitude).
 * Add a constructor overload to pass in the longitude and latitude as "double" type parameters that sets these properties.
 * Create a new method in your class called "GetDistanceTo" which accepts one double parameter:
 *      public double GetDistanceTo(GeoCoordinate other)
 * The implementation of the GetDistanceTo method will need to perform a distance calculation from the object's latitude
 * and longtiude properties to the "other" (parameter) latitude and longtude values and return the distance
 * in meters.  You should be able to find an good distance calculation formula if you search the web.
 * 
 * The store name (Title) is a clickable link.  When clicked, the google map will update to show the location of the store.
 * 
 * Possible Enhancements Ideas:
 * Extend the project to display the store's "Notes" for the currently selected store when the customer click's on a store's title.
 * On the Google javascript to create the store marker for the map, set the "title" property to the name of the store.
 * Extend the map display to popup store information (title, notes, phone, etc) in a popup window when marker is clicked on the map.
 */

namespace SitefinityWebApp.Custom
{
    public partial class StoreLocatorCustom : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        { 
            btnFindStores.Command += btnFindStores_Command;
            ddlDistance.SelectedIndexChanged += new EventHandler(ddlDistance_SelectedIndexChanged);
                        
            if (!IsPostBack)
            {
                BindStores();
            }
        }

        void BindStores()
        {            
            // To work with the data stored in your custom module built with Module Builder, you need to use the DynamicModuleManager
            var dynamicModuleManager = DynamicModuleManager.GetManager();

            // Next you need the data type for your dynamic data.  You can find this name in the backend at:
            // Advanced -> Settings -> Toolboxes -> Toolboxes -> PageControls -> Sections -> ContentToolboxSection -> Tools
            // Look for your dynamic module which will be something like "Telerik.Sitefinity.DynamicTypes.Model.StoreLocator.Store"
            // This is the name of the type that you need to resolve to determine the data type of your dynamic data.
            Type storeType = TypeResolutionService.ResolveType("Telerik.Sitefinity.DynamicTypes.Model.StoreLocator.Store");
            
            // Now get all your store's data using the GetDataItems(Type) method of the DynamicModuleManager where "Type" is
            // the data type (storeType) we found above.  We are adding an additional "Where" clause to retrieve only "Live" data.
            var stores = dynamicModuleManager
                .GetDataItems(storeType)
                .Where(s => s.Status == Telerik.Sitefinity.GenericContent.Model.ContentLifecycleStatus.Live);

            // Call our own method (below) that will take our "stores" data and calculate all of the distances to the stores.
            CalculateStoreDistances(stores);

            // Determine the distance range of stores to display based on the selection of distances (in miles)
            // from the DropDownList "ddlDistance".  The first selection "All Stores" will pick use an arbitrary large
            // distance to be sure to get all stores.  This is a bit easier than creating a dynamic Where clause below.
            int withinDistance = ddlDistance.SelectedValue == "0" ? 100000 : Convert.ToInt32(ddlDistance.SelectedValue);

            // We need to perform the sort of the stores in order to determine which store will be displayed
            // first on the page.  So sort the stores by distance and convert to a List which will bound
            // to the page's RadListView control
            IOrderedEnumerable<DynamicContent> sortedStores = stores.ToList()
                    .Where(x => Convert.ToInt32(TypeDescriptor.GetProperties(x)["Distance"].GetValue(x)) <= withinDistance)
                    .OrderBy(y => TypeDescriptor.GetProperties(y)["Distance"].GetValue(y));
                        
            DynamicContent firstStore = sortedStores.FirstOrDefault();
            if (firstStore != null)
            {
                // We have the first store in the list of stores so.

                // Use TypeDescriptor.GetProperties() method to get a list of all properties 
                // of our DynamicConent object.
                var properties = TypeDescriptor.GetProperties(firstStore);

                // Get a reference to the dynamic properties named "Latitude" and "Longitude"
                PropertyDescriptor latProperty = properties["Latitude"];
                PropertyDescriptor longProperty = properties["Longtitude"];

                // Get the latitude and longitude property values and store the values
                // in the javascript so the google map will show the location of the first
                // store in the list by default.
                litDefaultLat.Text = latProperty.GetValue(firstStore).ToString();
                litDefaultLong.Text = longProperty.GetValue(firstStore).ToString();
            }

            // Bind the RadListView control to show the list of stores
            listStores.DataSource = sortedStores;
            listStores.DataBind();

            // Display the number of stores
            lblStoreCount.Text = sortedStores.Count().ToString();
        }

        /// <summary>
        /// CalculateStoreDistances: Calculate all the store distances based on our "source" zip code 
        /// from the textbox "txtSourceZip" on our ascx page.  If the textbox is empty we will return zero distances.
        /// For each store we will calculate the distance to the store and save that distance back into the store
        /// object.  In addition we will save the latitude and longitude of the store back into the store object
        /// as well.  When we return to display the initial page we will want the lat,long of the first store
        /// to initialize our google map.
        /// </summary>
        /// <param name="stores">IQueryable<DynamicContent> store data returned from the DynamicModuleManager</param>
        /// <returns>The stores Iqueryable with the "Distance", "Latitude" and "Longitude" properties set to their calculated values</returns>
        void CalculateStoreDistances(IQueryable<DynamicContent> stores)
        {           
            // Get the source zip code from the textbox
            string sourceZip = txtSourceZip.Text.Trim();

            // Get the latitude, longitude of the location of source zip code.  If the source zip code is blank
            // then skip calling the GetCoordinate() method to reduce Google API calls
            GeoCoordinate sourceCoords = String.IsNullOrWhiteSpace(sourceZip) ? new GeoCoordinate(0,0) : GetCoordinate(sourceZip);

            // Loop through each store getting its Lat,Long coords and calculating distance
            // from source zip code to the store's zip code.
            foreach (var store in stores)
            {
                // In order to get the "Zip" property value from the dynamic "store" type we use the
                // TypeDescriptor.GetProperties() method to get a list of all properties for the store object.
                var properties = TypeDescriptor.GetProperties(store);

                // Once we have a list of store object's properties we can get an individual property by name
                PropertyDescriptor zipProperty = properties["Zip"];

                // With the PropertyDescriptor object, we can call its GetValue() method to get the property's value
                string storeZip = zipProperty.GetValue(store).ToString();
                   
                // Determine the store's location (latitude and longitude).  If you enter the coords
                // directly into the store in the backend then those coords will be used to show the
                // store on the map.  If the store's save lat and long are zero, then its zip code
                // will be used to approximate's its location on the map.
                GeoCoordinate storeCoords = null;
                double storeLatitude = Convert.ToDouble( properties["Latitude"].GetValue(store));
                double storeLongitude = Convert.ToDouble(properties["Longtitude"].GetValue(store));
                if (storeLatitude == 0 && storeLatitude == 0)
                {
                    // No latitude and longitude were saved in the store's data.
                    // Call our GetCoordinate method which converts a zip code into a GeoCoordinate object
                    // containing the approximate latitude and longitude of the zip code's location.
                    storeCoords = GetCoordinate(storeZip);
                }                                
                else 
                {
                    // A specific lat,long was entered into the store's data so use those
                    // coordinates to pinpoint the store.
                    storeCoords = new GeoCoordinate(storeLatitude, storeLongitude);
                }

                // We can now get the distance (in meters) from our source zip coordinates to our store's coordinates
                // by calling the GetDistanceTo() method of the GeoCoordinate (storeCoords) object.
                // If the source zip code is blank we will skip the call to GetDistanceTo() to save a bit of time.
                double distanceMeters = String.IsNullOrWhiteSpace(sourceZip) ? 0 : sourceCoords.GetDistanceTo(storeCoords);

                // For this demo we are converting meters into miles.
                double distanceMiles = distanceMeters * 0.000621371192;
                
                // Now that we have the distance to the store, we can save that distance back into our
                // store object's "Distance" property so the value will be available when we 
                // bind of RadListView control
                PropertyDescriptor distanceProperty = properties["Distance"];
                distanceProperty.SetValue(store, (decimal)Math.Round(distanceMiles, 2));

                // We will also save each store's latitude and longitude in pre-defined properties
                // in our store object.  Later when we sort the data by distance we will take the
                // first store in the list and then grab its lat, long so we can initialize
                // the google map with the lat,long of the first store in the list.
                PropertyDescriptor latProperty = properties["Latitude"];
                latProperty.SetValue(store, (decimal)Math.Round(storeCoords.Latitude, 5));

                PropertyDescriptor longProperty = properties["Longtitude"];
                longProperty.SetValue(store, (decimal)Math.Round(storeCoords.Longitude, 5));
            } // foreach store
        }

        /// <summary>
        /// Called when the Find Stores button is clicked.  Simply rebinds the store list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnFindStores_Command(object sender, CommandEventArgs e)
        {
            BindStores();
        }

        /// <summary>
        /// Called when the used changes distance drop down.  Simply rebinds the store list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ddlDistance_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindStores();
        }
    
        /// <summary>
        /// GetCoordinate: Returns a GetCoordinate object containing the latitude and longitude of the
        /// location of the given zip code.
        /// This example uses a call to Google's Geocoding API (V3) to convert the zip code into latitude and long
        /// See: https://developers.google.com/maps/documentation/geocoding/
        /// </summary>
        /// <param name="zip"></param>
        /// <returns></returns>
        GeoCoordinate GetCoordinate(string zip)
        {
            if (String.IsNullOrWhiteSpace(zip))
            {
                return new GeoCoordinate(0, 0);
            }

            // Setup the url for posting to google.  We are requesting "XML" as the return data set for the given zip code
            string url = string.Format("http://maps.googleapis.com/maps/api/geocode/xml?address={0}&sensor=false", zip);

            string xmlResponse = PostRequest(url, new byte[1], 3000);

            // We now have an XML response string from Google so load it into an XmlDocument so we can parse the data.
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlResponse);

            // The status node tells us if Google is returned valid data "OK", or an error
            XmlNode nodeStatus = xmlDoc.SelectSingleNode("/GeocodeResponse/status");

            double sourceLat = 0;
            double sourceLong = 0;

            if (nodeStatus.InnerText == "OK")
            {
                // We have valid data from google so get the latitude and longtidue values from the returned XML.

                XmlNode nodeLat = xmlDoc.SelectSingleNode("/GeocodeResponse/result/geometry/location/lat");
                XmlNode nodeLong = xmlDoc.SelectSingleNode("/GeocodeResponse/result/geometry/location/lng");
                Double.TryParse(nodeLat.InnerText, out sourceLat);
                Double.TryParse(nodeLong.InnerText, out sourceLong);
            }
            else
            {
                // Note there seems to be a limit to the number of times this api is called.  Unsure how this limit
                // is determined:  If it is a limit to a quick # of calls or a # of calls in a given time period.
                // It is up to you to determine how you want to handle the situation where the data returned is not valid.
                // You can throw an error (not great) or return an zero lat, long GetCoordinate (better).
                throw new Exception("Error returned from Google : " + nodeStatus.InnerText);
                //return new GeoCoordinate(sourceLat, sourceLong);
            }

            return new GeoCoordinate(sourceLat, sourceLong);
        } // GetLatLong       

        public static string PostRequest(string url, byte[] data, int timeout, string contentType = "application/x-www-form-urlencoded", bool keepAlive = false)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                request.Method = "POST";
                request.Timeout = timeout;
                request.ContentType = contentType;
                request.KeepAlive = keepAlive;

                HttpWebResponse response = SendRequest(request, data);

                return ReadResponse(response.GetResponseStream());
            }
            catch (WebException ex)
            {
                return ReadResponse(ex.Response.GetResponseStream());
            }
        }

        private static HttpWebResponse SendRequest(HttpWebRequest request, byte[] data)
        {
            // Send the data out over the wire
            try
            {
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
            }
            catch (Exception ex)
            {
                throw new WebException("An error occured while connecting", ex);
            }

            HttpWebResponse response = null;

            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (Exception wex)
            {
                throw new WebException("An error occured while connecting", wex);
            }
            return response;
        }

        private static string ReadResponse(Stream response)
        {
            string responseString = "";
            using (StreamReader sr = new StreamReader(response))
            {
                responseString = sr.ReadToEnd();
                sr.Close();
            }

            return responseString;
        }
    } // class StoreLocatorCustom
    
}