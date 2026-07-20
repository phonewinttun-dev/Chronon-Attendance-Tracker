namespace ACST.Shared
{
    public static class Permissions
    {
        public static class Roles
        {
            public const string View = "Permissions.Roles.View";
            public const string Manage = "Permissions.Roles.Manage";
        }

        public static class Modules
        {
            public const string View = "Permissions.Modules.View";
            public const string Create = "Permissions.Modules.Create";
            public const string Update = "Permissions.Modules.Update";
            public const string Delete = "Permissions.Modules.Delete";
        }

        public static class Semesters
        {
            public const string View = "Permissions.Semesters.View";
            public const string Create = "Permissions.Semesters.Create";
            public const string Update = "Permissions.Semesters.Update";
            public const string Delete = "Permissions.Semesters.Delete";
        }

        public static class ClassSessions
        {
            public const string View = "Permissions.ClassSessions.View";
            public const string Manage = "Permissions.ClassSessions.Manage";
            public const string Delete = "Permissions.ClassSessions.Delete";
        }

        public static class Analytics
        {
            public const string View = "Permissions.Analytics.View";
        }

        public static class Holidays
        {
            public const string View = "Permissions.Holidays.View";
            public const string Manage = "Permissions.Holidays.Manage";
        }

        public static class RecurringSchedules
        {
            public const string View = "Permissions.RecurringSchedules.View";
            public const string Manage = "Permissions.RecurringSchedules.Manage";
        }

        public static class GoogleCalendar
        {
            public const string Manage = "Permissions.GoogleCalendar.Manage";
        }

        public static class Search
        {
            public const string View = "Permissions.Search.View";
        }
    }
}
