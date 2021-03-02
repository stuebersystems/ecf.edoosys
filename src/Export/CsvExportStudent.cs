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

using Enbrea.Csv;
using Enbrea.Ecf;
using System.Globalization;

namespace Ecf.Edoosys
{
    public class CsvExportStudent
    {
        public readonly Date? BirthDate = null;
        public readonly string FirstName = null;
        public readonly EcfGender? Gender = null;
        public readonly string Id;
        public readonly string LastName = null;

        public CsvExportStudent(CsvTableReader csvTableReader)
        {
            csvTableReader.TryGetValue("Schüler_Stamm_ID", out Id);
            csvTableReader.TryGetValue("Vornamen", out FirstName);
            csvTableReader.TryGetValue("Familienname", out LastName);
            csvTableReader.TryGetValue("Geburtsdatum", out BirthDate);
            csvTableReader.TryGetValue("Geschlecht (männlich/weiblich)", out Gender);
        }
    }
}
