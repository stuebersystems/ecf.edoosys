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
    /// <summary>
    /// Implementation of a Gender converter from Edoosys CSV
    /// </summary>
    public class CsvGenderConverter : CsvDefaultEnumConverter
    {
        public CsvGenderConverter() : 
            base(typeof(EcfGender), CultureInfo.InvariantCulture, null, true)
        {
        }

        public override object FromString(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return value switch
                {
                    "weiblich" => EcfGender.Female,
                    "männlich" => EcfGender.Male,
                    _ => null,
                };
            }
            else
            {
                return null;
            }
        }
    }
}
