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
using ESRI.ArcGIS.Client.Symbols;
using ESRI.ArcGIS.OperationsDashboard;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using client = ESRI.ArcGIS.Client;

namespace OperationsDashboardAddIns
{
  /// <summary>
  /// Use this widget to sketch a line on map to generate a profile graph. 
  /// User can also use this widget with the Profile Graph Feature Action to generate profile graphs for features
  /// 
  /// To know more: https://developers.arcgis.com/rest/elevation/api-reference/profile.htm
  /// 
  /// Note: 
  /// In this widget we only assume that a view has only one map widget 
  /// Developers should consider allowing users to select the target map widget
  /// </summary>
  [Export("ESRI.ArcGIS.OperationsDashboard.Widget")]
  [ExportMetadata("DisplayName", "Profile Graph")]
  [ExportMetadata("Description", "Use this widget to create a profile graph based on a map sketch or a feature.")]
  [ExportMetadata("ImagePath", "/OperationsDashboardAddIns;component/Images/ElevationProfile.png")]
  [ExportMetadata("DataSourceRequired", true)]
  [DataContract]
  public partial class ProfileGraphFromMap : UserControl, IWidget, INotifyPropertyChanged
  {
    //A temporary graphic layer for the selected data point
    client.GraphicsLayer selecteDataPointLyr;
    //A temporary graphic layer for the map sketch
    client.GraphicsLayer mapSketchLyr;
    //The geometry of the sketch 
    client.Geometry.Polyline sketchGeometry;
    //A temporary graphic layer that traces user's mouse movement when sketching the profile line
    client.GraphicsLayer feedBackLyr = new client.GraphicsLayer() { ID = "traceLineFeefbackLyr" };

    //The map widget that communicates with the profile graph
    //
    //Note: 
    //In this widget we only assume that a view has only one map widget 
    //Developers should consider allowing users to select the target map widget
    public MapWidget MapWidget { get; set; }

    private Visibility isGraphVisible;
    public Visibility IsGraphVisible
    {
      get { return isGraphVisible; }
      set { SetField(ref isGraphVisible, value, () => IsGraphVisible); }
    }

    private bool canClearGraph;
    public bool CanClearGraph
    {
      get { return canClearGraph; }
      set { SetField(ref canClearGraph, value, () => CanClearGraph); }
    }

    //The geometry of the profile line 
    public client.Geometry.Polyline Profile { get; set; }

    //An array storing the m (distance) and z (elevation) values of each point on the profile line
    //This field is used to create the profile graph
    private KeyValuePair<double, double>[] mzPairs;
    public KeyValuePair<double, double>[] MZPairs
    {
      get { return mzPairs; }
      set { SetField(ref mzPairs, value, () => MZPairs); }
    }

    public ProfileGraphFromMap()
    {
      InitializeComponent();

      selecteDataPointLyr = new client.GraphicsLayer() { ID = "selecteDataPointLyr" };
      mapSketchLyr = new client.GraphicsLayer() { ID = "mapSketchLyr" };

      Caption = "Profile Graph Widget";

      IsGraphVisible = Visibility.Collapsed;

      DataContext = this;
    }

    #region Get the sketch line from user's input
    //When the Draw Line button is clicked:
    //The skecth (a polyline graphic) will be created and will be added to the sketch layer
    //The sketch layer will be added to the map for display
    //2 map events will be registered for user's interaction
    private void AddGraphics_Click(object sender, RoutedEventArgs e)
    {
      //Take the first map widget from the view
      MapWidget mapWidget = OperationsDashboard.Instance.Widgets.First(w => w.GetType() == typeof(MapWidget)) as MapWidget;
      if (mapWidget == null || mapWidget.Map == null)
        return;

      MapWidget = mapWidget;
      client.Map map = MapWidget.Map;

      //First, remove any existing graphic layer on the map
      RemoveAllGraphicLayers();

      //Create the geometry (a polyline) for the sketch
      sketchGeometry = new client.Geometry.Polyline();
      sketchGeometry.SpatialReference = map.SpatialReference;
      sketchGeometry.Paths.Add(new client.Geometry.PointCollection());

      //Create the sketch with the geometry and a symbol
      client.Graphic sketch = new client.Graphic()
      {
        Symbol = SimplePolylineSymbol.CreateLineSymbol(),
        Geometry = sketchGeometry,
      };

      //Add the sketch to the map sketch layer
      //mapSketchLyr.Graphics.Clear();
      mapSketchLyr.Graphics.Add(sketch);

      //Add the sketch layer back to the map
      map.Layers.Add(mapSketchLyr);

      //Register mouse click i.e. sketch begins
      //Register mouse double click i.e. sketch finishes
      map.MouseClick += map_MouseClick;
      map.MouseDoubleClick += map_MouseDoubleClick;
      map.MouseMove += map_MouseMove;
    }

    //Add a map point (from mouse click) to the geometry of the sketch
    //Create a temporary graphic to trace user's mouse movement
    void map_MouseClick(object sender, client.Map.MouseEventArgs e)
    {
      sketchGeometry.Paths[0].Add(e.MapPoint);


      //feedback layer
      feedBackLyr.Graphics.Clear();

      client.Map map = MapWidget.Map;

      #region Create a temporary graphic to trace user's mouse movement
      //Create a point collection using the last clicked point and the latest mouse position
      PointCollection pc = new PointCollection();
      pc.Add(sketchGeometry.Paths[0].Last());
      pc.Add(new MapPoint());

      //Create the geometry of the feedback line using the point collection
      client.Geometry.Polyline feedbackGeomrtry = new client.Geometry.Polyline();
      feedbackGeomrtry.SpatialReference = map.SpatialReference;
      feedbackGeomrtry.Paths.Add(pc);

      //Create the feedback line with the geometry and a symbol
      client.Graphic feedback = new client.Graphic()
      {
        Symbol = SimplePolylineSymbol.CreateLineSymbol(),
        Geometry = feedbackGeomrtry,
      };
      #endregion

      //Add the feedback line to the feedback layer 
      feedBackLyr.Graphics.Add(feedback);

      //Add the layer to the map if we haevn't done so
      if (!map.Layers.Contains(feedBackLyr))
        map.Layers.Add(feedBackLyr);
    }

    //Add a temporary graphic to trace user's mouse movement
    void map_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
      //Do nothing if user hasn't added the first point
      if (sketchGeometry.Paths[0].Count == 0)
        return;

      //Update the second point of the feedback layer with the current map point
      client.Map map = MapWidget.Map;
      (feedBackLyr.Graphics.First().Geometry as Polyline).Paths[0][1] = map.ScreenToMap(e.GetPosition(map));
    }

    //Finish the sketch when user double-clicks on the map
    async void map_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      client.Map map = MapWidget.Map;

      //Unregister the events
      map.MouseClick -= map_MouseClick;
      map.MouseDoubleClick -= map_MouseDoubleClick;
      map.MouseMove -= map_MouseMove;

      //Make a call to the REST API to get the profile line from the sketch
      ProfileService service = new ProfileService();
      Profile = await service.GetProfileLine(sketchGeometry);

      //if we fail to get the profile line, clear all graphics from the map
      if (Profile == null)
      {
        MessageBox.Show("Failed to get elevation profile");
        RemoveAllGraphicLayers();
        return;
      }

      //Update the graph with the profile info
      UpdateControls();

      CanClearGraph = true;
    }
    #endregion

    #region Create Profile Graph
    //Create the array of key/value pairs using the m and z values of each point on the profile line 
    //Then set the profile graph as visible
    public void UpdateControls()
    {
      bool createGraphResult = CreateGraphData();

      if (createGraphResult == false)
      {
        MessageBox.Show("Failed to generate the profile graph");
        RemoveAllGraphicLayers();
        return;
      }

      IsGraphVisible = Visibility.Visible;

      CanClearGraph = true;
    }

    //Create the array of key/value pairs using the m and z values of each point on the profile line 
    private bool CreateGraphData()
    {
      //Get the collection of points from the profile line
      PointCollection profilePointCol = Profile.Paths.FirstOrDefault();
      if (profilePointCol == null || profilePointCol.Count == 0)
        return false;

      //Create a key/value pair for each point in the collection
      List<KeyValuePair<double, double>> kvps = new List<KeyValuePair<double, double>>();
      foreach (MapPoint point in profilePointCol)
        kvps.Add(new KeyValuePair<double, double>(point.M, point.Z));

      //Feed MZPairs with the key/value pairs so that the graph can be updated
      MZPairs = kvps.ToArray();

      return true;
    }
    #endregion

    #region Handle the graphic layers and user's interaction with the profile graph
    //Show a point on the profile line when user clicks on a data point on the graph
    private void LineSeries_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      //Remove any existing graphic from the data point layer
      selecteDataPointLyr.Graphics.Clear();

      //When ClearGraphics_Click occurs, all data points will be removed and no new data points will be added
      //In this case, skip the rest of the method
      if (e.RemovedItems.Count > 0 && e.AddedItems.Count == 0)
        return;

      PointCollection profilePointCol = Profile.Paths.FirstOrDefault();
      if (profilePointCol == null || profilePointCol.Count == 0)
        return;

      //From the profile line, we get the map point whose m value is the same 
      //as user's selected data point on the graph
      double m = ((KeyValuePair<double, double>)e.AddedItems[0]).Key; //M value of the data point selected from the graph
      MapPoint mapPoint = profilePointCol.FirstOrDefault(mp => mp.M == m);  //Get the map point from the profile line with the same m value
      if (mapPoint == null)
        return;

      //Create a point graphic with the map point and a symbol
      client.Graphic dataPointGraphic = new client.Graphic()
      {
        Symbol = SimplePointSymbol.CreatePointSymbol(),
        Geometry = mapPoint
      };
      //Add the graphic to the data point layer
      selecteDataPointLyr.Graphics.Add(dataPointGraphic);

      //If we can find the data point layer, we first remove it 
      if (MapWidget.Map.Layers.Contains(selecteDataPointLyr))
        MapWidget.Map.Layers.Remove(selecteDataPointLyr);

      //Add the point layer back to make sure it always stay on top
      MapWidget.Map.Layers.Add(selecteDataPointLyr);
    }

    //Clear all graphics and hide the graph when ClearGraphics is clicked
    private void ClearGraphics_Click(object sender, RoutedEventArgs e)
    {
      //Remove the graphics and the graphic layers
      RemoveAllGraphicLayers();

      //Hide the graph
      IsGraphVisible = Visibility.Collapsed;

      CanClearGraph = false;
    }

    //Remove the graphics on the sketch layer as well as the data point layer when:
    //Clear Graphics is clicked
    //User adds a new profile line
    //The widget is being deactivated
    private void RemoveAllGraphicLayers()
    {
      var mapLyrs = MapWidget.Map.Layers;

      //Remove the graphics on the sketch layer 
      if (mapLyrs.Contains(mapSketchLyr))
      {
        mapSketchLyr.Graphics.Clear();
        mapLyrs.Remove(mapSketchLyr);
      }
      //Remove the graphics on the data point layer
      if (mapLyrs.Contains(selecteDataPointLyr))
      {
        selecteDataPointLyr.Graphics.Clear();
        mapLyrs.Remove(selecteDataPointLyr);
      }
      //Remove the graphics on the map feedback layer 
      if (mapLyrs.Contains(feedBackLyr))
      {
        feedBackLyr.Graphics.Clear();
        mapLyrs.Remove(feedBackLyr);
      }
    }
    #endregion

    #region IWidget Members
    private string _caption = "Default Caption";
    /// <summary>
    /// The text that is displayed in the widget's containing window title bar. This property is set during widget configuration.
    /// </summary>
    [DataMember(Name = "caption")]
    public string Caption
    {
      get { return _caption; }
      set { if (value != _caption) _caption = value; }
    }

    /// <summary>
    /// The unique identifier of the widget, set by the application when the widget is added to the configuration.
    /// </summary>
    [DataMember(Name = "id")]
    public string Id { get; set; }

    /// <summary>
    /// OnActivated is called when the widget is first added to the configuration, or when loading from a saved configuration, after all 
    /// widgets have been restored. Saved properties can be retrieved, including properties from other widgets.
    /// Note that some widgets may have properties which are set asynchronously and are not yet available.
    /// </summary>
    public void OnActivated()
    {
    }

    /// <summary>
    ///  OnDeactivated is called before the widget is removed from the configuration.
    /// </summary>
    public void OnDeactivated()
    {
      if (MapWidget == null)
        return;

      RemoveAllGraphicLayers();
    }

    /// <summary>
    ///  Determines if the Configure method is called after the widget is created, before it is added to the configuration. Provides an opportunity to gather user-defined settings.
    ///  
    /// No config is needed so CanConfigure returns false
    /// </summary>
    public bool CanConfigure
    {
      get { return false; }
    }

    /// <summary>
    ///  Provides functionality for the widget to be configured by the end user through a dialog.
    /// </summary>
    public bool Configure(Window owner, IList<DataSource> dataSources)
    {
      throw new NotImplementedException();
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
}
