#region ENBREA - Copyright (C) 2021 STÜBER SYSTEMS GmbH
/*    
 *    ENBREA 
 *    
 *    Copyright (C) 2021 STÜBER SYSTEMS GmbH
 *
 *    This program is free software: you can redistribute it and/or modify
 *    it under the terms of the GNU Affero General Public License, version 3,
 *    as published by the Free Software Foundation.
 *
 *    This program is distributed in the hope that it will be useful,
 *    but WITHOUT ANY WARRANTY; without even the implied warranty of
 *    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *    GNU Affero General Public License for more details.
 *
 *    You should have received a copy of the GNU Affero General Public License
 *    along with this program. If not, see <http://www.gnu.org/licenses/>.
 *
 */
#endregion

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Ecf.Edoosys
{
    public static class ConfigurationManager
    {
        public const string EmbeddedNodeName = "Edoosys";

        public static async Task<Configuration> LoadFromFile(string fileName, CancellationToken cancellationToken = default)
        {
            if (File.Exists(fileName))
            {
                using var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

                var jsonDoc = await JsonDocument.ParseAsync(fileStream, default, cancellationToken);

                if (jsonDoc.RootElement.TryGetProperty(EmbeddedNodeName, out var jsonElement))
                {
                    return JsonSerializer.Deserialize<Configuration>(jsonElement.GetRawText());
                }
                else
                {
                    throw new InvalidOperationException($"No configuration for \"{EmbeddedNodeName}\" found in file \"{fileName}\").");
                }
            }
            else
            {
                throw new FileNotFoundException($"File \"{fileName}\") does not exists.");
            }
        }
    }
}