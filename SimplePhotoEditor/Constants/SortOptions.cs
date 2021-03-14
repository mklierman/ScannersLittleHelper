using System;
using System.Collections.Generic;
using System.Text;

namespace SimplePhotoEditor.Constants
{
    public static class SortOptions
    {
        public static readonly KeyValuePair<string, string>[] SortByKeyValuePair =
        {
            new KeyValuePair<string, string>("CreatedDate", "Created Date"),
            new KeyValuePair<string, string>("ModifiedDate", "Modified Date"),
            new KeyValuePair<string, string>("FileName", "File Name")
        };

        public static readonly KeyValuePair<string, string>[] SortAscDescKeyValuePair =
        {
            new KeyValuePair<string, string>("Asc", "Ascending"),
            new KeyValuePair<string, string>("Desc", "Descending")
        };
    }
}
