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

using ESRI.ArcGIS.OperationsDashboard;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using client = ESRI.ArcGIS.Client;

namespace OperationsDashboardAddIns
{
  /// <summary>
  /// Use this feature action to generate a profile graphh based on the input line feature.
  /// 
  /// To know more: https://developers.arcgis.com/rest/elevation/api-reference/profile.htm
  /// 
  /// Note: 
  /// The Profile Graph Widget might be present in the operation view in order to display the profile line
  /// created by this feature action
  /// 
  /// </summary>
  [Export("ESRI.ArcGIS.OperationsDashboard.FeatureAction")]
  [ExportMetadata("DisplayName", "Generate Profile Graph")]
  [ExportMetadata("Description", "Use this feature action to create a profile graph. Profile Graph Widget is required.")]
  [ExportMetadata("ImagePath", "/OperationsDashboardAddIns;component/Images/ElevationProfile.png")]
  public class ProfileGraphFromFeature : IFeatureAction
  {
    //The profile line created by the Elevation Analysis services of the ArcGIS REST API 
    public client.Geometry.Polyline Profile { get; private set; }

    public ProfileGraphFromFeature()
    {
    }

    #region IFeatureAction

    /// <summary>
    ///  Determines if a Configure button is shown for the feature action.
    ///  Provides an opportunity to gather user-defined settings.
    /// </summary>
    /// <value>True if the Configure button should be shown, otherwise false.</value>
    public bool CanConfigure
    {
      get { return false; }
    }

    /// <summary>
    ///  Provides functionality for the feature action to be configured by the end user through a dialog.
    ///  Called when the user clicks the Configure button next to the feature action.
    /// </summary>
    /// <param name="owner">The application window which should be the owner of the dialog.</param>
    /// <returns>True if the user clicks ok, otherwise false.</returns>
    public bool Configure(System.Windows.Window owner)
    {
      // Implement this method if CanConfigure returned true.
      throw new NotImplementedException();
    }

    /// <summary>
    /// Determines if the feature action can be executed based on the specified data source and feature, before the option to execute
    /// the feature action is displayed to the user.
    /// </summary>
    /// <param name="dataSource">The data source which will be subsequently passed to the Execute method if CanExecute returns true.</param>
    /// <param name="feature">The data source which will be subsequently passed to the Execute method if CanExecute returns true.</param>
    /// <returns>True if the feature action should be enabled, otherwise false.</returns>
    public bool CanExecute(ESRI.ArcGIS.OperationsDashboard.DataSource dataSource, client.Graphic feature)
    {
      //CanExecute = false if the feature's data source is a stand alone data source (i.e. map widget is null)
      MapWidget mw = MapWidget.FindMapWidget(dataSource);
      if (mw == null)
        return false;

      //CanExecute = false if the input feature's geometry is null
      if (feature.Geometry == null)
        return false;

      //CanExecute = false if the input feature is not a polyline
      if (feature.Geometry.GetType() != typeof(client.Geometry.Polyline))
        return false;

      return true;
    }

    /// <summary>
    /// Execute is called when the user chooses the feature action from the feature actions context menu. Only called if
    /// CanExecute returned true.
    /// </summary>
    public async void Execute(ESRI.ArcGIS.OperationsDashboard.DataSource dataSource, client.Graphic feature)
    {
      //This feature action relies on a profile graph widget to create a graph based on the profile line
      //Check if the view has a ProfileGraphWidget widget to display the output graph
      ProfileGraphFromMap profileWidget = OperationsDashboard.Instance.Widgets.FirstOrDefault(w => w.GetType() == typeof(ProfileGraphFromMap)) as ProfileGraphFromMap;
      if (profileWidget == null)
      {
        MessageBox.Show("Add the Profile Graph Widget to the view to execute this feature action", "Profile Graph Widget Required");
        return;
      }

      //Send the feature to the Elevation Analysis service to get the profile line
      ProfileService service = new ProfileService();
      Profile = await service.GetProfileLine(feature.Geometry);
      if (Profile == null)
      {
        MessageBox.Show("Failed to get elevation profile");
        return;
      }

      //Send the result (Profile) to the profile graph widget and ask it to generate the graph
      profileWidget.MapWidget = MapWidget.FindMapWidget(dataSource);
      profileWidget.Profile = Profile;
      profileWidget.UpdateControls();
    }

    #endregion
  }
}
