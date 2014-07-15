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

using ESRI.ArcGIS.Client.Symbols;
using System;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace OperationsDashboardAddIns
{
  #region Used by Map Tool
  //A class that creates a simple point symbol 
  class FlickrPushpinSymbol
  {
    public static PictureMarkerSymbol CreatePushpinSymbol()
    {
      PictureMarkerSymbol pms = new PictureMarkerSymbol()
      {
        Source = new BitmapImage(new Uri(@"pack://application:,,,/OperationsDashboardAddIns;component/Images/FlickrPin64.png")),
        OffsetX = 35,
        OffsetY = 60,
      };
      return pms;
    }
  }  
  #endregion

  #region Used by Widget
  //A class that creates a simple polyline symbol
  class SimplePolylineSymbol
  {
    public static CartographicLineSymbol CreateLineSymbol()
    {
      Brush b = new SolidColorBrush(Color.FromRgb(0, 150, 255)) { Opacity = 100 };
      CartographicLineSymbol _symbol = new CartographicLineSymbol()
      {
        Color = b,
        Width = 4,
      };
      return _symbol;
    }
  }

  //A class that creates a simple point symbol
  class SimplePointSymbol
  {
    public static SimpleMarkerSymbol CreatePointSymbol()
    {
      Color symbolColor = (Color)ColorConverter.ConvertFromString("#FF9900");
      SimpleMarkerSymbol _symbol = new SimpleMarkerSymbol()
      {
        Color = new SolidColorBrush(symbolColor),
        Style = SimpleMarkerSymbol.SimpleMarkerStyle.Circle,
        Size = 13
      };
      return _symbol;
    }
  } 
  #endregion
}

