using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using FSkyss.Core.Interfaces;
using FSkyss.Core.Models;
using FSkyss.Core.Models.Geography;
using FSkyss.Core.Models.Geometry;
using FSkyss.Plugins.Geo.ArcGis;
using FSkyss.Plugins.Geo.ArcGis.SpecialCases;

namespace FSkyss.Modules.Geo
{
    [Export(typeof(IPlugin))]
    [Export(typeof(IDataProvider))]
    public class ArcGisDataProvider : PluginBase, IDataProvider
    {
        public async Task<IEnumerable<T>> GetAllAsync<T>(object rootObject = null, bool includeProperties = false) where T : class
        {
            var source = new ArcGisSource(new()
            {
                Source = Name,
                SchoolDoors = new()
                {
                    SourceUrl = SchoolDoorSourceUrl,
                    NameField = "Skolenavn",
                    NumberField = "Skolenummer",
                },
                Zones = new()
                {
                    SourceUrl = ZoneSourceUrl,
                    NameField = ZoneNameField,
                    NumberField = ZoneNumberField
                }
            });

            if (typeof(T) == typeof(ZoneShape) && EnableZoneSource)
                return (IEnumerable<T>)await source.GetAllZonesAsync();

            if (typeof(T) == typeof(SchoolDoor) && EnableSchoolDoorSource)
                return (IEnumerable<T>)await source.GetSchoolDoorsAsync();

            if (typeof(T) == typeof(SchoolDistrictShape) && EnableSpecialCaseBergensKart)
            {
                var bk = new BergensKart();
                return (IEnumerable<T>)await bk.GetSchoolDistrictsAsync();
            }

            return new List<T>();
        }



        public Task<T> GetSingleAsync<T>(string id, bool includeProperties = false) where T : class
            => throw new NotImplementedException();

        public IEnumerable<Type> GetAvailableTypes()
        {
            yield return typeof(BaseShape);
            if (EnableZoneSource) yield return typeof(ZoneShape);
            if (EnableSchoolDoorSource) yield return typeof(SchoolDoor);
            if (EnableSpecialCaseBergensKart) yield return typeof(SchoolDistrictShape);
        }
        #region Plugin boilerplate

        public override string Name => "ArcGisDataProvider";
        public override string Description => "Leverer geodata fra ArcGis-kilder";

        public override List<ISetting> DefaultAppSettings => new List<ISetting>
        {
            new AppSetting(Name, "ZoneSourceUrl",
                "https://services3.arcgis.com/Hk7T1hgTNPByMamY/arcgis/rest/services/kollektivsoner_buss/FeatureServer",
                "URL til MapServer med soner") { SubCategory = "Zones"},
            new AppSetting(Name, "EnableZoneSource", true, "Aktiver sone-kilde") { SubCategory = "Zones"},
            new AppSetting(Name, "ZoneNameField", "Sonenavn", "Felt i svar fra server som inneholder sonenavnet") { SubCategory = "Zones"},
            new AppSetting(Name, "ZoneNumberField", "Sonenr", "Felt i svar fra server som inneholder sonenummer") { SubCategory = "Zones"},
            new AppSetting(Name, "EnableSpecialCaseBergensKart", false, "(Special case) Aktiver skolekretser fra Bergenskart") { SubCategory = "Zones"},

            new AppSetting(Name, "SchoolDoorSourceUrl",
                "",
                "URL til MapServer med skoledører") { SubCategory = "SchoolDoors"},
            new AppSetting(Name, "EnableSchoolDoorSource", false, "Aktiver skoledør-kilde") { SubCategory = "SchoolDoors"},
        };

        private bool EnableZoneSource => Settings.GetBool("EnableZoneSource");
        private bool EnableSpecialCaseBergensKart => Settings.GetBool("EnableSpecialCaseBergensKart");
        private string ZoneSourceUrl => Settings.GetString("ZoneSourceUrl");
        private string ZoneNameField => Settings.GetString("ZoneNameField");
        private string ZoneNumberField => Settings.GetString("ZoneNumberField");

        private bool EnableSchoolDoorSource
        {
            get
            {
                var enabled = Settings.GetBool("EnableSchoolDoorSource");
                var settingsOk = !string.IsNullOrEmpty(SchoolDoorSourceUrl);

                return enabled && settingsOk;
            }
        }

        private string SchoolDoorSourceUrl => Settings.GetString("SchoolDoorSourceUrl");

        public override PluginStatus GetStatus()
        {
            //var availableTypes = GetAvailableTypes().Select(t => t.Name);
            return new PluginStatus
            {
                IsOnline = true,
                ResponseTime = 0,
                StatusMessage = "OK" //  $"Tilbyr typer: {string.Join(", ", availableTypes)}"
            };
        }

        public override PluginInfo GetPluginInfo()
        {
            return new PluginInfo
            {
                Name = Name,
                Status = GetStatus(),
                Version = typeof(ArcGisDataProvider).Assembly.GetName().Version.ToString(),
            };
        }
        #endregion Plugin boilerplate
    }
}
