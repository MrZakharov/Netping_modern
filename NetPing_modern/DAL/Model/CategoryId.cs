namespace NetPing_modern.DAL
{
    public static class CategoryId
    {
        public const string MonitoringId = "_nping_bases";
        public const string PowerId = "_nping_power";
        public const string SwitchesId = "_swtch";

        public static class MonitoringSection
        {
            public const string DevicesId = "_nping_bases";
            public const string SensorsId = "_acces_sensr";
            public const string AccessoriesId = "_acces_mains";
            public const string SolutionsId = "_solut_sensr";
        }

        public static class PowerSection
        {
            public const string DevicesId = "_nping_power";
            public const string SensorsId = "_acces_sensr";
            public const string AccessoriesId = "_acces_mains";
            public const string SolutionsId = "_solut_power";
        }

        public static class SwitchesSection
        {
            public const string DevicesId = "_swtch";
            public const string AccessoriesId = "_acces_mains";
            public const string SolutionsId = "_solut_poe";
        }
    }
}