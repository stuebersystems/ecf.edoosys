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

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ecf.Edoosys
{
    public class EcfExportOptions
    {
        public string DatabaseConnection { get; set; }
        public ICollection<EcfExportFile> Files { get; set; } = new List<EcfExportFile>();
        public bool NoSchoolClassGroups { get; set; } = true;
        public string SchoolNo { get; set; }
        public string SchoolYearCode { get; set; }
        public string Separator { get; set; }
        public string SourceFileName { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EcfSourceProvider SourceProvider { get; set; } = EcfSourceProvider.Postgres;
        public string TargetFolderName { get; set; }
    }
}
