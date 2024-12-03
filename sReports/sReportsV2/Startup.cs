﻿using Autofac;
using ExcelImporter.Constants;
using ExcelImporter.Importers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Serilog;
using sReportsV2.Cache.Singleton;
using sReportsV2.Common.Constants;
using sReportsV2.Common.Enums;
using sReportsV2.Common.Helpers;
using sReportsV2.Configs;
using sReportsV2.DAL.Sql.Interfaces;
using sReportsV2.DAL.Sql.Sql;
using sReportsV2.Domain.DatabaseMigrationScripts;
using sReportsV2.Domain.Sql.Entities.ChemotherapySchema;
using sReportsV2.Domain.Sql.Entities.GlobalThesaurusUser;
using sReportsV2.Domain.Sql.Entities.OrganizationEntities;
using sReportsV2.Domain.Sql.Entities.User;
using sReportsV2.HL7.Components;
using sReportsV2.HL7.Constants;
using sReportsV2.Initializer.AccessManagment;
using sReportsV2.Initializer.Codes;
using sReportsV2.Initializer.CodeSets;
using sReportsV2.SqlDomain.Interfaces;
using sReportsV2.UMLS.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using sReportsV2.Common.Configurations;
using sReportsV2.MapperProfiles;
using sReportsV2.Common.Extensions;
using sReportsV2.App_Start;
using sReportsV2.DAL.Sql.Implementations;
using sReportsV2.SqlDomain.Implementations;
using sReportsV2.Domain.Mongo;
using Microsoft.EntityFrameworkCore;
using sReportsV2.BusinessLayer.Implementations;
using sReportsV2.BusinessLayer.Interfaces;
using AutoMapper;

namespace sReportsV2
{
    public partial class Startup
    {
        public IConfiguration Configuration { get; }
        private const string defaultUsername = "nikola.cihoric";
        private const string defaultOrganization = ResourceTypes.CompanyName;

        private bool ShouldInitData(IWebHostEnvironment env)
        {
            return !env.IsDevelopment();
        }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            MongoConfiguration.ConnectionString = Configuration["MongoDB"];
            DirectoryHelper.ProjectBaseDirectory = env.ContentRootPath;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            SerilogConfiguration.ConfigureWritingToFile(Configuration);
            Log.Information("New deploy has started.");

            services.AddLogging();
            services.AddHttpContextAccessor();
            services.AddTransient(typeof(CommonGlobalAfterMapping<>));

            services.AddSingleton<GlobalExceptionHandler>();

            services.AddDbContext<SReportsContext>(options => options.UseSqlServer(Configuration["Sql"], sqlOptions =>
            {
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            }));

            // Register MongoDB
            var mongoConnectionString = Configuration["MongoDB"];
            services.AddSingleton<IMongoClient>(s => new MongoClient(mongoConnectionString));
            services.AddSingleton<SingletonDataContainer>();

            services.AddCustomAutoMapper();

            // Register MVC services
            services.AddControllers()
                    .AddNewtonsoftJson();

            services.AddScoped<AccountService>();
            services.AddScoped<IModuleDAL, ModuleDAL>();
            services.AddScoped<IPermissionDAL, PermissionDAL>();
            services.AddScoped<IPermissionModuleDAL, PermissionModuleDAL>();
            services.AddScoped<ICodeSetDAL, CodeSetDAL>();
            services.AddScoped<ICodeDAL, CodeDAL>();
            services.AddScoped<ICodeAliasViewDAL, CodeAliasViewDAL>();
            services.AddScoped<ICodeSystemDAL, CodeSystemDAL>();
            services.AddScoped<IPositionPermissionDAL, PositionPermissionDAL>();
            services.AddScoped<PositionPermissionInitializer>();
            services.AddScoped<IRestRequestSender, RestRequestSender>();
            services.AddScoped<IBodySurfaceCalculationFormulaDAL, BodySurfaceCalculationFormulaDAL>();

            ConfigureAuth(services);

            services.AddHttpContextAccessor();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(2);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
            services.AddAuthorization();
            services.AddControllersWithViews().AddRazorRuntimeCompilation();
            services.AddRazorPages();
            services.AddTransient<IStartupFilter, RequestStartupFilter>();

            StartMllpServer(services);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            if (!env.IsDevelopment())
            {
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                string defaultController = Configuration["DefaultController"];
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=" + defaultController + "}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });

            app.Use(async (context, next) =>
            {
                GlobalConfig.Configure(context.RequestServices.GetService<IHttpContextAccessor>());
                await next.Invoke();
            });

            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SReportsContext>();
                dbContext.Database.Migrate();

                var migrator = new MongoMigrator(Configuration, dbContext);
                migrator.SetToLatestVersion();

                if (ShouldInitData(env))
                {
                    if (Configuration.IsGlobalThesaurusRunning())
                    {
                        InitializeGlobalThesarus(serviceProvider);
                    }
                    else
                    {
                        Initialize(serviceProvider, app);
                    }
                }
                
                SingletonDataContainer.Initialize(scope.ServiceProvider);
            }

            Log.Information("Application is running.");
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            // Register your services directly with Autofac here. This is instead of the ConfigureServices method.
            RegisterServices(builder);
        }

        private void RegisterServices(ContainerBuilder builder)
        {
            // Register MVC controllers
            builder.RegisterAssemblyTypes(typeof(Startup).Assembly)
                   .Where(t => t.Name.EndsWith("Controller"))
                   .InstancePerLifetimeScope();

            AutofacConfiguration.Configure(builder, Configuration);
        }

        #region default_initialisation
        private void Initialize(IServiceProvider serviceProvider, IApplicationBuilder app)
        {
            PopulateInitialData(serviceProvider);
            PopulatePermissions(app);
            PopulateUsers(serviceProvider);
            PopulateExtendedProperties(serviceProvider);
        }

        private void PopulatePermissions(IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var initializer = scope.ServiceProvider.GetRequiredService<PositionPermissionInitializer>();
                initializer.SetInitialData();
            }
        }

        private void PopulateUsers(IServiceProvider serviceProvider)
        {
            var userDAL = serviceProvider.GetService<IPersonnelDAL>();
            var codeDAL = serviceProvider.GetService<ICodeDAL>();

            if (userDAL.CountAll() == 0)
            {
                int organizationId = InsertDefaultOrganization(serviceProvider);
                string salt = PasswordHelper.CreateSalt(10);
                int? activeUserStateCD = codeDAL.GetByCodeSetIdAndPreferredTerm((int)CodeSetList.UserState, CodeAttributeNames.Active);

                Personnel user = new Personnel(defaultUsername, PasswordHelper.Hash(ResourceTypes.DefaultPass, salt), salt, "nikola.cihoric@insel.ch", "Nikola", "Cihoric", DateTime.Now, organizationId)
                {

                    Organizations = new List<PersonnelOrganization>()
                    {
                        new PersonnelOrganization()
                        {
                            OrganizationId = organizationId,
                            StateCD = activeUserStateCD
                        }
                    }
                };
                userDAL.InsertOrUpdate(user);
            }
            int? archivedUserStateCD = codeDAL.GetByCodeSetIdAndPreferredTerm((int)CodeSetList.UserState, CodeAttributeNames.Archived);

            AssignRolesIfNotSet(userDAL, serviceProvider);
            userDAL.UpdateUsersCountForAllOrganization(archivedUserStateCD);
        }

        private int InsertDefaultOrganization(IServiceProvider serviceProvider)
        {
            var codeDAL = serviceProvider.GetService<ICodeDAL>();
            int? countryCD = codeDAL.GetByCodeSetIdAndPreferredTerm((int)CodeSetList.Country, ResourceTypes.CompanyCountry);
            var organizationDAL = serviceProvider.GetService<IOrganizationDAL>();
            Organization organization = new Organization()
            {
                Name = defaultOrganization,
                OrganizationAddress = new OrganizationAddress()
                {
                    City = "Zurich",
                    PostalCode = "6312",
                    Street = "Sumpfstrasse 24",
                    CountryCD = countryCD,
                    State = ResourceTypes.CompanyCountry,
                    StreetNumber = 24,
                },
                TimeZoneOffset = "+01:00",
                TimeZone = "(UTC+01:00) Amsterdam, Berlin, Bern, Rome, Stockholm, Vienna"
            };

            organizationDAL.InsertOrUpdate(organization);

            return organization.OrganizationId;
        }

        private void AssignRolesIfNotSet(IPersonnelDAL userDAL, IServiceProvider serviceProvider)
        {
            var codeDAL = serviceProvider.GetService<ICodeDAL>();
            int? superAdministratorPositionId = codeDAL.GetByPreferredTerm(PredifinedRole.SuperAdministrator.ToString(), CodeSetAttributeNames.Role)?.CodeId;
            var user = userDAL.GetByUsername(defaultUsername);
            if (user != null && user.PersonnelPositions.Count == 0 && superAdministratorPositionId.HasValue)
            {
                user.UpdateRoles(new List<int>() { superAdministratorPositionId.Value });
                userDAL.InsertOrUpdate(user);
            }
        }

        private void StartMllpServer(IServiceCollection services)
        {
            bool hasMllpPortEnvironmentVariable = Environment.GetEnvironmentVariable(HL7Constants.MLLP_PORT, EnvironmentVariableTarget.Machine) != null;
            if (Configuration.IsSReportsRunning() && hasMllpPortEnvironmentVariable)
            {
                services.AddHostedService<HL7MllpServer>();
            }
        }

        private void PopulateInitialData(IServiceProvider serviceProvider)
        {
            InsertCodingSystems(serviceProvider);
            PopulateChemotherapySchemaInitialData(serviceProvider);
            InsertCodeSets(serviceProvider);
            InsertCodes(serviceProvider);
            UpdateCodeSetIds(serviceProvider);
        }

        private void InsertCountriesFromExcel(IServiceProvider serviceProvider)
        {
            var codeDAL = serviceProvider.GetService<ICodeDAL>();
            var thesaurusDAL = serviceProvider.GetService<IThesaurusDAL>();
            string fileAndSheetName = "CountryCodes";
            CountryCodeImporter countryCodeImporter = new CountryCodeImporter(fileAndSheetName, fileAndSheetName, thesaurusDAL, codeDAL, serviceProvider.GetService<ICodeSystemDAL>(), serviceProvider.GetService<IThesaurusTranslationDAL>(), serviceProvider.GetService<IO4CodeableConceptDAL>(),
                serviceProvider.GetService<ICodeSetDAL>(), Configuration
                );
            countryCodeImporter.ImportDataFromExcelToDatabase();
        }

        private void InsertCodeSets(IServiceProvider serviceProvider)
        {
            var codeSetDAL = serviceProvider.GetService<ICodeSetDAL>();
            var thesaurusDAL = serviceProvider.GetService<IThesaurusDAL>();
            CodeSetsImporter importer = new CodeSetsImporter(codeSetDAL, thesaurusDAL);
            importer.Import();
        }

        private void InsertCodes(IServiceProvider serviceProvider)
        {
            InsertCountriesFromExcel(serviceProvider);

            var codeDAL = serviceProvider.GetService<ICodeDAL>();
            var codeSetDAL = serviceProvider.GetService<ICodeSetDAL>();
            var thesaurusDAL = serviceProvider.GetService<IThesaurusDAL>();
            var codeAssociationDAL = serviceProvider.GetService<ICodeAssociationDAL>();
            CodesImporter importer = new CodesImporter(codeDAL, codeSetDAL, thesaurusDAL, codeAssociationDAL, Configuration);
            importer.Import();
        }

        private void InsertCodingSystems(IServiceProvider serviceProvider)
        {
            SqlImporter importer = new SqlImporter(serviceProvider.GetService<IThesaurusDAL>(),
                serviceProvider.GetService<IThesaurusTranslationDAL>(),
                serviceProvider.GetService<IO4CodeableConceptDAL>(),
                serviceProvider.GetService<ICodeDAL>(),
                serviceProvider.GetService<ICodeSystemDAL>(), serviceProvider.GetService<IAdministrativeDataDAL>(),
                Configuration);
            importer.ImportCodingSystems();
        }

        private void PopulateExtendedProperties(IServiceProvider serviceProvider)
        {
            var administrativeDataDAL = serviceProvider.GetService<IAdministrativeDataDAL>();

            ExtendedPropertiesImporter extendedPropertiesColumnImporter = new ExtendedPropertiesImporter(administrativeDataDAL, ExtendedPropertiesConstants.FileName, ExtendedPropertiesConstants.ColumnDescriptionSheet);
            extendedPropertiesColumnImporter.ImportDataFromExcelToDatabase();
            ExtendedPropertiesImporter extendedPropertiesTableImporter = new ExtendedPropertiesImporter(administrativeDataDAL, ExtendedPropertiesConstants.FileName, ExtendedPropertiesConstants.TableDescriptionSheet);
            extendedPropertiesTableImporter.ImportDataFromExcelToDatabase();
        }

        private void UpdateServiceTypes(IServiceProvider serviceProvider)
        {
            var encounterDAL = serviceProvider.GetService<IEncounterDAL>();
            var codeDAL = serviceProvider.GetService<ICodeDAL>();
            var serviceTypes = codeDAL.GetByCodeSetId((int)CodeSetList.ServiceType);
            foreach (var encounter in encounterDAL.GetAll())
            {
                encounter.ServiceTypeCD = serviceTypes.Where(x => x.ThesaurusEntryId == encounter.ServiceTypeCD).Any()
                    ? serviceTypes.Where(x => x.ThesaurusEntryId == encounter.ServiceTypeCD).FirstOrDefault()?.CodeId
                    : serviceTypes.FirstOrDefault()?.CodeId;

                encounterDAL.InsertOrUpdate(encounter);
            }
        }

        private void UpdateCodeSetIds(IServiceProvider serviceProvider)
        {
            var codeSetProperties = Assembly.GetAssembly(typeof(CodeSetValues)).GetTypes().Where(x => x.Name == "CodeSetValues").FirstOrDefault()?.GetProperties();
            var codeSetDAL = serviceProvider.GetService<ICodeSetDAL>();
            int tempCodeSetId = 3001;
            if (codeSetProperties != null)
            {
                foreach (var codeSetName in codeSetProperties)
                {
                    var newCodeSet = codeSetDAL.GetByPreferredTerm(codeSetName.CustomAttributes?.FirstOrDefault()?.ConstructorArguments?.FirstOrDefault().Value.ToString());
                    var properCodeSetId = (int)(CodeSetList)Enum.Parse(typeof(CodeSetList), codeSetName.Name);
                    var oldCodeSet = codeSetDAL.GetById(properCodeSetId);

                    if (oldCodeSet != null && newCodeSet.CodeSetId != oldCodeSet.CodeSetId)
                    {
                        oldCodeSet.NewCodeSetId = tempCodeSetId;
                        codeSetDAL.Insert(oldCodeSet);
                        tempCodeSetId++;
                    }
                    if (newCodeSet.CodeSetId != properCodeSetId)
                    {
                        newCodeSet.NewCodeSetId = properCodeSetId;
                        codeSetDAL.Insert(newCodeSet);
                        if (properCodeSetId == (int)CodeSetList.ServiceType)
                            UpdateServiceTypes(serviceProvider);
                    }
                }
            }
        }

        #region smart_oncology_initialisation

        private void PopulateChemotherapySchemaInitialData(IServiceProvider serviceProvider)
        {
            AddUnits(serviceProvider);
            ImportChemotherapySchemaDataFromExcel(serviceProvider);
            ImportChemotherapySchemasFromExcel(serviceProvider);
        }

        private void AddUnits(IServiceProvider serviceProvider)
        {
            var unitDAL = serviceProvider.GetService<IUnitDAL>();
            if (unitDAL.GetAllCount() == 0)
            {
                List<Unit> units = new List<Unit> {
                    {
                        new Unit { Name = "mg", Description = "A unit of measurement of mass in the metric system equal to a thousandth of a gram"}
                    },
                    {
                        new Unit { Name = "g", Description = "A unit of measurement of mass in the metric system"}
                    },
                    {
                        new Unit { Name = "IU", Description = "An abbreviations which stands for International Units"}
                    },
                    {
                        new Unit { Name = "ml", Description = "A metric unit used to measure capacity that's equal to one-thousandth of a liter"}
                    },
                    {
                        new Unit { Name = "mg/m2", Description = "A unit for Body Surface Area Based Dosing"}
                    },
                    {
                        new Unit { Name = "AUC", Description = "An abbreviations for Area Under the curve"}
                    }
                };
                unitDAL.InsertMany(units);
            }
        }

        private void ImportChemotherapySchemaDataFromExcel(IServiceProvider serviceProvider)
        {
            IBodySurfaceCalculationFormulaDAL bodySurfaceCalculationFormulaDAL = serviceProvider.GetService<IBodySurfaceCalculationFormulaDAL>();
            BodySurfaceCalculationFormulaImporter bodySurfaceCalculationFormulaImporter = new BodySurfaceCalculationFormulaImporter(bodySurfaceCalculationFormulaDAL, ChemotherapySchemaConstants.ChemOncAdditionalDataFile, ChemotherapySchemaConstants.BodySurfaceCalculationFormulaSheet);
            bodySurfaceCalculationFormulaImporter.ImportDataFromExcelToDatabase();

            var routeOfAdministrationDAL = serviceProvider.GetService<IRouteOfAdministrationDAL>();
            RouteOfAdministrationImporter routeOfAdministrationImporter = new RouteOfAdministrationImporter(routeOfAdministrationDAL, ChemotherapySchemaConstants.ChemOncAdditionalDataFile, ChemotherapySchemaConstants.RouteOfAdministrationSheet);
            routeOfAdministrationImporter.ImportDataFromExcelToDatabase();

            var medicationDoseTypeDAL = serviceProvider.GetService<IMedicationDoseTypeDAL>();
            MedicationDoseTypeImporter medicationDoseTypeImporter = new MedicationDoseTypeImporter(medicationDoseTypeDAL, ChemotherapySchemaConstants.ChemOncDrugDosingTimeFile, ChemotherapySchemaConstants.DrugDosingTimeSheet);
            medicationDoseTypeImporter.ImportDataFromExcelToDatabase();
        }

        private void ImportChemotherapySchemasFromExcel(IServiceProvider serviceProvider)
        {
            var userDAL = serviceProvider.GetService<IPersonnelDAL>();
            Personnel user = userDAL.GetByUsername(defaultUsername);
            if (user != null)
            {
                SchemaImporterV2 schemaImporterV2 = new SchemaImporterV2("Chemotherapy Compendium Import - 26.11.2021", "Basic Data", serviceProvider.GetService<IChemotherapySchemaDAL>(), serviceProvider.GetService<IRouteOfAdministrationDAL>(),
                  serviceProvider.GetService<IUnitDAL>(),
                user.PersonnelId);
                schemaImporterV2.ImportDataFromExcelToDatabase();
            }
        }

        #endregion

        #endregion

        #region global_thesaurus_initialisation
        private void InitializeGlobalThesarus(IServiceProvider serviceProvider)
        {
            PopulateGlobalThesaurusRoles(serviceProvider);
            SetGlobalThesaurusUsers(serviceProvider);
            PopulateGlobalThesaurusInitialData(serviceProvider);
        }

        private void PopulateGlobalThesaurusRoles(IServiceProvider serviceProvider)
        {
            IGlobalThesaurusRoleDAL globalThesaurusRoleDAL = serviceProvider.GetService<IGlobalThesaurusRoleDAL>();

            if (globalThesaurusRoleDAL.Count() == 0)
            {

                GlobalThesaurusRole roleSuperAdministrator = new GlobalThesaurusRole
                {
                    Name = PredifinedGlobalUserRole.SuperAdministrator.ToString(),
                    Description = "A super administrator is a person who has full access to user and any other modules. Super administrator can manage user's roles."
                };
                GlobalThesaurusRole roleViewer = new GlobalThesaurusRole
                {
                    Name = PredifinedGlobalUserRole.Viewer.ToString(),
                    Description = "A viewer can access to the smart oncology content and view it."
                };
                GlobalThesaurusRole roleEditor = new GlobalThesaurusRole
                {
                    Name = PredifinedGlobalUserRole.Editor.ToString(),
                    Description = "A editor can edit terminology terms."
                };
                GlobalThesaurusRole roleCurator = new GlobalThesaurusRole
                {
                    Name = PredifinedGlobalUserRole.Curator.ToString(),
                    Description = "A curator can edit and cure terms."
                };

                globalThesaurusRoleDAL.InsertOrUpdate(roleSuperAdministrator);
                globalThesaurusRoleDAL.InsertOrUpdate(roleViewer);
                globalThesaurusRoleDAL.InsertOrUpdate(roleEditor);
                globalThesaurusRoleDAL.InsertOrUpdate(roleCurator);
            }
        }

        private void SetGlobalThesaurusUsers(IServiceProvider serviceProvider)
        {
            var userRoleDAL = serviceProvider.GetService<IGlobalThesaurusRoleDAL>();
            var roles = userRoleDAL.GetAll().ToList();
            var userDAL = serviceProvider.GetService<IGlobalThesaurusUserDAL>();
            var codeDAL = serviceProvider.GetService<ICodeDAL>();
            SetGlobalThesaurusSuperAdministrators(userDAL, codeDAL, roles);
        }

        private void SetGlobalThesaurusSuperAdministrators(IGlobalThesaurusUserDAL userDAL, ICodeDAL codeDAL, List<GlobalThesaurusRole> roles)
        {
            List<int> roleIds = roles.Select(x => x.GlobalThesaurusRoleId).ToList();
            foreach (var superAdministrator in GetGlobalThesaurusSuperAdministrators(codeDAL))
            {
                var user = userDAL.GetByEmail(superAdministrator.Email);
                if (user == null)
                {
                    GlobalThesaurusUser userDb = superAdministrator;
                    userDb.UpdateRoles(roleIds);
                    userDAL.InsertOrUpdate(userDb);
                }
            }
        }

        private List<GlobalThesaurusUser> GetGlobalThesaurusSuperAdministrators(ICodeDAL codeDAL)
        {
            int? internalSourceCD = codeDAL.GetByCodeSetIdAndPreferredTerm((int)CodeSetList.GlobalUserSource, CodeAttributeNames.Internal);
            int? activeStatusCD = codeDAL.GetByCodeSetIdAndPreferredTerm((int)CodeSetList.GlobalUserStatus, CodeAttributeNames.Active);

            return new List<GlobalThesaurusUser>() {
                new GlobalThesaurusUser()
                {
                    Country = ResourceTypes.CompanyCountry,
                    FirstName = "Nikola",
                    LastName = "Cihoric",
                    Affiliation = "MD",
                    StatusCD = activeStatusCD,
                    Email = "nikola.cihoric@wemedoo.com",
                    Password = Configuration["DefaultPassword"],
                    SourceCD = internalSourceCD,
                    Phone = ""
                }
            };
        }

        private void PopulateGlobalThesaurusInitialData(IServiceProvider serviceProvider)
        {
            var thesaurusDAL = serviceProvider.GetService<IThesaurusDAL>();
            var translationDAL = serviceProvider.GetService<IThesaurusTranslationDAL>();
            var codeableConceptDAL = serviceProvider.GetService<IO4CodeableConceptDAL>();
            var codeDAL = serviceProvider.GetService<ICodeDAL>();
            var globalUserDAL = serviceProvider.GetService<IGlobalThesaurusUserDAL>();
            var administrativeDataDAL = serviceProvider.GetService<IAdministrativeDataDAL>();

            InsertCodingSystems(serviceProvider);

            GlobalThesaurusImporter globalThesaurusExcelImporter = new GlobalThesaurusImporter(
                thesaurusDAL,
                globalUserDAL,
                translationDAL,
                codeableConceptDAL,
                codeDAL,
                administrativeDataDAL,
                GlobalThesaurusConstants.FileName,
                GlobalThesaurusConstants.Sheet,
                Configuration
             );
            globalThesaurusExcelImporter.ImportDataFromExcelToDatabase();
        }

        #endregion
    }
}
