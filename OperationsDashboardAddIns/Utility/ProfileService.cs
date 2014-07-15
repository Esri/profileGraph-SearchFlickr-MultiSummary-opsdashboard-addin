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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace OperationsDashboardAddIns
{
  #region Helper classes
  [DataContract(Name = "JobDescription")]
  public class JobStatus
  {
    [DataMember(Name = "jobId")]
    public string Id { get; set; }

    [DataMember(Name = "jobStatus")]
    public string Status { get; set; }

    [DataMember(Name = "results")]
    public OutputProfile Results { get; set; }
  }

  [DataContract(Name = "OutputProfile")]
  public class OutputProfile
  {
    [DataMember(Name = "dataType")]
    public string Datatype { get; set; }

    [DataMember(Name = "value")]
    public GPFeatureRecordSet FeatureSet { get; set; }
  }

  [DataContract(Name = "value")]
  public class GPFeatureRecordSet
  {
    [DataMember(Name = "displayFieldName")]
    public string DisplayFieldName { get; set; }

    [DataMember(Name = "geometryType")]
    public string GeometryType { get; set; }

    [DataMember(Name = "spatialReference")]
    public SpatialReference SpatialReference { get; set; }

    [DataMember(Name = "fields")]
    public Fields[] Fields { get; set; }

    [DataMember(Name = "features")]
    public Features[] Features { get; set; }

    [DataMember(Name = "exceededTransferLimit")]
    public bool ExceededTransferLimit { get; set; }

    [DataMember(Name = "hasZ")]
    public bool HasZ { get; set; }

    [DataMember(Name = "hasM")]
    public bool HasM { get; set; }
  }

  /// <summary>
  /// Features
  /// </summary>
  [DataContract(Name = "features")]
  public class Features
  {
    [DataMember(Name = "attributes")]
    public Attributes Attributes { get; set; }

    [DataMember(Name = "geometry")]
    public Shape Geometry { get; set; }
  }

  /// <summary>
  /// Attributes
  /// </summary>
  [DataContract(Name = "attributes")]
  public class Attributes
  {
    [DataMember(Name = "OBJECTID")]
    public object OID { get; set; }

    [DataMember(Name = "PourPtID")]
    public int PourPTId { get; set; }

    [DataMember(Name = "Description")]
    public string Description { get; set; }

    [DataMember(Name = "DataResolution")]
    public string DataResolution { get; set; }

    [DataMember(Name = "LengthKm")]
    public double LengthKm { get; set; }

    [DataMember(Name = "Shape_Length")]
    public double ShapeLength { get; set; }
  }

  /// <summary>
  /// Shape
  /// </summary>
  [DataContract(Name = "geometry")]
  public class Shape
  {
    [DataMember(Name = "paths")]
    public List<object[][]> Paths { get; set; }
  } 
  #endregion

  class ProfileService
  {
    Token _token;
    public Token Token
    {
      get { return _token; }
    }

    JobStatus _jobStatus;
    public JobStatus JobStatus
    {
      get { return _jobStatus; }
    }

    //The profile line to be retrieved from the service
    //The line has a set of point features with the x, y, m (distance) and z (elevation) data 
    OutputProfile _outputProfileLine;
    public OutputProfile OutputProfileLine
    {
      get { return _outputProfileLine; }
    }

    //Use the ArcGIS REST API to create a profile line based on the input geometry
    public async Task<Polyline> GetProfileLine(Geometry geometry)
    {
      try
      {
        string requestUrlBase = @"http://elevation.arcgis.com/arcgis/rest/services/Tools/Elevation/GPServer/Profile/";

        //Create the token to use
        TokenService elevationServices = new TokenService();
        _token = await elevationServices.GenerateTokenAsync();

        #region Submit a profile task to be executed asynchronously. A unique job ID will be assigned for the transaction.
        string oidField = "OID";
        string lengthField = "Shape_Length";
        string InputLineFeatures = CreateInputLineFeaturesJson(geometry, oidField, lengthField);
        string additonalParams = "&ProfileIDField=" + oidField + "&DEMResolution=FINEST&MaximumSampleDistance=10&MaximumSampleDistanceUnits=Kilometers&returnZ=true&returnM=true&env%3AoutSR=102100&env%3AprocessSR=102100&f=json";
        string profileServiceUrl = string.Format("{0}submitJob?token={1}&InputLineFeatures={2}{3}", requestUrlBase, _token.AccessToken, InputLineFeatures, additonalParams);

        System.Net.HttpWebRequest webRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(profileServiceUrl);
        webRequest.Timeout = 0xea60;
        System.Net.WebResponse response = await webRequest.GetResponseAsync(); 
        #endregion

        #region Use the jobId to check the status of the job. Keep checking if the jobStatus is not "Succeeded"
        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(JobStatus));
        _jobStatus = (JobStatus)serializer.ReadObject(response.GetResponseStream() as Stream);
        while (_jobStatus.Status.Contains("Executing") || _jobStatus.Status.Contains("esriJobWaiting") || _jobStatus.Status.Contains("Submitted"))
        {
          string statusUrl = string.Format("{0}jobs/{1}?f=pjson&token={2}", requestUrlBase, _jobStatus.Id, _token.AccessToken);
          webRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(statusUrl);
          response = await webRequest.GetResponseAsync();
          _jobStatus = (JobStatus)serializer.ReadObject(response.GetResponseStream());
        } 
        #endregion

        #region The job has successfully completed. Use the jobId to retrieve the result, then use the result to create a profile line 
        if (_jobStatus.Status.Contains("Succeeded"))
        {
          string resultsUrl = string.Format("{0}jobs/{1}/results/OutputProfile?returnZ=true&returnM=true&f=pjson&token={2}", requestUrlBase, _jobStatus.Id, _token.AccessToken);
          webRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(resultsUrl);
          response = await webRequest.GetResponseAsync();

          serializer = new DataContractJsonSerializer(typeof(OutputProfile));

          //Parse the result as the output profile line
          _outputProfileLine = (OutputProfile)serializer.ReadObject(response.GetResponseStream());
          _outputProfileLine.FeatureSet.HasM = true; 
          _outputProfileLine.FeatureSet.HasZ = true;

          //Create a polyline (profile) from the geometry of the output profile line
          Polyline profile = new Polyline();
          foreach (var points in _outputProfileLine.FeatureSet.Features.FirstOrDefault().Geometry.Paths)
          {
            PointCollection collection = new PointCollection();
            foreach (var point in points)
              collection.Add(
                new MapPoint(
                  Convert.ToDouble(point[0]), //[0] is x
                  Convert.ToDouble(point[1]), //[1] is x
                  Convert.ToDouble(point[2]), //[2] is z
                  Convert.ToDouble(point[3]), //[3] is m
                  new ESRI.ArcGIS.Client.Geometry.SpatialReference(102100)));   
            profile.Paths.Add(collection);
          }

          return profile;
        }
        return null;
        #endregion
      }
      catch (Exception)
      {
        return null;
      }
    }

    #region Helper methods - for creating the JSON string to be passed to the profile service
    private string CreateInputLineFeaturesJson(Geometry geometry, string oidField, string lengthField)
    {
      //below is an example geometry JSON string
      string featureGeometryPathObject = geometry.ToJson();
      string toRemove = featureGeometryPathObject.Substring(1, featureGeometryPathObject.IndexOf(',')); //extract the spatial reference property
      featureGeometryPathObject = featureGeometryPathObject.Replace(toRemove, string.Empty);


      string json = string.Empty;
      json += ObjectBegin();

      json += CreatePair("displayFieldName", AddDoubleQuotes(""));

      json += Next();

      json += CreatePair("geometryType", AddDoubleQuotes("esriGeometryPolyline"));

      json += Next();

      json += CreatePair("spatialReference",
          ObjectBegin() +
            CreatePair("wkid", "102100") +
            Next() +
            CreatePair("latestWkid", "3857") +
          ObjectEnd());

      json += Next();

      json += CreatePair("fields",
        ArrayBegin() +
          ObjectBegin() +
            CreatePair("name", AddDoubleQuotes(oidField)) +
            Next() +
            CreatePair("type", AddDoubleQuotes("esriFieldTypeOID")) +
          ObjectEnd() +
          Next() +
          ObjectBegin() +
            CreatePair("name", AddDoubleQuotes(lengthField)) +
            Next() +
            CreatePair("type", AddDoubleQuotes("esriFieldTypeDouble")) +
          ObjectEnd() +
        ArrayEnd());

      json += Next();

      json += CreatePair("features",
        ArrayBegin() +
          ObjectBegin() +
            CreatePair("geometry", featureGeometryPathObject) +
          ObjectEnd() +
        ArrayEnd());

      json += Next();

      json += CreatePair("exceededTransferLimit", "false");

      json += ObjectEnd();

      return json;
    }

    private string AddDoubleQuotes(string String)
    {
      return string.Format("\"{0}\"", String);
    }

    private string CreatePair(string String, string Value)
    {
      return AddDoubleQuotes(String) + ":" + Value;
    }

    private string Next()
    {
      return ",";
    }

    private string ArrayBegin()
    {
      return "[";
    }

    private string ArrayEnd()
    {
      return "]";
    }

    private string ObjectBegin()
    {
      return "{";
    }

    private string ObjectEnd()
    {
      return "}";
    } 
    #endregion
  }
}
