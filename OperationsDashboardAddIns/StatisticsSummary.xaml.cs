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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using client = ESRI.ArcGIS.Client;
using opsDash = ESRI.ArcGIS.OperationsDashboard;

namespace OperationsDashboardAddIns
{
  /// <summary>
  /// A Widget is a dockable add-in class for Operations Dashboard for ArcGIS that implements IWidget. By returning true from CanConfigure, 
  /// this widget provides the ability for the user to configure the widget properties showing a settings Window in the Configure method.
  /// By implementing IDataSourceConsumer, this Widget indicates it requires a DataSource to function and will be notified when the 
  /// data source is updated or removed.
  /// </summary>
  [Export("ESRI.ArcGIS.OperationsDashboard.Widget")]
  [ExportMetadata("DisplayName", "Multi-Value Summary")]
  [ExportMetadata("Description", "A summary widget that displays the maximum, average, minimum and/or sum of a certain field")]
  [ExportMetadata("ImagePath", "/OperationsDashboardAddIns;component/Images/MultiValueSummary.png")]
  [ExportMetadata("DataSourceRequired", true)]
  [DataContract]
  public partial class StatisticsSummary : UserControl, IWidget, IDataSourceConsumer, INotifyPropertyChanged
  {
    //Id of the seletced data source
    [DataMember(Name = "dataSourceId")]
    public string DataSourceId { get; set; }
    private DataSource DataSource = null;

    //The Map Widget that works with this widget
    private MapWidget MapWidget = null;

    //The selected field
    [DataMember(Name = "field")]
    public string Field { get; set; }

    //User's selected statistics
    [DataMember(Name = "statistics")]
    List<Statistic> Statistics { get; set; }

    //Leading and trailng texts appended to the value
    [DataMember(Name = "trailingText")]
    public string TrailingText { get; set; }

    [DataMember(Name = "leadingText")]
    public string LeadingText { get; set; }

    //List of items that populate the summary page
    private ObservableCollection<StatisticList> statisticsLists;
    public ObservableCollection<StatisticList> StatisticsLists
    {
      get { return statisticsLists; }
      set { SetField(ref statisticsLists, value, () => StatisticsLists); }
    }

    //A flag that controls if the current map extent will be part of the query parameter
    private bool useCurrentExtent;
    public bool UseCurrentExtent
    {
      get { return useCurrentExtent; }
      set
      {
        SetField(ref useCurrentExtent, value, () => UseCurrentExtent);
        CalculateStatistics();
      }
    }

    //A flag that controls whether the UseCurrentExtent checkbox will be enabled
    private bool canUseCurrentExtent;
    public bool CanUseCurrentExtent
    {
      get { return canUseCurrentExtent; }
      set
      {
        SetField(ref canUseCurrentExtent, value, () => CanUseCurrentExtent);

        //If we can't use the current map extent, we make sure the checkbox is unchecked
        if (CanUseCurrentExtent == false)
          UseCurrentExtent = false;
      }
    }

    //Output statistic values
    public double Max { get; set; }
    public double Avg { get; set; }
    public double Min { get; set; }
    public double Sum { get; set; }

    public StatisticsSummary()
    {
      InitializeComponent();

      DataContext = this;
    }

    #region IWidget Members

    private string caption = "Default Caption";
    /// <summary>
    /// The text that is displayed in the widget's containing window title bar. This property is set during widget configuration.
    /// </summary>
    [DataMember(Name = "caption")]
    public string Caption
    {
      get { return caption; }
      set { if (value != caption) caption = value; }
    }

    /// <summary>
    /// The unique identifier of the widget, set by the application when the widget is added to the configuration.
    /// </summary>
    [DataMember(Name = "id")]
    public string Id { get; set; }

    /// <summary>
    /// OnActivated is called when the widget is first added to the configuration, or when loading from a saved configuration, after all 
    /// widgets have been restored. Saved properties can be retrieved, including properties from other widgets.
    /// Note that some widgets may have properties which are set asynchronously and are not yet a+vailable.
    /// </summary>
    public void OnActivated()
    {
      //Always starts with assuming the CanUseCurrentExtent checkbox is unchecked;
      CanUseCurrentExtent = false;
    }

    /// <summary>
    ///  OnDeactivated is called before the widget is removed from the configuration.
    /// </summary>
    public void OnDeactivated()
    {
    }

    /// <summary>
    ///  Determines if the Configure method is called after the widget is created, before it is added to the configuration. Provides an opportunity to gather user-defined settings.
    /// </summary>
    /// <value>Return true if the Configure method should be called, otherwise return false.</value>
    public bool CanConfigure
    {
      get { return true; }
    }

    /// <summary>
    ///  Provides functionality for the widget to be configured by the end user through a dialog.
    /// </summary>
    /// <param name="owner">The application window which should be the owner of the dialog.</param>
    /// <param name="dataSources">The complete list of DataSources in the configuration.</param>
    /// <returns>True if the user clicks ok, otherwise false.</returns>
    public bool Configure(Window owner, IList<DataSource> dataSources)
    {
      // Show the configuration dialog. Pass the saved settings to populate the UIs
      Config.StatisticsSummaryDialog dialog = new Config.StatisticsSummaryDialog(Caption, DataSourceId, Field, Statistics, LeadingText, TrailingText)
      {
        Owner = owner
      };
      if (dialog.ShowDialog() != true)
        return false;

      // Retrieve the selected values from the configuration dialog.
      Caption = dialog.Caption;
      Statistics = dialog.SelectedStatistics;
      LeadingText = dialog.LeadingText;
      TrailingText = dialog.TrailingText;
      DataSourceId = dialog.SelectedDataSource.Id;
      Field = dialog.SelectedField.Name;

      if (String.IsNullOrEmpty(DataSourceId) || String.IsNullOrEmpty(Field))
        return false;

      return true;
    }

    //Calculate the statistics, then update the UI controls
    private void CalculateStatistics()
    {
      DataSource = opsDash.OperationsDashboard.Instance.DataSources.FirstOrDefault(ds => ds.Id == DataSourceId);
      if (DataSource == null || DataSource.IsBroken)
        return;

      foreach (Statistic statistic in Statistics)
        CalculateOneStatistic(statistic);

      ////Update StatisticsLists, which in turn will trigger the UI to update
      //UpdateControls();
    }

    //Calculate a single statistic
    private async void CalculateOneStatistic(Statistic statistic)
    {
      //Construct the query
      opsDash.Query query = new opsDash.Query()
      {
        ReturnGeometry = false,
        WhereClause = "1=1",
        Fields = new string[] { Field }
      };

      //If map widget is valid and UseCurrentExtent = true, we will set the current extent into the query
      if (MapWidget == null)
        query.SpatialFilter = null;
      else
      {
        if (UseCurrentExtent == false)
          query.SpatialFilter = null;
        else
          query.SpatialFilter = MapWidget.Map.Extent;
      }

      //Create a query task, pass in the query and the type of statistic then start the task
      List<Task<opsDash.QueryResult>> queryTasks = new List<Task<opsDash.QueryResult>>();
      opsDash.QueryResult queryResult = await DataSource.ExecuteQueryStatisticAsync(statistic, query);

      //Query results are back. Check the results
      if (queryResult.Canceled)
        return;

      //There should only be one feature carrying the requested value 
      if (queryResult.Features == null || queryResult.Features.Count != 1)
        return;

      //The feature should not be null and should have the field with the correct field name
      client.Graphic feature = queryResult.Features[0];

      //When we pan outside of the map extent and do the calculation, feature.Attributes[Field] will be null
      //Resetting all values to 0
      if (feature == null || feature.Attributes[Field] == null)
        Max = Min = Avg = Sum = 0;
      else
      {
        //Try to set the field into a double field
        double value = 0;
        Double.TryParse(feature.Attributes[Field].ToString(), out value);

        //Based on the requested, set the value to the output statsitic
        if (statistic == Statistic.Max)
          Max = value;
        else if (statistic == Statistic.Average)
          Avg = value;
        else if (statistic == Statistic.Min)
          Min = value;
        else if (statistic == Statistic.Sum)
          Sum = value;
      }

      //Update StatisticsLists, which in turn will trigger the UI to update
      UpdateControls();
    }

    //Update StatisticsLists so that the UI elements will be updated accordingly
    private void UpdateControls()
    {
      StatisticsLists = new ObservableCollection<StatisticList>();

      if (Statistics.Contains(Statistic.Max))
        StatisticsLists.Add(new StatisticList("Maximum", LeadingText, FormatValue(Max), TrailingText));
      if (Statistics.Contains(Statistic.Average))
        StatisticsLists.Add(new StatisticList("Average", LeadingText, FormatValue(Avg), TrailingText));
      if (Statistics.Contains(Statistic.Min))
        StatisticsLists.Add(new StatisticList("Minimum", LeadingText, FormatValue(Min), TrailingText));
      if (Statistics.Contains(Statistic.Sum))
        StatisticsLists.Add(new StatisticList("Sum", LeadingText, FormatValue(Sum), TrailingText));
    }

    //Helper method
    private string FormatValue(double Value)
    {
      //Determine if the input number is an integer or a double
      //if integer, show 0 decimal place; 
      //if double, show 2 decimal place
      //In both cases, add thousand separators 
      string format = Convert.ToInt32(Value) == Value ? "N0" : "N2";

      //return the value as a string after formatting it
      return Value.ToString(format);
    }
    #endregion

    #region IDataSourceConsumer Members

    /// <summary>
    /// Returns the ID(s) of the data source(s) consumed by the widget.
    /// </summary>
    public string[] DataSourceIds
    {
      get { return new string[] { DataSourceId }; }
    }

    /// <summary>
    /// Called when a DataSource is removed from the configuration. 
    /// </summary>
    /// <param name="dataSource">The DataSource being removed.</param>
    public void OnRemove(DataSource dataSource)
    {
      // Respond to data source being removed.
      DataSourceId = null;
    }

    /// <summary>
    /// Called when a DataSource found in the DataSourceIds property is updated.
    /// </summary>
    /// <param name="dataSource">The DataSource being updated.</param>
    public void OnRefresh(DataSource dataSource)
    {
      DataSource = opsDash.OperationsDashboard.Instance.DataSources.FirstOrDefault(ds => ds.Id == DataSourceId);
      if (DataSource == null)
        return;
      else
      {
        //Check if the data source is a standalone data source by trying to find the map widget that provides it
        //If the map widget is not null (i.e. the data source is a standalone datasource), enable the Use Current Map Extent checkbox
        MapWidget = MapWidget.FindMapWidget(DataSource);
        if (MapWidget != null)
          CanUseCurrentExtent = true;
      }

      //Call CalculateStatistics() regardless of the CanUseCurrentExtent setting
      CalculateStatistics();
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

    public class StatisticList
    {
      public StatisticList(string StatisticText, string LeadingText, string ValueText, string TrailingText)
      {
        this.StatisticText = StatisticText;
        this.LeadingText = LeadingText;
        this.ValueText = ValueText;
        this.TrailingText = TrailingText;
      }

      private string statisticText;
      public string StatisticText
      {
        get { return statisticText; }
        set { statisticText = value; }
      }

      private string leadingText;
      public string LeadingText
      {
        get { return leadingText; }
        set { leadingText = value; }
      }

      private string trailingText;
      public string TrailingText
      {
        get { return trailingText; }
        set { trailingText = value; }
      }

      private string valueText;
      public string ValueText
      {
        get { return valueText; }
        set { valueText = value; }
      }
    }

  }
}
