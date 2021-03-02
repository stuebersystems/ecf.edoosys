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
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Ecf.Edoosys.Xunit
{
    /// <summary>
    /// Integration tests for <see cref="CsvExportManager"/>.
    /// </summary>
    public class IntegrationTest
    {
        [Fact]
        public async Task TestEcfExport()
        {
            var ecfFolder = new DirectoryInfo(Path.Combine(GetOutputFolder(), "EcfExport"));
            if (!ecfFolder.Exists)
            {
                ecfFolder.Create();
            };

            var csvFile = Path.Combine(GetOutputFolder(), "Assets", "test.csv");
            var cfgFile = Path.Combine(GetOutputFolder(), "Assets", "test.config.json");

            var csvConfig = await ConfigurationManager.LoadFromFile(cfgFile);

            csvConfig.EcfExport.TargetFolderName = ecfFolder.FullName;
            csvConfig.EcfExport.SourceFileName = csvFile;

            var exportManager = new CsvExportManager(csvConfig);

            await exportManager.Execute();

            await ValidateSchooClassesFile(ecfFolder);
            await ValidateStudentsFile(ecfFolder);
        }

        private async Task ValidateSchooClassesFile(DirectoryInfo ecfFolder)
        {

            using var csvReader = new CsvReader(Path.Combine(ecfFolder.FullName, EcfTables.SchoolClasses + ".csv"), true);

            var ecfTableReader = new EcfTableReader(csvReader);

            await ecfTableReader.ReadHeadersAsync();

            Assert.Equal(2, ecfTableReader.Headers.Count);
            Assert.Equal(EcfHeaders.Id, ecfTableReader.Headers[0]);
            Assert.Equal(EcfHeaders.Code, ecfTableReader.Headers[1]);
        }

        private async Task ValidateStudentsFile(DirectoryInfo ecfFolder)
        {

            using var csvReader = new CsvReader(Path.Combine(ecfFolder.FullName, EcfTables.Students + ".csv"), true);

            var ecfTableReader = new EcfTableReader(csvReader);

            await ecfTableReader.ReadHeadersAsync();

            Assert.Equal(5, ecfTableReader.Headers.Count);
            Assert.Equal(EcfHeaders.Id, ecfTableReader.Headers[0]);
            Assert.Equal(EcfHeaders.LastName, ecfTableReader.Headers[1]);
            Assert.Equal(EcfHeaders.FirstName, ecfTableReader.Headers[2]);
            Assert.Equal(EcfHeaders.Gender, ecfTableReader.Headers[3]);
            Assert.Equal(EcfHeaders.Birthdate, ecfTableReader.Headers[4]);

            await ecfTableReader.ReadAsync();
            Assert.Equal("Duck", ecfTableReader.GetValue<string>(EcfHeaders.LastName));
            Assert.Equal("Tick", ecfTableReader.GetValue<string>(EcfHeaders.FirstName));
            Assert.Equal(EcfGender.Female, ecfTableReader.GetValue<EcfGender>(EcfHeaders.Gender));
            Assert.Equal(new Date(2001, 1, 1), ecfTableReader.GetValue<Date>(EcfHeaders.Birthdate));
        }

        private string GetOutputFolder()
        {
            // Get the full location of the assembly
            string assemblyPath = System.Reflection.Assembly.GetAssembly(typeof(IntegrationTest)).Location;

            // Get the folder that's in
            return Path.GetDirectoryName(assemblyPath);
        }
    }
}
