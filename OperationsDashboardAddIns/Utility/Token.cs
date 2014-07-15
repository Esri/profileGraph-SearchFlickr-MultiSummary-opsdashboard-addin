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

using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace OperationsDashboardAddIns
{
  /// <summary>
  /// Token 
  /// </summary>
  [DataContract(Name = "Token")]
  public class Token
  {
    [DataMember(Name = "access_token")]
    public string AccessToken { get; set; }
  }

  /// <summary>
  /// Spatial Reference
  /// </summary>
  [DataContract(Name = "spatialReference")]
  public class SpatialReference
  {
    [DataMember(Name = "wkid")]
    public string WKID { get; set; }

    [DataMember(Name = "latestWkid")]
    public string LatestWkid { get; set; }
  }

  /// <summary>
  /// Fields
  /// </summary>
  [DataContract(Name = "fields")]
  public class Fields
  {
    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "type")]
    public string Datatype { get; set; }

    [DataMember(Name = "alias")]
    public string Alias { get; set; }
  }

  /// <summary>
  /// Elevation Services base class.
  /// </summary>
  internal class TokenService
  {
    Token _token = null;

    internal async Task<Token> GenerateTokenAsync()
    {
      //A token is needed to generate a census report. To do so, you need to pass in your client_id and client_secret.
      //For the instructions to obtain the token, read http://resources.arcgis.com/en/help/arcgis-rest-api/index.html#/Accessing_services_provided_by_Esri/02r300000268000000/
      string tokenUrl = string.Format(@"https://www.arcgis.com/sharing/oauth2/token?client_id={0}&grant_type=client_credentials&client_secret={1}&f=pjson",
        LoginInfo.client_id,
        LoginInfo.client_secret);

      HttpWebRequest webRequest = (HttpWebRequest)System.Net.WebRequest.Create(tokenUrl);
      webRequest.Timeout = 0xea60;
      System.Net.WebResponse response = await webRequest.GetResponseAsync();

      try
      {
        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Token));
        _token = (Token)serializer.ReadObject(response.GetResponseStream());
        return _token;
      }
      catch
      {
        return null;
      }
    }
  }
    
}
