//Copyright 2014 Esri
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.​

using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using ESRI.ArcGIS.OperationsDashboard;
using FlickrNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using client = ESRI.ArcGIS.Client;

namespace OperationsDashboardAddIns
{
  /// <summary>
  /// Use this map tool to search for photos from Flickr based on a tag and the current map extent
  /// </summary>
  public partial class FlickrMapToolbar : UserControl, IMapToolbar, INotifyPropertyChanged
  {
    MapWidget _mapWidget = null;
    Flickr flickr;
    PhotoSearchOptions searchOps;
    List<PhotoInfo> photoInfos;
    client.GraphicsLayer pushpinsLayer;

    //Available tags
    private List<string> tags;
    public List<string> Tags
    {
      get { return tags; }
      set { SetField(ref tags, value, () => Tags); }
    }

    //User's selected tag
    private string seletcedTag;
    public string SeletcedTag
    {
      get { return seletcedTag; }
      set { SetField(ref seletcedTag, value, () => SeletcedTag); }
    }

    /// <summary>
    /// Instantiate the properties
    /// </summary>
    /// <param name="mapWidget"></param>
    public FlickrMapToolbar(MapWidget mapWidget)
    {
      InitializeComponent();

      // Store a reference to the MapWidget that the toolbar has been installed to.
      _mapWidget = mapWidget;

      //Set the Tags property so the UIs can be updated
      Tags = new List<string>() { "Weather", "Trails", "Flood", "Fire", "Wildlife" };

      //Get the FlickrManager and create a search options object. They help us talk to the Flickr API
      flickr = FlickrManager.GetInstance();
      searchOps = new PhotoSearchOptions();

      //The graphic layer which contains the pushpin graphics for the retrieved photos
      pushpinsLayer = new client.GraphicsLayer() { ID = "pushpinLyr" };

      //Info of the retrieved photos (for updating the photo info window)
      photoInfos = new List<PhotoInfo>();

      DataContext = this;
    }

    /// <summary>
    /// OnActivated is called when the toolbar is installed into the map widget.
    /// </summary>
    public void OnActivated()
    {
      //Add the graphic layer to the map 
      if (_mapWidget == null || _mapWidget.Map == null)
        return;
      _mapWidget.Map.Layers.Add(pushpinsLayer);

      //Set the default selected tag to the first one 
      SeletcedTag = Tags.First();

      //Specify the search options
      searchOps.ContentType = ContentTypeSearch.PhotosOnly;
      searchOps.Extras = PhotoSearchExtras.AllUrls | PhotoSearchExtras.Description | PhotoSearchExtras.Geo | PhotoSearchExtras.OwnerName;
      searchOps.SortOrder = PhotoSearchSortOrder.DatePostedDescending;
      searchOps.SafeSearch = SafetyLevel.Safe;

      //Info window - For simplicity purpose we directly manipulate the properties of the info window UI element
      MyInfoWindow.Map = _mapWidget.Map;
      pushpinsLayer.MouseEnter += graphicsLayer_MouseEnter;
      pushpinsLayer.MouseLeave += graphicsLayer_MouseLeave;
    }

    /// <summary>
    ///  OnDeactivated is called before the toolbar is uninstalled from the map widget. 
    /// </summary>
    public void OnDeactivated()
    {
      if (_mapWidget == null || _mapWidget.Map == null)
        return;

      //Discard the graphicsLayer
      pushpinsLayer.MouseEnter -= graphicsLayer_MouseEnter;
      pushpinsLayer.MouseLeave -= graphicsLayer_MouseLeave;

      if (_mapWidget.Map.Layers.Contains(pushpinsLayer))
      {
        try
        {
          _mapWidget.Map.Layers.Remove(pushpinsLayer);
        }
        catch
        {
        }
        pushpinsLayer = null;
      }
    }

    /// <summary>
    /// Finished with the toolbar, revert to the default toolbar.
    /// </summary>
    private void Done_Click(object sender, RoutedEventArgs e)
    {
      if (_mapWidget != null)
      {
        _mapWidget.SetToolbar(null);
      }
    }

    /// <summary>
    /// Do the photo search. Show the results on the map using pushpin. Set up the photoInfos object 
    /// so that we know the photo URL and its location when we want to show it
    /// </summary>
    private void SearchFlikr_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        pushpinsLayer.Graphics.Clear();

        //Add a tag to the search options
        searchOps.Tags = SeletcedTag;

        //Add the current extent to the search options
        //FlickrManager only takes latitude and longitude. We might need to transform the extent
        bool isGeographic = _mapWidget.Map.SpatialReference.WKID == 4326;
        WebMercator wm = new WebMercator();
        Envelope extent = _mapWidget.Map.Extent;
        if (!isGeographic)
        {
          Envelope extentToGeo = wm.ToGeographic(extent) as Envelope;
          searchOps.BoundaryBox = new BoundaryBox(extentToGeo.XMin, extentToGeo.YMin, extentToGeo.XMax, extentToGeo.YMax);
        }
        else
          searchOps.BoundaryBox = new BoundaryBox(extent.XMin, extent.YMin, extent.XMax, extent.YMax);

        //Do the search asynchronously
        flickr.PhotosSearchAsync(searchOps, (FlickrResult<PhotoCollection> photoColResult) =>
        {
          //Searh is finished. Manipulate the search results here
          if (photoColResult.Error != null)
            throw new Exception("Error in search results");

          PhotoCollection photoCol = photoColResult.Result;
          List<FlickrNet.Photo> pCollPublic = photoCol.Where(p => p.IsPublic && !string.IsNullOrEmpty(p.LargeUrl)).ToList();
          if (pCollPublic.Count == 0)
          {
            MessageBox.Show("No photos were found");
            return;
          }
          foreach (FlickrNet.Photo photo in pCollPublic)
          {
            //Show a pushpin at the location of the photo. Transformation might be required depending on the map spatial reference
            MapPoint photoLocation;
            if (!isGeographic)
              photoLocation = wm.FromGeographic(new MapPoint(photo.Longitude, photo.Latitude)) as MapPoint;
            else
              photoLocation = new MapPoint(photo.Longitude, photo.Latitude);

            //Create the pushpin graphic with a symbol and the photo location
            client.Graphic pushpin = new client.Graphic()
            {
              Symbol = FlickrPushpinSymbol.CreatePushpinSymbol(),
              Geometry = photoLocation
            };

            //Add the graphic to the layer
            pushpinsLayer.Graphics.Add(pushpin);

            //Add the photo info to the photoInfos list 
            //To avoid copy right infringement, we only pass the placeholder image's path instead of the photo's actual URL to PhotoInfo
            photoInfos.Add(new PhotoInfo(@"pack://application:,,,/OperationsDashboardAddIns;component/Images/PhotoPlaceHolder.png", photoLocation));
          }
        });
      }
      catch (Exception ex)
      {
        MessageBox.Show("Error searching for photos. " + ex.Message);
      }
    }

    #region Event handlers controlling showing/hiding the info window (the window which shows the retrieved photo)
    //Open the info window (with the retrieved photo) when mouse enters the pushpin graphic
    void graphicsLayer_MouseEnter(object sender, client.GraphicMouseEventArgs e)
    {
      //Get the pushpin graphic we are interested in
      client.Graphic pushpin = e.Graphic;

      //Get the photoInfo object associated with the pushpin graphic 
      //From the photoInfo object we will get the URL of the photo
      var photoInfo = photoInfos.FirstOrDefault(pi => pi.Location.X == (pushpin.Geometry as MapPoint).X && pi.Location.Y == (pushpin.Geometry as MapPoint).Y);
      if (photoInfo == null)
        return;

      //Set the content of the info window to the target photoInfo
      //Specify the location where it should open
      //Then open it
      MyInfoWindow.Content = photoInfo;
      MyInfoWindow.Anchor = pushpin.Geometry as MapPoint;
      MyInfoWindow.Placement = client.Toolkit.InfoWindow.PlacementMode.Bottom;
      MyInfoWindow.IsOpen = true;
    }

    //Close the info window when mouse leaves
    void graphicsLayer_MouseLeave(object sender, client.GraphicMouseEventArgs e)
    {
      MyInfoWindow.IsOpen = false;
    }
    #endregion

    #region implement INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged<T>(Expression<Func<T>> expression)
    {
      if (expression == null) return;
      MemberExpression body = expression.Body as MemberExpression;
      if (body == null) return;
      OnPropertyChanged(body.Member.Name);
    }

    private void OnPropertyChanged(string propertyName)
    {
      PropertyChangedEventHandler handler = PropertyChanged;
      if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, Expression<Func<T>> expression)
    {
      if (EqualityComparer<T>.Default.Equals(field, value)) return false;
      field = value;
      OnPropertyChanged(expression);
      return true;
    }
    #endregion
  }

  class PhotoInfo
  {
    public PhotoInfo(string URL, MapPoint Location)
    {
      this.URL = URL;
      this.Location = Location;
    }

    public string URL { get; private set; }
    public MapPoint Location { get; private set; }
  }
}
