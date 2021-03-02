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
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ecf.Edoosys
{
    public class CsvExportManager : CustomManager
    {
        private int _recordCounter = 0;
        private int _tableCounter = 0;

        public CsvExportManager(
            Configuration config,
            CancellationToken cancellationToken = default,
            EventWaitHandle cancellationEvent = default)
            : base(config, cancellationToken, cancellationEvent)
        {
        }

        public async override Task Execute()
        {
            try
            {
                // Init counters
                _tableCounter = 0;
                _recordCounter = 0;

                // Report status
                Console.WriteLine();
                Console.WriteLine("[Extracting] Start...");

                // Preperation
                PrepareExportFolder();

                // Education
                await Execute(EcfTables.Teachers, async (r, w, h) => await ExportTeachers(r, w, h));
                await Execute(EcfTables.Subjects, async (r, w, h) => await ExportSubjects(r, w, h));
                await Execute(EcfTables.SchoolClasses, async (r, w, h) => await ExportSchoolClasses(r, w, h));
                await Execute(EcfTables.Students, async (r, w, h) => await ExportStudents(r, w, h));
                await Execute(EcfTables.StudentSchoolClassAttendances, async (r, w, h) => await ExportStudentSchoolClassAttendances(r, w, h));
                await Execute(EcfTables.StudentSubjects, async (r, w, h) => await ExportStudentSubjects(r, w, h));

                // Report status
                Console.WriteLine($"[Extracting] {_tableCounter} table(s) and {_recordCounter} record(s) extracted");
            }
            catch 
            {
                // Report error 
                Console.WriteLine();
                Console.WriteLine($"[Error] Extracting failed. Only {_tableCounter} table(s) and {_recordCounter} record(s) extracted");
                throw;
            }
        }

        private async Task Execute(string ecfTableName, Func<CsvTableReader, EcfTableWriter, string[], Task<int>> action)
        {
            if (ShouldExportTable(ecfTableName, out var ecfFile))
            {
                // Report status
                Console.WriteLine($"[Extracting] [{ecfTableName}] Start...");

                // Open Edoosys file for import
                using var csvReaderStream = new FileStream(_config.EcfExport.SourceFileName, FileMode.Open, FileAccess.Read, FileShare.None);

                // Create CSV Reader for Edoosys file
                using var csvReader = new CsvReader(csvReaderStream, Encoding.UTF8, true);

                // The Edoosys file is semicolon separeted
                csvReader.Configuration.Separator = ';';

                // Create CSV Table Reader for Edoosys file
                var csvTableReader = new CsvTableReader(csvReader, new CsvConverterResolver());

                // Generate ECF file name
                var ecfFileName = Path.ChangeExtension(Path.Combine(_config.EcfExport.TargetFolderName, ecfTableName), "csv");

                // Create ECF file for export
                using var ecfWriterStream = new FileStream(ecfFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

                // Create ECF Writer
                using var ecfWriter = new CsvWriter(ecfWriterStream, Encoding.UTF8);

                // Call table specific action
                var ecfRecordCounter = await action(csvTableReader, new EcfTableWriter(ecfWriter), ecfFile?.Headers);

                // Inc counters
                _recordCounter += ecfRecordCounter;
                _tableCounter++;

                // Report status
                Console.WriteLine($"[Extracting] [{ecfTableName}] {ecfRecordCounter} record(s) extracted");
            }
        }

        private async Task<int> ExportSchoolClasses(CsvTableReader csvTableReader, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfCache = new HashSet<string>();
            var ecfRecordCounter = 0;

            await csvTableReader.ReadHeadersAsync();

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.Code);
            }

            while (await csvTableReader.ReadAsync() > 0)
            {
                var schoolClass = new CsvExportSchoolClass(csvTableReader);

                if (!string.IsNullOrEmpty(schoolClass.Id) && !ecfCache.Contains(schoolClass.Id))
                {
                    ecfTableWriter.TrySetValue(EcfHeaders.Id, schoolClass.Id);
                    ecfTableWriter.TrySetValue(EcfHeaders.Code, schoolClass.Code);

                    await ecfTableWriter.WriteAsync();

                    ecfCache.Add(schoolClass.Id);
                    ecfRecordCounter++;
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudents(CsvTableReader csvTableReader, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfCache = new HashSet<string>();
            var ecfRecordCounter = 0;

            await csvTableReader.ReadHeadersAsync();

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.LastName,
                    EcfHeaders.FirstName,
                    EcfHeaders.Gender,
                    EcfHeaders.Birthdate);
            }

            while (await csvTableReader.ReadAsync() > 0)
            {
                var student = new CsvExportStudent(csvTableReader);

                if (!ecfCache.Contains(student.Id))
                {
                    ecfTableWriter.TrySetValue(EcfHeaders.Id, student.Id);
                    ecfTableWriter.TrySetValue(EcfHeaders.LastName, student.LastName);
                    ecfTableWriter.TrySetValue(EcfHeaders.FirstName, student.FirstName);
                    ecfTableWriter.TrySetValue(EcfHeaders.Gender, student.Gender);
                    ecfTableWriter.TrySetValue(EcfHeaders.Birthdate, student.BirthDate);

                    await ecfTableWriter.WriteAsync();

                    ecfCache.Add(student.Id);
                    ecfRecordCounter++;
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudentSchoolClassAttendances(CsvTableReader csvTableReader, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfRecordCounter = 0;

            await csvTableReader.ReadHeadersAsync();

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.StudentId,
                    EcfHeaders.SchoolClassId);
            }

            while (await csvTableReader.ReadAsync() > 0)
            {
                var student = new CsvExportStudent(csvTableReader);
                var schoolClass = new CsvExportSchoolClass(csvTableReader);

                if (!string.IsNullOrEmpty(schoolClass.Id))
                {
                    ecfTableWriter.TrySetValue(EcfHeaders.StudentId, student.Id);
                    ecfTableWriter.TrySetValue(EcfHeaders.SchoolClassId, schoolClass.Id);

                    await ecfTableWriter.WriteAsync();

                    ecfRecordCounter++;
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportStudentSubjects(CsvTableReader csvTableReader, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfRecordCounter = 0;

            await csvTableReader.ReadHeadersAsync();

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.StudentId,
                    EcfHeaders.SchoolClassId,
                    EcfHeaders.SubjectId);
            }

            while (await csvTableReader.ReadAsync() > 0)
            {
                var student = new CsvExportStudent(csvTableReader);
                var schoolClass = new CsvExportSchoolClass(csvTableReader);

                if (csvTableReader.TryGetValue("Alle Lehrkräfte (Kürzel) mit Fach", out var value))
                {
                    var csvLineParser = new CsvLineParser(',');

                    var subValues = csvLineParser.Read(value);

                    csvLineParser.Configuration.Separator = ' ';

                    foreach (var subValue in subValues)
                    {
                        if (!string.IsNullOrEmpty(subValue))
                        {
                            var subValueParts = csvLineParser.Read(subValue.Trim());
                            if (subValueParts.Length == 2)
                            {
                                var subjectCode = subValueParts[1];

                                if (!string.IsNullOrEmpty(subjectCode))
                                {
                                    ecfTableWriter.TrySetValue(EcfHeaders.StudentId, student.Id);
                                    ecfTableWriter.TrySetValue(EcfHeaders.SchoolClassId, schoolClass.Id);
                                    ecfTableWriter.TrySetValue(EcfHeaders.SubjectId, subjectCode);

                                    await ecfTableWriter.WriteAsync();

                                    ecfRecordCounter++;
                                }
                            }
                        }
                    }
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportSubjects(CsvTableReader csvTableReader, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfCache = new HashSet<string>();
            var ecfRecordCounter = 0;

            await csvTableReader.ReadHeadersAsync();

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.Code);
            }

            while (await csvTableReader.ReadAsync() > 0)
            {
                if (csvTableReader.TryGetValue("Alle Lehrkräfte (Kürzel) mit Fach", out var value))
                {
                    var csvLineParser = new CsvLineParser(',');

                    var subValues = csvLineParser.Read(value);

                    csvLineParser.Configuration.Separator = ' ';

                    foreach (var subValue in subValues)
                    {
                        if (!string.IsNullOrEmpty(subValue))
                        {
                            var subValueParts = csvLineParser.Read(subValue.Trim());
                            if (subValueParts.Length == 2)
                            {
                                var subjectCode = subValueParts[1];

                                if (!string.IsNullOrEmpty(subjectCode) && !ecfCache.Contains(subjectCode))
                                {
                                    ecfTableWriter.TrySetValue(EcfHeaders.Id, subjectCode);
                                    ecfTableWriter.TrySetValue(EcfHeaders.Code, subjectCode);

                                    await ecfTableWriter.WriteAsync();

                                    ecfCache.Add(subjectCode);
                                    ecfRecordCounter++;
                                }
                            }
                        }
                    }
                }
            }

            return ecfRecordCounter;
        }

        private async Task<int> ExportTeachers(CsvTableReader csvTableReader, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            var ecfCache = new HashSet<string>();
            var ecfRecordCounter = 0;

            await csvTableReader.ReadHeadersAsync();

            if (ecfHeaders != null && ecfHeaders.Length > 0)
            {
                await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
            }
            else
            {
                await ecfTableWriter.WriteHeadersAsync(
                    EcfHeaders.Id,
                    EcfHeaders.Code);
            }

            while (await csvTableReader.ReadAsync() > 0)
            {
                if (csvTableReader.TryGetValue("Alle Lehrkräfte (Kürzel) mit Fach", out var value))
                {
                    var csvLineParser = new CsvLineParser(',');

                    var subValues = csvLineParser.Read(value);

                    csvLineParser.Configuration.Separator = ' ';

                    foreach (var subValue in subValues)
                    {
                        if (!string.IsNullOrEmpty(subValue))
                        {
                            var subValueParts = csvLineParser.Read(subValue.Trim());
                            if (subValueParts.Length == 2)
                            {
                                var teacherCode = subValueParts[0];

                                if (!string.IsNullOrEmpty(teacherCode) && !ecfCache.Contains(teacherCode))
                                {
                                    ecfTableWriter.TrySetValue(EcfHeaders.Id, teacherCode);
                                    ecfTableWriter.TrySetValue(EcfHeaders.Code, teacherCode);

                                    await ecfTableWriter.WriteAsync();

                                    ecfCache.Add(teacherCode);
                                    ecfRecordCounter++;
                                }
                            }
                        }
                    }
                }
            }

            return ecfRecordCounter;
        }
    }
}
