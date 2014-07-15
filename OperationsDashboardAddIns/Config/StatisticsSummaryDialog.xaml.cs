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

using ESRI.ArcGIS.Client.FeatureService;
using ESRI.ArcGIS.OperationsDashboard;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Windows;
using client = ESRI.ArcGIS.Client;

namespace OperationsDashboardAddIns.Config
{
  /// <summary>
  /// Interaction logic for StatisticsSummaryDialog.xaml
  /// </summary>
  public partial class StatisticsSummaryDialog : Window, INotifyPropertyChanged
  {
    #region Backing fields
    private ESRI.ArcGIS.OperationsDashboard.DataSource selectedDataSource;
    private client.Field selectedField;
    private string caption;
    private string leadingText;
    private string trailingText;
    private bool showMax;
    private bool showMin;
    private bool showAvg;
    private bool showSum;
    private bool canOK;
    #endregion

    public List<Statistic> SelectedStatistics { get; set; }

    public ESRI.ArcGIS.OperationsDashboard.DataSource SelectedDataSource
    {
      get { return selectedDataSource; }
      set { SetField(ref selectedDataSource, value, () => SelectedDataSource); }
    }

    public client.Field SelectedField
    {
      get { return selectedField; }
      set { SetField(ref selectedField, value, () => SelectedField); }
    }

    public string Caption
    {
      get { return caption; }
      set { SetField(ref caption, value, () => Caption); }
    }

    public string LeadingText
    {
      get { return leadingText; }
      set { SetField(ref leadingText, value, () => LeadingText); }
    }

    public string TrailingText
    {
      get { return trailingText; }
      set { SetField(ref trailingText, value, () => TrailingText); }
    }

    public bool ShowMax
    {
      get { return showMax; }
      set
      {
        SetField(ref showMax, value, () => ShowMax);
        ValidateInput();
      }
    }

    public bool ShowMin
    {
      get { return showMin; }
      set
      {
        SetField(ref showMin, value, () => ShowMin);
        ValidateInput();
      }
    }

    public bool ShowAvg
    {
      get { return showAvg; }
      set
      {
        SetField(ref showAvg, value, () => ShowAvg);
        ValidateInput();
      }
    }

    public bool ShowSum
    {
      get { return showSum; }
      set
      {
        SetField(ref showSum, value, () => ShowSum);
        ValidateInput();
      }
    }

    public bool CanOK
    {
      get { return canOK; }
      set { SetField(ref canOK, value, () => CanOK); }
    }

    public StatisticsSummaryDialog(
      string initialCaption,
      string initialDataSourceId,
      string initialField,
      List<Statistic> statistics,
      string leadingText,
      string trailingText)
    {
      InitializeComponent();

      // When re-configuring, initialize the widget config dialog from the existing settings.
      txtTitle.Text = initialCaption;
      if (!string.IsNullOrEmpty(initialDataSourceId))
      {
        DataSource dataSource = OperationsDashboard.Instance.DataSources.FirstOrDefault(ds => ds.Id == initialDataSourceId);
        if (dataSource != null)
        {
          DataSourceSelector.SelectedDataSource = dataSource;

          if (!string.IsNullOrEmpty(initialField))
          {
            client.Field field = dataSource.Fields.FirstOrDefault(fld => fld.FieldName == initialField);
            FieldComboBox.SelectedItem = field;
          }
        }
      }

      //Instantized SelectedStatistics or set it with the exsting statistics
      SelectedStatistics = statistics == null ? new List<Statistic>() : statistics;
      ShowMax = SelectedStatistics.Contains(Statistic.Max);
      ShowAvg = SelectedStatistics.Contains(Statistic.Average);
      ShowMin = SelectedStatistics.Contains(Statistic.Min);
      ShowSum = SelectedStatistics.Contains(Statistic.Sum);

      LeadingText = leadingText;
      TrailingText = trailingText;

      DataContext = this;
    }

    //Set properties based on user inputs
    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
      Caption = txtTitle.Text;
      SelectedDataSource = DataSourceSelector.SelectedDataSource;
      SelectedField = (client.Field)FieldComboBox.SelectedItem;

      SelectedStatistics.Clear();
      if (ShowMax)
        SelectedStatistics.Add(Statistic.Max);
      if (ShowAvg)
        SelectedStatistics.Add(Statistic.Average);
      if (ShowMin)
        SelectedStatistics.Add(Statistic.Min);
      if (ShowSum)
        SelectedStatistics.Add(Statistic.Sum);

      LeadingText = txtLeading.Text;
      TrailingText = txtTrailing.Text;

      DialogResult = true;
    }

    //Update the UIs when the selected data source is changed
    private IEnumerable<client.Field> numericFields = null;
    private void DataSourceSelector_SelectionChanged(object sender, EventArgs e)
    {
      DataSource dataSource = DataSourceSelector.SelectedDataSource;
      numericFields = dataSource.Fields.Where(f => IsValidField(f)); //Show only the numeric fields that do not have coded value domains

      ValidateInput();

      FieldComboBox.ItemsSource = numericFields;

      if (numericFields == null || numericFields.Count() == 0)
        return;
      FieldComboBox.SelectedItem = numericFields.First();
    }

    //Determine if a field can be listed in the combobox as a valid field 
    private bool IsValidField(client.Field field)
    {
      if (field.Type == client.Field.FieldType.Double ||
        field.Type == client.Field.FieldType.Integer ||
        field.Type == client.Field.FieldType.Single ||
        field.Type == client.Field.FieldType.SmallInteger)
      {
        if (field.Domain != null && field.Domain.GetType() == typeof(CodedValueDomain))
          return false;

        //The code here doesn't take subtype field into account. 
        //Developers should add the logic to filter out subtype fields

        return true;
      }

      return false;
    }

    //Determine if the OK button can be enabled
    private void ValidateInput()
    {
      if (numericFields == null || numericFields.Count() == 0)
      { CanOK = false; return; }

      if (ShowMax == false && ShowMin == false && ShowAvg == false && ShowSum == false)
      { CanOK = false; return; }
      else
        CanOK = true;
    }

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
