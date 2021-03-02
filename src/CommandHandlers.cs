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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ecf.Edoosys
{
    public static class CommandHandlers
    {
        public static async Task Export(FileInfo configFile)
        {
            await Execute(async (cancellationToken, cancellationEvent) =>
            {
                CustomManager exportManager;

                var config = await ConfigurationManager.LoadFromFile(configFile.FullName, cancellationToken);

                if (config.EcfExport?.SourceProvider == EcfSourceProvider.Postgres)
                {
                    exportManager = new DbExportManager(
                        config,
                        cancellationToken, cancellationEvent);
                }
                else
                {
                    exportManager = new CsvExportManager(
                        config,
                        cancellationToken, cancellationEvent);
                }

                try
                {
                    await exportManager.Execute();
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine($"[Error] Export failed");
                    Console.WriteLine($"[Error] {ex.Message}");
                    Environment.ExitCode = 1;
                }
            });
        }

        private static async Task Execute(Func<CancellationToken, EventWaitHandle, Task> action)
        {
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                using (var cancellationEvent = new EventWaitHandle(false, EventResetMode.AutoReset))
                {
                    Console.CancelKeyPress += (sender, eventArgs) =>
                    {
                        eventArgs.Cancel = true;
                        cancellationTokenSource.Cancel();
                        cancellationEvent.Set();
                    };

                    var stopwatch = new Stopwatch();

                    stopwatch.Start();

                    try
                    {
                        await action(cancellationTokenSource.Token, cancellationEvent);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"[Error] Console failed");
                        Console.WriteLine($"[Error] {ex.Message}");
                        Environment.ExitCode = 1;
                    }

                    stopwatch.Stop();

                    Console.WriteLine();
                    Console.WriteLine($"Time elapsed: {stopwatch.Elapsed}.");
                    Console.WriteLine();
                }
            }
        }
    }
}