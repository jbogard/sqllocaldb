﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="https://github.com/martincostello/sqllocaldb">
//   Martin Costello (c) 2012-2015
// </copyright>
// <license>
//   See license.txt in the project root for license information.
// </license>
// <summary>
//   Program.cs
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;
using System.Security.Principal;

namespace System.Data.SqlLocalDb
{
    /// <summary>
    /// An application that acts as a test harness for the <c>System.Data.SqlLocalDb</c> assembly.  This class cannot be inherited.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// The main entry point to the application.
        /// </summary>
        /// <param name="args">The command-line arguments passed to the application.</param>
        [Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Usage",
            "CA2202:Do not dispose objects multiple times",
            Justification = "It isn't.")]
        internal static void Main(string[] args)
        {
            PrintBanner();

            ISqlLocalDbApi localDB = new SqlLocalDbApiWrapper();

            if (!localDB.IsLocalDBInstalled())
            {
                Console.WriteLine(SR.SqlLocalDbApi_NotInstalledFormat, Environment.MachineName);
                return;
            }

            if (args?.Length == 1 && string.Equals(args[0], "/deleteuserinstances", StringComparison.OrdinalIgnoreCase))
            {
                SqlLocalDbApi.DeleteUserInstances(deleteFiles: true);
            }

            ISqlLocalDbProvider provider = new SqlLocalDbProvider();

            IList<ISqlLocalDbVersionInfo> versions = provider.GetVersions();

            Console.WriteLine(Strings.Program_VersionsListHeader);
            Console.WriteLine();

            foreach (ISqlLocalDbVersionInfo version in versions)
            {
                Console.WriteLine(version.Name);
            }

            Console.WriteLine();

            IList<ISqlLocalDbInstanceInfo> instances = provider.GetInstances();

            Console.WriteLine(Strings.Program_InstancesListHeader);
            Console.WriteLine();

            foreach (ISqlLocalDbInstanceInfo instanceInfo in instances)
            {
                Console.WriteLine(instanceInfo.Name);
            }

            Console.WriteLine();

            string instanceName = Guid.NewGuid().ToString();

            ISqlLocalDbInstance instance = provider.CreateInstance(instanceName);

            instance.Start();

            try
            {
                if (IsCurrentUserAdmin())
                {
                    instance.Share(Guid.NewGuid().ToString());
                }

                try
                {
                    using (SqlConnection connection = instance.CreateConnection())
                    {
                        connection.Open();

                        try
                        {
                            using (SqlCommand command = new SqlCommand("create database [MyDatabase]", connection))
                            {
                                command.ExecuteNonQuery();
                            }

                            using (SqlCommand command = new SqlCommand("drop database [MyDatabase]", connection))
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                        finally
                        {
                            connection.Close();
                        }
                    }
                }
                finally
                {
                    if (IsCurrentUserAdmin())
                    {
                        instance.Unshare();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                instance.Stop();
                localDB.DeleteInstance(instance.Name);
            }

            Console.WriteLine();
            Console.Write(Strings.Program_ExitPrompt);
            Console.ReadKey();
        }

        /// <summary>
        /// Returns whether the current user is in the administrators group on the local machine.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the current user is in the administrators
        /// group on the local machine; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsCurrentUserAdmin()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        /// <summary>
        /// Prints a banner to the console containing assembly and operating system information.
        /// </summary>
        private static void PrintBanner()
        {
            Assembly assembly = typeof(SqlLocalDbApi).Assembly;
            AssemblyName assemblyName = assembly.GetName();

            Console.WriteLine(
                Strings.Program_BannerFormat,
                assemblyName.Name,
                assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright,
                assemblyName.Version,
                assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version,
                assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion,
                assembly.GetCustomAttribute<AssemblyConfigurationAttribute>().Configuration,
                assembly.ImageRuntimeVersion,
                Environment.UserDomainName,
                Environment.UserName,
                IsCurrentUserAdmin(),
                Environment.OSVersion,
                Environment.Is64BitOperatingSystem,
                Environment.Is64BitProcess);
        }
    }
}
