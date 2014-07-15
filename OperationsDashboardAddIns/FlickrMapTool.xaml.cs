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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel.Composition;
using System.Runtime.Serialization;
using ESRI.ArcGIS.OperationsDashboard;
using client = ESRI.ArcGIS.Client;

namespace OperationsDashboardAddIns
{
  /// <summary>
  /// Use this map tool to search for photos from Flickr based on a tag and the current map extent
  /// </summary>
  [Export("ESRI.ArcGIS.OperationsDashboard.MapTool")]
  [ExportMetadata("DisplayName", "Search Flickr Photos")]
  [ExportMetadata("Description", "Search photos from Flickr using on the current map extent")]
  [ExportMetadata("ImagePath", "/OperationsDashboardAddIns;component/Images/FlickrPin16.png")]
  [DataContract]
  public partial class FlickrMapTool : UserControl, IMapTool
  {
    public FlickrMapTool()
    {
      InitializeComponent();
    }

    #region IMapTool

    /// <summary>
    /// The MapWidget property is set by the MapWidget that hosts the map tools. The application ensures that this property is set when the
    /// map widget containing this map tool is initialized.
    /// </summary>
    public MapWidget MapWidget { get; set; }

    /// <summary>
    /// OnActivated is called when the map tool is added to the toolbar of the map widget in which it is configured to appear. 
    /// Called when the operational view is opened, and also after a custom toolbar is reverted to the configured toolbar,
    /// and during toolbar configuration.
    /// </summary>
    public void OnActivated()
    {
    }

    /// <summary>
    ///  OnDeactivated is called before the map tool is removed from the toolbar. Called when the operational view is closed,
    ///  and also before a custom toolbar is installed, and during toolbar configuration.
    /// </summary>
    public void OnDeactivated()
    {
    }

    /// <summary>
    ///  Determines if a Configure button is shown for the map tool.
    /// </summary>
    public bool CanConfigure
    {
      get { return false; }
    }

    /// <summary>
    ///  Provides functionality for the map tool to be configured by the end user through a dialog.
    /// </summary>
    public bool Configure(System.Windows.Window owner)
    {
      // Implement this method if CanConfigure returned true.
      throw new NotImplementedException();
    }

    #endregion

    /// <summary>
    /// Provides the behaviour when the user clicks the map tool.
    /// </summary>
    private void Button_Click(object sender, RoutedEventArgs e)
    {
      // Install a custom toolbar 
      MapWidget.SetToolbar(new FlickrMapToolbar(MapWidget));

      // Set the Checked property of the ToggleButton to false after work is complete.
      ToggleButton.IsChecked = false;
    }

  }
}
