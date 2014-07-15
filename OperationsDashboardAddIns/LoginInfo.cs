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
using System.Threading.Tasks;

namespace OperationsDashboardAddIns
{
  public class LoginInfo
  {
    #region Login info for the profile graph addin
    public static readonly string client_id = "<YourClientId>";
    public static readonly string client_secret = "<YourClientSecret>"; 
    #endregion

    #region Login info for Flickr manager
    public static readonly string ApiKey = "<YourApiKey>";
    public static readonly string SharedSecret = "<YourSharedSecret>";
    #endregion
  }
}
