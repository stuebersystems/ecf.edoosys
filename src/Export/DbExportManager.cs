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
using Enbrea.Edoosys.Db;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ecf.Edoosys
{
    public class DbExportManager : CustomManager
    {
        private int _recordCounter = 0;
        private int _tableCounter = 0;
        private HashSet<string> _ecfTeacherCache = new HashSet<string>();
        private HashSet<string> _ecfSchoolClassesCache = new HashSet<string>();

        public DbExportManager(
            Configuration config,
            CancellationToken cancellationToken = default,
            EventWaitHandle cancellationEvent = default)
            : base(config, cancellationToken, cancellationEvent)
        {
        }

        public async override Task Execute()
        {
            var edoosysDbReader = new EdoosysDbReader(_config.EcfExport.DatabaseConnection);
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
                await Execute(EcfTables.Subjects, edoosysDbReader, async (r, w, h) => await ExportSubjects(r, w, h));
                await Execute(EcfTables.Students, edoosysDbReader, async (r, w, h) => await ExportStudents(r, w, h));
                await Execute(EcfTables.StudentSchoolClassAttendances, edoosysDbReader, async (r, w, h) => await ExportStudentSchoolClassAttendances(r, w, h));
                await Execute(EcfTables.StudentSubjects, edoosysDbReader, async (r, w, h) => await ExportStudentSubjects(r, w, h));
                await Execute(EcfTables.SchoolClasses, edoosysDbReader, async (r, w, h) => await ExportSchoolClasses(r, w, h));
                await Execute(EcfTables.Teachers, edoosysDbReader, async (r, w, h) => await ExportTeachers(r, w, h));

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

        private async Task Execute(string ecfTableName, EdoosysDbReader edoosysDbReader, Func<EdoosysDbReader, EcfTableWriter, string[], Task<int>> action)
        {
            if (ShouldExportTable(ecfTableName, out var ecfFile))
            {
                // Report status
                Console.WriteLine($"[Extracting] [{ecfTableName}] Start...");

                // Generate ECF file name
                var ecfFileName = Path.ChangeExtension(Path.Combine(_config.EcfExport.TargetFolderName, ecfTableName), "csv");

                // Create ECF file for export
                using var ecfWriterStream = new FileStream(ecfFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

                // Create ECF Writer
                using var ecfWriter = new CsvWriter(ecfWriterStream, Encoding.UTF8);

                // Call table specific action
                var ecfRecordCounter = await action(edoosysDbReader, new EcfTableWriter(ecfWriter), ecfFile?.Headers);

                // Inc counters
                _recordCounter += ecfRecordCounter;
                _tableCounter++;

                // Report status
                Console.WriteLine($"[Extracting] [{ecfTableName}] {ecfRecordCounter} record(s) extracted");
            }
        }

        private async Task<int> ExportSchoolClasses(EdoosysDbReader edoosysDbReader, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            if ((_config.EcfExport?.SchoolNo != null) && (_config.EcfExport?.SchoolYearCode != null))
            {

                var ecfCache = new HashSet<string>();
                var ecfRecordCounter = 0;

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

                await foreach (var schoolClass in edoosysDbReader.SchoolClassesAsync(_config.EcfExport.SchoolNo, _config.EcfExport.SchoolYearCode))
                {
                    if (_config.EcfExport.NoSchoolClassGroups)
                    {
                        if (!ecfCache.Contains(schoolClass.RootId))
                        {
                            if (_ecfSchoolClassesCache.Contains(schoolClass.RootId))
                            {
                                ecfTableWriter.TrySetValue(EcfHeaders.Id, schoolClass.RootId);
                                ecfTableWriter.TrySetValue(EcfHeaders.Code, schoolClass.RootCode);
                                ecfTableWriter.TrySetValue(EcfHeaders.Name, schoolClass.RootName);

                                await ecfTableWriter.WriteAsync();

                                ecfCache.Add(schoolClass.RootId);
                                ecfRecordCounter++;
                            }
                        }
                    }
                    else
                    {
                        if (_ecfSchoolClassesCache.Contains(schoolClass.Id))
                        {
                            ecfTableWriter.TrySetValue(EcfHeaders.Id, schoolClass.Id);
                            ecfTableWriter.TrySetValue(EcfHeaders.Code, $"{schoolClass.RootCode}_{schoolClass.Code}");
                            ecfTableWriter.TrySetValue(EcfHeaders.Name, $"{schoolClass.RootCode}_{schoolClass.Code}");

                            await ecfTableWriter.WriteAsync();

                            ecfRecordCounter++;
                        }
                    }
                }

                return ecfRecordCounter;
            }
            else
            {
                throw new Exception("No school no and/or no school year for edoo.sys database defined");
            }
        }

        private async Task<int> ExportStudents(EdoosysDbReader edoosysDbReader, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            if ((_config.EcfExport?.SchoolNo != null) && (_config.EcfExport?.SchoolYearCode != null))
            {
                var ecfRecordCounter = 0;

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

                await foreach (var student in edoosysDbReader.StudentsAsync(_config.EcfExport.SchoolNo, _config.EcfExport.SchoolYearCode, activeStudentsOnly: true))
                {
                    ecfTableWriter.TrySetValue(EcfHeaders.Id, student.Id);
                    ecfTableWriter.TrySetValue(EcfHeaders.LastName, student.Lastname);
                    ecfTableWriter.TrySetValue(EcfHeaders.FirstName, student.Firstname);
                    ecfTableWriter.TrySetValue(EcfHeaders.Gender, Converter.GetGender(student.Gender));
                    ecfTableWriter.TrySetValue(EcfHeaders.Birthdate, Converter.GetDate(student.Birthdate));

                    await ecfTableWriter.WriteAsync();

                    ecfRecordCounter++;
                }

                return ecfRecordCounter;
            }
            else
            {
                throw new Exception("No school no and/or no school year for edoo.sys database defined");
            }
        }

        private async Task<int> ExportStudentSchoolClassAttendances(EdoosysDbReader edoosysDbReader, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            if ((_config.EcfExport?.SchoolNo != null) && (_config.EcfExport?.SchoolYearCode != null))
            {
                var ecfRecordCounter = 0;

                if (ecfHeaders != null && ecfHeaders.Length > 0)
                {
                    await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
                }
                else
                {
                    await ecfTableWriter.WriteHeadersAsync(
                        EcfHeaders.Id,
                        EcfHeaders.StudentId,
                        EcfHeaders.SchoolClassId);
                }

                await foreach (var attendance in edoosysDbReader.StudentSchoolClassAttendancesAsync(_config.EcfExport.SchoolNo, _config.EcfExport.SchoolYearCode, activeStudentsOnly: true))
                {
                    var schoolClassId = _config.EcfExport.NoSchoolClassGroups ? attendance.SchoolClassRootId : attendance.SchoolClassId;

                    ecfTableWriter.TrySetValue(EcfHeaders.Id, GenerateKey(attendance.StudentId, schoolClassId));
                    ecfTableWriter.TrySetValue(EcfHeaders.StudentId, attendance.StudentId);
                    ecfTableWriter.TrySetValue(EcfHeaders.SchoolClassId, schoolClassId);
                    await ecfTableWriter.WriteAsync();

                    _ecfSchoolClassesCache.Add(schoolClassId);
                    ecfRecordCounter++;
                }

                return ecfRecordCounter;
            }
            else
            {
                throw new Exception("No school no and/or no school year for edoo.sys database defined");
            }
        }

        private async Task<int> ExportStudentSubjects(EdoosysDbReader edoosysDbReader, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            if ((_config.EcfExport?.SchoolNo != null) && (_config.EcfExport?.SchoolYearCode != null))
            {
                var ecfRecordCounter = 0;

                if (ecfHeaders != null && ecfHeaders.Length > 0)
                {
                    await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
                }
                else
                {
                    await ecfTableWriter.WriteHeadersAsync(
                        EcfHeaders.Id,
                        EcfHeaders.StudentId,
                        EcfHeaders.SchoolClassId,
                        EcfHeaders.SubjectId,
                        EcfHeaders.TeacherId);
                }

                await foreach (var studentSubject in edoosysDbReader.StudentSubjectsAsync(_config.EcfExport.SchoolNo, _config.EcfExport.SchoolYearCode, activeStudentsOnly: true))
                {
                    var schoolClassId = _config.EcfExport.NoSchoolClassGroups ? studentSubject.SchoolClassRootId : studentSubject.SchoolClassId;

                    ecfTableWriter.TrySetValue(EcfHeaders.Id, GenerateKey(studentSubject.StudentId, studentSubject.SubjectId, schoolClassId, studentSubject.TeacherId));
                    ecfTableWriter.TrySetValue(EcfHeaders.StudentId, studentSubject.StudentId);
                    ecfTableWriter.TrySetValue(EcfHeaders.SubjectId, studentSubject.SubjectId);
                    ecfTableWriter.TrySetValue(EcfHeaders.SchoolClassId, schoolClassId);
                    ecfTableWriter.TrySetValue(EcfHeaders.TeacherId, studentSubject.TeacherId);

                    await ecfTableWriter.WriteAsync();

                    _ecfTeacherCache.Add(studentSubject.TeacherId);
                    ecfRecordCounter++;
                }

                return ecfRecordCounter;
            }
            else
            {
                throw new Exception("No school no and/or no school year for edoo.sys database defined");
            }
        }

        private async Task<int> ExportSubjects(EdoosysDbReader edoosysDbReader, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            if ((_config.EcfExport?.SchoolNo != null) && (_config.EcfExport?.SchoolYearCode != null))
            {
                var ecfRecordCounter = 0;

                if (ecfHeaders != null && ecfHeaders.Length > 0)
                {
                    await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
                }
                else
                {
                    await ecfTableWriter.WriteHeadersAsync(
                        EcfHeaders.Id,
                        EcfHeaders.Code,
                        EcfHeaders.Name);
                }

                await foreach (var subject in edoosysDbReader.SubjectsAsync(_config.EcfExport.SchoolNo, _config.EcfExport.SchoolYearCode))
                {
                    ecfTableWriter.TrySetValue(EcfHeaders.Id, subject.Id);
                    ecfTableWriter.TrySetValue(EcfHeaders.Code, subject.Code);
                    ecfTableWriter.TrySetValue(EcfHeaders.Name, subject.Name);

                    await ecfTableWriter.WriteAsync();

                    ecfRecordCounter++;
                }

                return ecfRecordCounter;
            }
            else
            {
                throw new Exception("No school no and/or no school year for edoo.sys database defined");
            }
        }

        private async Task<int> ExportTeachers(EdoosysDbReader edoosysDbReader, EcfTableWriter ecfTableWriter, string[] ecfHeaders)
        {
            if ((_config.EcfExport?.SchoolNo != null) && (_config.EcfExport?.SchoolYearCode != null))
            {
                var ecfRecordCounter = 0;

                if (ecfHeaders != null && ecfHeaders.Length > 0)
                {
                    await ecfTableWriter.WriteHeadersAsync(ecfHeaders);
                }
                else
                {
                    await ecfTableWriter.WriteHeadersAsync(
                        EcfHeaders.Id,
                        EcfHeaders.Code,
                        EcfHeaders.LastName,
                        EcfHeaders.FirstName,
                        EcfHeaders.Gender,
                        EcfHeaders.Birthdate);
                }

                await foreach (var teacher in edoosysDbReader.TeachersAsync(_config.EcfExport.SchoolNo, _config.EcfExport.SchoolYearCode))
                {
                    if (_ecfTeacherCache.Contains(teacher.Id))
                    { 
                        ecfTableWriter.TrySetValue(EcfHeaders.Id, teacher.Id);
                        ecfTableWriter.TrySetValue(EcfHeaders.Code, teacher.Code);
                        ecfTableWriter.TrySetValue(EcfHeaders.LastName, teacher.Lastname);
                        ecfTableWriter.TrySetValue(EcfHeaders.FirstName, teacher.Firstname);
                        ecfTableWriter.TrySetValue(EcfHeaders.Gender, Converter.GetGender(teacher.Gender));
                        ecfTableWriter.TrySetValue(EcfHeaders.Birthdate, Converter.GetDate(teacher.Birthdate));

                        await ecfTableWriter.WriteAsync();

                        ecfRecordCounter++;
                    }
                }

                return ecfRecordCounter;
            }
            else
            {
                throw new Exception("No school no and/or no school year for edoo.sys database defined");
            }
        }

        private Guid GenerateKey(params string[] array)
        {
            var csvLineBuilder = new CsvLineBuilder();

            foreach (var arrayItem in array)
            {
                csvLineBuilder.Append(arrayItem);
            }
            return IdFactory.CreateIdFromValue(csvLineBuilder.ToString());
        }

    }
}
