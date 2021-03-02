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
    /// Implementation of a Date converter from from Edoosys CSV
    /// </summary>
    public class CsvDateConverter : CsvDefaultConverter
    {
        private readonly string[] _formats =
        {
            "dd.MM.yyyy"
        };

        public CsvDateConverter() : 
            base(typeof(Date), CultureInfo.InvariantCulture)
        {
        }

        public override object FromString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            else
            {
                return Date.ParseExact(value, _formats, CultureInfo, DateTimeStyles.None);
            }
        }
    }
}