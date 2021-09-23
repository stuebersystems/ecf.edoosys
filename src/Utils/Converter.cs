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

using Enbrea.Ecf;
using Enbrea.Edoosys.Db;
using System;

namespace Ecf.Edoosys
{
    /// <summary>
    /// Data type converter
    /// </summary>
    public static class Converter
    {
        public static Date? GetDate(DateTime? value)
        {
            if (value != null)
            {
                return new Date((DateTime)value);
            }
            return null;
        }

        public static EcfGender? GetGender(Gender? value)
        {
            if (value != null)
            {
                return (value) switch
                {
                    Gender.Male => EcfGender.Male,
                    Gender.Female => EcfGender.Female,
                    Gender.Diverse => EcfGender.Diverse,
                    _ => null,
                };
            }
            return null;
        }
    }
}