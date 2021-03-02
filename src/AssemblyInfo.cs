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

using System.Reflection;

namespace Ecf.Edoosys
{
    /// <summary>
    /// Helper class to extract assambly infos
    /// </summary>
    public static class AssemblyInfo
    {
        public static string GetCopyright()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), true);
            if (attributes.Length > 0)
            {
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
            else
            {
                return null;
            }
        }

        public static string GetTitle()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
            if (attributes.Length > 0)
            {
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
            else
            {
                return null;
            }
        }
    }
}
